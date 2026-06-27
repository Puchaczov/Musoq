using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using Musoq.Parser.Tokens;

namespace Musoq.Benchmarks.Baselines;

/// <summary>
///     Contains baseline implementations for lexer performance comparison.
///     These represent the "before" state for A/B testing optimizations.
/// </summary>
public static class LexerBaseline
{
    /// <summary>
    ///     Optimized FrozenDictionary-based keyword lookup (no allocation).
    ///     This is the optimized implementation for comparison.
    /// </summary>
    private static readonly FrozenDictionary<string, TokenType> KeywordMap =
        new Dictionary<string, TokenType>(StringComparer.OrdinalIgnoreCase)
        {
            ["desc"] = TokenType.Desc,
            ["asc"] = TokenType.Asc,
            ["and"] = TokenType.And,
            [","] = TokenType.Comma,
            ["<>"] = TokenType.Diff,
            [">"] = TokenType.Greater,
            [">="] = TokenType.GreaterEqual,
            ["-"] = TokenType.Hyphen,
            ["("] = TokenType.LeftParenthesis,
            [")"] = TokenType.RightParenthesis,
            ["<"] = TokenType.Less,
            ["<="] = TokenType.LessEqual,
            ["%"] = TokenType.Mod,
            ["not"] = TokenType.Not,
            ["or"] = TokenType.Or,
            ["+"] = TokenType.Plus,
            ["/"] = TokenType.FSlash,
            ["*"] = TokenType.Star,
            ["where"] = TokenType.Where,
            ["select"] = TokenType.Select,
            ["from"] = TokenType.From,
            ["="] = TokenType.Equality,
            ["like"] = TokenType.Like,
            ["rlike"] = TokenType.RLike,
            ["contains"] = TokenType.Contains,
            ["as"] = TokenType.As,
            ["except"] = TokenType.Except,
            ["intersect"] = TokenType.Intersect,
            ["union"] = TokenType.Union,
            ["."] = TokenType.Dot,
            ["having"] = TokenType.Having,
            ["take"] = TokenType.Take,
            ["skip"] = TokenType.Skip,
            ["with"] = TokenType.With,
            ["on"] = TokenType.On,
            ["is"] = TokenType.Is,
            ["null"] = TokenType.Null,
            ["true"] = TokenType.True,
            ["false"] = TokenType.False,
            ["in"] = TokenType.In,
            ["table"] = TokenType.Table,
            ["["] = TokenType.LeftSquareBracket,
            ["]"] = TokenType.RightSquareBracket,
            ["{"] = TokenType.LBracket,
            ["}"] = TokenType.RBracket,
            [";"] = TokenType.Semicolon,
            ["case"] = TokenType.Case,
            ["when"] = TokenType.When,
            ["then"] = TokenType.Then,
            ["else"] = TokenType.Else,
            ["end"] = TokenType.End,
            ["distinct"] = TokenType.Distinct,
            [":"] = TokenType.Colon,
            ["&"] = TokenType.Ampersand,
            ["|"] = TokenType.Pipe,
            ["^"] = TokenType.Caret,
            ["<<"] = TokenType.LeftShift,
            [">>"] = TokenType.RightShift,
            ["=>"] = TokenType.FatArrow,
            ["?"] = TokenType.QuestionMark
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Switch-based keyword lookup without allocating a lower-cased copy.
    ///     Keeps the benchmark identity of the original baseline.
    /// </summary>
    public static TokenType LookupKeywordWithSwitch(string tokenText)
    {
        var token = tokenText.AsSpan();
        switch (token.Length)
        {
            case 1:
                return token[0] switch
                {
                    ',' => TokenType.Comma,
                    '>' => TokenType.Greater,
                    '-' => TokenType.Hyphen,
                    '(' => TokenType.LeftParenthesis,
                    ')' => TokenType.RightParenthesis,
                    '<' => TokenType.Less,
                    '%' => TokenType.Mod,
                    '+' => TokenType.Plus,
                    '/' => TokenType.FSlash,
                    '*' => TokenType.Star,
                    '=' => TokenType.Equality,
                    '.' => TokenType.Dot,
                    '[' => TokenType.LeftSquareBracket,
                    ']' => TokenType.RightSquareBracket,
                    '{' => TokenType.LBracket,
                    '}' => TokenType.RBracket,
                    ';' => TokenType.Semicolon,
                    ':' => TokenType.Colon,
                    '&' => TokenType.Ampersand,
                    '|' => TokenType.Pipe,
                    '^' => TokenType.Caret,
                    '?' => TokenType.QuestionMark,
                    _ => TokenType.Word
                };
            case 2:
                return LookupLength2(Key2(token));
            case 3:
                return LookupLength3(Key3(token));
            case 4:
                return LookupLength4(Key4(token));
            case 5:
                return LookupLength5(Key4(token), token[4]);
            case 6:
                return LookupLength6(Key4(token), token[4], token[5]);
            case 8:
                return LookupLength8(Key4(token), token[4], token[5], token[6], token[7]);
            case 9:
                return Key4(token) == Key('i', 'n', 't', 'e') &&
                    EqualsFolded(token[4], 'r') &&
                    EqualsFolded(token[5], 's') &&
                    EqualsFolded(token[6], 'e') &&
                    EqualsFolded(token[7], 'c') &&
                    EqualsFolded(token[8], 't')
                    ? TokenType.Intersect
                    : TokenType.Word;
        }

        return TokenType.Word;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Key(char first, char second)
    {
        return ((uint)first << 8) | second;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Key(char first, char second, char third)
    {
        return ((uint)first << 16) | ((uint)second << 8) | third;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Key(char first, char second, char third, char fourth)
    {
        return ((uint)first << 24) | ((uint)second << 16) | ((uint)third << 8) | fourth;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Key2(ReadOnlySpan<char> token)
    {
        var first = FoldAscii(token[0]);
        var second = FoldAscii(token[1]);
        return (first | second) > 0x7f ? 0 : Key(first, second);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Key3(ReadOnlySpan<char> token)
    {
        var first = FoldAscii(token[0]);
        var second = FoldAscii(token[1]);
        var third = FoldAscii(token[2]);
        return (first | second | third) > 0x7f ? 0 : Key(first, second, third);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Key4(ReadOnlySpan<char> token)
    {
        var first = FoldAscii(token[0]);
        var second = FoldAscii(token[1]);
        var third = FoldAscii(token[2]);
        var fourth = FoldAscii(token[3]);
        return (first | second | third | fourth) > 0x7f ? 0 : Key(first, second, third, fourth);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TokenType LookupLength2(uint key)
    {
        if (key == Key('<', '>')) return TokenType.Diff;
        if (key == Key('>', '=')) return TokenType.GreaterEqual;
        if (key == Key('<', '=')) return TokenType.LessEqual;
        if (key == Key('o', 'r')) return TokenType.Or;
        if (key == Key('a', 's')) return TokenType.As;
        if (key == Key('o', 'n')) return TokenType.On;
        if (key == Key('i', 's')) return TokenType.Is;
        if (key == Key('i', 'n')) return TokenType.In;
        if (key == Key('<', '<')) return TokenType.LeftShift;
        if (key == Key('>', '>')) return TokenType.RightShift;
        if (key == Key('=', '>')) return TokenType.FatArrow;

        return TokenType.Word;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TokenType LookupLength3(uint key)
    {
        if (key == Key('a', 's', 'c')) return TokenType.Asc;
        if (key == Key('a', 'n', 'd')) return TokenType.And;
        if (key == Key('n', 'o', 't')) return TokenType.Not;
        if (key == Key('e', 'n', 'd')) return TokenType.End;

        return TokenType.Word;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TokenType LookupLength4(uint key)
    {
        if (key == Key('d', 'e', 's', 'c')) return TokenType.Desc;
        if (key == Key('f', 'r', 'o', 'm')) return TokenType.From;
        if (key == Key('l', 'i', 'k', 'e')) return TokenType.Like;
        if (key == Key('t', 'a', 'k', 'e')) return TokenType.Take;
        if (key == Key('s', 'k', 'i', 'p')) return TokenType.Skip;
        if (key == Key('w', 'i', 't', 'h')) return TokenType.With;
        if (key == Key('n', 'u', 'l', 'l')) return TokenType.Null;
        if (key == Key('t', 'r', 'u', 'e')) return TokenType.True;
        if (key == Key('c', 'a', 's', 'e')) return TokenType.Case;
        if (key == Key('w', 'h', 'e', 'n')) return TokenType.When;
        if (key == Key('t', 'h', 'e', 'n')) return TokenType.Then;
        if (key == Key('e', 'l', 's', 'e')) return TokenType.Else;

        return TokenType.Word;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TokenType LookupLength5(uint key, char fifth)
    {
        if (key == Key('w', 'h', 'e', 'r') && EqualsFolded(fifth, 'e')) return TokenType.Where;
        if (key == Key('r', 'l', 'i', 'k') && EqualsFolded(fifth, 'e')) return TokenType.RLike;
        if (key == Key('u', 'n', 'i', 'o') && EqualsFolded(fifth, 'n')) return TokenType.Union;
        if (key == Key('f', 'a', 'l', 's') && EqualsFolded(fifth, 'e')) return TokenType.False;
        if (key == Key('t', 'a', 'b', 'l') && EqualsFolded(fifth, 'e')) return TokenType.Table;

        return TokenType.Word;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TokenType LookupLength6(uint key, char fifth, char sixth)
    {
        if (key == Key('s', 'e', 'l', 'e') && EqualsFolded(fifth, 'c') && EqualsFolded(sixth, 't')) return TokenType.Select;
        if (key == Key('e', 'x', 'c', 'e') && EqualsFolded(fifth, 'p') && EqualsFolded(sixth, 't')) return TokenType.Except;
        if (key == Key('h', 'a', 'v', 'i') && EqualsFolded(fifth, 'n') && EqualsFolded(sixth, 'g')) return TokenType.Having;

        return TokenType.Word;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TokenType LookupLength8(uint key, char fifth, char sixth, char seventh, char eighth)
    {
        if (key == Key('c', 'o', 'n', 't') && EqualsFolded(fifth, 'a') && EqualsFolded(sixth, 'i') && EqualsFolded(seventh, 'n') && EqualsFolded(eighth, 's')) return TokenType.Contains;
        if (key == Key('d', 'i', 's', 't') && EqualsFolded(fifth, 'i') && EqualsFolded(sixth, 'n') && EqualsFolded(seventh, 'c') && EqualsFolded(eighth, 't')) return TokenType.Distinct;

        return TokenType.Word;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char FoldAscii(char character)
    {
        if ((uint)(character - 'A') <= 'Z' - 'A')
            return (char)(character | 0x20);

        return character;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool EqualsFolded(char character, char expected)
    {
        character = FoldAscii(character);
        return character <= 0x7f && character == expected;
    }

    /// <summary>
    ///     Optimized FrozenDictionary-based keyword lookup.
    /// </summary>
    public static TokenType LookupKeywordWithDictionary(string tokenText)
    {
        return KeywordMap.TryGetValue(tokenText, out var tokenType)
            ? tokenType
            : TokenType.Word;
    }
}
