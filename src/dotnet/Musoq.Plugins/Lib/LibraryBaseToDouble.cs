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
    public double? ToDouble(byte? value) => ConvertNullable(value, Convert.ToDouble);

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(sbyte? value) => ConvertNullable(value, Convert.ToDouble);

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(short? value) => ConvertNullable(value, Convert.ToDouble);

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(ushort? value) => ConvertNullable(value, Convert.ToDouble);

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(int? value) => ConvertNullable(value, Convert.ToDouble);

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(uint? value) => ConvertNullable(value, Convert.ToDouble);

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(long? value) => ConvertNullable(value, Convert.ToDouble);

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(ulong? value) => ConvertNullable(value, Convert.ToDouble);

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(float? value) => ConvertNullable(value, Convert.ToDouble);

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(double? value) => ConvertNullable(value, Convert.ToDouble);

    /// <summary>
    ///     Converts given value to double
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to double value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public double? ToDouble(decimal? value) => ConvertNullable(value, Convert.ToDouble);
}
