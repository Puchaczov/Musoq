using System;
using System.Collections.Generic;
using Musoq.Evaluator.Tests.Schema.RecursiveCte;

namespace Musoq.Evaluator.Tests;

internal static partial class RecursiveCteSupportedCaseCatalog
{
    private static IReadOnlyList<RecursiveCteSupportedCase> CreateMixedCases()
    {
        return
        [
            Case(
                "mixed-keyed-diamond-payload",
                Tags("union-keyed", "diamond", "path-payload", "targeted-three-way"),
                GraphPayloadQuery(),
                Columns(("Id", typeof(int)), ("Depth", typeof(int)), ("Payload", typeof(string))),
                [[1, 0, "root"], [2, 1, "one-two"], [3, 1, "one-three"], [4, 2, "to-four"]],
                RecursiveGraphFixtures.CreateEquivalentPayloadDiamondProvider),
            Case(
                "mixed-snapshot-index-cycle",
                Tags("union-keyed", "external-snapshot", "invariant-index", "cycle", "targeted-three-way"),
                GraphJoinQuery("inner join #graph.edges() e on e.SourceId = r.Id"),
                IdDepthColumns(),
                [[1, 0], [2, 1], [3, 2]],
                RecursiveGraphFixtures.CreateCycleProvider,
                new CompilationOptions(
                    usePrimitiveTypeValidation: false,
                    instrumentationMode: QueryInstrumentationMode.SourceBoundaries)),
            Case(
                "mixed-pruned-depth-outer-aggregate",
                Tags("union-keyed", "projection-pruning", "hidden-depth", "outer-aggregate", "targeted-three-way"),
                "with recursive walk (Id, Depth, Payload) as (" +
                "select 1, 0, 'root' from values {{ Seed: 1 }} seed union (Id) " +
                "select w.Id + 1, w.Depth + 1, w.Payload + 'x' from walk w where w.Depth < 3) " +
                "select Count(Id) as Nodes, Max(Id) as MaxId from walk",
                Columns(("Nodes", typeof(long)), ("MaxId", typeof(int?))),
                [[4L, 4]]),
            Case(
                "mixed-multiple-recursive-prior-parallel",
                Tags("multiple-recursive", "earlier-dependency", "ordinary-parallel-siblings", "targeted-three-way"),
                "with recursive seeds (Value) as (select Value from values {{ Value: 1 }} seed), " +
                "steps (Amount) as (select Amount from values {{ Amount: 1 }} step), " +
                "first (Value) as (select Value from seeds union all " +
                "select f.Value + s.Amount from first f cross join steps s where f.Value < 2), " +
                "second (Value) as (select Value from first where Value = 2 union all " +
                "select s.Value + 1 from second s where s.Value < 3) " +
                "select Value from second order by Value",
                Columns(("Value", typeof(int))),
                [[2], [3]],
                options: new CompilationOptions(useCteParallelization: true)),
            Case(
                "mixed-nullable-composite-duplicate-cast",
                Tags("union-keyed", "nullable-key", "composite-key", "duplicate-edge", "explicit-cast", "targeted-three-way"),
                "with recursive edges (SourceId, TargetId) as (select SourceId, TargetId from values " +
                "{{ SourceId: 1, TargetId: 2 }, { SourceId: 1, TargetId: 2 }} e), " +
                "states (Id, Code, Total, Depth) as (" +
                "select 1, null::String, 0::Decimal, 0 from values {{ Seed: 1 }} seed union (Id, Code) " +
                "select e.TargetId, s.Code, (s.Total + 1)::Decimal, s.Depth + 1 from states s " +
                "inner join edges e on e.SourceId = s.Id) " +
                "select Id, Code, Total, Depth from states order by Id",
                Columns(
                    ("Id", typeof(int)), ("Code", typeof(string)),
                    ("Total", typeof(decimal?)), ("Depth", typeof(int))),
                [[1, null, 0m, 0], [2, null, 1m, 1]],
                options: new CompilationOptions(useCteSidecarIndexes: false)),
            Case(
                "mixed-tight-limit-roots-keyed-duplicates",
                Tags("union-keyed", "multiple-roots", "same-generation-duplicates", "tight-row-limit", "targeted-three-way"),
                "with recursive nodes (Id) as (select Id from values {{ Id: 1 }, { Id: 2 }} seed " +
                "union (Id) select 3 from nodes n where n.Id < 3) select Id from nodes order by Id",
                Columns(("Id", typeof(int))),
                [[1], [2], [3]],
                options: new CompilationOptions().WithRecursiveCteLimits(new(10, 3))),
            Case(
                "mixed-correlated-empty-neighbors-aggregate",
                Tags("union-keyed", "cross-apply", "correlated-source", "empty-neighbors", "outer-aggregate", "targeted-three-way"),
                "with recursive reachable (Id, Depth) as (select RootId, 0 from #graph.roots() union (Id) " +
                "select e.TargetId, r.Depth + 1 from reachable r cross apply #graph.neighbors(r.Id) e) " +
                "select Count(Id) as Nodes, Max(Depth) as MaxDepth from reachable",
                Columns(("Nodes", typeof(long)), ("MaxDepth", typeof(int?))),
                [[3L, 2]],
                RecursiveGraphFixtures.CreateChainProvider),
            Case(
                "mixed-dead-recursive-failing-source",
                Tags("dead-cte", "failing-source", "ordinary-live-cte", "targeted-three-way"),
                "with recursive dead (Id) as (select RootId from #graph.roots() where RootId < 0 union (Id) " +
                "select e.TargetId from dead d inner join #graph.edges() e on e.SourceId = d.Id), " +
                "live (Value) as (select Value from values {{ Value: 42 }} row) select Value from live",
                Columns(("Value", typeof(int))),
                [[42]],
                () => RecursiveGraphFixtures.CreateFailingEdgesProvider()),
            GraphCase("pair-keyed-tree", Tags("union-keyed", "tree", "external-snapshot"),
                [[1, 0], [2, 1], [3, 1], [4, 2], [5, 2]], RecursiveGraphFixtures.CreateTreeProvider),
            GraphCase("pair-full-row-duplicate-edges", Tags("union", "full-row", "duplicate-edge", "chain", "external-snapshot"),
                [[1, 0], [2, 1], [3, 2]], RecursiveGraphFixtures.CreateDuplicateEdgesProvider, fullRow: true),
            GraphCase("pair-keyed-disconnected", Tags("union-keyed", "disconnected", "external-snapshot"),
                [[1, 0], [2, 1]], RecursiveGraphFixtures.CreateDisconnectedProvider),
            GraphCase("pair-keyed-multiple-roots", Tags("union-keyed", "multiple-roots", "external-snapshot"),
                [[1, 0], [2, 1], [10, 0], [11, 1]], RecursiveGraphFixtures.CreateMultipleRootsProvider),
            Case(
                "pair-keyed-self-loop-payload",
                Tags("union-keyed", "self-loop", "path-payload"),
                "with recursive walk (Id, Label) as (select RootId, 'root' from #graph.roots() union (Id) " +
                "select e.TargetId, e.Label from walk w inner join #graph.edges() e on e.SourceId = w.Id) " +
                "select Id, Label from walk",
                Columns(("Id", typeof(int)), ("Label", typeof(string))),
                [[1, "root"]],
                RecursiveGraphFixtures.CreateSelfLoopProvider),
            Case(
                "pair-union-all-tree-bounded-depth",
                Tags("union-all", "tree", "external-snapshot", "predicate-termination"),
                "with recursive walk (Id, Depth) as (select RootId, 0 from #graph.roots() union all " +
                "select e.TargetId, w.Depth + 1 from walk w inner join #graph.edges() e on e.SourceId = w.Id " +
                "where w.Depth < 2) select Id, Depth from walk order by Id",
                IdDepthColumns(),
                [[1, 0], [2, 1], [3, 1], [4, 2], [5, 2]],
                RecursiveGraphFixtures.CreateTreeProvider),
            Case(
                "pair-full-row-nullable-state",
                Tags("union", "full-row", "nullable-value", "predicate-termination"),
                "with recursive states (Id, ParentId, Depth) as (select 1, null, 0 from values {{ Seed: 1 }} seed union " +
                "select s.Id, s.Id, s.Depth + 1 from states s where s.Depth < 2) " +
                "select Id, ParentId, Depth from states order by Depth",
                Columns(("Id", typeof(int)), ("ParentId", typeof(int?)), ("Depth", typeof(int))),
                [[1, null, 0], [1, 1, 1], [1, 1, 2]]),
            Case(
                "pair-composite-key-branches",
                Tags("union-keyed", "composite-key", "multiple-roots", "string-state"),
                "with recursive states (Id, Branch) as (select Id, Branch from values " +
                "{{ Id: 1, Branch: 'A' }, { Id: 1, Branch: 'B' }} seed union (Id, Branch) " +
                "select s.Id + 1, s.Branch from states s where s.Id < 2) " +
                "select Id, Branch from states order by Id, Branch",
                Columns(("Id", typeof(int)), ("Branch", typeof(string))),
                [[1, "A"], [1, "B"], [2, "A"], [2, "B"]]),
            Case(
                "pair-sidecar-off-diamond",
                Tags("union-keyed", "diamond", "sidecar-disabled", "earlier-cte"),
                PriorEdgesGraphQuery(),
                IdDepthColumns(),
                [[1, 0], [2, 1], [3, 1], [4, 2]],
                RecursiveGraphFixtures.CreateDiamondProvider,
                new CompilationOptions(usePrimitiveTypeValidation: false, useCteSidecarIndexes: false)),
            Case(
                "pair-parallel-siblings-disabled",
                Tags("union-keyed", "ordinary-parallel-siblings", "parallelization-disabled"),
                ParallelSiblingQuery(),
                IdDepthColumns(),
                [[1, 0], [2, 1], [3, 2]],
                options: new CompilationOptions(useCteParallelization: false)),
            GraphCase(
                "pair-source-boundaries-chain",
                Tags("union-keyed", "chain", "instrumentation-source-boundaries"),
                [[1, 0], [2, 1], [3, 2]],
                RecursiveGraphFixtures.CreateChainProvider,
                options: new CompilationOptions(
                    usePrimitiveTypeValidation: false,
                    instrumentationMode: QueryInstrumentationMode.SourceBoundaries)),
            GraphCase(
                "pair-full-instrumentation-cycle",
                Tags("union-keyed", "cycle", "instrumentation-full"),
                [[1, 0], [2, 1], [3, 2]],
                RecursiveGraphFixtures.CreateCycleProvider,
                options: new CompilationOptions(
                    usePrimitiveTypeValidation: false,
                    instrumentationMode: QueryInstrumentationMode.Full)),
            Case(
                "pair-dependent-ordinary-aggregate",
                Tags("ordinary-after-recursive", "outer-aggregate", "state-depth"),
                "with recursive walk (Id, Depth) as (select 1, 0 from values {{ Seed: 1 }} seed union all " +
                "select w.Id + 1, w.Depth + 1 from walk w where w.Depth < 3), " +
                "deep (Id) as (select Id from walk where Depth > 1) select Count(Id) as Nodes from deep",
                Columns(("Nodes", typeof(long))),
                [[2L]]),
            Case(
                "pair-two-recursive-outer-set",
                Tags("multiple-recursive", "independent-siblings", "outer-set-operation"),
                "with recursive low (Value) as (select 1 from values {{ Seed: 1 }} s union all " +
                "select l.Value + 1 from low l where l.Value < 2), " +
                "high (Value) as (select 10 from values {{ Seed: 1 }} s union all " +
                "select h.Value + 1 from high h where h.Value < 11) " +
                "select Value from low union all select Value from high",
                Columns(("Value", typeof(int))),
                [[1], [2], [10], [11]],
                ordered: false),
            Case(
                "pair-cross-join-values",
                Tags("union-all", "cross-join", "values", "predicate-termination"),
                "with recursive counter (Value) as (select 1 from values {{ Seed: 1 }} seed union all " +
                "select c.Value + step.Amount from counter c cross join values {{ Amount: 1 }} step " +
                "where c.Value < 3) select Value from counter order by Value",
                Columns(("Value", typeof(int))),
                [[1], [2], [3]]),
            Case(
                "pair-outer-apply-empty-neighbors",
                Tags("union-keyed", "outer-apply", "empty-neighbors", "dedup-termination"),
                "with recursive walk (Id, Depth) as (select RootId, 0 from #graph.roots() union (Id) " +
                "select case when e.TargetId is null then w.Id else e.TargetId end, w.Depth + 1 " +
                "from walk w outer apply #graph.neighbors(w.Id) e) select Id, Depth from walk",
                IdDepthColumns(),
                [[1, 0]],
                RecursiveGraphFixtures.CreateEmptyEdgesProvider),
            Case(
                "pair-empty-anchor-failing-member-source",
                Tags("empty-anchor", "empty-frontier", "failing-source", "external-snapshot"),
                GraphJoinQuery("inner join #graph.edges() e on e.SourceId = r.Id"),
                IdDepthColumns(),
                [],
                () => RecursiveGraphFixtures.CreateFailingEdgesProvider(emptyRoots: true)),
            Case(
                "pair-earlier-recursive-outer-join",
                Tags("earlier-recursive-dependency", "outer-join", "multiple-recursive"),
                "with recursive first (Value) as (select 1 from values {{ Seed: 1 }} s union all " +
                "select f.Value + 1 from first f where f.Value < 3), " +
                "second (Value) as (select Value from first where Value > 1 union all " +
                "select s.Value + 10 from second s where s.Value < 3) " +
                "select f.Value as FirstValue, s.Value as SecondValue from first f " +
                "inner join second s on f.Value = s.Value order by f.Value",
                Columns(("FirstValue", typeof(int)), ("SecondValue", typeof(int))),
                [[2, 2], [3, 3]])
        ];
    }

