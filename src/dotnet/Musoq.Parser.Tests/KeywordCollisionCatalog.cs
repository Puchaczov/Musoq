using System;
using System.Linq;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

internal static class KeywordCollisionCatalog
{
    public static readonly (string Text, TokenType TokenType)[] SqlKeywords =
    [
        ("desc", TokenType.Desc), ("asc", TokenType.Asc), ("and", TokenType.And), ("or", TokenType.Or),
        ("not", TokenType.Not), ("where", TokenType.Where), ("select", TokenType.Select),
        ("from", TokenType.From), ("pivot", TokenType.Pivot), ("unpivot", TokenType.Unpivot),
        ("like", TokenType.Like), ("rlike", TokenType.RLike), ("as", TokenType.As), ("is", TokenType.Is),
        ("null", TokenType.Null), ("union", TokenType.Union), ("except", TokenType.Except),
        ("intersect", TokenType.Intersect), ("having", TokenType.Having), ("contains", TokenType.Contains),
        ("skip", TokenType.Skip), ("take", TokenType.Take), ("with", TokenType.With), ("on", TokenType.On),
        ("functions", TokenType.Functions), ("true", TokenType.True), ("false", TokenType.False),
        ("in", TokenType.In), ("exists", TokenType.Exists), ("any", TokenType.Any), ("some", TokenType.Some),
        ("all", TokenType.All), ("table", TokenType.Table), ("couple", TokenType.Couple),
        ("case", TokenType.Case), ("when", TokenType.When), ("then", TokenType.Then),
        ("else", TokenType.Else), ("end", TokenType.End), ("distinct", TokenType.Distinct),
        ("between", TokenType.Between), ("over", TokenType.Over), ("window", TokenType.Window)
    ];

    public static readonly (string Text, TokenType TokenType)[] SchemaKeywords =
    [
        ("binary", TokenType.Binary), ("text", TokenType.Text), ("le", TokenType.LittleEndian),
        ("be", TokenType.BigEndian), ("byte", TokenType.ByteType), ("sbyte", TokenType.SByteType),
        ("short", TokenType.ShortType), ("ushort", TokenType.UShortType), ("int", TokenType.IntType),
        ("uint", TokenType.UIntType), ("long", TokenType.LongType), ("ulong", TokenType.ULongType),
        ("float", TokenType.FloatType), ("double", TokenType.DoubleType), ("bits", TokenType.BitsType),
        ("align", TokenType.Align), ("string", TokenType.StringType), ("utf8", TokenType.Utf8),
        ("utf16le", TokenType.Utf16Le), ("utf16be", TokenType.Utf16Be), ("null", TokenType.Null),
        ("ascii", TokenType.Ascii), ("latin1", TokenType.Latin1), ("ebcdic", TokenType.Ebcdic),
        ("trim", TokenType.Trim), ("rtrim", TokenType.RTrim), ("ltrim", TokenType.LTrim),
        ("nullterm", TokenType.NullTerm), ("check", TokenType.Check), ("at", TokenType.At),
        ("pattern", TokenType.Pattern), ("literal", TokenType.Literal), ("until", TokenType.Until),
        ("between", TokenType.Between), ("chars", TokenType.Chars), ("token", TokenType.Token),
        ("rest", TokenType.Rest), ("whitespace", TokenType.Whitespace), ("optional", TokenType.Optional),
        ("repeat", TokenType.Repeat), ("switch", TokenType.Switch), ("substream", TokenType.Substream),
        ("nested", TokenType.Nested), ("escaped", TokenType.Escaped), ("greedy", TokenType.Greedy),
        ("lazy", TokenType.Lazy), ("lower", TokenType.Lower), ("upper", TokenType.Upper),
        ("capture", TokenType.Capture), ("extends", TokenType.Extends)
    ];

    public static readonly string[] ContextualExpressionIdentifiers = ["exists", "any", "some", "all"];

    public static readonly string[] ContextualClauseIdentifiers =
    [
        "query", "using", "keep", "settings", "column", "present", "missing", "values", "filter",
        "rows", "range", "qualify", "unbounded", "preceding", "following", "current", "nulls", "first",
        "last", "tie", "break", "by", "exclude", "replace", "rename", "ordinality", "semi", "anti",
        "cross", "asof", "let", "param"
    ];

    public static readonly string[] SchemaOverlapIdentifiers = ["null", "between", "end", "substream"];

    public static readonly string[] ReservedLiteralKeywords = ["null", "true", "false"];

    public static readonly string[] MultiWordGrammarTokens =
    [
        "not exists", "not in", "not like", "not rlike", "union all", "group by", "order by",
        "inner join", "left join", "right join", "full join", "cross apply", "outer apply", "with ordinality",
        "is distinct from", "is not distinct from"
    ];

    public static string[] ReservedSqlIdentifiers =>
        SqlKeywords
            .Where(keyword => Array.IndexOf(ContextualExpressionIdentifiers, keyword.Text) < 0 &&
                              Array.IndexOf(ReservedLiteralKeywords, keyword.Text) < 0)
            .Select(keyword => keyword.Text)
            .ToArray();
}
