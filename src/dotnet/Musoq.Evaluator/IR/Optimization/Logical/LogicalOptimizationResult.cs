using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed record LogicalOptimizationResult(
    LogicalNode InitialPlan,
    LogicalNode OptimizedPlan,
    OptimizationTrace Trace);

