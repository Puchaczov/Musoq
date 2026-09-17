using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

internal enum LikePatternShape
{
    Exact, Prefix, Suffix, Contains, MatchAll, RepeatedBoundaryPercent, Empty,
    SingleCharacter, MixedWildcards, InteriorPercent, RegexMetacharacters, RawBackslashes, Multiline
}

internal enum LikePatternSource
{
    Literal, SameRowColumn, Parameter, Variable, Concatenation, MethodResult,
    CaseExpression, CoalesceExpression, InnerApplyColumn
}

internal enum LikeCharacterDomain
{
    Ascii, Unicode, UnicodeCaseFoldingDivergence, ComposedAndDecomposed, SurrogateContaining
}

internal enum LikePolarity { Positive, Negated }

internal enum LikeNullCase { Neither, Input, Pattern, Both }

internal enum LikeQueryContext
{
    Projection, Where, BooleanComposition, CaseWhen, InnerJoin, LeftJoin, CrossApply, OuterApply,
    AggregateFilter, Having, Qualify, WindowFilter, Cte, RecursiveCte, SetBranch,
    DistinctOrderPage, PredicateQuantifier, Parallel
}

internal enum LikeExecutionStrategy { Direct, Prepared, Dynamic }

internal sealed record LikeQueryContextCoverage(
    string Id,
    LikeQueryContext Context,
    LikeExecutionStrategy Strategy,
    LikePatternSource? PatternSource = null);

