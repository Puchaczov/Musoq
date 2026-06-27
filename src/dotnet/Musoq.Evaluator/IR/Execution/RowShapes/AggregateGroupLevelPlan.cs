using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record AggregateGroupLevelPlan(
    int PrefixLength,
    AggregateGroupShape Shape)
{
    public IReadOnlyList<AggregateAccumulatorField> Accumulators => Shape.Accumulators;

    public bool IsRoot => PrefixLength == 0;
}
