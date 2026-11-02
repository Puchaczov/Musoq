using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Musoq.Schema;

namespace Musoq.Evaluator;

public sealed class QueryRunContext
{
    private static readonly QueryProgressOptions DefaultQueryProgressOptions = new();

    public QueryRunContext(
        CancellationToken cancellationToken,
        TypedParameterSnapshot? parameters = null,
        QueryPhaseEventHandler? phaseChanged = null,
        DataSourceEventHandler? dataSourceProgress = null,
        object? sender = null,
        string? queryId = null,
        ISchemaProvider? provider = null,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? sourceRuntimeSettingsBySourceContextId = null,
        IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>? sourceRuntimeSettingDescriptionsBySourceContextId = null,
        IReadOnlyDictionary<string, SourceExecutionPlan>? sourceExecutionPlans = null,
        ILogger? logger = null,
        QueryProgressEventHandler? queryProgress = null,
        QueryProgressOptions? queryProgressOptions = null)
    {
        CancellationToken = cancellationToken;
        _typedParameters = parameters ?? TypedParameterSnapshot.Empty;
        PhaseChanged = phaseChanged;
        DataSourceProgress = dataSourceProgress;
        QueryProgress = queryProgress;
        _queryProgressOptions = queryProgressOptions ?? DefaultQueryProgressOptions;
        Sender = sender ?? this;
        QueryId = queryId ?? string.Empty;
        _provider = provider;
        SourceRuntimeSettingsBySourceContextId = sourceRuntimeSettingsBySourceContextId ??
            new Dictionary<string, IReadOnlyDictionary<string, string>>();
        SourceRuntimeSettingDescriptionsBySourceContextId = sourceRuntimeSettingDescriptionsBySourceContextId ??
            new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>();
        SourceExecutionPlans = sourceExecutionPlans ?? new Dictionary<string, SourceExecutionPlan>();
        Logger = logger;
    }

    public CancellationToken CancellationToken { get; }

    /// <summary>Gets the execution's concrete parameter slots.</summary>
    private TypedParameterSnapshot _typedParameters = TypedParameterSnapshot.Empty;
    public TypedParameterSnapshot TypedParameters { get => _typedParameters; init => _typedParameters = value; }

    public QueryPhaseEventHandler? PhaseChanged { get; }

    public DataSourceEventHandler? DataSourceProgress { get; }

    public QueryProgressEventHandler? QueryProgress { get; }

    public QueryProgressOptions QueryProgressOptions => _queryProgressOptions;

    public object Sender { get; }

    public string QueryId { get; }

    private readonly ISchemaProvider? _provider;
    private ISchemaProvider? _reportingProvider;
    public ISchemaProvider? Provider
    {
        get
        {
            if (_provider is null) return null;
            var existing = Volatile.Read(ref _reportingProvider);
            if (existing is not null) return existing;
            return Interlocked.CompareExchange(ref _reportingProvider, DataSourceWarnings.WrapProvider(_provider), null) ?? _reportingProvider;
        }
    }

    /// <summary>Warnings are isolated to this query execution.</summary>
    public Musoq.Schema.Diagnostics.DataSourceWarningCollector DataSourceWarnings { get; init; } = new();

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
            var captured = Create(
                binding,
                options.TypedParameters,
                options.CancellationToken,
                options.PhaseChanged,
                options.DataSourceProgress,
                options.QueryProgress,
                options.QueryProgressOptions,
                sender,
                queryId ?? runnable.GetType().FullName ?? string.Empty,
                options.DataSourceWarnings);
            captured._typedParameters = options.TypedParameters;
            return captured;
        }

        return new QueryRunContext(
            options.CancellationToken,
            options.TypedParameters,
            options.PhaseChanged,
            options.DataSourceProgress,
            sender,
            queryId,
            queryProgress: options.QueryProgress,
            queryProgressOptions: options.QueryProgressOptions)
        {
            TypedParameters = options.TypedParameters,
            DataSourceWarnings = options.DataSourceWarnings ?? new()
        };
    }

    internal static QueryRunContext Create(
        EvaluatorQueryRuntimeBinding binding,
        TypedParameterSnapshot parameters,
        CancellationToken cancellationToken,
        QueryPhaseEventHandler? phaseChanged,
        DataSourceEventHandler? dataSourceProgress,
        QueryProgressEventHandler? queryProgress,
        QueryProgressOptions? queryProgressOptions,
        object sender,
        string queryId,
        Musoq.Schema.Diagnostics.DataSourceWarningCollector? dataSourceWarnings = null)
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
            binding.Logger,
            queryProgress,
            queryProgressOptions)
        { DataSourceWarnings = dataSourceWarnings ?? new() };
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

    public void CompleteQueryProgress()
    {
        if (QueryProgress is not { } handler)
            return;

        GetOrCreateProgressReporter(handler).Complete();
    }

    internal IEnumerable<IReadOnlyList<T>> CreateProgressChunks<T>(
        IEnumerable<IReadOnlyList<T>> chunks,
        string sourceContextId)
    {
        if (QueryProgress is not { } handler)
            return chunks;

        return new QueryProgressChunkEnumerable<T>(
            chunks,
            GetOrCreateProgressReporter(handler).CreateSource(sourceContextId));
    }

    private QueryProgressReporter GetOrCreateProgressReporter(QueryProgressEventHandler handler)
    {
        var reporter = Volatile.Read(ref _progressReporter);
        if (reporter != null)
            return reporter;

        var created = new QueryProgressReporter(QueryId, Sender, handler, QueryProgressOptions);
        return Interlocked.CompareExchange(ref _progressReporter, created, null) ?? created;
    }

    private QueryProgressReporter? _progressReporter;
    private readonly QueryProgressOptions _queryProgressOptions;
}
