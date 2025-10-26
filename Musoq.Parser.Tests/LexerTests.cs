using System.Linq;
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
        
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, token.TokenType);
        Assert.AreEqual("0xFF", token.Value);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.Plus, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.BinaryInteger, token.TokenType);
        Assert.AreEqual("0b101", token.Value);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.Hyphen, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.OctalInteger, token.TokenType);
        Assert.AreEqual("0o77", token.Value);
    }
    
    [TestMethod]
    public void NumberFormats_ZeroValues_ShouldTokenizeCorrectly()
    {
        var lexer = new Lexer("0x0 0b0 0o0", true);
        
        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, token.TokenType);
        Assert.AreEqual("0x0", token.Value);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.BinaryInteger, token.TokenType);
        Assert.AreEqual("0b0", token.Value);
        
        token = lexer.Next();
        Assert.AreEqual(TokenType.OctalInteger, token.TokenType);
        Assert.AreEqual("0o0", token.Value);
    }
    
    [TestMethod]
    public void ComplexNestedExpression_ShouldTokenizeCorrectly()
    {
        var query = "select (((((1 + (6 * 2)) + 4 + 4 + 4 + 2 + 8 + 1 + 4 + 1 + 1 + 1 + 1 + 1 + 1 + 32 + 1 + 4 + 4 + 4 + 1 + 4 + 4 + 1 + (6 * 4) + 1 + 1 + 1 + 1 + 32 + 1) + 4) + 1 + 1) + 4 + 4) + 4 + 4 + 4 from #some.a()";
        var lexer = new Lexer(query, true);
        
        int tokenCount = 0;
        while (lexer.Current().TokenType != TokenType.EndOfFile)
        {
            tokenCount++;
            lexer.Next();
        }
        
        Assert.IsGreaterThan(0, tokenCount, "Should have tokenized multiple tokens");
    }
    
    [TestMethod]
    public void VeryLongArithmeticChain_ShouldTokenizeQuickly()
    {
        var numbers = string.Join(" + ", System.Linq.Enumerable.Range(1, 50).Select(i => i.ToString()));
        var query = $"select {numbers} from #a.b()";
        var lexer = new Lexer(query, true);
        
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int tokenCount = 0;
        while (lexer.Current().TokenType != TokenType.EndOfFile)
        {
            tokenCount++;
            lexer.Next();
        }
        sw.Stop();
        
        Assert.IsGreaterThan(100, tokenCount, "Should have many tokens");
        Assert.IsLessThan(100, sw.ElapsedMilliseconds, $"Lexer should be fast but took {sw.ElapsedMilliseconds}ms");
    }
    
    [TestMethod]
    public void DeeplyNestedParentheses_ShouldTokenize()
    {
        var expr = "((((((1 + 2))))))";
        var query = $"select {expr} from #a.b()";
        var lexer = new Lexer(query, true);
        
        int leftParenCount = 0;
        int rightParenCount = 0;
        
        while (lexer.Current().TokenType != TokenType.EndOfFile)
        {
            var token = lexer.Current();
            if (token.TokenType == TokenType.LeftParenthesis) leftParenCount++;
            if (token.TokenType == TokenType.RightParenthesis) rightParenCount++;
            lexer.Next();
        }
        
        Assert.AreEqual(7, leftParenCount, "Should have 7 left parentheses");
        Assert.AreEqual(7, rightParenCount, "Should have 7 right parentheses");
    }
    
    [TestMethod]
    public void MixedOperatorsAndParentheses_ShouldTokenize()
    {
        var query = "select (1 + 2) * (3 - 4) / (5 + 6) - (7 * 8) + (9 / 10) from #a.b()";
        var lexer = new Lexer(query, true);
        
        int operatorCount = 0;
        while (lexer.Current().TokenType != TokenType.EndOfFile)
        {
            var token = lexer.Current();
            if (token.TokenType == TokenType.Plus || 
                token.TokenType == TokenType.Hyphen ||
                token.TokenType == TokenType.Star ||
                token.TokenType == TokenType.FSlash)
            {
                operatorCount++;
            }
            lexer.Next();
        }
        
        Assert.AreEqual(9, operatorCount, "Should have 9 arithmetic operators");
    }
}