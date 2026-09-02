using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Musoq.Schema.Optimization;

/// <summary>Immutable source execution metadata carried to the generated query.</summary>
public sealed record SourceExecutionPlan
{
    public required SourceIdentity Identity { get; init; }

    private IReadOnlyList<SourceColumnRef> _acceptedColumns = [];
    private IReadOnlyList<OrderByExpression> _acceptedOrderBy = [];
    private IReadOnlyDictionary<string, object?> _properties =
        new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>(StringComparer.Ordinal));

    public IReadOnlyList<SourceColumnRef> AcceptedColumns
    {
        get => _acceptedColumns;
        init => _acceptedColumns = FreezeList(value);
    }

    private IReadOnlyList<SourceComputedProjection> _acceptedComputedProjections = [];

    public IReadOnlyList<SourceComputedProjection> AcceptedComputedProjections
    {
        get => _acceptedComputedProjections;
        init => _acceptedComputedProjections = FreezeList(value);
    }

    public RowStreamReplayability Replayability { get; init; } = RowStreamReplayability.Unknown;

    public SourcePredicateExpression? AcceptedPredicate { get; init; }

    public IReadOnlyList<OrderByExpression> AcceptedOrderBy
    {
        get => _acceptedOrderBy;
        init => _acceptedOrderBy = FreezeList(value);
    }

    public long? AcceptedSkip { get; init; }

    public long? AcceptedTake { get; init; }

    public IReadOnlyDictionary<string, object?> Properties
    {
        get => _properties;
        init => _properties = FreezeProperties(value);
    }

    public static SourceExecutionPlan Empty(SourceIdentity identity)
    {
        return new SourceExecutionPlan { Identity = identity };
    }

    private static IReadOnlyList<T> FreezeList<T>(IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Array.AsReadOnly(values.ToArray());
    }

    private static IReadOnlyDictionary<string, object?> FreezeProperties(
        IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var copy = new Dictionary<string, object?>(values.Count, StringComparer.Ordinal);
        foreach (var pair in values)
        {
            copy[pair.Key] = pair.Value switch
            {
                IReadOnlyDictionary<string, object?> nested => FreezeProperties(nested),
                IReadOnlyDictionary<string, string> strings => new ReadOnlyDictionary<string, string>(
                    new Dictionary<string, string>(strings, StringComparer.Ordinal)),
                Array array => array.Clone(),
                _ => pair.Value
            };
        }

        return new ReadOnlyDictionary<string, object?>(copy);
    }
}
