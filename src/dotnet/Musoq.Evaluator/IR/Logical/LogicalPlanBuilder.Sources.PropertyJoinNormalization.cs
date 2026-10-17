using Musoq.Evaluator.IR.Logical.Nodes;
using IrNodes = Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{
    private static LogicalNode CreateJoinSource(IrNodes.JoinNode join)
    {
        if (join is not { Kind: IrNodes.JoinKind.Inner, Right: IrNodes.PropertySourceNode, WithOrdinality: false })
            return join;

        // A property source is evaluated against its owning left row. Model that
        // dependency before planning so it cannot become an independent hash input.
        return new IrNodes.FilterNode(
            join.OnPredicate,
            new IrNodes.ApplyNode(IrNodes.ApplyKind.Cross, join.Left, join.Right));
    }
}
