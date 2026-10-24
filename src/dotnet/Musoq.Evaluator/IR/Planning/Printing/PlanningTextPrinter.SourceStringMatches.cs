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
            if (conjunct is SourcePredicatePatternMatch directMatch)
            {
                var applicationIndex = FindApplication(directMatch, applications, consumed);
                if (applicationIndex >= 0)
                    consumed[applicationIndex] = true;

                AppendPatternMatch(
                    builder,
                    directMatch,
                    applicationIndex < 0 ? null : applications[applicationIndex].Phase,
                    ResolveFallbackReason(directMatch, capabilities, applicationIndex >= 0));
                continue;
            }

            foreach (var nestedMatch in CollectNestedPatternMatches(conjunct))
                AppendPatternMatch(builder, nestedMatch, null, "nested-non-conjunct");
        }
    }

    private static void AppendPatternMatch(
        StringBuilder builder,
        SourcePredicatePatternMatch match,
        SourcePredicateEvaluationPhase? phase,
        string fallbackReason)
    {
        if (match is SourcePredicateRegexMatch regex)
        {
            builder.AppendLine(
                System.Globalization.CultureInfo.InvariantCulture,
                $"      source regex-match: column={regex.Column.Name}, pattern='{EscapeMatchText(regex.Pattern)}', " +
                $"semantics={regex.Semantics}, timeoutMilliseconds={regex.MatchTimeout.TotalMilliseconds}, " +
                $"negated={FormatYesNo(regex.IsNegated)}, acceptedPhase={(phase?.ToString() ?? "none")}, " +
                $"fallback={fallbackReason}");
            return;
        }

        var stringMatch = (SourcePredicateStringMatch)match;
        builder.AppendLine(
            System.Globalization.CultureInfo.InvariantCulture,
            $"      source string-match: column={stringMatch.Column.Name}, pattern='{EscapeMatchText(stringMatch.OriginalPattern)}', " +
            $"kind={stringMatch.Kind}, needle='{EscapeMatchText(stringMatch.Needle)}', comparison={stringMatch.Comparison}, " +
            $"negated={FormatYesNo(stringMatch.IsNegated)}, acceptedPhase={(phase?.ToString() ?? "none")}, " +
            $"fallback={fallbackReason}");
    }

    private static string ResolveFallbackReason(
        SourcePredicatePatternMatch match,
        SourcePredicateCapabilities capabilities,
        bool hasApplication)
    {
        if (hasApplication)
            return "none";
        if (!capabilities.IsKnownVersion)
            return "unknown-contract-version";

        if (match is SourcePredicateRegexMatch regex)
        {
            var regexCapability = capabilities.RegexMatches.FirstOrDefault(candidate =>
                string.Equals(candidate.Column.Name, regex.Column.Name, StringComparison.OrdinalIgnoreCase));
            if (regexCapability == null)
                return "unsupported-column";
            if (regexCapability.Semantics != regex.Semantics)
                return "unsupported-semantics";
            if (regex.IsNegated && !regexCapability.SupportsNegation)
                return "unsupported-negation";
            return "provider-residual";
        }

        var stringMatch = (SourcePredicateStringMatch)match;
        var capability = capabilities.StringMatches.FirstOrDefault(candidate =>
            string.Equals(candidate.Column.Name, stringMatch.Column.Name, StringComparison.OrdinalIgnoreCase));
        if (capability == null)
            return "unsupported-column";
        if (capability.Comparison != stringMatch.Comparison)
            return "unsupported-comparison";
        if (stringMatch.IsNegated && !capability.SupportsNegation)
            return "unsupported-negation";
        if ((capability.Operations & ToOperation(stringMatch.Kind)) == 0)
            return "unsupported-kind";

        return "provider-residual";
    }

    private static int FindApplication(
        SourcePredicatePatternMatch match,
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

    private static IReadOnlyList<SourcePredicatePatternMatch> CollectNestedPatternMatches(
        SourcePredicateExpression predicate)
    {
        var result = new List<SourcePredicatePatternMatch>();
        AddNestedPatternMatches(predicate, result);
        return result;
    }

    private static void AddNestedPatternMatches(
        SourcePredicateExpression predicate,
        ICollection<SourcePredicatePatternMatch> result)
    {
        switch (predicate)
        {
            case SourcePredicatePatternMatch match:
                result.Add(match);
                break;
            case SourcePredicateLogical logical:
                AddNestedPatternMatches(logical.Left, result);
                AddNestedPatternMatches(logical.Right, result);
                break;
            case SourcePredicateComparison comparison:
                AddNestedPatternMatches(comparison.Left, result);
                AddNestedPatternMatches(comparison.Right, result);
                break;
            case SourcePredicateIn sourceIn:
                AddNestedPatternMatches(sourceIn.Expression, result);
                foreach (var value in sourceIn.Values)
                    AddNestedPatternMatches(value, result);
                break;
            case SourcePredicateNullCheck nullCheck:
                AddNestedPatternMatches(nullCheck.Expression, result);
                break;
            case SourcePredicateFlags flags:
                AddNestedPatternMatches(flags.Expression, result);
                AddNestedPatternMatches(flags.Mask, result);
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
