using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

public partial class LexerSchemaTests
{
    #region Complex Schema Tokenization

    [TestMethod]
    public void SimpleBinarySchema_ShouldTokenizeCorrectly()
    {
        var lexer = new Lexer("binary Header { Magic: int le, Version: short le }", true) { IsSchemaContext = true };

        lexer.Next();
        Assert.AreEqual(TokenType.Binary, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Identifier, lexer.Current().TokenType);
        Assert.AreEqual("Header", lexer.Current().Value);

        lexer.Next();
        Assert.AreEqual(TokenType.LBracket, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Identifier, lexer.Current().TokenType);
        Assert.AreEqual("Magic", lexer.Current().Value);

        lexer.Next();
        Assert.AreEqual(TokenType.Colon, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.IntType, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.LittleEndian, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Comma, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Identifier, lexer.Current().TokenType);
        Assert.AreEqual("Version", lexer.Current().Value);

        lexer.Next();
        Assert.AreEqual(TokenType.Colon, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.ShortType, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.LittleEndian, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.RBracket, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.EndOfFile, lexer.Current().TokenType);
    }

    [TestMethod]
    public void BinarySchemaWithStringField_ShouldTokenizeCorrectly()
    {
        var lexer = new Lexer("binary Record { Name: string[32] utf8 trim }", true) { IsSchemaContext = true };

        lexer.Next();
        Assert.AreEqual(TokenType.Binary, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Identifier, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.LBracket, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Identifier, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Colon, lexer.Current().TokenType);


        lexer.Next();
        Assert.AreEqual(TokenType.StringType, lexer.Current().TokenType, "Expected StringType token");

        lexer.Next();
        Assert.AreEqual(TokenType.LeftSquareBracket, lexer.Current().TokenType, "Expected LeftSquareBracket token");

        lexer.Next();
        Assert.AreEqual(TokenType.Integer, lexer.Current().TokenType, "Expected Integer token");

        lexer.Next();
        Assert.AreEqual(TokenType.RightSquareBracket, lexer.Current().TokenType, "Expected RightSquareBracket token");

        lexer.Next();
        Assert.AreEqual(TokenType.Utf8, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Trim, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.RBracket, lexer.Current().TokenType);
    }

    [TestMethod]
    public void BinarySchemaWithByteArray_ShouldTokenizeCorrectly()
    {
        var lexer = new Lexer("binary Data { Length: int le, Payload: byte[Length] }", true) { IsSchemaContext = true };

        lexer.Next();
        Assert.AreEqual(TokenType.Binary, lexer.Current().TokenType);

        lexer.Next();
        lexer.Next();
        lexer.Next();
        lexer.Next();
        lexer.Next();
        Assert.AreEqual(TokenType.IntType, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.LittleEndian, lexer.Current().TokenType);

        lexer.Next();
        lexer.Next();
        lexer.Next();


        lexer.Next();
        Assert.AreEqual(TokenType.ByteType, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.LeftSquareBracket, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Word, lexer.Current().TokenType);
        Assert.AreEqual("Length", lexer.Current().Value);

        lexer.Next();
        Assert.AreEqual(TokenType.RightSquareBracket, lexer.Current().TokenType);
    }

    [TestMethod]
    public void TextSchema_ShouldTokenizeCorrectly()
    {
        var lexer = new Lexer("text LogEntry { Timestamp: between '[' ']' }", true) { IsSchemaContext = true };

        lexer.Next();
        Assert.AreEqual(TokenType.Text, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Identifier, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.LBracket, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Identifier, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Colon, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Between, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.StringLiteral, lexer.Current().TokenType);
        Assert.AreEqual("[", lexer.Current().Value);

        lexer.Next();
        Assert.AreEqual(TokenType.StringLiteral, lexer.Current().TokenType);
        Assert.AreEqual("]", lexer.Current().Value);

        lexer.Next();
        Assert.AreEqual(TokenType.RBracket, lexer.Current().TokenType);
    }

    [TestMethod]
    public void BinarySchemaWithCheck_ShouldTokenizeCorrectly()
    {
        var lexer = new Lexer("binary Header { Magic: int le check Magic = 0xDEADBEEF }", true) { IsSchemaContext = true };

        lexer.Next();
        lexer.Next();
        lexer.Next();
        lexer.Next();
        lexer.Next();
        lexer.Next();
        lexer.Next();
        lexer.Next();
        Assert.AreEqual(TokenType.Check, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Identifier, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Equality, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.HexadecimalInteger, lexer.Current().TokenType);
    }

    [TestMethod]
    public void BinarySchemaWithAt_ShouldTokenizeCorrectly()
    {
        var lexer = new Lexer("binary PeHeader { DosMagic: string[2] ascii at 0 }", true) { IsSchemaContext = true };

        lexer.Next();
        lexer.Next();
        lexer.Next();
        lexer.Next();
        lexer.Next();


        lexer.Next();
        Assert.AreEqual(TokenType.StringType, lexer.Current().TokenType, "Expected StringType token");

        lexer.Next();
        Assert.AreEqual(TokenType.LeftSquareBracket, lexer.Current().TokenType, "Expected LeftSquareBracket token");

        lexer.Next();
        Assert.AreEqual(TokenType.Integer, lexer.Current().TokenType, "Expected Integer token");

        lexer.Next();
        Assert.AreEqual(TokenType.RightSquareBracket, lexer.Current().TokenType, "Expected RightSquareBracket token");

        lexer.Next();
        Assert.AreEqual(TokenType.Ascii, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.At, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Integer, lexer.Current().TokenType);
    }

    [TestMethod]
    public void SchemaWithExtends_ShouldTokenizeCorrectly()
    {
        var lexer = new Lexer("binary TextMessage extends BaseMessage { Content: string[Length] utf8 }", true) { IsSchemaContext = true };

        lexer.Next();
        Assert.AreEqual(TokenType.Binary, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Identifier, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Extends, lexer.Current().TokenType);

        lexer.Next();
        Assert.AreEqual(TokenType.Identifier, lexer.Current().TokenType);
    }

    #endregion
}
