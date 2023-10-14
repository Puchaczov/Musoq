using System;
using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{    
    /// <summary>
    /// Converts given value to DateTime
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to DateTime value</returns>
    [BindableMethod]
    public DateTime? ToDateTime(string value) => ToDateTime(value, CultureInfo.CurrentCulture.Name);

    /// <summary>
    /// Converts given value to DateTime
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="culture">The culture</param>
    /// <returns>Converted to DateTime value</returns>
    [BindableMethod]
    public DateTime? ToDateTime(string value, string culture)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (!DateTime.TryParse(value, CultureInfo.GetCultureInfo(culture), DateTimeStyles.None, out var result))
            return null;

        return result;
    }
}