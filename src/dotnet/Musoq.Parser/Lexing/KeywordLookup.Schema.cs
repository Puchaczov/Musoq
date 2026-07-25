using System.Collections.Generic;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

public static partial class KeywordLookup
{
    /// <summary>
    ///     Checks if the text is a schema-specific keyword.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if the text is a schema keyword.</returns>
    public static bool IsSchemaKeyword(string text)
    {
        return SchemaKeywordTypes.ContainsKey(text);
    }

    /// <summary>
    ///     Gets the token type for a schema keyword.
    /// </summary>
    /// <param name="text">The schema keyword text.</param>
    /// <returns>The corresponding token type.</returns>
    public static TokenType GetSchemaKeywordType(string text)
    {
        return SchemaKeywordTypes.GetValueOrDefault(text, TokenType.Word);
    }

    /// <summary>
    ///     Tries to get the token type for a schema keyword.
    /// </summary>
    /// <param name="text">The schema keyword text.</param>
    /// <param name="tokenType">The token type if found.</param>
    /// <returns>True if the text is a recognized schema keyword.</returns>
    public static bool TryGetSchemaKeyword(string text, out TokenType tokenType)
    {
        return SchemaKeywordTypes.TryGetValue(text, out tokenType);
    }

    public static bool TryGetSchemaKeyword(ReadOnlySpan<char> text, out TokenType tokenType)
    {
        switch (text.Length)
        {
            case 2:
                if (EqualsKeyword(text, "le")) return Found(TokenType.LittleEndian, out tokenType);
                if (EqualsKeyword(text, "be")) return Found(TokenType.BigEndian, out tokenType);
                if (EqualsKeyword(text, "at")) return Found(TokenType.At, out tokenType);
                break;
            case 3:
                if (EqualsKeyword(text, "int")) return Found(TokenType.IntType, out tokenType);
                break;
            case 4:
                if (EqualsKeyword(text, "text")) return Found(TokenType.Text, out tokenType);
                if (EqualsKeyword(text, "byte")) return Found(TokenType.ByteType, out tokenType);
                if (EqualsKeyword(text, "uint")) return Found(TokenType.UIntType, out tokenType);
                if (EqualsKeyword(text, "long")) return Found(TokenType.LongType, out tokenType);
                if (EqualsKeyword(text, "bits")) return Found(TokenType.BitsType, out tokenType);
                if (EqualsKeyword(text, "utf8")) return Found(TokenType.Utf8, out tokenType);
                if (EqualsKeyword(text, "trim")) return Found(TokenType.Trim, out tokenType);
                if (EqualsKeyword(text, "null")) return Found(TokenType.Null, out tokenType);
                if (EqualsKeyword(text, "rest")) return Found(TokenType.Rest, out tokenType);
                if (EqualsKeyword(text, "lazy")) return Found(TokenType.Lazy, out tokenType);
                break;
            case 5:
                if (EqualsKeyword(text, "sbyte")) return Found(TokenType.SByteType, out tokenType);
                if (EqualsKeyword(text, "short")) return Found(TokenType.ShortType, out tokenType);
                if (EqualsKeyword(text, "ulong")) return Found(TokenType.ULongType, out tokenType);
                if (EqualsKeyword(text, "float")) return Found(TokenType.FloatType, out tokenType);
                if (EqualsKeyword(text, "align")) return Found(TokenType.Align, out tokenType);
                if (EqualsKeyword(text, "ascii")) return Found(TokenType.Ascii, out tokenType);
                if (EqualsKeyword(text, "rtrim")) return Found(TokenType.RTrim, out tokenType);
                if (EqualsKeyword(text, "ltrim")) return Found(TokenType.LTrim, out tokenType);
                if (EqualsKeyword(text, "check")) return Found(TokenType.Check, out tokenType);
                if (EqualsKeyword(text, "until")) return Found(TokenType.Until, out tokenType);
                if (EqualsKeyword(text, "chars")) return Found(TokenType.Chars, out tokenType);
                if (EqualsKeyword(text, "token")) return Found(TokenType.Token, out tokenType);
                if (EqualsKeyword(text, "lower")) return Found(TokenType.Lower, out tokenType);
                if (EqualsKeyword(text, "upper")) return Found(TokenType.Upper, out tokenType);
                break;
            case 6:
                if (EqualsKeyword(text, "binary")) return Found(TokenType.Binary, out tokenType);
                if (EqualsKeyword(text, "ushort")) return Found(TokenType.UShortType, out tokenType);
                if (EqualsKeyword(text, "double")) return Found(TokenType.DoubleType, out tokenType);
                if (EqualsKeyword(text, "string")) return Found(TokenType.StringType, out tokenType);
                if (EqualsKeyword(text, "latin1")) return Found(TokenType.Latin1, out tokenType);
                if (EqualsKeyword(text, "ebcdic")) return Found(TokenType.Ebcdic, out tokenType);
                if (EqualsKeyword(text, "repeat")) return Found(TokenType.Repeat, out tokenType);
                if (EqualsKeyword(text, "switch")) return Found(TokenType.Switch, out tokenType);
                if (EqualsKeyword(text, "nested")) return Found(TokenType.Nested, out tokenType);
                if (EqualsKeyword(text, "greedy")) return Found(TokenType.Greedy, out tokenType);
                break;
            case 7:
                if (EqualsKeyword(text, "utf16le")) return Found(TokenType.Utf16Le, out tokenType);
                if (EqualsKeyword(text, "utf16be")) return Found(TokenType.Utf16Be, out tokenType);
                if (EqualsKeyword(text, "pattern")) return Found(TokenType.Pattern, out tokenType);
                if (EqualsKeyword(text, "literal")) return Found(TokenType.Literal, out tokenType);
                if (EqualsKeyword(text, "between")) return Found(TokenType.Between, out tokenType);
                if (EqualsKeyword(text, "escaped")) return Found(TokenType.Escaped, out tokenType);
                if (EqualsKeyword(text, "capture")) return Found(TokenType.Capture, out tokenType);
                if (EqualsKeyword(text, "extends")) return Found(TokenType.Extends, out tokenType);
                break;
            case 8:
                if (EqualsKeyword(text, "nullterm")) return Found(TokenType.NullTerm, out tokenType);
                if (EqualsKeyword(text, "optional")) return Found(TokenType.Optional, out tokenType);
                break;
            case 9:
                if (EqualsKeyword(text, "substream")) return Found(TokenType.Substream, out tokenType);
                break;
            case 10:
                if (EqualsKeyword(text, "whitespace")) return Found(TokenType.Whitespace, out tokenType);
                break;
        }

        tokenType = TokenType.Word;
        return false;
    }
}
