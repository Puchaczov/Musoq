using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.SourcePlanning;
using Musoq.Parser;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal static class SourcePredicatePlanContractValidator
{
    public static void Validate(SourcePlanRequest request, SourcePlanResult result, TextSpan span)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(result);
        if (result.ExecutionPlan == null)
            ThrowContract("the datasource returned no execution plan", span);
        if (result.ExecutionPlan.Identity != request.Identity)
            ThrowContract("the execution plan identity differs from the requested source identity", span);

        var comparer = SourcePredicateExpressionComparer.Instance;
        if (!comparer.Equals(result.AcceptedPredicate, result.ExecutionPlan.AcceptedPredicate))
        {
            ThrowContract("the accepted predicate differs from the predicate embedded in the execution plan", span);
        }

        ValidateEnumFingerprints(request.Predicate, result.AcceptedPredicate, span);
        ValidateEnumFingerprints(request.Predicate, result.ResidualPredicate, span);

        var requested = FlattenConjunction(request.Predicate);
        var returned = FlattenConjunction(result.AcceptedPredicate)
            .Concat(FlattenConjunction(result.ResidualPredicate))
            .ToArray();

        if (!AreExactMultisets(requested, returned, comparer))
        {
            ThrowContract(
                "accepted and residual predicates are not an exact partition of the requested predicate",
                span);
        }
    }

    private static SourcePredicateExpression[] FlattenConjunction(SourcePredicateExpression? predicate)
    {
        if (predicate == null)
            return [];

        var result = new List<SourcePredicateExpression>();
        AddConjuncts(predicate, result);
        return result.ToArray();
    }

    private static void AddConjuncts(
        SourcePredicateExpression predicate,
        ICollection<SourcePredicateExpression> result)
    {
        if (predicate is SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.And } logical)
        {
            AddConjuncts(logical.Left, result);
            AddConjuncts(logical.Right, result);
            return;
        }

        result.Add(predicate);
    }

    private static bool AreExactMultisets(
        IReadOnlyList<SourcePredicateExpression> expected,
        IReadOnlyList<SourcePredicateExpression> actual,
        IEqualityComparer<SourcePredicateExpression> comparer)
    {
        if (expected.Count != actual.Count)
            return false;

        var consumed = new bool[actual.Count];
        foreach (var candidate in expected)
        {
            var matched = false;
            for (var index = 0; index < actual.Count; index++)
            {
                if (consumed[index] || !comparer.Equals(candidate, actual[index]))
                    continue;

                consumed[index] = true;
                matched = true;
                break;
            }

            if (!matched)
                return false;
        }

        return true;
    }

    private static void ValidateEnumFingerprints(
        SourcePredicateExpression? requested,
        SourcePredicateExpression? returned,
        TextSpan span)
    {
        if (returned == null)
            return;

        var requestedFingerprints = new HashSet<string>(StringComparer.Ordinal);
        CollectEnumFingerprints(requested, requestedFingerprints);

        var returnedFingerprints = new HashSet<string>(StringComparer.Ordinal);
        CollectEnumFingerprints(returned, returnedFingerprints);
        var unexpected = returnedFingerprints.FirstOrDefault(fingerprint => !requestedFingerprints.Contains(fingerprint));
        if (unexpected == null)
            return;

        throw new EnumDescriptorMismatchException(
            FindFirstColumn(returned) ?? "<predicate>",
            span,
            "The datasource returned a predicate with a different enum fingerprint.");
    }

    private static void CollectEnumFingerprints(
        SourcePredicateExpression? predicate,
        ISet<string> fingerprints)
    {
        switch (predicate)
        {
            case SourcePredicateEnumLiteral literal:
                fingerprints.Add(literal.EnumFingerprint);
                break;
            case SourcePredicateComparison comparison:
                CollectEnumFingerprints(comparison.Left, fingerprints);
                CollectEnumFingerprints(comparison.Right, fingerprints);
                break;
            case SourcePredicateLogical logical:
                CollectEnumFingerprints(logical.Left, fingerprints);
                CollectEnumFingerprints(logical.Right, fingerprints);
                break;
            case SourcePredicateIn sourceIn:
                CollectEnumFingerprints(sourceIn.Expression, fingerprints);
                foreach (var value in sourceIn.Values)
                    CollectEnumFingerprints(value, fingerprints);
                break;
            case SourcePredicateNullCheck nullCheck:
                CollectEnumFingerprints(nullCheck.Expression, fingerprints);
                break;
            case SourcePredicateFlags flags:
                CollectEnumFingerprints(flags.Expression, fingerprints);
                CollectEnumFingerprints(flags.Mask, fingerprints);
                break;
        }
    }

    private static string? FindFirstColumn(SourcePredicateExpression predicate)
    {
        return predicate switch
        {
            SourcePredicateColumn column => column.Column.Name,
            SourcePredicateComparison comparison => FindFirstColumn(comparison.Left) ?? FindFirstColumn(comparison.Right),
            SourcePredicateLogical logical => FindFirstColumn(logical.Left) ?? FindFirstColumn(logical.Right),
            SourcePredicateIn sourceIn => FindFirstColumn(sourceIn.Expression),
            SourcePredicateNullCheck nullCheck => FindFirstColumn(nullCheck.Expression),
            SourcePredicateFlags flags => FindFirstColumn(flags.Expression),
            _ => null
        };
    }

    [DoesNotReturn]
    private static void ThrowContract(string detail, TextSpan span)
    {
        throw new SourcePlanContractException(detail, span);
    }
}
