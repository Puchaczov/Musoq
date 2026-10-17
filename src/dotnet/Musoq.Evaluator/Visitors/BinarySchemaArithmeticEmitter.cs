namespace Musoq.Evaluator.Visitors;

internal readonly record struct BinarySchemaArithmetic(
    Type LeftType, Type RightType, Type ResultType, string Left, string Right, string Operation);

internal static class BinarySchemaArithmeticEmitter
{
    internal static string Emit(BinarySchemaArithmetic arithmetic)
    {
        var left = arithmetic.Left;
        var right = arithmetic.Right;
        if (arithmetic.LeftType != arithmetic.RightType &&
            BinaryOperatorTypeRules.IsNumericType(BinarySchemaExpressionTypes.Unwrap(arithmetic.LeftType)) &&
            BinaryOperatorTypeRules.IsNumericType(BinarySchemaExpressionTypes.Unwrap(arithmetic.RightType)))
        {
            var name = BinarySchemaClrTypeNames.Map(BinarySchemaExpressionTypes.Unwrap(arithmetic.ResultType)) +
                (BinarySchemaExpressionTypes.IsNullable(arithmetic.ResultType) ? "?" : string.Empty);
            left = $"(({name})({left}))";
            right = $"(({name})({right}))";
        }
        return $"({left} {arithmetic.Operation} {right})";
    }
}
