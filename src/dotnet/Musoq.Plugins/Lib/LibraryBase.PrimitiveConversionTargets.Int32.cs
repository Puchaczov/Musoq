using System.Globalization;
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
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
    }

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(byte? value) => value;

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(sbyte? value) => value;

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(short? value) => value;

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(ushort? value) => value;

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(int? value) => value;

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(uint? value)
    {
        if (!value.HasValue || value.Value > int.MaxValue)
            return null;

        return (int)value.Value;
    }

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(long? value)
    {
        if (!value.HasValue || value.Value is < int.MinValue or > int.MaxValue)
            return null;

        return (int)value.Value;
    }

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(ulong? value)
    {
        if (!value.HasValue || value.Value > int.MaxValue)
            return null;

        return (int)value.Value;
    }

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(float? value)
    {
        if (!value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToInt32(value.Value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
            return null;

        try
        {
            return Convert.ToInt32(value.Value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(decimal? value)
    {
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToInt32(value.Value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(bool? value) => value.HasValue ? value.Value ? 1 : 0 : null;

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(char? value) => value.HasValue ? value.Value : null;

    /// <summary>
    ///     Converts given value to int
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to int value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public int? ToInt32(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        try
        {
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}
