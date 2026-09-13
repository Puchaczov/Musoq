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
        var acceptedMatches = CollectTopLevelStringMatches(result.AcceptedPredicate, span);
        var applications = result.ExecutionPlan.PredicateApplications;
        var consumed = new bool[acceptedMatches.Count];

        foreach (var application in applications)
        {
            var stringMatch = application.Predicate;

            if (!capabilities.IsKnownVersion || !capabilities.Supports(stringMatch, application.Phase))
            {
                ThrowContract(
                    "the execution plan applies a typed string predicate at an unadvertised capability or phase",
                    span);
            }

            var matchIndex = FindMatch(acceptedMatches, stringMatch, consumed);
            if (matchIndex < 0)
                ThrowContract("the execution plan applies a typed predicate that was not accepted", span);

            consumed[matchIndex] = true;
        }

        if (consumed.Any(static applied => !applied))
        {
            ThrowContract(
                "the execution plan is missing an evaluation application for an accepted typed string predicate",
                span);
        }
    }

    private static List<SourcePredicateStringMatch> CollectTopLevelStringMatches(
        SourcePredicateExpression? predicate,
        TextSpan span)
    {
        var result = new List<SourcePredicateStringMatch>();
        foreach (var conjunct in FlattenConjunction(predicate))
        {
            if (conjunct is SourcePredicateStringMatch stringMatch)
            {
                result.Add(stringMatch);
                continue;
            }

            if (ContainsStringMatch(conjunct))
            {
                ThrowContract(
                    "the accepted predicate contains a typed string match outside a direct top-level AND conjunct",
                    span);
            }
        }

        return result;
    }

    private static bool ContainsStringMatch(SourcePredicateExpression? predicate)
    {
        return predicate switch
        {
            SourcePredicateStringMatch => true,
            SourcePredicateLogical logical => ContainsStringMatch(logical.Left) || ContainsStringMatch(logical.Right),
            SourcePredicateComparison comparison =>
                ContainsStringMatch(comparison.Left) || ContainsStringMatch(comparison.Right),
            SourcePredicateIn sourceIn =>
                ContainsStringMatch(sourceIn.Expression) || sourceIn.Values.Any(ContainsStringMatch),
            SourcePredicateNullCheck nullCheck => ContainsStringMatch(nullCheck.Expression),
            SourcePredicateFlags flags => ContainsStringMatch(flags.Expression) || ContainsStringMatch(flags.Mask),
            _ => false
        };
    }

    private static int FindMatch(
        IReadOnlyList<SourcePredicateStringMatch> acceptedMatches,
        SourcePredicateStringMatch candidate,
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
