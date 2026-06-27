using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class ExecutionIrOptimizer
{
    public ExecutionIrOptimizationResult Optimize(ExecutionPlan initialPlan, CompilationOptions? compilationOptions = null)
    {
        ArgumentNullException.ThrowIfNull(initialPlan);

        var trace = new OptimizationTrace();
        var effectiveOptions = compilationOptions ?? new CompilationOptions();
        var options = new OptimizationOptions
        {
            FieldReadDiscoveryEnabled = false,
            ExpressionCseEnabled = effectiveOptions.UseCommonSubexpressionElimination,
            CrossNodeExpressionCseEnabled = effectiveOptions.UseCommonSubexpressionElimination
        };
        var result = new PlanOptimizationRunner<ExecutionPlan>(
            ExecutionIrOptimizationGroup.Pipeline).Run(
            initialPlan,
            new OptimizationContext(OptimizationStage.ExecutionIrOptimization, trace, options, OptimizationContextState.Empty));

        return new ExecutionIrOptimizationResult(initialPlan, result.Plan, trace);
    }
}
