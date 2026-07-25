using System.Text;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Physical;

public static partial class PhysicalPlanPrinter
{
    private static void PrintCte(
        PhysicalCteNode cte,
        StringBuilder builder,
        string prefix,
        int indent)
    {
        builder.Append(prefix).AppendLine("PhysicalCte");
        foreach (var definition in cte.Definitions)
        {
            builder.Append(prefix).AppendLine(
                System.Globalization.CultureInfo.InvariantCulture,
                $"  Definition [{definition.Name}]");
            PrintNode(definition.Plan, builder, indent + 4);
        }

        builder.Append(prefix).AppendLine("  Query");
        PrintNode(cte.Query, builder, indent + 4);
    }
}
