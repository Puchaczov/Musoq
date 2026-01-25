using System.Collections.Frozen;
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
    ///     Original switch-based keyword lookup (allocates via ToLowerInvariant).
    ///     This is the baseline implementation for comparison.
    /// </summary>
    public static TokenType LookupKeywordWithSwitch(string tokenText)
    {
        var loweredToken = tokenText.ToLowerInvariant();

        return loweredToken switch
        {
            "desc" => TokenType.Desc,
            "asc" => TokenType.Asc,
            "and" => TokenType.And,
            "," => TokenType.Comma,
            "<>" => TokenType.Diff,
            ">" => TokenType.Greater,
            ">=" => TokenType.GreaterEqual,
            "-" => TokenType.Hyphen,
            "(" => TokenType.LeftParenthesis,
            ")" => TokenType.RightParenthesis,
            "<" => TokenType.Less,
            "<=" => TokenType.LessEqual,
            "%" => TokenType.Mod,
            "not" => TokenType.Not,
            "or" => TokenType.Or,
            "+" => TokenType.Plus,
            "/" => TokenType.FSlash,
            "*" => TokenType.Star,
            "where" => TokenType.Where,
            "select" => TokenType.Select,
            "from" => TokenType.From,
            "=" => TokenType.Equality,
            "like" => TokenType.Like,
            "rlike" => TokenType.RLike,
            "contains" => TokenType.Contains,
            "as" => TokenType.As,
            "except" => TokenType.Except,
            "intersect" => TokenType.Intersect,
            "union" => TokenType.Union,
            "." => TokenType.Dot,
            "having" => TokenType.Having,
            "take" => TokenType.Take,
            "skip" => TokenType.Skip,
            "with" => TokenType.With,
            "on" => TokenType.On,
            "is" => TokenType.Is,
            "null" => TokenType.Null,
            "true" => TokenType.True,
            "false" => TokenType.False,
            "in" => TokenType.In,
            "table" => TokenType.Table,
            "[" => TokenType.LeftSquareBracket,
            "]" => TokenType.RightSquareBracket,
            "{" => TokenType.LBracket,
            "}" => TokenType.RBracket,
            ";" => TokenType.Semicolon,
            "case" => TokenType.Case,
            "when" => TokenType.When,
            "then" => TokenType.Then,
            "else" => TokenType.Else,
            "end" => TokenType.End,
            "distinct" => TokenType.Distinct,
            "inner" => TokenType.Word,
            "outer" => TokenType.Word,
            "left" => TokenType.Word,
            "right" => TokenType.Word,
            "cross" => TokenType.Word,
            "join" => TokenType.Word,
            "apply" => TokenType.Word,
            "group" => TokenType.Word,
            "by" => TokenType.Word,
            "order" => TokenType.Word,
            ":" => TokenType.Colon,
            "&" => TokenType.Ampersand,
            "|" => TokenType.Pipe,
            "^" => TokenType.Caret,
            "<<" => TokenType.LeftShift,
            ">>" => TokenType.RightShift,
            "=>" => TokenType.FatArrow,
            "?" => TokenType.QuestionMark,
            _ => TokenType.Word
        };
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
