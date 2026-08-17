using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class StructuredDiagnosticPayloadTests
{
    [TestMethod]
    public void Diagnostic_PreservesStructuredFactsAcrossCopies()
    {
        var related = new DiagnosticRelatedLocation(
            new SourceLocation(12, 2, 3),
            new SourceLocation(15, 2, 6),
            "Declaration is here",
            DiagnosticSourceKind.Schema);
        var action = DiagnosticAction.QuickFix("Use the declared name", new TextSpan(0, 3), "Name");
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ9001_InternalCompilerError,
            DiagnosticSeverity.Error,
            "Invariant failed",
            new SourceLocation(0, 1, 1),
            new SourceLocation(3, 1, 4),
            phase: DiagnosticPhase.Internal,
            sourceKind: DiagnosticSourceKind.GeneratedSource,
            arguments: new Dictionary<string, string>
            {
                ["symbol"] = "Name",
                ["actualTypes"] = "Int32"
            },
            relatedLocations: [related],
            correlationId: "corr-1")
            .WithSuggestedFix(action);

        var copy = diagnostic.WithExplanation("The generated expression could not be lowered.");

        Assert.AreEqual(DiagnosticPhase.Internal, copy.Phase);
        Assert.AreEqual(DiagnosticSourceKind.GeneratedSource, copy.SourceKind);
        Assert.AreEqual("Name", copy.Arguments["symbol"]);
        Assert.AreEqual("Int32", copy.Arguments["actualTypes"]);
        Assert.AreEqual("Declaration is here", copy.RelatedLocations[0].Message);
        Assert.AreEqual(DiagnosticSourceKind.Schema, copy.RelatedLocations[0].SourceKind);
        Assert.AreEqual("corr-1", copy.CorrelationId);
        Assert.AreEqual("Use the declared name", copy.SuggestedFixes[0].Title);
        Assert.IsNotNull(copy.SuggestedFixes[0].TextEdit);
    }

    [TestMethod]
    public void Envelope_PreservesKnownOffsetZeroInsertionAndEndLocation()
    {
        var sourceText = new SourceText("select");
        var (start, end) = sourceText.GetLocations(new TextSpan(0, 0));
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ2002_MissingToken,
            DiagnosticSeverity.Error,
            "Another statement starts here.",
            start,
            end,
            phase: DiagnosticPhase.Parse);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, sourceText.Text);

        Assert.AreEqual(0, envelope.Offset);
        Assert.AreEqual(0, envelope.EndOffset);
        Assert.AreEqual(0, envelope.Length);
        Assert.AreEqual(1, envelope.Line);
        Assert.AreEqual(1, envelope.Column);
        Assert.AreEqual(1, envelope.EndLine);
        Assert.AreEqual(1, envelope.EndColumn);
    }

    [TestMethod]
    public void Formatter_EmitsStructuredFieldsAndTextEdits()
    {
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ8001_CodeGenerationFailed,
            DiagnosticSeverity.Error,
            "Generated invariant failed",
            new SourceLocation(2, 1, 3),
            new SourceLocation(2, 1, 3),
            phase: DiagnosticPhase.CodeGeneration,
            sourceKind: DiagnosticSourceKind.GeneratedSource,
            arguments: new Dictionary<string, string> { ["symbol"] = "x\"y" },
            relatedLocations:
            [
                new DiagnosticRelatedLocation(
                    new SourceLocation(10, 2, 1),
                    message: "SQL origin")
            ],
            correlationId: "corr-2")
            .WithSuggestedFix(DiagnosticAction.QuickFix("Replace", new TextSpan(2, 0), "x"));

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic);
        var json = MusoqErrorEnvelopeFormatter.FormatJson(envelope);
        var text = MusoqErrorEnvelopeFormatter.FormatText(envelope);

        StringAssert.Contains(json, "\"source\":\"generated-source\"");
        StringAssert.Contains(json, "\"offset\":2");
        StringAssert.Contains(json, "\"length\":0");
        StringAssert.Contains(json, "\"arguments\":{\"symbol\":\"x\\\"y\"}");
        StringAssert.Contains(json, "\"actions\"");
        StringAssert.Contains(json, "\"newText\":\"x\"");
        StringAssert.Contains(json, "\"correlationId\":\"corr-2\"");
        StringAssert.Contains(text, "Source: generated-source");
        StringAssert.Contains(text, "Actions:");
    }

    [TestMethod]
    public void ExceptionEnvelope_IsSafeByDefaultAndVerboseWhenRequested()
    {
        const string secret = "secret-marker-structured-diagnostics";
        var exception = new Exception("outer", new InvalidOperationException(secret));

        var safe = MusoqErrorEnvelope.FromException(exception);
        var verbose = MusoqErrorEnvelope.FromExceptionVerbose(exception);

        Assert.IsNull(safe.Details);
        StringAssert.Contains(verbose.Details!, secret);
    }
}
