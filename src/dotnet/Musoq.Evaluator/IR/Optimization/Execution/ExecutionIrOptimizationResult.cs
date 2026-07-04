using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal sealed record ExecutionIrOptimizationResult(
    ExecutionPlan InitialPlan,
    ExecutionPlan OptimizedPlan,
    OptimizationTrace Trace);

