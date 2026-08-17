using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourceBoundaryPlan(
    string BoundaryId,
    SourceBoundaryKind Kind,
    ApplyKind ApplyKind,
    SourceBoundaryInputMode InputMode,
    SourceInvocationShape InvocationShape,
    SourceRowBehavior RowBehavior,
    SourceResultShape ResultShape,
    SourceCacheability Cacheability,
    PlanningConfidence CacheabilityConfidence,
    string Target,
    string[] InputAliases,
    string[] OutputAliases,
    PlanningConfidence Confidence,
    string Reason);
