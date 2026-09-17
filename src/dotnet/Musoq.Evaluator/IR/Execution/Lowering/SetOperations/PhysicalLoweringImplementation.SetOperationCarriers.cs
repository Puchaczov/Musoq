using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static bool CanShareSetOperationCarrier(PhysicalNode left, PhysicalNode right)
    {
        if (!ContainsPhysicalNode<PhysicalWindowNode>(left) ||
            !ContainsPhysicalNode<PhysicalWindowNode>(right) ||
            !ContainsPhysicalNode<PhysicalCteRefNode>(left) ||
            !ContainsPhysicalNode<PhysicalSchemaScanNode>(right))
        {
            return false;
        }

        var leftColumns = left.OutputSchema.Columns;
        var rightColumns = right.OutputSchema.Columns;
        if (leftColumns.Length != rightColumns.Length)
            return false;

        for (var index = 0; index < leftColumns.Length; index++)
        {
            var leftColumn = leftColumns[index];
            var rightColumn = rightColumns[index];
            if (leftColumn.Index != rightColumn.Index ||
                !string.Equals(leftColumn.Name, rightColumn.Name, StringComparison.Ordinal) ||
                leftColumn.Type != rightColumn.Type ||
                !string.Equals(leftColumn.IntendedTypeName, rightColumn.IntendedTypeName, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
