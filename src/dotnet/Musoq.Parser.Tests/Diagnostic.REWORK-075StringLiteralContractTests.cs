using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticRework075StringLiteralContractTests
{
    [TestMethod]
    [DataRow("""'\\'""", "\\")]
    [DataRow("""'\''""", "'")]
    [DataRow("""'\"'""", "\"")]
    [DataRow("""'\n'""", "\n")]
    [DataRow("""'\r'""", "\r")]
    [DataRow("""'\t'""", "\t")]
    [DataRow("""'\b'""", "\b")]
    [DataRow("""'\f'""", "\f")]
    [DataRow("""'\e'""", "\u001B")]
    [DataRow("""'\0'""", "\0")]
    [DataRow("""'\u0041'""", "A")]
    [DataRow("""'\x4A'""", "J")]
    [DataRow("""'\x00'""", "\0")]
    public void OrdinaryEscapeMatrix_ShouldDecodeEverySupportedSequence(string source, string expected)
    {
        var lexer = new Lexer(source, true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual(expected, token.Value);
        Assert.AreEqual(new TextSpan(0, source.Length), token.Span);
        Assert.AreEqual(TokenType.EndOfFile, lexer.Next().TokenType);
        Assert.IsEmpty(lexer.Diagnostics);
    }

    [TestMethod]
    public void OrdinaryLiteral_ShouldPreserveUnicodePunctuationAndDecodeMixedEscapes()
    {
        const string source = "'Δ [x]{y},?! \\q \\n'";
        const string expected = "Δ [x]{y},?! \\q \n";
        var lexer = new Lexer(source, true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual(expected, token.Value);
        Assert.AreEqual(new TextSpan(0, source.Length), token.Span);
        Assert.IsEmpty(lexer.Diagnostics);
    }

    [TestMethod]
    public void UnknownOrdinaryEscapes_ShouldRemainLiteralWithoutDiagnostics()
    {
        const string source = """'\q \v \z'""";
        var lexer = new Lexer(source, true, recoverOnError: true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual(source[1..^1], token.Value);
        Assert.AreEqual(new TextSpan(0, source.Length), token.Span);
        Assert.IsEmpty(lexer.Diagnostics);
    }

    [TestMethod]
    [DataRow("""'\u'""")]
    [DataRow("""'\u12'""")]
    [DataRow("""'\u123'""")]
    [DataRow("""'\u12G4'""")]
    [DataRow("""'\x'""")]
    [DataRow("""'\x1'""")]
    [DataRow("""'\x1G'""")]
    public void MalformedFixedLengthEscape_ShouldRemainLiteralAndExposeExactDiagnostic(string source)
    {
        var lexer = new Lexer(source, true, recoverOnError: true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual(source[1..^1], token.Value);
        Assert.AreEqual(new TextSpan(0, source.Length), token.Span);
        Assert.AreEqual(TokenType.EndOfFile, lexer.Next().TokenType);

        var diagnostic = lexer.Diagnostics.ToSortedList().Single();
        Assert.AreEqual(DiagnosticCode.MQ1004_InvalidEscapeSequence, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(new TextSpan(1, source.Length - 2), diagnostic.Span);
        Assert.AreEqual($"Invalid escape sequence '{source[1..^1]}'.", diagnostic.Message);

        var metadata = ErrorMetadataCatalog.Get(DiagnosticCode.MQ1004_InvalidEscapeSequence);
        Assert.IsNotNull(metadata);
        Assert.AreEqual(metadata.Explanation, diagnostic.Explanation);
        Assert.AreEqual(metadata.DocsReference, diagnostic.DocsReference);
        Assert.HasCount(metadata.SuggestedFixes.Length, diagnostic.SuggestedFixes);
        Assert.AreEqual(metadata.SuggestedFixes[0], diagnostic.SuggestedFixes[0].Title);
        Assert.AreEqual(DiagnosticActionKind.Suggestion, diagnostic.SuggestedFixes[0].Kind);
        Assert.IsNull(diagnostic.SuggestedFixes[0].TextEdit);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsEmpty(diagnostic.RelatedInfo);
    }

    [TestMethod]
    public void MalformedEscapeAfterValidFixedLengthEscape_ShouldStillBeDiagnosed()
    {
        const string source = """'\u0041-\x4G'""";
        var lexer = new Lexer(source, true, recoverOnError: true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual("A-\\x4G", token.Value);
        var invalidStart = source.IndexOf(@"\x4G", StringComparison.Ordinal);
        var diagnostic = lexer.Diagnostics.ToSortedList().Single();
        Assert.AreEqual(DiagnosticCode.MQ1004_InvalidEscapeSequence, diagnostic.Code);
        Assert.AreEqual(new TextSpan(invalidStart, 4), diagnostic.Span);
        Assert.AreEqual("Invalid escape sequence '\\x4G'.", diagnostic.Message);
    }

    [TestMethod]
    public void OrdinaryUnicodeEscapes_ShouldComposeSurrogatePairAndKeepSourceSpan()
    {
        const string source = """'prefix \u03A9 \uD83D\uDE00'""";
        var lexer = new Lexer(source, true);

        var token = lexer.Next();

        Assert.AreEqual("prefix Ω 😀", token.Value);
        Assert.AreEqual(new TextSpan(0, source.Length), token.Span);
        Assert.IsEmpty(lexer.Diagnostics);
    }

    [TestMethod]
    [DataRow("""r'\n \u0041 \x41 \q'""", "\\n \\u0041 \\x41 \\q")]
    [DataRow("""R'C:\Temp\path'""", "C:\\Temp\\path")]
    [DataRow("""r'a''b''''c'""", "a'b''c")]
    [DataRow("""r'C:\Temp\'""", "C:\\Temp\\")]
    [DataRow("""r'Δ[]{};!?/* --'""", "Δ[]{};!?/* --")]
    public void RawLiteralMatrix_ShouldPreserveContentAndDecodeOnlyDoubledQuotes(
        string source,
        string expected)
    {
        var lexer = new Lexer(source, true, recoverOnError: true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual(expected, token.Value);
        Assert.AreEqual(new TextSpan(0, source.Length), token.Span);
        Assert.AreEqual(TokenType.EndOfFile, lexer.Next().TokenType);
        Assert.IsEmpty(lexer.Diagnostics);
    }

    [TestMethod]
    public void RawLiteral_ShouldPreserveMultilineContentAndCommentMarkers()
    {
        const string source = "r'line one\n-- not a comment /* or bracket ]'";
        var lexer = new Lexer(source, true, recoverOnError: true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual("line one\n-- not a comment /* or bracket ]", token.Value);
        Assert.AreEqual(new TextSpan(0, source.Length), token.Span);
        Assert.IsEmpty(lexer.Comments);
        Assert.IsEmpty(lexer.Diagnostics);
    }

    [TestMethod]
    public void SeparatedRawPrefix_ShouldRemainIdentifierAndOrdinaryLiteral()
    {
        const string source = "R\t'value'";
        var lexer = new Lexer(source, true);

        var prefix = lexer.Next();
        var literal = lexer.Next();

        Assert.AreEqual(TokenType.Identifier, prefix.TokenType);
        Assert.AreEqual("R", prefix.Value);
        Assert.AreEqual(new TextSpan(0, 1), prefix.Span);
        Assert.AreEqual(TokenType.StringLiteral, literal.TokenType);
        Assert.AreEqual("value", literal.Value);
        Assert.AreEqual(new TextSpan(2, 7), literal.Span);
        Assert.AreEqual(TokenType.EndOfFile, lexer.Next().TokenType);
    }

    [TestMethod]
    public void RawTrailingBackslash_ShouldNotEscapeTheClosingQuote()
    {
        const string source = """r'C:\Temp\'""";
        var lexer = new Lexer(source, true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual("C:\\Temp\\", token.Value);
        Assert.AreEqual(new TextSpan(0, source.Length), token.Span);
        Assert.AreEqual(TokenType.EndOfFile, lexer.Next().TokenType);
        Assert.IsEmpty(lexer.Diagnostics);
    }

    [TestMethod]
    public void UnterminatedRawLiteral_InRecovery_ShouldStopAtStatementDelimiterWithExactDiagnostic()
    {
        const string source = "r'not closed; select 1";
        var lexer = new Lexer(source, true, recoverOnError: true);

        Assert.AreEqual(TokenType.Semicolon, lexer.Next().TokenType);
        Assert.AreEqual(TokenType.Select, lexer.Next().TokenType);

        var diagnostic = lexer.Diagnostics.ToSortedList().Single();
        Assert.AreEqual(DiagnosticCode.MQ1002_UnterminatedString, diagnostic.Code);
        Assert.AreEqual(new TextSpan(0, source.IndexOf(';')), diagnostic.Span);
        Assert.AreEqual(
            "Unterminated raw string literal: missing closing '",
            diagnostic.Message);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    public void UnterminatedOrdinaryLiteral_InRecovery_ShouldResumeAtNextLine()
    {
        const string source = "select 'not closed\nselect 1";
        var lexer = new Lexer(source, true, recoverOnError: true);

        Assert.AreEqual(TokenType.Select, lexer.Next().TokenType);
        Assert.AreEqual(TokenType.Select, lexer.Next().TokenType);

        var quoteStart = source.IndexOf('\'');
        var newline = source.IndexOf('\n');
        var diagnostic = lexer.Diagnostics.ToSortedList().Single();
        Assert.AreEqual(DiagnosticCode.MQ1002_UnterminatedString, diagnostic.Code);
        Assert.AreEqual(new TextSpan(quoteStart, newline - quoteStart), diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    public void UnterminatedOrdinaryLiteral_InStrictMode_ShouldExposeWholeRemainingSpan()
    {
        const string source = "'not closed";
        var lexer = new Lexer(source, true);

        var exception = Assert.ThrowsExactly<LexerException>(() => lexer.Next());

        Assert.AreEqual(DiagnosticCode.MQ1002_UnterminatedString, exception.Code);
        Assert.AreEqual(new TextSpan(0, source.Length), exception.Span);
    }
}
