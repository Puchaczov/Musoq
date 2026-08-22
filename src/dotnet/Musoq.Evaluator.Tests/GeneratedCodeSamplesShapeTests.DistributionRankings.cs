using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void WindowDistributionRankingsSample_WhenCheckedIn_ShouldSharePeerMetadata()
    {
        var sample = ReadSample(WindowDistributionRankingsSampleFileName).Content;

        Assert.Contains("var resultPercentRanks0 = new double[resultWindowRows.Count];", sample);
        Assert.Contains("var resultCumeDists1 = new double[resultWindowRows.Count];", sample);
        Assert.Contains("for (int resultPercentRanks0PeerStart = 0;", sample);
        Assert.Contains("var resultPercentRanks0PeerEnd = resultPercentRanks0PeerStart;", sample);
        Assert.Contains(".PeerEquals(", sample);
        Assert.Contains("resultPercentRanks0WindowPlanPartitionCount == 1 ? 0d", sample);
        Assert.Contains("(double)(resultPercentRanks0PeerEnd + 1) / resultPercentRanks0WindowPlanPartitionCount", sample);
        Assert.AreEqual(1, CountOccurrences(sample, "for (int resultPercentRanks0PeerStart = 0;"));
        Assert.IsFalse(sample.Contains("WindowPercentRank", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("WindowCumeDist", StringComparison.Ordinal));
    }
}
