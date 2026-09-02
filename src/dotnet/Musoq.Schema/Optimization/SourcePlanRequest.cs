using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

/// <summary>Metadata and portable operations requested from a datasource.</summary>
public sealed record SourcePlanRequest
{
    public required SourceIdentity Identity { get; init; }

    public IReadOnlyDictionary<string, string> SourceRuntimeSettings { get; init; } =
        new Dictionary<string, string>();

    public IReadOnlyList<SourceColumnRef> RequiredColumns { get; init; } = [];

    /// <summary>Stable portable scalar projections requested from a capable source.</summary>
    public IReadOnlyList<SourceComputedProjection> RequestedComputedProjections { get; init; } = [];

    /// <summary>Replayability of the source row stream, when the provider knows it.</summary>
    public RowStreamReplayability Replayability { get; init; } = RowStreamReplayability.Unknown;

    public SourcePredicateExpression? Predicate { get; init; }

    public IReadOnlyList<OrderByExpression> OrderBy { get; init; } = [];

    public long? Skip { get; init; }

    public long? Take { get; init; }

    public static SourcePlanRequest Empty(SourceIdentity identity)
    {
        return new SourcePlanRequest { Identity = identity };
    }
}
