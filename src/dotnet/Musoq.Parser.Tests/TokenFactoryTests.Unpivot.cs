using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public class TokenFactoryTestsUnpivot
{
    [TestMethod]
    public void Create_UnpivotToken_ReturnsCorrectType()
    {
        var token = TokenFactory.Create(TokenType.Unpivot, 0, "unpivot");

        Assert.IsInstanceOfType<UnpivotToken>(token);
    }
}
