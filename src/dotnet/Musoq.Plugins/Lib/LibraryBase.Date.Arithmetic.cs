using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Adds days to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="days">The days to add</param>
    /// <returns>Date with added days</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTime? AddDays(DateTime? date, int days)
    {
        return date?.AddDays(days);
    }

    /// <summary>
    ///     Adds days to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="days">The days to add</param>
    /// <returns>Date with added days</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTimeOffset? AddDays(DateTimeOffset? date, int days)
    {
        return date?.AddDays(days);
    }
    /// <summary>
    ///     Adds months to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="months">The months to add</param>
    /// <returns>Date with added months</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTime? AddMonths(DateTime? date, int months)
    {
        return date?.AddMonths(months);
    }

    /// <summary>
    ///     Adds months to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="months">The months to add</param>
    /// <returns>Date with added months</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTimeOffset? AddMonths(DateTimeOffset? date, int months)
    {
        return date?.AddMonths(months);
    }

    /// <summary>
    ///     Adds years to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="years">The years to add</param>
    /// <returns>Date with added years</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTime? AddYears(DateTime? date, int years)
    {
        return date?.AddYears(years);
    }

    /// <summary>
    ///     Adds years to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="years">The years to add</param>
    /// <returns>Date with added years</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTimeOffset? AddYears(DateTimeOffset? date, int years)
    {
        return date?.AddYears(years);
    }

    /// <summary>
    ///     Adds hours to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="hours">The hours to add</param>
    /// <returns>Date with added hours</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTime? AddHours(DateTime? date, int hours)
    {
        return date?.AddHours(hours);
    }

    /// <summary>
    ///     Adds hours to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="hours">The hours to add</param>
    /// <returns>Date with added hours</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTimeOffset? AddHours(DateTimeOffset? date, int hours)
    {
        return date?.AddHours(hours);
    }

    /// <summary>
    ///     Adds minutes to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="minutes">The minutes to add</param>
    /// <returns>Date with added minutes</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTime? AddMinutes(DateTime? date, int minutes)
    {
        return date?.AddMinutes(minutes);
    }

    /// <summary>
    ///     Adds minutes to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="minutes">The minutes to add</param>
    /// <returns>Date with added minutes</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTimeOffset? AddMinutes(DateTimeOffset? date, int minutes)
    {
        return date?.AddMinutes(minutes);
    }

    /// <summary>
    ///     Adds seconds to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="seconds">The seconds to add</param>
    /// <returns>Date with added seconds</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTime? AddSeconds(DateTime? date, int seconds)
    {
        return date?.AddSeconds(seconds);
    }

    /// <summary>
    ///     Adds seconds to the date
    /// </summary>
    /// <param name="date">The date</param>
    /// <param name="seconds">The seconds to add</param>
    /// <returns>Date with added seconds</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTimeOffset? AddSeconds(DateTimeOffset? date, int seconds)
    {
        return date?.AddSeconds(seconds);
    }
}
