using System.Globalization;
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
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
    }

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(byte? value) => value;

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(sbyte? value) => value;

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(short? value) => value;

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(ushort? value) => value;

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(int? value) => value;

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(uint? value) => value;

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(long? value) => value;

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(ulong? value)
    {
        if (!value.HasValue || value.Value > long.MaxValue)
            return null;

        return (long)value.Value;
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
        if (!value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value))
            return null;

        if (value.Value is < long.MinValue or > long.MaxValue)
            return null;

        return (long)value.Value;
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
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
            return null;

        if (value.Value is < long.MinValue or > long.MaxValue)
            return null;

        return (long)value.Value;
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
        if (!value.HasValue)
            return null;

        try
        {
            return Convert.ToInt64(value.Value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(bool? value) => value.HasValue ? value.Value ? 1L : 0L : null;

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(char? value) => value.HasValue ? value.Value : null;

    /// <summary>
    ///     Converts given value to long
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to long value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public long? ToInt64(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        try
        {
            return Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}
