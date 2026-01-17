using System;
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
    public TimeSpan? ToTimeSpan(string value)
    {
        return ToTimeSpan(value, CultureInfo.CurrentCulture.Name);
    }

    /// <summary>
    ///     Converts given value to TimeSpan
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="culture">The culture</param>
    /// <returns>Converted to TimeSpan value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public TimeSpan? ToTimeSpan(string value, string culture)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (!TimeSpan.TryParse(value, CultureInfo.GetCultureInfo(culture), out var result))
            return null;

        return result;
    }
}