using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Planning.Cardinality;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourcePlanningFacts(
    IReadOnlyDictionary<string, SourcePlanProperties> SourcesById,
    IReadOnlyDictionary<string, IrExpression[]> PushedPredicatesBySourceId,
    IReadOnlyDictionary<string, string[]> ProjectedColumnsBySourceId,
    IReadOnlyDictionary<string, ISchemaColumn[]> ProjectedSchemaColumnsBySourceId,
    IReadOnlyDictionary<string, SourcePredicatePlan> SourcePredicatePlansBySourceId,
    IReadOnlyDictionary<string, SourceInteractionPlan> SourceInteractionPlansBySourceId,
    IReadOnlyDictionary<string, SourcePlanRequest> SourcePlanRequestsBySourceId,
    IReadOnlyDictionary<string, SourcePlanResult> SourcePlanResultsBySourceId,
    IReadOnlyList<SourceBoundaryPlan> SourceBoundaryPlans,
    IReadOnlyList<SourceBoundaryStrategyPlan> SourceBoundaryStrategyPlans,
    IReadOnlyDictionary<string, SourceContractDiagnosticLocationMap> SourceContractDiagnosticLocationsBySourceId);

internal sealed record RequiredColumnFacts(
    IReadOnlyDictionary<string, IReadOnlySet<string>> RequiredColumnsByAlias,
    IReadOnlyDictionary<string, RequiredColumnUsage[]> RequiredColumnUsagesBySourceId,
    IReadOnlyList<RequiredColumnMappingPlan> RequiredColumnMappingPlans,
    IReadOnlyList<RequiredColumnBoundaryPlan> RequiredColumnBoundaryPlans);

internal sealed record PhysicalStrategyFacts(
    IReadOnlyList<PredicatePlacementPlan> PredicatePlacementPlans,
    IReadOnlyList<PredicateMovementPlan> PredicateMovementPlans);

internal sealed record BoundaryPruningFacts(
    IReadOnlyList<BoundaryRowShapePlan> BoundaryRowShapePlans,
    IReadOnlyList<RowWidthPruningPlan> RowWidthPruningPlans);

internal sealed record CardinalityPlanningFacts(IReadOnlyList<CardinalityFact> Facts);

internal sealed record PlanningFacts(
    SourcePlanningFacts SourcePlanning,
    RequiredColumnFacts RequiredColumns,
    PhysicalStrategyFacts PhysicalStrategies,
    BoundaryPruningFacts BoundaryPruning,
    CardinalityPlanningFacts Cardinality)
{
    public static PlanningFacts From(PlanProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        return properties.ToFacts();
    }

    public PlanProperties ToPlanProperties() => PlanProperties.FromFacts(this);
}

internal sealed partial record PlanProperties
{
    public PlanProperties(PlanningFacts facts)
        : this(
            facts.SourcePlanning.SourcesById,
            facts.SourcePlanning.PushedPredicatesBySourceId,
            facts.SourcePlanning.ProjectedColumnsBySourceId,
            facts.SourcePlanning.ProjectedSchemaColumnsBySourceId,
            facts.RequiredColumns.RequiredColumnsByAlias,
            facts.RequiredColumns.RequiredColumnUsagesBySourceId,
            facts.RequiredColumns.RequiredColumnMappingPlans,
            facts.RequiredColumns.RequiredColumnBoundaryPlans,
            facts.SourcePlanning.SourcePredicatePlansBySourceId,
            facts.SourcePlanning.SourceInteractionPlansBySourceId,
            facts.SourcePlanning.SourcePlanRequestsBySourceId,
            facts.SourcePlanning.SourcePlanResultsBySourceId,
            facts.SourcePlanning.SourceBoundaryPlans,
            facts.SourcePlanning.SourceBoundaryStrategyPlans,
            facts.BoundaryPruning.BoundaryRowShapePlans,
            facts.BoundaryPruning.RowWidthPruningPlans,
            facts.Cardinality.Facts,
            facts.PhysicalStrategies.PredicatePlacementPlans,
            facts.PhysicalStrategies.PredicateMovementPlans)
    {
        SourceContractDiagnosticLocationsBySourceId = facts.SourcePlanning.SourceContractDiagnosticLocationsBySourceId;
    }

    public static PlanProperties FromFacts(PlanningFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return new(facts);
    }

    public PlanningFacts ToFacts() => new(
        SourcePlanning,
        RequiredColumns,
        PhysicalStrategies,
        BoundaryPruning,
        Cardinality);

    public SourcePlanningFacts SourcePlanning => new(
        SourcesById,
        PushedPredicatesBySourceId,
        ProjectedColumnsBySourceId,
        ProjectedSchemaColumnsBySourceId,
        SourcePredicatePlansBySourceId,
        SourceInteractionPlansBySourceId,
        SourcePlanRequestsBySourceId,
        SourcePlanResultsBySourceId,
        SourceBoundaryPlans,
        SourceBoundaryStrategyPlans,
        SourceContractDiagnosticLocationsBySourceId);

    public RequiredColumnFacts RequiredColumns => new(
        RequiredColumnsByAlias,
        RequiredColumnUsagesBySourceId,
        RequiredColumnMappingPlans,
        RequiredColumnBoundaryPlans);

    public PhysicalStrategyFacts PhysicalStrategies => new(
        PredicatePlacementPlans,
        PredicateMovementPlans);

    public BoundaryPruningFacts BoundaryPruning => new(
        BoundaryRowShapePlans,
        RowWidthPruningPlans);

    public CardinalityPlanningFacts Cardinality => new(CardinalityFacts);
}
