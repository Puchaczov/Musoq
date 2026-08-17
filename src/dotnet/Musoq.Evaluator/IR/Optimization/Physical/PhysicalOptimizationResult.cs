using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed record PhysicalOptimizationResult(
    PhysicalNode InitialPlan,
    PhysicalNode OptimizedPlan,
    PlanProperties OptimizedProperties,
    IReadOnlyList<PlanningDecision> Decisions,
    OptimizationTrace Trace);

