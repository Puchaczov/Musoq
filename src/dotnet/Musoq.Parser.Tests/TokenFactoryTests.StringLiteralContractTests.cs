using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

public partial class TokenFactoryTests
{
    [TestMethod]
    public void CreateStringLiteralToken_PreservesCallerSuppliedSourceSpan()
    {
        var token = (StringLiteralToken)TokenFactory.CreateStringLiteralToken(4, "hello", 7);

        Assert.AreEqual("hello", token.Value);
        Assert.AreEqual(5, token.Span.Start);
        Assert.AreEqual(7, token.Span.Length);
    }
}
