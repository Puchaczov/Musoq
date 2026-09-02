using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore025WindowTests
{
    [TestMethod]
    public void WindowSpecification_CompositePartitionAndNullOrdering_ShouldParse()
    {
        var query = ParseQuery(
            "select RowNumber() over (partition by Country, City order by Name desc nulls last) from #A.entities()");
        var window = Assert.IsInstanceOfType<WindowFunctionNode>(query.Select.Fields[0].Expression);
        var specification = window.WindowSpecification ?? throw new AssertFailedException("Expected window specification.");

        Assert.HasCount(2, specification.PartitionFields);
        Assert.HasCount(1, specification.OrderByFields);
        Assert.AreEqual(Order.Descending, specification.OrderByFields[0].Order);
        Assert.AreEqual(NullOrdering.Last, specification.OrderByFields[0].NullOrdering);
    }

    [TestMethod]
    public void WindowPartitionBy_TrailingComma_ShouldReportExactDiagnostic()
    {
        const string query = "select RowNumber() over (partition by City, order by Name) from #A.entities()";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2014_TrailingComma,
            "Window PARTITION BY list has a trailing comma. Add another expression or remove the comma.",
            SpanOf(query, "order by"));
    }

    [TestMethod]
    public void WindowOrderBy_TrailingComma_ShouldReportExactDiagnostic()
    {
        const string query = "select RowNumber() over (order by Name,) from #A.entities()";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2014_TrailingComma,
            "Window ORDER BY list has a trailing comma. Add another expression or remove the comma.",
            new TextSpan(query.IndexOf(",)", StringComparison.Ordinal) + 1, 1));
    }

    [TestMethod]
    public void WindowOrderBy_InvalidNullPlacement_ShouldReportExactDiagnostic()
    {
        const string query = "select RowNumber() over (order by Name nulls middle) from #A.entities()";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2009_InvalidOrderByExpression,
            "Expected FIRST or LAST after NULLS in ORDER BY.",
            SpanOf(query, "middle"));
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
        Assert.AreEqual("Core Spec - " + (expectedCode == DiagnosticCode.MQ2009_InvalidOrderByExpression ? "ORDER BY Clause" : "Lists"), diagnostic.DocsReference);
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
