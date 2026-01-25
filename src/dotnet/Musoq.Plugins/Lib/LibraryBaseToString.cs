using System;
using System.Globalization;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(char? value)
    {
        if (value == null)
            return null;

        return value.ToString();
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(DateTimeOffset? value)
    {
        return value?.ToString();
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(DateTimeOffset? value, string? format)
    {
        return value?.ToString(format ?? "dd.MM.yyyy HH:mm:ss zzz", CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <param name="culture">The culture</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(DateTimeOffset? value, string? format, string? culture)
    {
        return value?.ToString(format ?? "dd.MM.yyyy HH:mm:ss zzz", CultureInfo.GetCultureInfo(culture ?? "en-EN"));
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(byte? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(byte? value, string format)
    {
        return value?.ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(sbyte? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(sbyte? value, string format)
    {
        return value?.ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(int? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(int? value, string format)
    {
        return value?.ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(uint? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(uint? value, string format)
    {
        return value?.ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(long? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(long? value, string format)
    {
        return value?.ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(ulong? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(ulong? value, string format)
    {
        return value?.ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(float? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(float? value, string format)
    {
        return value?.ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(double? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(double? value, string format)
    {
        return value?.ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(decimal? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The format</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(decimal? value, string format)
    {
        return value?.ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(bool? value)
    {
        return value?.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(object? value)
    {
        if (value == null)
            return null;

        return value.ToString();
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString<T>(T? value)
        where T : class
    {
        return value?.ToString();
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string ToString(string[] value)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < value.Length - 1; ++i)
        {
            builder.Append(value[i]);
            builder.Append(',');
        }

        if (value.Length > 0) builder.Append(value[^1]);

        return builder.ToString();
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string ToString<T>(T[] value)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < value.Length - 1; ++i)
        {
            builder.Append(value[i]);
            builder.Append(',');
        }

        if (value.Length > 0) builder.Append(value[^1]);

        return builder.ToString();
    }
}
