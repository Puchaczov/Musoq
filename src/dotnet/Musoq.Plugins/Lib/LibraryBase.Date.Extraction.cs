using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Extracts part of the date from the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="partOfDate">Part of the date</param>
    /// <returns>Extracted part of the date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int ExtractFromDate(string? date, string partOfDate)
    {
        ArgumentNullException.ThrowIfNull(partOfDate);
        return ExtractFromDate(date, CultureInfo.CurrentCulture, partOfDate);
    }

    /// <summary>
    ///     Extracts part of the date from the date based on given culture
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="culture"> The culture</param>
    /// <param name="partOfDate">Part of the date</param>
    /// <returns>Extracted part of the date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int ExtractFromDate(string date, string culture, string partOfDate)
    {
        ArgumentNullException.ThrowIfNull(partOfDate);
        return ExtractFromDate(date, new CultureInfo(culture), partOfDate);
    }
    /// <summary>
    ///     Extracts time from DateTime
    /// </summary>
    /// <param name="dateTime">The value</param>
    /// <returns>Time from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public TimeSpan? ExtractTimeSpan(DateTime? dateTime)
    {
        return dateTime?.TimeOfDay;
    }

    /// <summary>
    ///     Extracts time from DateTimeOffset
    /// </summary>
    /// <param name="dateTimeOffset">The value</param>
    /// <returns>Time from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public TimeSpan? ExtractTimeSpan(DateTimeOffset? dateTimeOffset)
    {
        return dateTimeOffset?.TimeOfDay;
    }

    private static int ExtractFromDate(string? date, CultureInfo culture, string partOfDate)
    {
        if (!DateTimeOffset.TryParse(date, culture, DateTimeStyles.None, out var value))
            throw new NotSupportedException($"'{date}' value looks to be not valid date.");

        return partOfDate.ToLower(culture) switch
        {
            "month" => value.Month,
            "year" => value.Year,
            "day" => value.Day,
            "hour" => value.Hour,
            "minute" => value.Minute,
            "second" => value.Second,
            "millisecond" => value.Millisecond,
            _ => throw new NotSupportedException($"specified part of date value ({partOfDate}) is not valid.")
        };
    }
}
