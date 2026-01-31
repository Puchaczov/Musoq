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
