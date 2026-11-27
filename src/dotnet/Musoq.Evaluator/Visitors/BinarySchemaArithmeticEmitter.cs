namespace Musoq.Evaluator.Visitors;

internal readonly record struct BinarySchemaArithmetic(
    Type LeftType,
    Type RightType,
    Type ResultType,
    string Left,
    string Right,
    string Operation
);

internal static class BinarySchemaArithmeticEmitter
{
    internal static string Emit(BinarySchemaArithmetic arithmetic)
    {
        var left = arithmetic.Left;
        var right = arithmetic.Right;
        if (arithmetic.Operation is "<<" or ">>")
        {
            var countType = BinarySchemaExpressionTypes.Unwrap(arithmetic.RightType);
            if (countType == typeof(uint) || countType == typeof(long) || countType == typeof(ulong))
            {
                var cast = BinarySchemaExpressionTypes.IsNullable(arithmetic.RightType) ? "int?" : "int";
                right = $"unchecked(({cast})({right}))";
            }
        }
        else if (
            arithmetic.LeftType != arithmetic.RightType
            && BinaryOperatorTypeRules.IsNumericType(BinarySchemaExpressionTypes.Unwrap(arithmetic.LeftType))
            && BinaryOperatorTypeRules.IsNumericType(BinarySchemaExpressionTypes.Unwrap(arithmetic.RightType))
        )
        {
            var resultType = BinarySchemaExpressionTypes.Unwrap(arithmetic.ResultType);
            var name =
                BinarySchemaClrTypeNames.Map(resultType)
                + (BinarySchemaExpressionTypes.IsNullable(arithmetic.ResultType) ? "?" : string.Empty);
            left = CastOperand(left, arithmetic.LeftType, resultType, name);
            right = CastOperand(right, arithmetic.RightType, resultType, name);
        }
        return $"({left} {arithmetic.Operation} {right})";
    }

    private static string CastOperand(string operand, Type sourceType, Type resultType, string typeName)
    {
        var cast = $"(({typeName})({operand}))";
        var source = Type.GetTypeCode(BinarySchemaExpressionTypes.Unwrap(sourceType));
        return
            (resultType == typeof(uint) || resultType == typeof(ulong))
            && source is TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64
            ? $"unchecked({cast})"
            : cast;
    }
}
