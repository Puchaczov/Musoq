using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore026WindowTests
{
    [TestMethod]
    public void WindowSpecification_ExplicitFrame_ShouldPreserveBounds()
    {
        var query = ParseQuery(
            "select Sum(Population) over (partition by City order by Population rows between 1 preceding and 2 following) from #A.entities()");
        var window = Assert.IsInstanceOfType<WindowFunctionNode>(query.Select.Fields[0].Expression);
        var specification = window.WindowSpecification ?? throw new AssertFailedException("Expected window specification.");
        var frame = specification.Frame ?? throw new AssertFailedException("Expected window frame.");

        Assert.AreEqual(WindowFrameType.Rows, frame.FrameType);
        Assert.AreEqual(WindowFrameBoundType.OffsetPreceding, frame.Start.BoundType);
        Assert.AreEqual(1, frame.Start.Offset);
        Assert.AreEqual(WindowFrameBoundType.OffsetFollowing, frame.End.BoundType);
        Assert.AreEqual(2, frame.End.Offset);
    }

    [TestMethod]
    public void WindowFrame_MissingEndBound_ShouldReportExactDiagnostic()
    {
        const string query = "select Sum(Population) over (order by Name rows between current row and) from #A.entities()";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2001_UnexpectedToken,
            "Expected window frame bound (UNBOUNDED PRECEDING/FOLLOWING, CURRENT ROW, or N PRECEDING/FOLLOWING). Musoq does not support SQL Server OFFSET/FETCH ROWS syntax. Use TAKE and SKIP instead.",
            new TextSpan(query.IndexOf("and)", StringComparison.Ordinal) + "and".Length, 1));
    }

    [TestMethod]
    public void GroupFrame_ShouldReportStableUnsupportedSyntaxDiagnostic()
    {
        const string query = "select Sum(Population) over (order by Population groups between current row and current row) from #A.entities()";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2009_InvalidOrderByExpression,
            "Unrecognized token for ComposeOrder(), the token was Identifier.",
            SpanOf(query, "groups"));
    }

    private static void AssertParseDiagnostic(
        string query,
        DiagnosticCode expectedCode,
        string expectedMessage,
        TextSpan expectedSpan)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        var result = new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(expectedMessage, diagnostic.Message);
        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    private static QueryNode ParseQuery(string query)
    {
        var lexer = new Lexer(query, true);
        var root = new Parser(lexer, lexer.Diagnostics).ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        return ((SingleSetNode)statements.Statements[0].Node).Query;
    }

    private static TextSpan SpanOf(string query, string text)
    {
        return new TextSpan(query.IndexOf(text, StringComparison.Ordinal), text.Length);
    }
}
