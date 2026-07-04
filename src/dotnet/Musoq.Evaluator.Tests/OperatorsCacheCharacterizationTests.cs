using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OperatorsCacheCharacterizationTests
{
    [TestMethod]
    public void Like_CurrentRuntimeCacheAddsEntriesForDistinctPatterns()
    {
        var operators = new Operators();
        var cache = PrivateStaticCache("LikeMatcherCache");
        var before = CacheCount(cache);
        var prefix = "wave1_like_" + Guid.NewGuid().ToString("N");
        var firstPattern = prefix + "_first%";
        var secondPattern = prefix + "_second%";

        Assert.IsTrue(operators.Like(prefix + "_first_value", firstPattern));
        Assert.IsTrue(operators.Like(prefix + "_first_again", firstPattern));
        Assert.IsTrue(operators.Like(prefix + "_second_value", secondPattern));

        var after = CacheCount(cache);
        Assert.IsTrue(
            after >= before + 2,
            "Current LIKE matching caches one unbounded static entry per distinct pattern and reuses duplicate pattern lookups.");
    }

    [TestMethod]
    public void RLike_CurrentRuntimeCacheAddsEntriesForDistinctPatterns()
    {
        var operators = new Operators();
        var cache = PrivateStaticCache("RLikePatternCache");
        var before = CacheCount(cache);
        var prefix = "wave1_rlike_" + Guid.NewGuid().ToString("N");
        var firstPattern = prefix + "_first_[0-9]+";
        var secondPattern = prefix + "_second_[0-9]+";

        Assert.IsTrue(operators.RLike(prefix + "_first_123", firstPattern));
        Assert.IsTrue(operators.RLike(prefix + "_first_456", firstPattern));
        Assert.IsTrue(operators.RLike(prefix + "_second_123", secondPattern));

        var after = CacheCount(cache);
        Assert.IsTrue(
            after >= before + 2,
            "Current RLIKE matching caches one unbounded static regex per distinct pattern and reuses duplicate pattern lookups.");
    }

    private static object PrivateStaticCache(string fieldName)
    {
        var field = typeof(Operators).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new AssertFailedException($"Operators.{fieldName} private static cache field was not found.");
        return field.GetValue(null)
            ?? throw new AssertFailedException($"Operators.{fieldName} private static cache field is null.");
    }

    private static int CacheCount(object cache)
    {
        var countProperty = cache.GetType().GetProperty("Count", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new AssertFailedException($"{cache.GetType().FullName} does not expose Count.");
        return countProperty.GetValue(cache) is int count
            ? count
            : throw new AssertFailedException($"{cache.GetType().FullName}.Count did not return an int.");
    }
}
