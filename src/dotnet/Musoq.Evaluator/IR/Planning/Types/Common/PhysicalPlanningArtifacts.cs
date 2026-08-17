using System.Collections.Generic;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record PhysicalPlanningArtifacts(PhysicalNode InitialPhysicalPlan, PhysicalNode OptimizedPhysicalPlan, PlanningFacts OptimizedFacts, IReadOnlyList<PlanningDecision> Decisions, OptimizationTrace OptimizerTrace)
{
    public PlanProperties OptimizedProperties => OptimizedFacts.ToPlanProperties();
}
