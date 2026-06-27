using System.Collections.Generic;

namespace Musoq.Plugins;

/// <summary>
///     Maximum accumulator used by the built-in window aggregate capability descriptor.
/// </summary>
/// <typeparam name="TInput">Comparable input type.</typeparam>
/// <typeparam name="TResult">Nullable aggregate result type.</typeparam>
public sealed class MaxWindowAccumulator<TInput, TResult> : IWindowRetractableAccumulator<TInput, TResult>
{
    private readonly List<TInput> _values = [];

    /// <inheritdoc />
    public void Reset() => _values.Clear();

    /// <inheritdoc />
    public void Accumulate(TInput value)
    {
        if (value is not null)
            _values.Add(value);
    }

    /// <inheritdoc />
    public void Retract(TInput value)
    {
        if (value is not null)
            _values.Remove(value);
    }

    /// <inheritdoc />
    public TResult GetValue() => WindowMinMaxAccumulatorCore.GetExtremeValue<TInput, TResult>(_values, compareLessThan: false);
}
