using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal sealed class CteSidecarIndexLoweringPass : IExecutionIrOptimizationPass
{
    public string Name => "CteSidecarIndexLowering";

    public OptimizationResult<ExecutionPlan> Optimize(ExecutionPlan plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var indexOnlyCandidates = ExecutionIrAnalysis
            .CollectNodes<ExecutionCteIndexOnlyStorageCandidate>(plan.Body)
            .ToArray();
        var rewriter = new CteSidecarIndexLoweringRewriter();
        var expanded = rewriter.RewritePlan(plan);
        var optimized = indexOnlyCandidates.Length == 0
            ? expanded
            : CteIndexOnlyStoragePruner.Apply(expanded, indexOnlyCandidates);
        if (!ReferenceEquals(optimized, plan))
        {
            return OptimizationResult<ExecutionPlan>.Changed(
                optimized,
                $"Lowered {rewriter.LoweredStores} sidecar store candidate(s), {rewriter.LoweredLoads} sidecar load candidate(s), {rewriter.ExpandedBuildCandidates} sidecar build candidate(s), {rewriter.ExpandedAppendRewriteCandidates} sidecar append rewrite candidate(s), {rewriter.ExpandedFusedProducerCandidates} fused producer candidate(s), and {indexOnlyCandidates.Length} index-only storage candidate(s).");
        }

        var stores = ExecutionIrAnalysis.CollectNodes<ExecutionStoreCteIndex>(plan.Body).ToArray();
        var loads = ExecutionIrAnalysis.CollectNodes<ExecutionLoadCteIndex>(plan.Body).ToArray();
        var hashCount = stores.Count(static store => store.Kind == ExecutionCteSidecarIndexKind.Hash) +
                        loads.Count(static load => load.Kind == ExecutionCteSidecarIndexKind.Hash);
        var keySetCount = stores.Count(static store => store.Kind == ExecutionCteSidecarIndexKind.KeySet) +
                          loads.Count(static load => load.Kind == ExecutionCteSidecarIndexKind.KeySet);

        return OptimizationResult<ExecutionPlan>.NoChange(
            plan,
            $"Observed {hashCount} hash sidecar operation(s) and {keySetCount} keyset sidecar operation(s); no selected sidecar candidates required lowering.");
    }

    private sealed class CteSidecarIndexLoweringRewriter : ExecutionCteCandidateExpansionRewriter
    {
        private readonly CteSidecarIndexLoweringFactory _factory = new();

        public int LoweredStores { get; private set; }

        public int LoweredLoads { get; private set; }

        public int ExpandedBuildCandidates { get; private set; }

        public int ExpandedAppendRewriteCandidates { get; private set; }

        public int ExpandedFusedProducerCandidates { get; private set; }

        protected override bool TryExpandCandidate(ExecutionNode node, out IReadOnlyList<ExecutionNode> expandedNodes)
        {
            switch (node)
            {
                case ExecutionCteSidecarIndexBuildCandidate candidate:
                {
                    expandedNodes = _factory.CreateIndexBuildNodes(candidate)
                        .Select(RewriteNode)
                        .ToArray();
                    ExpandedBuildCandidates++;
                    return true;
                }
                case ExecutionCteSidecarAppendRewriteCandidate candidate:
                {
                    expandedNodes = _factory.CreateAppendRewriteNodes(candidate)
                        .Select(RewriteNode)
                        .ToArray();
                    ExpandedAppendRewriteCandidates++;
                    return true;
                }
                case ExecutionCteFusedProducerCandidate candidate:
                {
                    expandedNodes =
                    [
                        new ExecutionFusedCteProducer(
                            candidate.Outputs,
                            RewriteBlock(candidate.Body))
                    ];
                    ExpandedFusedProducerCandidates++;
                    return true;
                }
                default:
                    expandedNodes = [];
                    return false;
            }
        }

        protected override ExecutionNode RewriteCteSidecarIndexStoreCandidate(ExecutionCteSidecarIndexStoreCandidate node)
        {
            LoweredStores++;
            return new ExecutionStoreCteIndex(
                node.Index,
                node.IndexSlot,
                node.Kind,
                node.KeyType,
                node.RowType,
                node.GeneratedRowTypeName);
        }

        protected override ExecutionNode RewriteCteSidecarIndexLoadCandidate(ExecutionCteSidecarIndexLoadCandidate node)
        {
            LoweredLoads++;
            return new ExecutionLoadCteIndex(
                node.Index,
                node.IndexSlot,
                node.Kind,
                node.KeyType,
                node.RowType,
                node.GeneratedRowTypeName);
        }
    }
}

