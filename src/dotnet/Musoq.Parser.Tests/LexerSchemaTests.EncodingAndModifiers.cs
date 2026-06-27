using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

public partial class LexerSchemaTests
{
    #region Encodings

    [TestMethod]
    public void Utf8_ShouldReturnUtf8Token()
    {
        var lexer = new Lexer("utf8", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Utf8, token.TokenType);
    }

    [TestMethod]
    public void Utf16Le_ShouldReturnUtf16LeToken()
    {
        var lexer = new Lexer("utf16le", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Utf16Le, token.TokenType);
    }

    [TestMethod]
    public void Utf16Be_ShouldReturnUtf16BeToken()
    {
        var lexer = new Lexer("utf16be", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Utf16Be, token.TokenType);
    }

    [TestMethod]
    public void Ascii_ShouldReturnAsciiToken()
    {
        var lexer = new Lexer("ascii", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Ascii, token.TokenType);
    }

    [TestMethod]
    public void Latin1_ShouldReturnLatin1Token()
    {
        var lexer = new Lexer("latin1", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Latin1, token.TokenType);
    }

    [TestMethod]
    public void Ebcdic_ShouldReturnEbcdicToken()
    {
        var lexer = new Lexer("ebcdic", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Ebcdic, token.TokenType);
    }

    #endregion

    #region Field Modifiers

    [TestMethod]
    public void Trim_ShouldReturnTrimToken()
    {
        var lexer = new Lexer("trim", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Trim, token.TokenType);
    }

    [TestMethod]
    public void RTrim_ShouldReturnRTrimToken()
    {
        var lexer = new Lexer("rtrim", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.RTrim, token.TokenType);
    }

    [TestMethod]
    public void LTrim_ShouldReturnLTrimToken()
    {
        var lexer = new Lexer("ltrim", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.LTrim, token.TokenType);
    }

    [TestMethod]
    public void NullTerm_ShouldReturnNullTermToken()
    {
        var lexer = new Lexer("nullterm", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.NullTerm, token.TokenType);
    }

    [TestMethod]
    public void Check_ShouldReturnCheckToken()
    {
        var lexer = new Lexer("check", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Check, token.TokenType);
    }

    [TestMethod]
    public void At_ShouldReturnAtToken()
    {
        var lexer = new Lexer("at", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.At, token.TokenType);
    }

    #endregion
}
