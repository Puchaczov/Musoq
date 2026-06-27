using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ScriptParameterExecutionTests : EnvironmentVariablesTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenScriptHasNoParameterBlock_ShouldExposeEmptyParameterMetadata()
    {
        const string query = "select Key, Value from #EnvironmentVariables.All() where Key = 'KEY_1'";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        Assert.IsEmpty(vm.ParameterDefinitions);
        Assert.IsEmpty(vm.RequiredParameters);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("KEY_1", table[0][0]);
        Assert.AreEqual("VALUE_1", table[0][1]);
    }

    [TestMethod]
    public void WhenScriptHasEmptyParameterBlock_ShouldExposeEmptyParameterMetadata()
    {
        const string query = "param() select Key, Value from #EnvironmentVariables.All() where Key = 'KEY_1'";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        Assert.IsEmpty(vm.ParameterDefinitions);
        Assert.IsEmpty(vm.RequiredParameters);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("KEY_1", table[0][0]);
        Assert.AreEqual("VALUE_1", table[0][1]);
    }

    [TestMethod]
    public void WhenRequiredScriptParameterIsUsedInWhere_ShouldUseRuntimeValue()
    {
        const string query = "param(key: string) select Key, Value from #EnvironmentVariables.All() where Key = $key";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        Assert.HasCount(1, vm.ParameterDefinitions);
        Assert.HasCount(1, vm.RequiredParameters);
        Assert.AreEqual("key", vm.RequiredParameters[0].Name);
        Assert.AreEqual(typeof(string), vm.ParameterDefinitions[0].ParameterType);

        vm.Parameters["key"] = "KEY_2";
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("KEY_2", table[0][0]);
        Assert.AreEqual("VALUE_2", table[0][1]);
    }

    [TestMethod]
    public void WhenRequiredScriptParameterIsUsedInSelect_ShouldProjectRuntimeValue()
    {
        const string query = "param(value: string) select $value from #EnvironmentVariables.All()";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        vm.Parameters["value"] = "Ada";
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Ada", table[0][0]);
        Assert.AreEqual("Ada", table[1][0]);
    }

    [TestMethod]
    public void WhenScriptParameterNamesDifferOnlyByCase_ShouldBindDistinctValues()
    {
        const string query = "param(key: string, Key: string) select $key, $Key from #EnvironmentVariables.All()";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        vm.Parameters["key"] = "lower";
        vm.Parameters["Key"] = "upper";
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("lower", table[0][0]);
        Assert.AreEqual("upper", table[0][1]);
    }

    [TestMethod]
    public void WhenScriptParameterDefaultExists_ShouldUseDefaultRuntimeValue()
    {
        const string query =
            "param(key: string = 'KEY_1') select Key, Value from #EnvironmentVariables.All() where Key = $key";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        Assert.HasCount(1, vm.ParameterDefinitions);
        Assert.IsTrue(vm.ParameterDefinitions[0].HasDefaultValue);
        Assert.AreEqual("KEY_1", vm.ParameterDefinitions[0].DefaultValue);
        Assert.IsEmpty(vm.RequiredParameters);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("KEY_1", table[0][0]);
        Assert.AreEqual("VALUE_1", table[0][1]);
    }

    [TestMethod]
    public void WhenScriptParameterDefaultIsOverridden_ShouldUseProvidedRuntimeValue()
    {
        const string query =
            "param(key: string = 'KEY_1') select Key, Value from #EnvironmentVariables.All() where Key = $key";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        vm.Parameters["key"] = "KEY_2";
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("KEY_2", table[0][0]);
        Assert.AreEqual("VALUE_2", table[0][1]);
    }

    [TestMethod]
    public void WhenRequiredScriptParameterIsMissing_ShouldThrowBeforeOpeningSource()
    {
        const string query = "param(key: string) select Key, Value from #Parameterized.Items() where Key = $key";
        var provider = new ParameterizedEnvironmentSchemaProvider();
        var vm = CompileWithProvider(query, provider);

        var ex = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
        AssertRuntimeEnvelope(
            ex,
            DiagnosticCode.MQ7003_RequiredScriptParameterMissing,
            "Required script parameter 'key' was not provided.");
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void WhenUnusedRequiredScriptParameterIsMissing_ShouldThrowBeforeOpeningSource()
    {
        const string query = "param(unused: string) select Key, Value from #Parameterized.Items()";
        var provider = new ParameterizedEnvironmentSchemaProvider();
        var vm = CompileWithProvider(query, provider);

        var ex = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
        AssertRuntimeEnvelope(
            ex,
            DiagnosticCode.MQ7003_RequiredScriptParameterMissing,
            "Required script parameter 'unused' was not provided.");
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void WhenRequiredScriptParameterIsProvidedWithWrongCase_ShouldThrowBeforeOpeningSource()
    {
        const string query = "param(key: string) select Key, Value from #Parameterized.Items() where Key = $key";
        var provider = new ParameterizedEnvironmentSchemaProvider();
        var vm = CompileWithProvider(query, provider);
        vm.Parameters["Key"] = "KEY_1";

        var ex = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
        AssertRuntimeEnvelope(
            ex,
            DiagnosticCode.MQ7003_RequiredScriptParameterMissing,
            "Required script parameter 'key' was not provided.");
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void WhenScriptParameterHasWrongClrType_ShouldThrowBeforeOpeningSource()
    {
        const string query = "param(limit: int) select Key, Value from #Parameterized.Items()";
        var provider = new ParameterizedEnvironmentSchemaProvider();
        var vm = CompileWithProvider(query, provider);
        vm.Parameters["limit"] = "100";

        var ex = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
        AssertRuntimeEnvelope(
            ex,
            DiagnosticCode.MQ7004_ScriptParameterTypeMismatch,
            "Script parameter 'limit' expected a value of type 'int' but received 'string'.");
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void WhenValueTypeScriptParameterIsNull_ShouldThrowBeforeOpeningSource()
    {
        const string query = "param(limit: int) select Key, Value from #Parameterized.Items()";
        var provider = new ParameterizedEnvironmentSchemaProvider();
        var vm = CompileWithProvider(query, provider);
        vm.Parameters["limit"] = null;

        var ex = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
        AssertRuntimeEnvelope(
            ex,
            DiagnosticCode.MQ7005_ScriptParameterNullNotAllowed,
            "Script parameter 'limit' expected a non-null value of type 'int'.");
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void WhenOptionalScriptParameterOverrideHasWrongClrType_ShouldThrowEnvelopeBeforeOpeningSource()
    {
        const string query = "param(limit: int = 10) select Key, Value from #Parameterized.Items()";
        var provider = new ParameterizedEnvironmentSchemaProvider();
        var vm = CompileWithProvider(query, provider);
        vm.Parameters["limit"] = "10";

        var ex = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
        AssertRuntimeEnvelope(
            ex,
            DiagnosticCode.MQ7004_ScriptParameterTypeMismatch,
            "Script parameter 'limit' expected a value of type 'int' but received 'string'.");
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void WhenDefaultedScriptParameterIsUsedAsSourceArgument_ShouldCompileAndRun()
    {
        const string query =
            "param(key: string = 'KEY_2') select Key, Value from #Parameterized.Items($key)";
        var vm = CompileWithProvider(query, new ParameterizedEnvironmentSchemaProvider());

        vm.Parameters["key"] = "KEY_1";
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("KEY_1", table[0][0]);
        Assert.AreEqual("VALUE_1", table[0][1]);
    }

    [TestMethod]
    public void WhenCompiledQueryRunsRepeatedly_ShouldRebindCurrentParameterValues()
    {
        const string query = "param(key: string) select Key, Value from #EnvironmentVariables.All() where Key = $key";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        vm.Parameters["key"] = "KEY_1";
        var firstTable = TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken));

        vm.Parameters["key"] = "KEY_2";
        var secondTable = TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken));

        Assert.AreEqual(1, firstTable.Count);
        Assert.AreEqual("KEY_1", firstTable[0][0]);
        Assert.AreEqual("VALUE_1", firstTable[0][1]);
        Assert.AreEqual(1, secondTable.Count);
        Assert.AreEqual("KEY_2", secondTable[0][0]);
        Assert.AreEqual("VALUE_2", secondTable[0][1]);
    }

    [TestMethod]
    public void WhenNullableScriptParameterHasNullDefault_ShouldProjectNull()
    {
        const string query = "param(limit: int? = null) select $limit from #EnvironmentVariables.All()";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        Assert.HasCount(1, vm.ParameterDefinitions);
        Assert.IsTrue(vm.ParameterDefinitions[0].HasDefaultValue);
        Assert.IsNull(vm.ParameterDefinitions[0].DefaultValue);
        Assert.IsEmpty(vm.RequiredParameters);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsNull(table[0][0]);
        Assert.IsNull(table[1][0]);
    }

    [TestMethod]
    public void WhenNullableScriptParameterNullDefaultIsOverridden_ShouldProjectProvidedValue()
    {
        const string query = "param(limit: int? = null) select $limit from #EnvironmentVariables.All()";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());
        vm.Parameters["limit"] = 42;

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(42, table[0][0]);
        Assert.AreEqual(42, table[1][0]);
    }

    [TestMethod]
    public void WhenPrimitiveScriptParameterDefaultsAreDeclared_ShouldProjectTypedValues()
    {
        const string guidText = "2ffcf6fa-3369-4300-946a-bb131a037985";
        const string dateTimeText = "2024-01-02T03:04:05.0000000Z";
        const string dateTimeOffsetText = "2024-01-02T03:04:05+02:00";
        const string timeSpanText = "01:30:00";
        const string query =
            "param(" +
            "intValue: int = 10, " +
            "uintValue: uint = 11ui, " +
            "longValue: long = 12l, " +
            "ulongValue: ulong = 13ul, " +
            "shortValue: short = 14s, " +
            "ushortValue: ushort = 15us, " +
            "sbyteValue: sbyte = 16b, " +
            "byteValue: byte = 17ub, " +
            "floatValue: float = 18.5, " +
            "doubleValue: double = 19.5, " +
            "decimalValue: decimal = 20.5d, " +
            "flag: bool = true, " +
            "code: char = 'x', " +
            "text: string = 'hello', " +
            "id: guid = '2ffcf6fa-3369-4300-946a-bb131a037985', " +
            "created: datetime = '2024-01-02T03:04:05.0000000Z', " +
            "seen: datetimeoffset = '2024-01-02T03:04:05+02:00', " +
            "elapsed: timespan = '01:30:00') " +
            "select $intValue, $uintValue, $longValue, $ulongValue, $shortValue, $ushortValue, " +
            "$sbyteValue, $byteValue, $floatValue, $doubleValue, $decimalValue, $flag, $code, $text, " +
            "$id, $created, $seen, $elapsed from #EnvironmentVariables.All()";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(10, table[0][0]);
        Assert.AreEqual(11u, table[0][1]);
        Assert.AreEqual(12L, table[0][2]);
        Assert.AreEqual(13UL, table[0][3]);
        Assert.AreEqual((short)14, table[0][4]);
        Assert.AreEqual((ushort)15, table[0][5]);
        Assert.AreEqual((sbyte)16, table[0][6]);
        Assert.AreEqual((byte)17, table[0][7]);
        Assert.AreEqual(18.5f, (float)table[0][8], 0.0001f);
        Assert.AreEqual(19.5d, (double)table[0][9], 0.0001d);
        Assert.AreEqual(20.5m, table[0][10]);
        Assert.IsTrue((bool)table[0][11]);
        Assert.AreEqual('x', table[0][12]);
        Assert.AreEqual("hello", table[0][13]);
        Assert.AreEqual(Guid.Parse(guidText), table[0][14]);
        Assert.AreEqual(
            DateTime.Parse(dateTimeText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            table[0][15]);
        Assert.AreEqual(
            DateTimeOffset.Parse(dateTimeOffsetText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            table[0][16]);
        Assert.AreEqual(TimeSpan.Parse(timeSpanText, CultureInfo.InvariantCulture), table[0][17]);
    }

    private CompiledQuery CompileWithProvider(string query, ISchemaProvider provider)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver);
    }

    private static void AssertRuntimeEnvelope(
        QueryExecutionException exception,
        DiagnosticCode expectedCode,
        string expectedMessage)
    {
        Assert.IsNotNull(exception.Envelope);
        Assert.AreEqual(expectedCode, exception.Envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, exception.Envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Runtime, exception.Envelope.Phase);
        Assert.AreEqual(expectedMessage, exception.Envelope.Message);
        Assert.IsNull(exception.Envelope.Snippet);
        Assert.IsNotNull(exception.Envelope.Explanation);
        Assert.IsGreaterThan(0, exception.Envelope.SuggestedFixes.Count);
        Assert.IsNotNull(exception.Envelope.DocsReference);

        var text = exception.FormatText();
        StringAssert.Contains(text, exception.Envelope.CodeString);
        StringAssert.Contains(text, "[runtime]");
        StringAssert.Contains(text, expectedMessage);
        StringAssert.Contains(text, "Try:");

        var json = exception.FormatJson();
        StringAssert.Contains(json, $"\"code\":\"{exception.Envelope.CodeString}\"");
        StringAssert.Contains(json, "\"phase\":\"runtime\"");
        StringAssert.Contains(json, expectedMessage);
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
                ? CreateEnvironmentVariableSources()["*"]
                : CreateEnvironmentVariableSources()["*"].Where(entity => entity.Key == key)).ToArray();

            return EnsureSourceType<T, EnvironmentVariableEntity>(
                name,
                new ParameterizedEnvironmentSource(rows));
        }
    }

    private sealed class ParameterizedEnvironmentSource(IReadOnlyList<EnvironmentVariableEntity> rows)
        : RowSource<EnvironmentVariableEntity>
    {
        public override IEnumerable<IReadOnlyList<EnvironmentVariableEntity>> Chunks => [rows];
    }
}
