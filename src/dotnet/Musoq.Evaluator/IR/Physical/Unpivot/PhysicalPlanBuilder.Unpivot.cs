using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Physical;

public sealed partial class PhysicalPlanBuilder
{
    private PhysicalUnpivotNode LowerUnpivot(UnpivotNode unpivot, PhysicalStrategyPlan strategyPlan)
    {
        return new PhysicalUnpivotNode(
            unpivot.Alias,
            unpivot.NameColumn,
            unpivot.ValueColumn,
            unpivot.Entries,
            unpivot.KeepFields,
            Lower(unpivot.Source, strategyPlan),
            unpivot.OutputSchema);
    }
}
