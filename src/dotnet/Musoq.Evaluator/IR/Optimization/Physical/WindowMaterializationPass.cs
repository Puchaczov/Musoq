using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed class WindowMaterializationPass : IPhysicalOptimizationPass
{
    public string Name => "WindowMaterialization";

    public OptimizationResult<PhysicalNode> Optimize(PhysicalNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var state = PhysicalOptimizationState.From(context);
        var rewritten = Rewrite(plan, state);

        return ReferenceEquals(plan, rewritten)
            ? OptimizationResult<PhysicalNode>.NoChange(plan, "No window inputs required materialization changes.")
            : OptimizationResult<PhysicalNode>.Changed(rewritten, "Inserted required window materialization boundaries.");
    }

    private static PhysicalNode Rewrite(PhysicalNode node, PhysicalOptimizationState state)
    {
        if (node is PhysicalWindowNode window)
            return RewriteWindow(window, state);

        return PhysicalPlanRewriter.RewriteChildren(
            node,
            child => Rewrite(child, state));
    }

    private static PhysicalNode RewriteWindow(PhysicalWindowNode window, PhysicalOptimizationState state)
    {
        var input = Rewrite(window.Input, state);
        var registrations = WindowExecutionStrategy.Select(window.Registrations);
        state.AddDecision(new PlanningDecision(
            PlanningDecisionCategory.WindowStrategy,
            "WindowMaterialization",
            "window",
            "MaterializeInput",
            PlanningConfidence.High,
            "Window computation requires a materialized input boundary."));

        if (ReferenceEquals(registrations, window.Registrations) && ReferenceEquals(input, window.Input) &&
            input is PhysicalMaterializeNode)
            return window;
        return new PhysicalWindowNode(registrations,
            input is PhysicalMaterializeNode ? input : new PhysicalMaterializeNode(input));
    }
}
