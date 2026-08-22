using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Musoq.Schema;

namespace Musoq.Evaluator;

internal sealed record EvaluatorQueryRuntimeBinding(
    ISchemaProvider Provider,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId,
    IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId,
    IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans,
    ILogger Logger)
{
    internal static EvaluatorQueryRuntimeBinding Capture(IQueryRunnable runnable)
    {
        ArgumentNullException.ThrowIfNull(runnable);

        return new EvaluatorQueryRuntimeBinding(
            runnable.Provider,
            FreezeNestedStringDictionary(runnable.SourceRuntimeSettingsBySourceContextId),
            FreezeNestedList(runnable.SourceRuntimeSettingDescriptionsBySourceContextId),
            new ReadOnlyDictionary<string, SourceExecutionPlan>(
                new Dictionary<string, SourceExecutionPlan>(runnable.SourceExecutionPlans, StringComparer.Ordinal)),
            runnable.Logger);
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> FreezeNestedStringDictionary(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> values)
    {
        return new ReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>(
            values.ToDictionary(
                static item => item.Key,
                static item => (IReadOnlyDictionary<string, string>)new ReadOnlyDictionary<string, string>(
                    new Dictionary<string, string>(item.Value, StringComparer.Ordinal)),
                StringComparer.Ordinal));
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<T>> FreezeNestedList<T>(
        IReadOnlyDictionary<string, IReadOnlyList<T>> values)
    {
        return new ReadOnlyDictionary<string, IReadOnlyList<T>>(
            values.ToDictionary(
                static item => item.Key,
                static item => (IReadOnlyList<T>)Array.AsReadOnly(item.Value.ToArray()),
                StringComparer.Ordinal));
    }
}

internal sealed record QueryExecutionContext(
    EvaluatorQueryRuntimeBinding Binding,
    IReadOnlyDictionary<string, object?> Parameters,
    CancellationToken CancellationToken,
    QueryPhaseEventHandler? PhaseChanged,
    DataSourceEventHandler? DataSourceProgress,
    QueryProgressEventHandler? QueryProgress,
    QueryProgressOptions? QueryProgressOptions,
    object Sender,
    string QueryId)
{
    internal static QueryExecutionContext Capture(
        IQueryRunnable runnable,
        IEnumerable<KeyValuePair<string, object?>> parameters,
        CancellationToken cancellationToken,
        QueryPhaseEventHandler? phaseChanged = null,
        DataSourceEventHandler? dataSourceProgress = null,
        QueryProgressEventHandler? queryProgress = null,
        QueryProgressOptions? queryProgressOptions = null,
        object? sender = null,
        string? queryId = null)
    {
        ArgumentNullException.ThrowIfNull(runnable);
        ArgumentNullException.ThrowIfNull(parameters);

        return new QueryExecutionContext(
            EvaluatorQueryRuntimeBinding.Capture(runnable),
            ParameterSnapshot.CaptureReadOnlyOrEmpty(parameters),
            cancellationToken,
            phaseChanged,
            dataSourceProgress,
            queryProgress,
            queryProgressOptions,
            sender ?? runnable,
            queryId ?? runnable.GetType().FullName ?? string.Empty);
    }

    internal QueryRunContext ToRunContext()
    {
        return QueryRunContext.Create(
            Binding,
            Parameters,
            CancellationToken,
            PhaseChanged,
            DataSourceProgress,
            QueryProgress,
            QueryProgressOptions,
            Sender,
            QueryId);
    }
}
