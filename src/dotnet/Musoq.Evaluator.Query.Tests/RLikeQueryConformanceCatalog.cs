using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

internal enum RLikePatternFamily
{
    Literal,
    AbsolutePrefix,
    AbsoluteSuffix,
    AbsoluteExact,
    Empty,
    Dot,
    CharacterClass,
    Quantifier,
    Alternation,
    Group,
    InlineOptions,
    Lookaround,
    Backreference,
    UnicodeCategory,
    EscapedLiteral,
    Multiline
}

internal enum RLikePatternSource
{
    Literal,
    SameRowColumn,
    Parameter,
    Variable,
    Concatenation,
    MethodResult,
    CaseExpression,
    CoalesceExpression,
    InnerApplyColumn
}

internal enum RLikeCharacterDomain
{
    Ascii,
    Unicode,
    CultureSensitive,
    ComposedAndDecomposed,
    SurrogateContaining
}

internal enum RLikePolarity { Positive, Negated }

internal enum RLikeNullCase { Neither, Input, Pattern, Both }

internal enum RLikeQueryContext
{
    Projection,
    Where,
    BooleanComposition,
    CaseWhen,
    InnerJoin,
    LeftJoin,
    CrossApply,
    OuterApply,
    AggregateFilter,
    Having,
    Qualify,
    WindowFilter,
    Cte,
    RecursiveCte,
    SetBranch,
    DistinctOrderPage,
    PredicateQuantifier,
    Parallel
}

internal enum RLikeExecutionStrategy { Direct, Prepared, Dynamic }

internal sealed record RLikeQueryContextCoverage(
    string Id,
    RLikeQueryContext Context,
    RLikeExecutionStrategy Strategy,
    RLikePatternSource? PatternSource = null);

internal sealed record RLikeQueryConformanceCase(
    string Id,
    string Query,
    BasicEntity[] Rows,
    object?[][] ExpectedRows,
    RLikePatternFamily Family,
    RLikePatternSource Source,
    RLikeCharacterDomain Domain,
    RLikePolarity Polarity,
    RLikeNullCase NullCase,
    RLikeExecutionStrategy ExpectedStrategy,
    IReadOnlyDictionary<string, object?[][]>? CultureExpectedRows = null,
    IReadOnlyDictionary<string, object?>? Parameters = null,
    bool CacheSensitive = false)
{
    public object?[][] GetExpectedRows(CultureInfo culture)
    {
        if (CultureExpectedRows is not null && CultureExpectedRows.TryGetValue(culture.Name, out var expected))
            return expected;

        return ExpectedRows;
    }
}

internal static class RLikeQueryConformanceCatalog
{
    public static IReadOnlyList<RLikeQueryConformanceCase> Cases { get; } =
        CreatePatternCases().Concat(CreateSourceCases()).Concat(CreateUnicodeCases()).Concat(CreateNullCases()).ToArray();

