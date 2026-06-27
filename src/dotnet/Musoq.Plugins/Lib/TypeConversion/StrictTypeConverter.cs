namespace Musoq.Plugins.Lib.TypeConversion;

/// <summary>
///     Type converter that rejects any conversions resulting in precision loss.
///     Used for equality comparisons where exact values matter.
/// </summary>
internal class StrictTypeConverter
{
    public int? TryConvertToInt32(object? value)
    {
        return TypeConversionCore.TryConvertToInt32(value, TypeConversionPolicy.Strict);
    }

    public long? TryConvertToInt64(object? value)
    {
        return TypeConversionCore.TryConvertToInt64(value, TypeConversionPolicy.Strict);
    }

    public decimal? TryConvertToDecimal(object? value)
    {
        return TypeConversionCore.TryConvertToDecimal(value, TypeConversionPolicy.Strict);
    }

    public double? TryConvertToDouble(object? value)
    {
        return TypeConversionCore.TryConvertToDouble(value, TypeConversionPolicy.Strict);
    }
}
