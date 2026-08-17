using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed class AggregateStrategySelectionPass : IPhysicalOptimizationPass
{
    public string Name => "AggregateStrategySelection";

    public OptimizationResult<PhysicalNode> Optimize(PhysicalNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var state = PhysicalOptimizationState.From(context);
        var rewritten = Rewrite(plan, state);

        return ReferenceEquals(plan, rewritten)
            ? OptimizationResult<PhysicalNode>.NoChange(
                plan,
                "No aggregate candidates were present in the physical plan.")
            : OptimizationResult<PhysicalNode>.Changed(
                rewritten,
                "Selected concrete aggregate strategies for aggregate candidates.");
    }

    private static PhysicalNode Rewrite(PhysicalNode node, PhysicalOptimizationState state)
    {
        if (node is PhysicalAggregateCandidateNode aggregate)
            return SelectAggregateStrategy(aggregate, state);

        return PhysicalPlanRewriter.RewriteChildren(
            node,
            child => Rewrite(child, state));
    }

    private static PhysicalNode SelectAggregateStrategy(
        PhysicalAggregateCandidateNode aggregate,
        PhysicalOptimizationState state)
    {
        var input = Rewrite(aggregate.Input, state);
        var strategy = PhysicalStrategyRules.ChooseAggregateStrategy(
            aggregate.GroupKeys.Length,
            aggregate.GroupKeyTypes);
        state.AddDecision(new PlanningDecision(
            PlanningDecisionCategory.AggregateStrategy,
            "AggregateStrategySelection",
            "aggregate",
            strategy.Kind.ToString(),
            strategy.Kind == AggregateStrategyKind.Unsupported ? PlanningConfidence.Low : PlanningConfidence.High,
            strategy.Reason));

        return strategy.Kind switch
        {
            AggregateStrategyKind.AggregateOnly => new PhysicalAggregateOnlyNode(
                aggregate.Bindings,
                input),
            AggregateStrategyKind.SingleKey => new PhysicalSingleKeyAggregateNode(
                aggregate.GroupKeys[0],
                aggregate.GroupKeyNames[0],
                aggregate.GroupKeyTypes[0],
                aggregate.Bindings,
                input),
            AggregateStrategyKind.ValueTuple => new PhysicalValueTupleAggregateNode(
                aggregate.GroupKeys,
                aggregate.GroupKeyNames,
                aggregate.GroupKeyTypes,
                aggregate.Bindings,
                input),
            _ => throw new NotSupportedException(strategy.Reason)
        };
    }
}

