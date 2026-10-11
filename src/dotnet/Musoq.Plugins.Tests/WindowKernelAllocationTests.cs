using System;
using System.Threading;
using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class WindowKernelAllocationTests
{
    [TestMethod]
    public void TypedFrameAndRankingOperations_ShouldAllocateNoBoxedInputsOrInvocationObjects()
    {
        (long Allocated, decimal Total, long Rank) measured = default;
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
        Assert.AreEqual(300_000m, measured.Total);
        Assert.AreEqual(100_000L, measured.Rank);
    }

    private static (long Allocated, decimal Total, long Rank) MeasureOperations()
    {
        decimal?[] values = [1m, 2m, null];
        int[] indices = [0, 1, 2];
        _ = Invoke(values, indices);
        _ = Invoke(values, indices);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var result = Invoke(values, indices);
        return (GC.GetAllocatedBytesForCurrentThread() - before, result.Total, result.Rank);
    }

    [TestMethod]
    public void DecimalBatch_WhenStorageIsOwnedAndPoolsAreWarm_ShouldAllocateNoInvocationObjects()
    {
        var allocated = -1L;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                decimal?[] values = [1.00m, 2.00m, null];
                int[] indices = [0, 1, 2];
                int[] starts = [0, 0, 1];
                int[] ends = [0, 1, 2];
                var results = new decimal?[3];
                var context = new WindowBatchContext<decimal?, decimal?>(values, indices, starts, ends, results);
                _ = MinWindowBatch<decimal>.TryEvaluate(in context);
                _ = MaxWindowBatch<decimal>.TryEvaluate(in context);
                _ = DecimalSumWindowBatch.TryEvaluate(in context);
                _ = DecimalSumWindowBatch.TryEvaluate(in context);
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var iteration = 0; iteration < 10_000; iteration++)
                {
                    _ = MinWindowBatch<decimal>.TryEvaluate(in context);
                    _ = MaxWindowBatch<decimal>.TryEvaluate(in context);
                    _ = DecimalSumWindowBatch.TryEvaluate(in context);
                }
                allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                CollectionAssert.AreEqual(new decimal?[] { 1.00m, 3.00m, 2.00m }, results);
            }
            catch (Exception exception) { failure = exception; }
        });
        using (ExecutionContext.SuppressFlow())
            thread.Start();
        thread.Join();
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
        Assert.AreEqual(0L, allocated);
    }

    private static (decimal Total, long Rank) Invoke(decimal?[] values, int[] indices)
    {
        var total = 0m;
        var rank = 0L;
        for (var iteration = 0; iteration < 100_000; iteration++)
        {
            var frame = new WindowFrameContext<decimal?>(values, indices, 1, 0, 1);
            total += FirstValueKernel<decimal>.Get(in frame).GetValueOrDefault();
            total += NthValueKernel<decimal>.Get(in frame, 2).GetValueOrDefault();
            var peers = new WindowRankingContext(1, 0, 1, 1, 3);
            rank += RankKernel.Get(in peers);
        }
        return (total, rank);
    }
}
