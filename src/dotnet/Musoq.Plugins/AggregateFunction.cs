namespace Musoq.Plugins;

/// <summary>
///     Provides helpers for SQL aggregate declaration stubs.
/// </summary>
public static class AggregateFunction
{
    /// <summary>
    ///     Marks an aggregate declaration method as metadata-only.
    /// </summary>
    /// <typeparam name="T">Declared aggregate result type.</typeparam>
    /// <returns>This method never returns when invoked directly.</returns>
    public static T NotInvoked<T>()
    {
        throw new InvalidOperationException(
            "Aggregate function declaration methods are metadata-only and must be lowered before execution.");
    }
}
