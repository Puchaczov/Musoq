using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tests.Schema.RecursiveCte;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public sealed record RecursiveCteExpectedColumn(string Name, Type ClrType);

public sealed record RecursiveCteSupportedCase(
    string Name,
    IReadOnlySet<string> FactorTags,
    string Query,
    CompilationOptions CompilationOptions,
    IReadOnlyList<RecursiveCteExpectedColumn> ExpectedColumns,
    IReadOnlyList<object?[]> ExpectedRows,
    bool Ordered,
    string? GeneratedSampleName,
    Func<ISchemaProvider>? CreateSchemaProvider = null);

internal static partial class RecursiveCteSupportedCaseCatalog
{
    public static IReadOnlyList<RecursiveCteSupportedCase> Cases { get; } =
    [
        .. CreateFocusedCases(),
        .. CreateMixedCases(),
        .. CreateHardeningCases()
    ];

    private static IReadOnlyList<RecursiveCteSupportedCase> CreateFocusedCases() =>
    [
        new(
            "union-all-counter",
            Tags("union-all", "self-only", "predicate-termination", "single-root", "chain", "values"),
            "with recursive counter (Value) as (" +
            "select Value from values {{ Value: 1 }} seed " +
            "union all " +
            "select c.Value + 1 from counter c where c.Value < 4) " +
            "select Value from counter order by Value",
            new CompilationOptions(),
            [new("Value", typeof(int))],
            [[1], [2], [3], [4]],
            true,
            "Q188_RecursiveUnionAllCounter"),
        new(
            "union-all-predicate-termination",
            Tags("union-all", "state-depth", "scalar-expression", "predicate-termination"),
            "with recursive powers (Value, Depth) as (" +
            "select Value, 0 from values {{ Value: 2 }} seed " +
            "union all " +
            "select p.Value * 2, p.Depth + 1 from powers p where p.Depth < 3) " +
            "select Value, Depth from powers order by Depth",
            new CompilationOptions(),
            [new("Value", typeof(int)), new("Depth", typeof(int))],
            [[2, 0], [4, 1], [8, 2], [16, 3]],
            true,
            "Q189_RecursiveUnionAllPredicateTermination"),
        new(
            "union-all-empty-anchor",
            Tags("union-all", "empty-anchor", "empty-frontier"),
            "with recursive counter (Value) as (" +
            "select Value from values {{ Value: 1 }} seed where Value < 0 " +
            "union all " +
            "select c.Value + 1 from counter c where c.Value < 4) " +
            "select Value from counter order by Value",
            new CompilationOptions(),
            [new("Value", typeof(int))],
            [],
            true,
            "Q190_RecursiveEmptyAnchor"),
        new(
            "union-all-multiple-roots",
            Tags("union-all", "multiple-roots", "breadth-first"),
            "with recursive counter (Value) as (" +
            "select Value from values {{ Value: 1 }, { Value: 10 }} seed " +
            "union all " +
            "select c.Value + 1 from counter c where c.Value < 3) " +
            "select Value from counter order by Value",
            new CompilationOptions(),
            [new("Value", typeof(int))],
            [[1], [2], [3], [10]],
            true,
            "Q191_RecursiveMultipleRoots"),
        new(
            "union-full-row-cycle",
            Tags("union", "full-row", "cycle", "dedup-termination", "values"),
            "with recursive cycle (Id) as (" +
            "select Id from values {{ Id: 1 }} seed " +
            "union " +
            "select (case when c.Id = 1 then 2 else 1 end) from cycle c) " +
            "select Id from cycle order by Id",
            new CompilationOptions(),
            [new("Id", typeof(int))],
            [[1], [2]],
            true,
            "Q192_RecursiveUnionFullRowCycle"),
        new(
            "union-single-key-cycle",
            Tags("union-keyed", "single-key", "cycle", "dedup-termination"),
            "with recursive cycle (Id) as (" +
            "select Id from values {{ Id: 1 }} seed " +
            "union (Id) " +
            "select (case when c.Id = 1 then 2 else 1 end) from cycle c) " +
            "select Id from cycle order by Id",
            new CompilationOptions(),
            [new("Id", typeof(int))],
            [[1], [2]],
            true,
            "Q193_RecursiveUnionSingleKeyCycle"),
        new(
            "union-composite-key",
            Tags("union-keyed", "composite-key", "cycle", "state-depth"),
            "with recursive states (Id, Branch, Depth) as (" +
            "select Id, Branch, 0 from values {{ Id: 1, Branch: 'A' }} seed " +
            "union (Id, Branch) " +
            "select (case when s.Id = 1 then 2 else 1 end), s.Branch, s.Depth + 1 from states s) " +
            "select Id, Branch, Depth from states order by Id",
            new CompilationOptions(),
            [new("Id", typeof(int)), new("Branch", typeof(string)), new("Depth", typeof(int))],
            [[1, "A", 0], [2, "A", 1]],
            true,
            "Q194_RecursiveUnionCompositeKey"),
        new(
            "union-keyed-non-key-payload",
            Tags("union-keyed", "single-key", "path-payload", "first-representative"),
            "with recursive paths (Id, Depth, Path) as (" +
            "select Id, 0, '1' from values {{ Id: 1 }} seed " +
            "union (Id) " +
            "select (case when p.Id = 1 then 2 else 1 end), p.Depth + 1, p.Path + '->' + (case when p.Id = 1 then '2' else '1' end) from paths p) " +
            "select Id, Depth, Path from paths order by Id",
            new CompilationOptions(),
            [new("Id", typeof(int)), new("Depth", typeof(int)), new("Path", typeof(string))],
            [[1, 0, "1"], [2, 1, "1->2"]],
            true,
            "Q195_RecursiveKeyedNonKeyPayload"),
        new(
            "union-anchor-duplicates",
            Tags("union", "full-row", "anchor-duplicates"),
            "with recursive numbers (Id) as (" +
            "select Id from values {{ Id: 1 }, { Id: 1 }} seed " +
            "union " +
            "select n.Id + 1 from numbers n where n.Id < 2) " +
            "select Id from numbers order by Id",
            new CompilationOptions(),
            [new("Id", typeof(int))],
            [[1], [2]],
            true,
            "Q196_RecursiveAnchorDuplicates"),
        new(
            "union-duplicate-generated-rows",
            Tags("union", "full-row", "duplicate-edge", "multiple-roots", "same-generation-duplicates"),
            "with recursive walk (Id) as (" +
            "select Id from values {{ Id: 1 }, { Id: 2 }} seed " +
            "union " +
            "select 3 from walk w where w.Id < 3) " +
            "select Id from walk order by Id",
            new CompilationOptions(),
            [new("Id", typeof(int))],
            [[1], [2], [3]],
            true,
            "Q197_RecursiveDuplicateEdges"),
        new(
            "union-full-row-self-loop",
            Tags("union", "full-row", "self-loop", "dedup-termination"),
            "with recursive loop (Id) as (" +
            "select Id from values {{ Id: 7 }} seed " +
            "union select l.Id from loop l) " +
            "select Id from loop",
            new CompilationOptions(),
            [new("Id", typeof(int))],
            [[7]],
            true,
            null),
        new(
            "union-nullable-composite-key",
            Tags("union-keyed", "composite-key", "nullable-key", "self-loop"),
            "with recursive states (Id, Code, Depth) as (" +
            "select Id, Code, 0 from values {{ Id: 1, Code: null }, { Id: 2, Code: 'x' }} seed " +
            "union (Id, Code) select s.Id, s.Code, s.Depth + 1 from states s where s.Depth < 1) " +
            "select Id, Code, Depth from states order by Id",
            new CompilationOptions(),
            [new("Id", typeof(int)), new("Code", typeof(string)), new("Depth", typeof(int))],
            [[1, null, 0], [2, "x", 0]],
            true,
            null),
        new(
            "inner-join-invariant-edges",
            Tags("union-keyed", "inner-join", "external-snapshot", "chain"),
            GraphJoinQuery("inner join #graph.edges() e on e.SourceId = r.Id"),
            new CompilationOptions(usePrimitiveTypeValidation: false),
            [new("Id", typeof(int)), new("Depth", typeof(int))],
            [[1, 0], [2, 1], [3, 2]],
            true,
            "Q198_RecursiveInnerJoinEdges",
            RecursiveGraphFixtures.CreateChainProvider),
        new(
            "cross-join-filter-invariant-edges",
            Tags("union-keyed", "cross-join", "filter", "external-snapshot", "chain"),
            GraphJoinQuery("cross join #graph.edges() e where e.SourceId = r.Id"),
            new CompilationOptions(usePrimitiveTypeValidation: false),
            [new("Id", typeof(int)), new("Depth", typeof(int))],
            [[1, 0], [2, 1], [3, 2]],
            true,
            "Q199_RecursiveCrossJoinFilter",
            RecursiveGraphFixtures.CreateChainProvider),
        new(
            "cross-apply-correlated-neighbors",
            Tags("union-keyed", "cross-apply", "correlated-source", "chain"),
            GraphJoinQuery("cross apply #graph.neighbors(r.Id) e"),
            new CompilationOptions(usePrimitiveTypeValidation: false),
            [new("Id", typeof(int)), new("Depth", typeof(int))],
            [[1, 0], [2, 1], [3, 2]],
            true,
            "Q200_RecursiveCrossApplyNeighbors",
            RecursiveGraphFixtures.CreateChainProvider),
        new(
            "outer-apply-correlated-neighbors",
            Tags("union-keyed", "outer-apply", "correlated-source", "empty-neighbors", "chain"),
            "with recursive reachable (Id, Depth) as (" +
            "select RootId, 0 from #graph.roots() " +
            "union (Id) " +
            "select (case when e.TargetId is null then r.Id else e.TargetId end), r.Depth + 1 " +
            "from reachable r outer apply #graph.neighbors(r.Id) e) " +
            "select Id, Depth from reachable order by Id",
            new CompilationOptions(usePrimitiveTypeValidation: false),
            [new("Id", typeof(int)), new("Depth", typeof(int))],
            [[1, 0], [2, 1], [3, 2]],
            true,
            "Q201_RecursiveOuterApplyNeighbors",
            RecursiveGraphFixtures.CreateChainProvider),
        new(
            "invariant-source-snapshot",
            Tags("union-keyed", "inner-join", "external-snapshot", "mutable-source", "cycle"),
            GraphJoinQuery("inner join #graph.edges() e on e.SourceId = r.Id"),
            new CompilationOptions(usePrimitiveTypeValidation: false),
            [new("Id", typeof(int)), new("Depth", typeof(int))],
            [[1, 0], [2, 1], [3, 2]],
            true,
            "Q202_RecursiveInvariantSourceSnapshot",
            RecursiveGraphFixtures.CreateCycleProvider),
        new(
            "invariant-hash-lookup",
            Tags("union-keyed", "inner-join", "external-snapshot", "invariant-index", "diamond"),
            GraphJoinQuery("inner join #graph.edges() e on e.SourceId = r.Id"),
            new CompilationOptions(usePrimitiveTypeValidation: false),
            [new("Id", typeof(int)), new("Depth", typeof(int))],
            [[1, 0], [2, 1], [3, 1], [4, 2]],
            true,
            "Q203_RecursiveInvariantHashLookup",
            RecursiveGraphFixtures.CreateDiamondProvider),
        new(
            "prior-values-cte-edges",
            Tags("union-keyed", "earlier-cte", "values", "inner-join", "chain"),
            "with recursive edges (SourceId, TargetId) as (" +
            "select SourceId, TargetId from values {{ SourceId: 1, TargetId: 2 }, { SourceId: 2, TargetId: 3 }} e), " +
            "reachable (Id, Depth) as (" +
            "select Id, 0 from values {{ Id: 1 }} seed " +
            "union (Id) " +
            "select e.TargetId, r.Depth + 1 from reachable r " +
            "inner join edges e on e.SourceId = r.Id) " +
            "select Id, Depth from reachable order by Id",
            new CompilationOptions(),
            [new("Id", typeof(int)), new("Depth", typeof(int))],
            [[1, 0], [2, 1], [3, 2]],
            true,
            "Q204_RecursivePriorValuesCteEdges"),
        new(
            "prior-materialized-cte",
            Tags("union-keyed", "earlier-cte", "materialized-cte", "external-source", "chain"),
            "with recursive edges (SourceId, TargetId) as (" +
            "select SourceId, TargetId from #graph.edges()), " +
            "reachable (Id, Depth) as (" +
            "select RootId, 0 from #graph.roots() " +
            "union (Id) select e.TargetId, r.Depth + 1 from reachable r " +
            "inner join edges e on e.SourceId = r.Id) " +
            "select Id, Depth from reachable order by Id",
            new CompilationOptions(usePrimitiveTypeValidation: false),
            [new("Id", typeof(int)), new("Depth", typeof(int))],
            [[1, 0], [2, 1], [3, 2]],
            true,
            "Q205_RecursivePriorMaterializedCte",
            RecursiveGraphFixtures.CreateChainProvider),
        new(
            "dependent-ordinary-cte",
            Tags("union-keyed", "ordinary-after-recursive", "outer-filter", "chain"),
            "with recursive reachable (Id, Depth) as (" +
            "select RootId, 0 from #graph.roots() union (Id) " +
            "select e.TargetId, r.Depth + 1 from reachable r " +
            "inner join #graph.edges() e on e.SourceId = r.Id), " +
            "deep (Id, Depth) as (select Id, Depth from reachable where Depth > 0) " +
            "select Id, Depth from deep",
            new CompilationOptions(usePrimitiveTypeValidation: false),
            [new("Id", typeof(int)), new("Depth", typeof(int))],
            [[2, 1], [3, 2]],
            false,
            "Q206_RecursiveDependentOrdinaryCte",
            RecursiveGraphFixtures.CreateChainProvider),
        new(
            "two-independent-recursive-ctes",
            Tags("union-all", "multiple-recursive", "independent-siblings", "outer-join"),
            "with recursive up (Value) as (" +
            "select Value from values {{ Value: 1 }} seed union all " +
            "select u.Value + 1 from up u where u.Value < 3), " +
            "down (Value) as (" +
            "select Value from values {{ Value: 5 }} seed union all " +
            "select d.Value - 1 from down d where d.Value > 3) " +
            "select u.Value as Up, d.Value as Down from up u " +
            "inner join down d on u.Value + d.Value = 6",
            new CompilationOptions(),
            [new("Up", typeof(int)), new("Down", typeof(int))],
            [[1, 5], [2, 4], [3, 3]],
            false,
            "Q207_RecursiveTwoIndependentCtes"),
        new(
            "recursive-depends-on-earlier-recursive",
            Tags("union-all", "multiple-recursive", "earlier-recursive-dependency", "chain"),
            "with recursive first (Value) as (" +
            "select Value from values {{ Value: 1 }} seed union all " +
            "select f.Value + 1 from first f where f.Value < 3), " +
            "second (Value) as (" +
            "select Value from first where Value = 2 union all " +
            "select s.Value + 1 from second s where s.Value < 4) " +
            "select Value from second order by Value",
            new CompilationOptions(),
            [new("Value", typeof(int))],
            [[2], [3], [4]],
            true,
            "Q208_RecursiveDependsOnEarlierRecursive"),
        new(
            "unused-recursive-definition",
            Tags("union-all", "unused-recursive", "dead-cte", "ordinary-live-cte"),
            "with recursive dead (Value) as (" +
            "select Value from values {{ Value: 1 }} seed union all " +
            "select d.Value + 1 from dead d where d.Value < 3), " +
            "live (Value) as (select Value from values {{ Value: 42 }} row) " +
            "select Value from live",
            new CompilationOptions(),
            [new("Value", typeof(int))],
            [[42]],
            true,
            "Q209_RecursiveUnusedDefinition"),
        new(
            "projection-pruned-recursive-state",
            Tags("union-keyed", "projection-pruning", "hidden-depth", "unused-payload"),
            "with recursive walk (Id, Depth, Path) as (" +
            "select Id, 0, 'root' from values {{ Id: 1 }} seed union (Id) " +
            "select w.Id + 1, w.Depth + 1, w.Path + '->next' from walk w where w.Depth < 2) " +
            "select Id from walk order by Id",
            new CompilationOptions(),
            [new("Id", typeof(int))],
            [[1], [2], [3]],
            true,
            "Q210_RecursiveProjectionPrunedState"),
        new(
            "full-row-identity-retains-hidden-payload",
            Tags("union", "full-row", "projection-pruning", "hidden-identity-state"),
            "with recursive states (Id, Version) as (" +
            "select Id, 0 from values {{ Id: 1 }} seed union " +
            "select s.Id, s.Version + 1 from states s where s.Version < 2) " +
            "select Id from states order by Id",
            new CompilationOptions(),
            [new("Id", typeof(int))],
            [[1], [1], [1]],
            true,
            null),
        new(
            "recursive-outer-filter-order",
            Tags("union-all", "outer-filter", "outer-order", "state-depth"),
            "with recursive walk (Id, Depth) as (" +
            "select Id, 0 from values {{ Id: 1 }} seed union all " +
            "select w.Id + 1, w.Depth + 1 from walk w where w.Depth < 3) " +
            "select Id, Depth from walk where Depth > 0 order by Depth desc",
            new CompilationOptions(),
            [new("Id", typeof(int)), new("Depth", typeof(int))],
            [[4, 3], [3, 2], [2, 1]],
            true,
            "Q211_RecursiveOuterFilterOrder"),
        new(
            "recursive-outer-join",
            Tags("union-all", "outer-join", "values", "state-depth"),
            "with recursive walk (Id) as (" +
            "select Id from values {{ Id: 1 }} seed union all " +
            "select w.Id + 1 from walk w where w.Id < 3) " +
            "select w.Id, l.Name from walk w inner join " +
            "values {{ Id: 1, Name: 'root' }, { Id: 2, Name: 'middle' }, { Id: 3, Name: 'leaf' }} l " +
            "on w.Id = l.Id",
            new CompilationOptions(),
            [new("w.Id", typeof(int)), new("l.Name", typeof(string))],
            [[1, "root"], [2, "middle"], [3, "leaf"]],
            false,
            "Q212_RecursiveOuterJoin"),
        new(
            "recursive-outer-aggregate",
            Tags("union-all", "outer-aggregate", "state-depth"),
            "with recursive walk (Id, Depth) as (" +
            "select Id, 0 from values {{ Id: 1 }} seed union all " +
            "select w.Id + 1, w.Depth + 1 from walk w where w.Depth < 3) " +
            "select Count(Id) as NodeCount, Max(Depth) as MaxDepth from walk",
            new CompilationOptions(),
            [new("NodeCount", typeof(long)), new("MaxDepth", typeof(int?))],
            [[4L, 3]],
            true,
            "Q213_RecursiveOuterAggregate"),
        new(
            "recursive-outer-window-and-set",
            Tags("union-all", "outer-window", "outer-set-operation", "row-number"),
            "with recursive walk (Id) as (" +
            "select Id from values {{ Id: 1 }} seed union all " +
            "select w.Id + 1 from walk w where w.Id < 3) " +
            "select Id, RowNumber() over (order by Id) as Ordinal from walk " +
            "union all select Id, RowNumber() over (order by Id) from walk where Id = 3",
            new CompilationOptions(),
            [new("Id", typeof(int)), new("Ordinal", typeof(long))],
            [[1, 1L], [2, 2L], [3, 3L], [3, 1L]],
            false,
            "Q214_RecursiveOuterWindowAndSet"),
        new(
            "recursive-nullable-columns",
            Tags("union-keyed", "nullable-value", "case", "state-depth"),
            "with recursive states (Id, ParentId, Depth) as (" +
            "select Id, ParentId, 0 from values {{ Id: 1, ParentId: null }, { Id: 0, ParentId: 1 }} seed " +
            "where Id = 1 union (Id) " +
            "select s.Id + 1, case when s.Id < 0 then null else s.Id end, s.Depth + 1 " +
            "from states s where s.Depth < 2) " +
            "select Id, ParentId, Depth from states order by Id",
            new CompilationOptions(),
            [new("Id", typeof(int)), new("ParentId", typeof(int?)), new("Depth", typeof(int))],
            [[1, null, 0], [2, 1, 1], [3, 2, 2]],
            true,
            "Q215_RecursiveNullableColumns"),
        new(
            "recursive-explicit-decimal-cast",
            Tags("union-all", "explicit-cast", "decimal", "anchor-derived-type"),
            "with recursive totals (Total, Depth) as (" +
            "select 0::Decimal, 0 from values {{ Seed: 1 }} seed union all " +
            "select (t.Total + 1)::Decimal, t.Depth + 1 from totals t where t.Depth < 2) " +
            "select Total, Depth from totals order by Depth",
            new CompilationOptions(),
            [new("Total", typeof(decimal?)), new("Depth", typeof(int))],
            [[0m, 0], [1m, 1], [2m, 2]],
            true,
            "Q216_RecursiveExplicitDecimalCast"),
        new(
            "recursive-case-and-scalar-expressions",
            Tags("union-all", "case", "scalar-expression", "string-state"),
            "with recursive labels (Value, Depth, Label) as (" +
            "select 1, 0, 'one' from values {{ Seed: 1 }} seed union all " +
            "select l.Value + 1, l.Depth + 1, " +
            "case when l.Value = 1 then 'even' else 'odd' end " +
            "from labels l where l.Depth < 2) " +
            "select Value, Depth, Label from labels order by Depth",
            new CompilationOptions(),
            [new("Value", typeof(int)), new("Depth", typeof(int)), new("Label", typeof(string))],
            [[1, 0, "one"], [2, 1, "even"], [3, 2, "odd"]],
            true,
            "Q217_RecursiveCaseAndScalarExpressions"),
        new(
            "recursive-wide-payload",
            Tags("union-keyed", "wide-row", "decimal", "string-state", "scalar-expression"),
            "with recursive wide (Id, Depth, A, B, C, D, E, F, Name, Flag, Amount, Code) as (" +
            "select 1, 0, 10, 20, 30, 40, 50, 60, 'row', true, 1::Decimal, 'x' " +
            "from values {{ Seed: 1 }} seed union (Id) " +
            "select w.Id + 1, w.Depth + 1, w.A, w.B, w.C, w.D, w.E, w.F, " +
            "w.Name + 'x', w.Flag, (w.Amount + 1)::Decimal, w.Code " +
            "from wide w where w.Depth < 2) " +
            "select Id, Depth, A, B, C, D, E, F, Name, Flag, Amount, Code from wide order by Id",
            new CompilationOptions(),
            [
                new("Id", typeof(int)), new("Depth", typeof(int)), new("A", typeof(int)),
                new("B", typeof(int)), new("C", typeof(int)), new("D", typeof(int)),
                new("E", typeof(int)), new("F", typeof(int)), new("Name", typeof(string)),
                new("Flag", typeof(bool)), new("Amount", typeof(decimal?)), new("Code", typeof(string))
            ],
            [
                [1, 0, 10, 20, 30, 40, 50, 60, "row", true, 1m, "x"],
                [2, 1, 10, 20, 30, 40, 50, 60, "rowx", true, 2m, "x"],
                [3, 2, 10, 20, 30, 40, 50, 60, "rowxx", true, 3m, "x"]
            ],
            true,
            "Q218_RecursiveWidePayload"),
        new(
            "recursive-default-limit-shape",
            Tags("union-all", "default-limits", "code-shape", "predicate-termination"),
            "with recursive counter (Value) as (" +
            "select 1 from values {{ Seed: 1 }} seed union all " +
            "select c.Value + 1 from counter c where c.Value < 3) " +
            "select Value from counter order by Value",
            new CompilationOptions(),
            [new("Value", typeof(int))],
            [[1], [2], [3]],
            true,
            "Q219_RecursiveLimitDefaultCodeShape"),
        new(
            "recursive-override-limit-shape",
            Tags("union-all", "override-limits", "code-shape", "predicate-termination"),
            "with recursive counter (Value) as (" +
            "select 1 from values {{ Seed: 1 }} seed union all " +
            "select c.Value + 1 from counter c where c.Value < 3) " +
            "select Value from counter order by Value",
            new CompilationOptions().WithRecursiveCteLimits(new(7, 25)),
            [new("Value", typeof(int))],
            [[1], [2], [3]],
            true,
            "Q220_RecursiveLimitOverrideCodeShape"),
        new(
            "recursive-sidecar-disabled",
            Tags("union-keyed", "earlier-cte", "sidecar-disabled", "chain"),
            "with recursive edges (SourceId, TargetId) as (" +
            "select SourceId, TargetId from #graph.edges()), " +
            "reachable (Id, Depth) as (" +
            "select RootId, 0 from #graph.roots() union (Id) " +
            "select e.TargetId, r.Depth + 1 from reachable r " +
            "inner join edges e on e.SourceId = r.Id) " +
            "select Id, Depth from reachable order by Id",
            new CompilationOptions(usePrimitiveTypeValidation: false, useCteSidecarIndexes: false),
            [new("Id", typeof(int)), new("Depth", typeof(int))],
            [[1, 0], [2, 1], [3, 2]],
            true,
            "Q221_RecursiveSidecarDisabled",
            RecursiveGraphFixtures.CreateChainProvider),
        new(
            "recursive-cte-parallel-siblings",
            Tags("union-keyed", "ordinary-parallel-siblings", "earlier-cte", "sequential-fixed-point"),
            "with recursive seeds (Id) as (" +
            "select Id from values {{ Id: 1 }} seed), " +
            "edges (SourceId, TargetId) as (" +
            "select SourceId, TargetId from values " +
            "{{ SourceId: 1, TargetId: 2 }, { SourceId: 2, TargetId: 3 }} edge), " +
            "reachable (Id, Depth) as (" +
            "select Id, 0 from seeds union (Id) " +
            "select e.TargetId, r.Depth + 1 from reachable r " +
            "inner join edges e on e.SourceId = r.Id) " +
            "select Id, Depth from reachable order by Id",
            new CompilationOptions(useCteParallelization: true),
            [new("Id", typeof(int)), new("Depth", typeof(int))],
            [[1, 0], [2, 1], [3, 2]],
            true,
            "Q222_RecursiveCteParallelSiblings")
    ];

    public static RecursiveCteSupportedCase GetBySampleName(string sampleName)
    {
        return Cases.Single(item => string.Equals(
            item.GeneratedSampleName,
            sampleName,
            StringComparison.Ordinal));
    }

    private static IReadOnlySet<string> Tags(params string[] tags)
    {
        return tags.ToHashSet(StringComparer.Ordinal);
    }

    private static string GraphJoinQuery(string memberSource)
    {
        return "with recursive reachable (Id, Depth) as (" +
               "select RootId, 0 from #graph.roots() " +
               "union (Id) " +
               "select e.TargetId, r.Depth + 1 from reachable r " +
               memberSource + ") " +
               "select Id, Depth from reachable order by Id";
    }
}
