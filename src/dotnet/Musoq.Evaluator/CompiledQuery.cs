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
public class CompiledQuery : IDisposable
{
    private readonly IDictionary<string, object?> _fallbackParameters = new Dictionary<string, object?>(StringComparer.Ordinal);
    private ITableRunnable? _runnable;
    private IParameterizedRunnable? _parameterizedRunnable;
    private IReadOnlyList<ScriptParameterDefinition>? _parameterDefinitions;
    private IReadOnlyList<ScriptParameterDefinition>? _requiredParameters;
    private IDisposable? _lifetimeOwner;
    private bool _disposed;

    public CompiledQuery(ITableRunnable runnable)
        : this(runnable, lifetimeOwner: null)
    {
    }

    public CompiledQuery(ITableRunnable runnable, IDisposable? lifetimeOwner)
    {
        _runnable = runnable ?? throw QueryExecutionException.ForNullRunnable();
        _parameterizedRunnable = runnable as IParameterizedRunnable;
        _parameterDefinitions = _parameterizedRunnable?.ParameterDefinitions.ToArray() ??
                                Array.Empty<ScriptParameterDefinition>();
        _requiredParameters = _parameterDefinitions.Where(definition => definition.IsRequired).ToArray();
        _lifetimeOwner = lifetimeOwner;
    }

    public event QueryPhaseEventHandler PhaseChanged
    {
        add => CurrentRunnable.PhaseChanged += value;
        remove => CurrentRunnable.PhaseChanged -= value;
    }

    public event DataSourceEventHandler DataSourceProgress
    {
        add => CurrentRunnable.DataSourceProgress += value;
        remove => CurrentRunnable.DataSourceProgress -= value;
    }

    public IDictionary<string, object?> Parameters
    {
        get
        {
            EnsureNotDisposed();
            return _parameterizedRunnable?.Parameters ?? _fallbackParameters;
        }
    }

    public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions
    {
        get
        {
            EnsureNotDisposed();
            return _parameterDefinitions ?? Array.Empty<ScriptParameterDefinition>();
        }
    }

    public IReadOnlyList<ScriptParameterDefinition> RequiredParameters
    {
        get
        {
            EnsureNotDisposed();
            return _requiredParameters ?? Array.Empty<ScriptParameterDefinition>();
        }
    }

    public Table Run()
    {
        return Run(CancellationToken.None);
    }

    public Table Run(CancellationToken token)
    {
        var runnable = CurrentRunnable;
        if (token.IsCancellationRequested)
            throw new OperationCanceledException("Query execution was cancelled before it started.", token);

        Table result;
        try
        {
            result = runnable.Run(token);
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
        var runnable = CurrentRunnable;
        if (token.IsCancellationRequested)
            throw new OperationCanceledException("Query execution was cancelled before it started.", token);

        if (runnable is not IProfiledRunnable profiledRunnable)
            throw new InvalidOperationException("Query was not compiled with profiling instrumentation.");

        var recorder = new QueryProfileRecorder(queryId: runnable.GetType().FullName);
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

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var runnable = _runnable;
        var lifetimeOwner = _lifetimeOwner;
        _runnable = null;
        _parameterizedRunnable = null;
        _parameterDefinitions = null;
        _requiredParameters = null;
        _lifetimeOwner = null;

        try
        {
            (runnable as IDisposable)?.Dispose();
        }
        finally
        {
            lifetimeOwner?.Dispose();
        }
    }

    private ITableRunnable CurrentRunnable
    {
        get
        {
            EnsureNotDisposed();
            return _runnable ?? throw new ObjectDisposedException(nameof(CompiledQuery));
        }
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CompiledQuery));
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
