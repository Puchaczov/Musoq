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

    /// <summary>
    /// Subtracts two DateTime values and returns the difference as a TimeSpan
    /// </summary>
    /// <param name="date1">The first DateTime value</param>
    /// <param name="date2">The second DateTime value</param>
    /// <returns>The difference between the two DateTime values as a TimeSpan</returns>
    [BindableMethod]
    public TimeSpan? SubtractDates(DateTime? date1, DateTime? date2)
    {
        if (date1.HasValue && date2.HasValue)
            return date1.Value - date2.Value;

        return null;
    }
}