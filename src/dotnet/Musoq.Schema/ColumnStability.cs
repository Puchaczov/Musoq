namespace Musoq.Schema;

/// <summary>
///     Describes whether a value may be evaluated once for the lifetime of its bound row.
/// </summary>
public enum ColumnStability
{
    /// <summary>
    ///     The value is repeatable and side-effect-free for the lifetime of the bound row.
    /// </summary>
    Stable,

    /// <summary>
    ///     The value may change, have observable access effects, or depend on invocation timing.
    /// </summary>
    Volatile
}