    private static IReadOnlyList<RecursiveCteExpectedColumn> IdDepthColumns() =>
        Columns(("Id", typeof(int)), ("Depth", typeof(int)));

    private static RecursiveCteSupportedCase GraphCase(
        string name,
        IReadOnlySet<string> tags,
        IReadOnlyList<object?[]> rows,
        Func<RecursiveGraphSchemaProvider> provider,
        bool fullRow = false,
        CompilationOptions? options = null)
    {
        var separator = fullRow ? "union" : "union (Id)";
        var query = "with recursive reachable (Id, Depth) as (select RootId, 0 from #graph.roots() " +
                    separator + " select e.TargetId, r.Depth + 1 from reachable r " +
                    "inner join #graph.edges() e on e.SourceId = r.Id) " +
                    "select Id, Depth from reachable order by Id";
        return Case(name, tags, query, IdDepthColumns(), rows, provider, options);
    }

    private static RecursiveCteSupportedCase Case(
        string name,
        IReadOnlySet<string> tags,
        string query,
        IReadOnlyList<RecursiveCteExpectedColumn> columns,
        IReadOnlyList<object?[]> rows,
        Func<RecursiveGraphSchemaProvider>? provider = null,
        CompilationOptions? options = null,
        bool ordered = true)
    {
        return new RecursiveCteSupportedCase(
            name,
            tags,
            query,
            options ?? new CompilationOptions(usePrimitiveTypeValidation: provider == null),
            columns,
            rows,
            ordered,
            GeneratedSampleName: null,
            provider);
    }

