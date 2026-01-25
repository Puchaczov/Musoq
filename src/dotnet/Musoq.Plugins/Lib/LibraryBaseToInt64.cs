using System;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts given value to Int64
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to Int64 value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(string? value)
    {
        if (long.TryParse(value, out var number))
            return number;

        return null;
    }

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(byte? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(sbyte? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(short? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(ushort? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(int? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(uint? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(long? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(ulong? value)
    {
        return (long?)value;
    }

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(float? value)
    {
        return (long?)value;
    }

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(double? value)
    {
        return (long?)value;
    }

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(decimal? value)
    {
        if (value == null)
            return null;

        return Convert.ToInt64(value.Value);
    }

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(object? value)
    {
        if (value == null)
            return null;

        return Convert.ToInt64(value);
    }
}
