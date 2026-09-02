using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal sealed partial class ExpressionCseHoistingPass : IExecutionIrOptimizationPass
{
    public string Name => "ExpressionCseHoisting";

    public OptimizationResult<ExecutionPlan> Optimize(ExecutionPlan plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        if (!IsExpressionCseEnabled(context) && !context.Options.StabilityAwareScalarReuseEnabled)
        {
            return OptimizationResult<ExecutionPlan>.NoChange(
                plan,
                "Expression CSE discovery is disabled by compilation options.");
        }

        var skipDiagnostics = ExpressionCseSkipDiagnostics.Analyze(plan);
        var rewriter = new ExpressionCseRewriter(
            IsCrossNodeExpressionCseEnabled(context) || context.Options.StabilityAwareScalarReuseEnabled,
            context.Options.ExpressionCseEnabled,
            context.Options.StabilityAwareScalarReuseEnabled);
        var optimized = rewriter.RewritePlan(plan);
        if (ReferenceEquals(optimized, plan))
        {
            return OptimizationResult<ExecutionPlan>.NoChange(
                plan,
                "No repeated deterministic expressions were found in supported append values." +
                FormatSkippedDiagnostics(skipDiagnostics));
        }

        return OptimizationResult<ExecutionPlan>.Changed(
            optimized,
            $"Inserted {rewriter.InsertedLets} expression CSE hoist(s) in {rewriter.RewrittenNodes} Execution IR node(s){FormatHelperRewriteCount(rewriter)}." +
            FormatSkippedDiagnostics(skipDiagnostics));
    }

    private static bool IsExpressionCseEnabled(OptimizationContext context)
    {
        return context.Options.ExpressionCseEnabled;
    }

    private static bool IsCrossNodeExpressionCseEnabled(OptimizationContext context)
    {
        return context.Options.CrossNodeExpressionCseEnabled;
    }

    private static string FormatHelperRewriteCount(ExpressionCseRewriter rewriter)
    {
        return rewriter.HelperBodyRewrittenNodes == 0
            ? string.Empty
            : $" including {rewriter.HelperBodyRewrittenNodes} helper-body node(s)";
    }

    private static string FormatSkippedDiagnostics(ExpressionCseSkipDiagnosticSummary diagnostics)
    {
        return diagnostics.HasSkippedOpportunities
            ? $" Skipped CSE opportunities remain in unsupported scopes: {diagnostics.Format()}."
            : string.Empty;
    }

    private sealed partial class ExpressionCseRewriter(
        bool enableCrossNodeCse,
        bool enableExpressionCse,
        bool enableStabilityAwareRegionReuse) : ExecutionIrRewriter
    {
        private readonly Stack<HashSet<string>> _visibleNameScopes = new();
        private int _helperBodyDepth;

        public int InsertedLets { get; private set; }

        public int RewrittenNodes { get; private set; }

        public int HelperBodyRewrittenNodes { get; private set; }

        public override ExecutionPlan RewritePlan(ExecutionPlan plan)
        {
            try
            {
                return base.RewritePlan(plan);
            }
            finally
            {
                _visibleNameScopes.Clear();
            }
        }

        public override ExecutionBlock RewriteBlock(ExecutionBlock block)
        {
            var usedNames = CreateUsedNameScope(block);
            _visibleNameScopes.Push(usedNames);
            try
            {
                var builder = new ExecutionBlockRewriteBuilder(block);

                for (var index = 0; index < block.Nodes.Count; index++)
                {
                    var node = block.Nodes[index];
                    var (rewrittenNode, hoisted) = RewriteNodeWithHoisting(node, usedNames);
                    var isChanged = !ReferenceEquals(rewrittenNode, node) || hoisted.Lets.Count > 0;

                    if (!isChanged && !builder.HasChanges)
                        continue;

                    builder.EnsureStartedAt(index);
                    foreach (var let in hoisted.Lets)
                    {
                        builder.Add(let);
                        usedNames.Add(let.Variable.Name);
                        InsertedLets++;
                    }

                    if (hoisted.Lets.Count > 0)
                    {
                        RewrittenNodes++;
                        if (_helperBodyDepth > 0)
                            HelperBodyRewrittenNodes++;
                    }

                    builder.Add(hoisted.Node);
                }

                var rewrittenBlock = builder.ToBlock();
                if (!enableCrossNodeCse)
                    return rewrittenBlock;

                var hoistedBlock = TryHoistBlockExpressions(rewrittenBlock, usedNames, enableStabilityAwareRegionReuse);
                if (hoistedBlock.Lets.Count > 0)
                {
                    InsertedLets += hoistedBlock.Lets.Count;
                    RewrittenNodes++;
                    if (_helperBodyDepth > 0)
                        HelperBodyRewrittenNodes++;
                }

                return hoistedBlock.Block;
            }
            finally
            {
                _visibleNameScopes.Pop();
            }
        }

        private HashSet<string> CreateUsedNameScope(ExecutionBlock block)
        {
            var usedNames = _visibleNameScopes.Count == 0
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(_visibleNameScopes.Peek(), StringComparer.Ordinal);

            foreach (var name in ExecutionIrAnalysis.CollectDeclaredVariableNames(block))
                usedNames.Add(name);

            return usedNames;
        }

        protected override ExecutionNode RewriteForEachIndexed(ExecutionForEachIndexed node)
        {
            _helperBodyDepth++;
            try
            {
                return base.RewriteForEachIndexed(node);
            }
            finally
            {
                _helperBodyDepth--;
            }
        }

        protected override ExecutionNode RewriteParallelSingleKeyAggregateLoop(ExecutionParallelSingleKeyAggregateLoop node)
        {
            var sourceRows = RewriteExpression(node.SourceRows);
            var key = RewriteExpression(node.Key);
            var aggregateBody = RewriteBlock(node.AggregateBody);

            return ReferenceEquals(sourceRows, node.SourceRows) &&
                   ReferenceEquals(key, node.Key) &&
                   ReferenceEquals(aggregateBody, node.AggregateBody)
                ? node
                : node with
                {
                    SourceRows = sourceRows,
                    Key = key,
                    AggregateBody = aggregateBody
                };
        }

        private (ExecutionNode RewrittenNode, HoistedNode Hoisted) RewriteNodeWithHoisting(
            ExecutionNode node,
            HashSet<string> usedNames)
        {
            if (enableCrossNodeCse &&
                node is ExecutionIf branch)
            {
                var condition = RewriteExpression(branch.Condition);
                var branchWithCondition = ReferenceEquals(condition, branch.Condition)
                    ? branch
                    : branch with { Condition = condition };
                var hoisted = TryHoistExpressionsSharedWithCondition(branchWithCondition, usedNames);
                var hoistedBranch = (ExecutionIf)hoisted.Node;
                var conditionHoisted = TryHoistConditionExpressions(hoistedBranch, usedNames);
                var conditionHoistedBranch = (ExecutionIf)conditionHoisted.Node;
                var body = RewriteBlock(conditionHoistedBranch.Body);
                var rewrittenBranch = TrySplitShortCircuitConditionAroundBodyLet(
                    ReferenceEquals(body, conditionHoistedBranch.Body)
                        ? conditionHoistedBranch
                        : conditionHoistedBranch with { Body = body });
                var lets = CombineLets(hoisted.Lets, conditionHoisted.Lets);

                return (rewrittenBranch, new HoistedNode(lets, rewrittenBranch));
            }

            var rewrittenNode = RewriteNode(node);
            return (rewrittenNode, TryHoistExpressions(rewrittenNode, usedNames));
        }

        private HoistedNode TryHoistExpressions(ExecutionNode node, HashSet<string> usedNames)
        {
            if (!enableExpressionCse)
                return new HoistedNode([], node);

            var expressions = GetSupportedNodeExpressions(node);
            if (expressions.Count == 0)
                return new HoistedNode([], node);

            var plan = ExpressionHoistPlanner.Create(
                expressions.SelectMany(static value => ExecutionExpressionCseFacts.CollectHoistableOccurrences(value)),
                usedNames);
            if (plan.Lets.Count == 0)
                return new HoistedNode([], node);

            var rewritten = ExpressionCseSubstitution.ReplaceSupportedNodeExpressions(
                node,
                plan.VariablesBySignature);

            return new HoistedNode(plan.Lets, rewritten);
        }

        private static IReadOnlyList<ExecutionLet> CombineLets(
            IReadOnlyList<ExecutionLet> first,
            IReadOnlyList<ExecutionLet> second)
        {
            return first.Count == 0
                ? second
                : second.Count == 0
                    ? first
                    : [..first, ..second];
        }

        private static HoistedBlock TryHoistBlockExpressions(
            ExecutionBlock block,
            HashSet<string> usedNames,
            bool enableStabilityAwareRegionReuse)
        {
            var isAggregateBlock = IsAggregateAccumulationBlock(block);
            if (!isAggregateBlock && (!enableStabilityAwareRegionReuse || !IsStableScalarReuseRegion(block)))
                return new HoistedBlock([], block);

            var insertionIndex = isAggregateBlock
                ? FindAggregateHoistInsertionIndex(block)
                : FindStableScalarReuseInsertionIndex(block);
            var prefix = block.Nodes.Take(insertionIndex).ToArray();
            var hoistRegion = new ExecutionBlock(block.Nodes.Skip(insertionIndex).ToArray());
            var occurrences = enableStabilityAwareRegionReuse
                ? ExecutionExpressionCseFacts.CollectStableScalarReuseOccurrences(hoistRegion)
                : ExecutionExpressionCseFacts.CollectHoistableOccurrences(hoistRegion);
            var plan = ExpressionHoistPlanner.Create(
                occurrences,
                usedNames);
            if (plan.Lets.Count == 0)
                return new HoistedBlock([], block);

            foreach (var let in plan.Lets)
                usedNames.Add(let.Variable.Name);

            var rewritten = isAggregateBlock
                ? ExpressionCseSubstitution.ReplaceAggregateBlockExpressions(
                    hoistRegion,
                    plan.VariablesBySignature)
                : new ExecutionBlock(hoistRegion.Nodes
                    .Select(node => ExpressionCseSubstitution.ReplaceSupportedNodeExpressions(
                        node,
                        plan.VariablesBySignature))
                    .ToArray());
            return new HoistedBlock(
                plan.Lets,
                block with { Nodes = [..prefix, ..plan.Lets, ..rewritten.Nodes] });
        }

        private static HoistedNode TryHoistExpressionsSharedWithCondition(
            ExecutionIf branch,
            HashSet<string> usedNames)
        {
            if (!CanHoistAcrossConditionBody(branch.Body))
                return new HoistedNode([], branch);

            var conditionOccurrences = ExecutionExpressionCseFacts.CollectHoistableOccurrences(branch.Condition).ToArray();
            if (conditionOccurrences.Length == 0)
                return new HoistedNode([], branch);

            var conditionSignatures = conditionOccurrences
                .Where(static occurrence => occurrence.IsSafeOrigin)
                .Select(static occurrence => occurrence.Signature)
                .ToHashSet(StringComparer.Ordinal);
            if (conditionSignatures.Count == 0)
                return new HoistedNode([], branch);

            var blockOccurrences = ExecutionExpressionCseFacts.CollectHoistableOccurrences(branch.Body).ToArray();
            if (blockOccurrences.Length == 0)
                return new HoistedNode([], branch);

            var plan = ExpressionHoistPlanner.Create(
                conditionOccurrences.Concat(blockOccurrences),
                usedNames,
                conditionSignatures);
            if (plan.Lets.Count == 0)
                return new HoistedNode([], branch);

            var rewritten = branch with
            {
                Condition = ExpressionCseSubstitution.Replace(branch.Condition, plan.VariablesBySignature),
                Body = ExpressionCseSubstitution.ReplaceAggregateBlockExpressions(branch.Body, plan.VariablesBySignature)
            };

            return new HoistedNode(plan.Lets, rewritten);
        }

        private static HoistedNode TryHoistConditionExpressions(
            ExecutionIf branch,
            HashSet<string> usedNames)
        {
            var plan = ExpressionHoistPlanner.Create(
                ExecutionExpressionCseFacts.CollectHoistableOccurrences(branch.Condition),
                usedNames);
            if (plan.Lets.Count == 0)
                return new HoistedNode([], branch);

            return new HoistedNode(
                plan.Lets,
                branch with { Condition = ExpressionCseSubstitution.Replace(branch.Condition, plan.VariablesBySignature) });
        }

        private static bool CanHoistAcrossConditionBody(ExecutionBlock body)
        {
            return ContainsAppendOutputNode(body) || IsAggregateAccumulationBlock(body);
        }

        private static bool ContainsAppendOutputNode(ExecutionBlock block)
        {
            foreach (var node in block.Nodes)
            {
                if (node is ExecutionAppendRow or ExecutionAppendRecord)
                    return true;

                if (node is ExecutionIf branch && ContainsAppendOutputNode(branch.Body))
                    return true;
            }

            return false;
        }

        private static bool IsAggregateAccumulationBlock(ExecutionBlock block)
        {
            return block.Nodes.Any(IsAggregateAccumulationNode);
        }

        private static bool IsStableScalarReuseRegion(ExecutionBlock block)
        {
            var regionNodes = block.Nodes
                .SkipWhile(IsPrologueNode)
                .ToArray();
            return regionNodes.Length > 1 &&
                   regionNodes.All(static node => node is
                       ExecutionAppendRow or
                       ExecutionAppendRecord or
                       ExecutionCreateGeneratedRow or
                       ExecutionCreateHashPayload or
                       ExecutionHashAdd or
                       ExecutionHashProbe or
                       ExecutionKeySetAdd or
                       ExecutionKeySetProbe or
                       ExecutionGetOrAddSingleKeyAggregateGroup or
                       ExecutionGetOrAddValueTupleAggregateGroup or
                       ExecutionAggregateSet or
                       ExecutionAggregateCapturedValueSet or
                       ExecutionComputeRankingWindow or
                       ExecutionComputeOffsetWindow or
                       ExecutionComputePluginWindow or
                       ExecutionWindowAggregateKernel or
                       ExecutionCreateRangeIndex or
                       ExecutionRangeProbe or
                       ExecutionRecursiveCteAppend);
        }

        private static int FindStableScalarReuseInsertionIndex(ExecutionBlock block)
        {
            for (var index = 0; index < block.Nodes.Count; index++)
            {
                if (!IsPrologueNode(block.Nodes[index]))
                    return index;
            }

            return block.Nodes.Count;
        }

        private static bool IsPrologueNode(ExecutionNode node)
        {
            return node is ExecutionLet or
                ExecutionAdaptExpando or
                ExecutionMethodTargetDeclarationCandidate or
                ExecutionCreateGeneratedRow or
                ExecutionCreateAsOfIndex;
        }

        private static int FindAggregateHoistInsertionIndex(ExecutionBlock block)
        {
            for (var index = 0; index < block.Nodes.Count; index++)
            {
                if (IsAggregateAccumulationNode(block.Nodes[index]))
                    return index;
            }

            return 0;
        }

        private static bool IsAggregateAccumulationNode(ExecutionNode node)
        {
            return node is ExecutionEnsureAggregateGroup or
                ExecutionGetOrAddSingleKeyAggregateGroup or
                ExecutionGetOrAddValueTupleAggregateGroup or
                ExecutionAggregateSet or
                ExecutionAggregateCapturedValueSet;
        }

        private IReadOnlyList<ExecutionExpression> GetSupportedNodeExpressions(ExecutionNode node)
        {
            return node switch
            {
                ExecutionAppendRow appendRow => appendRow.Values.Select(static value => value.Value).ToArray(),
                ExecutionAppendRecord appendRecord => appendRecord.Values.Select(static value => value.Value).ToArray(),
                ExecutionHashAdd hashAdd => [hashAdd.Key],
                ExecutionHashProbe hashProbe => [hashProbe.Key],
                ExecutionKeySetAdd keySetAdd => [keySetAdd.Key],
                ExecutionKeySetProbe keySetProbe => [keySetProbe.Key],
                ExecutionComputeRankingWindow or
                    ExecutionComputeOffsetWindow or
                    ExecutionComputePluginWindow or
                    ExecutionWindowAggregateKernel => ExecutionExpressionCseFacts.GetWindowHelperIndependentExpressions(node),
                _ when !enableStabilityAwareRegionReuse => [],
                ExecutionCreateGeneratedRow createRow => createRow.Values
                    .Select(static value => value.Value)
                    .Concat(createRow.Contexts)
                    .Concat(ExecutionNodeFacts.GetContextLayoutExpressions(createRow.ContextLayout))
                    .ToArray(),
                ExecutionCreateHashPayload payload => payload.Values.Select(static value => value.Value).ToArray(),
                ExecutionGetOrAddSingleKeyAggregateGroup getOrAdd => [getOrAdd.Key],
                ExecutionGetOrAddValueTupleAggregateGroup getOrAdd => getOrAdd.Keys,
                ExecutionAggregateSet aggregateSet => aggregateSet.Arguments
                    .Concat(aggregateSet.FilterPredicate == null ? [] : [aggregateSet.FilterPredicate])
                    .Concat(aggregateSet.AccumulatorInput == null ? [] : [aggregateSet.AccumulatorInput])
                    .ToArray(),
                ExecutionAggregateCapturedValueSet capturedValueSet => [capturedValueSet.Value],
                ExecutionCreateRangeIndex or
                    ExecutionRangeProbe => ExecutionNodeFacts.GetLocalExpressions(node).ToArray(),
                ExecutionRecursiveCteAppend append => append.AppendRow.Values
                    .Select(static value => value.Value)
                    .Concat(append.AppendRow.Contexts)
                    .Concat(ExecutionNodeFacts.GetContextLayoutExpressions(append.AppendRow.ContextLayout))
                    .ToArray(),
                _ => []
            };
        }

    }

    private sealed record HoistedNode(
        IReadOnlyList<ExecutionLet> Lets,
        ExecutionNode Node);

    private sealed record HoistedBlock(
        IReadOnlyList<ExecutionLet> Lets,
        ExecutionBlock Block);
}
