using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PredicateMovementPlanner
{
    private sealed class PredicateMovementPlanningState(
        LogicalNode logicalPlan,
        IReadOnlyDictionary<string, SourcePlanProperties> sources,
        IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlans,
        IReadOnlyDictionary<string, SourceInteractionPlan> sourceInteractionPlans)
    {
        private readonly Dictionary<string, SourcePlanProperties[]> _sourcesByAlias = sources.Values
            .GroupBy(static source => source.Alias, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IrExpression[]> _wherePredicatesByAlias =
            CreateWherePredicatesByAlias(logicalPlan, sourcePredicatePlans);
        private readonly Dictionary<string, SourceInteractionPlan[]> _interactionsByAlias = sourceInteractionPlans.Values
            .GroupBy(static plan => plan.Alias, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _movedWherePredicates = new(StringComparer.Ordinal);
        private readonly List<PredicateMovementPlan> _plans = [];
        private readonly List<PlanningDecision> _decisions = [];

        public IReadOnlyList<PredicateMovementPlan> Plans => _plans;

        public IReadOnlyList<PlanningDecision> Decisions => _decisions;

        public void Visit(LogicalNode node)
        {
            foreach (var child in node.Children)
                Visit(child);

            if (node is JoinNode join)
                AddJoinMovements(join);
        }

        private void AddJoinMovements(JoinNode join)
        {
            if (join.Kind != JoinKind.Inner)
                return;

            AddWhereMovements(join);
            AddJoinOnMovements(join);
        }

        private void AddWhereMovements(JoinNode join)
        {
            foreach (var entry in _wherePredicatesByAlias.OrderBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase))
            {
                var alias = entry.Key;
                foreach (var predicate in entry.Value)
                {
                    var key = CreateMovedWhereKey(alias, predicate);
                    if (_movedWherePredicates.Contains(key))
                        continue;

                    var side = ResolveSide(join, alias, predicate);
                    if (!side.IsEligible)
                        continue;

                    var result = CreatePlan(join, side.Side, PredicatePlacementOrigin.Where, alias, predicate, side.Reason);
                    if (!result.IsCreated)
                    {
                        AddDecision(CreateSkippedId(PredicatePlacementOrigin.Where, alias, predicate), "Skipped", PlanningConfidence.Medium, result.SkipReason);
                        continue;
                    }

                    _movedWherePredicates.Add(key);
                    AddPlan(result.Plan!);
                }
            }
        }

        private void AddJoinOnMovements(JoinNode join)
        {
            foreach (var predicate in SplitConjuncts(join.OnPredicate))
            {
                var aliases = ExtractAliases(predicate);
                if (aliases.Length != 1)
                    continue;

                var alias = aliases[0];
                var side = ResolveSide(join, alias, predicate);
                if (!side.IsEligible)
                    continue;

                var result = CreatePlan(join, side.Side, PredicatePlacementOrigin.JoinOn, alias, predicate, side.Reason);
                if (!result.IsCreated)
                {
                    AddDecision(CreateSkippedId(PredicatePlacementOrigin.JoinOn, alias, predicate), "Skipped", PlanningConfidence.Medium, result.SkipReason);
                    continue;
                }

                AddPlan(result.Plan!);
            }
        }

        private SideResolution ResolveSide(JoinNode join, string alias, IrExpression predicate)
        {
            var aliasEligibility = ResolveMovableSourceAlias(alias);
            if (!aliasEligibility.IsEligible)
                return SideResolution.NotEligible(aliasEligibility.Reason);

            var leftContainsAlias = ContainsProducedAlias(join.Left, alias);
            var rightContainsAlias = ContainsProducedAlias(join.Right, alias);
            if (leftContainsAlias == rightContainsAlias)
                return SideResolution.NotEligible($"Alias {alias} could not be mapped to exactly one inner join side.");

            if (leftContainsAlias)
                return ResolveSidePlacement(join.Left, PredicateMovementSide.Left, alias, predicate);

            return ResolveSidePlacement(join.Right, PredicateMovementSide.Right, alias, predicate);
        }

        private static PlanCreationResult CreatePlan(
            JoinNode join,
            PredicateMovementSide side,
            PredicatePlacementOrigin origin,
            string alias,
            IrExpression predicate,
            string sideReason)
        {
            var predicateText = IrExpressionPrinter.Print(predicate);
            var safety = CanMovePredicate(predicate);
            if (!safety.IsSafe)
                return PlanCreationResult.Skipped(safety.Reason);

            var movementId = CreateMovementId(origin, side, alias, predicate);
            var reason = $"Predicate is deterministic, source-local, and mapped to the {FormatSide(side)} side of an inner join. {sideReason} Original predicate remains in place as a semantic safety net.";
            var plan = new PredicateMovementPlan(
                movementId,
                join,
                side,
                origin,
                alias,
                predicate,
                predicateText,
                PlanningConfidence.High,
                reason);
            return PlanCreationResult.Created(plan);
        }

        private static string FormatSide(PredicateMovementSide side)
        {
            return side switch
            {
                PredicateMovementSide.Left => "left",
                PredicateMovementSide.Right => "right",
                _ => side.ToString()
            };
        }

        private void AddPlan(PredicateMovementPlan plan)
        {
            _plans.Add(plan);
            AddDecision(plan.MovementId, "Applied", plan.Confidence, plan.Reason);
        }

        private void AddDecision(string target, string outcome, PlanningConfidence confidence, string reason)
        {
            _decisions.Add(new PlanningDecision(
                PlanningDecisionCategory.PredicateMovement,
                "PredicateMovementPlan",
                target,
                outcome,
                confidence,
                reason));
        }

        private AliasEligibility ResolveMovableSourceAlias(string alias)
        {
            if (!_sourcesByAlias.TryGetValue(alias, out var matches) || matches.Length != 1)
                return AliasEligibility.NotEligible($"Alias {alias} is ambiguous across source scopes.");

            if (!_interactionsByAlias.TryGetValue(alias, out var interactions) || interactions.Length != 1)
                return AliasEligibility.NotEligible($"Alias {alias} does not have a unique source interaction plan.");

            var shapeKind = interactions[0].ShapeKind;
            if (shapeKind != SourceShapeKind.KnownClr)
                return AliasEligibility.NotEligible($"Alias {alias} has {shapeKind} row shape, so movement stays conservative.");

            return AliasEligibility.Eligible();
        }

        private static Dictionary<string, IrExpression[]> CreateWherePredicatesByAlias(
            LogicalNode logicalPlan,
            IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlans)
        {
            var result = new Dictionary<string, Dictionary<string, IrExpression>>(StringComparer.OrdinalIgnoreCase);

            foreach (var plan in sourcePredicatePlans.Values)
            foreach (var predicate in plan.PushedPredicates)
                AddWherePredicate(result, plan.Alias, predicate);

            AddFilterPredicates(logicalPlan, result);

            return result.ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value
                    .OrderBy(static predicate => predicate.Key, StringComparer.Ordinal)
                    .Select(static predicate => predicate.Value)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }

        private static void AddFilterPredicates(
            LogicalNode node,
            Dictionary<string, Dictionary<string, IrExpression>> result)
        {
            if (node is FilterNode filter)
            {
                foreach (var predicate in SplitConjuncts(filter.Predicate))
                {
                    var aliases = ExtractAliases(predicate);
                    if (aliases.Length == 1)
                        AddWherePredicate(result, aliases[0], predicate);
                }
            }

            foreach (var child in node.Children)
                AddFilterPredicates(child, result);
        }

        private static void AddWherePredicate(
            Dictionary<string, Dictionary<string, IrExpression>> result,
            string alias,
            IrExpression predicate)
        {
            if (string.IsNullOrWhiteSpace(alias))
                return;

            if (!result.TryGetValue(alias, out var predicates))
            {
                predicates = new Dictionary<string, IrExpression>(StringComparer.Ordinal);
                result[alias] = predicates;
            }

            predicates.TryAdd(IrExpressionPrinter.Print(predicate), predicate);
        }
    }
}
