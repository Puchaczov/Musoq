using System;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

/// <summary>
///     Provides fast character-based dispatch for token classification.
///     Uses first character(s) to determine token category, avoiding linear regex scanning.
/// </summary>
public static class CharacterDispatcher
{
    /// <summary>
    ///     Token category for first-pass classification.
    /// </summary>
    public enum TokenCategory
    {
        /// <summary>Whitespace characters (space, tab, newline, etc.)</summary>
        Whitespace,

        /// <summary>Start of an identifier or keyword</summary>
        Identifier,

        /// <summary>Start of a number literal</summary>
        Number,

        /// <summary>Start of a string literal (single quote)</summary>
        StringLiteral,

        /// <summary>Single-character operator</summary>
        SingleCharOperator,

        /// <summary>Operator that might be multi-character</summary>
        PotentialMultiCharOperator,

        /// <summary>Start of a comment or division operator</summary>
        SlashOrComment,

        /// <summary>Start of a schema reference (#)</summary>
        SchemaReference,

        /// <summary>Left parenthesis</summary>
        LeftParen,

        /// <summary>Right parenthesis</summary>
        RightParen,

        /// <summary>Left square bracket</summary>
        LeftBracket,

        /// <summary>Right square bracket</summary>
        RightBracket,

        /// <summary>Left curly brace</summary>
        LeftBrace,

        /// <summary>Right curly brace</summary>
        RightBrace,

        /// <summary>Unknown character</summary>
        Unknown
    }

    // Lookup table for ASCII characters (0-127)
    private static readonly TokenCategory[] AsciiCategories = new TokenCategory[128];

    static CharacterDispatcher()
    {
        // Initialize all to Unknown
        Array.Fill(AsciiCategories, TokenCategory.Unknown);

        // Whitespace
        AsciiCategories[' '] = TokenCategory.Whitespace;
        AsciiCategories['\t'] = TokenCategory.Whitespace;
        AsciiCategories['\n'] = TokenCategory.Whitespace;
        AsciiCategories['\r'] = TokenCategory.Whitespace;
        AsciiCategories['\f'] = TokenCategory.Whitespace;
        AsciiCategories['\v'] = TokenCategory.Whitespace;

        // Letters (start of identifier/keyword)
        for (var c = 'a'; c <= 'z'; c++)
            AsciiCategories[c] = TokenCategory.Identifier;
        for (var c = 'A'; c <= 'Z'; c++)
            AsciiCategories[c] = TokenCategory.Identifier;
        AsciiCategories['_'] = TokenCategory.Identifier;

        // Digits (start of number)
        for (var c = '0'; c <= '9'; c++)
            AsciiCategories[c] = TokenCategory.Number;

        // String literal
        AsciiCategories['\''] = TokenCategory.StringLiteral;

        // Single-character operators
        AsciiCategories[','] = TokenCategory.SingleCharOperator;
        AsciiCategories['.'] = TokenCategory.SingleCharOperator;
        AsciiCategories['+'] = TokenCategory.SingleCharOperator;
        AsciiCategories['*'] = TokenCategory.SingleCharOperator;
        AsciiCategories['%'] = TokenCategory.SingleCharOperator;
        AsciiCategories[';'] = TokenCategory.SingleCharOperator;
        AsciiCategories['?'] = TokenCategory.SingleCharOperator;
        AsciiCategories['^'] = TokenCategory.SingleCharOperator;
        AsciiCategories['&'] = TokenCategory.SingleCharOperator;
        AsciiCategories['|'] = TokenCategory.SingleCharOperator;

        // Operators that might be multi-character
        AsciiCategories['-'] = TokenCategory.PotentialMultiCharOperator; // - or -- (comment)
        AsciiCategories['<'] = TokenCategory.PotentialMultiCharOperator; // < or <= or <<
        AsciiCategories['>'] = TokenCategory.PotentialMultiCharOperator; // > or >= or >>
        AsciiCategories['='] = TokenCategory.PotentialMultiCharOperator; // = or =>
        AsciiCategories['!'] = TokenCategory.PotentialMultiCharOperator; // !=
        AsciiCategories[':'] = TokenCategory.PotentialMultiCharOperator; // : or ::

        // Slash (could be division or comment start)
        AsciiCategories['/'] = TokenCategory.SlashOrComment;

        // Schema reference
        AsciiCategories['#'] = TokenCategory.SchemaReference;

        // Brackets and braces
        AsciiCategories['('] = TokenCategory.LeftParen;
        AsciiCategories[')'] = TokenCategory.RightParen;
        AsciiCategories['['] = TokenCategory.LeftBracket;
        AsciiCategories[']'] = TokenCategory.RightBracket;
        AsciiCategories['{'] = TokenCategory.LeftBrace;
        AsciiCategories['}'] = TokenCategory.RightBrace;
    }

