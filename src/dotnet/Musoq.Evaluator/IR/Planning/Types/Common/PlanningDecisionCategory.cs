namespace Musoq.Evaluator.IR.Planning;

internal enum PlanningDecisionCategory
{
    PhysicalPlanning,
    PlanProperties,
    PredicatePushdown,
    ProjectionPruning,
    RequiredColumns,
    SourceInteraction,
    SourcePlanning,
    PredicatePlacement,
    PredicateMovement,
    CardinalityFacts,
    JoinStrategy,
    AggregateStrategy,
    OrderingStrategy,
    WindowStrategy,
    SetOperationStrategy,
    SubqueryStrategy,
    CteStrategy,
    SourceBoundaryStrategy,
    BoundaryRowShape,
    RowWidthPruning,
    ParallelEligibility,
    CteSidecarIndexStrategy,
    Materialization
}
