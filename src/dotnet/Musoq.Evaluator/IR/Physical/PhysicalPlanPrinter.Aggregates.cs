using System.Text;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Printing;

namespace Musoq.Evaluator.IR.Physical;

public static partial class PhysicalPlanPrinter
{
    private static void PrintAggregateCandidate(
        PhysicalAggregateCandidateNode aggregateCandidate,
        StringBuilder sb,
        string prefix,
        int indent)
    {
        sb.Append(prefix).Append("PhysicalAggregateCandidate [keys: ");
        PlanPrinterHelpers.AppendNames(sb, aggregateCandidate.GroupKeyNames);
        sb.Append("] [aggs: ");
        PlanPrinterHelpers.AppendAggregateBindings(sb, aggregateCandidate.Bindings);
        sb.AppendLine("]");
        PrintNode(aggregateCandidate.Input, sb, indent + 2);
    }
}
