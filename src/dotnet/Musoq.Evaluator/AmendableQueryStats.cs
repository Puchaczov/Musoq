using System.Threading;
using Musoq.Plugins;

namespace Musoq.Evaluator;

public class AmendableQueryStats : QueryStats
{
    public AmendableQueryStats()
    {
        InternalRowNumber = 0;
    }

    /// <summary>
    ///     Increments the row number and returns a lightweight struct snapshot.
    ///     This avoids allocating a new object for every row processed.
    /// </summary>
    public QueryStatsSnapshot IncrementRowNumber()
    {
        var value = Interlocked.Increment(ref InternalRowNumber);
        return new QueryStatsSnapshot(value);
    }
}

/// <summary>
///     A lightweight struct snapshot of query stats to avoid heap allocations in hot paths.
/// </summary>
public readonly struct QueryStatsSnapshot : IQueryStats
{
    public QueryStatsSnapshot(int rowNumber)
    {
        RowNumber = rowNumber;
    }

    public int RowNumber { get; }
}
