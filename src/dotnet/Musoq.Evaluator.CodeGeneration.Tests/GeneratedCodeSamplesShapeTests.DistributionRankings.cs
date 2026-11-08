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
        Assert.Contains("Musoq.Plugins.PercentRankKernel.Get(in resultPercentRanks0Context)", sample);
        Assert.Contains("Musoq.Plugins.CumeDistKernel.Get(in resultCumeDists1Context)", sample);
        Assert.AreEqual(1, CountOccurrences(sample, "for (int resultPercentRanks0PeerStart = 0;"));
        Assert.IsFalse(sample.Contains("WindowPercentRank", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("WindowCumeDist", StringComparison.Ordinal));
    }
}
