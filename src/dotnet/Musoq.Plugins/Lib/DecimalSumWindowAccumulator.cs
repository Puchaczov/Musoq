namespace Musoq.Plugins;

/// <summary>
///     Decimal sum accumulator used by the built-in window aggregate capability descriptor.
/// </summary>
/// <typeparam name="TInput">Numeric input type.</typeparam>
public sealed class DecimalSumWindowAccumulator<TInput> : IWindowRetractableAccumulator<TInput, decimal>
{
    private decimal _sum;

    /// <inheritdoc />
    public void Reset() => _sum = 0m;

    /// <inheritdoc />
    public void Accumulate(TInput value)
    {
        if (value is not null)
            _sum += Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public void Retract(TInput value)
    {
        if (value is not null)
            _sum -= Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public decimal GetValue() => _sum;
}
