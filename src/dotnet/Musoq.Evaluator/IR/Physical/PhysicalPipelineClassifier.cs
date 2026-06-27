using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Physical;

internal static class PhysicalPipelineClassifier
{
    internal static bool TryDecomposeSourcePipeline(
        PhysicalNode input,
        out PhysicalSourcePipeline pipeline)
    {
        if (TryGetSupportedSource(input, out var source))
        {
            pipeline = new PhysicalSourcePipeline(source, null);
            return true;
        }

        if (input is PhysicalFilterNode filter &&
            TryGetSupportedSource(filter.Input, out source))
        {
            pipeline = new PhysicalSourcePipeline(source, filter);
            return true;
        }

        pipeline = default;
        return false;
    }

    internal static PhysicalNode? GetPostOperationInput(PhysicalNode node)
    {
        return node switch
        {
            PhysicalSortNode sort => sort.Input,
            PhysicalTopNNode topN => topN.Input,
            PhysicalTopOffsetNode topOffset => topOffset.Input,
            PhysicalSkipNode skip => skip.Input,
            PhysicalTakeNode take => take.Input,
            _ => null
        };
    }

    private static bool TryGetSupportedSource(PhysicalNode input, out PhysicalNode source) {
        if (input is PhysicalSchemaScanNode
            or PhysicalCteRefNode
            or PhysicalInterpretSourceNode
            or PhysicalValuesScanNode
            or PhysicalUnpivotNode
            or PhysicalNestedLoopJoinNode
            or PhysicalHashJoinNode
            or PhysicalSortMergeJoinNode
            or PhysicalNestedLoopApplyNode)
        {
            source = input;
            return true;
        }

        source = null!;
        return false;
    }
}
