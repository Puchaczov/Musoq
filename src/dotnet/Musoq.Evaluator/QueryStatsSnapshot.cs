using Musoq.Plugins;

namespace Musoq.Evaluator;

/// <summary>
///     A lightweight struct snapshot of query stats to avoid heap allocations in hot paths.
/// </summary>
public readonly record struct QueryStatsSnapshot(int RowNumber) : IQueryStats;
