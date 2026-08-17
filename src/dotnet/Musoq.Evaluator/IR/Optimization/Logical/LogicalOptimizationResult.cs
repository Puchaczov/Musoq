using Musoq.Evaluator.IR.Logical;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed record LogicalOptimizationResult(
    LogicalNode InitialPlan,
    LogicalNode OptimizedPlan,
    OptimizationTrace Trace);

