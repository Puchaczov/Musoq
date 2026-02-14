using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

/// <summary>
///     Provides fast case-insensitive keyword lookup using frozen dictionaries.
///     Eliminates the need for ToLowerInvariant() allocations during tokenization.
/// </summary>
public static class KeywordLookup
{
    private static readonly FrozenDictionary<string, TokenType> Keywords;
    private static readonly FrozenDictionary<string, TokenType> SchemaKeywordTypes;
    private static readonly FrozenSet<string> SchemaKeywords;
    private static readonly FrozenDictionary<string, TokenType> Operators;

    static KeywordLookup()
    {
        // SQL keywords mapping (case-insensitive)
        var keywords = new Dictionary<string, TokenType>(StringComparer.OrdinalIgnoreCase)
        {
            // Basic SQL keywords
            { "desc", TokenType.Desc },
            { "asc", TokenType.Asc },
            { "and", TokenType.And },
            { "or", TokenType.Or },
            { "not", TokenType.Not },
            { "where", TokenType.Where },
            { "select", TokenType.Select },
            { "from", TokenType.From },
            { "like", TokenType.Like },
            { "rlike", TokenType.RLike },
            { "as", TokenType.As },
            { "is", TokenType.Is },
            { "null", TokenType.Null },
            { "union", TokenType.Union },
            { "except", TokenType.Except },
            { "intersect", TokenType.Intersect },
            { "having", TokenType.Having },
            { "contains", TokenType.Contains },
            { "skip", TokenType.Skip },
            { "take", TokenType.Take },
            { "with", TokenType.With },
            { "on", TokenType.On },
            { "functions", TokenType.Functions },
            { "true", TokenType.True },
            { "false", TokenType.False },
            { "in", TokenType.In },
            { "table", TokenType.Table },
            { "couple", TokenType.Couple },
            { "case", TokenType.Case },
            { "when", TokenType.When },
            { "then", TokenType.Then },
            { "else", TokenType.Else },
            { "end", TokenType.End },
            { "distinct", TokenType.Distinct },
            { "between", TokenType.Between }
        };

        Keywords = keywords.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        // Operators (case-sensitive, exact match)
        var operators = new Dictionary<string, TokenType>(StringComparer.Ordinal)
        {
            { ",", TokenType.Comma },
            { "<>", TokenType.Diff },
            { ">", TokenType.Greater },
            { ">=", TokenType.GreaterEqual },
            { "-", TokenType.Hyphen },
            { "(", TokenType.LeftParenthesis },
            { ")", TokenType.RightParenthesis },
            { "<", TokenType.Less },
            { "<=", TokenType.LessEqual },
            { "%", TokenType.Mod },
            { "+", TokenType.Plus },
            { "/", TokenType.FSlash },
            { "*", TokenType.Star },
            { "=", TokenType.Equality },
            { ".", TokenType.Dot },
            { "[", TokenType.LeftSquareBracket },
            { "]", TokenType.RightSquareBracket },
            { "{", TokenType.LBracket },
            { "}", TokenType.RBracket },
            { ";", TokenType.Semicolon },
            { ":", TokenType.Colon },
            { "&", TokenType.Ampersand },
            { "|", TokenType.Pipe },
            { "^", TokenType.Caret },
            { "<<", TokenType.LeftShift },
            { ">>", TokenType.RightShift },
            { "=>", TokenType.FatArrow },
            { "?", TokenType.QuestionMark }
        };

        Operators = operators.ToFrozenDictionary(StringComparer.Ordinal);

        // Schema-specific keywords that should only be recognized in schema context
        var schemaKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "binary", "text", "le", "be",
            "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong",
            "float", "double", "bits", "align", "string",
            "utf8", "utf16le", "utf16be", "ascii", "latin1", "ebcdic",
            "trim", "rtrim", "ltrim", "nullterm", "check", "at",
            "pattern", "literal", "until", "between", "chars", "token",
            "rest", "whitespace", "optional", "repeat", "switch", "nested",
            "escaped", "greedy", "lazy", "lower", "upper", "capture", "extends"
        };

        SchemaKeywords = schemaKeywords.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        // Schema keyword to token type mapping (case-insensitive)
        var schemaKeywordTypes = new Dictionary<string, TokenType>(StringComparer.OrdinalIgnoreCase)
        {
            { "binary", TokenType.Binary },
            { "text", TokenType.Text },
            { "le", TokenType.LittleEndian },
            { "be", TokenType.BigEndian },
            { "byte", TokenType.ByteType },
            { "sbyte", TokenType.SByteType },
            { "short", TokenType.ShortType },
            { "ushort", TokenType.UShortType },
            { "int", TokenType.IntType },
            { "uint", TokenType.UIntType },
            { "long", TokenType.LongType },
            { "ulong", TokenType.ULongType },
            { "float", TokenType.FloatType },
            { "double", TokenType.DoubleType },
            { "bits", TokenType.BitsType },
            { "align", TokenType.Align },
            { "string", TokenType.StringType },
            { "utf8", TokenType.Utf8 },
            { "utf16le", TokenType.Utf16Le },
            { "utf16be", TokenType.Utf16Be },
            { "ascii", TokenType.Ascii },
            { "latin1", TokenType.Latin1 },
            { "ebcdic", TokenType.Ebcdic },
            { "trim", TokenType.Trim },
            { "rtrim", TokenType.RTrim },
            { "ltrim", TokenType.LTrim },
            { "nullterm", TokenType.NullTerm },
            { "check", TokenType.Check },
            { "at", TokenType.At },
            { "pattern", TokenType.Pattern },
            { "literal", TokenType.Literal },
            { "until", TokenType.Until },
            { "between", TokenType.Between },
            { "chars", TokenType.Chars },
            { "token", TokenType.Token },
            { "rest", TokenType.Rest },
            { "whitespace", TokenType.Whitespace },
            { "optional", TokenType.Optional },
            { "repeat", TokenType.Repeat },
            { "switch", TokenType.Switch },
            { "nested", TokenType.Nested },
            { "escaped", TokenType.Escaped },
            { "greedy", TokenType.Greedy },
            { "lazy", TokenType.Lazy },
            { "lower", TokenType.Lower },
            { "upper", TokenType.Upper },
            { "capture", TokenType.Capture },
            { "extends", TokenType.Extends }
        };

        SchemaKeywordTypes = schemaKeywordTypes.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Tries to get the token type for a keyword using a string key.
    /// </summary>
    /// <param name="text">The text to look up.</param>
    /// <param name="tokenType">The token type if found.</param>
    /// <returns>True if the text is a recognized keyword.</returns>
    public static bool TryGetKeyword(string text, out TokenType tokenType)
    {
        return Keywords.TryGetValue(text, out tokenType);
    }

    /// <summary>
    ///     Tries to get the token type for an operator.
    /// </summary>
    /// <param name="text">The operator text to look up.</param>
    /// <param name="tokenType">The token type if found.</param>
    /// <returns>True if the text is a recognized operator.</returns>
    public static bool TryGetOperator(string text, out TokenType tokenType)
    {
        return Operators.TryGetValue(text, out tokenType);
    }

    /// <summary>
    ///     Checks if the text is a schema-specific keyword.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if the text is a schema keyword.</returns>
    public static bool IsSchemaKeyword(string text)
    {
        return SchemaKeywords.Contains(text);
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
}
