using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.SourcePlanning;
using Musoq.Parser;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal static partial class SourcePredicatePlanContractValidator
{
    public static void Validate(
        SourcePlanRequest request,
        SourcePlanResult result,
        SourcePredicateCapabilities capabilities,
        TextSpan span)
    {
        ValidateCapabilities(capabilities, span);
        Validate(request, result, span);
        ValidateTypedApplications(result, capabilities, span);
    }

    public static void ValidateCapabilities(
        SourcePredicateCapabilities capabilities,
        TextSpan span)
    {
        if (capabilities == null)
            ThrowContract("the source predicate capability declaration is missing", span);

        try
        {
            capabilities.Validate();
        }
        catch (ArgumentException exception)
        {
            ThrowContract($"the source predicate capability declaration is invalid: {exception.Message}", span);
        }
    }

    private static void ValidateTypedApplications(
        SourcePlanResult result,
        SourcePredicateCapabilities capabilities,
        TextSpan span)
    {
        var acceptedMatches = CollectTopLevelPatternMatches(result.AcceptedPredicate, span);
        var applications = result.ExecutionPlan.PredicateApplications;
        var consumed = new bool[acceptedMatches.Count];

        foreach (var application in applications)
        {
            var patternMatch = application.Predicate;

            if (!capabilities.IsKnownVersion || !capabilities.Supports(patternMatch, application.Phase))
            {
                ThrowContract(
                    "the execution plan applies a typed pattern predicate at an unadvertised capability or phase",
                    span);
            }

            var matchIndex = FindMatch(acceptedMatches, patternMatch, consumed);
            if (matchIndex < 0)
                ThrowContract("the execution plan applies a typed predicate that was not accepted", span);

            consumed[matchIndex] = true;
        }

        if (consumed.Any(static applied => !applied))
        {
            ThrowContract(
                "the execution plan is missing an evaluation application for an accepted typed pattern predicate",
                span);
        }
    }

    private static List<SourcePredicatePatternMatch> CollectTopLevelPatternMatches(
        SourcePredicateExpression? predicate,
        TextSpan span)
    {
        var result = new List<SourcePredicatePatternMatch>();
        foreach (var conjunct in FlattenConjunction(predicate))
        {
            if (conjunct is SourcePredicatePatternMatch patternMatch)
            {
                result.Add(patternMatch);
                continue;
            }

            if (SourcePredicatePatternMatchFacts.ContainsPatternMatch(conjunct))
            {
                ThrowContract(
                    "the accepted predicate contains a typed pattern match outside a direct top-level AND conjunct",
                    span);
            }
        }

        return result;
    }

    private static int FindMatch(
        IReadOnlyList<SourcePredicatePatternMatch> acceptedMatches,
        SourcePredicatePatternMatch candidate,
        IReadOnlyList<bool> consumed)
    {
        var comparer = SourcePredicateExpressionComparer.Instance;
        for (var index = 0; index < acceptedMatches.Count; index++)
        {
            if (!consumed[index] && comparer.Equals(acceptedMatches[index], candidate))
                return index;
        }

        return -1;
    }
}
