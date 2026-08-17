using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal sealed record ExecutionIrOptimizationResult(
    ExecutionPlan InitialPlan,
    ExecutionPlan OptimizedPlan,
    OptimizationTrace Trace);

