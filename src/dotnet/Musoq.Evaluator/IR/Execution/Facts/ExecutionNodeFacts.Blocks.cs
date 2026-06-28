using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Facts;

internal static partial class ExecutionNodeFacts
{
    internal static IEnumerable<ExecutionBlock> GetChildBlocks(ExecutionNode node)
    {
        switch (node)
        {
            case ExecutionForEach forEach:
                yield return forEach.Body;
                break;
            case ExecutionForEachWithOrdinality forEach:
                yield return forEach.Body;
                break;
            case ExecutionScopedBlock scopedBlock:
                yield return scopedBlock.Body;
                break;
            case ExecutionForEachIndexed forEachIndexed:
                yield return forEachIndexed.Body;
                break;
            case ExecutionParallelSingleKeyAggregateLoop parallelAggregate:
                yield return parallelAggregate.AggregateBody;
                break;
            case ExecutionIf ifNode:
                yield return ifNode.Body;
                break;
            case ExecutionHashProbe hashProbe:
                yield return hashProbe.Body;
                if (hashProbe.NoMatchBody != null)
                    yield return hashProbe.NoMatchBody;
                break;
            case ExecutionKeySetProbe keySetProbe:
                yield return keySetProbe.Body;
                if (keySetProbe.NoMatchBody != null)
                    yield return keySetProbe.NoMatchBody;
                break;
            case ExecutionAsOfProbe asOfProbe:
                yield return asOfProbe.Body;
                if (asOfProbe.NoMatchBody != null)
                    yield return asOfProbe.NoMatchBody;
                break;
            case ExecutionRangeProbe rangeProbe:
                yield return rangeProbe.Body;
                break;
            case ExecutionParallelBlock parallel:
                foreach (var task in parallel.Tasks)
                    yield return task.Body;
                yield return parallel.Merge.Body;
                break;
            case ExecutionParallelFilterProjectLoop parallelProject:
                yield return parallelProject.ProjectionBody;
                break;
            case ExecutionFusedCteProducer or ExecutionSingleUsePipelineFusionCandidate or
                ExecutionCteReadOnceFusionCandidate or ExecutionCteFusedProducerCandidate:
                yield return GetSingleChildBlock(node);
                break;
            case ExecutionWindowKernelPlan plan:
                yield return new ExecutionBlock(plan.Kernels);
                break;
        }
    }

    private static ExecutionBlock GetSingleChildBlock(ExecutionNode node)
    {
        return node switch
        {
            ExecutionFusedCteProducer fusedCte => fusedCte.Body,
            ExecutionSingleUsePipelineFusionCandidate candidate => candidate.Body,
            ExecutionCteReadOnceFusionCandidate candidate => candidate.Body,
            ExecutionCteFusedProducerCandidate candidate => candidate.Body,
            _ => ExecutionBlock.Empty
        };
    }
}
