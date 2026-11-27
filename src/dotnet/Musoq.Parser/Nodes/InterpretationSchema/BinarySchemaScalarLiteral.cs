using System.Globalization;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>Numeric literal values and target domains used by binary schema constraints and switch labels.</summary>
public static class BinarySchemaScalarLiteral
{
    public static bool TryGetNumber(Node node, out decimal value)
    {
        if (
            node is HyphenNode hyphen
            && TryGetNumber(hyphen.Left, out var left)
            && TryGetNumber(hyphen.Right, out var right)
        )
        {
            value = left - right;
            return true;
        }

        if (
            node is ConstantValueNode constant
            && constant.ObjValue
                is byte
                    or sbyte
                    or short
                    or ushort
                    or int
                    or uint
                    or long
                    or ulong
                    or float
                    or double
                    or decimal
        )
        {
            try
            {
                value = Convert.ToDecimal(constant.ObjValue, CultureInfo.InvariantCulture);
                return true;
            }
            catch (OverflowException) when (constant.ObjValue is double or float)
            {
                // NaN and infinity are not valid integer/byte validation literals.
            }
        }

        value = default;
        return false;
    }

    public static bool TryGetUnsignedInteger(Node node, out ulong value)
    {
        if (
            (node is HexIntegerNode or BinaryIntegerNode or OctalIntegerNode)
            && node is ConstantValueNode { ObjValue: long signedValue }
            && signedValue < 0
        )
        {
            value = unchecked((ulong)signedValue);
            return true;
        }

        if (
            !TryGetNumber(node, out var numeric)
            || numeric < 0
            || numeric > ulong.MaxValue
            || decimal.Truncate(numeric) != numeric
        )
        {
            value = default;
            return false;
        }

        value = Convert.ToUInt64(numeric, CultureInfo.InvariantCulture);
        return true;
    }

    public static bool FitsIntegralType(Type? type, decimal value) =>
        decimal.Truncate(value) == value
        && Type.GetTypeCode(type) switch
        {
            TypeCode.Byte => value is >= byte.MinValue and <= byte.MaxValue,
            TypeCode.SByte => value is >= sbyte.MinValue and <= sbyte.MaxValue,
            TypeCode.Int16 => value is >= short.MinValue and <= short.MaxValue,
            TypeCode.UInt16 => value is >= ushort.MinValue and <= ushort.MaxValue,
            TypeCode.Int32 => value is >= int.MinValue and <= int.MaxValue,
            TypeCode.UInt32 => value is >= uint.MinValue and <= uint.MaxValue,
            TypeCode.Int64 => value is >= long.MinValue and <= long.MaxValue,
            TypeCode.UInt64 => value is >= ulong.MinValue and <= ulong.MaxValue,
            _ => false,
        };

    public static bool IsCompatibleSwitchLabel(Type? selector, Node label)
    {
        var type = selector is null ? null : Nullable.GetUnderlyingType(selector) ?? selector;
        if (type == typeof(bool))
            return label is BooleanNode;
        if (type == typeof(string))
            return label is StringNode or WordNode;
        return TryGetNumber(label, out var value)
            && (
                type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal)
                || FitsIntegralType(type, value)
            );
    }
}
