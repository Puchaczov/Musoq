using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Creates a window function that assigns a sequential number to each row within a partition.
    /// </summary>
    [WindowFunction(Name = "RowNumber")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, long> WindowRowNumber()
    {
        return new RowNumberWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that assigns a rank to each row, with gaps for ties.
    ///     Receives the ORDER BY key via accumulate to detect ties.
    /// </summary>
    [WindowFunction(Name = "Rank")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, long> WindowRank()
    {
        return new RankWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that assigns a dense rank to each row, without gaps for ties.
    ///     Receives the ORDER BY key via accumulate to detect ties.
    /// </summary>
    [WindowFunction(Name = "DenseRank")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, long> WindowDenseRank()
    {
        return new DenseRankWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that distributes rows into a specified number of roughly equal groups.
    ///     Receives the bucket count via accumulate (from the SQL argument).
    /// </summary>
    [WindowFunction(Name = "Ntile")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, long> WindowNtile()
    {
        return new NtileWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that computes a running or whole-partition sum.
    /// </summary>
    [WindowFunction(Name = "Sum", CapabilityProviderType = typeof(LibraryBaseWindowAggregateCapabilityProvider))]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, decimal> WindowSum()
    {
        return new SumWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that computes a running or whole-partition count of non-null values.
    /// </summary>
    [WindowFunction(Name = "Count", CapabilityProviderType = typeof(LibraryBaseWindowAggregateCapabilityProvider))]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, int> WindowCount()
    {
        return new CountWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that computes a running or whole-partition average.
    /// </summary>
    [WindowFunction(Name = "Avg", CapabilityProviderType = typeof(LibraryBaseWindowAggregateCapabilityProvider))]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, decimal> WindowAvg()
    {
        return new AvgWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that computes a running or whole-partition minimum.
    /// </summary>
    [WindowFunction(Name = "Min", CapabilityProviderType = typeof(LibraryBaseWindowAggregateCapabilityProvider))]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, object?> WindowMin()
    {
        return new MinWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that computes a running or whole-partition maximum.
    /// </summary>
    [WindowFunction(Name = "Max", CapabilityProviderType = typeof(LibraryBaseWindowAggregateCapabilityProvider))]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, object?> WindowMax()
    {
        return new MaxWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that returns the first value in each partition.
    /// </summary>
    [WindowFunction(Name = "FirstValue")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, object?> WindowFirstValue()
    {
        return new FirstValueWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that returns the last value seen so far (running)
    ///     or the last value in the partition (unordered).
    /// </summary>
    [WindowFunction(Name = "LastValue")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, object?> WindowLastValue()
    {
        return new LastValueWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that returns the nth value in each partition.
    ///     The position argument is passed via <see cref="IWindowFunction.SetArguments"/>.
    /// </summary>
    [WindowFunction(Name = "NthValue")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, object?> WindowNthValue()
    {
        return new NthValueWindowFunction();
    }
}
