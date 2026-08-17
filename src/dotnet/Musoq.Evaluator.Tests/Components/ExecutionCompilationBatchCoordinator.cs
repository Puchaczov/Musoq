using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Musoq.Converter;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Components;

internal readonly record struct ExecutionCompilationBatchResult(
    BuildResult Result,
    bool WasBatched);

/// <summary>
/// Collects only explicitly eligible test compilations for a short bounded
/// window. It is intentionally test-only; production compilation never enters
/// this coordinator.
/// </summary>
internal sealed class ExecutionCompilationBatchCoordinator : IDisposable
{
    internal const int MaximumBatchSize = 16;
    private static readonly TimeSpan CollectionWindow = TimeSpan.FromMilliseconds(2);

    private readonly object _gate = new();
    private readonly ConcurrentQueue<PendingRequest> _queue = new();
    private readonly AutoResetEvent _queueSignal = new(false);
    private readonly SemaphoreSlim _batchSlots = new(2, 2);
    private readonly ManualResetEventSlim _workIdle = new(true);
    private readonly Func<IReadOnlyList<ExecutionBatchCompilationRequest>, IReadOnlyList<ExecutionBatchCompilationResult>> _batchCompiler;
    private readonly Func<ExecutionBatchCompilationRequest, BuildResult> _singleCompiler;
    private readonly int _maximumBatchSize;
    private readonly TimeSpan _collectionWindow;
    private readonly Action? _requestEnqueued;
    private long _nextKey;
    private bool _disposed;
    private int _workCount;
    private readonly Thread _dispatcher;

    internal ExecutionCompilationBatchCoordinator(
        Func<IReadOnlyList<ExecutionBatchCompilationRequest>, IReadOnlyList<ExecutionBatchCompilationResult>> batchCompiler,
        Func<ExecutionBatchCompilationRequest, BuildResult> singleCompiler,
        int maximumBatchSize = MaximumBatchSize,
        TimeSpan? collectionWindow = null,
        Action? requestEnqueued = null)
    {
        _batchCompiler = batchCompiler ?? throw new ArgumentNullException(nameof(batchCompiler));
        _singleCompiler = singleCompiler ?? throw new ArgumentNullException(nameof(singleCompiler));
        if (maximumBatchSize < 2)
            throw new ArgumentOutOfRangeException(nameof(maximumBatchSize));

        _maximumBatchSize = maximumBatchSize;
        _collectionWindow = collectionWindow ?? CollectionWindow;
        _requestEnqueued = requestEnqueued;
        _dispatcher = new Thread(Dispatch)
        {
            IsBackground = true,
            Name = "Musoq evaluator compilation batch dispatcher"
        };
        _dispatcher.Start();
    }

    internal ExecutionCompilationBatchResult Submit(
        string script,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions,
        string consumerFamily = "stable-typed",
        string batchOrigin = "test-coordinator")
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(schemaProvider);
        ArgumentNullException.ThrowIfNull(loggerResolver);
        ArgumentNullException.ThrowIfNull(compilationOptions);

