using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void RowBufferSafeParallelCteSamples_WhenCheckedIn_ShouldNotEmitTableBackedTaskHelpers()
    {
        var samples = ReadAllSamples()
            .Where(static sample =>
                sample.Content.Contains("ParallelBlock [cte-level-", StringComparison.Ordinal) &&
                sample.Content.Contains("StoreTable [__parallelCteLevel", StringComparison.Ordinal) &&
                sample.Content.Contains("_cteRowResults.Slot", StringComparison.Ordinal))
            .ToArray();

        Assert.IsNotEmpty(samples);

        foreach (var sample in samples)
        {
            Assert.IsFalse(
                sample.Content.Contains("private static Musoq.Evaluator.Tables.Table BuildCteLevel", StringComparison.Ordinal),
                sample.FileName);
            Assert.IsFalse(
                sample.Content.Contains("new Table(\"cte", StringComparison.Ordinal),
                sample.FileName);
            Assert.IsFalse(
                sample.Content.Contains("CastGeneratedRows<Cte", StringComparison.Ordinal) &&
                sample.Content.Contains("_tableResults[", StringComparison.Ordinal),
                sample.FileName);
        }
    }
}
