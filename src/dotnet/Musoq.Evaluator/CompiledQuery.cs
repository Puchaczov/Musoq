using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator;

[DebuggerStepThrough]
public class CompiledQuery
{
    private readonly ITableRunnable _runnable;
    private readonly IDictionary<string, object?> _fallbackParameters = new Dictionary<string, object?>(StringComparer.Ordinal);
    private readonly IParameterizedRunnable? _parameterizedRunnable;
    private readonly IReadOnlyList<ScriptParameterDefinition> _parameterDefinitions;
    private readonly IReadOnlyList<ScriptParameterDefinition> _requiredParameters;

    public CompiledQuery(ITableRunnable runnable)
    {
        _runnable = runnable ?? throw QueryExecutionException.ForNullRunnable();
        _parameterizedRunnable = runnable as IParameterizedRunnable;
        _parameterDefinitions = _parameterizedRunnable?.ParameterDefinitions.ToArray() ??
                                Array.Empty<ScriptParameterDefinition>();
        _requiredParameters = _parameterDefinitions.Where(definition => definition.IsRequired).ToArray();
    }

    public event QueryPhaseEventHandler PhaseChanged
    {
        add => _runnable.PhaseChanged += value;
        remove => _runnable.PhaseChanged -= value;
    }

    public event DataSourceEventHandler DataSourceProgress
    {
        add => _runnable.DataSourceProgress += value;
        remove => _runnable.DataSourceProgress -= value;
    }

    public IDictionary<string, object?> Parameters => _parameterizedRunnable?.Parameters ?? _fallbackParameters;

    public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions => _parameterDefinitions;

    public IReadOnlyList<ScriptParameterDefinition> RequiredParameters => _requiredParameters;

    public Table Run()
    {
        return Run(CancellationToken.None);
    }

    public Table Run(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            throw new OperationCanceledException("Query execution was cancelled before it started.", token);

        Table result;
        try
        {
            result = _runnable.Run(token);
            EnsurePublicResult(result);
        }
        catch (ScriptParameterBindingException ex)
        {
            throw QueryExecutionException.ForScriptParameterBinding(ex);
        }

        return result;
    }

    public QueryProfileResult RunWithProfile()
    {
        using var exitSourcesLoaderTokenSource = new CancellationTokenSource();

        var result = RunWithProfile(exitSourcesLoaderTokenSource.Token);
        exitSourcesLoaderTokenSource.Cancel();

        return result;
    }

    public QueryProfileResult RunWithProfile(CancellationToken token)
    {
        return RunWithProfile(token, emitTelemetry: true);
    }

    internal QueryProfileResult RunWithProfile(CancellationToken token, bool emitTelemetry)
    {
        if (token.IsCancellationRequested)
            throw new OperationCanceledException("Query execution was cancelled before it started.", token);

        if (_runnable is not IProfiledRunnable profiledRunnable)
            throw new InvalidOperationException("Query was not compiled with profiling instrumentation.");

        var recorder = new QueryProfileRecorder(queryId: _runnable.GetType().FullName);
        Table result;
        try
        {
            result = profiledRunnable.RunWithProfile(token, recorder);
            MaterializePublicResult(result);
        }
        catch (ScriptParameterBindingException ex)
        {
            throw QueryExecutionException.ForScriptParameterBinding(ex);
        }

        var profile = recorder.CreateSnapshot();
        if (emitTelemetry)
            QueryProfileTelemetry.Emit(profile);

        return new QueryProfileResult(result, profile, QueryProfileTextPrinter.Print(profile));
    }

    private static void MaterializePublicResult(Table result)
    {
        EnsurePublicResult(result);

        _ = result.Count;
    }

    private static void EnsurePublicResult(Table result)
    {
        if (result == null)
            throw new InvalidOperationException("Query execution returned null result.");
    }
}
