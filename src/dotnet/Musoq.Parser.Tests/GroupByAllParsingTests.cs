using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class GroupByAllParsingTests
{
    [TestMethod]
    public void Parse_WhenGroupByAll_ShouldCreateAllGroupByNode()
    {
        var query = ParseSingleQuery("select Col, Count(Col) from schema.method() group by all");

        Assert.IsNotNull(query.GroupBy);
        Assert.IsTrue(query.GroupBy.IsAll);
        Assert.HasCount(0, query.GroupBy.Fields);
        Assert.IsNull(query.GroupBy.Having);
    }

    [TestMethod]
    public void Parse_WhenGroupByAllWithHaving_ShouldKeepHaving()
    {
        var query = ParseSingleQuery(
            "select Col, Count(Col) from schema.method() group by all having Count(Col) > 1");

        Assert.IsNotNull(query.GroupBy);
        Assert.IsTrue(query.GroupBy.IsAll);
        Assert.IsNotNull(query.GroupBy.Having);
        Assert.AreEqual("group by all having Count(Col) > 1", query.GroupBy.ToString());
    }

    [TestMethod]
    public void Parse_WhenGroupByAllMixedWithExplicitField_ShouldThrow()
    {
        var lexer = new Lexer("select Col, Count(Col) from schema.method() group by all, Col", true);
        var parser = new Parser(lexer);

        var exception = Assert.Throws<SyntaxException>(parser.ComposeAll);

        Assert.Contains("GROUP BY ALL cannot be combined", exception.Message);
    }

    [TestMethod]
    public void Parse_WhenExplicitFieldPrecedesGroupByAll_ShouldReportStructuredDiagnostic()
    {
        const string query = "select Col, Count(Col) from schema.method() group by Col, all";
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(
            "GROUP BY ALL cannot be combined with explicit GROUP BY fields.",
            diagnostic.Message);
        Assert.AreEqual(new TextSpan(query.IndexOf("all", System.StringComparison.Ordinal), 3), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.AreEqual(
            "The parser encountered a token that does not fit the expected SQL grammar at this position.",
            diagnostic.Explanation);
        Assert.AreEqual("Core Spec - Statement Structure", diagnostic.DocsReference);
        CollectionAssert.AreEqual(
            new[]
            {
                "Check for missing keywords, commas, or parentheses near this location.",
                "Verify the query follows Musoq SQL syntax."
            },
            diagnostic.SuggestedFixes.Select(static action => action.Title).ToArray());
    }

    [TestMethod]
    public void Parse_WhenAllSelectedOutsideGroupBy_ShouldRemainExpression()
    {
        var query = ParseSingleQuery("select all from schema.method()");

        Assert.IsNull(query.GroupBy);
        Assert.AreEqual("select all from #schema.method()", query.ToString());
    }

    private static QueryNode ParseSingleQuery(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var root = parser.ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        var singleSet = (SingleSetNode)statements.Statements[0].Node;
        return singleSet.Query;
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
