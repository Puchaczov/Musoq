using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record ExecutionPlanningArtifacts(
    ExecutionStrategyPlan ExecutionStrategies,
    IReadOnlyDictionary<string, SourceInteractionPlan> SourceInteractionPlansBySourceId,
    IReadOnlyList<PlanningDecision> Decisions,
    IReadOnlyDictionary<string, SourceTransferStrategyPlan>? SourceTransferPlansBySourceId = null)
{
    internal IPlanningShapeResolver? ShapeBindings { get; init; }
    internal string GeneratedTypePrefix { get; init; } = string.Empty;
}