    public static IReadOnlyList<RLikeQueryContextCoverage> ContextCoverage { get; } =
    [
        new("context-projection", RLikeQueryContext.Projection, RLikeExecutionStrategy.Direct),
        new("context-where", RLikeQueryContext.Where, RLikeExecutionStrategy.Prepared),
        new("context-boolean", RLikeQueryContext.BooleanComposition, RLikeExecutionStrategy.Dynamic),
        new("context-case", RLikeQueryContext.CaseWhen, RLikeExecutionStrategy.Dynamic),
        new("context-inner-join", RLikeQueryContext.InnerJoin, RLikeExecutionStrategy.Dynamic),
        new("context-left-join", RLikeQueryContext.LeftJoin, RLikeExecutionStrategy.Dynamic),
        new("context-cross-apply", RLikeQueryContext.CrossApply, RLikeExecutionStrategy.Dynamic, RLikePatternSource.InnerApplyColumn),
        new("context-outer-apply", RLikeQueryContext.OuterApply, RLikeExecutionStrategy.Prepared),
        new("context-aggregate-filter", RLikeQueryContext.AggregateFilter, RLikeExecutionStrategy.Prepared),
        new("context-having", RLikeQueryContext.Having, RLikeExecutionStrategy.Prepared),
        new("context-qualify", RLikeQueryContext.Qualify, RLikeExecutionStrategy.Direct),
        new("context-window", RLikeQueryContext.WindowFilter, RLikeExecutionStrategy.Prepared),
        new("context-cte", RLikeQueryContext.Cte, RLikeExecutionStrategy.Dynamic),
        new("context-recursive", RLikeQueryContext.RecursiveCte, RLikeExecutionStrategy.Direct),
        new("context-set", RLikeQueryContext.SetBranch, RLikeExecutionStrategy.Direct),
        new("context-page", RLikeQueryContext.DistinctOrderPage, RLikeExecutionStrategy.Direct),
        new("context-quantifier", RLikeQueryContext.PredicateQuantifier, RLikeExecutionStrategy.Prepared),
        new("context-parallel", RLikeQueryContext.Parallel, RLikeExecutionStrategy.Dynamic)
    ];

    private static IEnumerable<RLikeQueryConformanceCase> CreatePatternCases()
    {
        yield return Literal("literal", "before-Alpha-after", "Alpha", true, RLikePatternFamily.Literal, RLikeExecutionStrategy.Direct);
        yield return Literal("absolute-prefix", "Alphabet", @"\AAlpha", true, RLikePatternFamily.AbsolutePrefix, RLikeExecutionStrategy.Direct);
        yield return Literal("absolute-suffix", "Alphabet", @"bet\z", true, RLikePatternFamily.AbsoluteSuffix, RLikeExecutionStrategy.Direct);
        yield return Literal("absolute-exact", "Alpha", @"\AAlpha\z", true, RLikePatternFamily.AbsoluteExact, RLikeExecutionStrategy.Direct);
        yield return Literal("empty", "Alpha", string.Empty, true, RLikePatternFamily.Empty, RLikeExecutionStrategy.Direct);
        yield return Literal("dot", "Alpha", "Al.ha", true, RLikePatternFamily.Dot, RLikeExecutionStrategy.Prepared);
        yield return Literal("character-class", "Alpha", "[A-Z]lpha", true, RLikePatternFamily.CharacterClass, RLikeExecutionStrategy.Prepared);
        yield return Literal("quantifier", "Alpha", "Al.+a", true, RLikePatternFamily.Quantifier, RLikeExecutionStrategy.Prepared);
        yield return Literal("alternation", "Beta", "Alpha|Beta", true, RLikePatternFamily.Alternation, RLikeExecutionStrategy.Prepared);
        yield return Literal("group", "Alpha", "(?:Al)pha", true, RLikePatternFamily.Group, RLikeExecutionStrategy.Prepared);
        yield return Literal("inline-options", "ALPHA", "(?i)alpha", true, RLikePatternFamily.InlineOptions, RLikeExecutionStrategy.Prepared);
        yield return Literal("lookaround", "AlphaBeta", "Alpha(?=Beta)", true, RLikePatternFamily.Lookaround, RLikeExecutionStrategy.Prepared);
        yield return Literal("backreference", "AlAl", @"\A(Al)\1\z", true, RLikePatternFamily.Backreference, RLikeExecutionStrategy.Prepared);
        yield return Literal("unicode-category", "Łódź", @"\A\p{L}+\z", true, RLikePatternFamily.UnicodeCategory, RLikeExecutionStrategy.Prepared, RLikeCharacterDomain.Unicode);
        yield return Literal("escaped-literal", "a.b", @"\Aa\.b\z", true, RLikePatternFamily.EscapedLiteral, RLikeExecutionStrategy.Prepared);
        yield return Literal("multiline", "first\nsecond", "(?m)^second$", true, RLikePatternFamily.Multiline, RLikeExecutionStrategy.Prepared);
    }

