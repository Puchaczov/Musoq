using System;
using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts given value to DateTime
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Converted to DateTime value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public DateTime? ToDateTime(string value)
    {
        return ToDateTime(value, CultureInfo.CurrentCulture.Name);
    }

    /// <summary>
    ///     Converts given value to DateTime using exact format
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="format">The exact format to use for parsing</param>
    /// <returns>Converted to DateTime value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public DateTime? ToDateTime(string value, string format)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        // Try parsing with exact format first
        if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            return result;

        // Fallback to culture-based parsing for backward compatibility
        // This allows the method to work with both format strings and culture names
        try
        {
            if (DateTime.TryParse(value, CultureInfo.GetCultureInfo(format), DateTimeStyles.None, out result))
                return result;
        }
        catch (CultureNotFoundException)
        {
            // format parameter wasn't a valid culture name, which is expected when using format strings
        }

        return null;
    }

    /// <summary>
    ///     Subtracts two DateTime values and returns the difference as a TimeSpan
    /// </summary>
    /// <param name="date1">The first DateTime value</param>
    /// <param name="date2">The second DateTime value</param>
    /// <returns>The difference between the two DateTime values as a TimeSpan</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public TimeSpan? SubtractDates(DateTime? date1, DateTime? date2)
    {
        if (date1.HasValue && date2.HasValue)
            return date1.Value - date2.Value;

        return null;
    }
}
