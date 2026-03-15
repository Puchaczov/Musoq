using System;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(string? value)
    {
        if (int.TryParse(value, out var number))
            return number;

        return null;
    }

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(byte? value) => ConvertNullable(value, Convert.ToInt32);

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(sbyte? value) => ConvertNullable(value, Convert.ToInt32);

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(short? value) => ConvertNullable(value, Convert.ToInt32);

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(ushort? value) => ConvertNullable(value, Convert.ToInt32);

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(int? value) => ConvertNullable(value, Convert.ToInt32);

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(uint? value) => ConvertNullable(value, Convert.ToInt32);

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(long? value) => ConvertNullable(value, Convert.ToInt32);

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(ulong? value) => ConvertNullable(value, Convert.ToInt32);

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(float? value) => ConvertNullable(value, Convert.ToInt32);

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(double? value) => ConvertNullable(value, Convert.ToInt32);

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(decimal? value) => ConvertNullable(value, Convert.ToInt32);

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(object? value)
    {
        if (value == null)
            return null;

        return Convert.ToInt32(value);
    }
}
