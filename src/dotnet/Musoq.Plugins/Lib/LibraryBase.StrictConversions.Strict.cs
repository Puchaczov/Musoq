using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{

    /// <summary>
    ///     Attempts to convert a value to Int32 with strict validation, rejecting any conversions that would lose precision.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Int32 value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    ///     This method rejects conversions that would result in precision loss, such as:
    ///     - Floating-point values that cannot be exactly represented as Int32
    ///     - Values outside the Int32 range (int.MinValue to int.MaxValue)
    ///     - Strings that cannot be parsed as valid Int32 values
    /// </remarks>
    [BindableMethod(true)]
    public int? TryConvertToInt32Strict(object? value)
    {
        return StrictConverter.TryConvertToInt32(value);
    }

    /// <summary>
    ///     Attempts to convert a value to Int64 with strict validation, rejecting any conversions that would lose precision.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Int64 value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    ///     This method rejects conversions that would result in precision loss, such as:
    ///     - Floating-point values that cannot be exactly represented as Int64
    ///     - Values outside the Int64 range (long.MinValue to long.MaxValue)
    ///     - Strings that cannot be parsed as valid Int64 values
    /// </remarks>
    [BindableMethod(true)]
    public long? TryConvertToInt64Strict(object? value)
    {
        return StrictConverter.TryConvertToInt64(value);
    }

    /// <summary>
    ///     Attempts to convert a value to Decimal with strict validation, rejecting any conversions that would lose precision.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Decimal value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    ///     This method rejects conversions that would result in precision loss, such as:
    ///     - Floating-point values that cannot be exactly represented as Decimal
    ///     - NaN or Infinity values
    ///     - Strings that cannot be parsed as valid Decimal values
    ///     Decimal has a larger range and precision than Int32/Int64 for fractional values.
    /// </remarks>
    [BindableMethod(true)]
    public decimal? TryConvertToDecimalStrict(object? value)
    {
        return StrictConverter.TryConvertToDecimal(value);
    }
}
