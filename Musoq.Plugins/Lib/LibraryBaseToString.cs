using System;
using System.Globalization;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string? ToString(char? value)
    {
        if (value == null)
            return null;

        return value.ToString();
    }

    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string? ToString(DateTimeOffset? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string? ToString(byte? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string? ToString(sbyte? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string? ToString(int? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string? ToString(uint? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string? ToString(long? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string? ToString(ulong? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string? ToString(float? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string? ToString(double? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string? ToString(decimal? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string? ToString(bool? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string? ToString(object? value)
    {
        if (value == null)
            return null;

        return value.ToString();
    }

    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string? ToString<T>(T? value)
        where T : class
    {
        return value?.ToString();
    }

    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string ToString(string[] value)
    {
        var builder = new StringBuilder();

        for (int i = 0; i < value.Length - 1; ++i)
        {
            builder.Append(value[i]);
            builder.Append(',');
        }

        if (value.Length > 0)
        {
            builder.Append(value[^1]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    public string ToString<T>(T[] value)
    {
        var builder = new StringBuilder();

        for (int i = 0; i < value.Length - 1; ++i)
        {
            builder.Append(value[i]);
            builder.Append(',');
        }

        if (value.Length > 0)
        {
            builder.Append(value[^1]);
        }

        return builder.ToString();
    }
}