using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourceBoundaryStrategyPlan(
    string BoundaryId,
    SourceBoundaryKind Kind,
    ApplyKind ApplyKind,
    SourceBoundaryInputMode InputMode,
    SourceBoundaryStrategyKind Strategy,
    SourceBoundaryCachingDecision CachingDecision,
    PlanningConfidence Confidence,
    string Reason);
