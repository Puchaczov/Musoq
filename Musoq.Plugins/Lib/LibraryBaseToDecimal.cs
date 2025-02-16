using System;
using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Converts given value to decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to decimal value</returns>
    [BindableMethod]
    public decimal? ToDecimal(string? value)
    {
        if (value == null)
            return null;

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var result))
            return result;

        return null;
    }

    /// <summary>
    /// Converts given value to decimal withing given culture
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="culture">The culture</param>
    /// <returns>Converted to decimal value</returns>
    [BindableMethod]
    public decimal? ToDecimal(string value, string culture)
    {
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo(culture), out var result))
            return result;

        return null;
    }

    /// <summary>
    /// Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    public decimal? ToDecimal(byte? value)
    {
        return value;
    }

    /// <summary>
    /// Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    public decimal? ToDecimal(sbyte? value)
    {
        return value;
    }

    /// <summary>
    /// Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    public decimal? ToDecimal(short? value)
    {
        return value;
    }

    /// <summary>
    /// Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    public decimal? ToDecimal(ushort? value)
    {
        return value;
    }

    /// <summary>
    /// Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    public decimal? ToDecimal(long? value)
    {
        return value;
    }

    /// <summary>
    /// Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    public decimal? ToDecimal(ulong? value)
    {
        return value;
    }

    /// <summary>
    /// Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    public decimal? ToDecimal(float? value)
    {
        return (decimal?)value;
    }

    /// <summary>
    /// Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    public decimal? ToDecimal(double? value)
    {
        if (value == null)
            return null;

        return Convert.ToDecimal(value.Value);
    }

    /// <summary>
    /// Converts given value to Decimal
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Decimal value</returns>
    [BindableMethod]
    public decimal? ToDecimal(object? value)
    {
        if (value == null)
            return null;

        return Convert.ToDecimal(value);
    }
}