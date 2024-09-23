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
}