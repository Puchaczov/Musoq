using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Musoq.Schema.Optimization;

/// <summary>Metadata and portable operations requested from a datasource.</summary>
public sealed record SourcePlanRequest
{
    private IReadOnlyList<SourceComputedProjection> _requestedComputedProjections = [];
    public required SourceIdentity Identity { get; init; }

    /// <summary>
    /// Gets the cooperative cancellation token for this transient planning request.
    /// Providers must not persist this token in a plan or artifact.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;

    public IReadOnlyDictionary<string, string> SourceRuntimeSettings { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<SourceColumnRef> RequiredColumns { get; init; } = [];

    /// <summary>Stable portable scalar projections requested from a capable source.</summary>
    public IReadOnlyList<SourceComputedProjection> RequestedComputedProjections
    {
        get => _requestedComputedProjections;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _requestedComputedProjections = value.Count == 0 ? [] : Array.AsReadOnly(value.ToArray());
        }
    }

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
