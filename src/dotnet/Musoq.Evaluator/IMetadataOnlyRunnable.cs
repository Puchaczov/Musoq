namespace Musoq.Evaluator;

/// <summary>
///     Marks a generated runnable whose result is derived entirely from compile-time
///     metadata and therefore must not require host parameter values.
/// </summary>
public interface IMetadataOnlyRunnable
{
    /// <summary>
    ///     Gets a value indicating whether this runnable performs metadata-only work.
    /// </summary>
    bool IsMetadataOnly => true;
}