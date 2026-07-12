using System.Collections.Generic;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static IEnumerable<ExecutionNode> FlattenNodes(ExecutionBlock block)
    {
        return ExecutionIrAnalysis.FlattenNodes(block);
    }

    private static IEnumerable<ExecutionBlock> GetChildBlocks(ExecutionNode node)
    {
        return ExecutionIrAnalysis.GetChildBlocks(node);
    }

    private static IEnumerable<ExecutionParallelSingleKeyAggregateLoop> CollectParallelSingleKeyAggregateLoops(
        ExecutionBlock block)
    {
        return ExecutionIrAnalysis.CollectNodes<ExecutionParallelSingleKeyAggregateLoop>(block);
    }

    private static IEnumerable<ExecutionParallelFilterProjectLoop> CollectParallelFilterProjectLoops(
        ExecutionBlock block)
    {
        return ExecutionIrAnalysis.CollectNodes<ExecutionParallelFilterProjectLoop>(block);
    }

    private static IEnumerable<ExecutionParallelBlock> CollectParallelBlocks(ExecutionBlock block)
    {
        return ExecutionIrAnalysis.CollectNodes<ExecutionParallelBlock>(block);
    }


    private static IEnumerable<ExecutionConstantInSet> CollectConstantInSets(ExecutionBlock block)
    {
        foreach (var inCheck in ExecutionIrAnalysis.CollectExpressions<ExecutionInCheck>(block))
        {
            if (inCheck.ConstantSet != null)
                yield return inCheck.ConstantSet;
        }
    }

    private static IEnumerable<ExecutionExpression> GetContextLayoutExpressions(ExecutionContextLayout? contextLayout)
    {
        return ExecutionIrAnalysis.GetContextLayoutExpressions(contextLayout);
    }

}
