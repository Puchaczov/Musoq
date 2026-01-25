using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for lexer recognition of Interpretation Schema tokens.
///     These tokens are used for binary and text schema definitions.
/// </summary>
[TestClass]
public class LexerSchemaTests
{
    #region Colon Separator

    [TestMethod]
    public void Colon_ShouldReturnColonToken()
    {
        var lexer = new Lexer(":", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Colon, token.TokenType);
    }

    #endregion

    #region Schema Inheritance

    [TestMethod]
    public void Extends_ShouldReturnExtendsToken()
    {
        var lexer = new Lexer("extends", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Extends, token.TokenType);
    }

    #endregion

    #region Schema Keywords

    [TestMethod]
    public void Binary_ShouldReturnBinaryToken()
    {
        var lexer = new Lexer("binary", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Binary, token.TokenType);
        Assert.AreEqual("binary", token.Value);
    }

    [TestMethod]
    public void Binary_CaseInsensitive_ShouldReturnBinaryToken()
    {
        var lexer = new Lexer("BINARY", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Binary, token.TokenType);
    }

    [TestMethod]
    public void Text_ShouldReturnTextToken()
    {
        var lexer = new Lexer("text", true);
        lexer.IsSchemaContext = true;
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
        var lexer = new Lexer("le", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.LittleEndian, token.TokenType);
        Assert.AreEqual("le", token.Value);
    }

    [TestMethod]
    public void BigEndian_ShouldReturnBigEndianToken()
    {
        var lexer = new Lexer("be", true);
        lexer.IsSchemaContext = true;
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
        var lexer = new Lexer("byte", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.ByteType, token.TokenType);
    }

    [TestMethod]
    public void SByteType_ShouldReturnSByteTypeToken()
    {
        var lexer = new Lexer("sbyte", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.SByteType, token.TokenType);
    }

    [TestMethod]
    public void ShortType_ShouldReturnShortTypeToken()
    {
        var lexer = new Lexer("short", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.ShortType, token.TokenType);
    }

    [TestMethod]
    public void UShortType_ShouldReturnUShortTypeToken()
    {
        var lexer = new Lexer("ushort", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.UShortType, token.TokenType);
    }

    [TestMethod]
    public void IntType_ShouldReturnIntTypeToken()
    {
        var lexer = new Lexer("int", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.IntType, token.TokenType);
    }

    [TestMethod]
    public void UIntType_ShouldReturnUIntTypeToken()
    {
        var lexer = new Lexer("uint", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.UIntType, token.TokenType);
    }

    [TestMethod]
    public void LongType_ShouldReturnLongTypeToken()
    {
        var lexer = new Lexer("long", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.LongType, token.TokenType);
    }

    [TestMethod]
    public void ULongType_ShouldReturnULongTypeToken()
    {
        var lexer = new Lexer("ulong", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.ULongType, token.TokenType);
    }

    [TestMethod]
    public void FloatType_ShouldReturnFloatTypeToken()
    {
        var lexer = new Lexer("float", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.FloatType, token.TokenType);
    }

    [TestMethod]
    public void DoubleType_ShouldReturnDoubleTypeToken()
    {
        var lexer = new Lexer("double", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.DoubleType, token.TokenType);
    }

    #endregion

    #region Array and Bit Types

    [TestMethod]
    public void BitsType_ShouldReturnBitsTypeToken()
    {
        var lexer = new Lexer("bits", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.BitsType, token.TokenType);
    }

    [TestMethod]
    public void Align_ShouldReturnAlignToken()
    {
        var lexer = new Lexer("align", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Align, token.TokenType);
    }

    [TestMethod]
    public void StringType_ShouldReturnStringTypeToken()
    {
        var lexer = new Lexer("string", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.StringType, token.TokenType);
    }

    #endregion

    #region Encodings

    [TestMethod]
    public void Utf8_ShouldReturnUtf8Token()
    {
        var lexer = new Lexer("utf8", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Utf8, token.TokenType);
    }

    [TestMethod]
    public void Utf16Le_ShouldReturnUtf16LeToken()
    {
        var lexer = new Lexer("utf16le", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Utf16Le, token.TokenType);
    }

    [TestMethod]
    public void Utf16Be_ShouldReturnUtf16BeToken()
    {
        var lexer = new Lexer("utf16be", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Utf16Be, token.TokenType);
    }

    [TestMethod]
    public void Ascii_ShouldReturnAsciiToken()
    {
        var lexer = new Lexer("ascii", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Ascii, token.TokenType);
    }

    [TestMethod]
    public void Latin1_ShouldReturnLatin1Token()
    {
        var lexer = new Lexer("latin1", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Latin1, token.TokenType);
    }

    [TestMethod]
    public void Ebcdic_ShouldReturnEbcdicToken()
    {
        var lexer = new Lexer("ebcdic", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Ebcdic, token.TokenType);
    }

    #endregion

    #region Field Modifiers

    [TestMethod]
    public void Trim_ShouldReturnTrimToken()
    {
        var lexer = new Lexer("trim", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Trim, token.TokenType);
    }

