using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public class DoubleColonLexerTests
{
    [TestMethod]
    public void DoubleColon_ShouldReturnDoubleColonToken()
    {
        var lexer = new Lexer("Column::Int32", true);

        Assert.AreEqual(TokenType.Identifier, lexer.Next().TokenType);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.DoubleColon, token.TokenType);
        Assert.AreEqual("::", token.Value);
    }
}
