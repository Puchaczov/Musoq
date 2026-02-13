using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public class LexerTests
{
    [TestMethod]
    public void CheckEmptyString_ShouldReturnStringLiteralToken()
    {
        var lexer = new Lexer("''", true);

        var token = lexer.Current();

        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual(string.Empty, token.Value);
    }

    [TestMethod]
    public void CheckTestString_ShouldReturnWordToken()
    {
        var lexer = new Lexer("'test'", true);

        var token = lexer.Current();

        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();

        Assert.AreEqual(TokenType.StringLiteral, token.TokenType);
        Assert.AreEqual("test", token.Value);
    }

    [TestMethod]
    public void HexadecimalLiteral_Lowercase_ShouldReturnHexToken()
    {
        var lexer = new Lexer("0xff", true);

        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, token.TokenType);
        Assert.AreEqual("0xff", token.Value);
    }

    [TestMethod]
    public void HexadecimalLiteral_Uppercase_ShouldReturnHexToken()
    {
        var lexer = new Lexer("0xFF", true);

        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, token.TokenType);
        Assert.AreEqual("0xFF", token.Value);
    }

    [TestMethod]
    public void HexadecimalLiteral_MixedCase_ShouldReturnHexToken()
    {
        var lexer = new Lexer("0XaB", true);

        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, token.TokenType);
        Assert.AreEqual("0XaB", token.Value);
    }

    [TestMethod]
    public void HexadecimalLiteral_SingleDigit_ShouldReturnHexToken()
    {
        var lexer = new Lexer("0xA", true);

        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, token.TokenType);
        Assert.AreEqual("0xA", token.Value);
    }

    [TestMethod]
    public void HexadecimalLiteral_LongValue_ShouldReturnHexToken()
    {
        var lexer = new Lexer("0xDEADBEEF", true);

        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, token.TokenType);
        Assert.AreEqual("0xDEADBEEF", token.Value);
    }

    [TestMethod]
    public void BinaryLiteral_Lowercase_ShouldReturnBinaryToken()
    {
        var lexer = new Lexer("0b101", true);

        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.BinaryInteger, token.TokenType);
        Assert.AreEqual("0b101", token.Value);
    }

    [TestMethod]
    public void BinaryLiteral_Uppercase_ShouldReturnBinaryToken()
    {
        var lexer = new Lexer("0B111", true);

        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.BinaryInteger, token.TokenType);
        Assert.AreEqual("0B111", token.Value);
    }

    [TestMethod]
    public void BinaryLiteral_SingleBit_ShouldReturnBinaryToken()
    {
        var lexer = new Lexer("0b1", true);

        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.BinaryInteger, token.TokenType);
        Assert.AreEqual("0b1", token.Value);
    }

    [TestMethod]
    public void BinaryLiteral_LongValue_ShouldReturnBinaryToken()
    {
        var lexer = new Lexer("0b11111111", true);

        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.BinaryInteger, token.TokenType);
        Assert.AreEqual("0b11111111", token.Value);
    }

    [TestMethod]
    public void OctalLiteral_Lowercase_ShouldReturnOctalToken()
    {
        var lexer = new Lexer("0o77", true);

        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.OctalInteger, token.TokenType);
        Assert.AreEqual("0o77", token.Value);
    }

    [TestMethod]
    public void OctalLiteral_Uppercase_ShouldReturnOctalToken()
    {
        var lexer = new Lexer("0O123", true);

        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.OctalInteger, token.TokenType);
        Assert.AreEqual("0O123", token.Value);
    }

    [TestMethod]
    public void OctalLiteral_SingleDigit_ShouldReturnOctalToken()
    {
        var lexer = new Lexer("0o7", true);

        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.OctalInteger, token.TokenType);
        Assert.AreEqual("0o7", token.Value);
    }

    [TestMethod]
    public void OctalLiteral_LargeValue_ShouldReturnOctalToken()
    {
        var lexer = new Lexer("0o777777", true);

        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.OctalInteger, token.TokenType);
        Assert.AreEqual("0o777777", token.Value);
    }

    [TestMethod]
    public void NumberFormats_InArithmeticExpression_ShouldTokenizeCorrectly()
    {
        var lexer = new Lexer("0xFF + 0b101 - 0o77", true);

        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, token.TokenType);
        Assert.AreEqual("0xFF", token.Value);

        token = lexer.Next();
        Assert.AreEqual(TokenType.Plus, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.BinaryInteger, token.TokenType);
        Assert.AreEqual("0b101", token.Value);

        token = lexer.Next();
        Assert.AreEqual(TokenType.Hyphen, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.OctalInteger, token.TokenType);
        Assert.AreEqual("0o77", token.Value);
    }

    [TestMethod]
    public void NumberFormats_ZeroValues_ShouldTokenizeCorrectly()
    {
        var lexer = new Lexer("0x0 0b0 0o0", true);

        var token = lexer.Current();
        Assert.AreEqual(TokenType.None, token.TokenType);

        token = lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, token.TokenType);
        Assert.AreEqual("0x0", token.Value);

        token = lexer.Next();
        Assert.AreEqual(TokenType.BinaryInteger, token.TokenType);
        Assert.AreEqual("0b0", token.Value);

        token = lexer.Next();
        Assert.AreEqual(TokenType.OctalInteger, token.TokenType);
        Assert.AreEqual("0o0", token.Value);
    }

    [TestMethod]
    public void ComplexNestedExpression_ShouldTokenizeCorrectly()
    {
        var query =
            "select (((((1 + (6 * 2)) + 4 + 4 + 4 + 2 + 8 + 1 + 4 + 1 + 1 + 1 + 1 + 1 + 1 + 32 + 1 + 4 + 4 + 4 + 1 + 4 + 4 + 1 + (6 * 4) + 1 + 1 + 1 + 1 + 32 + 1) + 4) + 1 + 1) + 4 + 4) + 4 + 4 + 4 from #some.a()";
        var lexer = new Lexer(query, true);

        var tokenCount = 0;
        while (lexer.Current().TokenType != TokenType.EndOfFile)
        {
            tokenCount++;
            lexer.Next();
        }

        Assert.IsGreaterThan(0, tokenCount, "Should have tokenized multiple tokens");
    }

    [TestMethod]
    public void VeryLongArithmeticChain_ShouldTokenizeQuickly()
    {
        var numbers = string.Join(" + ", Enumerable.Range(1, 50).Select(i => i.ToString()));
        var query = $"select {numbers} from #a.b()";
        var lexer = new Lexer(query, true);

        var sw = Stopwatch.StartNew();
        var tokenCount = 0;
        while (lexer.Current().TokenType != TokenType.EndOfFile)
        {
            tokenCount++;
            lexer.Next();
        }

        sw.Stop();

        Assert.IsGreaterThan(100, tokenCount, "Should have many tokens");
        Assert.IsLessThan(100, sw.ElapsedMilliseconds, $"Lexer should be fast but took {sw.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public void DeeplyNestedParentheses_ShouldTokenize()
    {
        var expr = "((((((1 + 2))))))";
        var query = $"select {expr} from #a.b()";
        var lexer = new Lexer(query, true);

        var leftParenCount = 0;
        var rightParenCount = 0;

        while (lexer.Current().TokenType != TokenType.EndOfFile)
        {
            var token = lexer.Current();
            if (token.TokenType == TokenType.LeftParenthesis) leftParenCount++;
            if (token.TokenType == TokenType.RightParenthesis) rightParenCount++;
            lexer.Next();
        }

        Assert.AreEqual(7, leftParenCount, "Should have 7 left parentheses");
        Assert.AreEqual(7, rightParenCount, "Should have 7 right parentheses");
    }

    [TestMethod]
    public void MixedOperatorsAndParentheses_ShouldTokenize()
    {
        var query = "select (1 + 2) * (3 - 4) / (5 + 6) - (7 * 8) + (9 / 10) from #a.b()";
        var lexer = new Lexer(query, true);

        var operatorCount = 0;
        while (lexer.Current().TokenType != TokenType.EndOfFile)
        {
            var token = lexer.Current();
            if (token.TokenType == TokenType.Plus ||
                token.TokenType == TokenType.Hyphen ||
                token.TokenType == TokenType.Star ||
                token.TokenType == TokenType.FSlash)
                operatorCount++;
            lexer.Next();
        }

        Assert.AreEqual(9, operatorCount, "Should have 9 arithmetic operators");
    }

    [TestMethod]
    public void BitwiseAnd_ShouldReturnAmpersandToken()
    {
        var lexer = new Lexer("a & b", true);

        var tokens = new List<Token>();
        var token = lexer.Next();
        while (token.TokenType != TokenType.EndOfFile)
        {
            tokens.Add(token);
            token = lexer.Next();
        }


        var tokenString = string.Join(", ", tokens.Select(t => $"{t.TokenType}:{t.Value}"));


        var nonWhitespaceTokens = tokens.Where(t => t.TokenType != TokenType.WhiteSpace).ToList();
        Assert.HasCount(3, nonWhitespaceTokens, $"Expected 3 non-whitespace tokens, got: {tokenString}");


        Assert.AreEqual(TokenType.Ampersand, nonWhitespaceTokens[1].TokenType,
            $"Expected Ampersand but got {nonWhitespaceTokens[1].TokenType}:{nonWhitespaceTokens[1].Value}. All tokens: {tokenString}");
    }

    [TestMethod]
    public void BitwiseOperators_InParentheses_ShouldTokenizeCorrectly()
    {
        var lexer = new Lexer("(Flags & 0x01)", true);

        var tokens = new List<Token>();
        var token = lexer.Next();
        while (token.TokenType != TokenType.EndOfFile)
        {
            if (token.TokenType != TokenType.WhiteSpace)
                tokens.Add(token);
            token = lexer.Next();
        }


        Assert.HasCount(5, tokens,
            $"Expected 5 tokens but got {tokens.Count}: {string.Join(", ", tokens.Select(t => $"{t.TokenType}:{t.Value}"))}");
        Assert.AreEqual(TokenType.LeftParenthesis, tokens[0].TokenType);

        Assert.IsTrue(tokens[1].TokenType == TokenType.Word || tokens[1].TokenType == TokenType.Identifier,
            $"Expected Word or Identifier but got {tokens[1].TokenType}:{tokens[1].Value}");
        Assert.AreEqual(TokenType.Ampersand, tokens[2].TokenType,
            $"Expected Ampersand but got {tokens[2].TokenType}:{tokens[2].Value}");
        Assert.AreEqual(TokenType.HexadecimalInteger, tokens[3].TokenType);
        Assert.AreEqual(TokenType.RightParenthesis, tokens[4].TokenType);
    }

    [TestMethod]
    public void SchemaContext_GenericWithArray_ShouldTokenizeCorrectly()
    {
        var lexer = new Lexer("Wrapper<Data>[5]", true);
        lexer.IsSchemaContext = true;

        var tokens = new List<Token>();
        var token = lexer.Next();
        while (token.TokenType != TokenType.EndOfFile)
        {
            tokens.Add(token);
            token = lexer.Next();
        }


        var tokenStr = string.Join(", ", tokens.Select(t => $"{t.TokenType}:{t.Value}"));


        Assert.HasCount(7, tokens, $"Expected 7 tokens but got {tokens.Count}: {tokenStr}");
        Assert.IsTrue(tokens[0].TokenType == TokenType.Word || tokens[0].TokenType == TokenType.Identifier,
            $"Token 0: Expected Word or Identifier but got {tokens[0].TokenType}:{tokens[0].Value}");
        Assert.AreEqual("Wrapper", tokens[0].Value);

        Assert.AreEqual(TokenType.Less, tokens[1].TokenType,
            $"Token 1: Expected Less but got {tokens[1].TokenType}:{tokens[1].Value}");

        Assert.IsTrue(tokens[2].TokenType == TokenType.Word || tokens[2].TokenType == TokenType.Identifier,
            $"Token 2: Expected Word or Identifier but got {tokens[2].TokenType}:{tokens[2].Value}");
        Assert.AreEqual("Data", tokens[2].Value);

        Assert.AreEqual(TokenType.Greater, tokens[3].TokenType,
            $"Token 3: Expected Greater but got {tokens[3].TokenType}:{tokens[3].Value}");

        Assert.AreEqual(TokenType.LeftSquareBracket, tokens[4].TokenType,
            $"Token 4: Expected LeftSquareBracket but got {tokens[4].TokenType}:{tokens[4].Value}");

        Assert.AreEqual(TokenType.Integer, tokens[5].TokenType,
            $"Token 5: Expected Integer but got {tokens[5].TokenType}:{tokens[5].Value}");
        Assert.AreEqual("5", tokens[5].Value);

        Assert.AreEqual(TokenType.RightSquareBracket, tokens[6].TokenType,
            $"Token 6: Expected RightSquareBracket but got {tokens[6].TokenType}:{tokens[6].Value}");
    }

    [TestMethod]
    public void SchemaContext_BracketedNumber_ShouldSplit()
    {
        var lexer = new Lexer("[5]", true);
        lexer.IsSchemaContext = true;

        var tokens = new List<Token>();
        var token = lexer.Next();
        while (token.TokenType != TokenType.EndOfFile)
        {
            tokens.Add(token);
            token = lexer.Next();
        }

        var tokenStr = string.Join(", ", tokens.Select(t => $"{t.TokenType}:{t.Value}(type={t.GetType().Name})"));

        Assert.HasCount(3, tokens, $"Expected 3 tokens but got {tokens.Count}: {tokenStr}");
        Assert.AreEqual(TokenType.LeftSquareBracket, tokens[0].TokenType,
            $"Token 0: Expected LeftSquareBracket but got {tokens[0].TokenType}:{tokens[0].Value}");
        Assert.AreEqual(TokenType.Integer, tokens[1].TokenType,
            $"Token 1: Expected Integer but got {tokens[1].TokenType}:{tokens[1].Value}");
        Assert.AreEqual("5", tokens[1].Value);
        Assert.AreEqual(TokenType.RightSquareBracket, tokens[2].TokenType,
            $"Token 2: Expected RightSquareBracket but got {tokens[2].TokenType}:{tokens[2].Value}");
    }

    [TestMethod]
    public void ArithmeticExpression_SubtractionWithSpaces_ShouldTokenize()
    {
        var lexer = new Lexer("Total - HeaderSize", true);

        var tokens = new List<Token>();
        var token = lexer.Next();
        while (token.TokenType != TokenType.EndOfFile)
        {
            tokens.Add(token);
            token = lexer.Next();
        }

        var tokenStr = string.Join(", ", tokens.Select(t => $"{t.TokenType}:{t.Value}"));


        Assert.HasCount(3, tokens, $"Expected 3 tokens but got {tokens.Count}: {tokenStr}");
        Assert.AreEqual(TokenType.Identifier, tokens[0].TokenType, $"Token 0: Expected Identifier but got {tokenStr}");
        Assert.AreEqual("Total", tokens[0].Value);
        Assert.AreEqual(TokenType.Hyphen, tokens[1].TokenType, $"Token 1: Expected Hyphen but got {tokenStr}");
        Assert.AreEqual(TokenType.Identifier, tokens[2].TokenType, $"Token 2: Expected Identifier but got {tokenStr}");
        Assert.AreEqual("HeaderSize", tokens[2].Value);
    }

    [TestMethod]
    public void BracketedArithmeticExpression_WithoutSchemaContext_TokenizesAsIdentifier()
    {
        var lexer = new Lexer("[Total - HeaderSize]", true);

        var tokens = new List<Token>();
        var token = lexer.Next();
        while (token.TokenType != TokenType.EndOfFile)
        {
            tokens.Add(token);
            token = lexer.Next();
        }

        var tokenStr = string.Join(", ", tokens.Select(t => $"{t.TokenType}:{t.Value}"));


        Assert.HasCount(1, tokens, $"Expected 1 token but got {tokens.Count}: {tokenStr}");
    }

    [TestMethod]
    public void KeyAccessWithArithmetic_InSchemaContext_ShouldSplit()
    {
        var lexer = new Lexer("byte[Total - HeaderSize]", true);
        lexer.IsSchemaContext = true;

        var tokens = new List<Token>();
        var token = lexer.Next();
        while (token.TokenType != TokenType.EndOfFile)
        {
            tokens.Add(token);
            token = lexer.Next();
        }

        var tokenStr = string.Join(", ", tokens.Select(t => $"{t.TokenType}:{t.Value}"));


        Assert.HasCount(6, tokens, $"Expected 6 tokens but got {tokens.Count}: {tokenStr}");
    }

    [TestMethod]
    public void QuestionMark_Alone_TokenizesAsQuestionMark()
    {
        var lexer = new Lexer("?", true);
        lexer.IsSchemaContext = true;

        var tokens = new List<Token>();
        var token = lexer.Next();
        while (token.TokenType != TokenType.EndOfFile)
        {
            tokens.Add(token);
            token = lexer.Next();
        }

        var tokenStr = string.Join(", ", tokens.Select(t => $"{t.TokenType}:{t.Value}"));


        Assert.HasCount(1, tokens, $"Expected 1 token but got {tokens.Count}: {tokenStr}");
        Assert.AreEqual(TokenType.QuestionMark, tokens[0].TokenType, $"Token 0: Expected QuestionMark but got {tokenStr}");
        Assert.AreEqual("?", tokens[0].Value, $"Token 0: Expected value '?' but got '{tokens[0].Value}'");
    }

    [TestMethod]
    public void QuestionMark_InSchemaContext_TokenizesAsQuestionMark()
    {
        var lexer = new Lexer("whitespace?", true);
        lexer.IsSchemaContext = true;

        var tokens = new List<Token>();
        var token = lexer.Next();
        while (token.TokenType != TokenType.EndOfFile)
        {
            tokens.Add(token);
            token = lexer.Next();
        }

        var tokenStr = string.Join(", ", tokens.Select(t => $"{t.TokenType}:{t.Value}"));


        Assert.HasCount(2, tokens, $"Expected 2 tokens but got {tokens.Count}: {tokenStr}");
        Assert.AreEqual(TokenType.Whitespace, tokens[0].TokenType, $"Token 0: Expected Whitespace but got {tokenStr}");
        Assert.AreEqual(TokenType.QuestionMark, tokens[1].TokenType, $"Token 1: Expected QuestionMark but got {tokenStr}");
        Assert.AreEqual("?", tokens[1].Value, $"Token 1: Expected value '?' but got '{tokens[1].Value}'");
    }

    [TestMethod]
    public void EndKeyword_AfterDot_ShouldBePropertyToken()
    {
        var lexer = new Lexer("l.End.X", true);

        var token = lexer.Next();
        Assert.AreEqual(TokenType.Identifier, token.TokenType,
            $"First token should be Identifier, got {token.TokenType}");
        Assert.AreEqual("l", token.Value);

        token = lexer.Next();
        Assert.AreEqual(TokenType.Dot, token.TokenType, $"Second token should be Dot, got {token.TokenType}");

        token = lexer.Next();
        Assert.AreEqual(TokenType.Property, token.TokenType,
            $"Third token 'End' should be Property (not End keyword), got {token.TokenType}:{token.Value}");
        Assert.AreEqual("End", token.Value);

        token = lexer.Next();
        Assert.AreEqual(TokenType.Dot, token.TokenType, $"Fourth token should be Dot, got {token.TokenType}");

        token = lexer.Next();
        Assert.AreEqual(TokenType.Property, token.TokenType,
            $"Fifth token 'X' should be Property, got {token.TokenType}");
        Assert.AreEqual("X", token.Value);
    }
}
