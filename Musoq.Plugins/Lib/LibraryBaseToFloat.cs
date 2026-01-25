using System;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(string? value)
    {
        if (value == null)
            return null;

        if (float.TryParse(value, out var number))
            return number;

        return null;
    }

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(byte? value)
    {
        if (value == null)
            return null;

        return Convert.ToSingle(value.Value);
    }

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(sbyte? value)
    {
        if (value == null)
            return null;

        return Convert.ToSingle(value.Value);
    }

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(short? value)
    {
        if (value == null)
            return null;

        return Convert.ToSingle(value.Value);
    }

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(ushort? value)
    {
        if (value == null)
            return null;

        return Convert.ToSingle(value.Value);
    }

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(int? value)
    {
        if (value == null)
            return null;

        return Convert.ToSingle(value.Value);
    }

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(uint? value)
    {
        if (value == null)
            return null;

        return Convert.ToSingle(value.Value);
    }

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(long? value)
    {
        if (value == null)
            return null;

        return Convert.ToSingle(value.Value);
    }

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(ulong? value)
    {
        if (value == null)
            return null;

        return Convert.ToSingle(value.Value);
    }

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(float? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(decimal? value)
    {
        if (value == null)
            return null;

        return Convert.ToSingle(value.Value);
    }
}
