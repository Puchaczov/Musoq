using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

public partial class TokenFactoryTests
{
    [TestMethod]
    public void Create_PivotToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Pivot, 0, "pivot");
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<PivotToken>(token);
    }
}
