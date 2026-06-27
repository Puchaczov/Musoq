using System.Globalization;
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
    public string? ToString(DateTimeOffset? value, string? format)
    {
        return value?.ToString(format ?? "dd.MM.yyyy HH:mm:ss zzz", CultureInfo.InvariantCulture);
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
}
