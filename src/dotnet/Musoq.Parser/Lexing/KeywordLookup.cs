using System.Collections.Frozen;
using System.Collections.Generic;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

public static partial class KeywordLookup
{
    private static readonly FrozenDictionary<string, TokenType> Keywords = CreateKeywords();
    private static readonly FrozenDictionary<string, TokenType> SchemaKeywordTypes = CreateSchemaKeywordTypes();
    private static readonly FrozenSet<string> SchemaKeywords = CreateSchemaKeywords();
    private static readonly FrozenDictionary<string, TokenType> Operators = CreateOperators();

    private static FrozenDictionary<string, TokenType> CreateKeywords()
    {
        return new Dictionary<string, TokenType>(StringComparer.OrdinalIgnoreCase)
        {
            { "desc", TokenType.Desc },
            { "asc", TokenType.Asc },
            { "and", TokenType.And },
            { "or", TokenType.Or },
            { "not", TokenType.Not },
            { "where", TokenType.Where },
            { "select", TokenType.Select },
            { "from", TokenType.From },
            { "pivot", TokenType.Pivot },
            { "unpivot", TokenType.Unpivot },
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
            { "exists", TokenType.Exists }, { "any", TokenType.Any },
            { "some", TokenType.Some }, { "all", TokenType.All },
            { "table", TokenType.Table },
            { "couple", TokenType.Couple },
            { "case", TokenType.Case },
            { "when", TokenType.When },
            { "then", TokenType.Then },
            { "else", TokenType.Else },
            { "end", TokenType.End },
            { "distinct", TokenType.Distinct },
            { "between", TokenType.Between },
            { "over", TokenType.Over },
            { "window", TokenType.Window }
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static FrozenDictionary<string, TokenType> CreateOperators()
    {
        return new Dictionary<string, TokenType>(StringComparer.Ordinal)
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
            { "::", TokenType.DoubleColon },
            { "&", TokenType.Ampersand },
            { "|", TokenType.Pipe },
            { "^", TokenType.Caret },
            { "<<", TokenType.LeftShift },
            { ">>", TokenType.RightShift },
            { "=>", TokenType.FatArrow },
            { "??", TokenType.NullCoalescing },
            { "?", TokenType.QuestionMark }
        }.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static FrozenSet<string> CreateSchemaKeywords()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "binary", "text", "le", "be",
            "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong",
            "float", "double", "bits", "align", "string",
            "utf8", "utf16le", "utf16be", "ascii", "latin1", "ebcdic",
            "trim", "rtrim", "ltrim", "nullterm", "check", "at",
            "pattern", "literal", "until", "between", "chars", "token",
            "rest", "whitespace", "optional", "repeat", "switch", "nested",
            "escaped", "greedy", "lazy", "lower", "upper", "capture", "extends"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }

    private static FrozenDictionary<string, TokenType> CreateSchemaKeywordTypes()
    {
        return new Dictionary<string, TokenType>(StringComparer.OrdinalIgnoreCase)
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
            { "substream", TokenType.Substream },
            { "nested", TokenType.Nested },
            { "escaped", TokenType.Escaped },
            { "greedy", TokenType.Greedy },
            { "lazy", TokenType.Lazy },
            { "lower", TokenType.Lower },
            { "upper", TokenType.Upper },
            { "capture", TokenType.Capture },
            { "extends", TokenType.Extends }
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public static bool TryGetKeyword(string text, out TokenType tokenType)
    {
        return Keywords.TryGetValue(text, out tokenType);
    }

    public static bool TryGetKeyword(ReadOnlySpan<char> text, out TokenType tokenType)
    {
        switch (text.Length)
        {
            case 2:
                if (EqualsKeyword(text, "or")) return Found(TokenType.Or, out tokenType);
                if (EqualsKeyword(text, "as")) return Found(TokenType.As, out tokenType);
                if (EqualsKeyword(text, "is")) return Found(TokenType.Is, out tokenType);
                if (EqualsKeyword(text, "on")) return Found(TokenType.On, out tokenType);
                if (EqualsKeyword(text, "in")) return Found(TokenType.In, out tokenType);
                break;
            case 3:
                if (EqualsKeyword(text, "and")) return Found(TokenType.And, out tokenType);
                if (EqualsKeyword(text, "not")) return Found(TokenType.Not, out tokenType);
                if (EqualsKeyword(text, "any")) return Found(TokenType.Any, out tokenType);
                if (EqualsKeyword(text, "all")) return Found(TokenType.All, out tokenType);
                if (EqualsKeyword(text, "asc")) return Found(TokenType.Asc, out tokenType);
                if (EqualsKeyword(text, "end")) return Found(TokenType.End, out tokenType);
                break;
            case 4:
                if (EqualsKeyword(text, "desc")) return Found(TokenType.Desc, out tokenType);
                if (EqualsKeyword(text, "from")) return Found(TokenType.From, out tokenType);
                if (EqualsKeyword(text, "like")) return Found(TokenType.Like, out tokenType);
                if (EqualsKeyword(text, "null")) return Found(TokenType.Null, out tokenType);
                if (EqualsKeyword(text, "skip")) return Found(TokenType.Skip, out tokenType);
                if (EqualsKeyword(text, "take")) return Found(TokenType.Take, out tokenType);
                if (EqualsKeyword(text, "with")) return Found(TokenType.With, out tokenType);
                if (EqualsKeyword(text, "true")) return Found(TokenType.True, out tokenType);
                if (EqualsKeyword(text, "some")) return Found(TokenType.Some, out tokenType);
                if (EqualsKeyword(text, "case")) return Found(TokenType.Case, out tokenType);
                if (EqualsKeyword(text, "when")) return Found(TokenType.When, out tokenType);
                if (EqualsKeyword(text, "then")) return Found(TokenType.Then, out tokenType);
                if (EqualsKeyword(text, "else")) return Found(TokenType.Else, out tokenType);
                if (EqualsKeyword(text, "over")) return Found(TokenType.Over, out tokenType);
                break;
            case 5:
                if (EqualsKeyword(text, "where")) return Found(TokenType.Where, out tokenType);
                if (EqualsKeyword(text, "pivot")) return Found(TokenType.Pivot, out tokenType);
                if (EqualsKeyword(text, "rlike")) return Found(TokenType.RLike, out tokenType);
                if (EqualsKeyword(text, "union")) return Found(TokenType.Union, out tokenType);
                if (EqualsKeyword(text, "table")) return Found(TokenType.Table, out tokenType);
                if (EqualsKeyword(text, "false")) return Found(TokenType.False, out tokenType);
                break;
            case 6:
                if (EqualsKeyword(text, "select")) return Found(TokenType.Select, out tokenType);
                if (EqualsKeyword(text, "except")) return Found(TokenType.Except, out tokenType);
                if (EqualsKeyword(text, "having")) return Found(TokenType.Having, out tokenType);
                if (EqualsKeyword(text, "couple")) return Found(TokenType.Couple, out tokenType);
                if (EqualsKeyword(text, "window")) return Found(TokenType.Window, out tokenType);
                if (EqualsKeyword(text, "exists")) return Found(TokenType.Exists, out tokenType);
                break;
            case 7:
                if (EqualsKeyword(text, "between")) return Found(TokenType.Between, out tokenType);
                if (EqualsKeyword(text, "unpivot")) return Found(TokenType.Unpivot, out tokenType);
                break;
            case 8:
                if (EqualsKeyword(text, "contains")) return Found(TokenType.Contains, out tokenType);
                if (EqualsKeyword(text, "distinct")) return Found(TokenType.Distinct, out tokenType);
                break;
            case 9:
                if (EqualsKeyword(text, "functions")) return Found(TokenType.Functions, out tokenType);
                if (EqualsKeyword(text, "intersect")) return Found(TokenType.Intersect, out tokenType);
                break;
        }

        tokenType = TokenType.Word;
        return false;
    }

}
