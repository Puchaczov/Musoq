using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

/// <summary>
///     Branch coverage tests for ParseResult, ParseException, and DiagnosticFormatter.
/// </summary>
[TestClass]
public class ParseResultFormattingTests
{
    private static RootNode ParseQuery(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        return parser.ComposeAll();
    }

    #region ParseResult Branch Coverage

    [TestMethod]
    public void ParseResult_WithNoErrors_ShouldBeSuccessful()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var root = ParseQuery(sourceText.Text);
        var parseResult = new ParseResult(root, sourceText);

        Assert.IsTrue(parseResult.Success);
        Assert.IsFalse(parseResult.HasErrors);
        Assert.IsFalse(parseResult.HasWarnings);
        Assert.IsTrue(parseResult.HasAst);
        Assert.AreEqual(0, parseResult.ErrorCount);
        Assert.AreEqual(0, parseResult.WarningCount);
    }

    [TestMethod]
    public void ParseResult_WithErrors_ShouldNotBeSuccessful()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var root = ParseQuery(sourceText.Text);
        var errors = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "test error", new TextSpan(0, 5))
        };
        var parseResult = new ParseResult(root, sourceText, errors);

        Assert.IsFalse(parseResult.Success);
        Assert.IsTrue(parseResult.HasErrors);
        Assert.AreEqual(1, parseResult.ErrorCount);
    }

    [TestMethod]
    public void ParseResult_WithWarnings_ShouldHaveWarnings()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var root = ParseQuery(sourceText.Text);
        var warnings = new[]
        {
            Diagnostic.Warning(DiagnosticCode.MQ5001_UnusedAlias, "test warning", new TextSpan(0, 5))
        };
        var parseResult = new ParseResult(root, sourceText, warnings);

        Assert.IsTrue(parseResult.Success);
        Assert.IsTrue(parseResult.HasWarnings);
        Assert.AreEqual(1, parseResult.WarningCount);
        Assert.AreEqual(0, parseResult.ErrorCount);
    }

    [TestMethod]
    public void ParseResult_Errors_ShouldFilterOnlyErrors()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var root = ParseQuery(sourceText.Text);
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 5)),
            Diagnostic.Warning(DiagnosticCode.MQ5001_UnusedAlias, "warning", new TextSpan(5, 3))
        };
        var parseResult = new ParseResult(root, sourceText, diagnostics);

        var errors = parseResult.Errors.ToArray();

        Assert.HasCount(1, errors);
        Assert.AreEqual(DiagnosticSeverity.Error, errors[0].Severity);
    }

    [TestMethod]
    public void ParseResult_Warnings_ShouldFilterOnlyWarnings()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var root = ParseQuery(sourceText.Text);
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error", new TextSpan(0, 5)),
            Diagnostic.Warning(DiagnosticCode.MQ5001_UnusedAlias, "warning", new TextSpan(5, 3))
        };
        var parseResult = new ParseResult(root, sourceText, diagnostics);

        var warnings = parseResult.Warnings.ToArray();

        Assert.HasCount(1, warnings);
        Assert.AreEqual(DiagnosticSeverity.Warning, warnings[0].Severity);
    }

    [TestMethod]
    public void ParseResult_Failed_ShouldHaveNoAst()
    {
        var sourceText = new SourceText("invalid");
        var errors = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "parse failed", new TextSpan(0, 7))
        };

        var result = ParseResult.Failed(sourceText, errors);

        Assert.IsFalse(result.HasAst);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.HasErrors);
    }

    [TestMethod]
    public void ParseResult_GetSortedDiagnostics_ShouldSortByOffset()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "second", new TextSpan(10, 5)),
            Diagnostic.Error(DiagnosticCode.MQ2002_MissingToken, "first", new TextSpan(0, 5))
        };
        var parseResult = new ParseResult(null, sourceText, diagnostics);

        var sorted = parseResult.GetSortedDiagnostics().ToArray();

        Assert.AreEqual("first", sorted[0].Message);
        Assert.AreEqual("second", sorted[1].Message);
    }

    [TestMethod]
    public void ParseResult_GetDiagnosticsAt_ShouldFindDiagnosticsAtPosition()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "at position", new TextSpan(5, 3))
        };
        var parseResult = new ParseResult(null, sourceText, diagnostics);

        var found = parseResult.GetDiagnosticsAt(6).ToArray();

        Assert.HasCount(1, found);
    }

    [TestMethod]
    public void ParseResult_GetDiagnosticsAt_WithTolerance_ShouldFindNearbyDiagnostics()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "at position", new TextSpan(5, 3))
        };
        var parseResult = new ParseResult(null, sourceText, diagnostics);

        var found = parseResult.GetDiagnosticsAt(10, tolerance: 3).ToArray();

        Assert.HasCount(1, found);
    }

    [TestMethod]
    public void ParseResult_GetDiagnosticsOnLine_ShouldFindDiagnosticsOnSpecificLine()
    {
        var sourceText = new SourceText("SELECT\nFROM table", "test.sql");
        var diagnostics = new[]
        {
            Diagnostic.Error(
                DiagnosticCode.MQ2001_UnexpectedToken,
                "error on line 2",
                new SourceLocation(7, 2, 1),
                new SourceLocation(11, 2, 5))
        };
        var parseResult = new ParseResult(null, sourceText, diagnostics);

        var line2 = parseResult.GetDiagnosticsOnLine(2).ToArray();

        Assert.HasCount(1, line2);
    }

    [TestMethod]
    public void ParseResult_FormatDiagnostics_WhenEmpty_ShouldReturnNoDiagnostics()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var parseResult = new ParseResult(null, sourceText, Array.Empty<Diagnostic>());

        var formatted = parseResult.FormatDiagnostics();

        Assert.AreEqual("No diagnostics.", formatted);
    }

    [TestMethod]
    public void ParseResult_FormatDiagnostics_WhenHasErrors_ShouldFormat()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "some error", new TextSpan(0, 5))
        };
        var parseResult = new ParseResult(null, sourceText, diagnostics);

        var formatted = parseResult.FormatDiagnostics();

        Assert.Contains("some error", formatted);
    }

    [TestMethod]
    public void ParseResult_ThrowIfErrors_WhenNoErrors_ShouldNotThrow()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var parseResult = new ParseResult(null, sourceText, Array.Empty<Diagnostic>());

        parseResult.ThrowIfErrors();
    }

    [TestMethod]
    public void ParseResult_ThrowIfErrors_WhenHasErrors_ShouldThrow()
    {
        var sourceText = new SourceText("select 1 from #system.dual()");
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "fatal error", new TextSpan(0, 5))
        };
        var parseResult = new ParseResult(null, sourceText, diagnostics);

        Assert.Throws<ParseException>(() => parseResult.ThrowIfErrors());
    }

    #endregion

    #region ParseException Branch Coverage

    [TestMethod]
    public void ParseException_ShouldCarryDiagnostics()
    {
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error 1", new TextSpan(0, 5)),
            Diagnostic.Error(DiagnosticCode.MQ2002_MissingToken, "error 2", new TextSpan(5, 3))
        };

        var exception = new ParseException("Parse failed", diagnostics);

        Assert.AreEqual("Parse failed", exception.Message);
        Assert.HasCount(2, exception.Diagnostics);
    }

    #endregion

    #region DiagnosticFormatter Branch Coverage

    [TestMethod]
    public void DiagnosticFormatter_Format_WithFilePath_ShouldIncludeFilePath()
    {
        var formatter = new DiagnosticFormatter();
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "test error",
            new SourceLocation(5, 2, 3, "test.sql"),
            new SourceLocation(10, 2, 8));

        var result = formatter.Format(diagnostic);

        Assert.Contains("test.sql", result);
        Assert.Contains("test error", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_Format_WithoutFilePath_ShouldUseParentheses()
    {
        var formatter = new DiagnosticFormatter();
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "test error",
            new TextSpan(0, 5));

        var result = formatter.Format(diagnostic);

        Assert.StartsWith("(", result);
        Assert.Contains("error", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_Format_WithColor_ShouldIncludeAnsiCodes()
    {
        var formatter = new DiagnosticFormatter { UseColor = true };
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "test error",
            new TextSpan(0, 5));

        var result = formatter.Format(diagnostic);

        Assert.Contains("\u001b[31m", result);
        Assert.Contains("\u001b[0m", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_Format_WithoutContextSnippet_ShouldOmitSnippet()
    {
        var formatter = new DiagnosticFormatter { IncludeContextSnippet = false };
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "test error",
            new TextSpan(0, 5));

        var result = formatter.Format(diagnostic);

        Assert.DoesNotContain("-->", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_Format_WithContextSnippet_ShouldShowSnippet()
    {
        var formatter = new DiagnosticFormatter { IncludeContextSnippet = true };
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticSeverity.Error,
            "test error",
            new SourceLocation(0, 1, 1),
            new SourceLocation(5, 1, 6),
            "SELECT * FROM table");

        var result = formatter.Format(diagnostic);

        Assert.Contains("SELECT * FROM table", result);
        Assert.Contains("^", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_Format_WithSuggestedFixes_ShouldShowFixes()
    {
        var formatter = new DiagnosticFormatter();
        var fix = DiagnosticAction.QuickFix("Add missing semicolon", new TextSpan(5, 1), ";");
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "missing semicolon",
            new TextSpan(0, 5))
            .WithSuggestedFix(fix);

        var result = formatter.Format(diagnostic);

        Assert.Contains("Suggested fixes:", result);
        Assert.Contains("Add missing semicolon", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_Format_WarningSeverity_ShouldShowWarning()
    {
        var formatter = new DiagnosticFormatter();
        var diagnostic = Diagnostic.Warning(
            DiagnosticCode.MQ5001_UnusedAlias,
            "unused alias",
            new TextSpan(0, 5));

        var result = formatter.Format(diagnostic);

        Assert.Contains("warning", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_FormatAsJson_ShouldReturnValidStructure()
    {
        var formatter = new DiagnosticFormatter();
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "test error",
            new SourceLocation(0, 1, 1),
            new SourceLocation(5, 1, 6));

        var result = formatter.FormatAsJson(diagnostic);

        Assert.Contains("\"range\"", result);
        Assert.Contains("\"severity\"", result);
        Assert.Contains("\"code\"", result);
        Assert.Contains("\"message\"", result);
        Assert.Contains("test error", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_FormatAsJson_WithSpecialChars_ShouldEscape()
    {
        var formatter = new DiagnosticFormatter();
        var diagnostic = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "error with \"quotes\" and\nnewline",
            new TextSpan(0, 5));

        var result = formatter.FormatAsJson(diagnostic);

        Assert.Contains("\\\"quotes\\\"", result);
        Assert.Contains("\\n", result);
    }

    [TestMethod]
    public void DiagnosticFormatter_Format_WithColorSnippet_ShouldColorErrorLine()
    {
        var formatter = new DiagnosticFormatter { UseColor = true, IncludeContextSnippet = true };
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticSeverity.Warning,
            "test warning",
            new SourceLocation(0, 1, 1),
            new SourceLocation(5, 1, 6),
            "SELECT bad syntax");

        var result = formatter.Format(diagnostic);

        Assert.Contains("\u001b[33m", result);
    }

    #endregion
}
