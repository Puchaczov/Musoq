using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;


[TestClass]
public class DiagnosticErrorEnvelopeTests
{
    #region MusoqErrorEnvelope Tests

    [TestMethod]
    public void MusoqErrorEnvelope_FromDiagnostic_ShouldPopulateAllFields()
    {
        var location = new SourceLocation(7, 1, 8);
        var endLocation = new SourceLocation(12, 1, 13);
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticSeverity.Error,
            "Unknown column 'Foo'",
            location,
            endLocation);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, "SELECT Foo FROM #test.data() t");

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, envelope.Code);
        Assert.AreEqual("MQ3001", envelope.CodeString);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual("Unknown column 'Foo'", envelope.Message);
        Assert.AreEqual(1, envelope.Line);
        Assert.AreEqual(8, envelope.Column);
        Assert.IsNotNull(envelope.Explanation);
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsNotNull(envelope.DocsReference);
    }

    [TestMethod]
    public void MusoqErrorEnvelope_FromDiagnostic_WhenDiagnosticHasExplanation_ShouldPreferDiagnosticExplanation()
    {
        var location = new SourceLocation(0, 1, 1);
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticSeverity.Error,
            "Unknown column",
            location,
            explanation: "Custom explanation from diagnostic");

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic);

        Assert.AreEqual("Custom explanation from diagnostic", envelope.Explanation);
    }

    [TestMethod]
    public void MusoqErrorEnvelope_FromDiagnostic_WhenNoMetadata_ShouldHaveNullExplanation()
    {
        var location = new SourceLocation(0, 1, 1);
        var diagnostic = new Diagnostic(
            (DiagnosticCode)(-1),
            DiagnosticSeverity.Error,
            "Unknown error",
            location);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic);

        Assert.IsNull(envelope.Explanation);
        Assert.IsNull(envelope.DocsReference);
    }

    [TestMethod]
    public void MusoqErrorEnvelope_FromDiagnostic_WhenDiagnosticHasSuggestedFixes_ShouldUseDiagnosticFixes()
    {
        var location = new SourceLocation(0, 1, 1);
        var action = DiagnosticAction.QuickFix("Use alias.Column", new TextSpan(0, 3), "alias.Column");
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticSeverity.Error,
            "Unknown column",
            location,
            suggestedFixes: [action]);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic);

        Assert.HasCount(1, envelope.SuggestedFixes);
        Assert.AreEqual("Use alias.Column", envelope.SuggestedFixes[0]);
    }

    [TestMethod]
    public void MusoqErrorEnvelope_FromDiagnostic_WhenLocationInvalid_ShouldHaveNullLineAndColumn()
    {
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ9001_InternalCompilerError,
            DiagnosticSeverity.Error,
            "Error",
            SourceLocation.None);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic);

        Assert.IsNull(envelope.Line);
        Assert.IsNull(envelope.Column);
    }

    [TestMethod]
    public void MusoqErrorEnvelope_FromException_ShouldOmitSensitiveDetailsByDefault()
    {
        var inner = new InvalidOperationException("Something broke internally");
        var exception = new Exception("Query failed", inner);

        var envelope = MusoqErrorEnvelope.FromException(exception, "SELECT * FROM #test.data() t");

        Assert.AreEqual(DiagnosticCode.MQ9001_InternalCompilerError, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.IsNull(envelope.Details);

        var verbose = MusoqErrorEnvelope.FromExceptionVerbose(exception, "SELECT * FROM #test.data() t");
        Assert.AreEqual("Something broke internally", verbose.Details);
    }

    [TestMethod]
    public void MusoqErrorEnvelope_FromException_WhenNullMessage_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MusoqErrorEnvelope(
                DiagnosticCode.MQ9001_InternalCompilerError,
                DiagnosticSeverity.Error,
                DiagnosticPhase.Runtime,
                null!,
                null, null, null, null, null,
                Array.Empty<string>(), null, null));
    }

    #endregion

    #region MusoqErrorEnvelopeFormatter Tests

    [TestMethod]
    public void MusoqErrorEnvelopeFormatter_FormatText_ShouldContainCodeSeverityAndPhase()
    {
        var envelope = CreateTestEnvelope(
            DiagnosticCode.MQ3022_MissingAlias,
            DiagnosticSeverity.Error,
            DiagnosticPhase.Bind,
            "Missing alias for data source");

        var text = MusoqErrorEnvelopeFormatter.FormatText(envelope);

        Assert.Contains("MQ3022", text);
        Assert.Contains("[error]", text);
        Assert.Contains("[bind]", text);
        Assert.Contains("Missing alias for data source", text);
    }

    [TestMethod]
    public void MusoqErrorEnvelopeFormatter_FormatText_WhenLineAndColumn_ShouldShowAtLine()
    {
        var envelope = CreateTestEnvelope(
            DiagnosticCode.MQ3022_MissingAlias,
            DiagnosticSeverity.Error,
            DiagnosticPhase.Bind,
            "Missing alias",
            line: 3, column: 10);

        var text = MusoqErrorEnvelopeFormatter.FormatText(envelope);

        Assert.Contains("At: line 3, column 10", text);
    }

    [TestMethod]
    public void MusoqErrorEnvelopeFormatter_FormatText_WhenNoLocation_ShouldShowRuntime()
    {
        var envelope = CreateTestEnvelope(
            DiagnosticCode.MQ7010_DataSourceOpenFailed,
            DiagnosticSeverity.Error,
            DiagnosticPhase.Runtime,
            "Binding failed");

        var text = MusoqErrorEnvelopeFormatter.FormatText(envelope);

        Assert.Contains("At: runtime", text);
    }

    [TestMethod]
    public void MusoqErrorEnvelopeFormatter_FormatText_WhenExplanationPresent_ShouldShowWhy()
    {
        var envelope = CreateTestEnvelope(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticSeverity.Error,
            DiagnosticPhase.Bind,
            "Unknown column",
            explanation: "Column was not found in sources.");

        var text = MusoqErrorEnvelopeFormatter.FormatText(envelope);

        Assert.Contains("Why: Column was not found in sources.", text);
    }

    [TestMethod]
    public void MusoqErrorEnvelopeFormatter_FormatText_WhenSuggestedFixes_ShouldShowTryBlock()
    {
        var envelope = CreateTestEnvelope(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticSeverity.Error,
            DiagnosticPhase.Bind,
            "Unknown column",
            suggestedFixes: ["Check spelling.", "Use alias.ColumnName"]);

        var text = MusoqErrorEnvelopeFormatter.FormatText(envelope);

        Assert.Contains("Try:", text);
        Assert.Contains("1) Check spelling.", text);
        Assert.Contains("2) Use alias.ColumnName", text);
    }

    [TestMethod]
    public void MusoqErrorEnvelopeFormatter_FormatText_WhenDocsPresent_ShouldShowDocs()
    {
        var envelope = CreateTestEnvelope(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticSeverity.Error,
            DiagnosticPhase.Bind,
            "Unknown column",
            docsReference: "Core Spec §Column References");

        var text = MusoqErrorEnvelopeFormatter.FormatText(envelope);

        Assert.Contains("Docs: Core Spec §Column References", text);
    }

    [TestMethod]
    public void MusoqErrorEnvelopeFormatter_FormatJson_ShouldContainAllFields()
    {
        var envelope = CreateTestEnvelope(
            DiagnosticCode.MQ3022_MissingAlias,
            DiagnosticSeverity.Error,
            DiagnosticPhase.Bind,
            "Missing alias",
            line: 2, column: 5, length: 10,
            explanation: "Aliases required",
            suggestedFixes: ["Add alias"],
            docsReference: "Core Spec §Aliasing");

        var json = MusoqErrorEnvelopeFormatter.FormatJson(envelope);

        Assert.Contains("\"code\":\"MQ3022\"", json);
        Assert.Contains("\"severity\":\"error\"", json);
        Assert.Contains("\"phase\":\"bind\"", json);
        Assert.Contains("\"message\":\"Missing alias\"", json);
        Assert.Contains("\"line\":2", json);
        Assert.Contains("\"column\":5", json);
        Assert.Contains("\"length\":10", json);
        Assert.Contains("\"why\":\"Aliases required\"", json);
        Assert.Contains("\"hints\":[\"Add alias\"]", json);
        Assert.Contains("\"docs\":\"Core Spec §Aliasing\"", json);
    }

    [TestMethod]
    public void MusoqErrorEnvelopeFormatter_FormatJson_WhenNoLocation_ShouldOmitLocationBlock()
    {
        var envelope = CreateTestEnvelope(
            DiagnosticCode.MQ9001_InternalCompilerError,
            DiagnosticSeverity.Error,
            DiagnosticPhase.Runtime,
            "Error");

        var json = MusoqErrorEnvelopeFormatter.FormatJson(envelope);

        Assert.DoesNotContain("\"location\"", json);
    }

    [TestMethod]
    public void MusoqErrorEnvelopeFormatter_FormatJson_ShouldEscapeSpecialCharacters()
    {
        var envelope = CreateTestEnvelope(
            DiagnosticCode.MQ9001_InternalCompilerError,
            DiagnosticSeverity.Error,
            DiagnosticPhase.Runtime,
            "Error with \"quotes\" and\nnewline");

        var json = MusoqErrorEnvelopeFormatter.FormatJson(envelope);

        Assert.Contains("\\\"quotes\\\"", json);
        Assert.Contains("\\n", json);
    }

    #endregion

    #region Test Helpers

    private static MusoqErrorEnvelope CreateTestEnvelope(
        DiagnosticCode code,
        DiagnosticSeverity severity,
        DiagnosticPhase phase,
        string message,
        int? line = null,
        int? column = null,
        int? length = null,
        string? snippet = null,
        string? explanation = null,
        string[]? suggestedFixes = null,
        string? docsReference = null,
        string? details = null)
    {
        return new MusoqErrorEnvelope(
            code, severity, phase, message,
            line, column, length, snippet,
            explanation, suggestedFixes ?? Array.Empty<string>(),
            docsReference, details);
    }

    #endregion
}
