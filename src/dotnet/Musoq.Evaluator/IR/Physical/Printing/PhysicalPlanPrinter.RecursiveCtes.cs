using System.Linq;
using System.Text;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Printing;

namespace Musoq.Evaluator.IR.Physical;

public static partial class PhysicalPlanPrinter
{
    private static void PrintRecursiveCte(
        PhysicalRecursiveCteNode recursiveCte,
        StringBuilder builder,
        string prefix,
        int indent)
    {
        builder.Append(prefix).Append("PhysicalRecursiveCte [");
        builder.Append(recursiveCte.Name);
        builder.Append("] [");
        builder.Append(recursiveCte.UnionKind);
        if (recursiveCte.Keys.Length > 0)
        {
            builder.Append(": ");
            PlanPrinterHelpers.AppendNames(builder, recursiveCte.Keys);
        }

        builder.AppendLine("]");
        builder.Append(prefix).AppendLine("  Anchor");
        PrintNode(recursiveCte.Anchor, builder, indent + 4);
        foreach (var invariant in recursiveCte.Invariants)
        {
            builder.Append(prefix).Append("  Invariant [").Append(invariant.Name).Append("; ")
                .Append(invariant.StorageKind).Append("; fields ");
            PlanPrinterHelpers.AppendNames(
                builder,
                invariant.Fields.Select(static field => field.OutputName).ToArray());
            builder.AppendLine("]");
            PrintNode(invariant.Plan, builder, indent + 4);
        }

        builder.Append(prefix).AppendLine("  RecursiveMember");
        PrintNode(recursiveCte.RecursiveMember, builder, indent + 4);
    }
}
