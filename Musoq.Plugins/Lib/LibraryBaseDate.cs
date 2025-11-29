using System;
using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Extracts part of the date from the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="partOfDate">Part of the date</param>
    /// <returns>Extracted part of the date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int ExtractFromDate(string? date, string partOfDate)
        => ExtractFromDate(date, CultureInfo.CurrentCulture, partOfDate);

    /// <summary>
    /// Extracts part of the date from the date based on given culture
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="culture"> The culture</param>
    /// <param name="partOfDate">Part of the date</param>
    /// <returns>Extracted part of the date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int ExtractFromDate(string date, string culture, string partOfDate)
        => ExtractFromDate(date, new CultureInfo(culture), partOfDate);

    /// <summary>
    /// Gets the current datetime
    /// </summary>
    /// <returns>Current datetime</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    [NonDeterministic]
    public DateTimeOffset? GetDate()
        => DateTimeOffset.Now;

    /// <summary>
    /// Gets the current datetime in UTC
    /// </summary>
    /// <returns>Current datetime in UTC</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    [NonDeterministic]
    public DateTimeOffset? UtcGetDate()
        => DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the month from DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Month from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Month(DateTimeOffset? value)
        => value?.Month;
        
    /// <summary>
    /// Gets the year from DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Year from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Year(DateTimeOffset? value)
        => value?.Year;
        
    /// <summary>
    /// Gets the day from DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Day from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Day(DateTimeOffset? value)
        => value?.Day;
        
    /// <summary>
    /// Gets the hour from DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Hour from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Hour(DateTimeOffset? value)
        => value?.Hour;
        
    /// <summary>
    /// Gets the minute from DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Minute from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Minute(DateTimeOffset? value)
        => value?.Minute;
        
    /// <summary>
    /// Gets the second from DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Second from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Second(DateTimeOffset? value)
        => value?.Second;
        
    /// <summary>
    /// Gets the millisecond from DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Millisecond from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Milliseconds(DateTimeOffset? value)
        => value?.Millisecond;
        
    /// <summary>
    /// Gets the day of week from DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Day of week from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? DayOfWeek(DateTimeOffset? value)
        => (int?)value?.DayOfWeek;

    /// <summary>
    /// Adds days to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="days">The days to add</param>
    /// <returns>Date with added days</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTime? AddDays(DateTime? date, int days)
        => date?.AddDays(days);

    /// <summary>
    /// Adds days to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="days">The days to add</param>
    /// <returns>Date with added days</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTimeOffset? AddDays(DateTimeOffset? date, int days)
        => date?.AddDays(days);

    /// <summary>
    /// Extracts time from DateTimeOffset
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

    /// <summary>
    /// Adds months to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="months">The months to add</param>
    /// <returns>Date with added months</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTime? AddMonths(DateTime? date, int months)
        => date?.AddMonths(months);

    /// <summary>
    /// Adds months to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="months">The months to add</param>
    /// <returns>Date with added months</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTimeOffset? AddMonths(DateTimeOffset? date, int months)
        => date?.AddMonths(months);

    /// <summary>
    /// Adds years to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="years">The years to add</param>
    /// <returns>Date with added years</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTime? AddYears(DateTime? date, int years)
        => date?.AddYears(years);

    /// <summary>
    /// Adds years to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="years">The years to add</param>
    /// <returns>Date with added years</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTimeOffset? AddYears(DateTimeOffset? date, int years)
        => date?.AddYears(years);

    /// <summary>
    /// Adds hours to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="hours">The hours to add</param>
    /// <returns>Date with added hours</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTime? AddHours(DateTime? date, int hours)
        => date?.AddHours(hours);

    /// <summary>
    /// Adds hours to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="hours">The hours to add</param>
    /// <returns>Date with added hours</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTimeOffset? AddHours(DateTimeOffset? date, int hours)
        => date?.AddHours(hours);

    /// <summary>
    /// Adds minutes to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="minutes">The minutes to add</param>
    /// <returns>Date with added minutes</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTime? AddMinutes(DateTime? date, int minutes)
        => date?.AddMinutes(minutes);

    /// <summary>
    /// Adds minutes to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="minutes">The minutes to add</param>
    /// <returns>Date with added minutes</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTimeOffset? AddMinutes(DateTimeOffset? date, int minutes)
        => date?.AddMinutes(minutes);

    /// <summary>
    /// Adds seconds to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="seconds">The seconds to add</param>
    /// <returns>Date with added seconds</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTime? AddSeconds(DateTime? date, int seconds)
        => date?.AddSeconds(seconds);

    /// <summary>
    /// Adds seconds to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="seconds">The seconds to add</param>
    /// <returns>Date with added seconds</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTimeOffset? AddSeconds(DateTimeOffset? date, int seconds)
        => date?.AddSeconds(seconds);

    /// <summary>
    /// Returns the start of the day (midnight) for the given date
    /// </summary>
    /// <param name="date">The date</param>
    /// <returns>The start of the day (00:00:00)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTime? StartOfDay(DateTime? date)
        => date?.Date;

    /// <summary>
    /// Returns the start of the day (midnight) for the given date
    /// </summary>
    /// <param name="date">The date</param>
    /// <returns>The start of the day (00:00:00)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTimeOffset? StartOfDay(DateTimeOffset? date)
    {
        if (!date.HasValue)
            return null;
        
        return new DateTimeOffset(date.Value.Date, date.Value.Offset);
    }

    /// <summary>
    /// Returns the end of the day (23:59:59.9999999) for the given date
    /// </summary>
    /// <param name="date">The date</param>
    /// <returns>The end of the day</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTime? EndOfDay(DateTime? date)
    {
        if (!date.HasValue)
            return null;
        
        return date.Value.Date.AddDays(1).AddTicks(-1);
    }

    /// <summary>
    /// Returns the end of the day (23:59:59.9999999) for the given date
    /// </summary>
    /// <param name="date">The date</param>
    /// <returns>The end of the day</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTimeOffset? EndOfDay(DateTimeOffset? date)
    {
        if (!date.HasValue)
            return null;
        
        return new DateTimeOffset(date.Value.Date.AddDays(1).AddTicks(-1), date.Value.Offset);
    }

    /// <summary>
    /// Determines whether the date falls on a weekend (Saturday or Sunday)
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
    /// Determines whether the date falls on a weekend (Saturday or Sunday)
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
    /// Determines whether the date falls on a weekday (Monday through Friday)
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
    /// Determines whether the date falls on a weekday (Monday through Friday)
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
    /// Calculates the difference in days between two dates
    /// </summary>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <returns>The number of days between the two dates (can be negative)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? DateDiffInDays(DateTime? startDate, DateTime? endDate)
    {
        if (!startDate.HasValue || !endDate.HasValue)
            return null;
        
        return (int)(endDate.Value - startDate.Value).TotalDays;
    }

    /// <summary>
    /// Calculates the difference in days between two dates
    /// </summary>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <returns>The number of days between the two dates (can be negative)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? DateDiffInDays(DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        if (!startDate.HasValue || !endDate.HasValue)
            return null;
        
        return (int)(endDate.Value - startDate.Value).TotalDays;
    }

    /// <summary>
    /// Calculates the difference in hours between two dates
    /// </summary>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <returns>The number of hours between the two dates (can be negative)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? DateDiffInHours(DateTime? startDate, DateTime? endDate)
    {
        if (!startDate.HasValue || !endDate.HasValue)
            return null;
        
        return (int)(endDate.Value - startDate.Value).TotalHours;
    }

    /// <summary>
    /// Calculates the difference in hours between two dates
    /// </summary>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <returns>The number of hours between the two dates (can be negative)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? DateDiffInHours(DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        if (!startDate.HasValue || !endDate.HasValue)
            return null;
        
        return (int)(endDate.Value - startDate.Value).TotalHours;
    }

    /// <summary>
    /// Calculates the difference in minutes between two dates
    /// </summary>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <returns>The number of minutes between the two dates (can be negative)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? DateDiffInMinutes(DateTime? startDate, DateTime? endDate)
    {
        if (!startDate.HasValue || !endDate.HasValue)
            return null;
        
        return (int)(endDate.Value - startDate.Value).TotalMinutes;
    }

    /// <summary>
    /// Calculates the difference in minutes between two dates
    /// </summary>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <returns>The number of minutes between the two dates (can be negative)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? DateDiffInMinutes(DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        if (!startDate.HasValue || !endDate.HasValue)
            return null;
        
        return (int)(endDate.Value - startDate.Value).TotalMinutes;
    }

    /// <summary>
    /// Calculates the difference in seconds between two dates
    /// </summary>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <returns>The number of seconds between the two dates (can be negative)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public long? DateDiffInSeconds(DateTime? startDate, DateTime? endDate)
    {
        if (!startDate.HasValue || !endDate.HasValue)
            return null;
        
        return (long)(endDate.Value - startDate.Value).TotalSeconds;
    }

    /// <summary>
    /// Calculates the difference in seconds between two dates
    /// </summary>
    /// <param name="startDate">The start date</param>
    /// <param name="endDate">The end date</param>
    /// <returns>The number of seconds between the two dates (can be negative)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public long? DateDiffInSeconds(DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        if (!startDate.HasValue || !endDate.HasValue)
            return null;
        
        return (long)(endDate.Value - startDate.Value).TotalSeconds;
    }

    /// <summary>
    /// Returns the week of the year for the given date
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
    /// Returns the week of the year for the given date
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
    /// Returns the quarter of the year for the given date (1-4)
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
    /// Returns the quarter of the year for the given date (1-4)
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
    /// Returns the day of the year for the given date (1-366)
    /// </summary>
    /// <param name="date">The date</param>
    /// <returns>The day of year</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? DayOfYear(DateTime? date)
        => date?.DayOfYear;

    /// <summary>
    /// Returns the day of the year for the given date (1-366)
    /// </summary>
    /// <param name="date">The date</param>
    /// <returns>The day of year</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? DayOfYear(DateTimeOffset? date)
        => date?.DayOfYear;

    /// <summary>
    /// Determines whether the year of the given date is a leap year
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
    /// Determines whether the year of the given date is a leap year
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

    /// <summary>
    /// Checks if a DateTime value is between the specified start and end dates (inclusive).
    /// </summary>
    /// <param name="value">The date to check.</param>
    /// <param name="start">The start date (inclusive).</param>
    /// <param name="end">The end date (inclusive).</param>
    /// <returns>True if the date is between start and end; otherwise false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public bool? IsBetween(DateTime? value, DateTime? start, DateTime? end)
    {
        if (!value.HasValue || !start.HasValue || !end.HasValue)
            return null;

        return value.Value >= start.Value && value.Value <= end.Value;
    }

    /// <summary>
    /// Checks if a DateTimeOffset value is between the specified start and end dates (inclusive).
    /// </summary>
    /// <param name="value">The date to check.</param>
    /// <param name="start">The start date (inclusive).</param>
    /// <param name="end">The end date (inclusive).</param>
    /// <returns>True if the date is between start and end; otherwise false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public bool? IsBetween(DateTimeOffset? value, DateTimeOffset? start, DateTimeOffset? end)
    {
        if (!value.HasValue || !start.HasValue || !end.HasValue)
            return null;

        return value.Value >= start.Value && value.Value <= end.Value;
    }

    /// <summary>
    /// Checks if a DateTime value is between the specified start and end dates (exclusive).
    /// </summary>
    /// <param name="value">The date to check.</param>
    /// <param name="start">The start date (exclusive).</param>
    /// <param name="end">The end date (exclusive).</param>
    /// <returns>True if the date is strictly between start and end; otherwise false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public bool? IsBetweenExclusive(DateTime? value, DateTime? start, DateTime? end)
    {
        if (!value.HasValue || !start.HasValue || !end.HasValue)
            return null;

        return value.Value > start.Value && value.Value < end.Value;
    }

    /// <summary>
    /// Checks if a DateTimeOffset value is between the specified start and end dates (exclusive).
    /// </summary>
    /// <param name="value">The date to check.</param>
    /// <param name="start">The start date (exclusive).</param>
    /// <param name="end">The end date (exclusive).</param>
    /// <returns>True if the date is strictly between start and end; otherwise false.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public bool? IsBetweenExclusive(DateTimeOffset? value, DateTimeOffset? start, DateTimeOffset? end)
    {
        if (!value.HasValue || !start.HasValue || !end.HasValue)
            return null;

        return value.Value > start.Value && value.Value < end.Value;
    }
}