using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Gets the month from DateTime
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Month from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Month(DateTime? value)
    {
        return value?.Month;
    }

    /// <summary>
    ///     Gets the month from DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Month from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Month(DateTimeOffset? value)
    {
        return value?.Month;
    }

    /// <summary>
    ///     Gets the year from DateTime
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Year from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Year(DateTime? value)
    {
        return value?.Year;
    }

    /// <summary>
    ///     Gets the year from DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Year from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Year(DateTimeOffset? value)
    {
        return value?.Year;
    }

    /// <summary>
    ///     Gets the day from DateTime
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Day from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Day(DateTime? value)
    {
        return value?.Day;
    }

    /// <summary>
    ///     Gets the day from DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Day from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Day(DateTimeOffset? value)
    {
        return value?.Day;
    }

    /// <summary>
    ///     Gets the hour from DateTime
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Hour from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Hour(DateTime? value)
    {
        return value?.Hour;
    }

    /// <summary>
    ///     Gets the hour from DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Hour from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Hour(DateTimeOffset? value)
    {
        return value?.Hour;
    }

    /// <summary>
    ///     Gets the minute from DateTime
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Minute from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Minute(DateTime? value)
    {
        return value?.Minute;
    }

    /// <summary>
    ///     Gets the minute from DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Minute from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Minute(DateTimeOffset? value)
    {
        return value?.Minute;
    }

    /// <summary>
    ///     Gets the second from DateTime
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Second from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Second(DateTime? value)
    {
        return value?.Second;
    }

    /// <summary>
    ///     Gets the second from DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Second from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Second(DateTimeOffset? value)
    {
        return value?.Second;
    }

    /// <summary>
    ///     Gets the millisecond from DateTime
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Millisecond from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Milliseconds(DateTime? value)
    {
        return value?.Millisecond;
    }

    /// <summary>
    ///     Gets the millisecond from DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Millisecond from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? Milliseconds(DateTimeOffset? value)
    {
        return value?.Millisecond;
    }

    /// <summary>
    ///     Gets the day of week from DateTime
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Day of week from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? DayOfWeek(DateTime? value)
    {
        return (int?)value?.DayOfWeek;
    }

    /// <summary>
    ///     Gets the day of week from DateTimeOffset
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Day of week from a given date</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public int? DayOfWeek(DateTimeOffset? value)
    {
        return (int?)value?.DayOfWeek;
    }
}
