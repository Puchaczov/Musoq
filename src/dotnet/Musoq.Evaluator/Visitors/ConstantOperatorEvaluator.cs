using System.Globalization;

namespace Musoq.Evaluator.Visitors;

internal static class ConstantOperatorEvaluator
{
    public static object EvaluatePreservingNumericType(
        ConstantOperatorKind operation,
        object left,
        object right)
    {
        return operation switch
        {
            ConstantOperatorKind.Add => Add(left, right),
            ConstantOperatorKind.Subtract => Subtract(left, right),
            ConstantOperatorKind.Multiply => Multiply(left, right),
            ConstantOperatorKind.Divide => Divide(left, right),
            ConstantOperatorKind.Modulo => Modulo(left, right),
            ConstantOperatorKind.BitwiseAnd => (long)left & (long)right,
            ConstantOperatorKind.BitwiseOr => (long)left | (long)right,
            ConstantOperatorKind.BitwiseXor => (long)left ^ (long)right,
            ConstantOperatorKind.LeftShift => (long)left << (int)(long)right,
            ConstantOperatorKind.RightShift => (long)left >> (int)(long)right,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }

    public static decimal EvaluateDecimalArithmetic(
        ConstantOperatorKind operation,
        decimal left,
        decimal right)
    {
        return operation switch
        {
            ConstantOperatorKind.Add => left + right,
            ConstantOperatorKind.Subtract => left - right,
            ConstantOperatorKind.Multiply => left * right,
            ConstantOperatorKind.Divide => left / right,
            ConstantOperatorKind.Modulo => left % right,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }

    public static long EvaluateIntegralBitwise(
        ConstantOperatorKind operation,
        long left,
        long right)
    {
        return operation switch
        {
            ConstantOperatorKind.BitwiseAnd => left & right,
            ConstantOperatorKind.BitwiseOr => left | right,
            ConstantOperatorKind.BitwiseXor => left ^ right,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }

    public static long EvaluateIntegralShift(
        ConstantOperatorKind operation,
        long left,
        int right)
    {
        return operation switch
        {
            ConstantOperatorKind.LeftShift => left << right,
            ConstantOperatorKind.RightShift => left >> right,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }

    public static int CompareValues(object? left, object? right)
    {
        if (left == null && right == null)
            return 0;

        if (left == null)
            return -1;

        if (right == null)
            return 1;

        if (IsNumeric(left) && IsNumeric(right))
        {
            var leftValue = Convert.ToDecimal(left, CultureInfo.InvariantCulture);
            var rightValue = Convert.ToDecimal(right, CultureInfo.InvariantCulture);
            return leftValue.CompareTo(rightValue);
        }

        if (left is IComparable comparable && left.GetType().IsInstanceOfType(right))
            return comparable.CompareTo(right);

        return string.CompareOrdinal(left.ToString(), right.ToString());
    }

    public static bool IsZero(object value)
    {
        return IsNumeric(value) && Convert.ToDecimal(value, CultureInfo.InvariantCulture) == 0m;
    }

    public static bool IsNumeric(object value)
    {
        return value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }

    public static bool IsInteger(object value)
    {
        return value is byte or sbyte or short or ushort or int or uint or long or ulong;
    }

    private static object Add(object left, object right)
    {
        return left switch
        {
            decimal l => checked(l + (decimal)right),
            double l => l + (double)right,
            float l => l + (float)right,
            ulong l => checked(l + (ulong)right),
            long l => checked(l + (long)right),
            uint l => checked(l + (uint)right),
            int l => checked(l + (int)right),
            ushort l => checked(l + Convert.ToInt32(right, CultureInfo.InvariantCulture)),
            short l => checked(l + Convert.ToInt32(right, CultureInfo.InvariantCulture)),
            byte l => checked(l + Convert.ToInt32(right, CultureInfo.InvariantCulture)),
            sbyte l => checked(l + Convert.ToInt32(right, CultureInfo.InvariantCulture)),
            _ => throw new InvalidCastException($"Cannot add {left.GetType().Name}")
        };
    }

    private static object Subtract(object left, object right)
    {
        return left switch
        {
            decimal l => checked(l - (decimal)right),
            double l => l - (double)right,
            float l => l - (float)right,
            ulong l => checked(l - (ulong)right),
            long l => checked(l - (long)right),
            uint l => checked(l - (uint)right),
            int l => checked(l - (int)right),
            ushort l => checked(l - Convert.ToInt32(right, CultureInfo.InvariantCulture)),
            short l => checked(l - Convert.ToInt32(right, CultureInfo.InvariantCulture)),
            byte l => checked(l - Convert.ToInt32(right, CultureInfo.InvariantCulture)),
            sbyte l => checked(l - Convert.ToInt32(right, CultureInfo.InvariantCulture)),
            _ => throw new InvalidCastException($"Cannot subtract {left.GetType().Name}")
        };
    }

    private static object Multiply(object left, object right)
    {
        return left switch
        {
            decimal l => checked(l * (decimal)right),
            double l => l * (double)right,
            float l => l * (float)right,
            ulong l => checked(l * (ulong)right),
            long l => checked(l * (long)right),
            uint l => checked(l * (uint)right),
            int l => checked(l * (int)right),
            ushort l => checked(l * Convert.ToInt32(right, CultureInfo.InvariantCulture)),
            short l => checked(l * Convert.ToInt32(right, CultureInfo.InvariantCulture)),
            byte l => checked(l * Convert.ToInt32(right, CultureInfo.InvariantCulture)),
            sbyte l => checked(l * Convert.ToInt32(right, CultureInfo.InvariantCulture)),
            _ => throw new InvalidCastException($"Cannot multiply {left.GetType().Name}")
        };
    }

    private static object Divide(object left, object right)
    {
        return left switch
        {
            decimal l => l / (decimal)right,
            double l => l / (double)right,
            float l => l / (float)right,
            ulong l => l / (ulong)right,
            long l => l / (long)right,
            uint l => l / (uint)right,
            int l => l / (int)right,
            ushort l => l / Convert.ToInt32(right, CultureInfo.InvariantCulture),
            short l => l / Convert.ToInt32(right, CultureInfo.InvariantCulture),
            byte l => l / Convert.ToInt32(right, CultureInfo.InvariantCulture),
            sbyte l => l / Convert.ToInt32(right, CultureInfo.InvariantCulture),
            _ => throw new InvalidCastException($"Cannot divide {left.GetType().Name}")
        };
    }

    private static object Modulo(object left, object right)
    {
        return left switch
        {
            decimal l => l % (decimal)right,
            double l => l % (double)right,
            float l => l % (float)right,
            ulong l => l % (ulong)right,
            long l => l % (long)right,
            uint l => l % (uint)right,
            int l => l % (int)right,
            ushort l => l % Convert.ToInt32(right, CultureInfo.InvariantCulture),
            short l => l % Convert.ToInt32(right, CultureInfo.InvariantCulture),
            byte l => l % Convert.ToInt32(right, CultureInfo.InvariantCulture),
            sbyte l => l % Convert.ToInt32(right, CultureInfo.InvariantCulture),
            _ => throw new InvalidCastException($"Cannot modulo {left.GetType().Name}")
        };
    }
}
