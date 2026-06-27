using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.IR.Planning.Cardinality;
using PhysicalPlanBuilder = Musoq.Evaluator.IR.Physical.PhysicalPlanBuilder;
using PlanningContext = Musoq.Evaluator.IR.Planning.PlanningContext;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class PhysicalPlanningPipeline
{
    public PhysicalPlanningPipelineResult Plan(
        PlanningContext context,
        PlanProperties initialProperties,
        ExecutionShapeResolver shapeResolver)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(initialProperties);

        var decisions = new List<PlanningDecision>();
        var strategyResult = PhysicalStrategyPlanner.Plan(
            context.LogicalPlan,
            context.CompilationOptions,
            initialProperties.SourcePlanResultsBySourceId);
        decisions.AddRange(strategyResult.Decisions);

        var initialPhysicalPlan = BuildInitialPhysicalPlan(
            context,
            initialProperties,
            strategyResult.Strategies);
        var initialCardinalityFacts = CardinalityFactPlanner.Plan(initialPhysicalPlan, initialProperties);
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
                optimizationResult.OptimizedProperties,
                decisions,
                optimizationResult.Trace));
    }

    private static PhysicalNode BuildInitialPhysicalPlan(
        PlanningContext context,
        PlanProperties properties,
        PhysicalStrategyPlan strategyPlan)
    {
        var physicalBuilder = new PhysicalPlanBuilder(properties.PredicateMovementPlans,
            strategyPlan);

        return physicalBuilder.Lower(context.LogicalPlan);
    }
}
