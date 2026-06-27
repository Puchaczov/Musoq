using System.Text;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Printing;

namespace Musoq.Evaluator.IR.Physical;

public static partial class PhysicalPlanPrinter
{
    private static void PrintDesc(PhysicalDescNode desc, StringBuilder sb, string prefix)
    {
        if (desc.Type == DescType.Query)
        {
            sb.Append(prefix).AppendLine("PhysicalDescQuery");
            return;
        }

        sb.Append(prefix).AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"PhysicalDesc [{PlanPrinterHelpers.FormatSchemaName(desc.SchemaName)}.{desc.MethodName}()] [{desc.Type}] [{desc.Column}]");
    }
}
