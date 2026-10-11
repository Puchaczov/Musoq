using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RLikeLiteralIdentityCacheTests
{
    [TestMethod]
    [DataRow("plain")]
    [DataRow("Łódź")]
    [DataRow("")]
    [DataRow(@"\Aplain\z")]
    [DataRow(@"\Aplain")]
    [DataRow(@"plain\z")]
    [DataRow("(?!)")]
    public void CacheHit_WhenInputAndPatternShareIdentity_ShouldAgreeWithRegex(string pattern)
    {
        var slot = new RLikeMatcherCacheSlot();
        var first = new string(pattern.ToCharArray());
        Operators.RLikeDynamic(first, first, slot);
        var second = new string(pattern.ToCharArray());
        var reference = new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(2));

        var result = Operators.RLikeDynamic(second, second, slot);

        Assert.AreEqual(reference.IsMatch(second), result);
        Assert.AreEqual(1, slot.MatcherConstructionCount);
    }

    [TestMethod]
    public void SharedInvalidPattern_ShouldRetainLazySyntaxFailure()
    {
        var slot = new RLikeMatcherCacheSlot();
        const string pattern = "[";
        Assert.IsFalse(Operators.RLikeDynamic(null, pattern, slot));

        Assert.Throws<InvalidRuntimeRegexException>(() => Operators.RLikeDynamic(pattern, pattern, slot));
    }

    [TestMethod]
    public void FactoryWithDifferentPattern_ShouldFailBeforeIdentityOptimization()
    {
        var slot = new RLikeMatcherCacheSlot(false, static _ => Operators.PrepareRLike("different")!);

        Assert.Throws<InvalidOperationException>(() => Operators.RLikeDynamic("literal", "literal", slot));
    }
}
