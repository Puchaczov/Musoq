namespace Musoq.Schema.Api;

/// <summary>
///     Provides query-level hints that data sources can use for optimization.
///     These hints represent values from SKIP, TAKE and other clauses that might
///     allow the data source to fetch fewer rows from external APIs.
/// </summary>
public class QueryHints
{
    /// <summary>
    ///     Gets the SKIP value if specified in the query, or null if not present.
    ///     Data sources can use this to implement server-side pagination.
    /// </summary>
    public long? SkipValue { get; init; }

    /// <summary>
    ///     Gets the TAKE value if specified in the query, or null if not present.
    ///     Data sources can use this to limit API requests to only fetch needed rows.
    /// </summary>
    public long? TakeValue { get; init; }

    /// <summary>
    ///     Gets whether the query contains a DISTINCT clause.
    ///     Data sources might use this for optimization decisions.
    /// </summary>
    public bool IsDistinct { get; init; }

    /// <summary>
    ///     Gets whether there are any query hints that might allow optimization.
    /// </summary>
    public bool HasOptimizationHints => SkipValue.HasValue || TakeValue.HasValue || IsDistinct;

    /// <summary>
    ///     Gets the effective maximum rows needed considering both skip and take.
    ///     Returns null if take is not specified.
    /// </summary>
    public long? EffectiveMaxRowsToFetch => TakeValue.HasValue
        ? TakeValue.Value + (SkipValue ?? 0)
        : null;

    /// <summary>
    ///     Creates an empty QueryHints with no optimization hints.
    /// </summary>
    public static QueryHints Empty { get; } = new();

    /// <summary>
    ///     Creates a new QueryHints with the specified skip and take values.
    /// </summary>
    public static QueryHints Create(long? skip = null, long? take = null, bool isDistinct = false)
    {
        return new QueryHints
        {
            SkipValue = skip,
            TakeValue = take,
            IsDistinct = isDistinct
        };
    }

    /// <summary>
    ///     Creates a new QueryHints with only skip and take values.
    /// </summary>
    public static QueryHints WithSkipAndTake(long? skip, long? take)
    {
        return new QueryHints
        {
            SkipValue = skip,
            TakeValue = take,
            IsDistinct = false
        };
    }

    /// <summary>
    ///     Creates a new QueryHints with only the distinct flag set.
    /// </summary>
    public static QueryHints WithDistinct()
    {
        return new QueryHints
        {
            SkipValue = null,
            TakeValue = null,
            IsDistinct = true
        };
    }
}
