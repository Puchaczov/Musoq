namespace Musoq.Plugins;

/// <summary>
///     Represents statistics of the query.
/// </summary>
public class QueryStats
{
    /// <summary>
    ///     Number of rows in the result set.
    /// </summary>
    protected int InternalRowNumber;

    /// <summary>
    ///     Number of rows in the result set.
    /// </summary>
    public int RowNumber
    {
        get => InternalRowNumber;
        protected set => InternalRowNumber = value;
    }
}