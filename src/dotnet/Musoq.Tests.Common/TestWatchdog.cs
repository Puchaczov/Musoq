using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Tests.Common;

public sealed class TestWatchdog : IDisposable
{
    public static TimeSpan Limit { get; } = TimeSpan.FromMinutes(2);

    private readonly CancellationTokenSource _deadline;

    public TestWatchdog(CancellationToken testCancellation = default)
    {
        _deadline = CancellationTokenSource.CreateLinkedTokenSource(testCancellation);
        _deadline.CancelAfter(Limit);
    }

    public CancellationToken Token => _deadline.Token;

    public static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static Task StartWorker(Action action) =>
        Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    public static Task<T> StartWorker<T>(Func<T> action) =>
        Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    public void Wait(ManualResetEventSlim signal, string stage)
    {
        try
        {
            signal.Wait(Token);
        }
        catch (OperationCanceledException) when (Token.IsCancellationRequested)
        {
            throw new AssertFailedException($"The test watchdog expired while waiting for {stage}.");
        }
    }

    public async Task WaitAsync(Task signal, string stage)
    {
        try
        {
            await signal.WaitAsync(Token);
        }
        catch (OperationCanceledException exception)
            when (exception.CancellationToken == Token && Token.IsCancellationRequested)
        {
            throw new AssertFailedException($"The test watchdog expired while waiting for {stage}.");
        }
    }

    public void Rendezvous(Barrier barrier, string stage, CancellationToken cancellation = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(Token, cancellation);
        try
        {
            barrier.SignalAndWait(deadline.Token);
        }
        catch (OperationCanceledException) when (Token.IsCancellationRequested)
        {
            throw new AssertFailedException($"The test watchdog expired while waiting for {stage}.");
        }
    }

    public async Task ReachedBeforeCompletionAsync(Task signal, Task operation, string stage)
    {
        var reached = Task.WhenAny(signal, operation);
        await WaitAsync(reached, stage);
        if (await reached == operation)
        {
            await operation;
            throw new AssertFailedException($"The operation completed before reaching {stage}.");
        }

        await signal;
    }

    public static async Task WithCleanupAsync(Func<Task> verify, Func<Task> cleanup)
    {
        Exception? failure = null;
        try
        {
            await verify();
        }
        catch (Exception exception)
        {
            failure = exception;
            throw;
        }
        finally
        {
            try
            {
                await cleanup().WaitAsync(Limit);
            }
            catch (Exception cleanupFailure) when (failure is not null)
            {
                if (!ReferenceEquals(failure, cleanupFailure))
                    throw new AggregateException("The test and its cleanup both failed.", failure, cleanupFailure);
            }
        }
    }

    public void Dispose() => _deadline.Dispose();
}
