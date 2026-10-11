using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PredicateMovementPlanner
{
    private static ExpressionSafety CanMovePredicate(IrExpression predicate)
    {
        if (!Musoq.Evaluator.IR.SourcePlanning.SourcePredicateGraphValidator.IsWithinBudget(predicate, System.Threading.CancellationToken.None))
            return ExpressionSafety.Unsafe("Predicate exceeds the transfer graph budget.");
        if (!IrExpressionNullSemantics.IsBoolean(predicate.ReturnType))
            return ExpressionSafety.Unsafe($"Predicate {IrExpressionPrinter.Print(predicate)} is not a typed boolean expression.");

        if (!ExpressionStabilityAnalyzer.IsStable(predicate))
            return ExpressionSafety.Unsafe("Predicate contains volatile or unstable expressions.");
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
                return IsTotalCast(strictCast)
                    ? CanMoveExpression(strictCast.Expression)
                    : ExpressionSafety.Unsafe("Predicate contains a potentially throwing conversion.");
            case BinaryOp binary:
                if (binary.Kind is not (BinaryOpKind.And or BinaryOpKind.Or) &&
                    (!IsTotalScalar(binary.Left) || !IsTotalScalar(binary.Right)))
                    return ExpressionSafety.Unsafe("Predicate contains an operation on an unproven scalar type.");
                if (binary.Kind is BinaryOpKind.Divide or BinaryOpKind.Modulo ||
                    binary.Kind is BinaryOpKind.Add or BinaryOpKind.Subtract or BinaryOpKind.Multiply &&
                    (Nullable.GetUnderlyingType(binary.ReturnType) ?? binary.ReturnType) == typeof(decimal))
                    return ExpressionSafety.Unsafe("Predicate contains potentially failing arithmetic.");
                return Combine(binary.Left, binary.Right);
            case UnaryOp unary:
                return CanMoveExpression(unary.Operand);
            case IsNullCheck isNull:
                return CanMoveExpression(isNull.Expression);
            case InCheck inCheck:
                if (!IsTotalScalar(inCheck.Expression) || inCheck.Values.Any(value => !IsTotalScalar(value)))
                    return ExpressionSafety.Unsafe("Membership contains an unproven scalar type.");
                return Combine([inCheck.Expression, .. inCheck.Values]);
            case CollectionInCheck collectionInCheck:
                return Combine(collectionInCheck.Expression, collectionInCheck.Collection);
            case PatternMatch patternMatch:
                return patternMatch.Kind == PatternKind.Like && patternMatch.Pattern is Literal { Value: string }
                    ? Combine(patternMatch.Expression, patternMatch.Pattern)
                    : ExpressionSafety.Unsafe("Predicate contains an unproven pattern expression.");
            case Between between:
                return Combine([between.Expression, between.Low, between.High]);
            case CaseWhen caseWhen:
                return CanMoveCaseWhen(caseWhen);
            case Coalesce coalesce:
                return Combine(coalesce.Expressions);
            case ArrayAccess arrayAccess:
                return ExpressionSafety.Unsafe("Predicate contains a potentially failing array access.");
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
        if (IrExpressionDeterminism.TryGetFirstBlockedReason(methodCall, out var reason, "Predicate"))
            return ExpressionSafety.Unsafe(reason);
        if (methodCall.EnumIntrinsic is EnumIntrinsicKind.HasAnyFlags or EnumIntrinsicKind.HasAllFlags &&
            methodCall.OperandEnumType is { IsFlags: true } && methodCall.EnumMask != null)
            return Combine(methodCall.Arguments);
        if (methodCall.Method.DeclaringType != typeof(Musoq.Plugins.LibraryBase))
            return ExpressionSafety.Unsafe("Predicate contains a method without a totality proof.");
        var isTotal = methodCall.Arguments.Count == 1 && methodCall.Method.Name is "ToUpper" or "ToLower" ||
                      methodCall.Arguments.Count == 2 && methodCall.Method.Name is "Contains" or "StartsWith" or "EndsWith" or "IndexOf" or "LastIndexOf";
        if (!isTotal)
            return ExpressionSafety.Unsafe("Predicate contains a method without a totality proof.");
        return Combine(methodCall.Arguments);
    }

    internal static bool IsTransferSafe(IrExpression predicate) => CanMovePredicate(predicate).IsSafe;

    private static bool IsTotalScalar(IrExpression expression) =>
        expression is Literal { Value: null } || IsTotalScalarType(expression.ReturnType);

    private static bool IsTotalScalarType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsEnum || type == typeof(Guid) || type == typeof(DateTimeOffset) ||
               Type.GetTypeCode(type) is not (TypeCode.Object or TypeCode.Empty or TypeCode.DBNull);
    }

    private static bool IsTotalCast(StrictCast cast)
    {
        var source = Nullable.GetUnderlyingType(cast.Expression.ReturnType) ?? cast.Expression.ReturnType;
        var target = Nullable.GetUnderlyingType(cast.ReturnType) ?? cast.ReturnType;
        if (Nullable.GetUnderlyingType(cast.Expression.ReturnType) != null && Nullable.GetUnderlyingType(cast.ReturnType) == null)
            return false;
        return source == target || source == typeof(int) &&
            (target == typeof(long) || target == typeof(decimal) || target == typeof(double));
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
