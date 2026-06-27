using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

public sealed record SourcePlanResult
{
    public required SourceExecutionPlan ExecutionPlan { get; init; }

    public IReadOnlyList<SourceColumnRef> AcceptedColumns { get; init; } = [];

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
            ExecutionPlan = SourceExecutionPlan.Empty(request.Identity),
            ResidualPredicate = request.Predicate,
            ResidualOrderBy = request.OrderBy,
            ResidualSkip = request.Skip,
            ResidualTake = request.Take,
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
                AcceptedPredicate = request.Predicate,
                AcceptedOrderBy = request.OrderBy,
                AcceptedSkip = request.Skip,
                AcceptedTake = request.Take
            },
            AcceptedColumns = request.RequiredColumns,
            AcceptedPredicate = request.Predicate,
            AcceptedOrderBy = request.OrderBy,
            AcceptedSkip = request.Skip,
            AcceptedTake = request.Take
        };
    }
}
