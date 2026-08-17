using System.Text;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Printing;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Physical;

public static partial class PhysicalPlanPrinter
{
    private static void PrintJoinCandidate(
        PhysicalJoinCandidateNode joinCandidate,
        StringBuilder sb,
        string prefix,
        int indent)
    {
        sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalJoinCandidate [{joinCandidate.Kind}] [");
        sb.Append(IrExpressionPrinter.Print(joinCandidate.OnPredicate));
        sb.Append(']');
        AppendTieBreak(sb, joinCandidate.TieBreak);
        sb.AppendLine();
        PrintNode(joinCandidate.Left, sb, indent + 2);
        PrintNode(joinCandidate.Right, sb, indent + 2);
    }

    private static void AppendTieBreak(StringBuilder sb, OrderField? tieBreak)
    {
        if (tieBreak == null)
            return;

        sb.Append(" [tie: ");
        PlanPrinterHelpers.AppendOrderFields(sb, [tieBreak]);
        sb.Append(']');
    }

    private static void AppendSortMergePartitions(StringBuilder sb, PhysicalSortMergeJoinNode join)
    {
        if (join.LeftPartitionKeys.Length == 0)
            return;

        sb.Append(" [partitions: ");
        for (var index = 0; index < join.LeftPartitionKeys.Length; index++)
        {
            if (index > 0)
                sb.Append(", ");
            sb.Append(IrExpressionPrinter.Print(join.LeftPartitionKeys[index]));
            sb.Append(" = ");
            sb.Append(IrExpressionPrinter.Print(join.RightPartitionKeys[index]));
        }
        sb.Append(']');
    }
}
