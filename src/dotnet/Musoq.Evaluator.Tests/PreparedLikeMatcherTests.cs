using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class PreparedLikeMatcherTests
{
    [TestMethod]
    public void CreateLegacyLikeRegexPattern_ShouldEscapeAndExpandInOneCanonicalForm()
    {
        var pattern = Operators.CreateLegacyLikeRegexPattern(@"a.c$^{[(|)*+?\_%");

        Assert.AreEqual(@"\Aa\.c\$\^\{\[\(\|\)\*\+\?\\..*\z", pattern);
    }

    [TestMethod]
    public void CreateLegacyLikeRegexPattern_WhenPatternIsLong_ShouldUseCheckedCompleteExpansion()
    {
        var pattern = new string('%', 4096);

        var regexPattern = Operators.CreateLegacyLikeRegexPattern(pattern);

        Assert.AreEqual(8196, regexPattern.Length);
        Assert.IsTrue(regexPattern.StartsWith(@"\A.*.*", StringComparison.Ordinal));
        Assert.IsTrue(regexPattern.EndsWith(@".*.*\z", StringComparison.Ordinal));
    }

    [TestMethod]
    [DataRow("literal")]
    [DataRow("literal%")]
    [DataRow("%literal")]
    [DataRow("%literal%")]
    [DataRow("Żółć%")]
    public void LegacyMatcher_WhenShapeHasOnlySimpleBoundaryWildcards_ShouldUseTimeoutBoundedBacktracking(
        string pattern)
    {
        Assert.IsTrue(Operators.CanUseTimeoutBoundedBacktrackingMatcher(pattern));
    }

    [TestMethod]
    [DataRow("%")]
    [DataRow("%%")]
    [DataRow("literal%%")]
    [DataRow("%literal%%")]
    [DataRow("lit%eral")]
    [DataRow("lit_ral")]
    public void LegacyMatcher_WhenShapeCanBacktrackRepeatedly_ShouldKeepNonBacktrackingEngine(string pattern)
    {
        Assert.IsFalse(Operators.CanUseTimeoutBoundedBacktrackingMatcher(pattern));
    }

    [TestMethod]
    [DataRow("literal", @"\Aliteral\z")]
    [DataRow("literal%", @"\Aliteral")]
    [DataRow("%literal", @"literal\z")]
    [DataRow("%literal%", "literal")]
    [DataRow("a.c%", @"\Aa\.c")]
    [DataRow("literal%%", @"\Aliteral.*.*\z")]
    public void OptimizedLegacyRegexPattern_ShouldRemoveOnlyRedundantBoundaryWildcards(
        string pattern,
        string expectedRegex)
    {
        Assert.AreEqual(expectedRegex, Operators.CreateOptimizedLegacyLikeRegexPattern(pattern));
    }

    [TestMethod]
    public void PrepareLike_WhenBoundaryWildcardsRepeat_ShouldRetainOriginalPatternAndLiteralOffsets()
    {
        var matcher = Operators.PrepareLike("%%%needle%%")!;

        Assert.AreEqual("%%%needle%%", matcher.OriginalPattern);
        Assert.AreEqual(3, matcher.LiteralStart);
        Assert.AreEqual(6, matcher.LiteralLength);
        Assert.IsTrue(matcher.IsMatch("prefix-needle-suffix"));
        Assert.IsFalse(matcher.IsMatch("prefix-other-suffix"));
    }

    [TestMethod]
    public void PrepareLike_WhenAsciiShapeNeedsUnicodeFallback_ShouldUseOriginalPattern()
    {
        var matcher = Operators.PrepareLike("%%%K%%")!;

        Assert.AreEqual(FreshHistoricalLike("unit-K-value", "%%%K%%"), matcher.IsMatch("unit-K-value"));
    }

    [TestMethod]
    public void PrepareLike_WhenPatternUsesUnsupportedShape_ShouldUsePreparedLegacyMatcher()
    {
        var matcher = Operators.PrepareLike("h_%o")!;

        Assert.IsTrue(matcher.IsMatch("hello"));
        Assert.IsFalse(matcher.IsMatch("help"));
    }

    [TestMethod]
    public void PrepareLike_WhenPatternAndCultureRepeat_ShouldReusePreparedMatcher()
    {
        var pattern = $"prepared-cache-{Guid.NewGuid():N}_Ż%";

        var first = Operators.PrepareLike(pattern);
        var second = Operators.PrepareLike(pattern);

        Assert.AreSame(first, second);
    }

    [TestMethod]
    public void PrepareLike_WhenCultureChanges_ShouldNotReuseMatcherPreparedUnderAnotherCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var pattern = $"prepared-culture-{Guid.NewGuid():N}-I";

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var invariant = Operators.PrepareLike(pattern);

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var turkish = Operators.PrepareLike(pattern);

            Assert.AreNotSame(invariant, turkish);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void LikeDynamic_WhenOperandsAreNull_ShouldReturnFalse()
    {
        Assert.IsFalse(Operators.LikeDynamic(null, "value"));
        Assert.IsFalse(Operators.LikeDynamic("value", null));
        Assert.IsFalse(Operators.LikeDynamic(null, null));
    }

    [TestMethod]
    public void LikeDynamic_WhenPatternsAlternate_ShouldMatchInstanceCompatibilityWrapper()
    {
        var operators = new Operators();
        var cases = new[]
        {
            (Input: "alphabet", Pattern: "a%"),
            (Input: "alphabet", Pattern: "%bet"),
            (Input: "alphabet", Pattern: "%pha%"),
            (Input: "alphabet", Pattern: "a_p%"),
            (Input: "Żółć", Pattern: "ż%")
        };

        for (var repetition = 0; repetition < 8; repetition++)
        {
            foreach (var (input, pattern) in cases)
            {
                Assert.AreEqual(
                    operators.Like(input, pattern),
                    Operators.LikeDynamic(input, pattern),
                    $"Input '{input}', pattern '{pattern}', repetition {repetition}.");
            }
        }
    }

    [TestMethod]
    public void LikeDynamic_WhenSingleAsciiPrefixIsCached_ShouldPreserveAsciiAndUnicodeSemantics()
    {
        var cache = new LikeMatcherCacheSlot();
        var inputs = new[] { "Kilo", "kilo", "other", "Kelvin", "İstanbul", string.Empty };

        foreach (var input in inputs)
        {
            Assert.AreEqual(
                FreshHistoricalLike(input, "K%"),
                Operators.LikeDynamic(input, "K%", cache),
                input);
        }

        Assert.AreEqual(1, cache.Count);
        Assert.AreEqual(1, cache.MatcherConstructionCount);
    }

    private static bool FreshHistoricalLike(string input, string pattern)
    {
        var escaped = Regex.Escape(pattern);
        var sqlPattern = escaped
            .Replace("_", ".", StringComparison.Ordinal)
            .Replace("%", ".*", StringComparison.Ordinal);
        return Regex.IsMatch(
            input,
            string.Concat(@"\A", sqlPattern, @"\z"),
            RegexOptions.IgnoreCase | RegexOptions.Singleline,
            TimeSpan.FromSeconds(1));
    }
}
