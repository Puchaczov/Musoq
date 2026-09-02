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

    internal static Type CaseResultType(Type returnType, CaseWhenBranch[] branches, IrExpression? elseExpression) =>
        returnType == typeof(bool) &&
        (elseExpression is null || branches.Any(static branch => CanBeNull(branch.Result)) || CanBeNull(elseExpression))
            ? typeof(bool?)
            : returnType;
}
