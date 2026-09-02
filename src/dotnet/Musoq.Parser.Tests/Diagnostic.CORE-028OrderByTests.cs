using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore028OrderByTests
{
    [TestMethod]
    public void OrderBy_MixedDirectionsAndNullPlacement_ShouldPreserveFieldMetadata()
    {
        const string query = "select Name from #A.entities() order by City desc nulls first, Name nulls last, Population asc";
        var orderBy = ParseQuery(query).OrderBy ?? throw new AssertFailedException("Expected ORDER BY.");

        Assert.HasCount(3, orderBy.Fields);
        Assert.AreEqual(Order.Descending, orderBy.Fields[0].Order);
        Assert.AreEqual(NullOrdering.First, orderBy.Fields[0].NullOrdering);
        Assert.AreEqual(Order.Ascending, orderBy.Fields[1].Order);
        Assert.AreEqual(NullOrdering.Last, orderBy.Fields[1].NullOrdering);
        Assert.AreEqual(Order.Ascending, orderBy.Fields[2].Order);
        Assert.AreEqual(NullOrdering.Default, orderBy.Fields[2].NullOrdering);
    }

    [TestMethod]
    public void OrderBy_InvalidNullPlacement_ShouldReportExactDiagnostic()
    {
        const string query = "select Name from #A.entities() order by City nulls middle";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2009_InvalidOrderByExpression,
            "Expected FIRST or LAST after NULLS in ORDER BY.",
            SpanOf(query, "middle"),
            "Core Spec - ORDER BY Clause");
    }

    [TestMethod]
    public void OrderBy_TrailingComma_ShouldReportExactDiagnostic()
    {
        const string query = "select Name from #A.entities() order by City,";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2014_TrailingComma,
            "ORDER BY list has a trailing comma. Add another expression or remove the comma.",
            new TextSpan(query.Length - 1, 1),
            "Core Spec - Lists");
    }

    [TestMethod]
    public void OrderBy_LeadingComma_ShouldReportExactDiagnostic()
    {
        const string query = "select Name from #A.entities() order by ,City";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2015_LeadingComma,
            "ORDER BY list has a leading comma. Add an expression before the comma or remove it.",
            new TextSpan(query.IndexOf(',', StringComparison.Ordinal), 1),
            "Core Spec - Lists");
    }

    [TestMethod]
    [DataRow("take -1", "TAKE", "-1")]
    [DataRow("skip -1", "SKIP", "-1")]
    [DataRow("take 3.5", "TAKE", "3.5")]
    [DataRow("skip 'two'", "SKIP", "'two'")]
    public void SliceCount_InvalidForm_ShouldReportExactDiagnostic(string clause, string clauseName, string offendingText)
    {
        var query = $"select 1 from #A.entities() {clause}";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2038_InvalidSliceCount,
            $"{clauseName} count must be a non-negative integer.",
            SpanOf(query, offendingText),
            "Core Spec - TAKE and SKIP");
    }

    private static void AssertParseDiagnostic(
        string query,
        DiagnosticCode expectedCode,
        string expectedMessage,
        TextSpan expectedSpan,
        string expectedDocsReference)
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
        Assert.AreEqual(expectedDocsReference, diagnostic.DocsReference);
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
