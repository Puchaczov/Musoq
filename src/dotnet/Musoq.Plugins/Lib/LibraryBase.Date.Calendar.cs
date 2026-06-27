using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Determines whether the date falls on a weekend (Saturday or Sunday)
    /// </summary>
    /// <param name="date">The date to check</param>
    /// <returns>True if the date is a Saturday or Sunday; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public bool? IsWeekend(DateTime? date)
    {
        if (!date.HasValue)
            return null;

        return date.Value.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday;
    }

    /// <summary>
    ///     Determines whether the date falls on a weekend (Saturday or Sunday)
    /// </summary>
    /// <param name="date">The date to check</param>
    /// <returns>True if the date is a Saturday or Sunday; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public bool? IsWeekend(DateTimeOffset? date)
    {
        if (!date.HasValue)
            return null;

        return date.Value.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday;
    }

    /// <summary>
    ///     Determines whether the date falls on a weekday (Monday through Friday)
    /// </summary>
    /// <param name="date">The date to check</param>
    /// <returns>True if the date is a weekday; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public bool? IsWeekday(DateTime? date)
    {
        if (!date.HasValue)
            return null;

        return date.Value.DayOfWeek != System.DayOfWeek.Saturday &&
               date.Value.DayOfWeek != System.DayOfWeek.Sunday;
    }

    /// <summary>
    ///     Determines whether the date falls on a weekday (Monday through Friday)
    /// </summary>
    /// <param name="date">The date to check</param>
    /// <returns>True if the date is a weekday; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public bool? IsWeekday(DateTimeOffset? date)
    {
        if (!date.HasValue)
            return null;

        return date.Value.DayOfWeek != System.DayOfWeek.Saturday &&
               date.Value.DayOfWeek != System.DayOfWeek.Sunday;
    }
    /// <summary>
    ///     Returns the week of the year for the given date
    /// </summary>
    /// <param name="date">The date</param>
    /// <returns>The week number (1-53)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? WeekOfYear(DateTime? date)
    {
        if (!date.HasValue)
            return null;

        return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            date.Value,
            CalendarWeekRule.FirstFourDayWeek,
            System.DayOfWeek.Monday);
    }

    /// <summary>
    ///     Returns the week of the year for the given date
    /// </summary>
    /// <param name="date">The date</param>
    /// <returns>The week number (1-53)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? WeekOfYear(DateTimeOffset? date)
    {
        if (!date.HasValue)
            return null;

        return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            date.Value.DateTime,
            CalendarWeekRule.FirstFourDayWeek,
            System.DayOfWeek.Monday);
    }

    /// <summary>
    ///     Returns the quarter of the year for the given date (1-4)
    /// </summary>
    /// <param name="date">The date</param>
    /// <returns>The quarter (1-4)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Quarter(DateTime? date)
    {
        if (!date.HasValue)
            return null;

        return (date.Value.Month - 1) / 3 + 1;
    }

    /// <summary>
    ///     Returns the quarter of the year for the given date (1-4)
    /// </summary>
    /// <param name="date">The date</param>
    /// <returns>The quarter (1-4)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Quarter(DateTimeOffset? date)
    {
        if (!date.HasValue)
            return null;

        return (date.Value.Month - 1) / 3 + 1;
    }

    /// <summary>
    ///     Returns the day of the year for the given date (1-366)
    /// </summary>
    /// <param name="date">The date</param>
    /// <returns>The day of year</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? DayOfYear(DateTime? date)
    {
        return date?.DayOfYear;
    }

    /// <summary>
    ///     Returns the day of the year for the given date (1-366)
    /// </summary>
    /// <param name="date">The date</param>
    /// <returns>The day of year</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? DayOfYear(DateTimeOffset? date)
    {
        return date?.DayOfYear;
    }

    /// <summary>
    ///     Determines whether the year of the given date is a leap year
    /// </summary>
    /// <param name="date">The date</param>
    /// <returns>True if the year is a leap year; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public bool? IsLeapYear(DateTime? date)
    {
        if (!date.HasValue)
            return null;

        return DateTime.IsLeapYear(date.Value.Year);
    }

    /// <summary>
    ///     Determines whether the year of the given date is a leap year
    /// </summary>
    /// <param name="date">The date</param>
    /// <returns>True if the year is a leap year; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public bool? IsLeapYear(DateTimeOffset? date)
    {
        if (!date.HasValue)
            return null;

        return DateTime.IsLeapYear(date.Value.Year);
    }
}
