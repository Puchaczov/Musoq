using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.External.Contracts;
using Musoq.Evaluator.Tests.External.Rows;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using MusoqApi = Musoq.Converter.Musoq;
using DataSourceSchemaColumn = Musoq.Schema.DataSources.SchemaColumn;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class ExternalClrReferenceEndToEndTests
{
    private readonly ILoggerResolver _loggerResolver = new TestsLoggerResolver();

    [TestMethod]
    public void ExternalConcreteRow_InheritedInterfaceAndNestedPath_CompilesAndRunsEmptyAndOneRow()
    {
        const string query =
            "select r.InheritedName, r.Marker, r.Payload.Nested.Value from #external.rows() r";

        using (var empty = Compile(query, []))
        using (var emptyTable = empty.Run())
        {
            TableMaterializationTestHelper.AssertColumns(
                emptyTable,
                ("r.InheritedName", typeof(string)),
                ("r.Marker", typeof(string)),
                ("r.Payload.Nested.Value", typeof(int)));
            TableMaterializationTestHelper.AssertRowsInOrder(emptyTable);
        }

        var row = CreateRow();
        using var compiled = Compile(query, [row]);
        using var table = compiled.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [row.InheritedName, row.Marker, row.Payload.Nested.Value]);
        AssertFixtureReferenceDelta(query, [typeof(ExternalRow).Assembly, typeof(ExternalPayload).Assembly]);
    }

    [TestMethod]
    public void ExternalNestedFrameworkPropertyPath_AddsIntermediateUriAssembly()
    {
        const string query =
            "select r.Payload.ExternalUri.AbsoluteUri from #external.rows() r";

        using var compiled = Compile(query, [CreateRow()]);
        using var table = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("r.Payload.ExternalUri.AbsoluteUri", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["https://example.invalid/"]);
        AssertFixtureReferenceDelta(
            query,
            [typeof(ExternalRow).Assembly, typeof(ExternalPayload).Assembly, typeof(Uri).Assembly]);
    }

    [TestMethod]
    public void ExternalShapes_ArrayNullableEnumNestedGenericAndConstraint_CompileAndRun()
    {
        const string query =
            "select r.Payload.Nested.Value, r.Payload.Leaves, r.Status from #external.rows() r";

        using var compiled = Compile(query, [CreateRow()]);
        using var table = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("r.Payload.Nested.Value", typeof(int)),
            ("r.Payload.Leaves", typeof(ExternalLeaf[])),
            ("r.Status", typeof(ExternalStatus?)));
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(7, table[0].Values[0]);
        Assert.AreEqual(ExternalStatus.Ready, table[0].Values[2]);
        var leaves = (ExternalLeaf[])table[0].Values[1];
        Assert.HasCount(1, leaves);
        Assert.AreEqual(11, leaves[0].Value);

        var genericProvider = CreateSchemaProvider(
            [CreateRow()],
            useGenericRow: true);
        const string genericQuery =
            "select r.Payload.Nested.Value from #external.generic() r";
        using var genericCompiled = InstanceCreator.CompileForExecution(
            genericQuery,
            Guid.NewGuid().ToString(),
            genericProvider,
            _loggerResolver,
            CreateOptions());
        using var genericTable = genericCompiled.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(genericTable, [7]);
        var genericItems = InstanceCreator.CreateForAnalyze(
            genericQuery,
            Guid.NewGuid().ToString(),
            genericProvider,
            _loggerResolver,
            CreateOptions());
        AssertFixtureReferenceDelta(
            genericItems,
            [typeof(GenericExternalRow<ExternalPayload>).Assembly, typeof(ExternalPayload).Assembly]);
    }

    [TestMethod]
    public void ExternalPluginDeclaringReturnParameterAndGenericTypes_CompileAndRun()
    {
        const string query =
            "select GetContractNumber(r.Payload), GetContractNumber(Echo(r.Payload)), GetLeaf(r.Payload), GetMarker(r.Payload) from #external.rows() r";

        using var compiled = Compile(query, [CreateRow()]);
        using var table = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("GetContractNumber(r.Payload)", typeof(int)),
            ("GetContractNumber(Echo(r.Payload))", typeof(int)),
            ("GetLeaf(r.Payload)", typeof(ExternalLeaf)),
            ("GetMarker(r.Payload)", typeof(string)));
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0].Values[0]);
        Assert.AreEqual(42, table[0].Values[1]);
        Assert.AreEqual(7, ((ExternalLeaf)table[0].Values[2]).Value);
        Assert.AreEqual("external-payload", table[0].Values[3]);
        AssertFixtureReferenceDelta(query, [typeof(ExternalLibrary).Assembly, typeof(ExternalPayload).Assembly]);
    }

    [TestMethod]
    public void ExternalReferences_InspectionStoreLoadAndQueryBuilderPaths_KeepExactFixtureDelta()
    {
        const string query =
            "select r.Payload.ContractNumber as Number, r.InheritedName as Name from #external.rows() r";
        var provider = CreateSchemaProvider([CreateRow()]);

        var inspection = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            CreateOptions());
        var generated = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            CreateOptions());
        StringAssert.Contains(generated.GeneratedCSharpCode, "GetRowSource<Musoq.Evaluator.Tests.External.Rows.ExternalRow>");
        AssertFixtureReferenceDelta(
            inspection,
            [typeof(ExternalRow).Assembly, typeof(ExternalPayload).Assembly]);

        var stored = InstanceCreator.CompileForStore(
            query,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver);
        Assert.IsTrue(stored.DllFile.Length > 0);

        var chunks = new[] { new[] { CreateRow() } };
        var builder = MusoqApi
            .Query("select r.Payload.ContractNumber as Number, r.InheritedName as Name from #fixture.rows() r")
            .Source<ExternalRow>("#fixture", "rows", chunks);
        var typedInspection = builder.InspectTyped<ExternalProjection>();
        StringAssert.Contains(typedInspection.GeneratedCSharpCode, "ExternalRow");

        var artifact = builder.CompileArtifact<ExternalProjection>();
        var loaded = MusoqApi.Load<ExternalProjection>(artifact);
        var result = loaded
            .Run(CancellationToken.None, MusoqApi.Source("#fixture", "rows", chunks))
            .ToArray();
        Assert.HasCount(1, result);
        Assert.AreEqual(new ExternalProjection(42, "inherited"), result[0]);
    }

    [TestMethod]
    public void ExternalReferences_RepeatedCacheHitCacheIneligibleAndConcurrentCompilation_Succeed()
    {
        const string query = "select r.Payload.Nested.Value from #external.rows() r";
        var provider = CreateSchemaProvider([CreateRow()]);

        var first = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            CreateOptions());
        var second = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            CreateOptions());
        Assert.AreSame(
            GetReference(first, typeof(ExternalPayload).Assembly),
            GetReference(second, typeof(ExternalPayload).Assembly));

        var cacheIneligible = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            new CompilationOptions(sourceRuntimeSettingsResolver: new StaticSettingsResolver()));
        AssertFixtureReferenceDelta(
            cacheIneligible,
            [typeof(ExternalRow).Assembly, typeof(ExternalPayload).Assembly]);

        var results = new string[4][];
        Parallel.For(0, results.Length, index =>
        {
            var items = InstanceCreator.CreateForAnalyze(
                query,
                Guid.NewGuid().ToString(),
                provider,
                _loggerResolver,
                CreateOptions());
            results[index] = GetReferenceFileNames(items);
        });

        for (var index = 1; index < results.Length; index++)
            CollectionAssert.AreEqual(results[0], results[index]);
    }

    private CompiledQuery Compile(string query, IReadOnlyList<ExternalRow> rows)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            CreateSchemaProvider(rows),
            _loggerResolver,
            CreateOptions());
    }

    private void AssertFixtureReferenceDelta(
        string query,
        IReadOnlyList<Assembly> expectedAssemblies)
    {
        var items = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            CreateSchemaProvider([CreateRow()]),
            _loggerResolver,
            CreateOptions());
        AssertFixtureReferenceDelta(items, expectedAssemblies);
    }

    private void AssertFixtureReferenceDelta(
        Musoq.Converter.Build.BuildItems items,
        IReadOnlyList<Assembly> expectedAssemblies)
    {
        var baseline = InstanceCreator.CreateForAnalyze(
            "select 1 from #primitive.values()",
            Guid.NewGuid().ToString(),
            CreatePrimitiveSchemaProvider(),
            _loggerResolver,
            CreateOptions());
        var expected = expectedAssemblies
            .SelectMany(static assembly => new[]
            {
                Path.GetFileName(assembly.Location)!
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var actual = GetReferenceFileNames(items)
            .Except(GetReferenceFileNames(baseline), StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        CollectionAssert.AreEqual(expected, actual);
        CollectionAssert.DoesNotContain(actual, "System.Diagnostics.Process.dll");
        CollectionAssert.DoesNotContain(actual, "System.Xml.dll");
        CollectionAssert.DoesNotContain(actual, "System.Net.Http.dll");
    }

    private static string[] GetReferenceFileNames(Musoq.Converter.Build.BuildItems items)
    {
        return items.RenderingArtifacts.Compilation.References
            .OfType<PortableExecutableReference>()
            .Select(static reference => Path.GetFileName(reference.FilePath))
            .Where(static name => name is not null)
            .Select(static name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static PortableExecutableReference GetReference(
        Musoq.Converter.Build.BuildItems items,
        Assembly assembly)
    {
        return items.RenderingArtifacts.Compilation.References
            .OfType<PortableExecutableReference>()
            .Single(reference => string.Equals(
                Path.GetFileName(reference.FilePath),
                Path.GetFileName(assembly.Location),
                StringComparison.OrdinalIgnoreCase));
    }

    private static CompilationOptions CreateOptions() =>
        new(usePrimitiveTypeValidation: false);

    private static ISchemaProvider CreateSchemaProvider(
        IReadOnlyList<ExternalRow> rows,
        bool useGenericRow = false)
    {
        var schema = new GenericSchema<ExternalLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                {
                    useGenericRow ? "generic" : "rows",
                    useGenericRow
                        ? (new ExternalTable(typeof(GenericExternalRow<ExternalPayload>)), new GenericExternalRowSource(rows))
                        : (new ExternalTable(typeof(ExternalRow)), new ExternalRowSource(rows))
                }
            });

        return new GenericSchemaProvider(new Dictionary<string, ISchema>
        {
            { "#external", schema }
        });
    }

    private static ISchemaProvider CreatePrimitiveSchemaProvider()
    {
        var schema = new GenericSchema<GenericLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                { "values", (new PrimitiveTable(), new EmptyPrimitiveRowSource()) }
            });

        return new GenericSchemaProvider(new Dictionary<string, ISchema>
        {
            { "#primitive", schema }
        });
    }

    private static ExternalRow CreateRow() =>
        new()
        {
            InheritedName = "inherited",
            Payload = new ExternalPayload
            {
                ContractNumber = 42,
                ExternalUri = new Uri("https://example.invalid"),
                Status = ExternalStatus.Ready,
                NullableStatus = ExternalStatus.Ready,
                Nested = new ExternalLeaf { Value = 7, Label = "nested" },
                Leaves = [new ExternalLeaf { Value = 11, Label = "array" }]
            },
            Status = ExternalStatus.Ready,
            Leaves = [new ExternalLeaf { Value = 11, Label = "array" }]
        };

    private sealed class ExternalTable(Type rowType) : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new DataSourceSchemaColumn(nameof(ExternalRow.InheritedName), 0, typeof(string)),
            new DataSourceSchemaColumn(nameof(ExternalRow.Marker), 1, typeof(string)),
            new DataSourceSchemaColumn(nameof(ExternalRow.Payload), 2, typeof(ExternalPayload)),
            new DataSourceSchemaColumn(nameof(ExternalRow.Status), 3, typeof(ExternalStatus?)),
            new DataSourceSchemaColumn(nameof(ExternalRow.Leaves), 4, typeof(ExternalLeaf[])),
            new DataSourceSchemaColumn(nameof(ExternalRow.NestedValues), 5, typeof(List<Dictionary<string, ExternalLeaf>>))
        ];

        public SchemaTableMetadata Metadata { get; } = new(rowType);

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.FirstOrDefault(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            var column = GetColumnByName(name);
            return column is null ? [] : [column];
        }
    }

    private sealed class ExternalRowSource(IReadOnlyList<ExternalRow> rows) : RowSourceBase<ExternalRow>
    {
        protected override void CollectChunks(IChunkWriter<ExternalRow> writer) => writer.Write(rows);
    }

    private sealed class GenericExternalRowSource(IReadOnlyList<ExternalRow> rows)
        : RowSourceBase<GenericExternalRow<ExternalPayload>>
    {
        protected override void CollectChunks(IChunkWriter<GenericExternalRow<ExternalPayload>> writer)
        {
            writer.Write(rows.Select(row => new GenericExternalRow<ExternalPayload>
            {
                InheritedName = row.InheritedName,
                Payload = row.Payload
            }).ToArray());
        }
    }

    private sealed class PrimitiveTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [new DataSourceSchemaColumn("Value", 0, typeof(int))];

        public SchemaTableMetadata Metadata { get; } = new(typeof(int));

        public ISchemaColumn? GetColumnByName(string name) =>
            string.Equals(name, "Value", StringComparison.OrdinalIgnoreCase) ? Columns[0] : null;

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            var column = GetColumnByName(name);
            return column is null ? [] : [column];
        }
    }

    private sealed class EmptyPrimitiveRowSource : RowSourceBase<int>
    {
        protected override void CollectChunks(IChunkWriter<int> writer)
        {
        }
    }

    private sealed class StaticSettingsResolver : ISourceRuntimeSettingsResolver
    {
        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request) =>
            new Dictionary<string, string>();
    }
}
