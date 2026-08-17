using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record PlanningResult(LogicalPlanningArtifacts LogicalArtifacts, PhysicalPlanningArtifacts PhysicalArtifacts, ExecutionPlanningArtifacts ExecutionArtifacts, PlanningFacts Facts, IReadOnlyList<PlanningDecision> Decisions)
{
    public PlanProperties Properties => Facts.ToPlanProperties();
}