    private static IEnumerable<RLikeQueryConformanceCase> CreateSourceCases()
    {
        yield return Dynamic("source-column", "Name rlike City", new BasicEntity { Id = 1, Name = "Alpha", City = @"\AAlpha\z" }, true, RLikePatternSource.SameRowColumn);
        yield return new RLikeQueryConformanceCase(
            "source-parameter",
            "param(pattern: string) select Id, Name, Name rlike $pattern as Matched from #A.Entities() order by Id",
            [Row(1, "Alpha")], [[1, "Alpha", true]], RLikePatternFamily.AbsolutePrefix,
            RLikePatternSource.Parameter, RLikeCharacterDomain.Ascii, RLikePolarity.Positive,
            RLikeNullCase.Neither, RLikeExecutionStrategy.Prepared,
            Parameters: new Dictionary<string, object?> { ["pattern"] = @"\AAl" }, CacheSensitive: true);
        yield return new RLikeQueryConformanceCase(
            "source-variable",
            "let pattern: string = r'\\AAl'; select Id, Name, Name rlike $pattern as Matched from #A.Entities() order by Id",
            [Row(1, "Alpha")], [[1, "Alpha", true]], RLikePatternFamily.AbsolutePrefix,
            RLikePatternSource.Variable, RLikeCharacterDomain.Ascii, RLikePolarity.Positive,
            RLikeNullCase.Neither, RLikeExecutionStrategy.Prepared, CacheSensitive: true);
        yield return Dynamic(
            "source-concatenation",
            "Name rlike (City + Country)",
            new BasicEntity { Id = 1, Name = "Alpha", City = @"\AAl", Country = "pha" },
            true,
            RLikePatternSource.Concatenation);
        yield return Dynamic(
            "source-method",
            "Name rlike ToUpper(City)",
            new BasicEntity { Id = 1, Name = "ALPHA", City = "alpha" },
            true,
            RLikePatternSource.MethodResult,
            RLikePatternFamily.Literal);
        yield return Dynamic(
            "source-case",
            "Name rlike case when Id = 1 then City else Country end",
            new BasicEntity { Id = 1, Name = "Alpha", City = @"\AAlpha\z", Country = "never" },
            true,
            RLikePatternSource.CaseExpression);
        yield return Dynamic(
            "source-coalesce",
            "Name rlike Coalesce(City, Country)",
            new BasicEntity { Id = 1, Name = "Alpha", City = null, Country = @"\AAlpha\z" },
            true,
            RLikePatternSource.CoalesceExpression);
    }

    private static IEnumerable<RLikeQueryConformanceCase> CreateUnicodeCases()
    {
        yield return Literal("unicode-literal", "Zażółć", "żół", true, RLikePatternFamily.Literal, RLikeExecutionStrategy.Direct, RLikeCharacterDomain.Unicode);
        yield return Dynamic("unicode-decomposed", "Name rlike City", new BasicEntity { Id = 1, Name = "é", City = "e\u0301" }, false, RLikePatternSource.SameRowColumn, RLikePatternFamily.Literal, RLikeCharacterDomain.ComposedAndDecomposed);
        yield return Dynamic("unicode-surrogate", "Name rlike City", new BasicEntity { Id = 1, Name = "\uD800x", City = "\uD800" }, true, RLikePatternSource.SameRowColumn, RLikePatternFamily.Literal, RLikeCharacterDomain.SurrogateContaining);
        yield return CultureSensitive("culture-inline-i", "I", @"(?i)\Ai\z", true, true, true, false);
        yield return CultureSensitive("culture-inline-dotted-i", "İ", @"(?i)\Ai\z", false, true, true, true);
    }