    private static IReadOnlyList<RecursiveCteExpectedColumn> Columns(
        params (string Name, Type Type)[] columns)
    {
        var result = new RecursiveCteExpectedColumn[columns.Length];
        for (var index = 0; index < columns.Length; index++)
            result[index] = new RecursiveCteExpectedColumn(columns[index].Name, columns[index].Type);

        return result;
    }

    private static string GraphPayloadQuery()
    {
        return "with recursive paths (Id, Depth, Payload) as (" +
               "select RootId, 0, 'root' from #graph.roots() union (Id) " +
               "select e.TargetId, p.Depth + 1, e.Label from paths p " +
               "inner join #graph.edges() e on e.SourceId = p.Id) " +
               "select Id, Depth, Payload from paths order by Id";
    }

    private static string PriorEdgesGraphQuery()
    {
        return "with recursive edges (SourceId, TargetId) as (select SourceId, TargetId from #graph.edges()), " +
               "reachable (Id, Depth) as (select RootId, 0 from #graph.roots() union (Id) " +
               "select e.TargetId, r.Depth + 1 from reachable r inner join edges e on e.SourceId = r.Id) " +
               "select Id, Depth from reachable order by Id";
    }

    private static string ParallelSiblingQuery()
    {
        return "with recursive seeds (Id) as (select Id from values {{ Id: 1 }} seed), " +
               "edges (SourceId, TargetId) as (select SourceId, TargetId from values " +
               "{{ SourceId: 1, TargetId: 2 }, { SourceId: 2, TargetId: 3 }} edge), " +
               "reachable (Id, Depth) as (select Id, 0 from seeds union (Id) " +
               "select e.TargetId, r.Depth + 1 from reachable r inner join edges e on e.SourceId = r.Id) " +
               "select Id, Depth from reachable order by Id";
    }
}
