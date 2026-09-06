using System.Collections.Generic;
using Musoq.Evaluator.IR.Optimization.Physical;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning.Cardinality;
using PhysicalPlanBuilder = Musoq.Evaluator.IR.Physical.PhysicalPlanBuilder;

namespace Musoq.Evaluator.IR.Planning;

internal sealed class PhysicalPlanningPipeline
{
    public PhysicalPlanningPipelineResult Plan(
        PlanningContext context,
        PlanningFacts initialFacts,
        IPlanningShapeResolver shapeResolver,
        IReadOnlyDictionary<string, SourceTransferStrategyPlan>? sourceTransferPlans = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(initialFacts);
        context.CancellationToken.ThrowIfCancellationRequested();

        var initialProperties = initialFacts.ToPlanProperties();
        var decisions = new List<PlanningDecision>();
        context.CancellationToken.ThrowIfCancellationRequested();
        var strategyResult = PhysicalStrategyPlanner.Plan(
            context.LogicalPlan,
            context.CompilationOptions,
            initialProperties.SourcePlanning.SourcePlanResultsBySourceId,
            context.CancellationToken);
        context.CancellationToken.ThrowIfCancellationRequested();
        decisions.AddRange(strategyResult.Decisions);

        var initialPhysicalPlan = BuildInitialPhysicalPlan(
            context,
            initialFacts.PhysicalStrategies,
            strategyResult.Strategies,
            sourceTransferPlans);
        context.CancellationToken.ThrowIfCancellationRequested();
        var initialCardinalityFacts = CardinalityFactPlanner.Plan(
            initialPhysicalPlan,
            initialFacts.SourcePlanning,
            context.CancellationToken);
        context.CancellationToken.ThrowIfCancellationRequested();
        var propertiesWithCardinalityFacts = initialProperties with
        {
            CardinalityFacts = initialCardinalityFacts.Facts
        };
        context.CancellationToken.ThrowIfCancellationRequested();
        var optimizationResult = new PhysicalOptimizer().Optimize(
            initialPhysicalPlan,
            propertiesWithCardinalityFacts,
            context.CompilationOptions,
            shapeResolver);
        context.CancellationToken.ThrowIfCancellationRequested();
        decisions.AddRange(optimizationResult.Decisions);

        context.CancellationToken.ThrowIfCancellationRequested();
        return new PhysicalPlanningPipelineResult(
            new PhysicalPlanningArtifacts(
                optimizationResult.InitialPlan,
                optimizationResult.OptimizedPlan,
                optimizationResult.OptimizedProperties.ToFacts(),
                decisions,
                optimizationResult.Trace));
    }

    private static PhysicalNode BuildInitialPhysicalPlan(
        PlanningContext context,
        PhysicalStrategyFacts physicalStrategies,
        PhysicalStrategyPlan strategyPlan,
        IReadOnlyDictionary<string, SourceTransferStrategyPlan>? sourceTransferPlans)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        var physicalBuilder = new PhysicalPlanBuilder(
            physicalStrategies.PredicateMovementPlans,
            strategyPlan,
            sourceTransferPlans,
            physicalStrategies.ApplyPredicateMovementPlans);

        var result = physicalBuilder.Lower(context.LogicalPlan);
        context.CancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}
