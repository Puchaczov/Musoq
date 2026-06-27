namespace Musoq.Plugins;

/// <summary>
///     Describes an optimized window aggregate capability exposed by a plugin factory.
/// </summary>
/// <param name="Function">The aggregate operation.</param>
/// <param name="Capabilities">The supported evaluation modes and type traits.</param>
/// <param name="InputType">The supported input type.</param>
/// <param name="ResultType">The supported result type.</param>
/// <param name="AccumulatorType">The accumulator contract type that implements the capability.</param>
public sealed record WindowAggregateCapability(
    WindowAggregateFunction Function,
    WindowAggregateCapabilities Capabilities,
    Type InputType,
    Type ResultType,
    Type AccumulatorType);
