using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class LexerSignificantContextTests
{
    [TestMethod]
    [DataRow("2 /* gap */ -1", true)]
    [DataRow("2 -- gap\n -1", true)]
    [DataRow("2 /* gap */ -1", false)]
    [DataRow("2 -- gap\n -1", false)]
    public void TriviaAfterOperand_ShouldRetainBinaryMinus(string source, bool skipTrivia)
    {
        var tokens = SignificantTokens(new Lexer(source, skipTrivia));

        CollectionAssert.AreEqual(new[] { TokenType.Integer, TokenType.Hyphen, TokenType.Integer }, tokens.Select(token => token.TokenType).ToArray());
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TriviaAfterParameter_ShouldRetainIndexDelimiters(bool skipTrivia)
    {
        var tokens = SignificantTokens(new Lexer("$items /* gap */ [0]", skipTrivia));

        CollectionAssert.AreEqual(new[] { TokenType.ParameterReference, TokenType.LeftSquareBracket, TokenType.Integer, TokenType.RightSquareBracket }, tokens.Select(token => token.TokenType).ToArray());
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TriviaAfterDot_ShouldRetainPropertyClassification(bool skipTrivia)
    {
        var tokens = SignificantTokens(new Lexer("#some. /* gap */ table", skipTrivia));

        Assert.AreEqual(TokenType.Property, tokens[^1].TokenType);
    }

    [TestMethod]
    public void ObservationCallbackPeek_ShouldUseTheObservedTokenContext()
    {
        var peeks = new List<Token>();
        Lexer? lexer = null;
        lexer = new Lexer("#some. /* gap */ table with", true, false, token => PeekAfterDot(lexer!, token, peeks));

        var tokens = SignificantTokens(lexer);

        Assert.AreEqual(TokenType.Property, peeks.Single().TokenType);
        Assert.AreEqual(TokenType.Property, tokens[2].TokenType);
    }

    private static Token[] SignificantTokens(ILexer lexer)
    {
        var tokens = new List<Token>();
        for (var token = lexer.Next(); token.TokenType != TokenType.EndOfFile; token = lexer.Next())
            if (token.TokenType is not (TokenType.WhiteSpace or TokenType.Comment))
                tokens.Add(token);
        return tokens.ToArray();
    }

    private static void PeekAfterDot(Lexer lexer, Token token, List<Token> peeks)
    {
        if (token.TokenType == TokenType.Dot)
            peeks.Add(lexer.Peek());
    }
}
