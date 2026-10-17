using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class WindowKernelTests
{
    [TestMethod]
    public void TypedFrameAndRankingOperations_ShouldReturnExpectedValues()
    {
        decimal?[] values = [1m, 2m, null];
        int[] indices = [0, 1, 2];
        var frame = new WindowFrameContext<decimal?>(values, indices, 1, 0, 1);
        var peers = new WindowRankingContext(1, 0, 1, 1, 3);

        Assert.AreEqual(1m, FirstValueKernel<decimal>.Get(in frame));
        Assert.AreEqual(2m, NthValueKernel<decimal>.Get(in frame, 2));
        Assert.AreEqual(1L, RankKernel.Get(in peers));
    }

    [TestMethod]
    public void DecimalBatch_WhenFramesOverlap_ShouldReturnExpectedSums()
    {
        decimal?[] values = [1.00m, 2.00m, null];
        int[] indices = [0, 1, 2];
        int[] starts = [0, 0, 1];
        int[] ends = [0, 1, 2];
        var results = new decimal?[3];
        var context = new WindowBatchContext<decimal?, decimal?>(values, indices, starts, ends, results);

        Assert.IsTrue(DecimalSumWindowBatch.TryEvaluate(in context));
        CollectionAssert.AreEqual(new decimal?[] { 1.00m, 3.00m, 2.00m }, results);
    }
}
