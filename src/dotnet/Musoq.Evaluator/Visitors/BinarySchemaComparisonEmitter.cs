namespace Musoq.Evaluator.Visitors;

internal readonly record struct BinarySchemaComparison(
    Type LeftType, Type RightType, string Left, string Right, string Operation, bool MayBeNull);

internal static class BinarySchemaComparisonEmitter
{
    internal static string Emit(BinarySchemaComparison comparison)
    {
        var left = comparison.Left;
        var right = comparison.Right;
        if (comparison.LeftType == typeof(string) && comparison.RightType == typeof(string) &&
            comparison.Operation is not ("==" or "!="))
            return $"(string.CompareOrdinal({left}, {right}) {comparison.Operation} 0)";
        if (comparison.LeftType != comparison.RightType &&
            BinaryOperatorTypeRules.IsNumericType(comparison.LeftType) && BinaryOperatorTypeRules.IsNumericType(comparison.RightType))
        {
            var common = BinarySchemaClrTypeNames.Map(BinaryOperatorTypeRules.GetWiderNumericType(comparison.LeftType, comparison.RightType));
            var name = common + (comparison.MayBeNull ? "?" : string.Empty);
            left = $"(({name})({left}))";
            right = $"(({name})({right}))";
        }
        return $"({left} {comparison.Operation} {right})";
    }
}
