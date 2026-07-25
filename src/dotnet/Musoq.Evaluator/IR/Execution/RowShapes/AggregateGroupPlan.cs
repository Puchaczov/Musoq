using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record AggregateGroupPlan
{
    public AggregateGroupPlan(
        AggregateGroupShape leafShape,
        IReadOnlyList<AggregateGroupLevelPlan> levels)
    {
        LeafShape = leafShape;
        Levels = ExecutionIrCollections.Freeze(levels);
    }

    public AggregateGroupShape LeafShape { get; init; }

    public IReadOnlyList<AggregateGroupLevelPlan> Levels { get; init; }

    public bool RequiresParentLinks => LeafShape.RequiresParentLinks;
}
