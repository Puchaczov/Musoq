namespace Musoq.Plugins;

/// <summary>
///     Describes optional execution capabilities advertised by a window aggregate factory.
/// </summary>
[Flags]
public enum WindowAggregateCapabilities
{
    /// <summary>No optimized aggregate capability is advertised.</summary>
    None = 0,

    /// <summary>The function can compute one value for an entire partition.</summary>
    WholePartition = 1,

    /// <summary>The function can compute values by accumulating rows in window order.</summary>
    Running = 1 << 1,

    /// <summary>The function can evaluate bounded ROWS frames.</summary>
    BoundedRows = 1 << 2,

    /// <summary>The function supports a statically typed input value.</summary>
    TypedInput = 1 << 3,

    /// <summary>The function produces a statically typed result value.</summary>
    TypedResult = 1 << 4,

    /// <summary>The function can merge independent accumulator states.</summary>
    Merge = 1 << 5
}
