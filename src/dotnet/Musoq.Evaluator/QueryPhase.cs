namespace Musoq.Evaluator;

/// <summary>
///     Represents a once-only execution entry marker for a query.
/// </summary>
public enum QueryPhase
{
    /// <summary>
    ///     Query execution or deferred enumeration is beginning.
    /// </summary>
    Begin,

    /// <summary>
    ///     Source setup or source enumeration is beginning.
    /// </summary>
    From,

    /// <summary>
    ///     The SQL WHERE predicate is beginning to execute.
    /// </summary>
    Where,

    /// <summary>
    ///     SQL grouping is beginning. Aggregate-only queries do not emit this marker.
    /// </summary>
    GroupBy,

    /// <summary>
    ///     Final projection or output production is beginning.
    /// </summary>
    Select,

    /// <summary>
    ///     The query scope has terminated.
    /// </summary>
    End
}
