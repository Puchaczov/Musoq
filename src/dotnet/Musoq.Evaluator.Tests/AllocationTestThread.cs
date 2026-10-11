using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Musoq.Evaluator.Tests;

internal static class AllocationTestThread
{
    public static TResult Run<TResult>(Func<TResult> measure)
    {
        TResult result = default!;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                _ = GC.GetAllocatedBytesForCurrentThread();
                result = measure();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        using (ExecutionContext.SuppressFlow())
            thread.Start();
        thread.Join();
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
        return result;
    }
}
