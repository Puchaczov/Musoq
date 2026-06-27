using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Returns the start of the day (midnight) for the given date
    /// </summary>
    /// <param name="date">The date</param>
    /// <returns>The start of the day (00:00:00)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    public DateTime? StartOfDay(DateTime? date)
    {
        return date?.Date;
    }

    /// <summary>
    ///     Returns the start of the day (midnight) for the given date
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
    ///     Returns the end of the day (23:59:59.9999999) for the given date
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
    ///     Returns the end of the day (23:59:59.9999999) for the given date
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
}
