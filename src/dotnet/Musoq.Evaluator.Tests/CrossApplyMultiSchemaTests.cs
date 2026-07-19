using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for cross apply with multiple different schemas.
///     These tests verify that cross apply works correctly when combining
///     different schema data sources, especially in CTE contexts.
/// </summary>
[TestClass]
public class CrossApplyMultiSchemaTests
{
    static CrossApplyMultiSchemaTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    public TestContext TestContext { get; set; }

    private ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();

    /// <summary>
    ///     Test case for issue: CTE with cross apply to a different schema should not throw KeyNotFoundException.
    ///     The query pattern:
    ///     WITH files AS (
    ///     SELECT FullPath FROM osSchema.files() f
    ///     WHERE condition
    ///     )
    ///     SELECT * FROM files f CROSS APPLY abcSchema.something(...args) e
    ///     This previously threw: "The given key 'abcSchema' was not present in the dictionary"
    ///     because 'abcSchema' was treated as a table alias instead of a schema name.
    /// </summary>
    [TestMethod]
    public void WhenCteWithCrossApplyToDifferentSchema_ShouldNotThrowKeyNotFound()
    {
        const string query = @"
            with files as (
                select f.FullPath as FullPath from first.data() f
                where f.FullPath like '%test%'
            )
            select f.FullPath, s.Country, s.Money from files f cross apply second.data() s";

        var firstSource = new[]
        {
            new FileEntity { FullPath = "/path/to/test/file1.txt" },
            new FileEntity { FullPath = "/path/to/test/file2.txt" },
            new FileEntity { FullPath = "/path/to/other/file3.txt" }
        };

        var secondSource = new[]
        {
            new DataEntity { Country = "Country1", Money = 1000m },
            new DataEntity { Country = "Country2", Money = 2000m }
        };

        var vm = CreateVirtualMachineWithTwoSchemas(query, firstSource, secondSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("f.FullPath", typeof(string)),
            ("s.Country", typeof(string)),
            ("s.Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["/path/to/test/file1.txt", "Country1", 1000m],
            ["/path/to/test/file1.txt", "Country2", 2000m],
            ["/path/to/test/file2.txt", "Country1", 1000m],
            ["/path/to/test/file2.txt", "Country2", 2000m]);
    }

    /// <summary>
    ///     Test case for cross apply with different schema without CTE - direct usage.
    /// </summary>
    [TestMethod]
    public void WhenDirectCrossApplyToDifferentSchema_ShouldWork()
    {
        const string query = @"
            select f.FullPath, s.Country, s.Money 
            from first.data() f 
            cross apply second.data() s";

        var firstSource = new[]
        {
            new FileEntity { FullPath = "/path/to/file1.txt" },
            new FileEntity { FullPath = "/path/to/file2.txt" }
        };

        var secondSource = new[]
        {
            new DataEntity { Country = "Country1", Money = 1000m }
        };

        var vm = CreateVirtualMachineWithTwoSchemas(query, firstSource, secondSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("f.FullPath", typeof(string)),
            ("s.Country", typeof(string)),
            ("s.Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["/path/to/file1.txt", "Country1", 1000m],
            ["/path/to/file2.txt", "Country1", 1000m]);
    }

    /// <summary>
    ///     Test case for nested CTEs with cross apply to different schema.
    /// </summary>
    [TestMethod]
    public void WhenNestedCteWithCrossApplyToDifferentSchema_ShouldWork()
    {
        const string query = @"
            with 
                files as (
                    select f.FullPath as FullPath from first.data() f
                ),
                enrichedFiles as (
                    select f.FullPath, s.Country from files f cross apply second.data() s
                )
            select * from enrichedFiles";

        var firstSource = new[]
        {
            new FileEntity { FullPath = "/path/to/file1.txt" }
        };

        var secondSource = new[]
        {
            new DataEntity { Country = "Country1", Money = 1000m },
            new DataEntity { Country = "Country2", Money = 2000m }
        };

        var vm = CreateVirtualMachineWithTwoSchemas(query, firstSource, secondSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("FullPath", typeof(string)),
            ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["/path/to/file1.txt", "Country1"],
            ["/path/to/file1.txt", "Country2"]);
    }

    private CompiledQuery CreateVirtualMachineWithTwoSchemas(
        string script,
        FileEntity[] firstSource,
        DataEntity[] secondSource)
    {
        var firstSchema = new TestSchema<FileEntity>("first", firstSource);
        var secondSchema = new TestSchema<DataEntity>("second", secondSource);

        var provider = new TestMultiSchemaProvider(new Dictionary<string, ISchema>
        {
            { "#first", firstSchema },
            { "#second", secondSchema }
        });

        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver);
    }

    #region Test Entities and Schemas

    public class FileEntity
    {
        public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
        {
            { nameof(FullPath), 0 },
            { nameof(Country), 1 }
        };

        public static readonly IReadOnlyDictionary<int, Func<FileEntity, object?>> IndexToObjectAccessMap =
            new Dictionary<int, Func<FileEntity, object?>>
            {
                { 0, e => e.FullPath },
                { 1, e => e.Country }
            };

        public string FullPath { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    public class DataEntity
    {
        public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
        {
            { nameof(Country), 0 },
            { nameof(Money), 1 }
        };

        public static readonly IReadOnlyDictionary<int, Func<DataEntity, object?>> IndexToObjectAccessMap =
            new Dictionary<int, Func<DataEntity, object?>>
            {
                { 0, e => e.Country },
                { 1, e => e.Money }
            };

        public string Country { get; set; } = string.Empty;
        public decimal Money { get; set; }
    }

    private sealed class TestSchema<TEntity>(
        string name,
        TEntity[] source,
        Func<object?[], GenericChunkSource<TEntity>, RowSource<TEntity>>? filter = null)
        : SchemaBase(name, CreateLibrary())
    {
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            return new GenericEntityTable<TEntity>();
        }

        public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
        {
            var nameToIndex =
                typeof(TEntity).GetField("NameToIndexMap")?.GetValue(null) as IReadOnlyDictionary<string, int>
                ?? throw new InvalidOperationException($"Missing {nameof(GenericChunkSource<>)} name-to-index map.");
            var indexToAccess =
                typeof(TEntity).GetField("IndexToObjectAccessMap")?.GetValue(null) as
                    IReadOnlyDictionary<int, Func<TEntity, object?>>
                ?? throw new InvalidOperationException($"Missing {nameof(GenericChunkSource<>)} index access map.");

            var source1 = new GenericChunkSource<TEntity>(source, nameToIndex, indexToAccess);

            if (filter != null && parameters.Length > 0)
                return EnsureSourceType<T>(name, filter(parameters, source1));

            return EnsureSourceType<T, TEntity>(name, source1);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();
            methodManager.RegisterLibraries(new GenericLibrary());
            return new MethodsAggregator(methodManager);
        }
    }

    private sealed class TestMultiSchemaProvider(IDictionary<string, ISchema> schemas) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return schemas[schema];
        }
    }

    #endregion
}
