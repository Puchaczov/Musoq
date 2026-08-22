using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.SourcePlanning;
using Musoq.Schema.Optimization;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class ApplyPredicateMovementPlanner
{
    public static ApplyPredicateMovementPlanningResult Plan(
        LogicalNode logicalPlan,
        IReadOnlyDictionary<string, SourcePlanProperties> sources,
        IReadOnlyDictionary<string, SourcePlanResult>? sourcePlanResults = null,
        IReadOnlyDictionary<string, SourcePredicatePlan>? sourcePredicatePlans = null)
    {
        ArgumentNullException.ThrowIfNull(logicalPlan);
        ArgumentNullException.ThrowIfNull(sources);

        var state = new PlanningState(logicalPlan, sources, sourcePlanResults, sourcePredicatePlans);
        state.Visit(logicalPlan);
        return new ApplyPredicateMovementPlanningResult(state.Plans, state.Decisions);
    }

    private sealed partial class PlanningState
    {
        private readonly Dictionary<string, int> _producedAliasCounts;
        private readonly Dictionary<ApplyNode, int> _applyOrdinals = new(ReferenceComparer<ApplyNode>.Instance);
        private readonly Dictionary<ApplyNode, HashSet<string>> _expandedLeftAliases = new(ReferenceComparer<ApplyNode>.Instance);
        private readonly HashSet<string> _sourceAcceptedPredicateTexts;
        private readonly IReadOnlyDictionary<string, IReadOnlySet<string>> _sourceAliasesByCteAlias;
        private readonly IReadOnlyDictionary<string, SourcePlanProperties> _sources;
        private ApplyBoundary[] _allBoundaries = [];
        private readonly List<ApplyPredicateMovementPlan> _plans = [];
        private readonly List<PlanningDecision> _decisions = [];
        private int _predicateOrdinal;

        public PlanningState(
            LogicalNode logicalPlan,
            IReadOnlyDictionary<string, SourcePlanProperties> sources,
            IReadOnlyDictionary<string, SourcePlanResult>? sourcePlanResults,
            IReadOnlyDictionary<string, SourcePredicatePlan>? sourcePredicatePlans)
        {
            _sources = sources;
            _sourceAcceptedPredicateTexts = CreateSourceAcceptedPredicateTexts(sourcePlanResults, sourcePredicatePlans);
            _producedAliasCounts = CollectProducedAliasCounts(logicalPlan);
            _sourceAliasesByCteAlias = CreateSourceAliasesByCteAlias(logicalPlan);

            var ordinal = 0;
            foreach (var apply in CollectApplyBoundaries(logicalPlan))
                _applyOrdinals[apply] = ordinal++;

            var rawBoundaries = _applyOrdinals
                .OrderBy(static entry => entry.Value)
                .Select(entry => CreateBoundary(entry.Key))
                .ToArray();

            foreach (var boundary in rawBoundaries)
            {
                var leftAliases = new HashSet<string>(boundary.LeftAliases, StringComparer.OrdinalIgnoreCase);
                if (boundary.Apply.Left is CteRefNode cteRef)
                {
                    var producer = rawBoundaries
                        .Where(previous => previous.Ordinal < boundary.Ordinal)
                        .LastOrDefault(previous => string.Equals(
                            CreateCteName(previous),
                            cteRef.CteName,
                            StringComparison.OrdinalIgnoreCase));
                    if (producer != null)
                    {
                        leftAliases.UnionWith(producer.LeftAliases);
                        leftAliases.UnionWith(producer.RightAliases);
                    }
                }

                _expandedLeftAliases[boundary.Apply] = leftAliases;
            }

            _allBoundaries = rawBoundaries
                .Select(boundary => new ApplyBoundary(
                    boundary.Apply,
                    boundary.Ordinal,
                    new HashSet<string>(_expandedLeftAliases[boundary.Apply], StringComparer.OrdinalIgnoreCase),
                    boundary.RightAliases))
                .ToArray();
        }

        public IReadOnlyList<ApplyPredicateMovementPlan> Plans => _plans;

        public IReadOnlyList<PlanningDecision> Decisions => _decisions;

        public void Visit(LogicalNode node)
        {
            if (node is FilterNode filter)
                PlanFilter(filter);

            foreach (var child in node.Children)
                Visit(child);
        }

        private void PlanFilter(FilterNode filter)
        {
            var boundaries = CollectApplyBoundaries(filter.Input)
                .Select(CreateBoundary)
                .Where(static boundary => boundary.Apply.Kind is ApplyKind.Cross or ApplyKind.Outer)
                .ToArray();
            if (boundaries.Length == 0 && ContainsCteReference(filter.Input))
            {
                boundaries = _allBoundaries
                    .Where(static boundary => boundary.Apply.Kind is ApplyKind.Cross or ApplyKind.Outer)
                    .ToArray();
            }

            if (boundaries.Length == 0)
                return;

            foreach (var predicate in SplitTopLevelConjuncts(filter.Predicate))
            {
                var ordinal = _predicateOrdinal++;
                PlanConjunct(predicate, ordinal, boundaries);
            }
        }

        private void PlanConjunct(
            IrExpression predicate,
            int ordinal,
            IReadOnlyList<ApplyBoundary> boundaries)
        {
            var originalPredicateText = IrExpressionPrinter.Print(predicate);
            var movementPredicate = RewriteCteAliases(predicate);
            var aliases = AliasRefExtractor.Extract(movementPredicate).ToArray();
            if (aliases.Length == 0)
                return;

            if (_sourceAcceptedPredicateTexts.Contains(originalPredicateText))
            {
                _decisions.Add(new PlanningDecision(
                    PlanningDecisionCategory.PredicateMovement,
                    "ApplyPredicateMovementPlan",
                    $"Where:SourceAccepted:{ordinal}:{originalPredicateText}",
                    "AlreadySourcePushed",
                    PlanningConfidence.High,
                    "Predicate was accepted by source planning and does not need a duplicate APPLY guard."));
                return;
            }

            var candidates = boundaries
                .Where(boundary => aliases.All(boundary.LeftAliases.Contains))
                .OrderBy(static boundary => boundary.LeftAliases.Count)
                .ThenBy(static boundary => boundary.Ordinal)
                .ToArray();

            if (candidates.Length == 0)
            {
                AddResidualDecision(
                    ordinal,
                    originalPredicateText,
                    PlanningConfidence.Medium,
                    $"Predicate references a right-side or future APPLY alias and remains residual until all referenced aliases are available. Available APPLY left scopes: {FormatScopes(boundaries)}.");
                return;
            }

            if (aliases.Any(IsAmbiguousAlias))
            {
                AddResidualDecision(
                    ordinal,
                    originalPredicateText,
                    PlanningConfidence.Low,
                    "Predicate references an alias that appears in multiple source scopes, so APPLY movement remains conservative.");
                return;
            }

            if (predicate is BinaryOp { Kind: BinaryOpKind.Or })
            {
                AddResidualDecision(
                    ordinal,
                    originalPredicateText,
                    PlanningConfidence.High,
                    "OR predicates are kept intact; only top-level AND conjuncts can become APPLY guards.");
                return;
            }

            if (predicate.ReturnType != typeof(bool))
            {
                AddResidualDecision(
                    ordinal,
                    originalPredicateText,
                    PlanningConfidence.Low,
                    "Predicate is not a typed boolean expression, so it remains residual.");
                return;
            }

            if (!IrExpressionDeterminism.IsDeterministic(predicate))
            {
                IrExpressionDeterminism.TryGetFirstBlockedReason(predicate, out var blockedReason, "Predicate");
                AddResidualDecision(
                    ordinal,
                    originalPredicateText,
                    PlanningConfidence.Medium,
                    string.IsNullOrWhiteSpace(blockedReason)
                        ? "Predicate is non-deterministic or contains an unsupported planning-time expression, so it remains residual."
                        : $"{blockedReason} APPLY movement is disabled for this predicate.");
                return;
            }

            var boundary = candidates[0];
            movementPredicate = RewriteForBoundary(movementPredicate, boundary);
            var boundaryAliases = AliasRefExtractor.Extract(movementPredicate).ToArray();
            var predicateText = IrExpressionPrinter.Print(movementPredicate);
            var plan = new ApplyPredicateMovementPlan(
                CreateMovementId(boundary, ordinal, predicateText),
                boundary.Apply,
                PredicatePlacementOrigin.Where,
                PredicateEarliestPlacement.PreApplyRight,
                boundaryAliases,
                movementPredicate,
                predicateText,
                PlanningConfidence.High,
                $"Predicate is deterministic and references only aliases available before the right side of the {FormatApplyName(boundary.Apply.Kind)} boundary; it can be evaluated before right-side setup.")
            {
                ResidualPredicateText = originalPredicateText
            };

            _plans.Add(plan);
            _decisions.Add(new PlanningDecision(
                PlanningDecisionCategory.PredicateMovement,
                "ApplyPredicateMovementPlan",
                plan.MovementId,
                plan.Placement.ToString(),
                plan.Confidence,
                plan.Reason));
        }
    }
}
