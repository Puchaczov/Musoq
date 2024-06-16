namespace Musoq.Plugins;

/// <summary>
/// Represents statistics of the query.
/// </summary>
public class QueryStats
{
    /// <summary>
    /// Number of rows in the result set.
    /// </summary>
    public int RowNumber { get; protected set; }
}