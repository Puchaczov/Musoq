using System.Collections.Generic;
using Musoq.Evaluator;

namespace Musoq.Evaluator.IR.Optimization;

internal interface IPlanOptimizationPass<TPlan>
{
    string Name { get; }

    OptimizationResult<TPlan> Optimize(TPlan plan, OptimizationContext context);
}
