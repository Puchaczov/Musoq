using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticUx062DialectConfusionTests
{
    [TestMethod]
    [DataRow(
        "select 1 from #system.dual() d limit 5",
        "limit",
        "TAKE",
        DiagnosticCode.MQ2001_UnexpectedToken)]
    [DataRow(
        "select 1 from #system.dual() d offset 2",
        "offset",
        "SKIP",
        DiagnosticCode.MQ2001_UnexpectedToken)]
    [DataRow(
        "select 1 from #system.dual() d order by 1 offset 2",
        "offset",
        "SKIP",
        DiagnosticCode.MQ2009_InvalidOrderByExpression)]
    [DataRow(
        "select 1 from #system.dual() d order by 1 fetch next 5 rows only",
        "fetch",
        "TAKE",
        DiagnosticCode.MQ2009_InvalidOrderByExpression)]
    [DataRow(
        "select 1 from #system.dual() d order by 1 rows",
        "rows",
        "TAKE",
        DiagnosticCode.MQ2001_UnexpectedToken)]
    [DataRow(
        "select 1 from #system.dual() d order by 1 next 5",
        "next",
        "TAKE",
        DiagnosticCode.MQ2009_InvalidOrderByExpression)]
    [DataRow(
        "select 1 from #system.dual() d order by 1 only",
        "only",
        "TAKE",
        DiagnosticCode.MQ2009_InvalidOrderByExpression)]
    [DataRow(
        "select 1 from #system.dual() d where 1 ilike '1'",
        "ilike",
        "LIKE",
        DiagnosticCode.MQ2001_UnexpectedToken)]
    public void ForeignDialectKeyword_ShouldReportNativeGuidanceAtKeyword(
        string query,
        string keyword,
        string nativeKeyword,
        DiagnosticCode expectedCode)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        var expectedSpan = new TextSpan(query.IndexOf(keyword, StringComparison.OrdinalIgnoreCase), keyword.Length);

        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.Contains(nativeKeyword, diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nativeKeyword, diagnostic.Explanation!, StringComparison.OrdinalIgnoreCase);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
        Assert.IsNotNull(diagnostic.ContextSnippet);
    }

    [TestMethod]
    [DataRow("select 1 from #system.dual() limit 5", "limit", "5", "TAKE")]
    [DataRow("select 1 from #system.dual() offset 2", "offset", "2", "SKIP")]
    public void PaginationKeyword_ShouldRemainActionableWhenParserSpanIsOperand(
        string query,
        string keyword,
        string operand,
        string nativeKeyword)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, diagnostic.Code);
        Assert.AreEqual(new TextSpan(query.IndexOf(operand, StringComparison.Ordinal), operand.Length), diagnostic.Span);
        Assert.AreEqual(keyword, query.Substring(query.IndexOf(keyword, StringComparison.Ordinal), keyword.Length));
        Assert.Contains(nativeKeyword, diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.AreEqual("Core Spec §TAKE / SKIP", diagnostic.DocsReference);
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    [DataRow("select top 5 1 from #system.dual() d", "top", "TOP")]
    [DataRow("select first 5 1 from #system.dual() d", "first", "FIRST")]
    public void PrefixPagination_ShouldRejectUndocumentedSyntaxAtPrefix(
        string query,
        string dialectKeyword,
        string expectedKeyword)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        var expectedSpan = new TextSpan(query.IndexOf(dialectKeyword, StringComparison.OrdinalIgnoreCase), dialectKeyword.Length);

        Assert.AreEqual(DiagnosticCode.MQ2030_UnsupportedSyntax, diagnostic.Code);
        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.Contains(expectedKeyword, diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TAKE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.AreEqual("Core Spec §TAKE / SKIP", diagnostic.DocsReference);
        Assert.IsTrue(diagnostic.SuggestedFixes.Any(fix => fix.Title.Contains("TAKE", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Ilike_ShouldRecommendLikeWithoutAcceptingPostgreSqlSyntax()
    {
        const string query = "select 1 from #system.dual() d where 1 ilike '1'";
        var result = ParseWithDiagnostics(query);
        var diagnostic = result.Diagnostics.Single();

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, diagnostic.Code);
        Assert.AreEqual(new TextSpan(query.IndexOf("ilike", StringComparison.Ordinal), 5), diagnostic.Span);
        Assert.Contains("LIKE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsTrue(diagnostic.SuggestedFixes.Any(fix => fix.Title.Contains("ToLower", StringComparison.OrdinalIgnoreCase)));
        Assert.AreEqual("Core Spec §LIKE Operator", diagnostic.DocsReference);
        Assert.IsTrue(diagnostic.SuggestedFixes.Any(fix => fix.Title.Contains("Replace ILIKE with LIKE", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void FunctionStyleCast_ShouldRecommendStrictPostfixCast()
    {
        const string query = "select cast(1 as int) from #system.dual()";
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        var asSpan = new TextSpan(query.IndexOf(" as ", StringComparison.Ordinal) + 1, 0);

        Assert.AreEqual(DiagnosticCode.MQ2021_UnclosedFunctionCall, diagnostic.Code);
        Assert.AreEqual(asSpan, diagnostic.Span);
        Assert.Contains("postfix", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("::Int32", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.AreEqual("Core Spec §Strict Postfix Casts", diagnostic.DocsReference);
        Assert.IsTrue(diagnostic.SuggestedFixes.Any(fix => fix.Title.Contains("::", StringComparison.Ordinal)));
    }

    [TestMethod]
    [DataRow(
        "param([string]$author) select 1 from #system.dual()",
        "[string]$author",
        DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax)]
    [DataRow(
        "def query(author: str = 'x') select 1 from #system.dual()",
        "def",
        DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax)]
    [DataRow(
        "declare author string; select 1 from #system.dual()",
        "declare",
        DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax)]
    public void BorrowedDeclarationStyle_ShouldReportMusoqParameterSyntax(
        string query,
        string offendingText,
        DiagnosticCode expectedCode)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        var start = query.IndexOf(offendingText, StringComparison.Ordinal);

        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(new TextSpan(start, offendingText.Length), diagnostic.Span);
        Assert.Contains("param(", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
    }

    [TestMethod]
    public void MusoqNativeEquivalents_ShouldRemainValidAndUndecorated()
    {
        var queries = new[]
        {
            "params(limit: int = 5) select 1 from #system.dual() d order by 1 skip 2 take 5",
            "select 1 from #system.dual() d where 'x' like 'x'",
            "select 1::Int32 from #system.dual()"
        };

        foreach (var query in queries)
        {
            var result = ParseWithDiagnostics(query);

            Assert.IsTrue(result.Success, query + Environment.NewLine + result.FormatDiagnostics());
            Assert.IsEmpty(result.Diagnostics, query + Environment.NewLine + result.FormatDiagnostics());
        }
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