    /// <summary>
    ///     Gets the token category for a character.
    /// </summary>
    /// <param name="c">The character to classify.</param>
    /// <returns>The token category.</returns>
    public static TokenCategory GetCategory(char c)
    {
        if (c < 128)
            return AsciiCategories[c];


        if (char.IsLetter(c))
            return TokenCategory.Identifier;
        if (char.IsWhiteSpace(c))
            return TokenCategory.Whitespace;

        return TokenCategory.Unknown;
    }

    /// <summary>
    ///     Gets the token type for a single-character operator.
    /// </summary>
    /// <param name="c">The operator character.</param>
    /// <returns>The token type.</returns>
    public static TokenType GetSingleCharOperatorType(char c)
    {
        return c switch
        {
            ',' => TokenType.Comma,
            '.' => TokenType.Dot,
            '+' => TokenType.Plus,
            '*' => TokenType.Star,
            '%' => TokenType.Mod,
            ';' => TokenType.Semicolon,
            '?' => TokenType.QuestionMark,
            '^' => TokenType.Caret,
            '&' => TokenType.Ampersand,
            '|' => TokenType.Pipe,
            '(' => TokenType.LeftParenthesis,
            ')' => TokenType.RightParenthesis,
            '[' => TokenType.LeftSquareBracket,
            ']' => TokenType.RightSquareBracket,
            '{' => TokenType.LBracket,
            '}' => TokenType.RBracket,
            _ => TokenType.Word
        };
    }

    /// <summary>
    ///     Checks if a character can continue an identifier.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character is valid in an identifier.</returns>
    public static bool IsIdentifierContinuation(char c)
    {
        if (c < 128)
            return (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z') ||
                   (c >= '0' && c <= '9') ||
                   c == '_';
        return char.IsLetterOrDigit(c);
    }

    /// <summary>
    ///     Checks if a character is a digit.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character is a digit.</returns>
    public static bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    /// <summary>
    ///     Checks if a character is a hex digit.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character is a hex digit.</returns>
    public static bool IsHexDigit(char c)
    {
        return (c >= '0' && c <= '9') ||
               (c >= 'a' && c <= 'f') ||
               (c >= 'A' && c <= 'F');
    }

    /// <summary>
    ///     Checks if a character is a binary digit.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character is 0 or 1.</returns>
    public static bool IsBinaryDigit(char c)
    {
        return c is '0' or '1';
    }

    /// <summary>
    ///     Checks if a character is an octal digit.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character is 0-7.</returns>
    public static bool IsOctalDigit(char c)
    {
        return c >= '0' && c <= '7';
    }

    /// <summary>
    ///     Checks if a character is whitespace.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character is whitespace.</returns>
    public static bool IsWhitespace(char c)
    {
        return c is ' ' or '\t' or '\n' or '\r' or '\f' or '\v';
    }

    /// <summary>
    ///     Gets the type of number literal based on prefix (0x, 0b, 0o).
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="position">Current position.</param>
    /// <returns>The number type or null if decimal.</returns>
    public static TokenType? GetNumberPrefixType(string input, int position)
    {
        if (position + 1 >= input.Length || input[position] != '0')
            return null;

        var second = input[position + 1];
        return second switch
        {
            'x' or 'X' => TokenType.HexadecimalInteger,
            'b' or 'B' => TokenType.BinaryInteger,
            'o' or 'O' => TokenType.OctalInteger,
            _ => null
        };
    }
}
