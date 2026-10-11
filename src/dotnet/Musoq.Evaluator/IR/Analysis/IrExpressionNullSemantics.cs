using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Analysis;

internal static class IrExpressionNullSemantics
{
    internal static bool IsBoolean(Type type) =>
        type == typeof(bool) || Nullable.GetUnderlyingType(type) == typeof(bool);

    internal static bool IsSqlComparison(BinaryOpKind kind) => kind is
        BinaryOpKind.Equal or BinaryOpKind.NotEqual or BinaryOpKind.GreaterThan or
        BinaryOpKind.LessThan or BinaryOpKind.GreaterOrEqual or BinaryOpKind.LessOrEqual;

    internal static bool CanBeNull(IrExpression expression) =>
        expression is Literal { Value: null } || !expression.ReturnType.IsValueType ||
        Nullable.GetUnderlyingType(expression.ReturnType) != null;

    internal static bool IsNullableBoolean(IrExpression expression) =>
        expression is Literal { Value: null } || Nullable.GetUnderlyingType(expression.ReturnType) == typeof(bool) ||
        expression switch
        {
            BinaryOp { Kind: BinaryOpKind.And or BinaryOpKind.Or } binary => IsNullableBoolean(binary.Left) || IsNullableBoolean(binary.Right),
            BinaryOp { UsesSqlNullSemantics: true } binary => CanBeNull(binary.Left) || CanBeNull(binary.Right),
            UnaryOp { Kind: UnaryOpKind.Not } unary => IsNullableBoolean(unary.Operand),
            Between between => CanBeNull(between.Expression) || CanBeNull(between.Low) || CanBeNull(between.High),
            _ => false
        };

    internal static Type? NullableBooleanResult(BinaryOpKind kind, IrExpression left, IrExpression right) =>
        (kind is BinaryOpKind.And or BinaryOpKind.Or && (IsNullableBoolean(left) || IsNullableBoolean(right))) ||
        (IsSqlComparison(kind) && (CanBeNull(left) || CanBeNull(right))) ? typeof(bool?) : null;

    internal static Type BinaryResultType(BinaryOpKind kind, IrExpression left, IrExpression right, Type declaredType)
    {
        if (NullableBooleanResult(kind, left, right) is { } booleanType)
            return booleanType;
        if (kind is BinaryOpKind.Add or BinaryOpKind.Subtract or BinaryOpKind.Multiply or BinaryOpKind.Divide or BinaryOpKind.Modulo &&
            Nullable.GetUnderlyingType(declaredType) is null &&
            Musoq.Evaluator.Visitors.BinaryOperatorTypeRules.IsNumericType(declaredType) &&
            (Nullable.GetUnderlyingType(left.ReturnType) != null || Nullable.GetUnderlyingType(right.ReturnType) != null))
            return typeof(Nullable<>).MakeGenericType(declaredType);
        return declaredType;
    }

    internal static Type CaseResultType(Type returnType, CaseWhenBranch[] branches, IrExpression? elseExpression)
    {
        if (!returnType.IsValueType || Nullable.GetUnderlyingType(returnType) != null)
            return returnType;
        foreach (var branch in branches)
        {
            if (branch.Condition is Literal { Value: false })
                continue;
            if (CanBeNull(branch.Result))
                return typeof(Nullable<>).MakeGenericType(returnType);
            if (branch.Condition is Literal { Value: true })
                return returnType;
        }
        return elseExpression is null || CanBeNull(elseExpression)
            ? typeof(Nullable<>).MakeGenericType(returnType)
            : returnType;
    }
}
