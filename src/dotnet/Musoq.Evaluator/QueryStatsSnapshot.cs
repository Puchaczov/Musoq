using Musoq.Plugins;

namespace Musoq.Evaluator;

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
