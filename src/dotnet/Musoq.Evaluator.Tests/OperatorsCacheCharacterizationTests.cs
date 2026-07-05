using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OperatorsCacheCharacterizationTests
{
    [TestMethod]
    public void Like_RuntimeCacheReusesDuplicatePatternsAndStaysBounded()
    {
        var operators = new Operators();
        var cache = PrivateStaticCache<string, Func<string, bool>>("LikeMatcherCache");
        cache.Clear();
        var prefix = "wave1_like_" + Guid.NewGuid().ToString("N");
        var firstPattern = prefix + "_first%";
        var secondPattern = prefix + "_second%";

        Assert.IsTrue(operators.Like(prefix + "_first_value", firstPattern));
        Assert.IsTrue(operators.Like(prefix + "_first_again", firstPattern));
        Assert.IsTrue(operators.Like(prefix + "_second_value", secondPattern));

        Assert.AreEqual(2, cache.Count);

        for (var i = 0; i < RuntimeCacheOptions.PatternCacheSize + 10; i++)
            Assert.IsTrue(operators.Like($"{prefix}_bounded_{i}_value", $"{prefix}_bounded_{i}%"));

        Assert.AreEqual(RuntimeCacheOptions.PatternCacheSize, cache.Count);
    }

    [TestMethod]
    public void RLike_RuntimeCacheReusesDuplicatePatternsAndStaysBounded()
    {
        var operators = new Operators();
        var cache = PrivateStaticCache<string, Regex>("RLikePatternCache");
        cache.Clear();
        var prefix = "wave1_rlike_" + Guid.NewGuid().ToString("N");
        var firstPattern = prefix + "_first_[0-9]+";
        var secondPattern = prefix + "_second_[0-9]+";

        Assert.IsTrue(operators.RLike(prefix + "_first_123", firstPattern));
        Assert.IsTrue(operators.RLike(prefix + "_first_456", firstPattern));
        Assert.IsTrue(operators.RLike(prefix + "_second_123", secondPattern));

        Assert.AreEqual(2, cache.Count);
        Assert.IsTrue(cache.TryGetValue(firstPattern, out var regex));
        Assert.AreEqual(RuntimeCacheOptions.DefaultRegexTimeout, regex.MatchTimeout);

        for (var i = 0; i < RuntimeCacheOptions.PatternCacheSize + 10; i++)
            Assert.IsTrue(operators.RLike($"{prefix}_bounded_{i}_123", $"{prefix}_bounded_{i}_[0-9]+"));

        Assert.AreEqual(RuntimeCacheOptions.PatternCacheSize, cache.Count);
    }

    private static BoundedRuntimeCache<TKey, TValue> PrivateStaticCache<TKey, TValue>(string fieldName)
        where TKey : notnull
    {
        var field = typeof(Operators).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new AssertFailedException($"Operators.{fieldName} private static cache field was not found.");
        return field.GetValue(null) as BoundedRuntimeCache<TKey, TValue>
            ?? throw new AssertFailedException($"Operators.{fieldName} private static cache has an unexpected type.");
    }
}
