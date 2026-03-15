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
    public float? ToFloat(byte? value) => ConvertNullable(value, Convert.ToSingle);

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(sbyte? value) => ConvertNullable(value, Convert.ToSingle);

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(short? value) => ConvertNullable(value, Convert.ToSingle);

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(ushort? value) => ConvertNullable(value, Convert.ToSingle);

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(int? value) => ConvertNullable(value, Convert.ToSingle);

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(uint? value) => ConvertNullable(value, Convert.ToSingle);

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(long? value) => ConvertNullable(value, Convert.ToSingle);

    /// <summary>
    ///     Converts given value to float
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to float value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public float? ToFloat(ulong? value) => ConvertNullable(value, Convert.ToSingle);

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
    public float? ToFloat(decimal? value) => ConvertNullable(value, Convert.ToSingle);

    private static TResult? ConvertNullable<TSource, TResult>(TSource? value, Func<TSource, TResult> converter)
        where TSource : struct
        where TResult : struct
    {
        if (value == null)
            return null;

        return converter(value.Value);
    }
}
