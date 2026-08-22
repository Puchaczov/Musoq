using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        if (value is string stringValue)
            return ToDouble(stringValue);

        try
        {
            var result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return double.IsNaN(result) || double.IsInfinity(result) ? null : result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(string? value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return null;

        return double.IsNaN(number) || double.IsInfinity(number) ? null : number;
    }

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(byte? value) => value;

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(sbyte? value) => value;

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(short? value) => value;

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(ushort? value) => value;

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(int? value) => value;

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(uint? value) => value;

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(long? value) => value;

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(ulong? value) => value;

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(float? value)
    {
        if (!value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value))
            return null;

        return value;
    }

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
            return null;

        return value;
    }

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(decimal? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToDouble(value.Value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(bool? value) => value.HasValue ? value.Value ? 1d : 0d : null;

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(char? value) => value.HasValue ? value.Value : null;
}
