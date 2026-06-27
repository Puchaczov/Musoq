using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

public partial class TokenFactoryTests
{
    [TestMethod]
    public void Create_SemiJoinToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.SemiJoin, 0, "semi join");

        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<SemiJoinToken>(token);
    }

    [TestMethod]
    public void Create_AntiJoinToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.AntiJoin, 0, "anti join");

        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<AntiJoinToken>(token);
    }

    [TestMethod]
    public void Create_CrossJoinToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.CrossJoin, 0, "cross join");

        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<CrossJoinToken>(token);
    }
}