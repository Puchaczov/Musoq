using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore008ScriptVariableTests
{
    [TestMethod]
    [DataRow(
        "let string topic = 'important'; select 1 from #test.rows()",
        "string topic",
        DisplayName = "C# declaration order")]
    [DataRow(
        "let topic: string; select 1 from #test.rows()",
        "topic: string;",
        DisplayName = "missing initializer")]
    public void MalformedLetDeclarations_ShouldReportExactStructuredParseDiagnostic(
        string query,
        string offendingText)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ2033_InvalidScriptVariableDeclaration, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);

        var start = query.IndexOf(offendingText, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start);
        Assert.AreEqual(new TextSpan(start, offendingText.Length), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    public void LetAsAnExpressionIdentifier_ShouldRemainContextualOutsideDeclarationPosition()
    {
        const string query = "select let from #test.rows()";

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
