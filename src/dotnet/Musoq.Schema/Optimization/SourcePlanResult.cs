using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

/// <summary>Validated accepted/residual source operations and execution metadata.</summary>
public sealed record SourcePlanResult
{
    public required SourceExecutionPlan ExecutionPlan { get; init; }

    public IReadOnlyList<SourceColumnRef> AcceptedColumns { get; init; } = [];

    public IReadOnlyList<SourceComputedProjection> AcceptedComputedProjections { get; init; } = [];

    public IReadOnlyList<SourceComputedProjection> ResidualComputedProjections { get; init; } = [];

    public RowStreamReplayability Replayability { get; init; } = RowStreamReplayability.Unknown;

    public SourcePredicateExpression? AcceptedPredicate { get; init; }

    public SourcePredicateExpression? ResidualPredicate { get; init; }

    public IReadOnlyList<OrderByExpression> AcceptedOrderBy { get; init; } = [];

    public IReadOnlyList<OrderByExpression> ResidualOrderBy { get; init; } = [];

    public long? AcceptedSkip { get; init; }

    public long? ResidualSkip { get; init; }

    public long? AcceptedTake { get; init; }

    public long? ResidualTake { get; init; }

    public CardinalityEstimate? Cardinality { get; init; }

    public IReadOnlyList<OptimizationDiagnostic> Diagnostics { get; init; } = [];

    public IReadOnlyList<SourceContractDiagnostic> ContractDiagnostics { get; init; } = [];

    public static SourcePlanResult RejectAll(SourcePlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new SourcePlanResult
        {
            ExecutionPlan = SourceExecutionPlan.Empty(request.Identity) with { Replayability = request.Replayability },
            ResidualPredicate = request.Predicate,
            ResidualComputedProjections = request.RequestedComputedProjections,
            ResidualOrderBy = request.OrderBy,
            ResidualSkip = request.Skip,
            ResidualTake = request.Take,
            Replayability = request.Replayability,
            Cardinality = CardinalityEstimate.Unknown("Source did not accept ORDER BY/SKIP/TAKE pushdown.")
        };
    }

    public static SourcePlanResult AcceptAll(SourcePlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new SourcePlanResult
        {
            ExecutionPlan = new SourceExecutionPlan
            {
                Identity = request.Identity,
                AcceptedColumns = request.RequiredColumns,
                AcceptedComputedProjections = request.RequestedComputedProjections,
                Replayability = request.Replayability,
                AcceptedPredicate = request.Predicate,
                AcceptedOrderBy = request.OrderBy,
                AcceptedSkip = request.Skip,
                AcceptedTake = request.Take
            },
            AcceptedColumns = request.RequiredColumns,
            AcceptedComputedProjections = request.RequestedComputedProjections,
            AcceptedPredicate = request.Predicate,
            AcceptedOrderBy = request.OrderBy,
            AcceptedSkip = request.Skip,
            AcceptedTake = request.Take,
            Replayability = request.Replayability
        };
    }
}
