using System.Collections.Generic;
using System.Linq;

namespace Musoq.Schema.Optimization;

/// <summary>Validated accepted/residual source operations and execution metadata.</summary>
public sealed record SourcePlanResult
{
    private IReadOnlyList<SourceComputedProjection> _acceptedComputedProjections = [];
    private IReadOnlyList<SourceComputedProjection> _residualComputedProjections = [];
    public required SourceExecutionPlan ExecutionPlan { get; init; }

    public IReadOnlyList<SourceColumnRef> AcceptedColumns { get; init; } = [];

    public IReadOnlyList<SourceComputedProjection> AcceptedComputedProjections
    {
        get => _acceptedComputedProjections;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _acceptedComputedProjections = value.Count == 0 ? [] : Array.AsReadOnly(value.ToArray());
        }
    }

    public IReadOnlyList<SourceComputedProjection> ResidualComputedProjections
    {
        get => _residualComputedProjections;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _residualComputedProjections = value.Count == 0 ? [] : Array.AsReadOnly(value.ToArray());
        }
    }

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

    /// <summary>Provider-authored public warnings or recognized errors.</summary>
    public IReadOnlyList<Musoq.Schema.Diagnostics.DataSourceDiagnostic> ReportedDiagnostics
    {
        get => _reportedDiagnostics;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _reportedDiagnostics = Array.AsReadOnly(System.Linq.Enumerable.ToArray(value));
        }
    }

    private readonly IReadOnlyList<Musoq.Schema.Diagnostics.DataSourceDiagnostic> _reportedDiagnostics = [];

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
                PredicateApplications = SourcePredicateApplication.ForRowFiltering(request.Predicate),
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
