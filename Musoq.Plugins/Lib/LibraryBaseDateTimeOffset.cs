using System;
using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{    
    /// <summary>
    /// Converts given value to DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to DateTimeOffset value</returns>
    [BindableMethod]
    public DateTime? ToDateTimeOffset(string value) => ToDateTime(value, CultureInfo.CurrentCulture.Name);

    /// <summary>
    /// Converts given value to DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="culture">The culture</param>
    /// <returns>Converted to DateTimeOffset value</returns>
    [BindableMethod]
    public DateTimeOffset? ToDateTimeOffset(string value, string culture)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (!DateTimeOffset.TryParse(value, CultureInfo.GetCultureInfo(culture), DateTimeStyles.None, out var result))
            return null;

        return result;
    }
}