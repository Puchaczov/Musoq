using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Musoq.Converter;
using Musoq.Converter.Build;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Schema.Basic;

public class BasicEntityTestBase
{
    protected static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    protected static readonly CompilationOptions ValidationEnabledCompilationOptions =
        new(usePrimitiveTypeValidation: true);

    static BasicEntityTestBase()
    {
        Culture.ApplyWithDefaultCulture();
    }

    protected CancellationTokenSource TokenSource { get; } = new();

    protected ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();

    private readonly object _batchedQueryGate = new();
    private readonly List<CompiledQuery> _batchedQueries = [];

    protected BuildItems CreateBuildItems<T>(string script)
    {
        return InstanceCreator.CreateForAnalyze(
            script,
            Guid.NewGuid().ToString(),
            typeof(T) == typeof(UsedColumnsOrUsedWhereEntity)
                ? new UsedColumnsOrUsedWhereSchemaProvider<UsedColumnsOrUsedWhereEntity>(
                    CreateMockObjectFor<UsedColumnsOrUsedWhereEntity>())
                : new MockBasedSchemaProvider(CreateMockObjectFor<BasicEntity>()),
            LoggerResolver);
    }

    protected CompiledQuery CreateAndRunVirtualMachine<T>(
        string script,
        IDictionary<string, IEnumerable<T>> sources)
        where T : BasicEntity
    {
        var result = StableTypedExecutionCompilationCoordinator.Submit(
            script,
            new BasicSchemaProvider<T>(sources),
            LoggerResolver,
            TestCompilationOptions);

        if (!result.Result.Succeeded)
        {
            throw result.Result.CaughtException != null
                ? new MusoqQueryException(result.Result.ToEnvelopes(), result.Result.CaughtException)
                : new MusoqQueryException(result.Result.ToEnvelopes());
        }

        if (result.WasBatched)
        {
            lock (_batchedQueryGate)
                _batchedQueries.Add(result.Result.CompiledQuery);
        }

        return result.Result.CompiledQuery;
    }

    [TestCleanup]
    public void DisposeBatchedCompiledQueries()
    {
        CompiledQuery[] queries;
        lock (_batchedQueryGate)
        {
            queries = _batchedQueries.ToArray();
            _batchedQueries.Clear();
        }

        foreach (var query in queries)
            query.Dispose();
    }

    protected CompiledQuery CreateAndRunVirtualMachine<T>(
        string script,
        IDictionary<string, IEnumerable<T>> sources,
        CompilationOptions compilationOptions)
        where T : BasicEntity
    {
        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<T>(sources),
            LoggerResolver,
            compilationOptions);
    }

    protected CompiledQuery CreateAndRunVirtualMachine(
        string script,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? SourceRuntimeSettingsBySourceContextId = null,
        ISchemaProvider? schemaProvider = null)
    {
        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            schemaProvider ?? throw new ArgumentNullException(nameof(schemaProvider)),
            LoggerResolver,
            TestCompilationOptions);
    }

    private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CreateMockedEnvironmentVariables()
    {
        var environmentVariablesMock = new Mock<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>>();
        environmentVariablesMock.Setup(f => f[It.IsAny<string>()]).Returns(new Dictionary<string, string>());

        return environmentVariablesMock.Object;
    }

    protected void TestMethodTemplate<TResult>(string operation, TResult score)
    {
        var table = TestResultMethodTemplate(operation);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(typeof(TResult), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(score, table[0][0]);
    }

    protected void TestMethodBatchTemplate(params (string Operation, object Expected)[] cases)
    {
        ArgumentNullException.ThrowIfNull(cases);
        Assert.IsNotEmpty(cases);

        var projection = string.Join(
            ", ",
            cases.Select((item, index) => $"{item.Operation} as Result{index}"));
        var query = $"select {projection} from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var table = TableMaterializationTestHelper.Materialize(
            CreateAndRunVirtualMachine(query, sources).Run());

        Assert.AreEqual(1, table.Count);
        Assert.HasCount(cases.Length, table.Columns);

        for (var index = 0; index < cases.Length; index++)
        {
            Assert.AreEqual(cases[index].Expected.GetType(), table.Columns.ElementAt(index).ColumnType);
            Assert.AreEqual(cases[index].Expected, table[0][index]);
        }
    }

    protected static IDictionary<string, IEnumerable<BasicEntity>> CreateSingleSource(
        params BasicEntity[] entities)
    {
        return new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", entities } };
    }

    protected static void AssertColumn(Table table, int index, string expectedName, Type expectedType)
    {
        var column = table.Columns.ElementAt(index);
        Assert.AreEqual(expectedName, column.ColumnName);
        Assert.AreEqual(expectedType, column.ColumnType);
    }

    protected Table TestResultMethodTemplate(string operation)
    {
        var query = $"select {operation} from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("ABCAACBA")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        return TableMaterializationTestHelper.Materialize(vm.Run());
    }

    private static IDictionary<string, IEnumerable<T>> CreateMockObjectFor<T>()
    {
        var mock = new Mock<IDictionary<string, IEnumerable<T>>>();
        mock.Setup(f => f[It.IsAny<string>()]).Returns([]);

        return mock.Object;
    }

    private sealed class MockBasedSchemaProvider(IDictionary<string, IEnumerable<BasicEntity>> schemas)
        : BasicSchemaProvider<BasicEntity>(schemas)
    {
        public override ISchema GetSchema(string schema)
        {
            return new GenericSchema<BasicEntity, BasicEntityTable>(Values[schema], BasicEntity.TestNameToIndexMap,
                BasicEntity.TestIndexToObjectAccessMap);
        }
    }
}
