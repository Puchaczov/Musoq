using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning.Cardinality;

internal static class CardinalityFactPlanner
{
    public static CardinalityFactPlanningResult Plan(PhysicalNode physicalPlan, SourcePlanningFacts sourcePlanning)
    {
        ArgumentNullException.ThrowIfNull(physicalPlan);
        ArgumentNullException.ThrowIfNull(sourcePlanning);

        var state = new State(sourcePlanning);
        state.Visit(physicalPlan);

        return new CardinalityFactPlanningResult(state.Facts, state.Decisions);
    }

    private sealed class State(SourcePlanningFacts sourcePlanning)
    {
        private readonly List<CardinalityFact> _facts = [];
        private readonly List<PlanningDecision> _decisions = [];
        private int _takeIndex;
        private int _topNIndex;
        private int _topOffsetIndex;

        public IReadOnlyList<CardinalityFact> Facts => _facts;

        public IReadOnlyList<PlanningDecision> Decisions => _decisions;

        public CardinalityBounds? Visit(PhysicalNode node)
        {
            switch (node)
            {
                case PhysicalValuesScanNode values:
                    return AddFact(new CardinalityFact(
                        $"values:{values.Alias}",
                        "Values",
                        CardinalityKind.Exact,
                        values.Rows.Count,
                        values.Rows.Count,
                        values.Rows.Count,
                        1d,
                        $"VALUES source has {values.Rows.Count} row(s)."),
                        values);

                case PhysicalSchemaScanNode scan:
                    return TryCreateSourceFact(scan, out var sourceFact)
                        ? AddFact(sourceFact, scan)
                        : null;

                case PhysicalTakeNode take:
                    return VisitTakeLike(
                        targetId: $"take:{_takeIndex++}",
                        targetKind: "Take",
                        limit: take.Count,
                        input: take.Input,
                        targetNode: take,
                        reason: $"TAKE limits output to at most {take.Count} row(s).");

                case PhysicalTopNNode topN:
                    return VisitTakeLike(
                        targetId: $"top-n:{_topNIndex++}",
                        targetKind: "TopN",
                        limit: topN.N,
                        input: topN.Input,
                        targetNode: topN,
                        reason: $"Top-N limits output to at most {topN.N} row(s).");

                case PhysicalTopOffsetNode topOffset:
                    return VisitTopOffset(topOffset);

                case PhysicalSkipNode skip:
                    return ApplySkip(Visit(skip.Input), skip.Count);

                case PhysicalProjectNode project:
                    return project.IsDistinct
                        ? ApplyReducingBoundary(Visit(project.Input), "DISTINCT projection may remove rows.")
                        : Visit(project.Input);

                case PhysicalSortNode sort:
                    return Visit(sort.Input);

                case PhysicalMaterializeNode materialize:
                    return Visit(materialize.Input);

                case PhysicalFilterNode filter:
                    return ApplyReducingBoundary(Visit(filter.Input), "Filter may remove rows.");

                case PhysicalHavingFilterNode having:
                    return ApplyReducingBoundary(Visit(having.Input), "HAVING may remove rows.");

                case PhysicalQualifyFilterNode qualify:
                    return ApplyReducingBoundary(Visit(qualify.Input), "QUALIFY may remove rows.");

                default:
                    foreach (var child in node.Children)
                        Visit(child);

                    return null;
            }
        }

        private CardinalityBounds VisitTakeLike(
            string targetId,
            string targetKind,
            long limit,
            PhysicalNode input,
            PhysicalNode targetNode,
            string reason)
        {
            var inputBounds = Visit(input);
            var fact = inputBounds?.ExactRows is { } exact
                ? new CardinalityFact(
                    targetId,
                    targetKind,
                    CardinalityKind.Exact,
                    Math.Min(exact, limit),
                    Math.Min(exact, limit),
                    Math.Min(exact, limit),
                    inputBounds.Confidence,
                    reason)
                : new CardinalityFact(
                    targetId,
                    targetKind,
                    CardinalityKind.Bounded,
                    null,
                    0,
                    inputBounds?.UpperBound is { } upper ? Math.Min(upper, limit) : limit,
                    inputBounds?.Confidence ?? 1d,
                    reason);

            return AddFact(fact, targetNode);
        }

