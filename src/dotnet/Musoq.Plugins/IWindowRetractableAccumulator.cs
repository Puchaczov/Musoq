namespace Musoq.Plugins;

/// <summary>
///     Accumulator contract for bounded ROWS frames that can remove values from the current state.
/// </summary>
/// <typeparam name="TInput">Input value type.</typeparam>
/// <typeparam name="TResult">Aggregate result type.</typeparam>
public interface IWindowRetractableAccumulator<in TInput, out TResult> : IWindowAccumulator<TInput, TResult>
{
    /// <summary>Removes a value from the current state.</summary>
    /// <param name="value">Value to retract.</param>
    void Retract(TInput value);
}
