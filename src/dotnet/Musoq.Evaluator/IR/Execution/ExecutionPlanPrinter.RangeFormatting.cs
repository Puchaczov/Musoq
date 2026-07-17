using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static string FormatRangeIndexKey(ExecutionCreateRangeIndex createIndex)
    {
        return string.Join(", ", (createIndex.PartitionKeys ?? [])
            .Select(key => FormatExpression(key.Right))
            .Concat([FormatExpression(createIndex.CandidateKey)]));
    }

    private static string FormatRangeProbeKey(ExecutionRangeProbe rangeProbe)
    {
        return string.Join(" and ", (rangeProbe.PartitionKeys ?? [])
            .Select(key => $"{FormatExpression(key.Left)} = {FormatExpression(key.Right)}")
            .Concat([FormatExpression(rangeProbe.ProbeKey)]));
    }
}
