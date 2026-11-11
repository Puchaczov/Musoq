using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public class PassPrimitiveTypesTests : BasicEntityTestBase
{

    [TestMethod]
    public void GetSchemaTableAndRowSourcePassedPrimitiveArgumentsTest()
    {
        var query = "select 1 from #test.whatever(1, 2d, true, false, 'text')";
        var tableCalls = 0;
        var sourceCalls = 0;
        using var vm = CreateAndRunVirtualMachine(query, [new TestEntity()], passedParams =>
        {
            tableCalls++;
            AssertSourceArguments(passedParams);
        }, passedParams =>
        {
            sourceCalls++;
            AssertSourceArguments(passedParams);
        });

        using var table = vm.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);
        Assert.IsGreaterThan(0, tableCalls);
        Assert.IsGreaterThan(0, sourceCalls);
    }

    [TestMethod]
    public void CallWithPrimitiveArgumentsTest()
    {
        var query = "select PrimitiveArgumentsMethod(1, 2d, true, false, 'text') from #test.whatever()";

        using var vm = CreateAndRunVirtualMachine(query, [new TestEntity()]);

        using var table = vm.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1]);
    }

    private static void AssertSourceArguments(object?[] arguments)
    {
        Assert.HasCount(5, arguments);
        Assert.AreEqual(1, arguments[0]);
        Assert.AreEqual(2m, arguments[1]);
        Assert.AreEqual(true, arguments[2]);
        Assert.AreEqual(false, arguments[3]);
        Assert.AreEqual("text", arguments[4]);
    }

    private CompiledQuery CreateAndRunVirtualMachine(string script, IReadOnlyList<TestEntity> source,
        Action<object?[]>? onGetTable = null, Action<object?[]>? onGetRowSource = null)
    {
        return InstanceCreator.CompileForExecution(script, Guid.NewGuid().ToString(),
            new TestSchemaProvider(source, onGetTable, onGetRowSource), LoggerResolver);
    }

    private sealed class TestSchemaProvider(
        IReadOnlyList<TestEntity> entities,
        Action<object?[]>? onGetTable,
        Action<object?[]>? onGetRowSource)
        : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new TestSchema(entities, onGetTable, onGetRowSource);
        }
    }

    private sealed class TestSchema(
        IReadOnlyList<TestEntity> entities,
        Action<object?[]>? onGetTable,
        Action<object?[]>? onGetRowSource)
        : SchemaBase("test", CachedLibrary.Value)
    {
        private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);

        public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext communicator, TArguments nativeArguments)
        {
            var parameters = global::Musoq.Tests.Common.NativeSourceTestArguments.Read(nativeArguments);
            onGetRowSource?.Invoke(parameters);
            return EnsureSourceType<T, TestEntity>(name, new EntitySource<TestEntity>([entities], new Dictionary<string, int>(),
                new Dictionary<int, Func<TestEntity, object?>>()));
        }

        public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            onGetTable?.Invoke(parameters);
            return new TestTable();
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();
            var lib = new Library();

            methodManager.RegisterLibraries(lib);

            return new MethodsAggregator(methodManager);
        }

        public override SchemaMethodInfo[] GetConstructors()
        {
            return [];
        }
    }

    private sealed class TestTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => [];

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.Single(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }

        public SchemaTableMetadata Metadata { get; } = new(typeof(TestEntity));
    }

    public sealed class TestEntity;
}
