using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Evaluator.IR.SourcePlanning;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.IR.Planning.Printing;

internal static partial class PlanningTextPrinter
{
    private static void AppendSourceStringMatchDiagnostics(
        StringBuilder builder,
        SourcePredicateExpression? predicate,
        SourcePlanResult plan,
        SourcePredicateCapabilities capabilities)
    {
        var applications = plan.ExecutionPlan.PredicateApplications;
        var consumed = new bool[applications.Count];
        foreach (var conjunct in FlattenSourceConjunction(predicate))
        {
            if (conjunct is SourcePredicateStringMatch directMatch)
            {
                var applicationIndex = FindApplication(directMatch, applications, consumed);
                if (applicationIndex >= 0)
                    consumed[applicationIndex] = true;

                AppendStringMatch(
                    builder,
                    directMatch,
                    applicationIndex < 0 ? null : applications[applicationIndex].Phase,
                    ResolveFallbackReason(directMatch, capabilities, applicationIndex >= 0));
                continue;
            }

            foreach (var nestedMatch in CollectNestedStringMatches(conjunct))
                AppendStringMatch(builder, nestedMatch, null, "nested-non-conjunct");
        }
    }

    private static void AppendStringMatch(
        StringBuilder builder,
        SourcePredicateStringMatch match,
        SourcePredicateEvaluationPhase? phase,
        string fallbackReason)
    {
        builder.AppendLine(
            System.Globalization.CultureInfo.InvariantCulture,
            $"      source string-match: column={match.Column.Name}, pattern='{EscapeMatchText(match.OriginalPattern)}', " +
            $"kind={match.Kind}, needle='{EscapeMatchText(match.Needle)}', comparison={match.Comparison}, " +
            $"negated={FormatYesNo(match.IsNegated)}, acceptedPhase={(phase?.ToString() ?? "none")}, " +
            $"fallback={fallbackReason}");
    }

    private static string ResolveFallbackReason(
        SourcePredicateStringMatch match,
        SourcePredicateCapabilities capabilities,
        bool hasApplication)
    {
        if (hasApplication)
            return "none";
        if (!capabilities.IsKnownVersion)
            return "unknown-contract-version";

        var capability = capabilities.StringMatches.FirstOrDefault(candidate =>
            string.Equals(candidate.Column.Name, match.Column.Name, StringComparison.OrdinalIgnoreCase));
        if (capability == null)
            return "unsupported-column";
        if (capability.Comparison != match.Comparison)
            return "unsupported-comparison";
        if (match.IsNegated && !capability.SupportsNegation)
            return "unsupported-negation";
        if ((capability.Operations & ToOperation(match.Kind)) == 0)
            return "unsupported-kind";

        return "provider-residual";
    }

    private static int FindApplication(
        SourcePredicateStringMatch match,
        IReadOnlyList<SourcePredicateApplication> applications,
        IReadOnlyList<bool> consumed)
    {
        for (var index = 0; index < applications.Count; index++)
        {
            if (!consumed[index] && SourcePredicateExpressionComparer.Instance.Equals(match, applications[index].Predicate))
                return index;
        }

        return -1;
    }

    private static IReadOnlyList<SourcePredicateExpression> FlattenSourceConjunction(
        SourcePredicateExpression? predicate)
    {
        var result = new List<SourcePredicateExpression>();
        AddSourceConjuncts(predicate, result);
        return result;
    }

    private static void AddSourceConjuncts(
        SourcePredicateExpression? predicate,
        ICollection<SourcePredicateExpression> result)
    {
        if (predicate is SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.And } logical)
        {
            AddSourceConjuncts(logical.Left, result);
            AddSourceConjuncts(logical.Right, result);
            return;
        }

        if (predicate != null)
            result.Add(predicate);
    }

    private static IReadOnlyList<SourcePredicateStringMatch> CollectNestedStringMatches(
        SourcePredicateExpression predicate)
    {
        var result = new List<SourcePredicateStringMatch>();
        AddNestedStringMatches(predicate, result);
        return result;
    }

    private static void AddNestedStringMatches(
        SourcePredicateExpression predicate,
        ICollection<SourcePredicateStringMatch> result)
    {
        switch (predicate)
        {
            case SourcePredicateStringMatch match:
                result.Add(match);
                break;
            case SourcePredicateLogical logical:
                AddNestedStringMatches(logical.Left, result);
                AddNestedStringMatches(logical.Right, result);
                break;
            case SourcePredicateComparison comparison:
                AddNestedStringMatches(comparison.Left, result);
                AddNestedStringMatches(comparison.Right, result);
                break;
            case SourcePredicateIn sourceIn:
                AddNestedStringMatches(sourceIn.Expression, result);
                foreach (var value in sourceIn.Values)
                    AddNestedStringMatches(value, result);
                break;
            case SourcePredicateNullCheck nullCheck:
                AddNestedStringMatches(nullCheck.Expression, result);
                break;
            case SourcePredicateFlags flags:
                AddNestedStringMatches(flags.Expression, result);
                AddNestedStringMatches(flags.Mask, result);
                break;
        }
    }

    private static SourceStringMatchOperations ToOperation(SourceStringMatchKind kind) => kind switch
    {
        SourceStringMatchKind.Exact => SourceStringMatchOperations.Exact,
        SourceStringMatchKind.Prefix => SourceStringMatchOperations.Prefix,
        SourceStringMatchKind.Suffix => SourceStringMatchOperations.Suffix,
        SourceStringMatchKind.Contains => SourceStringMatchOperations.Contains,
        _ => SourceStringMatchOperations.None
    };

    private static string EscapeMatchText(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("'", "''", StringComparison.Ordinal)
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);
}
