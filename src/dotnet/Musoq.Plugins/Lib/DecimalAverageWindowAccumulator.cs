namespace Musoq.Plugins;

/// <summary>
///     Decimal average accumulator used by the built-in window aggregate capability descriptor.
/// </summary>
/// <typeparam name="TInput">Numeric input type.</typeparam>
public sealed class DecimalAverageWindowAccumulator<TInput> : IWindowRetractableAccumulator<TInput, decimal>
{
    private decimal _sum;
    private int _count;

    /// <inheritdoc />
    public void Reset()
    {
        _sum = 0m;
        _count = 0;
    }

    /// <inheritdoc />
    public void Accumulate(TInput value)
    {
        if (value is not null)
        {
            _sum += Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture);
            _count++;
        }
    }

    /// <inheritdoc />
    public void Retract(TInput value)
    {
        if (value is not null)
        {
            _sum -= Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture);
            _count--;
        }
    }

    /// <inheritdoc />
    public decimal GetValue() => _count > 0 ? _sum / _count : 0m;
}
