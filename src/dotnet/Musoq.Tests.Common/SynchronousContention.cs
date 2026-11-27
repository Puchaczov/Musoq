using System;
using System.Threading;
using System.Threading.Tasks;

namespace Musoq.Tests.Common;

/// <summary>Holds the first factory while a second request reaches the component's contention boundary.</summary>
public sealed class SynchronousContention : IDisposable
{
    private readonly TestWatchdog _watchdog = new();
    private readonly TaskCompletionSource _ownerEntered = TestWatchdog.Signal();
    private readonly TaskCompletionSource _release = TestWatchdog.Signal();
    private readonly TaskCompletionSource _contenderReached = TestWatchdog.Signal();

    public void HoldOwner()
    {
        _ownerEntered.TrySetResult();
        _release.Task.WaitAsync(_watchdog.Token).GetAwaiter().GetResult();
    }

    public void ObserveContender()
    {
        if (_ownerEntered.Task.IsCompleted)
            _contenderReached.TrySetResult();
    }

    public void EnterMonitor(object gate)
    {
        if (Monitor.TryEnter(gate))
            return;

        ObserveContender();
        Monitor.Enter(gate);
    }

    public void WaitForAdmission(SemaphoreSlim gate, CancellationToken token)
    {
        if (gate.Wait(0, token))
            return;

        ObserveContender();
        gate.Wait(token);
    }

    public async Task<T[]> RunAsync<T>(Func<T> request, int count = 2)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 2);
        var results = new T[count];
        var first = TestWatchdog.StartWorker(request);
        Task<T>? second = null;

        await TestWatchdog.WithCleanupAsync(
            async () =>
            {
                await _watchdog.ReachedBeforeCompletionAsync(_ownerEntered.Task, first, "the factory owner");
                second = TestWatchdog.StartWorker(request);
                await _watchdog.ReachedBeforeCompletionAsync(_contenderReached.Task, second, "the contention boundary");
                _release.TrySetResult();
                await _watchdog.WaitAsync(Task.WhenAll(first, second), "both requests");
                results[0] = await first;
                results[1] = await second;
            },
            () =>
            {
                _release.TrySetResult();
                return Task.WhenAll(first, second ?? Task.CompletedTask);
            }
        );

        for (var index = 2; index < count; index++)
            results[index] = request();
        return results;
    }

    public void Dispose()
    {
        _release.TrySetResult();
        _watchdog.Dispose();
    }
}
