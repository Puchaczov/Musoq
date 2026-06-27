using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class SingleUsePipelineFusionPass : IPlanOptimizationPass<ExecutionPlan>
{
    public string Name => "SingleUsePipelineFusion";

    public OptimizationResult<ExecutionPlan> Optimize(ExecutionPlan plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var rewriter = new SingleUsePipelineFusionRewriter();
        var optimized = rewriter.RewritePlan(plan);
        if (!ReferenceEquals(optimized, plan))
        {
            return OptimizationResult<ExecutionPlan>.Changed(
                optimized,
                $"Expanded {rewriter.ExpandedCandidates} selected single-use fusion candidate(s) into related phase marker(s).");
        }

        var producers = ExecutionIrAnalysis.CollectNodes<ExecutionFusedCteProducer>(plan.Body).ToArray();
        var outputCount = producers.Sum(static producer => producer.Outputs.Count);

        return OptimizationResult<ExecutionPlan>.NoChange(
            plan,
            $"Observed {producers.Length} fused producer(s) with {outputCount} output(s); no selected fusion candidates required expansion.");
    }

    private sealed class SingleUsePipelineFusionRewriter : ExecutionCteCandidateExpansionRewriter
    {
        public int ExpandedCandidates { get; private set; }

        protected override bool TryExpandCandidate(
            ExecutionNode node,
            out IReadOnlyList<ExecutionNode> expandedNodes)
        {
            if (node is not ExecutionSingleUsePipelineFusionCandidate candidate)
            {
                expandedNodes = [];
                return false;
            }

            var body = RewriteBlock(candidate.Body);
            expandedNodes = [new ExecutionRelatedCtePhase(candidate.RelatedTableIndex), .. body.Nodes];
            ExpandedCandidates++;
            return true;
        }
    }
}
