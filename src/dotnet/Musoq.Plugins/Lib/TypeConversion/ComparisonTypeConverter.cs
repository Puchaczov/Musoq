namespace Musoq.Plugins.Lib.TypeConversion;

/// <summary>
///     Type converter that allows precision loss but validates range constraints.
///     Used for comparison operations (&gt;, &lt;, &gt;=, &lt;=) where approximate equality is acceptable.
/// </summary>
internal class ComparisonTypeConverter
{
    public int? TryConvertToInt32(object? value)
    {
        return TypeConversionCore.TryConvertToInt32(value, TypeConversionPolicy.Comparison);
    }

    public long? TryConvertToInt64(object? value)
    {
        return TypeConversionCore.TryConvertToInt64(value, TypeConversionPolicy.Comparison);
    }

    public decimal? TryConvertToDecimal(object? value)
    {
        return TypeConversionCore.TryConvertToDecimal(value, TypeConversionPolicy.Comparison);
    }

    public double? TryConvertToDouble(object? value)
    {
        return TypeConversionCore.TryConvertToDouble(value, TypeConversionPolicy.Comparison);
    }
}
