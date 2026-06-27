using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

public sealed record SourcePlanRequest
{
    public required SourceIdentity Identity { get; init; }

    public IReadOnlyDictionary<string, string> SourceRuntimeSettings { get; init; } =
        new Dictionary<string, string>();

    public IReadOnlyList<SourceColumnRef> RequiredColumns { get; init; } = [];

    public SourcePredicateExpression? Predicate { get; init; }

    public IReadOnlyList<OrderByExpression> OrderBy { get; init; } = [];

    public long? Skip { get; init; }

    public long? Take { get; init; }

    public static SourcePlanRequest Empty(SourceIdentity identity)
    {
        return new SourcePlanRequest { Identity = identity };
    }
}
