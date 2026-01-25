namespace Musoq.Plugins;

/// <summary>
///     Interface for query statistics to enable struct-based implementations without allocations.
/// </summary>
public interface IQueryStats
{
    /// <summary>
    ///     Number of rows in the result set.
    /// </summary>
    int RowNumber { get; }
}
