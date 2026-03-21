namespace Musoq.Plugins;

/// <summary>
///     Non-generic base interface for plugin-provided window functions.
///     Used by the engine at runtime to interact with window function instances
///     without knowing the concrete type parameters.
/// </summary>
public interface IWindowFunction
{
    /// <summary>
    ///     Called when a new partition begins. Must fully reset all internal state.
    /// </summary>
    void PartitionStart();

    /// <summary>
    ///     Called before <see cref="PartitionStart"/> with the number of rows in the partition.
    ///     Override for functions that need the partition size (e.g. NTILE).
    ///     Default implementation does nothing.
    /// </summary>
    /// <param name="size">The number of rows in the current partition.</param>
    void SetPartitionSize(int size) { }

    /// <summary>
    ///     Called once before partition processing with any extra SQL arguments
    ///     beyond the value column (e.g. the <c>n</c> in <c>NthValue(col, n)</c>).
    ///     Default implementation does nothing.
    /// </summary>
    /// <param name="args">Extra arguments from the SQL function call.</param>
    void SetArguments(object?[] args) { }

    /// <summary>
    ///     Called for each row in partition order. Accumulates the input value into internal state.
    /// </summary>
    /// <param name="value">The input value for the current row (boxed).</param>
    void AccumulateValue(object? value);

    /// <summary>
    ///     Returns the window function result for the current row.
    ///     Called after <see cref="AccumulateValue"/> for each row.
    /// </summary>
    object? GetCurrentValue();
}

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
