using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore001LexicalTests
{
    [TestMethod]
    public void MixedCaseKeywords_ShouldLexAsTheirKeywordTokens()
    {
        var lexer = new Lexer("SeLeCt 1 FrOm system.dual()", true);

        Assert.AreEqual(TokenType.Select, lexer.Next().TokenType);
        Assert.AreEqual(TokenType.Integer, lexer.Next().TokenType);
        Assert.AreEqual(TokenType.From, lexer.Next().TokenType);
    }

    [TestMethod]
    public void UnicodeIdentifier_ShouldRemainOneCaseSensitiveToken()
    {
        const string identifier = "café_列42";
        var lexer = new Lexer(identifier, true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.Identifier, token.TokenType);
        Assert.AreEqual(identifier, token.Value);
        Assert.AreEqual(new TextSpan(0, identifier.Length), token.Span);
    }

    [TestMethod]
    public void CombiningMarkInIdentifier_ShouldRemainPartOfTheIdentifier()
    {
        const string identifier = "cafe\u0301";
        var lexer = new Lexer(identifier, true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.Identifier, token.TokenType);
        Assert.AreEqual(identifier, token.Value);
    }

    [TestMethod]
    public void KeywordPrefixWithUnicodeContinuation_ShouldNotBecomeAKeyword()
    {
        const string identifier = "selecté";
        var lexer = new Lexer(identifier, true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.Identifier, token.TokenType);
        Assert.AreEqual(identifier, token.Value);
    }

    [TestMethod]
    public void IdentifierCasing_ShouldBePreservedAndCaseSensitive()
    {
        var lexer = new Lexer("Name name", true);

        var first = lexer.Next();
        var second = lexer.Next();

        Assert.AreEqual("Name", first.Value);
        Assert.AreEqual("name", second.Value);
        Assert.AreNotEqual(first.Value, second.Value);
    }

    [TestMethod]
    public void BracketQuotedIdentifier_ShouldAllowReservedWordsAndSpaces()
    {
        var lexer = new Lexer("[case] [Column With Spaces]", true);

        var reserved = lexer.Next();
        var spaced = lexer.Next();

        Assert.AreEqual(TokenType.Identifier, reserved.TokenType);
        Assert.AreEqual("case", reserved.Value);
        Assert.AreEqual("Column With Spaces", spaced.Value);
    }

    [TestMethod]
    public void LineAndBlockComments_ShouldBeSkippedWithoutChangingTheQuery()
    {
        const string query = "-- leading\nselect /* ignored SELECT FROM */ 1 -- trailing\nfrom system.dual()";
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());
    }

    [TestMethod]
    public void UnknownCharacter_ShouldProduceAStableLocatedLexerDiagnostic()
    {
        const string query = "select @ from system.dual()";
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success);
        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ1001_UnknownToken, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(new TextSpan(query.IndexOf('@'), 1), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
        Assert.IsNotNull(diagnostic.ContextSnippet);
    }

    [TestMethod]
    public void UnterminatedBlockComment_ShouldProduceAStableWholeCommentDiagnostic()
    {
        const string query = "select 1 from system.dual() /* missing";
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success);
        var diagnostic = result.Diagnostics.Single();
        var commentStart = query.IndexOf("/*", StringComparison.Ordinal);
        Assert.AreEqual(DiagnosticCode.MQ1005_UnterminatedBlockComment, diagnostic.Code);
        Assert.AreEqual(new TextSpan(commentStart, query.Length - commentStart), diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
        Assert.IsNotNull(diagnostic.ContextSnippet);
    }

    [TestMethod]
    public void UnterminatedBracketedIdentifier_ShouldReportTheMalformedLexicalSpan()
    {
        const string query = "select [Column from system.dual()";
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success);
        var diagnostic = result.Diagnostics.Single();
        var identifierStart = query.IndexOf('[', StringComparison.Ordinal);
        Assert.AreEqual(DiagnosticCode.MQ2011_MissingClosingBracket, diagnostic.Code);
        Assert.AreEqual(new TextSpan(identifierStart, query.Length - identifierStart), diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
