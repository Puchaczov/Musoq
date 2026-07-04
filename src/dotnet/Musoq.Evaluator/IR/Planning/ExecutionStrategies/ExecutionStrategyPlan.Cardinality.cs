using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning.Cardinality;

namespace Musoq.Evaluator.IR.Planning;

internal sealed partial class ExecutionStrategyPlan
{
    public ExecutionStrategyPlan WithCardinalityFacts(IReadOnlyList<CardinalityFact> facts)
    {
        return new ExecutionStrategyPlan(
            identityMap,
            parallelAggregateCandidateIds,
            parallelFilterProjectCandidateIds,
            parallelCteLevels,
            cteStrategies,
            cteSidecarIndexPlans,
            cteSidecarConsumersByJoinId,
            setOperationStrategies,
            sourceBoundaryStrategies,
            rowWidthPruningPlans,
            facts);
    }

    public bool TryResolveCardinalityCapacity(
        PhysicalNode node,
        out int capacity)
    {
        if (CardinalityFactAdvisor.TryResolveHighConfidenceCapacity(cardinalityFacts, node, out var resolvedCapacity, out _))
        {
            capacity = resolvedCapacity;
            return true;
        }

        capacity = 0;
        return false;
    }
}
