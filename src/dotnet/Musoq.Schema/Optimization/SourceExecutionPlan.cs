using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

public sealed record SourceExecutionPlan
{
    public required SourceIdentity Identity { get; init; }

    public IReadOnlyList<SourceColumnRef> AcceptedColumns { get; init; } = [];

    public SourcePredicateExpression? AcceptedPredicate { get; init; }

    public IReadOnlyList<OrderByExpression> AcceptedOrderBy { get; init; } = [];

    public long? AcceptedSkip { get; init; }

    public long? AcceptedTake { get; init; }

    public IReadOnlyDictionary<string, object?> Properties { get; init; } =
        new Dictionary<string, object?>();

    public static SourceExecutionPlan Empty(SourceIdentity identity)
    {
        return new SourceExecutionPlan { Identity = identity };
    }
}