        private CardinalityBounds VisitTopOffset(PhysicalTopOffsetNode topOffset)
        {
            var inputBounds = Visit(topOffset.Input);
            var exactRows = inputBounds?.ExactRows;
            var targetId = $"top-offset:{_topOffsetIndex++}";
            var fact = exactRows.HasValue
                ? new CardinalityFact(
                    targetId,
                    "TopOffset",
                    CardinalityKind.Exact,
                    ClampSlice(exactRows.Value, topOffset.Skip, topOffset.Take),
                    ClampSlice(exactRows.Value, topOffset.Skip, topOffset.Take),
                    ClampSlice(exactRows.Value, topOffset.Skip, topOffset.Take),
                    inputBounds!.Confidence,
                    $"Top-offset skips {topOffset.Skip} row(s) and takes at most {topOffset.Take} row(s).")
                : new CardinalityFact(
                    targetId,
                    "TopOffset",
                    CardinalityKind.Bounded,
                    null,
                    0,
                    inputBounds?.UpperBound is { } upper ? ClampSlice(upper, topOffset.Skip, topOffset.Take) : topOffset.Take,
                    inputBounds?.Confidence ?? 1d,
                    $"Top-offset skips {topOffset.Skip} row(s) and takes at most {topOffset.Take} row(s).");

            return AddFact(fact, topOffset);
        }

        private bool TryCreateSourceFact(PhysicalSchemaScanNode scan, out CardinalityFact fact)
        {
            fact = null!;
            if (string.IsNullOrWhiteSpace(scan.SourceContextId) ||
                !sourcePlanning.SourcePlanResultsBySourceId.TryGetValue(scan.SourceContextId, out var result) ||
                result.Cardinality == null ||
                result.Cardinality.Kind == CardinalityKind.Unknown)
            {
                return false;
            }

            var cardinality = result.Cardinality;
            fact = new CardinalityFact(
                $"source:{scan.SourceContextId}",
                "SourceEstimate",
                cardinality.Kind,
                cardinality.ExactRows,
                cardinality.LowerBound,
                cardinality.UpperBound,
                cardinality.Confidence,
                string.IsNullOrWhiteSpace(cardinality.Reason)
                    ? "Source returned a cardinality estimate."
                    : cardinality.Reason);
            return true;
        }

        private CardinalityBounds AddFact(CardinalityFact fact, PhysicalNode node)
        {
            fact = fact with { Node = node };
            _facts.Add(fact);
            _decisions.Add(new PlanningDecision(
                PlanningDecisionCategory.CardinalityFacts,
                "CardinalityFactPlanner",
                fact.TargetId,
                fact.Kind.ToString(),
                ResolveConfidence(fact),
                fact.Reason));

            return new CardinalityBounds(
                fact.Kind,
                fact.ExactRows,
                fact.LowerBound,
                fact.UpperBound,
                fact.Confidence,
                fact.Reason);
        }
    }

    private static CardinalityBounds? ApplySkip(CardinalityBounds? inputBounds, long skip)
    {
        if (inputBounds == null)
            return null;

        if (inputBounds.ExactRows.HasValue)
        {
            var exact = Math.Max(0, inputBounds.ExactRows.Value - skip);
            return inputBounds with
            {
                Kind = CardinalityKind.Exact,
                ExactRows = exact,
                LowerBound = exact,
                UpperBound = exact,
                Reason = $"SKIP removes up to {skip} row(s) from an exact input."
            };
        }

        if (inputBounds.UpperBound.HasValue)
        {
            return inputBounds with
            {
                Kind = CardinalityKind.Bounded,
                ExactRows = null,
                LowerBound = 0,
                UpperBound = Math.Max(0, inputBounds.UpperBound.Value - skip),
                Reason = $"SKIP removes up to {skip} row(s) from a bounded input."
            };
        }

        return null;
    }

    private static CardinalityBounds? ApplyReducingBoundary(CardinalityBounds? inputBounds, string reason)
    {
        if (inputBounds?.UpperBound == null)
            return null;

        return inputBounds with
        {
            Kind = CardinalityKind.Bounded,
            ExactRows = null,
            LowerBound = 0,
            Reason = reason
        };
    }

    private static long ClampSlice(long inputRows, long skip, long take)
    {
        return Math.Min(Math.Max(0, inputRows - skip), take);
    }

    private static PlanningConfidence ResolveConfidence(CardinalityFact fact)
    {
        return fact.Kind switch
        {
            CardinalityKind.Exact => PlanningConfidence.High,
            CardinalityKind.Bounded when fact.Confidence >= 0.8d => PlanningConfidence.High,
            CardinalityKind.Bounded => PlanningConfidence.Medium,
            CardinalityKind.Estimate when fact.Confidence >= 0.8d => PlanningConfidence.Medium,
            _ => PlanningConfidence.Low
        };
    }

    private sealed record CardinalityBounds(
        CardinalityKind Kind,
        long? ExactRows,
        long? LowerBound,
        long? UpperBound,
        double Confidence,
        string Reason);
}
