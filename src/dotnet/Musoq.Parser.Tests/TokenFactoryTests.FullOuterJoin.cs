using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

public partial class TokenFactoryTests
{
    [TestMethod]
    public void Create_OuterJoinToken_FullJoin_ReturnsCorrectType()
    {
        var regex = new Regex(@"(left|right|full)(?:\s+outer)?\s+join", RegexOptions.IgnoreCase);
        var match = regex.Match("full outer join");
        var token = TokenFactory.Create(TokenType.OuterJoin, 0, "full outer join", match);
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<OuterJoinToken>(token);
        var outerJoin = (OuterJoinToken)token;
        Assert.AreEqual(OuterJoinType.Full, outerJoin.Type);
    }

    [TestMethod]
    public void Create_OuterJoinToken_FullJoinWithoutOuter_ReturnsCorrectType()
    {
        var regex = new Regex(@"(left|right|full)(?:\s+outer)?\s+join", RegexOptions.IgnoreCase);
        var match = regex.Match("full join");
        var token = TokenFactory.Create(TokenType.OuterJoin, 0, "full join", match);
        Assert.IsNotNull(token);
        Assert.IsInstanceOfType<OuterJoinToken>(token);
        var outerJoin = (OuterJoinToken)token;
        Assert.AreEqual(OuterJoinType.Full, outerJoin.Type);
    }
}
