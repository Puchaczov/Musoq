using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.SourcePlanning;
using Musoq.Schema.Optimization;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class ApplyPredicateMovementPlanner
{
    private sealed partial class PlanningState
    {
        private bool IsAmbiguousAlias(string alias)
        {
            if (_producedAliasCounts.TryGetValue(alias, out var count) && count > 1)
                return true;

            return _sources.Values.Count(source =>
                       string.Equals(source.Alias, alias, StringComparison.OrdinalIgnoreCase)) > 1;
        }

        private void AddResidualDecision(
            int ordinal,
            string predicateText,
            PlanningConfidence confidence,
            string reason)
        {
            _decisions.Add(new PlanningDecision(
                PlanningDecisionCategory.PredicateMovement,
                "ApplyPredicateMovementPlan",
                $"Where:ApplyResidual:{ordinal}:{predicateText}",
                "RetainedResidual",
                confidence,
                reason));
        }

        private string CreateMovementId(ApplyBoundary boundary, int ordinal, string predicateText)
        {
            return $"Where:PreApplyRight:Apply{boundary.Ordinal}:{ordinal}:{predicateText}";
        }

        private ApplyBoundary CreateBoundary(ApplyNode apply)
        {
            var ordinal = _applyOrdinals.TryGetValue(apply, out var value) ? value : -1;
            var leftAliases = _expandedLeftAliases.TryGetValue(apply, out var expanded)
                ? new HashSet<string>(expanded, StringComparer.OrdinalIgnoreCase)
                : CollectProducedAliases(apply.Left);
            return new ApplyBoundary(
                apply,
                ordinal,
                leftAliases,
                CollectProducedAliases(apply.Right));
        }

        private static string CreateCteName(ApplyBoundary boundary)
        {
            return string.Concat(
                boundary.LeftAliases
                    .Concat(boundary.RightAliases)
                    .OrderBy(static alias => alias, StringComparer.OrdinalIgnoreCase));
        }

        private static HashSet<string> CreateSourceAcceptedPredicateTexts(
            IReadOnlyDictionary<string, SourcePlanResult>? sourcePlanResults,
            IReadOnlyDictionary<string, SourcePredicatePlan>? sourcePredicatePlans)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (sourcePlanResults == null || sourcePredicatePlans == null)
                return result;

            foreach (var entry in sourcePlanResults)
            {
                if (entry.Value.AcceptedPredicate == null ||
                    !sourcePredicatePlans.TryGetValue(entry.Key, out var sourcePredicatePlan))
                {
                    continue;
                }

                foreach (var predicate in SourcePredicateConjunctMatcher.MatchAcceptedConjuncts(
                             entry.Value.AcceptedPredicate,
                             sourcePredicatePlan,
                             allowWholePredicateMatch: entry.Value.ResidualPredicate == null))
                {
                    result.Add(IrExpressionPrinter.Print(predicate));
                }
            }

            return result;
        }
    }

    private sealed record ApplyBoundary(
        ApplyNode Apply,
        int Ordinal,
        HashSet<string> LeftAliases,
        HashSet<string> RightAliases);

    private static IEnumerable<IrExpression> SplitTopLevelConjuncts(IrExpression predicate)
    {
        if (predicate is BinaryOp { Kind: BinaryOpKind.And } and)
        {
            foreach (var left in SplitTopLevelConjuncts(and.Left))
                yield return left;

            foreach (var right in SplitTopLevelConjuncts(and.Right))
                yield return right;

            yield break;
        }

        yield return predicate;
    }

    private static Dictionary<string, int> CollectProducedAliasCounts(LogicalNode node)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        AddProducedAliasCounts(node, counts);
        return counts;
    }

    private static void AddProducedAliasCounts(LogicalNode node, IDictionary<string, int> counts)
    {
        var alias = node is CteRefNode ? null : GetProducedAlias(node);
        if (!string.IsNullOrWhiteSpace(alias))
            counts[alias] = counts.TryGetValue(alias, out var count) ? count + 1 : 1;

        foreach (var child in node.Children)
            AddProducedAliasCounts(child, counts);
    }

    private static HashSet<string> CollectProducedAliases(LogicalNode node)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddProducedAliases(node, aliases);
        return aliases;
    }

    private static void AddProducedAliases(LogicalNode node, ISet<string> aliases)
    {
        if (node is CteRefNode cteRef)
        {
            if (!string.IsNullOrWhiteSpace(cteRef.Alias))
                aliases.Add(cteRef.Alias);

            foreach (var column in cteRef.OutputSchema.Columns)
            {
                var separator = column.Name.IndexOf('.', StringComparison.Ordinal);
                if (separator > 0)
                    aliases.Add(column.Name[..separator]);
            }
        }

        var alias = GetProducedAlias(node);
        if (!string.IsNullOrWhiteSpace(alias))
            aliases.Add(alias);

        foreach (var child in node.Children)
            AddProducedAliases(child, aliases);
    }

    private static string? GetProducedAlias(LogicalNode node)
    {
        return node switch
        {
            SchemaScanNode scan => scan.Alias,
            InterpretSourceNode interpret => interpret.Alias,
            PropertySourceNode property => property.Alias,
            AccessMethodSourceNode accessMethod => accessMethod.Alias,
            CteRefNode cteRef => cteRef.Alias,
            ValuesScanNode values => values.Alias,
            UnpivotNode unpivot => unpivot.Alias,
            _ => null
        };
    }

    private static List<ApplyNode> CollectApplyBoundaries(LogicalNode node)
    {
        var applies = new List<ApplyNode>();
        AddApplyBoundaries(node, applies);
        return applies;
    }

    private static bool ContainsCteReference(LogicalNode node)
    {
        if (node is CteRefNode)
            return true;

        return node.Children.Any(ContainsCteReference);
    }

    private static void AddApplyBoundaries(LogicalNode node, ICollection<ApplyNode> applies)
    {
        if (node is ApplyNode apply)
            applies.Add(apply);

        foreach (var child in node.Children)
            AddApplyBoundaries(child, applies);
    }

    private static string FormatApplyName(ApplyKind kind)
    {
        return kind == ApplyKind.Outer ? "OUTER APPLY" : "CROSS APPLY";
    }

    private static string FormatScopes(IEnumerable<ApplyBoundary> boundaries)
    {
        var scopes = boundaries
            .Select(static boundary =>
                $"Apply{boundary.Ordinal}=[{string.Join(", ", boundary.LeftAliases.OrderBy(static alias => alias, StringComparer.OrdinalIgnoreCase))}]")
            .ToArray();
        return string.Join("; ", scopes);
    }
}
