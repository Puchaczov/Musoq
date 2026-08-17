using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Planning.Cardinality;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal sealed partial record PlanProperties(
    IReadOnlyDictionary<string, SourcePlanProperties> SourcesById,
    IReadOnlyDictionary<string, IrExpression[]> PushedPredicatesBySourceId,
    IReadOnlyDictionary<string, string[]> ProjectedColumnsBySourceId,
    IReadOnlyDictionary<string, ISchemaColumn[]> ProjectedSchemaColumnsBySourceId,
    IReadOnlyDictionary<string, IReadOnlySet<string>> RequiredColumnsByAlias,
    IReadOnlyDictionary<string, RequiredColumnUsage[]> RequiredColumnUsagesBySourceId,
    IReadOnlyList<RequiredColumnMappingPlan> RequiredColumnMappingPlans,
    IReadOnlyList<RequiredColumnBoundaryPlan> RequiredColumnBoundaryPlans,
    IReadOnlyDictionary<string, SourcePredicatePlan> SourcePredicatePlansBySourceId,
    IReadOnlyDictionary<string, SourceInteractionPlan> SourceInteractionPlansBySourceId,
    IReadOnlyDictionary<string, SourcePlanRequest> SourcePlanRequestsBySourceId,
    IReadOnlyDictionary<string, SourcePlanResult> SourcePlanResultsBySourceId,
    IReadOnlyList<SourceBoundaryPlan> SourceBoundaryPlans,
    IReadOnlyList<SourceBoundaryStrategyPlan> SourceBoundaryStrategyPlans,
    IReadOnlyList<BoundaryRowShapePlan> BoundaryRowShapePlans,
    IReadOnlyList<RowWidthPruningPlan> RowWidthPruningPlans,
    IReadOnlyList<CardinalityFact> CardinalityFacts,
    IReadOnlyList<PredicatePlacementPlan> PredicatePlacementPlans,
    IReadOnlyList<PredicateMovementPlan> PredicateMovementPlans);
