using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution.Facts;

internal static partial class ExecutionExpressionChildren
{
    internal static IEnumerable<ExecutionExpression> Get(ExecutionExpression expression)
    {
        return expression switch
        {
            ExecutionSubquery subquery => (subquery.Operand is null ? subquery.Bindings : subquery.Bindings.Append(subquery.Operand))
                .Concat(subquery.CapturedScriptValues).Concat(subquery.CapturedCteValues),
            ExecutionStructuralArray array => array.Elements,
            ExecutionCoalesce coalesce => coalesce.Expressions,
            ExecutionStringConcatenation concatenation => concatenation.Expressions,
            ExecutionValueTupleKey key => key.Parts,
            ExecutionAggregateCall aggregate => aggregate.Arguments,
            ExecutionMethodCall { InjectedSource: null } call => call.Arguments,
            _ => MaterializeSmallChildren(expression)
        };
    }

    private static IEnumerable<ExecutionExpression> MaterializeSmallChildren(ExecutionExpression expression)
    {
        var first = GetAt(expression, 0);
        if (first is null)
            return [];
        var second = GetAt(expression, 1);
        if (second is null)
            return [first];
        var third = GetAt(expression, 2);
        if (third is null)
            return [first, second];
        if (GetAt(expression, 3) is null)
            return [first, second, third];
        return EnumerateChildren(expression);
    }

    private static IEnumerable<ExecutionExpression> EnumerateChildren(ExecutionExpression expression)
    {
        var cursor = new ExecutionExpressionChildCursor(expression);
        while (cursor.MoveNext())
            yield return cursor.Current;
    }

    internal static ExecutionExpression? GetAt(ExecutionExpression expression, int index) =>
        expression switch
        {
            ExecutionSubquery subquery => SubqueryChild(subquery, index),
            ExecutionStructuralConversion conversion => index == 0 ? conversion.Input : null,
            ExecutionCteCollectionInput cteCollection => index == 0 ? cteCollection.Rows : null,
            ExecutionUnionMemberRead union => index == 0 ? union.Receiver : null,
            ExecutionMemberRead memberRead => index == 0 ? memberRead.Receiver : null,
            ExecutionUnary unary => index == 0 ? unary.Operand : null,
            ExecutionStrictCast strictCast => index == 0 ? strictCast.Expression : null,
            ExecutionMethodTargetReuseCandidate candidate => index == 0 ? candidate.MethodCall : null,
            ExecutionIsNullCheck isNull => index == 0 ? isNull.Expression : null,
            ExecutionRowPresence rowPresence => index == 0 ? rowPresence.PresenceSource : null,
            ExecutionBinary binary => index switch { 0 => binary.Left, 1 => binary.Right, _ => null },
            ExecutionArrayAccess arrayAccess => index switch { 0 => arrayAccess.Array, 1 => arrayAccess.Index, _ => null },
            ExecutionCollectionInCheck collectionInCheck => index switch { 0 => collectionInCheck.Expression, 1 => collectionInCheck.Collection, _ => null },
            ExecutionBetween between => index switch { 0 => between.Expression, 1 => between.Low, 2 => between.High, _ => null },
            ExecutionStructuralArray array => At(array.Elements, index),
            ExecutionCoalesce coalesce => At(coalesce.Expressions, index),
            ExecutionStringConcatenation concatenation => At(concatenation.Expressions, index),
            ExecutionValueTupleKey key => At(key.Parts, index),
            ExecutionAggregateCall aggregate => At(aggregate.Arguments, index),
            ExecutionStructuralRecord record => index < record.Fields.Count ? record.Fields[index].Value : null,
            ExecutionContextArray array => index < array.Segments.Count ? array.Segments[index].Value : null,
            ExecutionMethodCall call => index < call.Arguments.Count ? call.Arguments[index] : index == call.Arguments.Count ? call.InjectedSource : null,
            ExecutionInCheck check => index == 0 ? check.Expression : At(check.Values, index - 1),
            ExecutionCaseWhen conditional => CaseChild(conditional, index),
            ExecutionPatternMatch or ExecutionStringMatch or ExecutionPrepareLikeMatcher or ExecutionPreparedLikeMatch or
                ExecutionDynamicLikeMatch or ExecutionLikeMatcherCacheSlot or ExecutionPrepareRLikeMatcher or
                ExecutionPreparedRLikeMatch or ExecutionDynamicRLikeMatch or ExecutionRLikeMatcherCacheSlot =>
                    PatternExpressionFacts.GetChildAt(expression, index),
            _ => null
        };

    internal static ExecutionExpression? At(IReadOnlyList<ExecutionExpression> expressions, int index) =>
        (uint)index < (uint)expressions.Count ? expressions[index] : null;

    private static ExecutionExpression? SubqueryChild(ExecutionSubquery query, int index)
    {
        if (index < query.Bindings.Count)
            return query.Bindings[index];
        index -= query.Bindings.Count;
        if (query.Operand is not null)
        {
            if (index == 0)
                return query.Operand;
            index--;
        }
        if (index < query.CapturedScriptValues.Count)
            return query.CapturedScriptValues[index];
        index -= query.CapturedScriptValues.Count;
        return At(query.CapturedCteValues, index);
    }

    private static ExecutionExpression? CaseChild(ExecutionCaseWhen conditional, int index)
    {
        var branch = index / 2;
        if (branch < conditional.Branches.Count)
            return index % 2 == 0 ? conditional.Branches[branch].Condition : conditional.Branches[branch].Result;
        return index == conditional.Branches.Count * 2 ? conditional.ElseExpression : null;
    }
}
