using Musoq.Evaluator.IR.Physical;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed class PhysicalOptimizer
{
    public PhysicalOptimizationResult Optimize(
        PhysicalNode initialPlan,
        PlanProperties initialProperties,
        CompilationOptions compilationOptions,
        IPlanningShapeResolver shapeResolver)
    {
        return Optimize(
            initialPlan,
            new PhysicalOptimizationSession(initialProperties, compilationOptions, shapeResolver));
    }

    public PhysicalOptimizationResult Optimize(
        PhysicalNode initialPlan,
        PhysicalOptimizationSession session)
    {
        ArgumentNullException.ThrowIfNull(initialPlan);
        ArgumentNullException.ThrowIfNull(session);

        var trace = new OptimizationTrace();
        var state = new PhysicalOptimizationState(session);
        var contextState = new OptimizationContextState
        {
            PhysicalOptimizationState = state
        };
        var options = new OptimizationOptions
        {
            StabilityAwareScalarReuseEnabled = session.CompilationOptions.UseStabilityAwareScalarReuse
        };
        var result = new PlanOptimizationRunner<PhysicalNode>(
            PhysicalOptimizationGroup.Pipeline).Run(
            initialPlan,
            new OptimizationContext(OptimizationStage.PhysicalOptimization, trace, options, contextState));

        return new PhysicalOptimizationResult(
            initialPlan,
            result.Plan,
            state.OptimizedProperties,
            state.Decisions,
            trace);
    }
}
