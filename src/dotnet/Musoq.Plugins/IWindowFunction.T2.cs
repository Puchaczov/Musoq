namespace Musoq.Plugins;

/// <summary>
///     Generic interface for plugin-provided window functions.
///     A new instance is created per window function call site per query.
///     Implementations must be stateless across partitions — <see cref="IWindowFunction.PartitionStart"/>
///     must fully reset internal state.
/// </summary>
/// <typeparam name="TInput">Type of input value passed each row.</typeparam>
/// <typeparam name="TResult">Type of the computed result.</typeparam>
public interface IWindowFunction<in TInput, out TResult> : IWindowFunction
{
    /// <summary>
    ///     Called for each row in partition order.
    ///     Accumulates the typed input value into internal state.
    /// </summary>
    void Accumulate(TInput value);

    /// <summary>
    ///     Returns the typed window function result for the current row.
    ///     Called after <see cref="Accumulate"/> for each row.
    /// </summary>
    TResult GetValue();

    void IWindowFunction.AccumulateValue(object? value) => Accumulate((TInput)value!);

    object? IWindowFunction.GetCurrentValue() => GetValue();
}
