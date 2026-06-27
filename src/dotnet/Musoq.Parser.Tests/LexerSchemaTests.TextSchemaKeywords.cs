using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

public partial class LexerSchemaTests
{
    #region Text Schema Keywords

    [TestMethod]
    public void Pattern_ShouldReturnPatternToken()
    {
        var lexer = new Lexer("pattern", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Pattern, token.TokenType);
    }

    [TestMethod]
    public void Literal_ShouldReturnLiteralToken()
    {
        var lexer = new Lexer("literal", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Literal, token.TokenType);
    }

    [TestMethod]
    public void Until_ShouldReturnUntilToken()
    {
        var lexer = new Lexer("until", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Until, token.TokenType);
    }

    [TestMethod]
    public void Between_ShouldReturnBetweenToken()
    {
        var lexer = new Lexer("between", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Between, token.TokenType);
    }

    [TestMethod]
    public void Chars_ShouldReturnCharsToken()
    {
        var lexer = new Lexer("chars", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Chars, token.TokenType);
    }

    [TestMethod]
    public void Token_ShouldReturnTokenToken()
    {
        var lexer = new Lexer("token", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Token, token.TokenType);
    }

    [TestMethod]
    public void Rest_ShouldReturnRestToken()
    {
        var lexer = new Lexer("rest", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Rest, token.TokenType);
    }

    [TestMethod]
    public void Whitespace_ShouldReturnWhitespaceToken()
    {
        var lexer = new Lexer("whitespace", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Whitespace, token.TokenType);
    }

    [TestMethod]
    public void Optional_ShouldReturnOptionalToken()
    {
        var lexer = new Lexer("optional", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Optional, token.TokenType);
    }

    [TestMethod]
    public void Repeat_ShouldReturnRepeatToken()
    {
        var lexer = new Lexer("repeat", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Repeat, token.TokenType);
    }

    [TestMethod]
    public void Switch_ShouldReturnSwitchToken()
    {
        var lexer = new Lexer("switch", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Switch, token.TokenType);
    }

    [TestMethod]
    public void Nested_ShouldReturnNestedToken()
    {
        var lexer = new Lexer("nested", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Nested, token.TokenType);
    }

    [TestMethod]
    public void Escaped_ShouldReturnEscapedToken()
    {
        var lexer = new Lexer("escaped", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Escaped, token.TokenType);
    }

    [TestMethod]
    public void Greedy_ShouldReturnGreedyToken()
    {
        var lexer = new Lexer("greedy", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Greedy, token.TokenType);
    }

    [TestMethod]
    public void Lazy_ShouldReturnLazyToken()
    {
        var lexer = new Lexer("lazy", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Lazy, token.TokenType);
    }

    [TestMethod]
    public void Lower_ShouldReturnLowerToken()
    {
        var lexer = new Lexer("lower", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Lower, token.TokenType);
    }

    [TestMethod]
    public void Upper_ShouldReturnUpperToken()
    {
        var lexer = new Lexer("upper", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Upper, token.TokenType);
    }

    [TestMethod]
    public void Capture_ShouldReturnCaptureToken()
    {
        var lexer = new Lexer("capture", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Capture, token.TokenType);
    }

    #endregion
}
