using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore002StringLiteralTests
{
    [TestMethod]
    public void OrdinaryLiteral_ShouldDecodeEscapesAndPreserveUnicodePunctuation()
    {
        const string source = """'Δ {x}[y], \"quoted\" \u0041 \x42'""";
        var token = new Lexer(source, true).Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual("Δ {x}[y], \"quoted\" A B", token.Value);
        Assert.AreEqual(new TextSpan(0, source.Length), token.Span);
    }

    [TestMethod]
    public void OrdinaryAndRawLiterals_ShouldKeepDifferentBackslashSemantics()
    {
        var ordinary = new Lexer(@"'\n'", true).Next();
        var raw = new Lexer(@"r'\n'", true).Next();

        Assert.AreEqual("\n", ordinary.Value);
        Assert.AreEqual(@"\n", raw.Value);
    }

    [TestMethod]
    public void SeparatedRawPrefix_ShouldRemainAnIdentifierBeforeAnOrdinaryLiteral()
    {
        var lexer = new Lexer("r 'value'", true);

        var prefix = lexer.Next();
        var literal = lexer.Next();

        Assert.AreEqual(TokenType.Identifier, prefix.TokenType);
        Assert.AreEqual("r", prefix.Value);
        Assert.AreEqual(TokenType.StringLiteral, literal.TokenType);
        Assert.AreEqual("value", literal.Value);
    }

    [TestMethod]
    public void MalformedFixedLengthEscape_ShouldPreserveTextAndExposeStructuredDiagnostic()
    {
        const string query = @"select '\u12' from system.dual()";
        const string invalidEscape = @"\u12";
        var lexer = new Lexer(query, true, recoverOnError: true);
        string? literalValue = null;

        Token token;
        while ((token = lexer.Next()).TokenType != TokenType.EndOfFile)
        {
            if (token.TokenType == TokenType.StringLiteral)
                literalValue = token.Value;
        }

        Assert.AreEqual(invalidEscape, literalValue);
        var diagnostic = lexer.Diagnostics.ToSortedList().Single();
        Assert.AreEqual(DiagnosticCode.MQ1004_InvalidEscapeSequence, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(new TextSpan(query.IndexOf(invalidEscape, StringComparison.Ordinal), invalidEscape.Length),
            diagnostic.Span);
        Assert.Contains(invalidEscape, diagnostic.Message);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    public void UnterminatedOrdinaryLiteral_ShouldExposeItsWholeQuerySpanInRecovery()
    {
        const string query = "select 'not closed";
        var lexer = new Lexer(query, true, recoverOnError: true);

        while (lexer.Next().TokenType != TokenType.EndOfFile)
        {
        }

        var diagnostic = lexer.Diagnostics.ToSortedList().Single();
        var start = query.IndexOf('\'', StringComparison.Ordinal);
        Assert.AreEqual(DiagnosticCode.MQ1002_UnterminatedString, diagnostic.Code);
        Assert.AreEqual(new TextSpan(start, query.Length - start), diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }
}
