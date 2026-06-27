using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Planning.Cardinality;

internal static class CardinalityFactAdvisor
{
    public static bool TryResolveComparableRows(
        IReadOnlyList<CardinalityFact>? facts,
        string targetId,
        out long rows)
    {
        rows = 0;

        if (facts == null || facts.Count == 0)
            return false;

        foreach (var fact in facts)
        {
            if (!string.Equals(fact.TargetId, targetId, StringComparison.Ordinal))
                continue;

            return TryResolveComparableRows(fact, out rows);
        }

        return false;
    }

    public static bool TryResolveHighConfidenceCapacity(
        IReadOnlyList<CardinalityFact>? facts,
        PhysicalNode node,
        out int capacity,
        out CardinalityFact? matchedFact)
    {
        capacity = 0;
        matchedFact = null;

        if (facts == null || facts.Count == 0)
            return false;

        foreach (var fact in facts)
        {
            if (!ReferenceEquals(fact.Node, node))
                continue;

            if (!TryResolveHighConfidenceUpperBound(fact, out var upperBound))
                continue;

            if (upperBound > int.MaxValue)
                continue;

            capacity = (int)upperBound;
            matchedFact = fact;
            return true;
        }

        return false;
    }

    private static bool TryResolveComparableRows(CardinalityFact fact, out long rows)
    {
        rows = 0;

        switch (fact.Kind)
        {
            case CardinalityKind.Exact when fact.ExactRows is >= 0:
                rows = fact.ExactRows.Value;
                return true;
            case CardinalityKind.Bounded when fact is { Confidence: >= 0.8d, UpperBound: >= 0 }:
                rows = fact.UpperBound.Value;
                return true;
            default:
                return false;
        }
    }

    private static bool TryResolveHighConfidenceUpperBound(CardinalityFact fact, out long upperBound)
    {
        upperBound = 0;

        switch (fact.Kind)
        {
            case CardinalityKind.Exact when fact.ExactRows is >= 0:
                upperBound = fact.ExactRows.Value;
                return true;
            case CardinalityKind.Bounded when fact is { Confidence: >= 0.8d, UpperBound: >= 0 }:
                upperBound = fact.UpperBound.Value;
                return true;
            default:
                return false;
        }
    }
}
