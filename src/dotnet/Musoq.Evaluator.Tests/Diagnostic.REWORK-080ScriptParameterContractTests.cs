using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
public sealed class DiagnosticRework080ScriptParameterContractTests : EnvironmentVariablesTestBase
{
    [TestMethod]
    public void DefaultAndOverrideMatrix_ShouldPreserveDeclaredClrTypes()
    {
        const string guidText = "2ffcf6fa-3369-4300-946a-bb131a037985";
        const string dateTimeText = "2024-01-02T03:04:05.0000000Z";
        const string query =
            "param(label: string, enabled: bool = true, maybeCount: int? = null, " +
            $"id: guid = '{guidText}', created: datetime = '{dateTimeText}', delay: timespan = '01:02:03') " +
            "select $label, $enabled, $maybeCount, $id, $created, $delay from #ParameterRows.Items()";
        var provider = new CountingParameterSchemaProvider();
        var vm = CompileWithProvider(query, provider);

        vm.Parameters["label"] = "defaulted";
        var defaults = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, defaults.Count);
        Assert.AreEqual("defaulted", defaults[0][0]);
        Assert.AreEqual(true, defaults[0][1]);
        Assert.IsNull(defaults[0][2]);
        Assert.AreEqual(Guid.Parse(guidText), defaults[0][3]);
        Assert.AreEqual(
            DateTime.Parse(dateTimeText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            defaults[0][4]);
        Assert.AreEqual(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(3), defaults[0][5]);

        var overriddenId = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");
        var overriddenCreated = new DateTime(2025, 6, 7, 8, 9, 10, DateTimeKind.Utc);
        vm.Parameters["enabled"] = false;
        vm.Parameters["maybeCount"] = 42;
        vm.Parameters["id"] = overriddenId;
        vm.Parameters["created"] = overriddenCreated;
        vm.Parameters["delay"] = TimeSpan.FromMinutes(5);

        var overrides = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, overrides.Count);
        Assert.AreEqual("defaulted", overrides[0][0]);
        Assert.AreEqual(false, overrides[0][1]);
        Assert.AreEqual(42, overrides[0][2]);
        Assert.AreEqual(overriddenId, overrides[0][3]);
        Assert.AreEqual(overriddenCreated, overrides[0][4]);
        Assert.AreEqual(TimeSpan.FromMinutes(5), overrides[0][5]);
        Assert.AreEqual(2, provider.OpenCount);
    }

    [TestMethod]
    public void NullableValueAndReferenceParameters_ShouldAcceptNullOverrides()
    {
        const string query =
            "param(label: string = null, maybeCount: int? = 7) " +
            "select $label, $maybeCount from #ParameterRows.Items()";
        var provider = new CountingParameterSchemaProvider();
        var vm = CompileWithProvider(query, provider);

        vm.Parameters["label"] = null;
        vm.Parameters["maybeCount"] = null;

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsNull(table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.AreEqual(1, provider.OpenCount);
    }

    [TestMethod]
    public void SuppliedNumericValueWithDifferentClrType_ShouldNotBeConverted()
    {
        const string query = "param(limit: int = 10) select Key from #ParameterRows.Items()";
        var provider = new CountingParameterSchemaProvider();
        var vm = CompileWithProvider(query, provider);
        vm.Parameters["limit"] = 10L;

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);

        AssertRuntimeEnvelope(
            exception,
            DiagnosticCode.MQ7004_ScriptParameterTypeMismatch,
            "Script parameter 'limit' expected a value of type 'int' but received 'long'.");
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void RequiredParameterFailure_ShouldPrecedeSourceOpeningAndExposeCompleteEnvelope()
    {
        const string query = "param(key: string) select Key from #ParameterRows.Items($key)";
        var provider = new CountingParameterSchemaProvider();
        var vm = CompileWithProvider(query, provider);

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);

        AssertRuntimeEnvelope(
            exception,
            DiagnosticCode.MQ7003_RequiredScriptParameterMissing,
            "Required script parameter 'key' was not provided.");
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void NonNullableParameterNullFailure_ShouldPrecedeSourceOpening()
    {
        const string query = "param(limit: int) select Key from #ParameterRows.Items($limit)";
        var provider = new CountingParameterSchemaProvider();
        var vm = CompileWithProvider(query, provider);
        vm.Parameters["limit"] = null;

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);

        AssertRuntimeEnvelope(
            exception,
            DiagnosticCode.MQ7005_ScriptParameterNullNotAllowed,
            "Script parameter 'limit' expected a non-null value of type 'int'.");
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void CollectionParameterIn_ShouldAcceptListAndPreserveMembershipSemantics()
    {
        const string query =
            "param(keys: string[]) select Key from #ParameterRows.Items() where Key in $keys";
        var provider = new CountingParameterSchemaProvider();
        var vm = CompileWithProvider(query, provider);
        vm.Parameters["keys"] = new List<string> { "KEY_2" };

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("KEY_2", table[0][0]);
        Assert.AreEqual(1, provider.OpenCount);
    }

    [TestMethod]
    public void CollectionParameterNotIn_ShouldAcceptArrayAndReadOnlyListValues()
    {
        const string query =
            "param(keys: string[]) select Key from #ParameterRows.Items() where Key not in $keys";
        var provider = new CountingParameterSchemaProvider();
        var vm = CompileWithProvider(query, provider);
        vm.Parameters["keys"] = new[] { "KEY_1" };

        var arrayTable = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, arrayTable.Count);
        Assert.AreEqual("KEY_2", arrayTable[0][0]);

        vm.Parameters["keys"] = new ReadOnlyCollection<string>(["KEY_2"]);
        var readOnlyTable = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, readOnlyTable.Count);
        Assert.AreEqual("KEY_1", readOnlyTable[0][0]);
        Assert.AreEqual(2, provider.OpenCount);
    }

    [TestMethod]
    public void MissingCollectionParameter_ShouldExposeRequiredEnvelopeBeforeSourceOpening()
    {
        const string query =
            "param(keys: string[]) select Key from #ParameterRows.Items() where Key in $keys";
        var provider = new CountingParameterSchemaProvider();
        var vm = CompileWithProvider(query, provider);

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);

        AssertRuntimeEnvelope(
            exception,
            DiagnosticCode.MQ7003_RequiredScriptParameterMissing,
            "Required script parameter 'keys' was not provided.");
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void WrongCollectionElementType_ShouldExposeTypeEnvelopeBeforeSourceOpening()
    {
        const string query =
            "param(keys: string[]) select Key from #ParameterRows.Items() where Key in $keys";
        var provider = new CountingParameterSchemaProvider();
        var vm = CompileWithProvider(query, provider);
        vm.Parameters["keys"] = new List<int> { 1 };

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);

        AssertRuntimeEnvelope(
            exception,
            DiagnosticCode.MQ7004_ScriptParameterTypeMismatch,
            "Script parameter 'keys' expected a value of type 'IReadOnlyList<string>' but received 'List<int>'.");
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void DirectSourceParameter_ShouldBindCurrentValueAndOpenSourceOnce()
    {
        const string query = "param(key: string) select Key from #ParameterRows.Items($key)";
        var provider = new CountingParameterSchemaProvider();
        var vm = CompileWithProvider(query, provider);
        vm.Parameters["key"] = "KEY_2";

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("KEY_2", table[0][0]);
        Assert.AreEqual("KEY_2", provider.LastKey);
        Assert.AreEqual(1, provider.OpenCount);
    }

    [TestMethod]
    public void NestedSourceParameter_ShouldExposeExactBindEnvelope()
    {
        const string query =
            "param(key: string = 'KEY_1') select Key from #ParameterRows.Items($key + '_2')";
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            new CountingParameterSchemaProvider(),
            LoggerResolver);
        var envelope = result.ToEnvelopes().Single();

        Assert.AreEqual(DiagnosticCode.MQ3062_InvalidScriptParameterSourceArgument, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        var expression = "$key + '_2'";
        Assert.AreEqual(query.IndexOf(expression, StringComparison.Ordinal), envelope.Offset);
        Assert.AreEqual(expression.Length, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.DocsReference));
        StringAssert.Contains(envelope.Message, "must be passed directly");
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

        var text = exception.FormatText();
        StringAssert.Contains(text, envelope.CodeString);
        StringAssert.Contains(text, "[runtime]");
        StringAssert.Contains(text, expectedMessage);
        StringAssert.Contains(text, "Try:");

        var json = exception.FormatJson();
        StringAssert.Contains(json, $"\"code\":\"{envelope.CodeString}\"");
        StringAssert.Contains(json, "\"phase\":\"runtime\"");
        StringAssert.Contains(json, expectedMessage);
    }

    private sealed class CountingParameterSchemaProvider : ISchemaProvider
    {
        private readonly CountingParameterSchema _schema = new();

        public int OpenCount => _schema.OpenCount;

        public string? LastKey => _schema.LastKey;

        public ISchema GetSchema(string schema)
        {
            return _schema;
        }
    }

    private sealed class CountingParameterSchema : SchemaBase
    {
        private static readonly EnvironmentVariableEntity[] Rows =
        [
            new("KEY_1", "VALUE_1"),
            new("KEY_2", "VALUE_2")
        ];

        public int OpenCount { get; private set; }

        public string? LastKey { get; private set; }

        public CountingParameterSchema()
            : base("parameterRows", new MethodsAggregator(new MethodsManager()))
        {
        }

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
                new CountingParameterSource(rows));
        }
    }

    private sealed class CountingParameterSource(IReadOnlyList<EnvironmentVariableEntity> rows)
        : RowSource<EnvironmentVariableEntity>
    {
        public override IEnumerable<IReadOnlyList<EnvironmentVariableEntity>> Chunks => [rows];
    }
}
