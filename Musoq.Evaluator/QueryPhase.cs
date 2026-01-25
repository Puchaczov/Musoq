namespace Musoq.Evaluator;

/// <summary>
///     Represents the execution phase of a query.
/// </summary>
public enum QueryPhase
{
    /// <summary>
    ///     Query execution is beginning.
    /// </summary>
    Begin,

    /// <summary>
    ///     Executing the FROM clause - data source initialization.
    /// </summary>
    From,

    /// <summary>
    ///     Executing the WHERE clause - filtering rows.
    /// </summary>
    Where,

    /// <summary>
    ///     Executing the GROUP BY clause - grouping rows.
    /// </summary>
    GroupBy,

    /// <summary>
    ///     Executing the SELECT clause - projecting results.
    /// </summary>
    Select,

    /// <summary>
    ///     Query execution has completed.
    /// </summary>
    End
}
