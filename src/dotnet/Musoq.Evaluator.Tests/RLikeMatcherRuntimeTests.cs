using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class RLikeMatcherRuntimeTests
{
    [TestMethod]
    public void PrepareRLike_WhenPatternIsInvalid_ShouldDelayFailureUntilNonNullMatch()
    {
        var matcher = Operators.PrepareRLike("[");

        Assert.IsNotNull(matcher);
        Assert.IsFalse(Operators.RLikePrepared(null, matcher));
        Assert.Throws<ArgumentException>(() => Operators.RLikePrepared("value", matcher));
    }

    [TestMethod]
    public void PrepareRLike_WhenPatternIsNull_ShouldPreserveNullAsFalse()
    {
        var matcher = Operators.PrepareRLike(null);

        Assert.IsNull(matcher);
        Assert.IsFalse(Operators.RLikePrepared("value", matcher));
        Assert.IsFalse(Operators.RLikePrepared(null, matcher));
    }

    [TestMethod]
    public void PreparedMatcher_WhenCultureChanges_ShouldResolveCultureSpecificRegex()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var matcher = Operators.PrepareRLike(@"(?i)\Ai\z");
        Assert.IsNotNull(matcher);

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.IsTrue(matcher.IsMatch("I"));
            Assert.IsTrue(matcher.IsMatch("I"));

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            Assert.IsFalse(matcher.IsMatch("I"));

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.IsTrue(matcher.IsMatch("I"));
            Assert.AreEqual(3, matcher.RegexResolutionCount);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void DynamicMatcher_WhenInputOrPatternIsNull_ShouldAvoidCacheMutation()
    {
        var slot = new RLikeMatcherCacheSlot();

        Assert.IsFalse(Operators.RLikeDynamic(null, "[", slot));
        Assert.IsFalse(Operators.RLikeDynamic("value", null, slot));
        Assert.AreEqual(0, slot.Count);
        Assert.AreEqual(0, slot.MatcherConstructionCount);
    }

    [TestMethod]
    public void DynamicMatcher_ShouldRetainTwoEntriesAndEvictInFifoOrder()
    {
        var constructions = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
        var slot = new RLikeMatcherCacheSlot(false, pattern =>
        {
            constructions.AddOrUpdate(pattern, 1, static (_, count) => count + 1);
            return CreateMatcher(pattern);
        });

        Assert.IsTrue(Operators.RLikeDynamic("alpha", "alpha", slot));
        Assert.IsTrue(Operators.RLikeDynamic("beta", "beta", slot));
        Assert.IsTrue(Operators.RLikeDynamic("alpha", "alpha", slot));
        Assert.IsTrue(Operators.RLikeDynamic("gamma", "gamma", slot));
        Assert.IsTrue(Operators.RLikeDynamic("alpha", "alpha", slot));

        Assert.AreEqual(2, slot.Count);
        Assert.AreEqual(4, slot.MatcherConstructionCount);
        Assert.AreEqual(2, constructions["alpha"]);
        Assert.AreEqual(1, constructions["beta"]);
        Assert.AreEqual(1, constructions["gamma"]);
    }

    [TestMethod]
    public void DynamicMatcher_WhenCardinalityExceedsCapacity_ShouldStayBounded()
    {
        var slot = new RLikeMatcherCacheSlot(false, CreateMatcher);

        for (var index = 0; index < 1_024; index++)
        {
            var pattern = $"pattern-{index}";
            Assert.IsTrue(Operators.RLikeDynamic(pattern, pattern, slot));
        }

        Assert.AreEqual(2, slot.Count);
        Assert.AreEqual(1_024, slot.MatcherConstructionCount);
    }

    [TestMethod]
    public void DynamicMatcher_WhenLiteralPatternRepeats_ShouldClassifyOnceAndNeverConstructRegex()
    {
        var matcherConstructions = 0;
        var regexConstructions = 0;
        var slot = new RLikeMatcherCacheSlot(false, pattern =>
        {
            matcherConstructions++;
            return new PreparedRLikeMatcher(pattern, (value, _) =>
            {
                regexConstructions++;
                return new Regex(value, RegexOptions.Compiled, RuntimeCacheOptions.DefaultRegexTimeout);
            });
        });

        Assert.IsTrue(Operators.RLikeDynamic("head-alpha-tail", "alpha", slot));
        Assert.IsTrue(Operators.RLikeDynamic("alpha", "alpha", slot));

        Assert.AreEqual(1, matcherConstructions);
        Assert.AreEqual(1, slot.MatcherConstructionCount);
        Assert.AreEqual(0, regexConstructions);
    }

    [TestMethod]
    public void PreparedGenericMatcher_ShouldUseConfiguredRegexTimeout()
    {
        Regex? created = null;
        var matcher = new PreparedRLikeMatcher("v.lue", (pattern, _) =>
        {
            created = new Regex(pattern, RegexOptions.Compiled, RuntimeCacheOptions.DefaultRegexTimeout);
            return created;
        });

        Assert.IsTrue(matcher.IsMatch("value"));
        Assert.IsNotNull(created);
        Assert.AreEqual(RuntimeCacheOptions.DefaultRegexTimeout, created.MatchTimeout);
    }

    [TestMethod]
    public void InstanceWrapper_ShouldRemainEquivalentToStaticDynamicMatching()
    {
        var operators = new Operators();

        Assert.AreEqual(Operators.RLikeDynamic("alphabet", "pha"), operators.RLike("alphabet", "pha"));
        Assert.AreEqual(Operators.RLikeDynamic(null, "pha"), operators.RLike(null, "pha"));
        Assert.AreEqual(Operators.RLikeDynamic("alphabet", null), operators.RLike("alphabet", null));
    }

    private static PreparedRLikeMatcher CreateMatcher(string pattern) => new(
        pattern,
        static (value, _) => new Regex(value, RegexOptions.None, RuntimeCacheOptions.DefaultRegexTimeout));
}
