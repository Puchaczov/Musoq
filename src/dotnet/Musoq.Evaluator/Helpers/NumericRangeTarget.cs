using System.Numerics;
using System.Runtime.CompilerServices;

namespace Musoq.Evaluator.Helpers;

// Fractional and out-of-domain boundaries can lie strictly between representable keys.
// Keep the side of the rounded boundary so an inclusive search handles equality correctly.
internal readonly record struct NumericRangeTarget<TNumber>(TNumber? Value, int RemainderSign)
    where TNumber : struct, INumber<TNumber>
{
    internal static NumericRangeTarget<TNumber> Shift(TNumber? current, long offset)
    {
        if (!current.HasValue || offset == 0 || !TNumber.IsFinite(current.Value))
            return new(current, 0);

        if (typeof(TNumber) == typeof(double) || typeof(TNumber) == typeof(float))
            return ShiftFractional(double.CreateChecked(current.Value), (double)offset);
        if (typeof(TNumber) == typeof(decimal))
            return ShiftDecimal(decimal.CreateChecked(current.Value), offset);

        var shifted = Int128.CreateChecked(current.Value) + offset;
        var key = TNumber.CreateSaturating(shifted);
        return new(key, shifted.CompareTo(Int128.CreateChecked(key)));
    }

    private static NumericRangeTarget<TNumber> ShiftDecimal(decimal value, long offset)
    {
        if (offset > 0 && value > decimal.MaxValue - offset)
            return new(TNumber.CreateChecked(decimal.MaxValue), 1);
        if (offset < 0 && value < decimal.MinValue - offset)
            return new(TNumber.CreateChecked(decimal.MinValue), -1);

        var sum = value + offset;
        // Adding a whole offset can only round when decimal has to reduce the scale.
        var remainderSign = sum.Scale >= value.Scale ? 0 : decimal.Sign(SumRemainder(value, (decimal)offset, sum));
        return new(TNumber.CreateChecked(sum), remainderSign);
    }

    private static NumericRangeTarget<TNumber> ShiftFractional(double value, double offset)
    {
        var sum = value + offset;
        var key = TNumber.CreateSaturating(sum);
        var rounding = sum.CompareTo(double.CreateChecked(key));
        if (rounding != 0)
            return new(key, rounding);
        return new(key, double.Sign(SumRemainder(value, offset, sum)));
    }

    // Recover the low part of a rounded sum by subtracting the larger operand first (FastTwoSum).
    private static T SumRemainder<T>(T value, T offset, T sum)
        where T : IFloatingPoint<T> => T.Abs(value) >= T.Abs(offset) ? offset - (sum - value) : value - (sum - offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int Compare(TNumber? candidate, bool descending, bool nullsFirst)
    {
        if (!candidate.HasValue)
            return !Value.HasValue ? 0
                : nullsFirst ? -1
                : 1;
        if (!Value.HasValue)
            return nullsFirst ? 1 : -1;

        var comparison = candidate.GetValueOrDefault().CompareTo(Value.GetValueOrDefault());
        if (comparison == 0)
            comparison = -RemainderSign;
        return descending ? -comparison : comparison;
    }
}
