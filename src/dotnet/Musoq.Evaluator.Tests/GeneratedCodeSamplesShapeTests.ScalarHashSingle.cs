using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void ScalarHashSingleSample_ShouldKeepCarrierHotPathOptimizations()
    {
        var sample = ReadSamples().Single(item => item.FileName == ScalarSubqueryJoinOnSampleFileName).Content;

        Assert.Contains("ParallelSingleKeyAggregateLoop", sample);
        Assert.Contains("CorrelatedScalarSubqueryResultExtractor.GetValue", sample);
        Assert.IsFalse(
            sample.Contains("CorrelatedScalarSubqueryResult<string>?", StringComparison.Ordinal),
            "Q141 should not wrap the missing-safe carrier in Nullable<T>.");
        Assert.IsFalse(
            sample.Contains("__compiled2LibraryBase", StringComparison.Ordinal),
            "Q141 should not allocate a LibraryBase target while building the scalar hash key.");
    }
}
