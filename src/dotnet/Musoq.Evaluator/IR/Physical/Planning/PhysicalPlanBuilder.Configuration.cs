using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Physical;

public sealed partial class PhysicalPlanBuilder
{
    private readonly Dictionary<JoinNode, PredicateMovementPlan[]> _predicateMovementsByJoin;
    private readonly Dictionary<ApplyNode, ApplyPredicateMovementPlan[]> _applyPredicateMovementsByApply;
    private readonly PhysicalStrategyPlan? _strategyPlan;
    private readonly IReadOnlyDictionary<string, SourceTransferStrategyPlan> _sourceTransferPlans;

    public PhysicalPlanBuilder()
        : this(null, null, null, null)
    { }

    internal PhysicalPlanBuilder(
        IReadOnlyList<PredicateMovementPlan>? predicateMovementPlans,
        PhysicalStrategyPlan? strategyPlan,
        IReadOnlyDictionary<string, SourceTransferStrategyPlan>? sourceTransferPlans = null,
        IReadOnlyList<ApplyPredicateMovementPlan>? applyPredicateMovementPlans = null)
    {
        _predicateMovementsByJoin = CreatePredicateMovementsByJoin(predicateMovementPlans);
        _applyPredicateMovementsByApply = CreateApplyPredicateMovementsByApply(applyPredicateMovementPlans);
        _strategyPlan = strategyPlan;
        _sourceTransferPlans = sourceTransferPlans ?? new Dictionary<string, SourceTransferStrategyPlan>(StringComparer.Ordinal);
    }
}
