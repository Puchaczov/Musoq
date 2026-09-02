using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Printing;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Physical;

public static partial class PhysicalPlanPrinter
{
    private static void PrintUnpivot(PhysicalUnpivotNode unpivot, StringBuilder sb, string prefix, int indent)
    {
        sb.Append(prefix).Append(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalUnpivot [name: {unpivot.NameColumn}; value: {unpivot.ValueColumn}; entries: ");
        AppendUnpivotEntries(sb, unpivot.Entries);
        sb.Append("; keep: ");
        PlanPrinterHelpers.AppendProjectedFields(sb, unpivot.KeepFields.ToArray());
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"] as {unpivot.Alias}");
        PrintNode(unpivot.Source, sb, indent + 2);
    }

    private static void AppendUnpivotEntries(StringBuilder sb, IReadOnlyList<UnpivotEntry> entries)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(IrExpressionPrinter.Print(entries[i].Value));
            sb.Append(" as ");
            sb.Append(entries[i].NameValue);
        }
    }
}
