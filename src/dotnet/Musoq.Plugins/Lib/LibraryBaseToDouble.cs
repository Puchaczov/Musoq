using System;
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
        if (value == null)
            return null;

        return Convert.ToDouble(value);
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
        if (value == null)
            return null;

        if (double.TryParse(value, out var number))
            return number;

        return null;
    }

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(byte? value)
    {
        if (value == null)
            return null;

        return Convert.ToDouble(value.Value);
    }

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(sbyte? value)
    {
        if (value == null)
            return null;

        return Convert.ToDouble(value.Value);
    }

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(short? value)
    {
        if (value == null)
            return null;

        return Convert.ToDouble(value.Value);
    }

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(ushort? value)
    {
        if (value == null)
            return null;

        return Convert.ToDouble(value.Value);
    }

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(int? value)
    {
        if (value == null)
            return null;

        return Convert.ToDouble(value.Value);
    }

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(uint? value)
    {
        if (value == null)
            return null;

        return Convert.ToDouble(value.Value);
    }

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(long? value)
    {
        if (value == null)
            return null;

        return Convert.ToDouble(value.Value);
    }

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(ulong? value)
    {
        if (value == null)
            return null;

        return Convert.ToDouble(value.Value);
    }

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(float? value)
    {
        if (value == null)
            return null;

        return Convert.ToDouble(value.Value);
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
        if (value == null)
            return null;

        return Convert.ToDouble(value.Value);
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
        if (value == null)
            return null;

        return Convert.ToDouble(value.Value);
    }
}
