using System.Text;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Printing;

namespace Musoq.Evaluator.IR.Physical;

public static partial class PhysicalPlanPrinter
{
    private static void PrintProject(
        PhysicalProjectNode project,
        StringBuilder sb,
        string prefix,
        int indent)
    {
        sb.Append(prefix).Append("PhysicalProject [");
        PlanPrinterHelpers.AppendProjectedFields(sb, project.Fields);
        sb.AppendLine("]");
        PrintNode(project.Input, sb, indent + 2);
    }
}
