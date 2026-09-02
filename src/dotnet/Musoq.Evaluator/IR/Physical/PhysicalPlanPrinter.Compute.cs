using System.Text;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Printing;

namespace Musoq.Evaluator.IR.Physical;

public static partial class PhysicalPlanPrinter
{
    private static void PrintCompute(
        PhysicalComputeNode compute,
        StringBuilder sb,
        string prefix,
        int indent)
    {
        sb.Append(prefix).Append("PhysicalCompute [");
        PlanPrinterHelpers.AppendProjectedFields(sb, compute.ComputedFields.ToArray());
        sb.AppendLine("]");
        PrintNode(compute.Input, sb, indent + 2);
    }
}
