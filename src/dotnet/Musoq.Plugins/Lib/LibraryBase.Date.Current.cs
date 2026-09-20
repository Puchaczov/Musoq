using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    internal virtual DateTimeOffset CurrentLocalDateTime => DateTimeOffset.Now;

    internal virtual DateTimeOffset CurrentUtcDateTime => DateTimeOffset.UtcNow;

    /// <summary>
    ///     Gets the current datetime
    /// </summary>
    /// <returns>Current datetime</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    [NonDeterministic]
    public DateTimeOffset? GetDate()
    {
        return CurrentLocalDateTime;
    }

    /// <summary>
    ///     Gets the current datetime in UTC
    /// </summary>
    /// <returns>Current datetime in UTC</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DateTime)]
    [NonDeterministic]
    public DateTimeOffset? UtcGetDate()
    {
        return CurrentUtcDateTime;
    }
}
