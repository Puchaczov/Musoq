using Musoq.Evaluator.IR.Analysis;

namespace Musoq.Evaluator.IR.Expressions;

internal static class ComparisonOperandBinding
{
    internal static (IrExpression Left, IrExpression Right) Normalize(
        BinaryOpKind kind,
        IrExpression left,
        IrExpression right
    )
    {
        var nullSafe = kind is BinaryOpKind.IsDistinctFrom or BinaryOpKind.IsNotDistinctFrom;
        if (!nullSafe && !IrExpressionNullSemantics.IsSqlComparison(kind))
            return (left, right);
        var leftType = Nullable.GetUnderlyingType(left.ReturnType) ?? left.ReturnType;
        var rightType = Nullable.GetUnderlyingType(right.ReturnType) ?? right.ReturnType;
        if (nullSafe || kind is BinaryOpKind.Equal or BinaryOpKind.NotEqual)
        {
            if (leftType == typeof(char) && rightType == typeof(string))
                return BindCharacterEquality(left, right);
            if (rightType == typeof(char) && leftType == typeof(string))
            {
                var (character, text) = BindCharacterEquality(right, left);
                return (text, character);
            }
        }
        var mixedIntegralWidths =
            leftType == typeof(ulong) && IsSigned(rightType) || rightType == typeof(ulong) && IsSigned(leftType);
        var mixedDecimalFloating = !nullSafe && DecimalFloatingType(leftType, rightType) is not null;
        if (!mixedIntegralWidths && !mixedDecimalFloating)
            return (left, right);
        return (ToDecimal(left, leftType), ToDecimal(right, rightType));
    }

    private static (IrExpression Character, IrExpression Text) BindCharacterEquality(
        IrExpression character,
        IrExpression text
    ) =>
        text is Literal { Value: string { Length: 1 } unit }
            ? (character, new Literal(unit[0], typeof(char)))
            : (new StrictCast(character, nameof(String), typeof(string)), text);

    private static bool IsFloating(Type type) => type == typeof(float) || type == typeof(double);

    internal static Type? DecimalFloatingType(Type left, Type right)
    {
        left = Nullable.GetUnderlyingType(left) ?? left;
        right = Nullable.GetUnderlyingType(right) ?? right;
        if (left == typeof(decimal) && IsFloating(right))
            return right;
        return right == typeof(decimal) && IsFloating(left) ? left : null;
    }

    private static bool IsSigned(Type type) =>
        type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long);

    private static IrExpression ToDecimal(IrExpression value, Type type)
    {
        if (type == typeof(float))
            return new MethodCall(
                ((Func<float?, decimal?>)Operators.ToComparisonDecimal<float>).Method,
                [value],
                null,
                typeof(decimal?)
            );
        if (type == typeof(double))
            return new MethodCall(
                ((Func<double?, decimal?>)Operators.ToComparisonDecimal<double>).Method,
                [value],
                null,
                typeof(decimal?)
            );
        return type == typeof(decimal) ? value : new StrictCast(value, nameof(Decimal), typeof(decimal?));
    }
}
