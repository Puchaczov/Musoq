using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class RawStringLiteralDiagnosticsTests
{
    [TestMethod]
    [DataRow(@"'C:\u123'", 3, 5, @"\u123")]
    [DataRow(@"'C:\x1'", 3, 3, @"\x1")]
    public void OrdinaryMalformedFixedLengthEscape_InRecoveryMode_ShouldReportExactDiagnosticSpan(
        string source,
        int expectedStart,
        int expectedLength,
        string invalidEscape)
    {
        var lexer = new Lexer(source, true, true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        var diagnostic = lexer.Diagnostics.ToSortedList().Single();
        Assert.AreEqual(DiagnosticCode.MQ1004_InvalidEscapeSequence, diagnostic.Code);
        Assert.AreEqual(new TextSpan(expectedStart, expectedLength), diagnostic.Span);
        Assert.AreEqual($"Invalid escape sequence '{invalidEscape}'.", diagnostic.Message);
    }

    [TestMethod]
    public void RawUnterminatedLiteral_InRecoveryMode_ShouldReportPrefixInclusiveSpan()
    {
        const string source = @"r'C:\Temp";
        var lexer = new Lexer(source, true, true);

        lexer.Next();

        var diagnostic = lexer.Diagnostics.ToSortedList().Single();
        Assert.AreEqual(DiagnosticCode.MQ1002_UnterminatedString, diagnostic.Code);
        Assert.AreEqual(new TextSpan(0, source.Length), diagnostic.Span);
        Assert.AreEqual("Unterminated raw string literal: missing closing '", diagnostic.Message);
    }

    [TestMethod]
    public void RawMalformedEscapeLookingContent_ShouldNotReportMQ1004()
    {
        var lexer = new Lexer(@"r'\u123\x1\q\'", true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual(@"\u123\x1\q\", token.Value);
        Assert.IsFalse(lexer.Diagnostics.Any(diagnostic =>
            diagnostic.Code == DiagnosticCode.MQ1004_InvalidEscapeSequence));
    }

    [TestMethod]
    public void MQ1004Metadata_ShouldDescribeMalformedEscapesAndWindowsPathOptions()
    {
        var metadata = ErrorMetadataCatalog.Get(DiagnosticCode.MQ1004_InvalidEscapeSequence);

        Assert.IsNotNull(metadata);
        Assert.Contains("malformed", metadata.Explanation, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unknown escapes", metadata.Explanation, System.StringComparison.OrdinalIgnoreCase);
        Assert.IsTrue(metadata.SuggestedFixes.Any(fix =>
            fix.Contains("raw literal", System.StringComparison.OrdinalIgnoreCase) &&
            fix.Contains("double", System.StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    [DataRow("""'\\'""", @"\")]
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
    [DataRow("""'\q'""", @"\q")]
    public void OrdinaryStringLiteral_EscapeCompatibility_ShouldRemainUnchanged(
        string source,
        string expected)
    {
        var token = new Lexer(source, true).Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual(expected, token.Value);
    }
}
