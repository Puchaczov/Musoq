using System;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        var cache = PrivateStaticCache("LikeMatcherCache");
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
    public void Like_RuntimeCacheSeparatesMatchersPreparedUnderDifferentCultures()
    {
        var operators = new Operators();
        var cache = PrivateStaticCache("LikeMatcherCache");
        var originalCulture = CultureInfo.CurrentCulture;
        cache.Clear();

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.IsFalse(operators.Like("İ", "I"));

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.IsTrue(operators.Like("İ", "I"));

            Assert.AreEqual(2, cache.Count);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            cache.Clear();
        }
    }

    [TestMethod]
    public void LikeDynamic_RuntimeCacheRemainsBoundedDuringConcurrentHighCardinalityAsciiPrefixChurn()
    {
        var cache = PrivateStaticCache("LikeMatcherCache");
        cache.Clear();
        var prefix = "dynamic-like-churn-" + Guid.NewGuid().ToString("N");

        Parallel.For(0, RuntimeCacheOptions.PatternCacheSize * 4, index =>
        {
            Assert.IsTrue(Operators.LikeDynamic($"{prefix}-{index}-value", $"{prefix}-{index}%"));
        });

        Assert.AreEqual(RuntimeCacheOptions.PatternCacheSize, cache.Count);
        cache.Clear();
    }

    [TestMethod]
    public void LikeDynamic_WhenAsciiPrefixChurnPatternIsPrepared_ShouldKeepLegacyRegexLazy()
    {
        var matcherCache = PrivateStaticCache("LikeMatcherCache");
        var legacyCache = PrivateStaticCache("LegacyLikeMatcherCache");
        matcherCache.Clear();
        legacyCache.Clear();

        try
        {
            _ = Operators.LikeDynamic("dynamic-like-churn-1-value", "dynamic-like-churn-1%");

            Assert.AreEqual(0, legacyCache.Count);
        }
        finally
        {
            matcherCache.Clear();
            legacyCache.Clear();
        }
    }

    [TestMethod]
    public void LikeLegacyRegex_FrontEntryShouldReusePatternAndSeparateCultures()
    {
        var cache = PrivateStaticCache("LegacyLikeMatcherCache");
        var frontField = typeof(Operators).GetField(
            "_legacyLikeMatcherFrontEntry",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new AssertFailedException("Operators legacy LIKE front entry was not found.");
        var originalCulture = CultureInfo.CurrentCulture;
        var prefix = "legacy-front-" + Guid.NewGuid().ToString("N");
        var pattern = prefix + "I";
        cache.Clear();
        frontField.SetValue(null, null);

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.IsFalse(Operators.LikeLegacyRegex(prefix + "İ", pattern));
            var invariantEntry = frontField.GetValue(null);

            Assert.IsFalse(Operators.LikeLegacyRegex(prefix + "İ", pattern));
            Assert.AreSame(invariantEntry, frontField.GetValue(null));

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.IsTrue(Operators.LikeLegacyRegex(prefix + "İ", pattern));
            Assert.AreNotSame(invariantEntry, frontField.GetValue(null));
            Assert.AreEqual(2, cache.Count);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            cache.Clear();
            frontField.SetValue(null, null);
        }
    }

    [TestMethod]
    public void UnicodeAsciiPrefilter_ShouldPreserveKnownInvariantFoldsWithoutGlobalCharacterCache()
    {
        Assert.IsNull(typeof(Operators).GetField(
            "_invariantCharCasingFrontEntry",
            BindingFlags.NonPublic | BindingFlags.Static));

        foreach (var (content, pattern, literal) in new[]
                 {
                     ("Kelvin", "K%", "K"),
                     ("ſample", "S%", "S"),
                     ("Łelvin", "K%", "K"),
                     ("Łódź", "K%", "K")
                 })
        {
            Assert.AreEqual(
                FreshHistoricalLike(content, pattern),
                Operators.LikePrefix(content, literal),
                $"Unexpected result for {content} LIKE {pattern}.");
        }
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

    private static bool FreshHistoricalLike(string input, string pattern)
    {
        var escaped = Regex.Escape(pattern)
            .Replace("_", ".", StringComparison.Ordinal)
            .Replace("%", ".*", StringComparison.Ordinal);
        return Regex.IsMatch(
            input,
            string.Concat(@"\A", escaped, @"\z"),
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking,
            TimeSpan.FromSeconds(5));
    }

    private static PrivateCacheInspection PrivateStaticCache(string fieldName)
    {
        var field = typeof(Operators).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new AssertFailedException($"Operators.{fieldName} private static cache field was not found.");
        var value = field.GetValue(null)
            ?? throw new AssertFailedException($"Operators.{fieldName} private static cache is null.");
        return new PrivateCacheInspection(value);
    }

    private sealed class PrivateCacheInspection(object value)
    {
        private readonly PropertyInfo _count = value.GetType().GetProperty(nameof(Count))
            ?? throw new AssertFailedException("Runtime cache Count property was not found.");
        private readonly MethodInfo _clear = value.GetType().GetMethod(nameof(Clear))
            ?? throw new AssertFailedException("Runtime cache Clear method was not found.");

        public int Count => (int)(_count.GetValue(value)
            ?? throw new AssertFailedException("Runtime cache Count returned null."));

        public void Clear()
        {
            _clear.Invoke(value, null);
        }
    }
}
