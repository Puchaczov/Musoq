using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal static class AsOfKeyTypes
{
    public static Type ComparisonType(Type left, Type right)
    {
        var common = ValueType(left, right);
        if (common == typeof(object) || common is NullNode.NullType)
            throw new NotSupportedException("ASOF comparison keys require a concrete ordered type.");
        return Nullable.GetUnderlyingType(left) != null || Nullable.GetUnderlyingType(right) != null
            ? PrimitiveTypeResolver.MakeNullable(common)
            : common;
    }

    public static Type ValueType(Type left, Type right)
    {
        left = Nullable.GetUnderlyingType(left) ?? left;
        right = Nullable.GetUnderlyingType(right) ?? right;
        if (left == right)
            return left;

        if (!BinaryOperatorTypeRules.IsNumericType(left) || !BinaryOperatorTypeRules.IsNumericType(right))
            throw UnsupportedShape.Of($"ASOF key pair '{left.Name}', '{right.Name}'", "ASOF comparison binding");

        // Mixed signed and unsigned keys need a domain containing both operands, including negative probes.
        if (BinaryOperatorTypeRules.IsIntegralType(left) && BinaryOperatorTypeRules.IsIntegralType(right))
            return left == typeof(ulong) || right == typeof(ulong) ? typeof(decimal) : typeof(long);

        return left == typeof(decimal) || right == typeof(decimal)
            ? typeof(decimal)
            : NodeHelpers.GetReturnTypeMap(left, right);
    }
}
