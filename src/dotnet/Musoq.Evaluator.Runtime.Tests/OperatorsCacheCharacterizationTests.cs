using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tests.Architecture;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OperatorsCacheCharacterizationTests
{
    [TestMethod]
    public void Like_RuntimeCacheReusesDuplicatePatternsAndStaysBounded()
    {
        var prefix = "like_" + Guid.NewGuid().ToString("N");
        var pattern = prefix + "_first%";
        var operators = new Operators();
        Assert.IsTrue(operators.Like(prefix + "_first_value", pattern));
        var first = Operators.PrepareLike(pattern);
        Assert.IsTrue(operators.Like(prefix + "_first_again", pattern));
        Assert.AreSame(first, Operators.PrepareLike(pattern));
        AssertLikeCacheCapacity(prefix);
    }

    [TestMethod]
    public void Like_RuntimeCacheSeparatesMatchersPreparedUnderDifferentCultures()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var pattern = Guid.NewGuid().ToString("N") + "I";
        var input = pattern[..^1] + "İ";
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.IsFalse(new Operators().Like(input, pattern));
            var invariant = Operators.PrepareLike(pattern);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.IsTrue(new Operators().Like(input, pattern));
            Assert.AreNotSame(invariant, Operators.PrepareLike(pattern));
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.AreSame(invariant, Operators.PrepareLike(pattern));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void LikeDynamic_RuntimeCacheRemainsBoundedDuringConcurrentHighCardinalityAsciiPrefixChurn()
    {
        var prefix = "churn_" + Guid.NewGuid().ToString("N");
        Parallel.For(
            0,
            RuntimeCacheOptions.PatternCacheSize * 4,
            index => Assert.IsTrue(Operators.LikeDynamic($"{prefix}_{index}_value", $"{prefix}_{index}%"))
        );
        AssertLikeCacheCapacity(prefix);
    }

    [TestMethod]
    public void PreparedLikeMatcher_Constructors_ShouldKeepAsciiRegexBehindAnUnevaluatedLazy()
    {
        var lazy = CompilerContractSymbols
            .For(typeof(PreparedLikeMatcher))
            .GetField("_asciiLegacyMatcher", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(lazy);
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(Lazy<>)),
                lazy.Type.OriginalDefinition
            )
        );
        Assert.IsTrue(lazy.IsReadOnly);
        var path = Path.Combine(
            RepositorySourceScan.RepositoryRoot(),
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "PreparedLikeMatcher.cs"
        );
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
        var constructors = root.DescendantNodes().OfType<ConstructorDeclarationSyntax>().ToArray();
        Assert.HasCount(2, constructors);
        var eagerReads = constructors
            .SelectMany(constructor => constructor.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            .Where(member => member.Name.Identifier.ValueText == "Value")
            .ToArray();
        Assert.IsEmpty(eagerReads, "Preparing an ASCII matcher must not force Lazy.Value in its constructors.");
    }

    [TestMethod]
    public void LikeLegacyRegex_ShouldPreserveCasingAcrossRepeatedCallsAndCultureChanges()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var prefix = "legacy_" + Guid.NewGuid().ToString("N");
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.IsFalse(Operators.LikeLegacyRegex(prefix + "İ", prefix + "I"));
            Assert.IsFalse(Operators.LikeLegacyRegex(prefix + "İ", prefix + "I"));
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.IsTrue(Operators.LikeLegacyRegex(prefix + "İ", prefix + "I"));
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.IsFalse(Operators.LikeLegacyRegex(prefix + "İ", prefix + "I"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void UnicodeAsciiPrefilter_ShouldPreserveKnownInvariantFoldsWithoutGlobalCharacterCache()
    {
        Assert.IsNull(
            CompilerContractSymbols
                .For(typeof(Operators))
                .GetField("_invariantCharCasingFrontEntry", BindingFlags.NonPublic | BindingFlags.Static)
        );
        foreach (
            var (content, pattern, literal) in new[]
            {
                ("Kelvin", "K%", "K"),
                ("ſample", "S%", "S"),
                ("Łelvin", "K%", "K"),
                ("Łódź", "K%", "K"),
            }
        )
            Assert.AreEqual(FreshHistoricalLike(content, pattern), Operators.LikePrefix(content, literal));
    }

    [TestMethod]
    public void RLike_RuntimeCacheReusesDuplicatePatternsAndStaysBounded()
    {
        var prefix = "rlike_" + Guid.NewGuid().ToString("N");
        var first = ResolveRegex(prefix, 0);
        Assert.AreSame(first, ResolveRegex(prefix, 0));
        for (var index = 1; index < RuntimeCacheOptions.PatternCacheSize; index++)
            _ = ResolveRegex(prefix, index);
        Assert.AreSame(first, ResolveRegex(prefix, 0));
        _ = ResolveRegex(prefix, RuntimeCacheOptions.PatternCacheSize);
        Assert.AreNotSame(first, ResolveRegex(prefix, 0));
    }

    [TestMethod]
    public void RLike_RuntimeCacheSeparatesCultureSensitivePatterns()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var prefix = "culture_" + Guid.NewGuid().ToString("N");
        var pattern = "(?i)\\A" + prefix + "i(?<tail>x)\\k<tail>\\z";
        var input = prefix + "Ixx";
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var english = Operators.PrepareRLike(pattern)!;
            Assert.IsTrue(english.IsMatch(input));
            var englishRegex = english.ResolvedRegex;
            Assert.IsNotNull(englishRegex);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var turkish = Operators.PrepareRLike(pattern)!;
            Assert.IsFalse(turkish.IsMatch(input));
            Assert.AreNotSame(englishRegex, turkish.ResolvedRegex);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var englishAgain = Operators.PrepareRLike(pattern)!;
            Assert.IsTrue(englishAgain.IsMatch(input));
            Assert.AreSame(englishRegex, englishAgain.ResolvedRegex);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    private static void AssertLikeCacheCapacity(string prefix)
    {
        var firstPattern = prefix + "_capacity_0%";
        var first = Operators.PrepareLike(firstPattern);
        Assert.IsNotNull(first);
        for (var index = 1; index < RuntimeCacheOptions.PatternCacheSize; index++)
            _ = Operators.PrepareLike($"{prefix}_capacity_{index}%");
        Assert.AreSame(first, Operators.PrepareLike(firstPattern));
        _ = Operators.PrepareLike($"{prefix}_capacity_{RuntimeCacheOptions.PatternCacheSize}%");
        Assert.AreNotSame(first, Operators.PrepareLike(firstPattern));
    }

    private static Regex ResolveRegex(string prefix, int index)
    {
        var pattern = $@"\A{prefix}_{index}_(?<value>[0-9]+)_\k<value>\z";
        var matcher = Operators.PrepareRLike(pattern)!;
        Assert.IsTrue(matcher.IsMatch($"{prefix}_{index}_123_123"));
        Assert.IsNotNull(matcher.ResolvedRegex);
        return matcher.ResolvedRegex;
    }

    private static bool FreshHistoricalLike(string input, string pattern)
    {
        var escaped = Regex
            .Escape(pattern)
            .Replace("_", ".", StringComparison.Ordinal)
            .Replace("%", ".*", StringComparison.Ordinal);
        return Regex.IsMatch(
            input,
            string.Concat(@"\A", escaped, @"\z"),
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking,
            TimeSpan.FromSeconds(5)
        );
    }
}
