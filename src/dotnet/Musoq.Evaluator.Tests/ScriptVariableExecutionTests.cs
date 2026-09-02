using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class ScriptVariableExecutionTests : EnvironmentVariablesTestBase
{

    [TestMethod]
    public void WhenScriptVariableIsUsedInWhere_ShouldUseCompileTimeValue()
    {
        const string query =
            "let key: string = 'KEY_2'; select Key, $key from #EnvironmentVariables.All() where Key = $key";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        Assert.IsEmpty(vm.ParameterDefinitions);
        Assert.IsEmpty(vm.RequiredParameters);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("KEY_2", table[0][0]);
        Assert.AreEqual("KEY_2", table[0][1]);
    }

    [TestMethod]
    public void WhenScriptVariableUsesEarlierVariables_ShouldProjectComputedValues()
    {
        const string query =
            "let prefix: string = 'VALUE'; " +
            "let value: string = $prefix + '_1'; " +
            "let total: int = 10 + 5; " +
            "select $value, $total from #EnvironmentVariables.All()";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("VALUE_1", table[0][0]);
        Assert.AreEqual(15, table[0][1]);
    }

    [TestMethod]
    public void WhenScriptVariableUsesNonConstPrimitiveValue_ShouldProjectTypedValue()
    {
        const string guidText = "2ffcf6fa-3369-4300-946a-bb131a037985";
        const string dateTimeText = "2024-01-02T03:04:05.0000000Z";
        const string timeSpanText = "01:30:00";
        const string query =
            "let id: guid = '2ffcf6fa-3369-4300-946a-bb131a037985'; " +
            "let created: datetime = '2024-01-02T03:04:05.0000000Z'; " +
            "let elapsed: timespan = '01:30:00'; " +
            "select $id, $created, $elapsed from #EnvironmentVariables.All()";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(Guid.Parse(guidText), table[0][0]);
        Assert.AreEqual(
            DateTime.Parse(dateTimeText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            table[0][1]);
        Assert.AreEqual(TimeSpan.Parse(timeSpanText, CultureInfo.InvariantCulture), table[0][2]);
    }

    [TestMethod]
    public void WhenScriptVariableExpressionIsUsedAsSourceArgument_ShouldOpenSourceWithComputedValue()
    {
        const string query =
            "let prefix: string = 'KEY'; " +
            "let suffix: string = '_1'; " +
            "select Key, Value from #Parameterized.Items($prefix + $suffix)";
        var provider = new ParameterizedEnvironmentSchemaProvider();
        var vm = CompileWithProvider(query, provider);

        var table = TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken));

        Assert.AreEqual(1, provider.OpenCount);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("KEY_1", table[0][0]);
        Assert.AreEqual("VALUE_1", table[0][1]);
    }

    private CompiledQuery CompileWithProvider(string query, ISchemaProvider provider)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver);
    }

    private static Dictionary<string, IEnumerable<EnvironmentVariableEntity>> CreateEnvironmentVariableSources()
    {
        return new Dictionary<string, IEnumerable<EnvironmentVariableEntity>>
        {
            {
                "*",
                [
                    new EnvironmentVariableEntity("KEY_1", "VALUE_1"),
                    new EnvironmentVariableEntity("KEY_2", "VALUE_2")
                ]
            }
        };
    }

    private sealed class ParameterizedEnvironmentSchemaProvider : ISchemaProvider
    {
        public int OpenCount { get; private set; }

        public ISchema GetSchema(string schema)
        {
            return new ParameterizedEnvironmentSchema(() => OpenCount++);
        }
    }

    private sealed class ParameterizedEnvironmentSchema(Action onOpen)
        : SchemaBase("parameterized", new MethodsAggregator(new MethodsManager()))
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            return new EnvironmentVariableEntityTable();
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            onOpen();
            var key = parameters.Length > 0 ? parameters[0] as string : null;
            var rows = (key == null
                ? CreateRows()
                : CreateRows().Where(entity => entity.Key == key)).ToArray();

            return EnsureSourceType<T, EnvironmentVariableEntity>(
                name,
                new ParameterizedEnvironmentSource(rows));
        }

        private static IEnumerable<EnvironmentVariableEntity> CreateRows()
        {
            return
            [
                new EnvironmentVariableEntity("KEY_1", "VALUE_1"),
                new EnvironmentVariableEntity("KEY_2", "VALUE_2")
            ];
        }
    }

    private sealed class ParameterizedEnvironmentSource(IReadOnlyList<EnvironmentVariableEntity> rows)
        : RowSource<EnvironmentVariableEntity>
    {
        public override IEnumerable<IReadOnlyList<EnvironmentVariableEntity>> Chunks => [rows];
    }
}
