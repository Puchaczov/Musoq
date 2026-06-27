using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class LexerNullCoalescingTests
{
    [TestMethod]
    public void NullCoalescingOperator_ShouldReturnNullCoalescingTokenBeforeQuestionMark()
    {
        var lexer = new Lexer("?? ?", true);

        var token = lexer.Next();
        Assert.AreEqual(TokenType.NullCoalescing, token.TokenType);
        Assert.AreEqual("??", token.Value);

        token = lexer.Next();
        Assert.AreEqual(TokenType.QuestionMark, token.TokenType);
        Assert.AreEqual("?", token.Value);
    }
}