    private static IEnumerable<RLikeQueryConformanceCase> CreateNullCases()
    {
        yield return Dynamic("null-input", "Name rlike City", new BasicEntity { Id = 1, Name = null, City = "[" }, false, RLikePatternSource.SameRowColumn, nullCase: RLikeNullCase.Input);
        yield return Dynamic("null-pattern", "Name rlike City", new BasicEntity { Id = 1, Name = "Alpha", City = null }, false, RLikePatternSource.SameRowColumn, nullCase: RLikeNullCase.Pattern);
        yield return Dynamic("null-both", "Name rlike City", new BasicEntity { Id = 1, Name = null, City = null }, false, RLikePatternSource.SameRowColumn, nullCase: RLikeNullCase.Both);
        yield return Dynamic("null-input-negated", "Name not rlike City", new BasicEntity { Id = 1, Name = null, City = "[" }, true, RLikePatternSource.SameRowColumn, polarity: RLikePolarity.Negated, nullCase: RLikeNullCase.Input);
        yield return Dynamic("null-pattern-negated", "Name not rlike City", new BasicEntity { Id = 1, Name = "Alpha", City = null }, true, RLikePatternSource.SameRowColumn, polarity: RLikePolarity.Negated, nullCase: RLikeNullCase.Pattern);
        yield return Dynamic("null-both-negated", "Name not rlike City", new BasicEntity { Id = 1, Name = null, City = null }, true, RLikePatternSource.SameRowColumn, polarity: RLikePolarity.Negated, nullCase: RLikeNullCase.Both);
    }

    private static RLikeQueryConformanceCase Literal(
        string id,
        string input,
        string pattern,
        bool expected,
        RLikePatternFamily family,
        RLikeExecutionStrategy strategy,
        RLikeCharacterDomain domain = RLikeCharacterDomain.Ascii)
    {
        var escapedPattern = pattern.Replace("'", "''", StringComparison.Ordinal);
        return new RLikeQueryConformanceCase(
            id,
            $"select Id, Name, Name rlike r'{escapedPattern}' as Matched from #A.Entities() order by Id",
            [Row(1, input)], [[1, input, expected]], family, RLikePatternSource.Literal,
            domain, RLikePolarity.Positive, RLikeNullCase.Neither, strategy, CacheSensitive: true);
    }

    private static RLikeQueryConformanceCase Dynamic(
        string id,
        string predicate,
        BasicEntity row,
        bool expected,
        RLikePatternSource source,
        RLikePatternFamily family = RLikePatternFamily.AbsoluteExact,
        RLikeCharacterDomain domain = RLikeCharacterDomain.Ascii,
        RLikePolarity polarity = RLikePolarity.Positive,
        RLikeNullCase nullCase = RLikeNullCase.Neither) => new(
        id,
        $"select Id, Name, {predicate} as Matched from #A.Entities() order by Id",
        [row], [[row.Id, row.Name, expected]], family, source, domain, polarity, nullCase,
        RLikeExecutionStrategy.Dynamic, CacheSensitive: true);

    private static RLikeQueryConformanceCase CultureSensitive(
        string id,
        string input,
        string pattern,
        bool invariant,
        bool enUs,
        bool plPl,
        bool trTr) => new(
        id,
        $"select Id, Name, Name rlike r'{pattern}' as Matched from #A.Entities() order by Id",
        [Row(1, input)], [[1, input, invariant]], RLikePatternFamily.InlineOptions,
        RLikePatternSource.Literal, RLikeCharacterDomain.CultureSensitive, RLikePolarity.Positive,
        RLikeNullCase.Neither, RLikeExecutionStrategy.Prepared,
        new Dictionary<string, object?[][]>(StringComparer.Ordinal)
        {
            [string.Empty] = [[1, input, invariant]],
            ["en-US"] = [[1, input, enUs]],
            ["pl-PL"] = [[1, input, plPl]],
            ["tr-TR"] = [[1, input, trTr]]
        },
        CacheSensitive: true);

    private static BasicEntity Row(int id, string? name) => new() { Id = id, Name = name };
}
