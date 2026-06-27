using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts a Unix timestamp (seconds since epoch) to DateTime.
    /// </summary>
    /// <param name="unixTimestamp">The Unix timestamp</param>
    /// <returns>The DateTime in UTC</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public DateTime? UnixToDateTime(long? unixTimestamp)
    {
        if (!unixTimestamp.HasValue)
            return null;

        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp.Value).UtcDateTime;
    }

    /// <summary>
    ///     Converts a Unix timestamp in milliseconds to DateTime.
    /// </summary>
    /// <param name="unixTimestampMs">The Unix timestamp in milliseconds</param>
    /// <returns>The DateTime in UTC</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public DateTime? UnixMillisToDateTime(long? unixTimestampMs)
    {
        if (!unixTimestampMs.HasValue)
            return null;

        return DateTimeOffset.FromUnixTimeMilliseconds(unixTimestampMs.Value).UtcDateTime;
    }

    /// <summary>
    ///     Converts a DateTime to Unix timestamp (seconds since epoch).
    /// </summary>
    /// <param name="dateTime">The DateTime</param>
    /// <returns>The Unix timestamp</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public long? DateTimeToUnix(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return null;

        return new DateTimeOffset(dateTime.Value).ToUnixTimeSeconds();
    }

    /// <summary>
    ///     Converts a DateTime to Unix timestamp in milliseconds.
    /// </summary>
    /// <param name="dateTime">The DateTime</param>
    /// <returns>The Unix timestamp in milliseconds</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public long? DateTimeToUnixMillis(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return null;

        return new DateTimeOffset(dateTime.Value).ToUnixTimeMilliseconds();
    }

    /// <summary>
    ///     Converts a Unix timestamp (seconds since epoch) to DateTimeOffset.
    /// </summary>
    /// <param name="unixTimestamp">The Unix timestamp</param>
    /// <returns>The DateTimeOffset in UTC</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public DateTimeOffset? UnixToDateTimeOffset(long? unixTimestamp)
    {
        if (!unixTimestamp.HasValue)
            return null;

        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp.Value);
    }

    /// <summary>
    ///     Converts a Unix timestamp in milliseconds to DateTimeOffset.
    /// </summary>
    /// <param name="unixTimestampMs">The Unix timestamp in milliseconds</param>
    /// <returns>The DateTimeOffset in UTC</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public DateTimeOffset? UnixMillisToDateTimeOffset(long? unixTimestampMs)
    {
        if (!unixTimestampMs.HasValue)
            return null;

        return DateTimeOffset.FromUnixTimeMilliseconds(unixTimestampMs.Value);
    }

    /// <summary>
    ///     Converts a DateTimeOffset to Unix timestamp (seconds since epoch).
    /// </summary>
    /// <param name="dateTimeOffset">The DateTimeOffset</param>
    /// <returns>The Unix timestamp</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public long? DateTimeOffsetToUnix(DateTimeOffset? dateTimeOffset)
    {
        if (!dateTimeOffset.HasValue)
            return null;

        return dateTimeOffset.Value.ToUnixTimeSeconds();
    }

    /// <summary>
    ///     Converts a DateTimeOffset to Unix timestamp in milliseconds.
    /// </summary>
    /// <param name="dateTimeOffset">The DateTimeOffset</param>
    /// <returns>The Unix timestamp in milliseconds</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public long? DateTimeOffsetToUnixMillis(DateTimeOffset? dateTimeOffset)
    {
        if (!dateTimeOffset.HasValue)
            return null;

        return dateTimeOffset.Value.ToUnixTimeMilliseconds();
    }
}
