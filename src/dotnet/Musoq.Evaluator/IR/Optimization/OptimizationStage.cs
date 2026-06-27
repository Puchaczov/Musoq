using System.Collections.Generic;
using Musoq.Evaluator;

namespace Musoq.Evaluator.IR.Optimization;

internal enum OptimizationStage
{
    PreLogicalNormalization,
    LogicalNormalization,
    LogicalOptimization,
    PhysicalSelection,
    PhysicalOptimization,
    ExecutionIrOptimization,
    CodegenReadability
}
