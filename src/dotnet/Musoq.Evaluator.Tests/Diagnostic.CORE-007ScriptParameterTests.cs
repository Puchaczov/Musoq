using System;
using System.Collections.Generic;
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
public sealed class DiagnosticCore007ScriptParameterTests : EnvironmentVariablesTestBase
{
    [TestMethod]
    public void WhenRequiredCollectionParameterIsMissing_ShouldFailBeforeOpeningSource()
    {
        const string query =
            "param(keys: string[]) select Key from #Parameterized.Items() where Key in $keys";
        var provider = new CountingSchemaProvider();
        var vm = CompileWithProvider(query, provider);

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);

        AssertRuntimeEnvelope(
            exception,
            DiagnosticCode.MQ7003_RequiredScriptParameterMissing,
            "Required script parameter 'keys' was not provided.");
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void WhenCollectionParameterIsNull_ShouldFailBeforeOpeningSource()
    {
        const string query =
            "param(keys: string[]) select Key from #Parameterized.Items() where Key not in $keys";
        var provider = new CountingSchemaProvider();
        var vm = CompileWithProvider(query, provider);
        vm.Parameters["keys"] = null;

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);

        AssertRuntimeEnvelope(
            exception,
            DiagnosticCode.MQ7005_ScriptParameterNullNotAllowed,
            "Script parameter 'keys' expected a non-null value of type 'IReadOnlyList<string>'.");
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void WhenCollectionParameterHasWrongElementType_ShouldFailBeforeOpeningSource()
    {
        const string query =
            "param(keys: string[]) select Key from #Parameterized.Items() where Key in $keys";
        var provider = new CountingSchemaProvider();
        var vm = CompileWithProvider(query, provider);
        vm.Parameters["keys"] = new[] { 1, 2 };

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);

        AssertRuntimeEnvelope(
            exception,
            DiagnosticCode.MQ7004_ScriptParameterTypeMismatch,
            "Script parameter 'keys' expected a value of type 'IReadOnlyList<string>' but received 'int[]'.");
        Assert.AreEqual(0, provider.OpenCount);
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
        Assert.AreEqual(expectedMessage, envelope.Message);
        Assert.IsNull(envelope.Snippet);
        Assert.IsNotNull(envelope.Explanation);
        Assert.IsGreaterThan(0, envelope.SuggestedFixes.Count);
        Assert.IsNotNull(envelope.DocsReference);
    }

    private sealed class CountingSchemaProvider : ISchemaProvider
    {
        public int OpenCount { get; private set; }

        public ISchema GetSchema(string schema)
        {
            return new CountingSchema(() => OpenCount++);
        }
    }

    private sealed class CountingSchema(Action onOpen)
        : SchemaBase("counting", new MethodsAggregator(new MethodsManager()))
    {
        private static readonly EnvironmentVariableEntity[] Rows =
        [
            new("KEY_1", "VALUE_1"),
            new("KEY_2", "VALUE_2")
        ];

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
            return EnsureSourceType<T, EnvironmentVariableEntity>(
                name,
                new CountingSource(Rows));
        }

        private sealed class CountingSource(IReadOnlyList<EnvironmentVariableEntity> rows)
            : RowSource<EnvironmentVariableEntity>
        {
            public override IEnumerable<IReadOnlyList<EnvironmentVariableEntity>> Chunks => [rows];
        }
    }
}
