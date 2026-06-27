using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Logical;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed record LogicalOptimizationResult(
    LogicalNode InitialPlan,
    LogicalNode OptimizedPlan,
    OptimizationTrace Trace);
