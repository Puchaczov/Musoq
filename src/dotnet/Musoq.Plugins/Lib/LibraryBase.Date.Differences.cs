using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Calculates the difference in days between two dates
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
    ///     Calculates the difference in days between two dates
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
    ///     Calculates the difference in hours between two dates
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
    ///     Calculates the difference in hours between two dates
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
    ///     Calculates the difference in minutes between two dates
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
    ///     Calculates the difference in minutes between two dates
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
    ///     Calculates the difference in seconds between two dates
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
    ///     Calculates the difference in seconds between two dates
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
}
