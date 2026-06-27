namespace Musoq.Plugins;

/// <summary>
///     Identifies the aggregate operation represented by a capability descriptor.
/// </summary>
public enum WindowAggregateFunction
{
    /// <summary>Sum aggregate.</summary>
    Sum,

    /// <summary>Count aggregate.</summary>
    Count,

    /// <summary>Average aggregate.</summary>
    Avg,

    /// <summary>Minimum aggregate.</summary>
    Min,

    /// <summary>Maximum aggregate.</summary>
    Max
}
