using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void IsDistinctFromSample_WhenCheckedIn_ShouldUseDirectTypedComparisons()
    {
        var sample = ReadSamples().Single(static item =>
            item.FileName == IsDistinctFromNullSafeComparisonSampleFileName);

        Assert.Contains("IS DISTINCT FROM", sample.Content);
        Assert.Contains("IS NOT DISTINCT FROM", sample.Content);
        Assert.Contains(" != ", sample.Content);
        Assert.Contains(" == ", sample.Content);
        Assert.IsFalse(sample.Content.Contains("InternalIsDistinctFromOperator", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains("InternalIsNotDistinctFromOperator", StringComparison.Ordinal), sample.FileName);
        Assert.IsFalse(sample.Content.Contains("System.Reflection", StringComparison.Ordinal), sample.FileName);
    }
}
