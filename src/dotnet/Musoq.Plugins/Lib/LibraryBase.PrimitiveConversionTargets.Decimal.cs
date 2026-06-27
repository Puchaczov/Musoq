using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts given value to decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to decimal value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public decimal? ToDecimal(string? value)
    {
        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    /// <summary>
    ///     Converts given value to decimal withing given culture
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="culture">The culture</param>
    /// <returns>Converted to decimal value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public decimal? ToDecimal(string value, string culture)
    {
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo(culture), out var result))
            return result;

        return null;
    }

    /// <summary>
    ///     Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public decimal? ToDecimal(byte? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public decimal? ToDecimal(sbyte? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public decimal? ToDecimal(short? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public decimal? ToDecimal(ushort? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public decimal? ToDecimal(long? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public decimal? ToDecimal(ulong? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public decimal? ToDecimal(float? value)
    {
        if (!value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToDecimal(value.Value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public decimal? ToDecimal(double? value)
    {
        if (value == null)
            return null;

        if (double.IsNaN(value.Value) || double.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToDecimal(value.Value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public decimal? ToDecimal(bool? value) => value.HasValue ? value.Value ? 1m : 0m : null;

    /// <summary>
    ///     Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public decimal? ToDecimal(char? value) => value.HasValue ? value.Value : null;

    /// <summary>
    ///     Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public decimal? ToDecimal(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        if (value is string stringValue)
            return ToDecimal(stringValue);

        try
        {
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}
