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
    public string? ToString(float? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture);
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
        return value?.ToString(format, CultureInfo.InvariantCulture);
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
        return value?.ToString(CultureInfo.InvariantCulture);
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
        return value?.ToString(format, CultureInfo.InvariantCulture);
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
        return value?.ToString(CultureInfo.InvariantCulture);
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
        return value?.ToString(format, CultureInfo.InvariantCulture);
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
        return value?.ToString();
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(string? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(Guid? value)
    {
        return value?.ToString("D");
    }

    /// <summary>
    ///     Converts given value to string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to string value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToString(TimeSpan? value)
    {
        return value?.ToString(null, CultureInfo.InvariantCulture);
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
        if (IsNullConversionInput(value))
            return null;

        return value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value!.ToString();
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
        ArgumentNullException.ThrowIfNull(value);
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
        ArgumentNullException.ThrowIfNull(value);
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
