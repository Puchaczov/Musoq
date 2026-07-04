using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Expressions;

internal static class IrExpressionTraversal
{
    public static bool IsKnownExpressionKind(IrExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return expression switch
        {
            ColumnRef or Literal or WildcardLiteral or ScriptParameterRef or ScriptVariableRef or RowPresence or
                AggregateRef or WindowFunctionRef or CteTableRef or BinaryOp or UnaryOp or ArrayAccess or
                IsNullCheck or InCheck or CollectionInCheck or PatternMatch or Between or CaseWhen or Coalesce or
                MethodCall or StrictCast => true,
            _ => false
        };
    }

    public static IEnumerable<IrExpression> Children(IrExpression? expression)
    {
        if (expression == null)
            yield break;

        switch (expression)
        {
            case BinaryOp binary:
                yield return binary.Left;
                yield return binary.Right;
                yield break;
            case UnaryOp unary:
                yield return unary.Operand;
                yield break;
            case ArrayAccess arrayAccess:
                yield return arrayAccess.Array;
                yield return arrayAccess.Index;
                yield break;
            case IsNullCheck isNull:
                yield return isNull.Expression;
                yield break;
            case InCheck inCheck:
                yield return inCheck.Expression;
                foreach (var value in inCheck.Values)
                    yield return value;
                yield break;
            case CollectionInCheck collectionInCheck:
                yield return collectionInCheck.Expression;
                yield return collectionInCheck.Collection;
                yield break;
            case PatternMatch patternMatch:
                yield return patternMatch.Expression;
                yield return patternMatch.Pattern;
                yield break;
            case Between between:
                yield return between.Expression;
                yield return between.Low;
                yield return between.High;
                yield break;
            case CaseWhen caseWhen:
                foreach (var branch in caseWhen.Branches)
                {
                    yield return branch.Condition;
                    yield return branch.Result;
                }

                if (caseWhen.ElseExpression != null)
                    yield return caseWhen.ElseExpression;
                yield break;
            case Coalesce coalesce:
                foreach (var part in coalesce.Expressions)
                    yield return part;
                yield break;
            case MethodCall methodCall:
                foreach (var argument in methodCall.Arguments)
                    yield return argument;
                yield break;
            case StrictCast strictCast:
                yield return strictCast.Expression;
                yield break;
        }
    }

    public static IEnumerable<IrExpression> SelfAndDescendants(IrExpression? expression)
    {
        if (expression == null)
            yield break;

        yield return expression;

        foreach (var child in Children(expression))
        foreach (var descendant in SelfAndDescendants(child))
            yield return descendant;
    }
}
