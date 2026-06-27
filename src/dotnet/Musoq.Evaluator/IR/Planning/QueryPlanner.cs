using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;
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

        var shapeResolver = new ExecutionShapeResolver(
            context.Scope,
            context.InferredColumns,
            schemaRegistry: context.SchemaRegistry);
        var physicalPlanningResult = new PhysicalPlanningPipeline().Plan(
            context,
            propertyResult.Properties,
            shapeResolver);
        var physicalPlanningArtifacts = physicalPlanningResult.Artifacts;
        decisions.AddRange(physicalPlanningArtifacts.Decisions);
        var physicalPlan = physicalPlanningArtifacts.OptimizedPhysicalPlan;
        var sourceRewrittenProperties = physicalPlanningArtifacts.OptimizedProperties;
        decisions.AddRange(SubqueryLoweringStrategyPlanner.Plan(physicalPlan).Decisions);
        var rowShapePlanningResult = BoundaryRowShapePlanner.Plan(physicalPlan, sourceRewrittenProperties);
        var requiredColumnBoundaryResult = RequiredColumnBoundaryPlanner.Plan(physicalPlan, rowShapePlanningResult.Plans);
        var rowWidthPruningResult = RowWidthPruningPlanner.Plan(rowShapePlanningResult.Plans);
        var cardinalityFactResult = CardinalityFactPlanner.Plan(physicalPlan, sourceRewrittenProperties);
        var planProperties = sourceRewrittenProperties with
        {
            RequiredColumnBoundaryPlans = requiredColumnBoundaryResult.Plans,
            BoundaryRowShapePlans = rowShapePlanningResult.Plans,
            RowWidthPruningPlans = rowWidthPruningResult.Plans,
            CardinalityFacts = cardinalityFactResult.Facts
        };
        decisions.AddRange(requiredColumnBoundaryResult.Decisions);
        decisions.AddRange(rowShapePlanningResult.Decisions);
        decisions.AddRange(rowWidthPruningResult.Decisions);
        decisions.AddRange(cardinalityFactResult.Decisions);

        var executionStrategyResult = ExecutionStrategyPlanner.Plan(
            physicalPlan,
            context.CompilationOptions,
            context.CteExecutionPlan,
            shapeResolver);
        var executionStrategies = executionStrategyResult.Strategies
            .WithSourceBoundaryStrategies(planProperties.SourceBoundaryStrategyPlans)
            .WithRowWidthPruningPlans(rowWidthPruningResult.Plans)
            .WithCardinalityFacts(cardinalityFactResult.Facts);
        var executionPlanningArtifacts = new ExecutionPlanningArtifacts(executionStrategies, planProperties.SourceInteractionPlansBySourceId, executionStrategyResult.Decisions);
        decisions.AddRange(executionPlanningArtifacts.Decisions);
        decisions.AddRange(MaterializationPlanner.Plan(physicalPlan, executionPlanningArtifacts.ExecutionStrategies));

        return new PlanningResult(
            context.LogicalArtifacts,
            physicalPlanningArtifacts,
            executionPlanningArtifacts,
            planProperties,
            decisions);
    }
}
