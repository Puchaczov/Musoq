using System.Collections.Generic;
using Musoq.Evaluator.Tests.Schema.RecursiveCte;

namespace Musoq.Evaluator.Tests;

internal static partial class RecursiveCteSupportedCaseCatalog
{
    private static IReadOnlyList<RecursiveCteSupportedCase> CreateHardeningCases() =>
    [
        HardeningCase(
            "uncorrelated-cross-apply-snapshot",
            Tags("union-keyed", "chain", "external-snapshot", "cross-apply", "uncorrelated-source"),
            "with recursive reachable (Id, Depth) as (" +
            "select RootId, 0 from #graph.roots() union (Id) " +
            "select e.TargetId, r.Depth + 1 from reachable r cross apply #graph.edges() e " +
            "where e.SourceId = r.Id) select Id, Depth from reachable order by Id",
            [[1, 0], [2, 1], [3, 2]],
            "Q223_RecursiveUncorrelatedApplySnapshot",
            RecursiveGraphFixtures.CreateChainProvider),
        HardeningCase(
            "composite-invariant-subplan",
            Tags("union-keyed", "chain", "external-snapshot", "composite-invariant", "invariant-index"),
            "with recursive reachable (Id, Depth) as (" +
            "select RootId, 0 from #graph.roots() union (Id) " +
            "select e.TargetId, r.Depth + 1 from #graph.edges() e " +
            "inner join values {{ Label: 'one-two' }, { Label: 'two-three' }} expected " +
            "on e.Label = expected.Label inner join reachable r on e.SourceId = r.Id) " +
            "select Id, Depth from reachable order by Id",
            [[1, 0], [2, 1], [3, 2]],
            "Q224_RecursiveCompositeInvariantSubplan",
            RecursiveGraphFixtures.CreateChainProvider),
        HardeningCase(
            "mutable-source-value-snapshot",
            Tags("union-keyed", "chain", "external-snapshot", "mutable-source", "value-snapshot"),
            GraphJoinQuery("inner join #graph.edges() e on e.SourceId = r.Id"),
            [[1, 0], [2, 1], [3, 2]],
            "Q225_RecursiveMutableSourceValueSnapshot",
            RecursiveGraphFixtures.CreateMutableSnapshotProvider),
        HardeningCase(
            "uncorrelated-outer-apply-empty-snapshot-limit-shape",
            Tags("union-keyed", "self-loop", "external-snapshot", "outer-apply", "uncorrelated-source", "empty-neighbors"),
            "with recursive reachable (Id, Depth) as (" +
            "select RootId, 0 from #graph.roots() union (Id) " +
            "select case when e.TargetId is null then r.Id else e.TargetId end, r.Depth + 1 " +
            "from reachable r outer apply #graph.edges() e " +
            "where e.SourceId is null or e.SourceId = r.Id) " +
            "select Id, Depth from reachable order by Id",
            [[1, 0]],
            "Q226_RecursiveSnapshotLimitCodeShape",
            RecursiveGraphFixtures.CreateEmptyEdgesProvider,
            new CompilationOptions(usePrimitiveTypeValidation: false)
                .WithRecursiveCteLimits(new(100, 100, 1)))
    ];

    private static RecursiveCteSupportedCase HardeningCase(
        string name,
        IReadOnlySet<string> tags,
        string query,
        IReadOnlyList<object?[]> rows,
        string sampleName,
        System.Func<RecursiveGraphSchemaProvider> provider,
        CompilationOptions? options = null) =>
        new(
            name,
            tags,
            query,
            options ?? new CompilationOptions(usePrimitiveTypeValidation: false),
            [new RecursiveCteExpectedColumn("Id", typeof(int)), new RecursiveCteExpectedColumn("Depth", typeof(int))],
            rows,
            true,
            sampleName,
            provider);
}
