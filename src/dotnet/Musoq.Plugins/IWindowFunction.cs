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
