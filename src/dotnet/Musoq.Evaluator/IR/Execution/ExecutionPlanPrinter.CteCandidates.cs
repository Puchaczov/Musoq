using System.Globalization;
using System.Text;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static void AppendCteStrategyCandidateNode(StringBuilder builder, ExecutionNode node, string prefix)
    {
        switch (node)
        {
            case ExecutionCteSidecarIndexBuildCandidate candidate:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CteSidecarIndexBuildCandidate [indexes {candidate.Indexes.Count.ToString(CultureInfo.InvariantCulture)}]");
                break;
            case ExecutionCteSidecarAppendRewriteCandidate candidate:
                builder.AppendLine(
                    CultureInfo.InvariantCulture,
                    $"{prefix}CteSidecarAppendRewriteCandidate [{candidate.AppendRow.Table.Name} <- {candidate.AppendRow.RowShape.TypeName}({FormatRowValues(candidate.AppendRow.Values)}); indexes {candidate.Indexes.Count.ToString(CultureInfo.InvariantCulture)}]");
                break;
            case ExecutionCteIndexOnlyStorageCandidate candidate:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CteIndexOnlyStorageCandidate [{candidate.TableName}: {candidate.RowTypeName}, keepPayloadRows {candidate.KeepPayloadRows}]");
                break;
        }
    }

    private static void AppendCteProducerNode(
        StringBuilder builder,
        ExecutionNode node,
        int indentation,
        string prefix)
    {
        switch (node)
        {
            case ExecutionFusedCteProducer fusedCte:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}FusedCteProducer [{FormatFusedCteOutputs(fusedCte.Outputs)}]");
                AppendBlock(builder, fusedCte.Body, indentation + 2);
                break;
            case ExecutionCteFusedProducerCandidate candidate:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CteFusedProducerCandidate [{FormatFusedCteOutputs(candidate.Outputs)}]");
                AppendBlock(builder, candidate.Body, indentation + 2);
                break;
        }
    }
}
