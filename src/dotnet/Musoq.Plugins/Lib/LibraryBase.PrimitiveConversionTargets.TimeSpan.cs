using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts given value to TimeSpan
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to TimeSpan value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public TimeSpan? ToTimeSpan(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    /// <summary>
    ///     Converts given value to TimeSpan
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="culture">The culture</param>
    /// <returns>Converted to TimeSpan value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public TimeSpan? ToTimeSpan(string? value, string culture)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (!TimeSpan.TryParse(value, CultureInfo.GetCultureInfo(culture), out var result))
            return null;

        return result;
    }

    /// <summary>
    ///     Converts given value to TimeSpan
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to TimeSpan value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public TimeSpan? ToTimeSpan(TimeSpan? value)
    {
        return value;
    }

    /// <summary>
    ///     Converts given value to TimeSpan
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to TimeSpan value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public TimeSpan? ToTimeSpan(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        if (value is TimeSpan timeSpan)
            return timeSpan;

        return ToTimeSpan(value!.ToString());
    }
}
