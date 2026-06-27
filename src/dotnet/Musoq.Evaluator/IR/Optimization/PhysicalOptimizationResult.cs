using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.SourcePlanning;
using Musoq.Evaluator.IR.Planning;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed record PhysicalOptimizationResult(
    PhysicalNode InitialPlan,
    PhysicalNode OptimizedPlan,
    PlanProperties OptimizedProperties,
    IReadOnlyList<PlanningDecision> Decisions,
    OptimizationTrace Trace);
