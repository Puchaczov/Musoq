using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

public partial class TokenFactoryTests
{
    #region Special Method Tests

    [TestMethod]
    public void CreateSchemaKeywordToken_ReturnsSchemaKeywordToken()
    {
        var token = TokenFactory.CreateSchemaKeywordToken(TokenType.Word, 0, "schema");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<SchemaKeywordToken>(token);
    }

    [TestMethod]
    public void CreateStringLiteralToken_ReturnsStringLiteralToken()
    {
        var token = TokenFactory.CreateStringLiteralToken(0, "hello", 7);
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<StringLiteralToken>(token);
    }

    [TestMethod]
    public void CreateEmptyWordToken_ReturnsWordToken()
    {
        var token = TokenFactory.CreateEmptyWordToken(0);
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<WordToken>(token);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Create_UnknownTokenType_ReturnsNull()
    {
        var token = TokenFactory.Create(TokenType.EndOfFile, 0, "");
        Assert.IsNull(token);
    }

    [TestMethod]
    public void Create_SingleDigitInteger_ReturnsIntegerToken()
    {
        var token = TokenFactory.Create(TokenType.Integer, 0, "5");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<IntegerToken>(token);
    }

    [TestMethod]
    public void Create_IntegerWithLongAbbreviation_ReturnsIntegerToken()
    {
        var token = TokenFactory.Create(TokenType.Integer, 0, "100MB");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<IntegerToken>(token);
    }

    #endregion
}
