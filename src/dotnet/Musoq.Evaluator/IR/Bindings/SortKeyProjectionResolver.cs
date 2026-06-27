using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using AggregateRefRewriter = Musoq.Evaluator.IR.Expressions.AggregateRefRewriter;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Bindings;

internal static class SortKeyProjectionResolver
{
    public static string? TryResolveOutputName(
        IrExpression sortExpression,
        IReadOnlyList<ProjectedField> projectedFields)
    {
        ArgumentNullException.ThrowIfNull(sortExpression);
        ArgumentNullException.ThrowIfNull(projectedFields);
        return TryResolveAggregateSortOutputName(sortExpression, projectedFields)
               ?? TryResolveProjectedFieldByExpression(sortExpression, projectedFields);
    }

    private static string? TryResolveProjectedFieldByExpression(
        IrExpression sortExpression,
        IReadOnlyList<ProjectedField> projectedFields)
    {
        var sortPrinted = IrExpressionPrinter.Print(sortExpression);
        if (string.IsNullOrWhiteSpace(sortPrinted))
            return null;

        foreach (var field in projectedFields)
        {
            if (string.Equals(IrExpressionPrinter.Print(field.Expression), sortPrinted, StringComparison.Ordinal))
                return field.OutputName;
        }

        return null;
    }

    private static string? TryResolveAggregateSortOutputName(
        IrExpression sortExpression,
        IReadOnlyList<ProjectedField> projectedFields)
    {
        if (sortExpression is AggregateRef aggregateRef)
        {
            var aggregateCandidates = projectedFields
                .Where(field => field.Expression is AggregateRef fieldAggregateRef &&
                                string.Equals(fieldAggregateRef.Identifier, aggregateRef.Identifier, StringComparison.Ordinal))
                .ToArray();

            return aggregateCandidates.Length == 1
                ? aggregateCandidates[0].OutputName
                : null;
        }

        if (sortExpression is not MethodCall sortMethodCall ||
            !AggregateRefRewriter.IsAggregateMethod(sortMethodCall.Method))
            return null;

        var normalizedSortIdentifier = AggregateRefRewriter.NormalizeIdentifier(
            AggregateRefRewriter.ExtractIdentifier(sortMethodCall));

        if (!string.IsNullOrWhiteSpace(normalizedSortIdentifier))
        {
            var identifierCandidates = projectedFields
                .Where(field => MatchesAggregateIdentifier(field.Expression, normalizedSortIdentifier))
                .ToArray();

            if (identifierCandidates.Length == 1)
                return identifierCandidates[0].OutputName;
        }

        var methodCandidates = projectedFields
            .Where(field => field.Expression is MethodCall projectedMethodCall &&
                            AggregateRefRewriter.IsAggregateMethod(projectedMethodCall.Method) &&
                            projectedMethodCall.Method == sortMethodCall.Method)
            .ToArray();

        if (methodCandidates.Length == 1)
            return methodCandidates[0].OutputName;

        var aggregateRefCandidates = projectedFields
            .Where(field => field.Expression is AggregateRef)
            .ToArray();

        return aggregateRefCandidates.Length == 1
            ? aggregateRefCandidates[0].OutputName
            : null;
    }

    private static bool MatchesAggregateIdentifier(IrExpression expression, string normalizedIdentifier)
    {
        return expression switch
        {
            ColumnRef columnRef => string.Equals(
                AggregateRefRewriter.NormalizeIdentifier(columnRef.ColumnName),
                normalizedIdentifier,
                StringComparison.Ordinal),
            AggregateRef aggregateRef => string.Equals(
                AggregateRefRewriter.NormalizeIdentifier(aggregateRef.Identifier),
                normalizedIdentifier,
                StringComparison.Ordinal),
            MethodCall methodCall when AggregateRefRewriter.IsAggregateMethod(methodCall.Method) =>
                string.Equals(
                    AggregateRefRewriter.NormalizeIdentifier(AggregateRefRewriter.ExtractIdentifier(methodCall)),
                    normalizedIdentifier,
                    StringComparison.Ordinal),
            _ => false
        };
    }
}
