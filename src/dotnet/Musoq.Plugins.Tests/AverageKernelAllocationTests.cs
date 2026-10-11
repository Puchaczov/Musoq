using System;
using System.Threading;
using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class AverageKernelAllocationTests
{
    [TestMethod]
    public void ConcreteIntegralAverageOperations_ShouldAllocateNoBoxedInputsOrInvocationObjects()
    {
        (long Allocated, decimal? Average, long Count) measured = default;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { measured = MeasureOperations(); }
            catch (Exception exception) { failure = exception; }
        });
        using (ExecutionContext.SuppressFlow())
            thread.Start();
        thread.Join();
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();

        Assert.AreEqual(0L, measured.Allocated);
        Assert.AreEqual(50m, measured.Average);
        Assert.AreEqual(101_000L, measured.Count);
    }

    private static (long Allocated, decimal? Average, long Count) MeasureOperations()
    {
        var state = new AverageKernel<int, decimal>.State();
        Accumulate(ref state);
        _ = AverageKernel<int, decimal>.Get(in state);
        Accumulate(ref state);
        _ = AverageKernel<int, decimal>.Get(in state);
        AverageKernel<int, decimal>.Reset(ref state);

        var before = GC.GetAllocatedBytesForCurrentThread();
        Accumulate(ref state);
        var average = AverageKernel<int, decimal>.Get(in state);
        return (GC.GetAllocatedBytesForCurrentThread() - before, average, state.Count);
    }

    private static void Accumulate(ref AverageKernel<int, decimal>.State state)
    {
        for (var index = 0; index < 101_000; index++)
            AverageKernel<int, decimal>.Set(ref state, index % 101);
    }
}
