namespace Musoq.Plugins.Lib.TypeConversion;

/// <summary>
///     Type converter that rejects strings entirely and only accepts boxed numeric types.
///     Used for arithmetic operations on System.Object columns.
/// </summary>
internal class NumericOnlyTypeConverter
{
    public int? TryConvertToInt32(object? value)
    {
        return TypeConversionCore.TryConvertToInt32(value, TypeConversionPolicy.NumericOnly);
    }

    public long? TryConvertToInt64(object? value)
    {
        return TypeConversionCore.TryConvertToInt64(value, TypeConversionPolicy.NumericOnly);
    }

    public decimal? TryConvertToDecimal(object? value)
    {
        return TypeConversionCore.TryConvertToDecimal(value, TypeConversionPolicy.NumericOnly);
    }

    public double? TryConvertToDouble(object? value)
    {
        return TypeConversionCore.TryConvertToDouble(value, TypeConversionPolicy.NumericOnly);
    }

    public decimal? TryConvertNumericOnly(object? value)
    {
        return TypeConversionCore.TryConvertNumericOnly(value);
    }
}
