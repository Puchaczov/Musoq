using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticRework079StatementParameterContractTests
{
    [TestMethod]
    public void RootGrammar_ShouldAllowLeadingParameterBoundaryAndRequireLaterSeparators()
    {
        const string query =
            "params() let value: int = 1; select $value from #test.rows(); select 2 from #test.rows();";
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var statements = Assert.IsInstanceOfType<StatementsArrayNode>(result.Root!.Expression);
        Assert.HasCount(4, statements.Statements);
        Assert.IsInstanceOfType<ParameterBlockNode>(statements.Statements[0].Node);
        Assert.IsInstanceOfType<ScriptVariableDeclarationNode>(statements.Statements[1].Node);
        Assert.IsInstanceOfType<SingleSetNode>(statements.Statements[2].Node);
        Assert.IsInstanceOfType<SingleSetNode>(statements.Statements[3].Node);
    }

    [TestMethod]
    public void ParameterDeclarationMatrix_ShouldPreserveCaseTypesDefaultsAndSpans()
    {
        const string query =
            "params(Flag: boolean, flag: bit, amount: money = 10.5, ids: guid[], maybe: datetime? = null) " +
            "select $Flag, $flag from #test.rows()";
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var statements = Assert.IsInstanceOfType<StatementsArrayNode>(result.Root!.Expression);
        var block = Assert.IsInstanceOfType<ParameterBlockNode>(statements.Statements[0].Node);
        Assert.AreEqual(
            new TextSpan(query.IndexOf("params", StringComparison.Ordinal), "params(Flag: boolean, flag: bit, amount: money = 10.5, ids: guid[], maybe: datetime? = null)".Length),
            block.Span);
        CollectionAssert.AreEqual(
            new[] { "Flag", "flag", "amount", "ids", "maybe" },
            block.Parameters.Select(static parameter => parameter.Name).ToArray());
        CollectionAssert.AreEqual(
            new[] { "boolean", "bit", "money", "guid[]", "datetime?" },
            block.Parameters.Select(static parameter => parameter.DeclaredTypeName).ToArray());
        Assert.IsFalse(block.Parameters[0].IsNullable);
        Assert.IsFalse(block.Parameters[1].IsNullable);
        Assert.IsInstanceOfType<DecimalNode>(block.Parameters[2].DefaultValue);
        Assert.IsFalse(block.Parameters[3].HasDefaultValue);
        Assert.IsTrue(block.Parameters[4].IsNullable);
        Assert.IsInstanceOfType<NullNode>(block.Parameters[4].DefaultValue);

        var declarationTexts = new[]
        {
            "Flag: boolean",
            "flag: bit",
            "amount: money = 10.5",
            "ids: guid[]",
            "maybe: datetime? = null"
        };
        for (var index = 0; index < block.Parameters.Length; index++)
        {
            var parameter = block.Parameters[index];
            var text = declarationTexts[index];
            var start = query.IndexOf(text, StringComparison.Ordinal);
            Assert.IsGreaterThanOrEqualTo(0, start);
            Assert.AreEqual(new TextSpan(start, text.Length), parameter.Span);
        }
    }

    [TestMethod]
    [DataRow(
        "select 1 from #test.rows() select 2 from #test.rows()",
        "select",
        DiagnosticCode.MQ2001_UnexpectedToken,
        DisplayName = "query statements")]
    public void StatementBatchWithoutSeparator_ShouldReportOneExactStructuredDiagnostic(
        string query,
        string offendingText,
        DiagnosticCode expectedCode)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        var start = query.LastIndexOf(offendingText, StringComparison.Ordinal);
        Assert.AreEqual(new TextSpan(start, offendingText.Length), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(start, envelope.Offset);
        Assert.AreEqual(offendingText.Length, envelope.Length);
        Assert.IsNotEmpty(envelope.Actions);
    }

    [TestMethod]
    [DataRow("param()", DisplayName = "canonical spelling")]
    [DataRow("params();", DisplayName = "alias spelling with terminator")]
    public void ParameterBlockWithoutScriptItem_ShouldReportIncompleteStatement(string query)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ2016_IncompleteStatement, diagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(new TextSpan(query.Length, 0), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    [DataRow(
        "param(author string) select 1 from #test.rows()",
        "author string",
        DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration)]
    [DataRow(
        "param(author: ) select 1 from #test.rows()",
        ")",
        DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration)]
    [DataRow(
        "param(author: string,) select 1 from #test.rows()",
        ")",
        DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration)]
    [DataRow(
        "param(author: string = $other) select 1 from #test.rows()",
        "$other",
        DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration)]
    [DataRow(
        "param([string]$author) select 1 from #test.rows()",
        "[string]$author",
        DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax)]
    [DataRow(
        "def query(author: string) select 1 from #test.rows()",
        "def",
        DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax)]
    [DataRow(
        "declare author string; select 1 from #test.rows()",
        "declare",
        DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax)]
    public void MalformedParameterSyntax_ShouldReportOneExactStructuredDiagnostic(
        string query,
        string offendingText,
        DiagnosticCode expectedCode)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        var start = query.IndexOf(offendingText, StringComparison.Ordinal);
        Assert.AreEqual(new TextSpan(start, offendingText.Length), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
