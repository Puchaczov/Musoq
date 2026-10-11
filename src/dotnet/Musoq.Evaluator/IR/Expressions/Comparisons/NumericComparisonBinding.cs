using Musoq.Plugins;
using Musoq.Evaluator.IR.Analysis;

namespace Musoq.Evaluator.IR.Expressions;

internal static class NumericComparisonBinding
{
    internal static (IrExpression Left, IrExpression Right) Normalize(
        BinaryOpKind kind, IrExpression left, IrExpression right)
    {
        if (!IrExpressionNullSemantics.IsSqlComparison(kind))
            return (left, right);
        var leftType = Nullable.GetUnderlyingType(left.ReturnType) ?? left.ReturnType;
        var rightType = Nullable.GetUnderlyingType(right.ReturnType) ?? right.ReturnType;
        var mixedIntegralWidths = leftType == typeof(ulong) && IsSigned(rightType) ||
                                  rightType == typeof(ulong) && IsSigned(leftType);
        var mixedDecimalFloating = leftType == typeof(decimal) && IsFloating(rightType) ||
                                   rightType == typeof(decimal) && IsFloating(leftType);
        if (!mixedIntegralWidths && !mixedDecimalFloating)
            return (left, right);
        return (ToDecimal(left, leftType), ToDecimal(right, rightType));
    }

    private static bool IsFloating(Type type)
        => type == typeof(float) || type == typeof(double);

    private static bool IsSigned(Type type)
        => type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long);

    private static IrExpression ToDecimal(IrExpression value, Type type)
    {
        var parameterType = typeof(Nullable<>).MakeGenericType(type);
        var method = typeof(LibraryBase).GetMethod(nameof(LibraryBase.ToDecimal), [parameterType])
            ?? throw new InvalidOperationException($"No concrete decimal conversion exists for {type}.");
        return new MethodCall(method, [value], null, typeof(decimal?));
    }
}
