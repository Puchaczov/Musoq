using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticRuntime060EnvelopeSafetyTests
{
    [TestMethod]
    public void MusoqErrorEnvelope_DirectConstruction_ShouldInferSourceDomainFromCodeFamily()
    {
        var cases = new[]
        {
            (Code: DiagnosticCode.MQ2001_UnexpectedToken, Phase: DiagnosticPhase.Parse, Source: DiagnosticSourceKind.Query),
            (Code: DiagnosticCode.MQ3001_UnknownColumn, Phase: DiagnosticPhase.Bind, Source: DiagnosticSourceKind.Query),
            (Code: DiagnosticCode.MQ4001_InvalidBinarySchemaField, Phase: DiagnosticPhase.Schema, Source: DiagnosticSourceKind.Schema),
            (Code: DiagnosticCode.MQ7003_RequiredScriptParameterMissing, Phase: DiagnosticPhase.Runtime, Source: DiagnosticSourceKind.Runtime),
            (Code: DiagnosticCode.MQ7011_DataSourceReadFailed, Phase: DiagnosticPhase.DataSource, Source: DiagnosticSourceKind.DataSource),
            (Code: DiagnosticCode.MQ8001_CodeGenerationFailed, Phase: DiagnosticPhase.CodeGeneration, Source: DiagnosticSourceKind.GeneratedSource),
            (Code: DiagnosticCode.MQ9002_InternalExecutionError, Phase: DiagnosticPhase.Internal, Source: DiagnosticSourceKind.Internal)
        };

        foreach (var testCase in cases)
        {
            var envelope = new MusoqErrorEnvelope(
                testCase.Code,
                DiagnosticSeverity.Error,
                testCase.Phase,
                "stable message",
                null,
                null,
                null,
                null,
                null,
                Array.Empty<string>(),
                null,
                null);

            Assert.AreEqual(testCase.Source, envelope.SourceKind, testCase.Code.ToString());
        }
    }

    [TestMethod]
    public void MusoqErrorEnvelope_FromException_ShouldHideSensitiveDetailsUntilVerboseFormatting()
    {
        const string secret = "runtime-envelope-secret-marker";
        var exception = new InvalidOperationException(
            $"outer implementation detail: {secret}",
            new InvalidOperationException($"inner implementation detail: {secret}"));

        var safe = MusoqErrorEnvelope.FromException(exception);
        var safeText = MusoqErrorEnvelopeFormatter.FormatText(safe);
        var safeJson = MusoqErrorEnvelopeFormatter.FormatJson(safe);

        Assert.AreEqual(DiagnosticCode.MQ9001_InternalCompilerError, safe.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, safe.Severity);
        Assert.AreEqual(DiagnosticPhase.Internal, safe.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Internal, safe.SourceKind);
        Assert.IsNull(safe.Details);
        Assert.IsFalse(safe.Message.Contains(secret, StringComparison.Ordinal));
        Assert.IsFalse(safeText.Contains(secret, StringComparison.Ordinal));
        Assert.IsFalse(safeJson.Contains(secret, StringComparison.Ordinal));
        Assert.IsFalse(string.IsNullOrWhiteSpace(safe.CorrelationId));

        var verbose = MusoqErrorEnvelope.FromExceptionVerbose(exception);

        Assert.IsTrue(verbose.Details?.Contains(secret, StringComparison.Ordinal));
    }

    [TestMethod]
    public void InternalExecutionException_ShouldPreserveCorrelationAndSafeEnvelopeTaxonomy()
    {
        const string secret = "internal-execution-secret-marker";
        var original = new InvalidOperationException(secret);
        var exception = InternalDiagnosticException.ForExecution(original);

        var safe = MusoqErrorEnvelope.FromException(exception);
        var json = MusoqErrorEnvelopeFormatter.FormatJson(safe);

        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, safe.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, safe.Severity);
        Assert.AreEqual(DiagnosticPhase.Internal, safe.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Internal, safe.SourceKind);
        Assert.AreEqual(exception.CorrelationId, safe.CorrelationId);
        Assert.AreEqual(exception.CorrelationId, safe.Arguments["correlationId"]);
        Assert.IsNull(safe.Offset);
        Assert.IsNull(safe.EndOffset);
        Assert.IsNull(safe.Details);
        Assert.IsFalse(json.Contains(secret, StringComparison.Ordinal));

        var verbose = MusoqErrorEnvelope.FromExceptionVerbose(exception);

        Assert.IsTrue(verbose.Details?.Contains(secret, StringComparison.Ordinal));
    }

    [TestMethod]
    public void MusoqErrorEnvelopeFormatter_FormatJson_ShouldRoundTripAllControlCharacters()
    {
        const string message = "control\0\b\f\n\r\t\u0001\u001f";
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ9002_InternalExecutionError,
            DiagnosticSeverity.Error,
            message,
            SourceLocation.None,
            SourceLocation.None,
            phase: DiagnosticPhase.Internal,
            sourceKind: DiagnosticSourceKind.Internal,
            arguments: new Dictionary<string, string> { ["fact"] = message });

        var json = MusoqErrorEnvelopeFormatter.FormatJson(MusoqErrorEnvelope.FromDiagnostic(diagnostic));
        using var document = JsonDocument.Parse(json);

        Assert.AreEqual("MQ9002", document.RootElement.GetProperty("code").GetString());
        Assert.AreEqual(message, document.RootElement.GetProperty("message").GetString());
        Assert.AreEqual(message, document.RootElement.GetProperty("arguments").GetProperty("fact").GetString());
    }
}
