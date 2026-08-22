using System.Linq;
using System.Text;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Physical;

public static partial class PhysicalPlanPrinter
{
    private static void PrintApply(PhysicalNestedLoopApplyNode apply, StringBuilder sb, string prefix, int indent)
    {
        sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalNestedLoopApply [{apply.Kind}{(apply.WithOrdinality ? ", with ordinality" : string.Empty)}]");
        if (apply.ApplyPredicateMovementPlans.Count > 0)
            sb.Append(" [guards: ").Append(string.Join(", ", apply.ApplyPredicateMovementPlans.Select(static plan => $"{plan.Placement}: {plan.PredicateText}"))).Append(']');
        sb.AppendLine();
        PrintNode(apply.Left, sb, indent + 2);
        PrintNode(apply.Right, sb, indent + 2);
    }
}
