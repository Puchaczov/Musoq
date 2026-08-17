using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;
using Musoq.Evaluator.IR.Planning.Cardinality;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed class JoinStrategySelectionPass : IPhysicalOptimizationPass
{
    public string Name => "JoinStrategySelection";

    public OptimizationResult<PhysicalNode> Optimize(PhysicalNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var state = PhysicalOptimizationState.From(context);
        var rewritten = Rewrite(plan, state);

        return ReferenceEquals(plan, rewritten)
            ? OptimizationResult<PhysicalNode>.NoChange(
                plan,
                "No join candidates were present in the physical plan.")
            : OptimizationResult<PhysicalNode>.Changed(
                rewritten,
                "Selected concrete join strategies for join candidates.");
    }

    private static PhysicalNode Rewrite(PhysicalNode node, PhysicalOptimizationState state)
    {
        if (node is PhysicalJoinCandidateNode join)
            return SelectJoinStrategy(join, state);

        return PhysicalPlanRewriter.RewriteChildren(
            node,
            child => Rewrite(child, state));
    }

    private static PhysicalNode SelectJoinStrategy(
        PhysicalJoinCandidateNode join,
        PhysicalOptimizationState state)
    {
        var left = Rewrite(join.Left, state);
        var right = Rewrite(join.Right, state);
        var strategy = PhysicalStrategyRules.ChooseJoinStrategy(
            join.Kind,
            join.OnPredicate,
            left,
            right,
            state.CompilationOptions,
            state.Facts.CardinalityFacts);
        var strategyKind = strategy.Kind;
        var reason = strategy.Reason;
        if (strategyKind == JoinStrategyKind.HashJoin &&
            RequiresNestedLoopForDynamicHashJoin(left, right, state))
        {
            strategyKind = JoinStrategyKind.NestedLoop;
            reason =
                "Physical planning selected nested-loop because generated hash join lowering cannot stream dynamic or expando join inputs.";
        }
        else if (strategyKind == JoinStrategyKind.SortMergeJoin &&
                 RequiresNestedLoopForSortMergeProbe(right, state))
        {
            strategyKind = JoinStrategyKind.NestedLoop;
            reason =
                "Physical planning selected nested-loop because generated sort-merge/range join lowering requires a non-dynamic source-entity or table-row right input.";
        }

        state.AddDecision(new PlanningDecision(
            PlanningDecisionCategory.JoinStrategy,
            "JoinStrategySelection",
            join.Kind.ToString(),
            strategyKind.ToString(),
            PlanningConfidence.High,
            reason));

        if (strategyKind == JoinStrategyKind.NestedLoop)
            state.AddDecision(NestedLoopJoinRiskAdvisor.CreateRiskDecision(
                join.Kind, left, right, state.Facts.CardinalityFacts));

        return strategyKind switch
        {
            JoinStrategyKind.HashJoin => new PhysicalHashJoinNode(
                join.Kind,
                strategy.HashJoin!.BuildKeys,
                strategy.HashJoin.ProbeKeys,
                strategy.HashJoin.Residual,
                left,
                right),
            JoinStrategyKind.SortMergeJoin => new PhysicalSortMergeJoinNode(
                join.Kind,
                strategy.SortMergeJoin!.LeftKey,
                strategy.SortMergeJoin.RightKey,
                strategy.SortMergeJoin.ComparisonKind,
                strategy.SortMergeJoin.Residual,
                left,
                right)
            {
                LeftPartitionKeys = strategy.SortMergeJoin.LeftPartitionKeys,
                RightPartitionKeys = strategy.SortMergeJoin.RightPartitionKeys
            },
            JoinStrategyKind.NestedLoop => new PhysicalNestedLoopJoinNode(
                join.Kind,
                join.OnPredicate,
                left,
                right,
                join.TieBreak),
            _ => throw UnsupportedShape.Of($"Join strategy '{strategy.Kind}'")
        };
    }

    private static bool RequiresNestedLoopForDynamicHashJoin(
        PhysicalNode left,
        PhysicalNode right,
        PhysicalOptimizationState state)
    {
        return IsDynamicFlatJoinInput(left, state) ||
               IsDynamicFlatJoinInput(right, state);
    }

    private static bool RequiresNestedLoopForSortMergeProbe(
        PhysicalNode right,
        PhysicalOptimizationState state)
    {
        var source = UnwrapFilterInput(right);
        if (source is PhysicalSchemaScanNode scan)
        {
            var shape = state.ShapeResolver.ResolveSourceShape(scan);
            return !CanUseRangeProbeSource(shape);
        }

        return source is PhysicalValuesScanNode or PhysicalPropertySourceNode or PhysicalAccessMethodSourceNode;
    }

    private static bool IsDynamicFlatJoinInput(PhysicalNode node, PhysicalOptimizationState state)
    {
        return UnwrapFilterInput(node) is PhysicalSchemaScanNode scan &&
               state.ShapeResolver.ResolveSourceShape(scan).Kind == PlanningRowShapeKind.ExpandoAdapter;
    }

    private static PhysicalNode UnwrapFilterInput(PhysicalNode node)
    {
        while (node is PhysicalFilterNode filter)
            node = filter.Input;

        return node;
    }

    private static bool CanUseRangeProbeSource(PlanningRowShape sourceShape)
    {
        return sourceShape.Kind is PlanningRowShapeKind.SourceEntity or PlanningRowShapeKind.TableRow &&
               !sourceShape.RuntimeType.IsValueType &&
               !DynamicEntityBoundary.IsDynamicMetaObjectProvider(sourceShape.RuntimeType);
    }
}

