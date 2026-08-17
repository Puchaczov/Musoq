using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal sealed class CteReadOnceFusionPass : IExecutionIrOptimizationPass
{
    public string Name => "CteReadOnceFusion";

    public OptimizationResult<ExecutionPlan> Optimize(ExecutionPlan plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var rewriter = new CteReadOnceFusionRewriter();
        var optimized = rewriter.RewritePlan(plan);
        if (!ReferenceEquals(optimized, plan))
        {
            return OptimizationResult<ExecutionPlan>.Changed(
                optimized,
                $"Expanded {rewriter.ExpandedCandidates} selected read-once CTE fusion candidate(s).");
        }

        var relatedPhaseCount = ExecutionIrAnalysis.CollectNodes<ExecutionRelatedCtePhase>(plan.Body).Count();

        return OptimizationResult<ExecutionPlan>.NoChange(
            plan,
            $"Observed {relatedPhaseCount} related CTE phase marker(s); no selected read-once fusion candidates required expansion.");
    }

    private sealed class CteReadOnceFusionRewriter : ExecutionCteCandidateExpansionRewriter
    {
        public int ExpandedCandidates { get; private set; }

        protected override bool TryExpandCandidate(
            ExecutionNode node,
            out IReadOnlyList<ExecutionNode> expandedNodes)
        {
            if (node is not ExecutionCteReadOnceFusionCandidate candidate)
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

