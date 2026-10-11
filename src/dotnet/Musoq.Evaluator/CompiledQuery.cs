using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;

namespace Musoq.Evaluator;

[DebuggerStepThrough]
public class CompiledQuery : IDisposable, IQueryProgressSource
{
    private readonly object _runtimeGate = new();
    private readonly SemaphoreSlim _executionGate = new(1, 1);
    private readonly ManualResetEventSlim _admissionsCompleted = new(true);
    private readonly ManualResetEventSlim _activeRunsCompleted = new(true);
    private readonly ManualResetEventSlim _resultLifetimesCompleted = new(true);
    private TypedParameterValues? _typedParameters;
    private ITableRunnable? _runnable;
    private IParameterizedRunnable? _parameterizedRunnable;
    private IReadOnlyList<ScriptParameterDefinition>? _parameterDefinitions;
    private IReadOnlyList<ScriptParameterContract>? _parameterContracts;
    private IReadOnlyList<ScriptParameterDefinition>? _requiredParameters;
    private IDisposable? _lifetimeOwner;
    private bool _disposed;
    private int _pendingExecutions;
    private int _activeRuns;
    private int _activeResultLifetimes;
    private event QueryPhaseEventHandler? _phaseChangedHandlers;
    private event DataSourceEventHandler? _dataSourceProgressHandlers;
    private event QueryProgressEventHandler? _queryProgressHandlers;

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
        _parameterContracts = _parameterizedRunnable?.ParameterContracts.ToArray() ??
                              _parameterDefinitions.Select(static definition => definition.Contract).ToArray();
        _requiredParameters = _parameterDefinitions.Where(definition => definition.IsRequired).ToArray();
        _lifetimeOwner = lifetimeOwner;
        if (runnable is ITypedParameterizedRunnable typed && typed.TypedParameters.Count != 0)
            TypedParameters.ReplaceWith(typed.TypedParameters);
    }

    public event QueryPhaseEventHandler PhaseChanged
    {
        add
        {
            lock (_runtimeGate)
            {
                EnsureNotDisposed();
                _phaseChangedHandlers += value;
                CurrentRunnable.PhaseChanged += value;
            }
        }
        remove
        {
            lock (_runtimeGate)
            {
                EnsureNotDisposed();
                _phaseChangedHandlers -= value;
                CurrentRunnable.PhaseChanged -= value;
            }
        }
    }

    public event DataSourceEventHandler DataSourceProgress
    {
        add
        {
            lock (_runtimeGate)
            {
                EnsureNotDisposed();
                _dataSourceProgressHandlers += value;
                CurrentRunnable.DataSourceProgress += value;
            }
        }
        remove
        {
            lock (_runtimeGate)
            {
                EnsureNotDisposed();
                _dataSourceProgressHandlers -= value;
                CurrentRunnable.DataSourceProgress -= value;
            }
        }
    }

    public event QueryProgressEventHandler QueryProgress
    {
        add
        {
            lock (_runtimeGate)
            {
                EnsureNotDisposed();
                _queryProgressHandlers += value;
                if (CurrentRunnable is IQueryProgressSource source)
                    source.QueryProgress += value;
            }
        }
        remove
        {
            lock (_runtimeGate)
            {
                EnsureNotDisposed();
                _queryProgressHandlers -= value;
                if (CurrentRunnable is IQueryProgressSource source)
                    source.QueryProgress -= value;
            }
        }
    }

    /// <summary>Gets concrete parameter slots for the next execution.</summary>
    public TypedParameterValues TypedParameters
    {
        get
        {
            lock (_runtimeGate)
            {
                EnsureNotDisposed();
                return _typedParameters ??= new TypedParameterValues(_runtimeGate, EnsureTypedParametersMutable);
            }
        }
    }

    private void EnsureTypedParametersMutable()
    {
        EnsureNotDisposed();
        if (Volatile.Read(ref _activeRuns) != 0)
            throw new InvalidOperationException("Parameters cannot change while a query is executing.");
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

    public IReadOnlyList<ScriptParameterContract> ParameterContracts
    {
        get
        {
            EnsureNotDisposed();
            return _parameterContracts ?? Array.Empty<ScriptParameterContract>();
        }
    }

    public Table Run()
    {
        return Run(CancellationToken.None);
    }

    public Table Run(CancellationToken token)
    {
        return RunCore(token, progressOptions: null);
    }

    public Table Run(QueryProgressOptions progressOptions, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(progressOptions);
        return RunCore(token, progressOptions);
    }

    /// <summary>Executes with independently captured native parameter slots and callbacks.</summary>
    public Table Run(TypedQueryRunOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.TypedParameters);
        return RunCore(options.CancellationToken, options.QueryProgressOptions, options);
    }

    private Table RunCore(CancellationToken token, QueryProgressOptions? progressOptions, TypedQueryRunOptions? options = null)
    {
        var admission = BeginAdmission();
        var gateAcquired = false;
        var executionStarted = false;
        var admissionOpen = true;

        try
        {
            QueryExecutionContext context;
            if (!admission.UseContext)
            {
                _executionGate.Wait(token);
                gateAcquired = true;
            }

            lock (_runtimeGate)
            {
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException("Query execution was cancelled before it started.", token);

                var parameterSnapshot = options?.TypedParameters ?? CaptureParameterSnapshot(token);
                ValidateRequiredParameters(parameterSnapshot);
                EnsureDataSourceLifecycleProvider(admission.Runnable);
                BeginExecution();
                executionStarted = true;
                context = CaptureExecutionContext(admission.Runnable, parameterSnapshot, token, progressOptions, options);
                if (!admission.UseContext)
                    ApplyParameterSnapshot(context);
            }
            EndAdmission();
            admissionOpen = false;

            Table result;
            try
            {
                result = admission.UseContext
                    ? ((IContextTableRunnable)admission.Runnable).Run(context.ToRunContext())
                    : admission.Runnable.Run(context.CancellationToken);
                EnsurePublicResult(result);
                AttachDeferredResultLifetime(result);
            }
            catch (ScriptParameterBindingException ex)
            {
                throw QueryExecutionException.ForScriptParameterBinding(ex);
            }
            catch (DataSourceLifecycleException ex)
            {
                throw QueryExecutionException.ForDataSourceFailure(ex);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (QueryExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw ExecutionFailureConverter.Convert("Execution", ex);
            }

            return result;
        }
        finally
        {
            if (admissionOpen)
                EndAdmission();
            if (gateAcquired)
                _executionGate.Release();
            if (executionStarted)
                EndExecution();
        }
    }

    /// <summary>
    ///     Runs the query through its true asynchronous contract when the runnable provides one.
    ///     Runnables that only implement <see cref="ITableRunnable" /> use that interface's
    ///     documented compatibility fallback.
    /// </summary>
    public async ValueTask<Table> RunAsync(CancellationToken token = default)
    {
        return await RunAsyncCore(token, progressOptions: null).ConfigureAwait(false);
    }

    public ValueTask<Table> RunAsync(QueryProgressOptions progressOptions, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(progressOptions);
        return RunAsyncCore(token, progressOptions);
    }

    /// <summary>Asynchronously executes with independently captured native parameter slots.</summary>
    public ValueTask<Table> RunAsync(TypedQueryRunOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.TypedParameters);
        return RunAsyncCore(options.CancellationToken, options.QueryProgressOptions, options);
    }

    private async ValueTask<Table> RunAsyncCore(CancellationToken token, QueryProgressOptions? progressOptions, TypedQueryRunOptions? options = null)
    {
        var admission = BeginAdmission();
        var gateAcquired = false;
        var executionStarted = false;
        var admissionOpen = true;

        try
        {
            QueryExecutionContext context;
            if (!admission.UseContext)
            {
                await _executionGate.WaitAsync(token).ConfigureAwait(false);
                gateAcquired = true;
            }

            lock (_runtimeGate)
            {
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException("Query execution was cancelled before it started.", token);

                var parameterSnapshot = options?.TypedParameters ?? CaptureParameterSnapshot(token);
                ValidateRequiredParameters(parameterSnapshot);
                EnsureDataSourceLifecycleProvider(admission.Runnable);
                BeginExecution();
                executionStarted = true;
                context = CaptureExecutionContext(admission.Runnable, parameterSnapshot, token, progressOptions, options);
                if (!admission.UseContext)
                    ApplyParameterSnapshot(context);
            }
            EndAdmission();
            admissionOpen = false;

            Table result;
            try
            {
                result = admission.Runnable is IContextAsyncTableRunnable contextAsyncRunnable
                    ? await contextAsyncRunnable.RunAsync(context.ToRunContext()).ConfigureAwait(false)
                    : admission.UseContext
                        ? await Task.Run(() => ((IContextTableRunnable)admission.Runnable).Run(context.ToRunContext()), token).ConfigureAwait(false)
                    : admission.Runnable is IAsyncTableRunnable asyncRunnable
                    ? await asyncRunnable.RunAsync(context.CancellationToken).ConfigureAwait(false)
                        : await admission.Runnable.RunAsync(context.CancellationToken).ConfigureAwait(false);
                EnsurePublicResult(result);
                AttachDeferredResultLifetime(result);
            }
            catch (ScriptParameterBindingException ex)
            {
                throw QueryExecutionException.ForScriptParameterBinding(ex);
            }
            catch (DataSourceLifecycleException ex)
            {
                throw QueryExecutionException.ForDataSourceFailure(ex);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (QueryExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw ExecutionFailureConverter.Convert("Execution", ex);
            }

            return result;
        }
        finally
        {
            if (admissionOpen)
                EndAdmission();
            if (gateAcquired)
                _executionGate.Release();
            if (executionStarted)
                EndExecution();
        }
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
        return RunWithProfile(token, emitTelemetry: true, progressOptions: null);
    }

    public QueryProfileResult RunWithProfile(
        QueryProgressOptions progressOptions,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(progressOptions);
        return RunWithProfile(token, emitTelemetry: true, progressOptions);
    }

    internal QueryProfileResult RunWithProfile(
        CancellationToken token,
        bool emitTelemetry,
        QueryProgressOptions? progressOptions = null)
    {
        var admission = BeginAdmission();
        var gateAcquired = false;
        var executionStarted = false;
        var admissionOpen = true;
        try
        {
            IProfiledRunnable profiledRunnable;
            QueryExecutionContext context;
            _executionGate.Wait(token);
            gateAcquired = true;
            lock (_runtimeGate)
            {
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException("Query execution was cancelled before it started.", token);

                if (admission.Runnable is not IProfiledRunnable profiled)
                    throw new InvalidOperationException("Query was not compiled with profiling instrumentation.");

                var parameterSnapshot = CaptureParameterSnapshot(token);
                ValidateRequiredParameters(parameterSnapshot);
                EnsureDataSourceLifecycleProvider(admission.Runnable);
                profiledRunnable = profiled;
                BeginExecution();
                executionStarted = true;
                context = CaptureExecutionContext(admission.Runnable, parameterSnapshot, token, progressOptions);
                ApplyParameterSnapshot(context);
            }
            EndAdmission();
            admissionOpen = false;

            var recorder = new QueryProfileRecorder(queryId: admission.Runnable.GetType().FullName);
            Table result;
            try
            {
                result = admission.Runnable is IContextProfiledRunnable contextualProfiledRunnable
                    ? contextualProfiledRunnable.RunWithProfile(context.ToRunContext(), recorder)
                    : profiledRunnable.RunWithProfile(context.CancellationToken, recorder);
                MaterializePublicResult(result);
            }
            catch (ScriptParameterBindingException ex)
            {
                throw QueryExecutionException.ForScriptParameterBinding(ex);
            }
            catch (DataSourceLifecycleException ex)
            {
                throw QueryExecutionException.ForDataSourceFailure(ex);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (QueryExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw ExecutionFailureConverter.Convert("Profiling", ex);
            }

            var profile = recorder.CreateSnapshot();
            if (emitTelemetry)
                QueryProfileTelemetry.Emit(profile);

            return new QueryProfileResult(result, profile, QueryProfileTextPrinter.Print(profile));
        }
        finally
        {
            if (admissionOpen)
                EndAdmission();
            if (gateAcquired)
                _executionGate.Release();
            if (executionStarted)
                EndExecution();
        }
    }

    public void Dispose()
    {
        ITableRunnable? runnable;
        IDisposable? lifetimeOwner;
        lock (_runtimeGate)
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        _admissionsCompleted.Wait();
        _activeRunsCompleted.Wait();
        _resultLifetimesCompleted.Wait();
        lock (_runtimeGate)
        {
            runnable = _runnable;
            lifetimeOwner = _lifetimeOwner;
            _runnable = null;
            _parameterizedRunnable = null;
            _parameterDefinitions = null;
            _requiredParameters = null;
            _lifetimeOwner = null;
        }

        try
        {
            (runnable as IDisposable)?.Dispose();
        }
        finally
        {
            lifetimeOwner?.Dispose();
            _executionGate.Dispose();
            _admissionsCompleted.Dispose();
            _activeRunsCompleted.Dispose();
            _resultLifetimesCompleted.Dispose();
        }
    }

    private (ITableRunnable Runnable, bool UseContext) BeginAdmission()
    {
        lock (_runtimeGate)
        {
            EnsureNotDisposed();
            var runnable = _runnable ?? throw new ObjectDisposedException(nameof(CompiledQuery));
            if (Interlocked.Increment(ref _pendingExecutions) == 1)
                _admissionsCompleted.Reset();

            return (runnable, runnable is IContextTableRunnable);
        }
    }

    private void EndAdmission()
    {
        lock (_runtimeGate)
        {
            if (Interlocked.Decrement(ref _pendingExecutions) == 0)
                _admissionsCompleted.Set();
        }
    }

    private QueryExecutionContext CaptureExecutionContext(
        ITableRunnable runnable,
        TypedParameterSnapshot parameters,
        CancellationToken token,
        QueryProgressOptions? progressOptions,
        TypedQueryRunOptions? options = null)
    {
        return QueryExecutionContext.Capture(
            runnable,
            parameters,
            token,
            _phaseChangedHandlers + options?.PhaseChanged,
            _dataSourceProgressHandlers + options?.DataSourceProgress,
            _queryProgressHandlers + options?.QueryProgress,
            progressOptions,
            runnable,
            runnable.GetType().FullName);
    }

    private TypedParameterSnapshot CaptureParameterSnapshot(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return _typedParameters?.Capture(token) ?? TypedParameterSnapshot.Empty;
    }

    private void ValidateRequiredParameters(TypedParameterSnapshot snapshot)
    {
        if (_runnable is IMetadataOnlyRunnable { IsMetadataOnly: true })
            return;
        foreach (var definition in RequiredParameters)
        {
            if (snapshot.Contains(definition.Name))
                continue;
            throw QueryExecutionException.ForScriptParameterBinding(
                ScriptParameterBindingException.MissingRequired(definition.Name));
        }
    }

    private void ApplyParameterSnapshot(QueryExecutionContext context)
    {
        if (_runnable is ITypedParameterizedRunnable typed)
            typed.TypedParameters = context.TypedParameters;
    }

    private void BeginExecution()
    {
        if (Interlocked.Increment(ref _activeRuns) == 1)
            _activeRunsCompleted.Reset();
    }

    private void EndExecution()
    {
        lock (_runtimeGate)
        {
            if (Interlocked.Decrement(ref _activeRuns) == 0)
            {
                _activeRunsCompleted.Set();
            }
        }
    }

    private void AttachDeferredResultLifetime(Table result)
    {
        if (!result.HasDeferredMaterialization)
            return;

        DeferredResultLifetimeLease lease;
        lock (_runtimeGate)
        {
            if (Interlocked.Increment(ref _activeResultLifetimes) == 1)
                _resultLifetimesCompleted.Reset();

            lease = new DeferredResultLifetimeLease(ReleaseResultLifetime);
        }

        if (!result.TryAttachLifetimeLease(lease))
            lease.Dispose();
    }

    private void ReleaseResultLifetime()
    {
        lock (_runtimeGate)
        {
            if (Interlocked.Decrement(ref _activeResultLifetimes) == 0)
                _resultLifetimesCompleted.Set();
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

    private static void EnsureDataSourceLifecycleProvider(ITableRunnable runnable)
    {
        if (runnable.Provider is { } provider)
            runnable.Provider = DataSourceLifecycle.WrapProvider(provider);
    }

    private sealed class DeferredResultLifetimeLease(Action release) : IDisposable
    {
        private Action? _release = release ?? throw new ArgumentNullException(nameof(release));

        ~DeferredResultLifetimeLease()
        {
            Release();
        }

        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        private void Release()
        {
            Interlocked.Exchange(ref _release, null)?.Invoke();
        }
    }
}