        var sequence = Interlocked.Increment(ref _nextKey);
        var key = $"basic-{sequence:x16}";
        var request = new ExecutionBatchCompilationRequest(
            key,
            script,
            $"Musoq.Evaluator.Tests.BasicBatch.{key}",
            schemaProvider,
            loggerResolver,
            compilationOptions,
            ConsumerFamily: consumerFamily,
            BatchOrigin: batchOrigin,
            EnqueuedTimestamp: Stopwatch.GetTimestamp());
        var pending = new PendingRequest(request);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _queue.Enqueue(pending);
        }
        _queueSignal.Set();
        _requestEnqueued?.Invoke();

        return pending.Completion.Task.GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        _queueSignal.Set();
        _dispatcher.Join();
        _workIdle.Wait();
        _queueSignal.Dispose();
        _batchSlots.Dispose();
        _workIdle.Dispose();
    }

    private void Dispatch()
    {
        while (true)
        {
            _queueSignal.WaitOne();
            if (Volatile.Read(ref _disposed))
            {
                DrainAfterShutdown();
                return;
            }

            while (_queue.TryDequeue(out var first))
            {
                var batch = Collect(first);
                if (Volatile.Read(ref _disposed))
                {
                    ProcessIndividually(batch);
                    DrainAfterShutdown();
                    return;
                }

                Schedule(batch);
                if (_queue.IsEmpty)
                    break;
            }
        }
    }

    private IReadOnlyList<PendingRequest> Collect(PendingRequest first)
    {
        var batch = new List<PendingRequest>(_maximumBatchSize) { first };
        var deadline = Stopwatch.GetTimestamp() +
                       (long)(_collectionWindow.TotalSeconds * Stopwatch.Frequency);

        while (batch.Count < _maximumBatchSize)
        {
            if (Volatile.Read(ref _disposed))
                break;

            while (batch.Count < _maximumBatchSize && _queue.TryDequeue(out var pending))
                batch.Add(pending);

            if (batch.Count >= _maximumBatchSize)
                break;

            var remaining = deadline - Stopwatch.GetTimestamp();
            if (remaining <= 0)
                break;

            var milliseconds = Math.Max(
                1,
                (int)Math.Ceiling(remaining * 1000d / Stopwatch.Frequency));
            _queueSignal.WaitOne(milliseconds);
        }

        return batch;
    }

    private void Schedule(IReadOnlyList<PendingRequest> pending)
    {
        if (pending.Count > 1)
            _batchSlots.Wait();

        Interlocked.Increment(ref _workCount);
        _workIdle.Reset();
        ThreadPool.QueueUserWorkItem(
            static state => ((BatchWork)state!).Run(),
            new BatchWork(this, pending, pending.Count > 1));
    }

    private void Process(IReadOnlyList<PendingRequest> pending)
    {
        if (pending.Count == 1)
        {
            ProcessIndividually(pending);
            return;
        }

        IReadOnlyList<ExecutionBatchCompilationResult>? batchResults = null;
        try
        {
            batchResults = _batchCompiler(pending.Select(static item => item.Request).ToArray());
        }
        catch
        {
            // A batch is an optimization only. The original single-query path
            // remains authoritative for diagnostics and environmental failures.
        }

        var resultsByKey = batchResults?
            .GroupBy(static result => result.Key, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.Last(), StringComparer.Ordinal);

        foreach (var item in pending)
        {
            if (resultsByKey is not null &&
                resultsByKey.TryGetValue(item.Request.Key, out var batchResult) &&
                batchResult.Result.Succeeded)
            {
                item.Complete(new ExecutionCompilationBatchResult(batchResult.Result, WasBatched: true));
                continue;
            }

            ProcessIndividually([item]);
        }
    }

    private void ProcessIndividually(IReadOnlyList<PendingRequest> pending)
    {
        foreach (var item in pending)
        {
            try
            {
                item.Complete(new ExecutionCompilationBatchResult(
                    _singleCompiler(item.Request),
                    WasBatched: false));
            }
            catch (Exception exception)
            {
                item.Fail(exception);
            }
        }
    }

    private void DrainAfterShutdown()
    {
        while (_queue.TryDequeue(out var pending))
            ProcessIndividually([pending]);
    }

    private void CompleteWork(bool occupiedBatchSlot)
    {
        if (occupiedBatchSlot)
            _batchSlots.Release();

        if (Interlocked.Decrement(ref _workCount) == 0)
            _workIdle.Set();
    }

    private sealed class BatchWork(
        ExecutionCompilationBatchCoordinator owner,
        IReadOnlyList<PendingRequest> pending,
        bool occupiedBatchSlot)
    {
        public void Run()
        {
            try
            {
                owner.Process(pending);
            }
            finally
            {
                owner.CompleteWork(occupiedBatchSlot);
            }
        }
    }

    private sealed class PendingRequest(ExecutionBatchCompilationRequest request)
    {
        internal ExecutionBatchCompilationRequest Request { get; } = request;

        internal TaskCompletionSource<ExecutionCompilationBatchResult> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal void Complete(ExecutionCompilationBatchResult result) => Completion.TrySetResult(result);

        internal void Fail(Exception exception) => Completion.TrySetException(exception);
    }
}

internal static class StableTypedExecutionCompilationCoordinator
{
    private static readonly ExecutionCompilationBatchCoordinator Shared = new(
        InstanceCreator.CompileForExecutionBatch,
        static request => InstanceCreator.CompileWithDiagnostics(
            request.Script,
            request.AssemblyName,
            request.SchemaProvider,
            request.LoggerResolver,
            request.CompilationOptions));

    internal static ExecutionCompilationBatchResult Submit(
        string script,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions,
        string consumerFamily = "stable-typed",
        string batchOrigin = "test-coordinator")
    {
        return Shared.Submit(
            script,
            schemaProvider,
            loggerResolver,
            compilationOptions,
            consumerFamily,
            batchOrigin);
    }
}
