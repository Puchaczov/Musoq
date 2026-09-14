using System;
using System.Linq;
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
            "Q371_UnicodeLikeClauseContexts.cs",
            "Q372_ConstantRLike.cs",
            "Q373_DynamicColumnRLike.cs",
            "Q374_CorrelatedApplyRLike.cs",
            "Q375_LiteralAndFallbackRLike.cs");

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
        Assert.Contains("Operators.PrepareRLike", samples[4].Content);
        Assert.Contains("Operators.RLikePrepared", samples[4].Content);
        Assert.Contains("Operators.RLikeDynamic", samples[5].Content);
        Assert.Contains("new Musoq.Evaluator.RLikeMatcherCacheSlot(false)", samples[5].Content);
        Assert.Contains("Operators.PrepareRLike", samples[6].Content);
        Assert.Contains("Operators.RLikePrepared", samples[6].Content);
        Assert.Contains("comparison=Ordinal", samples[7].Content);
        Assert.Contains("strategy=direct-ordinal", samples[7].Content);
        Assert.Contains("fallback=RegexMetacharacter", samples[7].Content);
        Assert.Contains("Operators.PrepareRLike", samples[7].Content);
        Assert.Contains("Operators.RLikePrepared", samples[7].Content);
        Assert.IsFalse(
            samples.Skip(4).Any(static sample =>
                sample.Content.Contains("new Musoq.Evaluator.Operators().RLike", StringComparison.Ordinal)));
    }
}
