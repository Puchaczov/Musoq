using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

/// <summary>
///     Ultra-fast first-character-based token classification.
///     Uses lookup tables to instantly determine which token category a character belongs to.
/// </summary>
public static class FastCharacterClassifier
{
    private const int AsciiCharacterCount = 128;

    /// <summary>
    ///     Token categories for fast dispatch.
    /// </summary>
    public enum CharCategory : byte
    {
        /// <summary>Unknown character - requires full regex scan.</summary>
        Unknown = 0,

        /// <summary>Whitespace character.</summary>
        Whitespace = 1,

        /// <summary>Letter or underscore - start of identifier/keyword.</summary>
        Identifier = 2,

        /// <summary>Digit - start of number.</summary>
        Digit = 3,

        /// <summary>Single quote - start of string literal.</summary>
        Quote = 4,

        /// <summary>Single-character operator.</summary>
        SingleCharOperator = 5,

        /// <summary>Multi-character operator start (could be 1 or 2 chars).</summary>
        MultiCharOperator = 6,

        /// <summary>Hash - start of schema reference (#table.source()).</summary>
        Hash = 7,

        /// <summary>Dash - could be minus, comment start, or negative number.</summary>
        Dash = 8,

        /// <summary>Slash - could be divide or comment start.</summary>
        Slash = 9,

        /// <summary>Dot - could be decimal or member access.</summary>
        Dot = 10,

        /// <summary>Square bracket - array access or column reference.</summary>
        SquareBracket = 11,

        /// <summary>Colon - could be single : or postfix cast ::.</summary>
        Colon = 12,

        /// <summary>Dollar - start of a script parameter reference.</summary>
        Dollar = 13,

        /// <summary>Question mark - could be ? or ??.</summary>
        QuestionMark = 14
    }

    // Lookup table for ASCII characters (0-127)
    private static readonly CharCategory[] AsciiCategories = CreateAsciiCategories();

    // Single-character operators that can be resolved immediately
    private static readonly FrozenDictionary<char, TokenType> SingleCharOperators = CreateSingleCharOperators();

    // Cached single-character strings for ASCII chars to avoid ToString() allocations
    private static readonly string[] CharToStringCache = CreateCharToStringCache();

    private static string[] CreateCharToStringCache()
    {
        var cache = new string[AsciiCharacterCount];
        for (var i = 0; i < AsciiCharacterCount; i++)
            cache[i] = ((char)i).ToString();

        return cache;
    }

    private static CharCategory[] CreateAsciiCategories()
    {
        var categories = new CharCategory[AsciiCharacterCount];

        // Whitespace
        categories[' '] = CharCategory.Whitespace;
        categories['\t'] = CharCategory.Whitespace;
        categories['\n'] = CharCategory.Whitespace;
        categories['\r'] = CharCategory.Whitespace;

        // Identifiers (letters and underscore)
        for (var c = 'a'; c <= 'z'; c++)
            categories[c] = CharCategory.Identifier;
        for (var c = 'A'; c <= 'Z'; c++)
            categories[c] = CharCategory.Identifier;
        categories['_'] = CharCategory.Identifier;

        // Digits
        for (var c = '0'; c <= '9'; c++)
            categories[c] = CharCategory.Digit;

        // String literal
        categories['\''] = CharCategory.Quote;

        // Single-character operators
        categories[','] = CharCategory.SingleCharOperator;
        categories['+'] = CharCategory.SingleCharOperator;
        categories['*'] = CharCategory.SingleCharOperator;
        categories['%'] = CharCategory.SingleCharOperator;
        categories['('] = CharCategory.SingleCharOperator;
        categories[')'] = CharCategory.SingleCharOperator;
        categories['{'] = CharCategory.SingleCharOperator;
        categories['}'] = CharCategory.SingleCharOperator;
        categories[';'] = CharCategory.SingleCharOperator;
        categories['?'] = CharCategory.QuestionMark;

        // Multi-character operator starts
        categories['<'] = CharCategory.MultiCharOperator;
        categories['>'] = CharCategory.MultiCharOperator;
        categories['='] = CharCategory.MultiCharOperator;
        categories['!'] = CharCategory.MultiCharOperator;
        categories['&'] = CharCategory.MultiCharOperator;
        categories['|'] = CharCategory.MultiCharOperator;
        categories['^'] = CharCategory.MultiCharOperator;

        // Special cases
        categories['#'] = CharCategory.Hash;
        categories['-'] = CharCategory.Dash;
        categories['/'] = CharCategory.Slash;
        categories['.'] = CharCategory.Dot;
        categories['['] = CharCategory.SquareBracket;
        categories[']'] = CharCategory.SingleCharOperator; // ] is always single
        categories[':'] = CharCategory.Colon;
        categories['$'] = CharCategory.Dollar;

        return categories;
    }

    private static FrozenDictionary<char, TokenType> CreateSingleCharOperators()
    {
        return new Dictionary<char, TokenType>
        {
            [','] = TokenType.Comma,
            ['+'] = TokenType.Plus,
            ['*'] = TokenType.Star,
            ['%'] = TokenType.Mod,
            ['('] = TokenType.LeftParenthesis,
            [')'] = TokenType.RightParenthesis,
            ['{'] = TokenType.LBracket,
            ['}'] = TokenType.RBracket,
            ['['] = TokenType.LeftSquareBracket,
            [']'] = TokenType.RightSquareBracket,
            [';'] = TokenType.Semicolon,
            ['?'] = TokenType.QuestionMark
        }.ToFrozenDictionary();
    }

    /// <summary>
    ///     Gets the category of a character for fast dispatch.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharCategory GetCategory(char c)
    {
        return c < 128 ? AsciiCategories[c] : CharCategory.Identifier;
    }

    /// <summary>
    ///     Tries to get the token type for a single-character operator.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetSingleCharOperator(char c, out TokenType tokenType)
    {
        return SingleCharOperators.TryGetValue(c, out tokenType);
    }

    /// <summary>
    ///     Checks if a character is a whitespace.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWhitespace(char c)
    {
        return c is ' ' or '\t' or '\n' or '\r';
    }

    /// <summary>
    ///     Checks if a character can continue an identifier.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIdentifierContinue(char c)
    {
        return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_';
    }

    /// <summary>
    ///     Checks if a character can start an identifier.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIdentifierStart(char c)
    {
        return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_';
    }

    /// <summary>
    ///     Checks if a character is a digit.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDigit(char c)
    {
        return c is >= '0' and <= '9';
    }

    /// <summary>
    ///     Checks if a character is a hex digit.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHexDigit(char c)
    {
        return c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
    }

    /// <summary>
    ///     Gets a cached string representation of an ASCII character.
    ///     Avoids allocations for common single-character tokens.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CharToString(char c)
    {
        return c < 128 ? CharToStringCache[c] : c.ToString();
    }
}
