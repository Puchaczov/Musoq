using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticUx064DiagnosticContractTests
{
    [TestMethod]
    public void Envelope_WithKnownSourceLocation_ShouldKeepPreciseSpanAndSnippet()
    {
        const string query = "select 1\r\nfrom #test.people()";
        var sourceText = new SourceText(query, "query.musoq");
        var start = query.IndexOf("from", StringComparison.Ordinal);
        var span = new TextSpan(start, "from".Length);
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticSeverity.Error,
            "Unexpected FROM",
            sourceText.GetLocation(start),
            sourceText.GetLocation(span.End),
            explanation: "The FROM clause is not valid at this position.",
            suggestedFixes: [DiagnosticAction.Suggestion("Check the statement order.")],
            docsReference: "Core Spec - Statement Structure");

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);

        Assert.AreEqual(start, envelope.Offset);
        Assert.AreEqual(span.End, envelope.EndOffset);
        Assert.AreEqual(span.Length, envelope.Length);
        Assert.AreEqual(2, envelope.Line);
        Assert.AreEqual(1, envelope.Column);
        StringAssert.Contains(envelope.Snippet!, "from #test.people()");
        Assert.AreEqual("The FROM clause is not valid at this position.", envelope.Explanation);
        Assert.AreEqual("Core Spec - Statement Structure", envelope.DocsReference);
        Assert.HasCount(1, envelope.Actions);
    }

    [TestMethod]
    public void Envelope_WithKnownZeroLengthSpan_ShouldRepresentInsertionPoint()
    {
        const string query = "select\nfrom #test.people()";
        var sourceText = new SourceText(query);
        var insertion = new TextSpan(query.IndexOf("from", StringComparison.Ordinal), 0);
        var locations = sourceText.GetLocations(insertion);
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ2002_MissingToken,
            DiagnosticSeverity.Error,
            "Missing expression",
            locations.Start,
            locations.End);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);

        Assert.AreEqual(insertion.Start, envelope.Offset);
        Assert.AreEqual(insertion.Start, envelope.EndOffset);
        Assert.AreEqual(0, envelope.Length);
        Assert.AreEqual(2, envelope.Line);
        Assert.AreEqual(1, envelope.Column);
        StringAssert.Contains(envelope.Snippet!, "from #test.people()");
    }

    [TestMethod]
    public void Envelope_WithUnknownLocation_ShouldNotInventQuerySnippet()
    {
        var diagnostic = Diagnostic.ErrorUnknownLocation(
            DiagnosticCode.MQ9001_InternalCompilerError,
            "The compiler failed.");

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, "select 1");

        Assert.IsNull(envelope.Line);
        Assert.IsNull(envelope.Column);
        Assert.IsNull(envelope.Offset);
        Assert.IsNull(envelope.EndOffset);
        Assert.IsNull(envelope.Snippet);
    }

    [TestMethod]
    public void Envelope_WithOnlyStartLocation_ShouldUsePointForSnippetWithoutNegativeLength()
    {
        const string query = "select\nfrom #test.people()";
        var start = query.IndexOf("from", StringComparison.Ordinal);
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticSeverity.Error,
            "Unexpected token",
            new SourceLocation(start, 2, 1),
            SourceLocation.None);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);

        Assert.AreEqual(start, envelope.Offset);
        Assert.IsNull(envelope.EndOffset);
        Assert.IsNull(envelope.Length);
        StringAssert.Contains(envelope.Snippet!, "from #test.people()");
    }

    [TestMethod]
    public void EnvelopeFormatting_ShouldBeDeterministicAndPreserveActions()
    {
        var location = new SourceLocation(2, 1, 3);
        var action = DiagnosticAction.QuickFix("Replace token", new TextSpan(2, 3), "new");
        var first = new Diagnostic(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticSeverity.Error,
            "Unknown column",
            location,
            new SourceLocation(5, 1, 6),
            suggestedFixes: [action],
            explanation: "The column was not found.",
            docsReference: "Core Spec - Column References",
            arguments: new Dictionary<string, string>
            {
                ["zeta"] = "last",
                ["alpha"] = "first"
            });
        var second = new Diagnostic(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticSeverity.Error,
            "Unknown column",
            location,
            new SourceLocation(5, 1, 6),
            suggestedFixes: [action],
            explanation: "The column was not found.",
            docsReference: "Core Spec - Column References",
            arguments: new Dictionary<string, string>
            {
                ["alpha"] = "first",
                ["zeta"] = "last"
            });

        var firstEnvelope = MusoqErrorEnvelope.FromDiagnostic(first);
        var secondEnvelope = MusoqErrorEnvelope.FromDiagnostic(second);

        Assert.AreEqual(
            MusoqErrorEnvelopeFormatter.FormatText(firstEnvelope),
            MusoqErrorEnvelopeFormatter.FormatText(secondEnvelope));
        var firstJson = MusoqErrorEnvelopeFormatter.FormatJson(firstEnvelope);
        var secondJson = MusoqErrorEnvelopeFormatter.FormatJson(secondEnvelope);
        Assert.AreEqual(firstJson, secondJson);
        using var document = JsonDocument.Parse(firstJson);
        var edit = document.RootElement.GetProperty("actions")[0].GetProperty("edit");
        Assert.AreEqual(2, edit.GetProperty("start").GetInt32());
        Assert.AreEqual(3, edit.GetProperty("length").GetInt32());
        Assert.AreEqual("new", edit.GetProperty("newText").GetString());
    }

    [TestMethod]
    public void DiagnosticFormatter_UnknownLocationAndControlCharacters_ShouldRemainSafeJson()
    {
        var diagnostic = Diagnostic.ErrorUnknownLocation(
            DiagnosticCode.MQ9002_InternalExecutionError,
            "failure\0\b\f\u0001");
        var formatter = new DiagnosticFormatter();

        var json = formatter.FormatAsJson(diagnostic);

        using var document = JsonDocument.Parse(json);
        Assert.AreEqual(JsonValueKind.Null, document.RootElement.GetProperty("range").ValueKind);
        Assert.AreEqual("failure\0\b\f\u0001", document.RootElement.GetProperty("message").GetString());
        StringAssert.Contains(formatter.Format(diagnostic), "(unknown):");
    }
}
