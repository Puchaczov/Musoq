using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.SourcePlanning;
using Musoq.Evaluator.IR.Planning;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed record PhysicalOptimizationResult(
    PhysicalNode InitialPlan,
    PhysicalNode OptimizedPlan,
    PlanProperties OptimizedProperties,
    IReadOnlyList<PlanningDecision> Decisions,
    OptimizationTrace Trace);

