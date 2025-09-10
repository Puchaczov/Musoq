using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public class LexerTests
{
    [TestMethod]
    public void CheckEmptyString_ShouldReturnWordToken()
    {
        var lexer = new Lexer("''", true);

        var token = lexer.Current();

        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();

        Assert.AreEqual(TokenType.Word, token.TokenType);
        Assert.AreEqual(string.Empty, token.Value);
    }

    [TestMethod]
    public void CheckTestString_ShouldReturnWordToken()
    {
        var lexer = new Lexer("'test'", true);

        var token = lexer.Current();

        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();

        Assert.AreEqual(TokenType.Word, token.TokenType);
        Assert.AreEqual("test", token.Value);
    }
    
    [TestMethod]
    public void HexadecimalLiteral_Lowercase_ShouldReturnHexToken()
    {
        var lexer = new Lexer("0xff", true);
        
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, token.TokenType);
        Assert.AreEqual("0xff", token.Value);
    }
    
    [TestMethod]
    public void HexadecimalLiteral_Uppercase_ShouldReturnHexToken()
    {
        var lexer = new Lexer("0xFF", true);
        
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, token.TokenType);
        Assert.AreEqual("0xFF", token.Value);
    }
    
    [TestMethod]
    public void HexadecimalLiteral_MixedCase_ShouldReturnHexToken()
    {
        var lexer = new Lexer("0XaB", true);
        
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, token.TokenType);
        Assert.AreEqual("0XaB", token.Value);
    }
    
    [TestMethod]
    public void HexadecimalLiteral_SingleDigit_ShouldReturnHexToken()
    {
        var lexer = new Lexer("0xA", true);
        
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, token.TokenType);
        Assert.AreEqual("0xA", token.Value);
    }
    
    [TestMethod]
    public void HexadecimalLiteral_LongValue_ShouldReturnHexToken()
    {
        var lexer = new Lexer("0xDEADBEEF", true);
        
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, token.TokenType);
        Assert.AreEqual("0xDEADBEEF", token.Value);
    }
    
    [TestMethod]
    public void BinaryLiteral_Lowercase_ShouldReturnBinaryToken()
    {
        var lexer = new Lexer("0b101", true);
        
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.BinaryInteger, token.TokenType);
        Assert.AreEqual("0b101", token.Value);
    }
    
    [TestMethod]
    public void BinaryLiteral_Uppercase_ShouldReturnBinaryToken()
    {
        var lexer = new Lexer("0B111", true);
        
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.BinaryInteger, token.TokenType);
        Assert.AreEqual("0B111", token.Value);
    }
    
    [TestMethod]
    public void BinaryLiteral_SingleBit_ShouldReturnBinaryToken()
    {
        var lexer = new Lexer("0b1", true);
        
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.BinaryInteger, token.TokenType);
        Assert.AreEqual("0b1", token.Value);
    }
    
    [TestMethod]
    public void BinaryLiteral_LongValue_ShouldReturnBinaryToken()
    {
        var lexer = new Lexer("0b11111111", true);
        
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.BinaryInteger, token.TokenType);
        Assert.AreEqual("0b11111111", token.Value);
    }
    
    [TestMethod]
    public void OctalLiteral_Lowercase_ShouldReturnOctalToken()
    {
        var lexer = new Lexer("0o77", true);
        
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.OctalInteger, token.TokenType);
        Assert.AreEqual("0o77", token.Value);
    }
    
    [TestMethod]
    public void OctalLiteral_Uppercase_ShouldReturnOctalToken()
    {
        var lexer = new Lexer("0O123", true);
        
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.OctalInteger, token.TokenType);
        Assert.AreEqual("0O123", token.Value);
    }
    
    [TestMethod]
    public void OctalLiteral_SingleDigit_ShouldReturnOctalToken()
    {
        var lexer = new Lexer("0o7", true);
        
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.OctalInteger, token.TokenType);
        Assert.AreEqual("0o7", token.Value);
    }
    
    [TestMethod]
    public void OctalLiteral_LargeValue_ShouldReturnOctalToken()
    {
        var lexer = new Lexer("0o777777", true);
        
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.OctalInteger, token.TokenType);
        Assert.AreEqual("0o777777", token.Value);
    }
    
    [TestMethod]
    public void NumberFormats_InArithmeticExpression_ShouldTokenizeCorrectly()
    {
        var lexer = new Lexer("0xFF + 0b101 - 0o77", true);
        
        // Skip None token
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        // First hex token
        token = lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, token.TokenType);
        Assert.AreEqual("0xFF", token.Value);
        
        // Plus operator
        token = lexer.Next();
        Assert.AreEqual(TokenType.Plus, token.TokenType);
        
        // Binary token
        token = lexer.Next();
        Assert.AreEqual(TokenType.BinaryInteger, token.TokenType);
        Assert.AreEqual("0b101", token.Value);
        
        // Minus operator
        token = lexer.Next();
        Assert.AreEqual(TokenType.Hyphen, token.TokenType);
        
        // Octal token
        token = lexer.Next();
        Assert.AreEqual(TokenType.OctalInteger, token.TokenType);
        Assert.AreEqual("0o77", token.Value);
    }
    
    [TestMethod]
    public void NumberFormats_ZeroValues_ShouldTokenizeCorrectly()
    {
        var lexer = new Lexer("0x0 0b0 0o0", true);
        
        // Skip None token
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        // Hex zero
        token = lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, token.TokenType);
        Assert.AreEqual("0x0", token.Value);
        
        // Skip whitespace
        token = lexer.Next();
        Assert.AreEqual(TokenType.WhiteSpace, token.TokenType);
        
        // Binary zero
        token = lexer.Next();
        Assert.AreEqual(TokenType.BinaryInteger, token.TokenType);
        Assert.AreEqual("0b0", token.Value);
        
        // Skip whitespace
        token = lexer.Next();
        Assert.AreEqual(TokenType.WhiteSpace, token.TokenType);
        
        // Octal zero
        token = lexer.Next();
        Assert.AreEqual(TokenType.OctalInteger, token.TokenType);
        Assert.AreEqual("0o0", token.Value);
    }
}