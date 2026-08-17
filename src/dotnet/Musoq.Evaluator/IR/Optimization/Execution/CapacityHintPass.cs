using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal sealed class CapacityHintPass : IExecutionIrOptimizationPass
{
    public string Name => "CapacityHints";

    public OptimizationResult<ExecutionPlan> Optimize(ExecutionPlan plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        var candidateRewriter = new CapacityHintCandidateLoweringRewriter();
        var candidateOptimized = candidateRewriter.RewritePlan(plan);
        if (candidateRewriter.LoweredCandidates > 0)
        {
            return OptimizationResult<ExecutionPlan>.Changed(
                candidateOptimized,
                FormatCandidateDiagnostics(candidateRewriter));
        }

        if (candidateRewriter.SkippedCandidates > 0)
        {
            return ReferenceEquals(candidateOptimized, plan)
                ? OptimizationResult<ExecutionPlan>.NoChange(plan, FormatCandidateDiagnostics(candidateRewriter))
                : OptimizationResult<ExecutionPlan>.Changed(candidateOptimized, FormatCandidateDiagnostics(candidateRewriter));
        }

        return OptimizationResult<ExecutionPlan>.NoChange(
            plan,
            $"Observed {CountCapacityHints(plan.Body)} finalized capacity hint(s); no capacity hint candidates were present.");
    }

    private static string FormatCandidateDiagnostics(CapacityHintCandidateLoweringRewriter rewriter)
    {
        return
            $"Consumed {rewriter.LoweredCandidates} capacity hint candidate(s){FormatCounts(rewriter.LoweredCandidateKinds)}; " +
            $"skipped {rewriter.SkippedCandidates} unsupported candidate(s){FormatCounts(rewriter.SkippedCandidateKinds)}.";
    }

    private static string FormatCounts(IReadOnlyDictionary<string, int> counts)
    {
        return counts.Count == 0
            ? string.Empty
            : $" [{string.Join(", ", counts.OrderBy(static entry => entry.Key).Select(static entry => $"{entry.Key}={entry.Value}"))}]";
    }

    private static int CountCapacityHints(ExecutionBlock block)
    {
        return ExecutionIrAnalysis.FlattenNodes(block).Count(static node => node switch
        {
            ExecutionCreateTable { CapacityHint: { } hint } => !ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionCreateRecordList { CapacityHint: { } hint } => !ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionEnsureTableCapacity { CapacityHint: { } hint } => !ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionCreateHash { CapacityHint: { } hint } => !ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionCreateKeySet { CapacityHint: { } hint } => !ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionSortTable { CapacityHint: { } hint } => !ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionTopNTable { CapacityHint: { } hint } => !ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionTopOffsetTable { CapacityHint: { } hint } => !ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionSkipTable { CapacityHint: { } hint } => !ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionTakeTable { CapacityHint: { } hint } => !ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionSliceTable { CapacityHint: { } hint } => !ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionProjectTable { CapacityHint: { } hint } => !ExecutionCapacityHintCandidates.IsCandidate(hint),
            ExecutionMaterializeRecordListToTable { CapacityHint: { } hint } => !ExecutionCapacityHintCandidates.IsCandidate(hint),
            _ => false
        });
    }

    private sealed class CapacityHintCandidateLoweringRewriter : ExecutionIrRewriter
    {
        public int LoweredCandidates { get; private set; }

        public int SkippedCandidates { get; private set; }

        public IReadOnlyDictionary<string, int> LoweredCandidateKinds => _loweredCandidateKinds;

        public IReadOnlyDictionary<string, int> SkippedCandidateKinds => _skippedCandidateKinds;

        private readonly Dictionary<string, int> _loweredCandidateKinds = new(StringComparer.Ordinal);

        private readonly Dictionary<string, int> _skippedCandidateKinds = new(StringComparer.Ordinal);

        protected override ExecutionCapacityHint RewriteCapacityHint(ExecutionCapacityHint capacityHint)
        {
            if (!ExecutionCapacityHintCandidates.IsCandidate(capacityHint))
                return base.RewriteCapacityHint(capacityHint);

            capacityHint = base.RewriteCapacityHint(capacityHint);
            var candidateKind = ExecutionCapacityHintCandidates.GetCandidateDiagnosticName(capacityHint);
            if (ExecutionCapacityHintCandidates.TryLower(capacityHint, out var lowered) && lowered != null)
            {
                LoweredCandidates++;
                CountCandidateKind(_loweredCandidateKinds, candidateKind);
                return lowered;
            }

            SkippedCandidates++;
            CountCandidateKind(_skippedCandidateKinds, candidateKind);
            return capacityHint;
        }

        private static void CountCandidateKind(Dictionary<string, int> counts, string candidateKind)
        {
            counts[candidateKind] = counts.GetValueOrDefault(candidateKind) + 1;
        }
    }
}

