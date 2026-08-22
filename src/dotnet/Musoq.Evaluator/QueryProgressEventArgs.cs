namespace Musoq.Evaluator;

/// <summary>
///     A synchronous, monotonic query progress snapshot.
/// </summary>
public sealed class QueryProgressEventArgs : EventArgs
{
    public QueryProgressEventArgs(
        string queryId,
        string? sourceContextId,
        long queryRowsProcessed,
        long? sourceRowsProcessed,
        long sequence,
        bool isFinal)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(queryRowsProcessed);
        ArgumentOutOfRangeException.ThrowIfNegative(sequence);
        if (sourceRowsProcessed < 0)
            throw new ArgumentOutOfRangeException(nameof(sourceRowsProcessed));

        QueryId = queryId;
        SourceContextId = sourceContextId;
        QueryRowsProcessed = queryRowsProcessed;
        SourceRowsProcessed = sourceRowsProcessed;
        Sequence = sequence;
        IsFinal = isFinal;
    }

    public string QueryId { get; }

    public string? SourceContextId { get; }

    public long QueryRowsProcessed { get; }

    public long? SourceRowsProcessed { get; }

    public long Sequence { get; }

    public bool IsFinal { get; }

    /// <summary>
    ///     Compatibility-friendly alias for the query-wide consumed-row count.
    /// </summary>
    public long RowsProcessed => QueryRowsProcessed;
}