    [TestMethod]
    public void RTrim_ShouldReturnRTrimToken()
    {
        var lexer = new Lexer("rtrim", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.RTrim, token.TokenType);
    }

    [TestMethod]
    public void LTrim_ShouldReturnLTrimToken()
    {
        var lexer = new Lexer("ltrim", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.LTrim, token.TokenType);
    }

    [TestMethod]
    public void NullTerm_ShouldReturnNullTermToken()
    {
        var lexer = new Lexer("nullterm", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.NullTerm, token.TokenType);
    }

    [TestMethod]
    public void Check_ShouldReturnCheckToken()
    {
        var lexer = new Lexer("check", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Check, token.TokenType);
    }

    [TestMethod]
    public void At_ShouldReturnAtToken()
    {
        var lexer = new Lexer("at", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.At, token.TokenType);
    }

    #endregion

    #region Text Schema Keywords

    [TestMethod]
    public void Pattern_ShouldReturnPatternToken()
    {
        var lexer = new Lexer("pattern", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Pattern, token.TokenType);
    }

    [TestMethod]
    public void Literal_ShouldReturnLiteralToken()
    {
        var lexer = new Lexer("literal", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Literal, token.TokenType);
    }

    [TestMethod]
    public void Until_ShouldReturnUntilToken()
    {
        var lexer = new Lexer("until", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Until, token.TokenType);
    }

    [TestMethod]
    public void Between_ShouldReturnBetweenToken()
    {
        var lexer = new Lexer("between", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Between, token.TokenType);
    }

    [TestMethod]
    public void Chars_ShouldReturnCharsToken()
    {
        var lexer = new Lexer("chars", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Chars, token.TokenType);
    }

    [TestMethod]
    public void Token_ShouldReturnTokenToken()
    {
        var lexer = new Lexer("token", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Token, token.TokenType);
    }

    [TestMethod]
    public void Rest_ShouldReturnRestToken()
    {
        var lexer = new Lexer("rest", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Rest, token.TokenType);
    }

    [TestMethod]
    public void Whitespace_ShouldReturnWhitespaceToken()
    {
        var lexer = new Lexer("whitespace", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Whitespace, token.TokenType);
    }

    [TestMethod]
    public void Optional_ShouldReturnOptionalToken()
    {
        var lexer = new Lexer("optional", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Optional, token.TokenType);
    }

    [TestMethod]
    public void Repeat_ShouldReturnRepeatToken()
    {
        var lexer = new Lexer("repeat", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Repeat, token.TokenType);
    }

    [TestMethod]
    public void Switch_ShouldReturnSwitchToken()
    {
        var lexer = new Lexer("switch", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Switch, token.TokenType);
    }

    [TestMethod]
    public void Nested_ShouldReturnNestedToken()
    {
        var lexer = new Lexer("nested", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Nested, token.TokenType);
    }

    [TestMethod]
    public void Escaped_ShouldReturnEscapedToken()
    {
        var lexer = new Lexer("escaped", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Escaped, token.TokenType);
    }

    [TestMethod]
    public void Greedy_ShouldReturnGreedyToken()
    {
        var lexer = new Lexer("greedy", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Greedy, token.TokenType);
    }

    [TestMethod]
    public void Lazy_ShouldReturnLazyToken()
    {
        var lexer = new Lexer("lazy", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Lazy, token.TokenType);
    }

    [TestMethod]
    public void Lower_ShouldReturnLowerToken()
    {
        var lexer = new Lexer("lower", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Lower, token.TokenType);
    }

    [TestMethod]
    public void Upper_ShouldReturnUpperToken()
    {
        var lexer = new Lexer("upper", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Upper, token.TokenType);
    }

    [TestMethod]
    public void Capture_ShouldReturnCaptureToken()
    {
        var lexer = new Lexer("capture", true);
        lexer.IsSchemaContext = true;
        lexer.Next();
        var token = lexer.Current();

        Assert.AreEqual(TokenType.Capture, token.TokenType);
    }

    #endregion

    #region Complex Schema Tokenization

    [TestMethod]
    public void SimpleBinarySchema_ShouldTokenizeCorrectly()
    {
        var lexer = new Lexer("binary Header { Magic: int le, Version: short le }", true);
        lexer.IsSchemaContext = true;

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
        var lexer = new Lexer("binary Record { Name: string[32] utf8 trim }", true);
        lexer.IsSchemaContext = true;

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
        var lexer = new Lexer("binary Data { Length: int le, Payload: byte[Length] }", true);
        lexer.IsSchemaContext = true;

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
        var lexer = new Lexer("text LogEntry { Timestamp: between '[' ']' }", true);
        lexer.IsSchemaContext = true;

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
        var lexer = new Lexer("binary Header { Magic: int le check Magic = 0xDEADBEEF }", true);
        lexer.IsSchemaContext = true;

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
        var lexer = new Lexer("binary PeHeader { DosMagic: string[2] ascii at 0 }", true);
        lexer.IsSchemaContext = true;

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
        var lexer = new Lexer("binary TextMessage extends BaseMessage { Content: string[Length] utf8 }", true);
        lexer.IsSchemaContext = true;

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
