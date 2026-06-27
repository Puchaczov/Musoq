using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Checks if a DateTime value is between the specified start and end dates (inclusive).
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
    ///     Checks if a DateTimeOffset value is between the specified start and end dates (inclusive).
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
    ///     Checks if a DateTime value is between the specified start and end dates (exclusive).
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
    ///     Checks if a DateTimeOffset value is between the specified start and end dates (exclusive).
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
