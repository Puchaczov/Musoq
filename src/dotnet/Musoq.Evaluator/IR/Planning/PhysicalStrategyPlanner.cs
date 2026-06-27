using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Musoq.Evaluator.IR.Logical;

namespace Musoq.Evaluator.IR.Planning;

internal static class PhysicalStrategyPlanner
{
    public static PhysicalStrategyPlanningResult Plan(
        LogicalNode node,
        CompilationOptions compilationOptions,
        IReadOnlyDictionary<string, SourcePlanResult>? sourcePlanResults = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(compilationOptions);
        var state = new PhysicalStrategyPlanningState();
        state.Visit(node);

        return new PhysicalStrategyPlanningResult(state.CreatePlan(), state.Decisions);
    }

    private sealed class PhysicalStrategyPlanningState
    {
        private readonly List<PlanningDecision> _decisions = [];

        public IReadOnlyList<PlanningDecision> Decisions => _decisions;

        public void Visit(LogicalNode node)
        {
            foreach (var child in node.Children)
                Visit(child);
        }

        public PhysicalStrategyPlan CreatePlan()
        {
            return new PhysicalStrategyPlan();
        }
    }
}