internal sealed record LikeQueryConformanceCase(
    string Id,
    string BehaviorFamily,
    string Query,
    BasicEntity[] Rows,
    (string Name, Type Type)[] ExpectedColumns,
    object?[][] ExpectedRows,
    LikePatternShape Shape,
    LikePatternSource Source,
    LikeCharacterDomain Domain,
    LikePolarity Polarity,
    LikeNullCase NullCase,
    LikeQueryContext Context,
    LikeExecutionStrategy ExpectedStrategy,
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

internal static class LikeQueryConformanceCatalog
{
    private static readonly (string Name, Type Type)[] ProjectionColumns =
    [
        ("Id", typeof(int)),
        ("Name", typeof(string)),
        ("Matched", typeof(bool))
    ];

    public static IReadOnlyList<LikeQueryConformanceCase> Cases { get; } =
        CreateShapeCases().Concat(CreateSourceCases()).Concat(CreateUnicodeCases()).Concat(CreateNullCases()).ToArray();

    public static IReadOnlyList<LikeQueryContextCoverage> ContextCoverage { get; } =
    [
        new("context-projection", LikeQueryContext.Projection, LikeExecutionStrategy.Direct),
        new("context-where", LikeQueryContext.Where, LikeExecutionStrategy.Direct),
        new("context-boolean", LikeQueryContext.BooleanComposition, LikeExecutionStrategy.Dynamic),
        new("context-case", LikeQueryContext.CaseWhen, LikeExecutionStrategy.Dynamic),
        new("context-inner-join", LikeQueryContext.InnerJoin, LikeExecutionStrategy.Dynamic),
        new("context-left-join", LikeQueryContext.LeftJoin, LikeExecutionStrategy.Dynamic),
        new("context-cross-apply", LikeQueryContext.CrossApply, LikeExecutionStrategy.Dynamic, LikePatternSource.InnerApplyColumn),
        new("context-outer-apply", LikeQueryContext.OuterApply, LikeExecutionStrategy.Dynamic),
        new("context-aggregate-filter", LikeQueryContext.AggregateFilter, LikeExecutionStrategy.Prepared),
        new("context-having", LikeQueryContext.Having, LikeExecutionStrategy.Prepared),
        new("context-qualify", LikeQueryContext.Qualify, LikeExecutionStrategy.Direct),
        new("context-window-filter", LikeQueryContext.WindowFilter, LikeExecutionStrategy.Direct),
        new("context-cte", LikeQueryContext.Cte, LikeExecutionStrategy.Dynamic),
        new("context-recursive-cte", LikeQueryContext.RecursiveCte, LikeExecutionStrategy.Direct),
        new("context-set-branch", LikeQueryContext.SetBranch, LikeExecutionStrategy.Direct),
        new("context-distinct-order-page", LikeQueryContext.DistinctOrderPage, LikeExecutionStrategy.Direct),
        new("context-quantifier", LikeQueryContext.PredicateQuantifier, LikeExecutionStrategy.Direct),
        new("context-parallel", LikeQueryContext.Parallel, LikeExecutionStrategy.Dynamic)
    ];

    private static IEnumerable<LikeQueryConformanceCase> CreateShapeCases()
    {
        yield return Literal("shape-exact", "Alpha", "Alpha", true, LikePatternShape.Exact, LikeExecutionStrategy.Direct);
        yield return Literal("shape-prefix", "Alphabet", "Al%", true, LikePatternShape.Prefix, LikeExecutionStrategy.Direct);
        yield return Literal("shape-suffix", "Alphabet", "%bet", true, LikePatternShape.Suffix, LikeExecutionStrategy.Direct);
        yield return Literal("shape-contains", "Alphabet", "%pha%", true, LikePatternShape.Contains, LikeExecutionStrategy.Direct);
        yield return Literal("shape-match-all", string.Empty, "%", true, LikePatternShape.MatchAll, LikeExecutionStrategy.Direct);
        yield return Literal("shape-repeated-percent", "Alpha", "%%%Alpha%%", true, LikePatternShape.RepeatedBoundaryPercent, LikeExecutionStrategy.Direct);
        yield return Literal("shape-empty", string.Empty, string.Empty, true, LikePatternShape.Empty, LikeExecutionStrategy.Direct);
        yield return Literal("shape-single-character", "Alpha", "Al_ha", true, LikePatternShape.SingleCharacter, LikeExecutionStrategy.Prepared);
        yield return Literal("shape-mixed-wildcards", "Alpha", "A_%a", true, LikePatternShape.MixedWildcards, LikeExecutionStrategy.Prepared);
        yield return Literal("shape-interior-percent", "Alphabet", "A%ha%et", true, LikePatternShape.InteriorPercent, LikeExecutionStrategy.Prepared);
        yield return Literal("shape-regex-metacharacters", "a.c$^[x](y)*+?|", "a.c$^[x](y)*+?|", true, LikePatternShape.RegexMetacharacters, LikeExecutionStrategy.Direct);
        yield return Literal("shape-raw-backslashes", @"C:\temp\file.txt", @"C:\temp\%", true, LikePatternShape.RawBackslashes, LikeExecutionStrategy.Direct, rawPattern: true);
        yield return Literal("shape-multiline", "line1\nline2", "line1\nline2", true, LikePatternShape.Multiline, LikeExecutionStrategy.Direct);
    }

    private static IEnumerable<LikeQueryConformanceCase> CreateSourceCases()
    {
        yield return Dynamic("source-same-row", "select Id, Name, Name like City as Matched from #A.Entities() order by Id", new BasicEntity { Id = 1, Name = "Alpha", City = "Al%" }, true, LikePatternSource.SameRowColumn);
        yield return new LikeQueryConformanceCase(
            "source-parameter", "pattern-source",
            "param(pattern: string) select Id, Name, Name like $pattern as Matched from #A.Entities() order by Id",
            [new BasicEntity { Id = 1, Name = "Alpha" }], ProjectionColumns, [[1, "Alpha", true]],
            LikePatternShape.Prefix, LikePatternSource.Parameter, LikeCharacterDomain.Ascii,
            LikePolarity.Positive, LikeNullCase.Neither, LikeQueryContext.Projection, LikeExecutionStrategy.Prepared,
            Parameters: new Dictionary<string, object?> { ["pattern"] = "Al%" }, CacheSensitive: true);
        yield return new LikeQueryConformanceCase(
            "source-variable", "pattern-source",
            "let pattern: string = 'Al%'; select Id, Name, Name like $pattern as Matched from #A.Entities() order by Id",
            [new BasicEntity { Id = 1, Name = "Alpha" }], ProjectionColumns, [[1, "Alpha", true]],
            LikePatternShape.Prefix, LikePatternSource.Variable, LikeCharacterDomain.Ascii,
            LikePolarity.Positive, LikeNullCase.Neither, LikeQueryContext.Projection, LikeExecutionStrategy.Prepared,
            CacheSensitive: true);
        yield return new LikeQueryConformanceCase(
            "source-concatenation", "pattern-source",
            "let prefix: string = 'Al'; select Id, Name, Name like ($prefix + '%') as Matched from #A.Entities() order by Id",
            [new BasicEntity { Id = 1, Name = "Alpha" }], ProjectionColumns, [[1, "Alpha", true]],
            LikePatternShape.Prefix, LikePatternSource.Concatenation, LikeCharacterDomain.Ascii,
            LikePolarity.Positive, LikeNullCase.Neither, LikeQueryContext.Projection, LikeExecutionStrategy.Dynamic,
            CacheSensitive: true);
        yield return Dynamic("source-method-result", "select Id, Name, Name like ToUpper(City) as Matched from #A.Entities() order by Id", new BasicEntity { Id = 1, Name = "ALPHA", City = "alpha" }, true, LikePatternSource.MethodResult, shape: LikePatternShape.Exact);
        yield return Dynamic("source-case-expression", "select Id, Name, Name like case when Id = 1 then City else Country end as Matched from #A.Entities() order by Id", new BasicEntity { Id = 1, Name = "Alpha", City = "Al%", Country = "no%" }, true, LikePatternSource.CaseExpression);
        yield return Dynamic("source-coalesce-expression", "select Id, Name, Name like Coalesce(City, Country) as Matched from #A.Entities() order by Id", new BasicEntity { Id = 1, Name = "Alpha", City = null, Country = "Al%" }, true, LikePatternSource.CoalesceExpression);
    }

    private static IEnumerable<LikeQueryConformanceCase> CreateUnicodeCases()
    {
        yield return Literal("unicode-ordinary", "Łódź", "Łódź", true, LikePatternShape.Exact, LikeExecutionStrategy.Prepared, LikeCharacterDomain.Unicode);
        yield return Literal("unicode-kelvin", "K", "K", true, LikePatternShape.Exact, LikeExecutionStrategy.Direct, LikeCharacterDomain.UnicodeCaseFoldingDivergence, true);
        yield return CultureSensitive("unicode-dotted-i", "İ", "I", false, true, true, false);
        yield return CultureSensitive("unicode-dotless-i", "ı", "I", false, false, false, true);
        yield return Literal("unicode-sigma-forms", "ς", "Σ", false, LikePatternShape.Exact, LikeExecutionStrategy.Prepared, LikeCharacterDomain.UnicodeCaseFoldingDivergence, true);
        yield return Dynamic("unicode-composed-decomposed", "select Id, Name, Name like City as Matched from #A.Entities() order by Id", new BasicEntity { Id = 1, Name = "é", City = "e\u0301" }, false, LikePatternSource.SameRowColumn, LikeCharacterDomain.ComposedAndDecomposed, LikePatternShape.Exact);
        yield return Dynamic("unicode-surrogate-containing", "select Id, Name, Name like City as Matched from #A.Entities() order by Id", new BasicEntity { Id = 1, Name = "\uD800x", City = "\uD800_" }, true, LikePatternSource.SameRowColumn, LikeCharacterDomain.SurrogateContaining, LikePatternShape.SingleCharacter);
    }

    private static IEnumerable<LikeQueryConformanceCase> CreateNullCases()
    {
        const string like = "select Id, Name, Name like City as Matched from #A.Entities() order by Id";
        const string notLike = "select Id, Name, Name not like City as Matched from #A.Entities() order by Id";
        yield return Dynamic("null-input-like", like, new BasicEntity { Id = 1, Name = null, City = "A%" }, false, LikePatternSource.SameRowColumn, nullCase: LikeNullCase.Input);
        yield return Dynamic("null-pattern-like", like, new BasicEntity { Id = 1, Name = "Alpha", City = null }, false, LikePatternSource.SameRowColumn, nullCase: LikeNullCase.Pattern);
        yield return Dynamic("null-both-like", like, new BasicEntity { Id = 1, Name = null, City = null }, false, LikePatternSource.SameRowColumn, nullCase: LikeNullCase.Both);
        yield return Dynamic("null-input-not-like", notLike, new BasicEntity { Id = 1, Name = null, City = "A%" }, true, LikePatternSource.SameRowColumn, polarity: LikePolarity.Negated, nullCase: LikeNullCase.Input);
        yield return Dynamic("null-pattern-not-like", notLike, new BasicEntity { Id = 1, Name = "Alpha", City = null }, true, LikePatternSource.SameRowColumn, polarity: LikePolarity.Negated, nullCase: LikeNullCase.Pattern);
        yield return Dynamic("null-both-not-like", notLike, new BasicEntity { Id = 1, Name = null, City = null }, true, LikePatternSource.SameRowColumn, polarity: LikePolarity.Negated, nullCase: LikeNullCase.Both);
    }

    private static LikeQueryConformanceCase Literal(string id, string input, string pattern, bool expected,
        LikePatternShape shape, LikeExecutionStrategy strategy, LikeCharacterDomain domain = LikeCharacterDomain.Ascii,
        bool cacheSensitive = false, bool rawPattern = false)
    {
        var escapedPattern = pattern.Replace("'", "''", StringComparison.Ordinal);
        var literalPrefix = rawPattern ? "r" : string.Empty;
        return new LikeQueryConformanceCase(
            id, "pattern-shape", $"select Id, Name, Name like {literalPrefix}'{escapedPattern}' as Matched from #A.Entities() order by Id",
            [new BasicEntity { Id = 1, Name = input }], ProjectionColumns, [[1, input, expected]], shape,
            LikePatternSource.Literal, domain, LikePolarity.Positive, LikeNullCase.Neither,
            LikeQueryContext.Projection, strategy, CacheSensitive: cacheSensitive);
    }

    private static LikeQueryConformanceCase Dynamic(string id, string query, BasicEntity row, bool expected,
        LikePatternSource source, LikeCharacterDomain domain = LikeCharacterDomain.Ascii,
        LikePatternShape shape = LikePatternShape.Prefix, LikePolarity polarity = LikePolarity.Positive,
        LikeNullCase nullCase = LikeNullCase.Neither)
    {
        return new LikeQueryConformanceCase(
            id, "pattern-source", query, [row], ProjectionColumns, [[row.Id, row.Name, expected]], shape,
            source, domain, polarity, nullCase, LikeQueryContext.Projection, LikeExecutionStrategy.Dynamic,
            CacheSensitive: true);
    }

    private static LikeQueryConformanceCase CultureSensitive(string id, string input, string pattern,
        bool invariant, bool enUs, bool plPl, bool trTr)
    {
        return new LikeQueryConformanceCase(
            id, "unicode-culture", $"select Id, Name, Name like '{pattern}' as Matched from #A.Entities() order by Id",
            [new BasicEntity { Id = 1, Name = input }], ProjectionColumns, [[1, input, invariant]],
            LikePatternShape.Exact, LikePatternSource.Literal, LikeCharacterDomain.UnicodeCaseFoldingDivergence,
            LikePolarity.Positive, LikeNullCase.Neither, LikeQueryContext.Projection, LikeExecutionStrategy.Direct,
            new Dictionary<string, object?[][]>(StringComparer.Ordinal)
            {
                [string.Empty] = [[1, input, invariant]], ["en-US"] = [[1, input, enUs]],
                ["pl-PL"] = [[1, input, plPl]], ["tr-TR"] = [[1, input, trTr]]
            }, CacheSensitive: true);
    }
}
