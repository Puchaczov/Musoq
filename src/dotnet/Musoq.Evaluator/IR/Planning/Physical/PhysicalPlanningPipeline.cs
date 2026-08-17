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
        IPlanningShapeResolver shapeResolver)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(initialFacts);

        var initialProperties = initialFacts.ToPlanProperties();
        var decisions = new List<PlanningDecision>();
        var strategyResult = PhysicalStrategyPlanner.Plan(
            context.LogicalPlan,
            context.CompilationOptions,
            initialProperties.SourcePlanning.SourcePlanResultsBySourceId);
        decisions.AddRange(strategyResult.Decisions);

        var initialPhysicalPlan = BuildInitialPhysicalPlan(
            context,
            initialFacts.PhysicalStrategies,
            strategyResult.Strategies);
        var initialCardinalityFacts = CardinalityFactPlanner.Plan(initialPhysicalPlan, initialFacts.SourcePlanning);
        var propertiesWithCardinalityFacts = initialProperties with
        {
            CardinalityFacts = initialCardinalityFacts.Facts
        };
        var optimizationResult = new PhysicalOptimizer().Optimize(
            initialPhysicalPlan,
            propertiesWithCardinalityFacts,
            context.CompilationOptions,
            shapeResolver);
        decisions.AddRange(optimizationResult.Decisions);

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
        PhysicalStrategyPlan strategyPlan)
    {
        var physicalBuilder = new PhysicalPlanBuilder(physicalStrategies.PredicateMovementPlans,
            strategyPlan);

        return physicalBuilder.Lower(context.LogicalPlan);
    }
}
