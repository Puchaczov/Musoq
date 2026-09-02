using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore023GroupByFilterTests
{
    [TestMethod]
    public void FilterClause_MissingOpeningParenthesis_ShouldReportExactParseDiagnostic()
    {
        const string query = "select Count(Name) filter where Name = 'Alice' from #some.a()";

        AssertMalformedFilter(
            query,
            "where",
            "Expected token is LeftParenthesis but received Where.");
    }

    [TestMethod]
    public void FilterClause_MissingWhereKeyword_ShouldReportExactParseDiagnostic()
    {
        const string query = "select Count(Name) filter (Population > 0) from #some.a()";

        AssertMalformedFilter(
            query,
            "Population",
            "Expected token is Where but received Identifier.");
    }

    private static void AssertMalformedFilter(string query, string offendingText, string expectedMessage)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        var result = new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(expectedMessage, diagnostic.Message);
        Assert.AreEqual(
            new TextSpan(query.IndexOf(offendingText, StringComparison.Ordinal), offendingText.Length),
            diagnostic.Span);
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
}
