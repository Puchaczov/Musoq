namespace Musoq.Evaluator;

/// <summary>
///     Event arguments for query phase change events.
/// </summary>
public class QueryPhaseEventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="QueryPhaseEventArgs" /> class.
    /// </summary>
    /// <param name="queryId">The unique identifier of the query.</param>
    /// <param name="phase">The current execution phase.</param>
    public QueryPhaseEventArgs(string queryId, QueryPhase phase)
    {
        QueryId = queryId;
        Phase = phase;
    }

    /// <summary>
    ///     Gets the unique identifier of the query being executed.
    /// </summary>
    public string QueryId { get; }

    /// <summary>
    ///     Gets the current execution phase.
    /// </summary>
    public QueryPhase Phase { get; }
}