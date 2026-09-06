using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator.IR.Logical;

namespace Musoq.Evaluator.IR.Planning;

internal static class PhysicalStrategyPlanner
{
    public static PhysicalStrategyPlanningResult Plan(
        LogicalNode node,
        CompilationOptions compilationOptions,
        IReadOnlyDictionary<string, SourcePlanResult>? sourcePlanResults = null)
    {
        return Plan(node, compilationOptions, sourcePlanResults, CancellationToken.None);
    }

    public static PhysicalStrategyPlanningResult Plan(
        LogicalNode node,
        CompilationOptions compilationOptions,
        IReadOnlyDictionary<string, SourcePlanResult>? sourcePlanResults,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(compilationOptions);
        cancellationToken.ThrowIfCancellationRequested();
        var state = new PhysicalStrategyPlanningState(cancellationToken);
        state.Visit(node);
        cancellationToken.ThrowIfCancellationRequested();

        return new PhysicalStrategyPlanningResult(state.CreatePlan(), state.Decisions);
    }

    private sealed class PhysicalStrategyPlanningState(CancellationToken cancellationToken)
    {
        private readonly List<PlanningDecision> _decisions = [];
        private readonly CancellationToken _cancellationToken = cancellationToken;

        public IReadOnlyList<PlanningDecision> Decisions => _decisions;

        public void Visit(LogicalNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            foreach (var child in node.Children)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                Visit(child);
            }
        }

        public PhysicalStrategyPlan CreatePlan()
        {
            _cancellationToken.ThrowIfCancellationRequested();
            return new PhysicalStrategyPlan();
        }
    }
}
