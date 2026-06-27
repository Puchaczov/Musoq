using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{

    /// <summary>
    ///     Smart numeric conversion that automatically selects the appropriate target type (Int32, Int64, or Decimal)
    ///     based on the actual value. Tries Int32 first, then Int64, then Decimal, returning the first successful conversion
    ///     as Decimal.
    /// </summary>
    /// <param name="value">The value to convert. Must be a boxed numeric type.</param>
    /// <returns>The converted value as Decimal; null if conversion fails.</returns>
    /// <remarks>
    ///     This method is used for arithmetic operations on System.Object columns.
    ///     It automatically handles integers, longs, floats, doubles, and decimals, including fractional values.
    ///     Returns Decimal to support all numeric types and enable compile-time operator usage.
    ///     Rejects strings, booleans, and other non-numeric types.
    /// </remarks>
    [BindableMethod(true)]
    public decimal? TryConvertNumericOnly(object? value)
    {
        return NumericOnlyConverter.TryConvertNumericOnly(value);
    }

    /// <summary>
    ///     Attempts to convert a value to Int32, rejecting strings and accepting only boxed numeric types.
    /// </summary>
    /// <param name="value">The value to convert. Must be a boxed numeric type.</param>
    /// <returns>The converted Int32 value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    ///     This method is used for arithmetic operations on System.Object columns.
    ///     It rejects string values and only accepts boxed numeric types (int, long, double, etc.).
    /// </remarks>
    [BindableMethod(true)]
    public int? TryConvertToInt32NumericOnly(object? value)
    {
        return NumericOnlyConverter.TryConvertToInt32(value);
    }

    /// <summary>
    ///     Attempts to convert a value to Int64, rejecting strings and accepting only boxed numeric types.
    /// </summary>
    /// <param name="value">The value to convert. Must be a boxed numeric type.</param>
    /// <returns>The converted Int64 value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    ///     This method is used for arithmetic operations on System.Object columns.
    ///     It rejects string values and only accepts boxed numeric types (int, long, double, etc.).
    /// </remarks>
    [BindableMethod(true)]
    public long? TryConvertToInt64NumericOnly(object? value)
    {
        return NumericOnlyConverter.TryConvertToInt64(value);
    }

    /// <summary>
    ///     Attempts to convert a value to Decimal, rejecting strings and accepting only boxed numeric types.
    /// </summary>
    /// <param name="value">The value to convert. Must be a boxed numeric type.</param>
    /// <returns>The converted Decimal value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    ///     This method is used for arithmetic operations on System.Object columns.
    ///     It rejects string values and only accepts boxed numeric types (int, long, double, etc.).
    /// </remarks>
    [BindableMethod(true)]
    public decimal? TryConvertToDecimalNumericOnly(object? value)
    {
        return NumericOnlyConverter.TryConvertToDecimal(value);
    }
}
