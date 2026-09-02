using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore031DescAndFromFirstTests
{
    [TestMethod]
    public void DescQuery_InvalidInnerStart_ShouldReportAnchoredDiagnostic()
    {
        const string query = "desc query (\n    1 + 2\n)";

        var diagnostic = ParseWithDiagnostics(query).Diagnostics.Single();

        Assert.AreEqual(DiagnosticCode.MQ2024_InvalidSubquery, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(new TextSpan(query.IndexOf('(', StringComparison.Ordinal), 1), diagnostic.Span);
        Assert.AreEqual(1, diagnostic.Location.Line);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    public void DescSettings_SchemaWithoutMethod_ShouldReportAnchoredDiagnostic()
    {
        const string query = "desc settings #schema";

        var diagnostic = ParseWithDiagnostics(query).Diagnostics.Single();

        Assert.AreEqual(DiagnosticCode.MQ2030_UnsupportedSyntax, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(new TextSpan(query.IndexOf("#schema", StringComparison.Ordinal), "#schema".Length), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    public void FromFirstQuery_ShouldPreserveClauseOrderInAst()
    {
        const string query =
            "from #schema.items() where Value > 0 group by Value select Value order by Value " +
            "skip 1 take 2";

        var root = new Parser(new Lexer(query, true)).ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        var singleSet = (SingleSetNode)statements.Statements.Single().Node;
        var parsed = (QueryNode)singleSet.Query;

        Assert.IsNotNull(parsed.From);
        Assert.IsNotNull(parsed.Where);
        Assert.IsNotNull(parsed.GroupBy);
        Assert.IsNotNull(parsed.Select);
        Assert.IsNotNull(parsed.OrderBy);
        Assert.IsNotNull(parsed.Skip);
        Assert.IsNotNull(parsed.Take);
        Assert.AreEqual(1L, parsed.Skip!.Value);
        Assert.AreEqual(2L, parsed.Take!.Value);
    }

    [TestMethod]
    public void FromFirstQuery_WithSelectBeforeWhere_ShouldRejectNearMiss()
    {
        const string query = "from #schema.items() select Value where Value > 0";

        var diagnostic = ParseWithDiagnostics(query).Diagnostics.Single();

        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(new TextSpan(query.IndexOf("where", StringComparison.Ordinal), "where".Length), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
