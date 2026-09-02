using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticUx061KeywordMisspellingsTests
{
    [TestMethod]
    [DataRow("SELCT 1 FROM #system.dual()", "SELCT", "SELECT", DiagnosticCode.MQ2001_UnexpectedToken)]
    [DataRow("SELECT 1 FRMO #system.dual()", "FRMO", "FROM", DiagnosticCode.MQ2004_MissingFromClause)]
    [DataRow("SELECT 1 FROM #system.dual() d WHRE 1 = 1", "WHRE", "WHERE", DiagnosticCode.MQ2001_UnexpectedToken)]
    [DataRow("SELECT 1 FROM #system.dual() d GROPU BY 1", "GROPU", "GROUP", DiagnosticCode.MQ2001_UnexpectedToken)]
    [DataRow("SELECT 1 FROM #system.dual() d ORDRE BY 1", "ORDRE", "ORDER", DiagnosticCode.MQ2001_UnexpectedToken)]
    [DataRow("SELECT 1 FROM #system.dual() d HAVIGN 1 = 1", "HAVIGN", "HAVING", DiagnosticCode.MQ2001_UnexpectedToken)]
    [DataRow("SELECT 1 FROM #system.dual() a JION #system.dual() b ON 1 = 1", "JION", "JOIN", DiagnosticCode.MQ2001_UnexpectedToken)]
    public void SingleEditAndTransposedKeyword_ShouldReportLocatedSuggestion(
        string query,
        string misspelling,
        string replacement,
        DiagnosticCode expectedCode)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        var expectedSpan = new TextSpan(query.IndexOf(misspelling, StringComparison.Ordinal), misspelling.Length);

        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.Contains($"Did you mean '{replacement}'?", diagnostic.Message);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotNull(diagnostic.ContextSnippet);

        var quickFix = diagnostic.SuggestedFixes.Single(fix => fix.Kind == DiagnosticActionKind.QuickFix);
        Assert.AreEqual($"Replace '{misspelling}' with '{replacement}'", quickFix.Title);
        Assert.IsNotNull(quickFix.TextEdit);
        Assert.AreEqual(expectedSpan, quickFix.TextEdit!.Span);
        Assert.AreEqual(replacement, quickFix.TextEdit.NewText);
    }

    [TestMethod]
    public void MisspelledFromOnLaterLine_ShouldPreserveLineAndColumn()
    {
        const string query = "SELECT 1\nFRMO #system.dual()";
        var result = ParseWithDiagnostics(query);
        var diagnostic = result.Diagnostics.Single();

        Assert.AreEqual(DiagnosticCode.MQ2004_MissingFromClause, diagnostic.Code);
        Assert.AreEqual(new TextSpan(query.IndexOf("FRMO", StringComparison.Ordinal), 4), diagnostic.Span);
        Assert.AreEqual(2, diagnostic.Location.Line);
        Assert.AreEqual(1, diagnostic.Location.Column);
        Assert.AreEqual(2, diagnostic.EndLocation.Line);
        Assert.AreEqual(5, diagnostic.EndLocation.Column);
        Assert.Contains("FROM", diagnostic.Message);
    }

    [TestMethod]
    public void CorrectKeywordCasing_ShouldRemainValid()
    {
        const string query =
            "sElEcT 1 fRoM #system.dual() d wHeRe 1 = 1 gRoUp By 1 hAvInG 1 = 1 oRdEr By 1";
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());
    }

    [TestMethod]
    public void NearKeywordAliases_ShouldRemainValidWhenGrammarProvidesTheBoundary()
    {
        var projectionAlias = ParseWithDiagnostics("SELECT 1 FRMO FROM #system.dual()");
        var sourceAlias = ParseWithDiagnostics("SELECT 1 FROM #system.dual() FRMO");

        Assert.IsTrue(projectionAlias.Success, projectionAlias.FormatDiagnostics());
        Assert.IsTrue(sourceAlias.Success, sourceAlias.FormatDiagnostics());
        Assert.AreEqual("FRMO", GetQuery(projectionAlias).Select.Fields[0].FieldName);
        Assert.AreEqual("FRMO", GetQuery(sourceAlias).From.Alias);
    }

    private static QueryNode GetQuery(ParseResult result)
    {
        var statements = (StatementsArrayNode)result.Root!.Expression;
        return ((SingleSetNode)statements.Statements.Single().Node).Query;
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
