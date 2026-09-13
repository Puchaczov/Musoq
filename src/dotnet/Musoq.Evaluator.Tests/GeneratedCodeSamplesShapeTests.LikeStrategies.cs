using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void LikeStrategySamples_ShouldContainOnlyExplicitLikeExecution()
    {
        var samples = ReadNamedSamples(
            "Q269_SpecCorePatternPredicates.cs",
            "Q369_DynamicColumnLike.cs",
            "Q370_CorrelatedApplyDynamicLike.cs",
            "Q371_UnicodeLikeClauseContexts.cs");

        foreach (var sample in samples)
        {
            Assert.IsFalse(
                sample.Content.Contains("new Musoq.Evaluator.Operators().Like", StringComparison.Ordinal),
                sample.FileName);
        }

        Assert.Contains("Operators.LikeDynamic", samples[1].Content);
        Assert.Contains("new Musoq.Evaluator.LikeMatcherCacheSlot(false)", samples[1].Content);
        Assert.Contains("Operators.LikeDynamic", samples[2].Content);
        Assert.Contains("Operators.PrepareLike", samples[3].Content);
        Assert.Contains("Operators.LikePrepared", samples[3].Content);
    }
}
