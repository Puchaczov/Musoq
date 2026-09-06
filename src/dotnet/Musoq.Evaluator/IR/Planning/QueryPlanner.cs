using System.Collections.Generic;
using Musoq.Evaluator.IR.Planning.Cardinality;
using Musoq.Evaluator.IR.Planning.Subqueries;

namespace Musoq.Evaluator.IR.Planning;

internal sealed class QueryPlanner
{
    public PlanningResult Plan(PlanningContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CancellationToken.ThrowIfCancellationRequested();
        var propertyResult = PlanningPropertyDeriver.Derive(context);
        context.CancellationToken.ThrowIfCancellationRequested();
        var sourceTransferResult = SourceTransferPlanner.Plan(context, propertyResult.Facts.SourcePlanning);
        context.CancellationToken.ThrowIfCancellationRequested();
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
        decisions.AddRange(sourceTransferResult.Decisions);

        var physicalPlanningResult = new PhysicalPlanningPipeline().Plan(
            context,
            propertyResult.Facts,
            context.ShapeResolver,
            sourceTransferResult.PlansBySourceId);
        context.CancellationToken.ThrowIfCancellationRequested();
        var physicalPlanningArtifacts = physicalPlanningResult.Artifacts;
        decisions.AddRange(physicalPlanningArtifacts.Decisions);
        var physicalPlan = physicalPlanningArtifacts.OptimizedPhysicalPlan;
        var sourceRewrittenFacts = physicalPlanningArtifacts.OptimizedFacts;
        context.CancellationToken.ThrowIfCancellationRequested();
        decisions.AddRange(SubqueryLoweringStrategyPlanner.Plan(physicalPlan).Decisions);
        context.CancellationToken.ThrowIfCancellationRequested();
        var rowShapePlanningResult = BoundaryRowShapePlanner.Plan(physicalPlan, sourceRewrittenFacts.RequiredColumns);
        context.CancellationToken.ThrowIfCancellationRequested();
        var requiredColumnBoundaryResult = RequiredColumnBoundaryPlanner.Plan(physicalPlan, rowShapePlanningResult.Plans);
        context.CancellationToken.ThrowIfCancellationRequested();
        var rowWidthPruningResult = RowWidthPruningPlanner.Plan(rowShapePlanningResult.Plans);
        context.CancellationToken.ThrowIfCancellationRequested();
        var cardinalityFactResult = CardinalityFactPlanner.Plan(
            physicalPlan,
            sourceRewrittenFacts.SourcePlanning,
            context.CancellationToken);
        context.CancellationToken.ThrowIfCancellationRequested();
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
        context.CancellationToken.ThrowIfCancellationRequested();
        var executionStrategies = executionStrategyResult.Strategies
            .WithSourceBoundaryStrategies(planFacts.SourcePlanning.SourceBoundaryStrategyPlans)
            .WithRowWidthPruningPlans(rowWidthPruningResult.Plans)
            .WithCardinalityFacts(cardinalityFactResult.Facts);
        var executionPlanningArtifacts = new ExecutionPlanningArtifacts(
            executionStrategies,
            planFacts.SourcePlanning.SourceInteractionPlansBySourceId,
            executionStrategyResult.Decisions,
            sourceTransferResult.PlansBySourceId);
        decisions.AddRange(executionPlanningArtifacts.Decisions);
        decisions.AddRange(MaterializationPlanner.Plan(physicalPlan, executionPlanningArtifacts.ExecutionStrategies));
        context.CancellationToken.ThrowIfCancellationRequested();

        return new PlanningResult(
            context.LogicalArtifacts,
            physicalPlanningArtifacts,
            executionPlanningArtifacts,
            planFacts,
            decisions);
    }
}
