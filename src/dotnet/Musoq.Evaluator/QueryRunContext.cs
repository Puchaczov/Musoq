using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
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
        string? queryId = null,
        ISchemaProvider? provider = null,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? sourceRuntimeSettingsBySourceContextId = null,
        IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>? sourceRuntimeSettingDescriptionsBySourceContextId = null,
        IReadOnlyDictionary<string, SourceExecutionPlan>? sourceExecutionPlans = null,
        ILogger? logger = null)
    {
        CancellationToken = cancellationToken;
        RuntimeParameters = ParameterSnapshot.CaptureReadOnlyOrEmpty(parameters);
        PhaseChanged = phaseChanged;
        DataSourceProgress = dataSourceProgress;
        Sender = sender ?? this;
        QueryId = queryId ?? string.Empty;
        Provider = provider;
        SourceRuntimeSettingsBySourceContextId = sourceRuntimeSettingsBySourceContextId ??
            new Dictionary<string, IReadOnlyDictionary<string, string>>();
        SourceRuntimeSettingDescriptionsBySourceContextId = sourceRuntimeSettingDescriptionsBySourceContextId ??
            new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>();
        SourceExecutionPlans = sourceExecutionPlans ?? new Dictionary<string, SourceExecutionPlan>();
        Logger = logger;
    }

    public CancellationToken CancellationToken { get; }

    public IReadOnlyDictionary<string, object?> RuntimeParameters { get; }

    public QueryPhaseEventHandler? PhaseChanged { get; }

    public DataSourceEventHandler? DataSourceProgress { get; }

    public object Sender { get; }

    public string QueryId { get; }

    public ISchemaProvider? Provider { get; }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; }

    public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; }

    public ILogger? Logger { get; }

    public static QueryRunContext Capture(
        TypedQueryRunOptions options,
        object? sender = null,
        string? queryId = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (sender is IQueryRunnable runnable)
        {
            var binding = EvaluatorQueryRuntimeBinding.Capture(runnable);
            return Create(
                binding,
                ParameterSnapshot.CaptureReadOnlyOrEmpty(options.Parameters),
                options.CancellationToken,
                options.PhaseChanged,
                options.DataSourceProgress,
                sender,
                queryId ?? runnable.GetType().FullName ?? string.Empty);
        }

        return new QueryRunContext(
            options.CancellationToken,
            options.Parameters,
            options.PhaseChanged,
            options.DataSourceProgress,
            sender,
            queryId);
    }

    internal static QueryRunContext Create(
        EvaluatorQueryRuntimeBinding binding,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken,
        QueryPhaseEventHandler? phaseChanged,
        DataSourceEventHandler? dataSourceProgress,
        object sender,
        string queryId)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(queryId);

        return new QueryRunContext(
            cancellationToken,
            parameters,
            phaseChanged,
            dataSourceProgress,
            sender,
            queryId,
            binding.Provider,
            binding.SourceRuntimeSettingsBySourceContextId,
            binding.SourceRuntimeSettingDescriptionsBySourceContextId,
            binding.SourceExecutionPlans,
            binding.Logger);
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
