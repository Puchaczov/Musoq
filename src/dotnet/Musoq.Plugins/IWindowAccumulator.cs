namespace Musoq.Plugins;

/// <summary>
///     Accumulates typed window aggregate input values and exposes a typed result.
/// </summary>
/// <typeparam name="TInput">Input value type.</typeparam>
/// <typeparam name="TResult">Aggregate result type.</typeparam>
public interface IWindowAccumulator<in TInput, out TResult>
{
    /// <summary>Resets the accumulator before a new partition or frame is evaluated.</summary>
    void Reset();

    /// <summary>Accumulates a value into the current state.</summary>
    /// <param name="value">Value to accumulate.</param>
    void Accumulate(TInput value);

    /// <summary>Gets the current aggregate result.</summary>
    /// <returns>The current aggregate result.</returns>
    TResult GetValue();
}
