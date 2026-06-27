using System.Collections.Generic;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PredicateMovementPlanner
{
    private static ExpressionSafety CanMovePredicate(IrExpression predicate)
    {
        if (predicate.ReturnType != typeof(bool))
            return ExpressionSafety.Unsafe($"Predicate {IrExpressionPrinter.Print(predicate)} is not a typed boolean expression.");

        return CanMoveExpression(predicate);
    }

    private static ExpressionSafety CanMoveExpression(IrExpression expression)
    {
        switch (expression)
        {
            case Literal or WildcardLiteral or ColumnRef or ScriptParameterRef or ScriptVariableRef:
                return ExpressionSafety.Safe();
            case RowPresence:
                return ExpressionSafety.Unsafe("Predicate contains a row-presence marker.");
            case StrictCast strictCast:
                return CanMoveExpression(strictCast.Expression);
            case BinaryOp binary:
                return Combine(binary.Left, binary.Right);
            case UnaryOp unary:
                return CanMoveExpression(unary.Operand);
            case IsNullCheck isNull:
                return CanMoveExpression(isNull.Expression);
            case InCheck inCheck:
                return Combine([inCheck.Expression, .. inCheck.Values]);
            case CollectionInCheck collectionInCheck:
                return CanMoveExpression(collectionInCheck.Expression);
            case PatternMatch patternMatch:
                return Combine(patternMatch.Expression, patternMatch.Pattern);
            case Between between:
                return Combine([between.Expression, between.Low, between.High]);
            case CaseWhen caseWhen:
                return CanMoveCaseWhen(caseWhen);
            case Coalesce coalesce:
                return Combine(coalesce.Expressions);
            case ArrayAccess arrayAccess:
                return Combine(arrayAccess.Array, arrayAccess.Index);
            case MethodCall methodCall:
                return CanMoveMethodCall(methodCall);
            case AggregateRef:
                return ExpressionSafety.Unsafe("Predicate contains an aggregate reference.");
            case WindowFunctionRef:
                return ExpressionSafety.Unsafe("Predicate contains a window function reference.");
            case CteTableRef:
                return ExpressionSafety.Unsafe("Predicate contains a table/subquery reference.");
            default:
                return ExpressionSafety.Unsafe($"Predicate contains unsupported expression {expression.GetType().Name}.");
        }
    }

    private static ExpressionSafety CanMoveCaseWhen(CaseWhen caseWhen)
    {
        foreach (var branch in caseWhen.Branches)
        {
            var branchSafety = Combine(branch.Condition, branch.Result);
            if (!branchSafety.IsSafe)
                return branchSafety;
        }

        if (caseWhen.ElseExpression is null)
            return ExpressionSafety.Safe();

        return CanMoveExpression(caseWhen.ElseExpression);
    }

    private static ExpressionSafety CanMoveMethodCall(MethodCall methodCall)
    {
        return IrExpressionDeterminism.TryGetFirstBlockedReason(methodCall, out var reason, "Predicate")
            ? ExpressionSafety.Unsafe(reason)
            : ExpressionSafety.Safe();
    }

    private static ExpressionSafety Combine(IrExpression left, IrExpression right)
    {
        return Combine([left, right]);
    }

    private static ExpressionSafety Combine(IReadOnlyList<IrExpression> expressions)
    {
        foreach (var expression in expressions)
        {
            var safety = CanMoveExpression(expression);
            if (!safety.IsSafe)
                return safety;
        }

        return ExpressionSafety.Safe();
    }

}
