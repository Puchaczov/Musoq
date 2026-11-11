using System.Linq;
using System.Numerics;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

/// <summary>Conservative range proof for evaluation in eager native source chunks.</summary>
internal static class SourceScalarTotality
{
    private readonly record struct Range(BigInteger Min, BigInteger Max)
    {
        public bool Contains(Range other) => Min <= other.Min && Max >= other.Max;
    }

    public static bool IsTotal(SourceScalarExpression expression)
    {
        if (!SourceScalarExpressionFacts.IsStable(expression))
            return false;
        return expression switch
        {
            SourceScalarLiteral or SourceScalarColumn => true,
            SourceScalarNullCheck check => IsTotal(check.Operand),
            SourceScalarCoalesce coalesce => coalesce.Expressions.All(IsTotal) && coalesce.Expressions.All(child =>
                Underlying(child.ResultType) == Underlying(coalesce.ResultType) ||
                NumericRange(coalesce.ResultType) is { } target && ValueRange(child) is { } value && target.Contains(value)),
            SourceScalarUnary { Operator: SourceScalarUnaryOperator.Not } unary =>
                Underlying(unary.Operand.ResultType) == typeof(bool) && IsTotal(unary.Operand),
            SourceScalarBinary { Operator: SourceScalarBinaryOperator.And or SourceScalarBinaryOperator.Or } binary =>
                Underlying(binary.Left.ResultType) == typeof(bool) && Underlying(binary.Right.ResultType) == typeof(bool) &&
                IsTotal(binary.Left) && IsTotal(binary.Right),
            SourceScalarBinary binary when binary.Operator >= SourceScalarBinaryOperator.Equal =>
                IsTotal(binary.Left) && IsTotal(binary.Right) &&
                NumericRange(binary.Left.ResultType) is not null && NumericRange(binary.Right.ResultType) is not null,
            _ => ValueRange(expression) is not null
        };
    }

    private static Range? ValueRange(SourceScalarExpression expression)
    {
        var limits = NumericRange(expression.ResultType);
        if (limits is null)
            return expression is SourceScalarCast cast && cast.ResultType == cast.Operand.ResultType && IsTotal(cast.Operand)
                ? NumericRange(cast.ResultType) : null;
        var range = expression switch
        {
            SourceScalarLiteral literal => LiteralRange(literal),
            SourceScalarColumn => limits,
            SourceScalarCast cast when IsTotal(cast.Operand) => ValueRange(cast.Operand),
            SourceScalarUnary { Operator: SourceScalarUnaryOperator.Negate } unary when ValueRange(unary.Operand) is { } value
                => new Range(-value.Max, -value.Min),
            SourceScalarBinary binary when ValueRange(binary.Left) is { } left && ValueRange(binary.Right) is { } right
                => BinaryRange(binary.Operator, left, right),
            SourceScalarCoalesce coalesce when coalesce.Expressions.Select(ValueRange).All(item => item.HasValue)
                => new Range(coalesce.Expressions.Select(ValueRange).Min(item => item!.Value.Min),
                    coalesce.Expressions.Select(ValueRange).Max(item => item!.Value.Max)),
            _ => null
        };
        return range is { } proven && limits.Value.Contains(proven) ? proven : null;
    }

    private static Range? BinaryRange(SourceScalarBinaryOperator operation, Range left, Range right)
    {
        if (operation == SourceScalarBinaryOperator.Add)
            return new(left.Min + right.Min, left.Max + right.Max);
        if (operation == SourceScalarBinaryOperator.Subtract)
            return new(left.Min - right.Max, left.Max - right.Min);
        if (operation == SourceScalarBinaryOperator.Multiply)
        {
            BigInteger[] corners = [left.Min * right.Min, left.Min * right.Max, left.Max * right.Min, left.Max * right.Max];
            return new(corners.Min(), corners.Max());
        }
        // Division, modulo and unchecked overflow retain Core exception timing.
        return null;
    }

    private static Range? LiteralRange(SourceScalarLiteral literal)
    {
        var number = literal.Value switch
        {
            sbyte value => (BigInteger?)value, byte value => value, short value => value, ushort value => value,
            int value => value, uint value => value, long value => value, ulong value => value,
            _ => null
        };
        return number is { } scalar ? new(scalar, scalar) : null;
    }

    private static Type Underlying(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    private static Range? NumericRange(Type type) => Type.GetTypeCode(Underlying(type)) switch
    {
        TypeCode.SByte => new(sbyte.MinValue, sbyte.MaxValue), TypeCode.Byte => new(byte.MinValue, byte.MaxValue),
        TypeCode.Int16 => new(short.MinValue, short.MaxValue), TypeCode.UInt16 => new(ushort.MinValue, ushort.MaxValue),
        TypeCode.Int32 => new(int.MinValue, int.MaxValue), TypeCode.UInt32 => new(uint.MinValue, uint.MaxValue),
        TypeCode.Int64 => new(long.MinValue, long.MaxValue), TypeCode.UInt64 => new(ulong.MinValue, ulong.MaxValue),
        TypeCode.Decimal => new(new BigInteger(decimal.MinValue), new BigInteger(decimal.MaxValue)),
        _ => null
    };
}
