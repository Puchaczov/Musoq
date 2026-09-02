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
public sealed class DiagnosticRework081ScriptVariableContractTests : EnvironmentVariablesTestBase
{
    [TestMethod]
    public void SupportedVariableTypeMatrix_ShouldEvaluateTypedCompileTimeValues()
    {
        const string guidText = "2ffcf6fa-3369-4300-946a-bb131a037985";
        const string dateTimeText = "2024-01-02T03:04:05.0000000Z";
        const string offsetText = "2024-01-02T03:04:05+02:00";
        const string query =
            "let boolValue: bit = true; " +
            "let byteValue: byte = 1ub; " +
            "let sbyteValue: sbyte = 2b; " +
            "let shortValue: short = 3s; " +
            "let ushortValue: ushort = 4us; " +
            "let intValue: int = 5 + 5; " +
            "let uintValue: uint = 6ui; " +
            "let longValue: long = 7l; " +
            "let ulongValue: ulong = 8ul; " +
            "let floatValue: float = 9.5; " +
            "let doubleValue: double = 10.5; " +
            "let decimalValue: money = 11.5d; " +
            "let charValue: char = 'x'; " +
            "let textValue: string = 'text'; " +
            $"let idValue: guid = '{guidText}'; " +
            $"let dateValue: datetime = '{dateTimeText}'; " +
            $"let offsetValue: datetimeoffset = '{offsetText}'; " +
            "let durationValue: timespan = '01:02:03'; " +
            "let nullableValue: int? = null; " +
            "select $boolValue, $byteValue, $sbyteValue, $shortValue, $ushortValue, " +
            "$intValue, $uintValue, $longValue, $ulongValue, $floatValue, $doubleValue, " +
            "$decimalValue, $charValue, $textValue, $idValue, $dateValue, $offsetValue, " +
            "$durationValue, $nullableValue from #EnvironmentVariables.All()";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(true, table[0][0]);
        Assert.AreEqual((byte)1, table[0][1]);
        Assert.AreEqual((sbyte)2, table[0][2]);
        Assert.AreEqual((short)3, table[0][3]);
        Assert.AreEqual((ushort)4, table[0][4]);
        Assert.AreEqual(10, table[0][5]);
        Assert.AreEqual(6u, table[0][6]);
        Assert.AreEqual(7L, table[0][7]);
        Assert.AreEqual(8UL, table[0][8]);
        Assert.AreEqual(9.5f, (float)table[0][9], 0.0001f);
        Assert.AreEqual(10.5d, (double)table[0][10], 0.0001d);
        Assert.AreEqual(11.5m, table[0][11]);
        Assert.AreEqual('x', table[0][12]);
        Assert.AreEqual("text", table[0][13]);
        Assert.AreEqual(Guid.Parse(guidText), table[0][14]);
        Assert.AreEqual(
            DateTime.Parse(dateTimeText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            table[0][15]);
        Assert.AreEqual(
            DateTimeOffset.Parse(offsetText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            table[0][16]);
        Assert.AreEqual(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(3), table[0][17]);
        Assert.IsNull(table[0][18]);
    }

    [TestMethod]
    public void EarlierVariablesAndConstantOperators_ShouldBeEvaluatedAtCompileTime()
    {
        const string query =
            "let prefix: string = 'VALUE'; " +
            "let label: string = $prefix + '_2'; " +
            "let total: int = (10 + 5) * 2; " +
            "let enabled: bool = $total between 30 and 30; " +
            "let maybe: int? = null; " +
            "let absent: bool = $maybe is null; " +
            "select $label, $total, $enabled, $maybe, $absent from #EnvironmentVariables.All()";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("VALUE_2", table[0][0]);
        Assert.AreEqual(30, table[0][1]);
        Assert.AreEqual(true, table[0][2]);
        Assert.IsNull(table[0][3]);
        Assert.AreEqual(true, table[0][4]);
    }

    [TestMethod]
    public void VariableDeclaredAfterAnEarlierStatement_ShouldBeUsableAfterItsDeclaration()
    {
        const string query =
            "table VariableRowsContract { Value: string }; " +
            "let label: string = 'KEY_2'; " +
            "select $label from #VariableRows.Items()";
        var provider = new VariableSchemaProvider();
        var vm = CompileWithProvider(query, provider);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("KEY_2", table[0][0]);
        Assert.AreEqual(1, provider.OpenCount);
    }

    [TestMethod]
    public void SharedVariableNamespace_ShouldRemainCaseSensitiveAndImmutable()
    {
        const string query =
            "let key: string = 'lower'; " +
            "let Key: string = 'upper'; " +
            "select $key, $Key from #VariableRows.Items()";
        var vm = CompileWithProvider(query, new VariableSchemaProvider());

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("lower", table[0][0]);
        Assert.AreEqual("upper", table[0][1]);
    }

    [TestMethod]
    public void VariableRuntimeOverride_ShouldBeRejectedAsAnUnknownParameter()
    {
        const string query = "let key: string = 'constant'; select $key from #VariableRows.Items()";
        var provider = new VariableSchemaProvider();
        var vm = CompileWithProvider(query, provider);
        vm.Parameters["key"] = "override";

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);

        AssertRuntimeEnvelope(
            exception,
            DiagnosticCode.MQ7006_UnknownScriptParameter,
            "Script parameter 'key' was provided but is not declared.");
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void ComputedVariableSourceArgument_ShouldUseItsCompileTimeValue()
    {
        const string query =
            "let prefix: string = 'KEY'; " +
            "let suffix: string = '_2'; " +
            "select Key from #VariableRows.Items($prefix + $suffix)";
        var provider = new VariableSchemaProvider();
        var vm = CompileWithProvider(query, provider);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("KEY_2", table[0][0]);
        Assert.AreEqual("KEY_2", provider.LastKey);
        Assert.AreEqual(1, provider.OpenCount);
    }

    [TestMethod]
    [DataRow(
        "param(key: string); let key: string = 'local'; select 1 from #EnvironmentVariables.All()",
        "let key: string = 'local'",
        DiagnosticCode.MQ3063_DuplicateScriptSymbolName,
        "declared more than once",
        DisplayName = "parameter and variable collision")]
    [DataRow(
        "let key: string = 'first'; let key: string = 'second'; select 1 from #EnvironmentVariables.All()",
        "let key: string = 'second'",
        DiagnosticCode.MQ3063_DuplicateScriptSymbolName,
        "declared more than once",
        DisplayName = "duplicate variable")]
    [DataRow(
        "let item: object = 'value'; select 1 from #EnvironmentVariables.All()",
        "let item: object = 'value'",
        DiagnosticCode.MQ3064_UnsupportedScriptVariableType,
        "is not supported",
        DisplayName = "unsupported variable type")]
    [DataRow(
        "let limit: int = 'many'; select 1 from #EnvironmentVariables.All()",
        "let limit: int = 'many'",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "cannot be converted",
        DisplayName = "invalid variable initializer")]
    public void VariableDeclarationDiagnostics_ShouldExposeExactBindEnvelopes(
        string query,
        string declaration,
        DiagnosticCode expectedCode,
        string expectedMessage)
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            new EnvironmentVariablesSchemaProvider(),
            LoggerResolver);
        var envelope = result.ToEnvelopes().Single();

        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(query.IndexOf(declaration, StringComparison.Ordinal), envelope.Offset);
        Assert.AreEqual(declaration.Length, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.DocsReference));
        StringAssert.Contains(envelope.Message, expectedMessage);
    }

    [TestMethod]
    [DataRow(
        "param(root: string = 'KEY'); let key: string = $root + '_1'; select 1 from #EnvironmentVariables.All()",
        "let key: string = $root + '_1'",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "runtime parameter",
        DisplayName = "runtime parameter initializer")]
    [DataRow(
        "let key: string = ToUpper('key_1'); select 1 from #EnvironmentVariables.All()",
        "let key: string = ToUpper('key_1')",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "only literals",
        DisplayName = "function initializer")]
    [DataRow(
        "let key: string = Key; select 1 from #EnvironmentVariables.All()",
        "let key: string = Key",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "only literals",
        DisplayName = "source column initializer")]
    public void RuntimeDependentInitializer_ShouldExposeExactBindEnvelope(
        string query,
        string declaration,
        DiagnosticCode expectedCode,
        string expectedMessage)
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            new EnvironmentVariablesSchemaProvider(),
            LoggerResolver);
        var envelope = result.ToEnvelopes().Single();

        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(query.IndexOf(declaration, StringComparison.Ordinal), envelope.Offset);
        Assert.AreEqual(declaration.Length, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.DocsReference));
        StringAssert.Contains(envelope.Message, expectedMessage);
    }

    [TestMethod]
    public void ForwardVariableReference_ShouldExposeUsedBeforeDeclarationEnvelope()
    {
        const string query =
            "let copy: int = $later; let later: int = 1; select 1 from #EnvironmentVariables.All()";
        const string declaration = "let copy: int = $later";
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            new EnvironmentVariablesSchemaProvider(),
            LoggerResolver);
        var envelope = result.ToEnvelopes().Single();

        Assert.AreEqual(DiagnosticCode.MQ3066_ScriptVariableUsedBeforeDeclaration, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(query.IndexOf(declaration, StringComparison.Ordinal), envelope.Offset);
        Assert.AreEqual(declaration.Length, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.DocsReference));
        StringAssert.Contains(envelope.Message, "before it is declared");
    }

    [TestMethod]
    public void ParameterAndVariableNameCollision_ShouldExposeDuplicateSymbolEnvelope()
    {
        const string query =
            "param(key: string); let key: string = 'local'; select $key from #EnvironmentVariables.All()";
        const string declaration = "let key: string = 'local'";
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            new EnvironmentVariablesSchemaProvider(),
            LoggerResolver);
        var envelope = result.ToEnvelopes().Single();

        Assert.AreEqual(DiagnosticCode.MQ3063_DuplicateScriptSymbolName, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(query.IndexOf(declaration, StringComparison.Ordinal), envelope.Offset);
        Assert.AreEqual(declaration.Length, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.DocsReference));
        StringAssert.Contains(envelope.Message, "declared more than once");
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
        var envelope = exception.Envelope;
        Assert.IsNotNull(envelope);
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Runtime, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Runtime, envelope.SourceKind);
        Assert.AreEqual(expectedMessage, envelope.Message);
        Assert.IsNull(envelope.Line);
        Assert.IsNull(envelope.Column);
        Assert.IsNull(envelope.Offset);
        Assert.IsNull(envelope.EndOffset);
        Assert.IsNull(envelope.Length);
        Assert.IsNull(envelope.Snippet);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.DocsReference));
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

    private sealed class VariableSchemaProvider : ISchemaProvider
    {
        private readonly VariableSchema _schema = new();

        public int OpenCount => _schema.OpenCount;

        public string? LastKey => _schema.LastKey;

        public ISchema GetSchema(string schema)
        {
            return _schema;
        }
    }

    private sealed class VariableSchema : SchemaBase
    {
        private static readonly EnvironmentVariableEntity[] Rows =
        [
            new("KEY_1", "VALUE_1"),
            new("KEY_2", "VALUE_2")
        ];

        public VariableSchema()
            : base("variableRows", new MethodsAggregator(new MethodsManager()))
        {
        }

        public int OpenCount { get; private set; }

        public string? LastKey { get; private set; }

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
            OpenCount++;
            LastKey = parameters.Length == 0 ? null : parameters[0] as string;
            var rows = LastKey == null
                ? Rows
                : Rows.Where(row => row.Key == LastKey).ToArray();

            return EnsureSourceType<T, EnvironmentVariableEntity>(
                name,
                new VariableSource(rows));
        }
    }

    private sealed class VariableSource(IReadOnlyList<EnvironmentVariableEntity> rows)
        : RowSource<EnvironmentVariableEntity>
    {
        public override IEnumerable<IReadOnlyList<EnvironmentVariableEntity>> Chunks => [rows];
    }
}
