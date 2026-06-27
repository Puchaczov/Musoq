using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Evaluator.IR.Execution;

internal static partial class ExecutionIrAnalysis
{
    internal static IEnumerable<ExecutionExpression> GetContextLayoutExpressions(ExecutionContextLayout? contextLayout) =>
        ExecutionNodeFacts.GetContextLayoutExpressions(contextLayout);

    internal static IEnumerable<ExecutionExpression> FlattenExpressions(ExecutionBlock block)
    {
        foreach (var node in FlattenNodes(block))
        {
            foreach (var expression in GetNodeExpressions(node))
            {
                foreach (var childExpression in FlattenExpressions(expression))
                    yield return childExpression;
            }
        }
    }

    internal static IEnumerable<ExecutionExpression> FlattenExpressions(IEnumerable<ExecutionExpression> expressions)
    {
        foreach (var expression in expressions)
        {
            foreach (var childExpression in FlattenExpressions(expression))
                yield return childExpression;
        }
    }

    internal static IEnumerable<ExecutionExpression> FlattenExpressions(ExecutionExpression? expression)
    {
        if (expression == null)
            yield break;

        yield return expression;

        foreach (var child in GetChildExpressions(expression))
        {
            foreach (var childExpression in FlattenExpressions(child))
                yield return childExpression;
        }
    }

    internal static IEnumerable<TExpression> CollectExpressions<TExpression>(ExecutionBlock block)
        where TExpression : ExecutionExpression
    {
        foreach (var expression in FlattenExpressions(block))
        {
            if (expression is TExpression match)
                yield return match;
        }
    }

    internal static IEnumerable<ExecutionExpression> GetNodeExpressions(ExecutionNode node)
    {
        return ExecutionNodeFacts.GetLocalExpressions(node);
    }

    internal static IEnumerable<ExecutionExpression> GetChildExpressions(ExecutionExpression expression)
    {
        return expression switch
        {
            ExecutionBinary binary => [binary.Left, binary.Right],
            ExecutionUnary unary => [unary.Operand],
            ExecutionMethodCall methodCall => methodCall.InjectedSource == null
                ? methodCall.Arguments
                : methodCall.Arguments.Concat([methodCall.InjectedSource]),
            ExecutionStrictCast strictCast => [strictCast.Expression],
            ExecutionMethodTargetReuseCandidate candidate => [candidate.MethodCall],
            ExecutionArrayAccess arrayAccess => [arrayAccess.Array, arrayAccess.Index],
            ExecutionIndexedHashRowCreate => [],
            ExecutionIndexedHashRowRowRead => [],
            ExecutionIndexedHashRowIndexRead => [],
            ExecutionIsNullCheck isNull => [isNull.Expression],
            ExecutionRowPresence rowPresence => [rowPresence.PresenceSource],
            ExecutionInCheck inCheck => [inCheck.Expression, .. inCheck.Values],
            ExecutionCollectionInCheck collectionInCheck => [collectionInCheck.Expression, collectionInCheck.Collection],
            ExecutionPatternMatch patternMatch => [patternMatch.Expression, patternMatch.Pattern],
            ExecutionBetween between => [between.Expression, between.Low, between.High],
            ExecutionCaseWhen caseWhen => caseWhen.ElseExpression == null
                ? caseWhen.Branches.SelectMany(static branch => new[] { branch.Condition, branch.Result })
                : caseWhen.Branches.SelectMany(static branch => new[] { branch.Condition, branch.Result }).Concat([caseWhen.ElseExpression]),
            ExecutionCoalesce coalesce => coalesce.Expressions,
            ExecutionCompositeKey compositeKey => compositeKey.Parts,
            ExecutionValueTupleKey valueTupleKey => valueTupleKey.Parts,
            ExecutionContextArray contextArray => contextArray.Segments.Select(static segment => segment.Value),
            ExecutionAggregateCall aggregateCall => aggregateCall.Arguments,
            _ => []
        };
    }
}
