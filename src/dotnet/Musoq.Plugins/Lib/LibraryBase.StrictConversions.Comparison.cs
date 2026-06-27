using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{

    /// <summary>
    ///     Attempts to convert a value to Int32 for comparison operations, allowing precision loss but validating range
    ///     constraints.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Int32 value if within valid range; otherwise, null.</returns>
    /// <remarks>
    ///     This method allows lossy conversions (e.g., 3.7 becomes 3) but rejects values outside the Int32 range.
    ///     Useful for comparison operations where approximate values are acceptable, such as:
    ///     - Comparing floating-point values to integers (e.g., 3.0 == 3)
    ///     - Range checks that tolerate fractional truncation
    /// </remarks>
    [BindableMethod(true)]
    public int? TryConvertToInt32Comparison(object? value)
    {
        return ComparisonConverter.TryConvertToInt32(value);
    }

    /// <summary>
    ///     Attempts to convert a value to Int64 for comparison operations, allowing precision loss but validating range
    ///     constraints.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Int64 value if within valid range; otherwise, null.</returns>
    /// <remarks>
    ///     This method allows lossy conversions (e.g., 3.7 becomes 3) but rejects values outside the Int64 range.
    ///     Useful for comparison operations where approximate values are acceptable, such as:
    ///     - Comparing floating-point values to long integers
    ///     - Range checks that tolerate fractional truncation
    /// </remarks>
    [BindableMethod(true)]
    public long? TryConvertToInt64Comparison(object? value)
    {
        return ComparisonConverter.TryConvertToInt64(value);
    }

    /// <summary>
    ///     Attempts to convert a value to Decimal for comparison operations, allowing precision loss but validating range
    ///     constraints.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Decimal value if within valid range; otherwise, null.</returns>
    /// <remarks>
    ///     This method allows lossy conversions but rejects NaN and Infinity values.
    ///     Decimal has a very large range, so most numeric values can be converted successfully.
    ///     Useful for comparison operations where high precision is needed but some loss is acceptable.
    /// </remarks>
    [BindableMethod(true)]
    public decimal? TryConvertToDecimalComparison(object? value)
    {
        return ComparisonConverter.TryConvertToDecimal(value);
    }
}
