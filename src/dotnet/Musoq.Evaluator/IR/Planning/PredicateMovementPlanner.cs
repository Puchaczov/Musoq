using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PredicateMovementPlanner
{
    public static PredicateMovementPlanningResult Plan(
        LogicalNode logicalPlan,
        IReadOnlyDictionary<string, SourcePlanProperties> sources,
        IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlans,
        IReadOnlyDictionary<string, SourceInteractionPlan> sourceInteractionPlans)
    {
        ArgumentNullException.ThrowIfNull(logicalPlan);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(sourcePredicatePlans);
        ArgumentNullException.ThrowIfNull(sourceInteractionPlans);
        var state = new PredicateMovementPlanningState(logicalPlan, sources, sourcePredicatePlans, sourceInteractionPlans);
        state.Visit(logicalPlan);
        return new PredicateMovementPlanningResult(state.Plans, state.Decisions);
    }


}
