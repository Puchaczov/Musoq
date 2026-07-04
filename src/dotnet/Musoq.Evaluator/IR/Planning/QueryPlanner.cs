using System.Collections.Generic;
using Musoq.Evaluator.IR.Planning.Cardinality;
using Musoq.Evaluator.IR.Planning.Subqueries;

namespace Musoq.Evaluator.IR.Planning;

internal sealed class QueryPlanner
{
    public PlanningResult Plan(PlanningContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var propertyResult = PlanningPropertyDeriver.Derive(context);
        var decisions = new List<PlanningDecision>(propertyResult.Decisions)
        {
            new(
                PlanningDecisionCategory.PhysicalPlanning,
                "PlannerBoundary",
                "logical-plan",
                "Planned",
                PlanningConfidence.High,
                "Physical planning is routed through QueryPlanner.")
        };

        var physicalPlanningResult = new PhysicalPlanningPipeline().Plan(
            context,
            propertyResult.Facts,
            context.ShapeResolver);
        var physicalPlanningArtifacts = physicalPlanningResult.Artifacts;
        decisions.AddRange(physicalPlanningArtifacts.Decisions);
        var physicalPlan = physicalPlanningArtifacts.OptimizedPhysicalPlan;
        var sourceRewrittenFacts = physicalPlanningArtifacts.OptimizedFacts;
        decisions.AddRange(SubqueryLoweringStrategyPlanner.Plan(physicalPlan).Decisions);
        var rowShapePlanningResult = BoundaryRowShapePlanner.Plan(physicalPlan, sourceRewrittenFacts.RequiredColumns);
        var requiredColumnBoundaryResult = RequiredColumnBoundaryPlanner.Plan(physicalPlan, rowShapePlanningResult.Plans);
        var rowWidthPruningResult = RowWidthPruningPlanner.Plan(rowShapePlanningResult.Plans);
        var cardinalityFactResult = CardinalityFactPlanner.Plan(physicalPlan, sourceRewrittenFacts.SourcePlanning);
        var planFacts = sourceRewrittenFacts with
        {
            RequiredColumns = sourceRewrittenFacts.RequiredColumns with
            {
                RequiredColumnBoundaryPlans = requiredColumnBoundaryResult.Plans
            },
            BoundaryPruning = new BoundaryPruningFacts(
                rowShapePlanningResult.Plans,
                rowWidthPruningResult.Plans),
            Cardinality = new CardinalityPlanningFacts(cardinalityFactResult.Facts)
        };
        decisions.AddRange(requiredColumnBoundaryResult.Decisions);
        decisions.AddRange(rowShapePlanningResult.Decisions);
        decisions.AddRange(rowWidthPruningResult.Decisions);
        decisions.AddRange(cardinalityFactResult.Decisions);

        var executionStrategyResult = ExecutionStrategyPlanner.Plan(
            physicalPlan,
            context.CompilationOptions,
            context.CteExecutionPlan,
            context.ShapeResolver);
        var executionStrategies = executionStrategyResult.Strategies
            .WithSourceBoundaryStrategies(planFacts.SourcePlanning.SourceBoundaryStrategyPlans)
            .WithRowWidthPruningPlans(rowWidthPruningResult.Plans)
            .WithCardinalityFacts(cardinalityFactResult.Facts);
        var executionPlanningArtifacts = new ExecutionPlanningArtifacts(executionStrategies, planFacts.SourcePlanning.SourceInteractionPlansBySourceId, executionStrategyResult.Decisions);
        decisions.AddRange(executionPlanningArtifacts.Decisions);
        decisions.AddRange(MaterializationPlanner.Plan(physicalPlan, executionPlanningArtifacts.ExecutionStrategies));

        return new PlanningResult(
            context.LogicalArtifacts,
            physicalPlanningArtifacts,
            executionPlanningArtifacts,
            planFacts,
            decisions);
    }
}
