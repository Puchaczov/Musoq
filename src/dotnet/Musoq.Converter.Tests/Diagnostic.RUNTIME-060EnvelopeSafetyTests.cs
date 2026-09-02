using System;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class DiagnosticRuntime060EnvelopeSafetyTests
{
    [TestMethod]
    public void CompileWithDiagnostics_ParseAndBindFailures_ShouldProduceStableQueryEnvelopes()
    {
        const string parseQuery = "SELECT FROM #system.dual()";
        var parseResult = InstanceCreator.CompileWithDiagnostics(
            parseQuery,
            "runtime-060-parse",
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        var parseEnvelopes = parseResult.ToEnvelopes();
        Assert.HasCount(1, parseEnvelopes);
        var parseEnvelope = parseEnvelopes[0];
        Assert.AreEqual(DiagnosticCode.MQ2005_InvalidSelectList, parseEnvelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, parseEnvelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, parseEnvelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, parseEnvelope.SourceKind);
        Assert.IsTrue(parseEnvelope.Offset.HasValue);
        Assert.IsTrue(parseEnvelope.EndOffset.HasValue);

        const string bindQuery = "SELECT nonexistent FROM #system.dual()";
        var bindResult = InstanceCreator.CompileWithDiagnostics(
            bindQuery,
            "runtime-060-bind",
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        var bindEnvelopes = bindResult.ToEnvelopes();
        Assert.HasCount(1, bindEnvelopes);
        var bindEnvelope = bindEnvelopes[0];
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, bindEnvelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, bindEnvelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, bindEnvelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, bindEnvelope.SourceKind);
        Assert.IsTrue(bindEnvelope.Offset.HasValue);
        Assert.IsTrue(bindEnvelope.EndOffset.HasValue);
        StringAssert.Contains(bindEnvelope.Snippet!, "nonexistent");
    }

    [TestMethod]
    public void GeneratedSourceDiagnostic_ShouldRemainGeneratedAndKeepItsTargetLocationInEnvelope()
    {
        var context = new DiagnosticContext(new SourceText("select 1", "query.musoq"));
        TargetDiagnosticReporter.Report(
            [
                new TargetDiagnostic(
                    "MT0601",
                    TargetDiagnosticSeverity.Error,
                    "generated compilation failed",
                    new TargetSourceRange(23, 4, 11, 5, 11, 9),
                    "CompiledQuery.g.cs",
                    "generated line")
            ],
            context);

        var diagnostic = context.Diagnostics.Single();
        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, "select 1");

        Assert.AreEqual(DiagnosticCode.MQ8001_CodeGenerationFailed, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.CodeGeneration, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.GeneratedSource, envelope.SourceKind);
        Assert.AreEqual(23, envelope.Offset);
        Assert.AreEqual(27, envelope.EndOffset);
        Assert.AreEqual(4, envelope.Length);
        Assert.AreEqual(11, envelope.Line);
        Assert.AreEqual(5, envelope.Column);
        StringAssert.Contains(envelope.Snippet!, "generated line");

        using var document = JsonDocument.Parse(MusoqErrorEnvelopeFormatter.FormatJson(envelope));
        Assert.AreEqual("MQ8001", document.RootElement.GetProperty("code").GetString());
        Assert.AreEqual("code-generation", document.RootElement.GetProperty("phase").GetString());
        Assert.AreEqual("generated-source", document.RootElement.GetProperty("source").GetString());
        Assert.AreEqual(23, document.RootElement.GetProperty("location").GetProperty("offset").GetInt32());
    }

    [TestMethod]
    public void RuntimeAndInternalExecutionFailures_ShouldExposeSafeStableEnvelopes()
    {
        var parameterFailure = QueryExecutionException.ForScriptParameterBinding(
            ScriptParameterBindingException.MissingRequired("apiKey"));
        var parameterEnvelope = parameterFailure.Envelope!;

        Assert.AreEqual(DiagnosticCode.MQ7003_RequiredScriptParameterMissing, parameterEnvelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, parameterEnvelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Runtime, parameterEnvelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Runtime, parameterEnvelope.SourceKind);
        Assert.IsNull(parameterEnvelope.Offset);
        Assert.IsNull(parameterEnvelope.EndOffset);

        const string secret = "runtime-060-private-detail";
        var internalFailure = QueryExecutionException.ForExecutionFailure(
            "row enumeration",
            new InvalidOperationException(secret));
        var internalEnvelope = internalFailure.Envelope!;
        var safeText = internalFailure.FormatText();
        var safeJson = internalFailure.FormatJson();

        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, internalEnvelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, internalEnvelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Internal, internalEnvelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Internal, internalEnvelope.SourceKind);
        Assert.IsFalse(string.IsNullOrWhiteSpace(internalEnvelope.CorrelationId));
        Assert.IsTrue(internalEnvelope.Arguments.ContainsKey("correlationId"));
        Assert.IsFalse(safeText.Contains(secret, StringComparison.Ordinal));
        Assert.IsFalse(safeJson.Contains(secret, StringComparison.Ordinal));
        Assert.IsTrue(internalFailure.FormatVerboseText().Contains(secret, StringComparison.Ordinal));
    }
}
