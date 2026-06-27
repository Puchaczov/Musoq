namespace Musoq.Plugins;

/// <summary>
///     Count accumulator used by the built-in window aggregate capability descriptor.
/// </summary>
/// <typeparam name="TInput">Input type.</typeparam>
public sealed class CountWindowAccumulator<TInput> : IWindowRetractableAccumulator<TInput, int>
{
    private int _count;

    /// <inheritdoc />
    public void Reset() => _count = 0;

    /// <inheritdoc />
    public void Accumulate(TInput value)
    {
        if (value is not null)
            _count++;
    }

    /// <inheritdoc />
    public void Retract(TInput value)
    {
        if (value is not null)
            _count--;
    }

    /// <inheritdoc />
    public int GetValue() => _count;
}
