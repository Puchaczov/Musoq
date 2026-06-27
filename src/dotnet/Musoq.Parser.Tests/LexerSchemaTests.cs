using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for lexer recognition of Interpretation Schema tokens.
///     These tokens are used for binary and text schema definitions.
/// </summary>
[TestClass]
public partial class LexerSchemaTests
{
    #region Colon Separator

    [TestMethod]
    public void Colon_ShouldReturnColonToken()
    {
        var lexer = new Lexer(":", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Colon, token.TokenType);
    }

    #endregion

    #region Schema Inheritance

    [TestMethod]
    public void Extends_ShouldReturnExtendsToken()
    {
        var lexer = new Lexer("extends", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Extends, token.TokenType);
    }

    #endregion

    #region Schema Keywords

    [TestMethod]
    public void Binary_ShouldReturnBinaryToken()
    {
        var lexer = new Lexer("binary", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Binary, token.TokenType);
        Assert.AreEqual("binary", token.Value);
    }

    [TestMethod]
    public void Binary_CaseInsensitive_ShouldReturnBinaryToken()
    {
        var lexer = new Lexer("BINARY", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Binary, token.TokenType);
    }

    [TestMethod]
    public void Text_ShouldReturnTextToken()
    {
        var lexer = new Lexer("text", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Text, token.TokenType);
        Assert.AreEqual("text", token.Value);
    }

    #endregion

    #region Endianness

    [TestMethod]
    public void LittleEndian_ShouldReturnLittleEndianToken()
    {
        var lexer = new Lexer("le", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.LittleEndian, token.TokenType);
        Assert.AreEqual("le", token.Value);
    }

    [TestMethod]
    public void BigEndian_ShouldReturnBigEndianToken()
    {
        var lexer = new Lexer("be", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.BigEndian, token.TokenType);
        Assert.AreEqual("be", token.Value);
    }

    #endregion

    #region Primitive Types

    [TestMethod]
    public void ByteType_ShouldReturnByteTypeToken()
    {
        var lexer = new Lexer("byte", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.ByteType, token.TokenType);
    }

    [TestMethod]
    public void SByteType_ShouldReturnSByteTypeToken()
    {
        var lexer = new Lexer("sbyte", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.SByteType, token.TokenType);
    }

    [TestMethod]
    public void ShortType_ShouldReturnShortTypeToken()
    {
        var lexer = new Lexer("short", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.ShortType, token.TokenType);
    }

    [TestMethod]
    public void UShortType_ShouldReturnUShortTypeToken()
    {
        var lexer = new Lexer("ushort", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.UShortType, token.TokenType);
    }

    [TestMethod]
    public void IntType_ShouldReturnIntTypeToken()
    {
        var lexer = new Lexer("int", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.IntType, token.TokenType);
    }

    [TestMethod]
    public void UIntType_ShouldReturnUIntTypeToken()
    {
        var lexer = new Lexer("uint", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.UIntType, token.TokenType);
    }

    [TestMethod]
    public void LongType_ShouldReturnLongTypeToken()
    {
        var lexer = new Lexer("long", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.LongType, token.TokenType);
    }

    [TestMethod]
    public void ULongType_ShouldReturnULongTypeToken()
    {
        var lexer = new Lexer("ulong", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.ULongType, token.TokenType);
    }

    [TestMethod]
    public void FloatType_ShouldReturnFloatTypeToken()
    {
        var lexer = new Lexer("float", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.FloatType, token.TokenType);
    }

    [TestMethod]
    public void DoubleType_ShouldReturnDoubleTypeToken()
    {
        var lexer = new Lexer("double", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.DoubleType, token.TokenType);
    }

    #endregion

    #region Array and Bit Types

    [TestMethod]
    public void BitsType_ShouldReturnBitsTypeToken()
    {
        var lexer = new Lexer("bits", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.BitsType, token.TokenType);
    }

    [TestMethod]
    public void Align_ShouldReturnAlignToken()
    {
        var lexer = new Lexer("align", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Align, token.TokenType);
    }

    [TestMethod]
    public void StringType_ShouldReturnStringTypeToken()
    {
        var lexer = new Lexer("string", true) { IsSchemaContext = true };
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.StringType, token.TokenType);
    }

    #endregion
}
