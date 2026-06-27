using System.Collections.Generic;
using System.Threading;
using Musoq.Schema;

namespace Musoq.Evaluator;

public sealed class QueryRunContext
{
    public QueryRunContext(
        CancellationToken cancellationToken,
        IEnumerable<KeyValuePair<string, object?>>? parameters = null,
        QueryPhaseEventHandler? phaseChanged = null,
        DataSourceEventHandler? dataSourceProgress = null,
        object? sender = null,
        string? queryId = null)
    {
        CancellationToken = cancellationToken;
        RuntimeParameters = ParameterSnapshot.CaptureReadOnlyOrEmpty(parameters);
        PhaseChanged = phaseChanged;
        DataSourceProgress = dataSourceProgress;
        Sender = sender ?? this;
        QueryId = queryId ?? string.Empty;
    }

    public CancellationToken CancellationToken { get; }

    public IReadOnlyDictionary<string, object?> RuntimeParameters { get; }

    public QueryPhaseEventHandler? PhaseChanged { get; }

    public DataSourceEventHandler? DataSourceProgress { get; }

    public object Sender { get; }

    public string QueryId { get; }

    public static QueryRunContext Capture(
        TypedQueryRunOptions options,
        object? sender = null,
        string? queryId = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new QueryRunContext(
            options.CancellationToken,
            options.Parameters,
            options.PhaseChanged,
            options.DataSourceProgress,
            sender,
            queryId);
    }

    public void ThrowIfCancellationRequested()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }

    public void NotifyPhaseChanged(QueryPhase phase)
    {
        NotifyPhaseChanged(QueryId, phase);
    }

    public void NotifyPhaseChanged(string queryId, QueryPhase phase)
    {
        PhaseChanged?.Invoke(Sender, new QueryPhaseEventArgs(queryId, phase));
    }

    public void NotifyDataSourceProgress(DataSourceEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        DataSourceProgress?.Invoke(Sender, args);
    }

    public void NotifyDataSourceProgress(object? sender, DataSourceEventArgs args)
    {
        NotifyDataSourceProgress(args);
    }
}
