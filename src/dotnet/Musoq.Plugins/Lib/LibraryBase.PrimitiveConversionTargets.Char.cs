using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : value[0];
    }

    /// <summary>
    ///     Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(char? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(int? value)
    {
        if (!value.HasValue || value.Value is < char.MinValue or > char.MaxValue)
            return null;

        return (char)value.Value;
    }

    /// <summary>
    ///     Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(short? value)
    {
        if (!value.HasValue || value.Value < 0)
            return null;

        return (char)value.Value;
    }

    /// <summary>
    ///     Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(sbyte? value)
    {
        if (!value.HasValue || value.Value < 0)
            return null;

        return (char)value.Value;
    }

    /// <summary>
    ///     Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(ushort? value)
    {
        return value.HasValue ? (char)value.Value : null;
    }

    /// <summary>
    ///     Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(uint? value)
    {
        if (!value.HasValue || value.Value > char.MaxValue)
            return null;

        return (char)value.Value;
    }

    /// <summary>
    ///     Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(long? value)
    {
        if (!value.HasValue || value.Value is < 0 or > char.MaxValue)
            return null;

        return (char)value.Value;
    }

    /// <summary>
    ///     Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(ulong? value)
    {
        if (!value.HasValue || value.Value > char.MaxValue)
            return null;

        return (char)value.Value;
    }

    /// <summary>
    ///     Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(float? value)
    {
        return null;
    }

    /// <summary>
    ///     Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(double? value)
    {
        return null;
    }

    /// <summary>
    ///     Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(decimal? value)
    {
        return null;
    }

    /// <summary>
    ///     Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(bool? value)
    {
        return null;
    }

    /// <summary>
    ///     Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(byte? value)
    {
        return value.HasValue ? (char)value.Value : null;
    }

    /// <summary>
    ///     Converts given value to character
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to character value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public char? ToChar(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        try
        {
            return Convert.ToChar(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}
