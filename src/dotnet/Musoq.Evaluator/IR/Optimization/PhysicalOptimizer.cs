using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.SourcePlanning;
using Musoq.Evaluator.IR.Planning;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class PhysicalOptimizer
{
    public PhysicalOptimizationResult Optimize(
        PhysicalNode initialPlan,
        PlanProperties initialProperties,
        CompilationOptions? compilationOptions = null,
        ExecutionShapeResolver? shapeResolver = null)
    {
        ArgumentNullException.ThrowIfNull(initialPlan);
        ArgumentNullException.ThrowIfNull(initialProperties);

        var trace = new OptimizationTrace();
        var state = new PhysicalOptimizationState(
            initialProperties,
            compilationOptions ?? new CompilationOptions(),
            shapeResolver);
        var contextState = new OptimizationContextState
        {
            PhysicalOptimizationState = state
        };
        var result = new PlanOptimizationRunner<PhysicalNode>(
            PhysicalOptimizationGroup.Pipeline).Run(
            initialPlan,
            new OptimizationContext(OptimizationStage.PhysicalOptimization, trace, OptimizationOptions.Default, contextState));

        return new PhysicalOptimizationResult(
            initialPlan,
            result.Plan,
            state.Properties,
            state.Decisions,
            trace);
    }
}
