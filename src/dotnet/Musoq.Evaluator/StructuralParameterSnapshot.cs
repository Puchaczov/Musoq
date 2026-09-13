using System.Collections.Generic;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator;

/// <summary>Owned parameter values and resource measurements captured for one execution.</summary>
public sealed class StructuralParameterSnapshot
{
    internal StructuralParameterSnapshot(
        IReadOnlyDictionary<string, object?> values,
        IReadOnlyDictionary<string, StructuralInputMetrics> metrics,
        StructuralInputMetrics aggregateMetrics)
    {
        Values = values ?? throw new ArgumentNullException(nameof(values));
        Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        AggregateMetrics = aggregateMetrics;
    }

    /// <summary>Gets the execution-owned values keyed by declared script parameter name.</summary>
    public IReadOnlyDictionary<string, object?> Values { get; }

    /// <summary>Gets measured usage for each structured parameter root.</summary>
    public IReadOnlyDictionary<string, StructuralInputMetrics> Metrics { get; }

    /// <summary>Gets the aggregate node and string usage across all captured roots.</summary>
    public StructuralInputMetrics AggregateMetrics { get; }
}
