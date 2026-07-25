using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record AggregateGroupLevelPlan
{
    public AggregateGroupLevelPlan(int prefixLength, AggregateGroupShape shape)
    {
        PrefixLength = prefixLength;
        Shape = shape;
    }

    public int PrefixLength { get; init; }

    public AggregateGroupShape Shape { get; init; }

    public IReadOnlyList<AggregateAccumulatorField> Accumulators => Shape.Accumulators;

    public bool IsRoot => PrefixLength == 0;
}
