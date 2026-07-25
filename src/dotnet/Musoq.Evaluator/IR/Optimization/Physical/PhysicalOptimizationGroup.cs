using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal static class PhysicalOptimizationGroup
{
    public static OptimizationPassPipeline<PhysicalNode> Pipeline { get; } = new(
        OptimizationStage.PhysicalOptimization,
        OptimizationPassRunMode.Once,
        [
            new(new SourcePredicateMetadataPass(), "Derive source predicate pushdown metadata."),
            new(new SourceProjectionMetadataPass(), "Derive conservative source projection metadata."),
            new(new ProjectionPruningPhysicalPass(), "Prune projections to columns the plan actually consumes."),
            new(new AggregateStrategySelectionPass(), "Select the aggregate execution strategy."),
            new(new PredicateMovementPhysicalPass(), "Place predicates at their cheapest safe position."),
            new(new JoinStrategySelectionPass(), "Select the join execution strategy."),
            new(new OrderingStrategySelectionPass(), "Select the ordering and top/offset strategy."),
            new(new WindowMaterializationPass(), "Select window materialization shape."),
            new(new SourcePredicatePhysicalRewritePass(), "Rewrite source nodes to honor pushed-down predicates."),
            new(new SourcePlanPhysicalRewritePass(), "Rewrite source nodes to honor selected source plans."),
            new(new RecursiveCteInvariantPlanningPass(), "Extract frontier-independent recursive inputs.")
        ]);

    public static IReadOnlyList<IPlanOptimizationPass<PhysicalNode>> Passes => Pipeline.Passes;
}

