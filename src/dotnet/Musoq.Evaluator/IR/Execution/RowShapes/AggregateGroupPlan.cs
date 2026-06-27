using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record AggregateGroupPlan(
    AggregateGroupShape LeafShape,
    IReadOnlyList<AggregateGroupLevelPlan> Levels)
{
    public bool RequiresParentLinks => LeafShape.RequiresParentLinks;
}
