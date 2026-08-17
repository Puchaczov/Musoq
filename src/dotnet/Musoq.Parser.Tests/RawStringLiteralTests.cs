using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public class RawStringLiteralTests
{
    [TestMethod]
    public void RawStringLiteral_LowercasePrefix_ReturnsValueAndPrefixSpan()
    {
        var lexer = new Lexer("r'hello'", true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual("hello", token.Value);
        Assert.AreEqual(0, token.Span.Start);
        Assert.AreEqual(8, token.Span.Length);
    }

    [TestMethod]
    public void RawStringLiteral_UppercasePrefix_ReturnsStringLiteralToken()
    {
        var lexer = new Lexer("R'hello'", true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual("hello", token.Value);
    }

    [TestMethod]
    public void RawStringLiteral_EmptyValue_ReturnsEmptyString()
    {
        var lexer = new Lexer("r''", true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual(string.Empty, token.Value);
    }

    [TestMethod]
    public void RawStringLiteral_WindowsPath_PreservesBackslashes()
    {
        var lexer = new Lexer(@"r'C:\Some\Path\To\Directory'", true);

        var token = lexer.Next();

        Assert.AreEqual(@"C:\Some\Path\To\Directory", token.Value);
    }

    [TestMethod]
    public void RawStringLiteral_EscapeLookingSequences_PreserveBackslashes()
    {
        var lexer = new Lexer(@"r'C:\new\test\x4A\u0041\n\t\0'", true);

        var token = lexer.Next();

        Assert.AreEqual(@"C:\new\test\x4A\u0041\n\t\0", token.Value);
    }

    [TestMethod]
    public void RawStringLiteral_TrailingBackslash_PreservesBackslash()
    {
        var lexer = new Lexer(@"r'C:\Temp\'", true);

        var token = lexer.Next();

        Assert.AreEqual(@"C:\Temp\", token.Value);
        Assert.IsEmpty(lexer.Diagnostics);
    }

    [TestMethod]
    public void RawStringLiteral_DoubledQuote_DecodesToSingleQuote()
    {
        var lexer = new Lexer(@"r'a''b'", true);

        var token = lexer.Next();

        Assert.AreEqual("a'b", token.Value);
    }

    [TestMethod]
    public void RawStringLiteral_QuoteOnlyValue_DecodesDoubledQuote()
    {
        var lexer = new Lexer("r''''", true);

        var token = lexer.Next();

        Assert.AreEqual("'", token.Value);
    }

    [TestMethod]
    public void RawStringLiteral_MultipleDoubledQuotes_DecodesAllPairs()
    {
        var lexer = new Lexer("r'a''b''''c'", true);

        var token = lexer.Next();

        Assert.AreEqual("a'b''c", token.Value);
    }

    [TestMethod]
    [DataRow(@"r'\\server\share'", @"\\server\share")]
    [DataRow(@"r'\\?\C:\Directory'", @"\\?\C:\Directory")]
    [DataRow(@"r'\\.\pipe\name'", @"\\.\pipe\name")]
    [DataRow(@"r'C:\A/B\C'", @"C:\A/B\C")]
    [DataRow(@"r'C:\\A\\B'", @"C:\\A\\B")]
    [DataRow(@"r'C:\Program Files\App'", @"C:\Program Files\App")]
    [DataRow(@"r'C:\Δata\[x]{y}*.log;!?,'", @"C:\Δata\[x]{y}*.log;!?,")]
    public void RawStringLiteral_WindowsAndPunctuationContent_IsPreserved(string source, string expected)
    {
        AssertRawValue(source, expected);
    }

    [TestMethod]
    public void RawStringLiteral_MultilineContent_IsPreserved()
    {
        var lexer = new Lexer("r'line one\nline two'", true);

        var token = lexer.Next();

        Assert.AreEqual("line one\nline two", token.Value);
    }

    [TestMethod]
    [DataRow(@"r'\q'", @"\q")]
    [DataRow(@"r'\u123'", @"\u123")]
    [DataRow(@"r'\x1'", @"\x1")]
    [DataRow(@"r'\'", @"\")]
    public void RawStringLiteral_MalformedEscapeLookingContent_IsLiteralAndHasNoDiagnostic(
        string source,
        string expected)
    {
        var lexer = new Lexer(source, true);

        var token = lexer.Next();

        Assert.AreEqual(expected, token.Value);
        Assert.IsEmpty(lexer.Diagnostics);
    }

    [TestMethod]
    public void RawStringLiteral_IsRecognizedInSchemaContext()
    {
        var lexer = new Lexer(@"r'C:\new\test'", true) { IsSchemaContext = true };

        var token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual(@"C:\new\test", token.Value);
    }

    [TestMethod]
    [DataRow("r")]
    [DataRow("R")]
    [DataRow("r1")]
    [DataRow("read")]
    public void RawPrefixWithoutAdjacentQuote_RemainsAnIdentifier(string source)
    {
        var lexer = new Lexer(source, true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.Identifier, token.TokenType);
        Assert.AreEqual(source, token.Value);
    }

    [TestMethod]
    public void RawPrefixFollowedByFunctionCall_RemainsAFunction()
    {
        var lexer = new Lexer("r()", true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.Function, token.TokenType);
        Assert.AreEqual("r", token.Value);
    }

    [TestMethod]
    public void RawPrefixFollowedByProperty_RemainsPropertyAccess()
    {
        var lexer = new Lexer("r.column", true);

        Assert.AreEqual(TokenType.Identifier, lexer.Next().TokenType);
        Assert.AreEqual(TokenType.Dot, lexer.Next().TokenType);
        Assert.AreEqual(TokenType.Property, lexer.Next().TokenType);
    }

    [TestMethod]
    public void RawPrefixFollowedByKeyAccess_RemainsKeyAccess()
    {
        var lexer = new Lexer("r['key']", true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.KeyAccess, token.TokenType);
        var keyAccess = (KeyAccessToken)token;
        Assert.AreEqual("r", keyAccess.Name);
        Assert.AreEqual("'key'", keyAccess.Key);
    }

    [TestMethod]
    public void RawPrefixSeparatedFromQuote_IsNotRawSyntax()
    {
        var lexer = new Lexer("r 'value'", true);

        var identifier = lexer.Next();
        var literal = lexer.Next();

        Assert.AreEqual(TokenType.Identifier, identifier.TokenType);
        Assert.AreEqual("r", identifier.Value);
        Assert.AreEqual(TokenType.StringLiteral, literal.TokenType);
        Assert.AreEqual("value", literal.Value);
    }

    [TestMethod]
    public void RawStringLiteral_WithCommentText_IsParsedAsOneLiteral()
    {
        var root = new Parser(new Lexer("select r'-- not a comment' from #some.files()", true))
            .ComposeAll();

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void RawStringLiteral_IsAcceptedInQueryExpressionContexts()
    {
        const string query = @"
let path: string = r'C:\A';
select
    $path,
    case when Col = r'C:\A' then r'C:\B' else r'C:\C' end,
    Length(r'C:\A')
from schema.method(r'C:\source', true)
where Col in (r'C:\A', r'C:\B')
  and Col like r'%A'
  and Col rlike r'.*A'
group by Col
having Count(Col) > Length(r'')
order by r'C:\A'";

        var root = new Parser(new Lexer(query, true)).ComposeAll();

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void RawStringLiteral_IsAcceptedInValuesSource()
    {
        const string query = @"
from values {
    { Path: r'C:\new\test' }
} paths
select paths.Path";

        var root = new Parser(new Lexer(query, true)).ComposeAll();

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void RawStringLiteral_UnterminatedStrictMode_ThrowsUnterminatedStringDiagnostic()
    {
        var lexer = new Lexer(@"r'C:\Temp", true);

        var exception = Assert.Throws<LexerException>(() => lexer.Next());

        Assert.AreEqual(DiagnosticCode.MQ1002_UnterminatedString, exception.Code);
        Assert.AreEqual(0, exception.Position);
    }

    [TestMethod]
    public void RawStringLiteral_UnterminatedRecoveryMode_ContinuesAcrossSemicolon()
    {
        var lexer = new Lexer("r'not closed; select", true, true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.Semicolon, token.TokenType);
        Assert.HasCount(1, lexer.Diagnostics);
        Assert.AreEqual(DiagnosticCode.MQ1002_UnterminatedString, lexer.Diagnostics.ToSortedList()[0].Code);

        token = lexer.Next();
        Assert.AreEqual(TokenType.Select, token.TokenType);
    }

    [TestMethod]
    public void RawStringLiteral_UnterminatedRecoveryMode_ContinuesAtNextLine()
    {
        var lexer = new Lexer("r'not closed\nselect", true, true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.Select, token.TokenType);
        Assert.HasCount(1, lexer.Diagnostics);
        Assert.AreEqual(DiagnosticCode.MQ1002_UnterminatedString, lexer.Diagnostics.ToSortedList()[0].Code);
    }

    private static void AssertRawValue(string source, string expected)
    {
        var lexer = new Lexer(source, true);
        var token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual(expected, token.Value);
        Assert.IsEmpty(lexer.Diagnostics);
    }
}
