using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Musoq.Tests.Common;

/// <summary>Holds a real factory call until a second dedicated caller has reached its blocking boundary.</summary>
public sealed class SynchronousContention : IDisposable
{
    private static readonly TimeSpan DeadlockTimeout = TimeSpan.FromMinutes(2);
    private readonly ManualResetEventSlim _ownerEntered = new(false);
    private readonly ManualResetEventSlim _release = new(false);
    private readonly ManualResetEventSlim _secondStarted = new(false);

    public void HoldOwner()
    {
        _ownerEntered.Set();
        if (!_release.Wait(DeadlockTimeout))
            throw new TimeoutException("The coordinated factory was not released.");
    }

    public T[] Run<T>(Func<T> request, int count = 2)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 2);
        var results = new T[count];
        var failures = new ExceptionDispatchInfo?[2];
        var first = new Thread(() => Invoke(0)) { IsBackground = true };
        var second = new Thread(() => Invoke(1)) { IsBackground = true };
        var secondLaunched = false;
        first.Start();
        try
        {
            if (!_ownerEntered.Wait(DeadlockTimeout))
                throw new TimeoutException("The first caller did not enter the factory.");
            second.Start();
            secondLaunched = true;
            if (!_secondStarted.Wait(DeadlockTimeout) ||
                !SpinWait.SpinUntil(() => !second.IsAlive ||
                    (second.ThreadState & ThreadState.WaitSleepJoin) != 0, DeadlockTimeout))
                throw new TimeoutException("The second caller did not reach its completion or blocking boundary.");
        }
        finally
        {
            _release.Set();
            var firstCompleted = first.Join(DeadlockTimeout);
            var secondCompleted = !secondLaunched || second.Join(DeadlockTimeout);
            if (!firstCompleted)
                throw new TimeoutException("The first caller did not terminate after release.");
            if (!secondCompleted)
                throw new TimeoutException("The second caller did not terminate after release.");
        }
        failures[0]?.Throw();
        failures[1]?.Throw();
        for (var index = 2; index < count; index++)
            results[index] = request();
        return results;

        void Invoke(int index)
        {
            try
            {
                if (index == 1)
                    _secondStarted.Set();
                results[index] = request();
            }
            catch (Exception exception)
            {
                failures[index] = ExceptionDispatchInfo.Capture(exception);
            }
        }
    }

    public void Dispose()
    {
        _release.Set();
        _ownerEntered.Dispose();
        _release.Dispose();
        _secondStarted.Dispose();
    }
}
