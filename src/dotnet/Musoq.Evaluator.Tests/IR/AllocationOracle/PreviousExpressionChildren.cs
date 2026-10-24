using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution.Facts;

internal static class PreviousExpressionChildren
{
    internal static IEnumerable<ExecutionExpression> Get(ExecutionExpression expression)
    {
        return expression switch
        {
            ExecutionStructuralRecord structuralRecord => structuralRecord.Fields.Select(static field => field.Value),
            ExecutionStructuralArray structuralArray => structuralArray.Elements,
            ExecutionStructuralConversion conversion => [conversion.Input],
            ExecutionCteCollectionInput cteCollection => [cteCollection.Rows],
            ExecutionBinary binary => [binary.Left, binary.Right],
            ExecutionMemberRead memberRead => [memberRead.Receiver],
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
            ExecutionPatternMatch or ExecutionStringMatch or ExecutionPrepareLikeMatcher or ExecutionPreparedLikeMatch or ExecutionDynamicLikeMatch or ExecutionLikeMatcherCacheSlot or
                ExecutionPrepareRLikeMatcher or ExecutionPreparedRLikeMatch or ExecutionDynamicRLikeMatch or ExecutionRLikeMatcherCacheSlot => PreviousPatternChildren.GetChildren(expression),
            ExecutionBetween between => [between.Expression, between.Low, between.High],
            ExecutionCaseWhen caseWhen => caseWhen.ElseExpression == null
                ? caseWhen.Branches.SelectMany(static branch => new[] { branch.Condition, branch.Result })
                : caseWhen.Branches.SelectMany(static branch => new[] { branch.Condition, branch.Result }).Concat([caseWhen.ElseExpression]),
            ExecutionCoalesce coalesce => coalesce.Expressions,
            ExecutionStringConcatenation concatenation => concatenation.Expressions,

            ExecutionValueTupleKey valueTupleKey => valueTupleKey.Parts,
            ExecutionContextArray contextArray => contextArray.Segments.Select(static segment => segment.Value),
            ExecutionAggregateCall aggregateCall => aggregateCall.Arguments,
            _ => []
        };
    }
}
