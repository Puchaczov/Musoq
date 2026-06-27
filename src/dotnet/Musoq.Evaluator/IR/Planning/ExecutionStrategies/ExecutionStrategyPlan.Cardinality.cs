using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning.Cardinality;

namespace Musoq.Evaluator.IR.Planning;

internal sealed partial class ExecutionStrategyPlan
{
    public ExecutionStrategyPlan WithCardinalityFacts(IReadOnlyList<CardinalityFact> facts)
    {
        return new ExecutionStrategyPlan(
            parallelAggregateCandidates,
            parallelFilterProjectCandidates,
            parallelCteLevels,
            cteStrategies,
            cteSidecarIndexPlans,
            setOperationStrategies,
            sourceBoundaryStrategies,
            rowWidthPruningPlans,
            facts);
    }

    public bool TryCreateCardinalityCapacityCandidate(
        PhysicalNode node,
        ExecutionVariable target,
        [NotNullWhen(true)] out ExecutionCapacityHint? capacityHint)
    {
        if (CardinalityFactAdvisor.TryResolveHighConfidenceCapacity(cardinalityFacts, node, out var capacity, out _))
        {
            capacityHint = ExecutionCapacityHintCandidates.CreateConstantCandidate(target, capacity);
            return true;
        }

        capacityHint = null;
        return false;
    }
}
