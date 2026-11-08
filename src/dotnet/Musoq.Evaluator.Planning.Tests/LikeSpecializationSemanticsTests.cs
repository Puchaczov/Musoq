using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class LikeSpecializationSemanticsTests
{
    [TestMethod]
    public void SpecializedHelpers_NullMatrix_ShouldRemainFalse()
    {
        Assert.IsFalse(Operators.LikeExact(null, "value"));
        Assert.IsFalse(Operators.LikeExact("value", null));
        Assert.IsFalse(Operators.LikePrefix(null, "value"));
        Assert.IsFalse(Operators.LikeSuffix(null, "value"));
        Assert.IsFalse(Operators.LikeContains(null, "value"));
        Assert.IsFalse(Operators.LikeLegacyRegex(null, "value"));
        Assert.IsFalse(Operators.LikeLegacyRegex("value", null));
        Assert.IsFalse(Operators.LikeLegacyRegexSingleAsciiPrefix(null, "K%", "K"));
    }

    [TestMethod]
    public void SpecializedHelpers_UnicodeInputs_ShouldMatchLegacyRegex()
    {
        AssertMatchesLegacy("K", "K", Operators.LikeExact("K", "K"));
        AssertMatchesLegacy("İ", "I", Operators.LikeExact("İ", "I"));
        AssertMatchesLegacy("Kelvin", "K%", Operators.LikePrefix("Kelvin", "K"));
        AssertMatchesLegacy("unit-K", "%K", Operators.LikeSuffix("unit-K", "K"));
        AssertMatchesLegacy("unit-K-value", "%K%", Operators.LikeContains("unit-K-value", "K"));
        AssertMatchesLegacy(
            "Kelvin",
            "K%",
            Operators.LikeLegacyRegexSingleAsciiPrefix("Kelvin", "K%", "K"));
    }

    [TestMethod]
    public void SpecializedHelpers_RegexMetacharacters_ShouldBeTreatedAsLiterals()
    {
        const string literal = @"a.c$^[x](y)*+?|\";

        AssertMatchesLegacy(literal, literal, Operators.LikeExact(literal, literal));
        AssertMatchesLegacy(literal + "tail", literal + "%", Operators.LikePrefix(literal + "tail", literal));
        AssertMatchesLegacy("head" + literal, "%" + literal, Operators.LikeSuffix("head" + literal, literal));
        AssertMatchesLegacy("head" + literal + "tail", "%" + literal + "%", Operators.LikeContains("head" + literal + "tail", literal));
    }

    [TestMethod]
    public void SpecializedHelpers_GeneratedUnicodeCorpus_ShouldMatchLegacyRegex()
    {
        var random = new Random(19450317);
        var alphabet = new[] { 'a', 'A', 'k', 'K', 'i', 'I', 'K', 'İ', 'ß', 'Ż', 'Σ', 'ς', 'σ', '-', '.' };

        for (var sample = 0; sample < 512; sample++)
        {
            var builder = new StringBuilder();
            var length = random.Next(0, 24);
            for (var index = 0; index < length; index++)
                builder.Append(alphabet[random.Next(alphabet.Length)]);

            var input = builder.ToString();
            AssertMatchesLegacy(input, "K", Operators.LikeExact(input, "K"));
            AssertMatchesLegacy(input, "K%", Operators.LikePrefix(input, "K"));
            AssertMatchesLegacy(input, "%K", Operators.LikeSuffix(input, "K"));
            AssertMatchesLegacy(input, "%K%", Operators.LikeContains(input, "K"));
        }
    }

    [TestMethod]
    public void ConservativeUnicodePrefilter_ShouldNeverRejectRegexPositiveUtf16CodeUnit()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var cultures = new[]
        {
            CultureInfo.InvariantCulture,
            CultureInfo.GetCultureInfo("en-US"),
            CultureInfo.GetCultureInfo("pl-PL"),
            CultureInfo.GetCultureInfo("tr-TR")
        };
        Span<char> input = stackalloc char[1];
        Span<char> literal = stackalloc char[1];

        try
        {
            foreach (var culture in cultures)
            {
                CultureInfo.CurrentCulture = culture;
                for (var letter = 'A'; letter <= 'Z'; letter++)
                {
                    literal[0] = letter;
                    var regex = new Regex(
                        string.Concat(@"\A", letter, @"\z"),
                        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking,
                        TimeSpan.FromSeconds(5));

                    for (var codeUnit = char.MinValue; ; codeUnit++)
                    {
                        input[0] = codeUnit;
                        if (regex.IsMatch(input))
                        {
                            if (!input.Equals(literal, StringComparison.OrdinalIgnoreCase))
                            {
                                Assert.IsTrue(
                                    letter is 'I' or 'K' or 'S',
                                    $"Culture '{culture.Name}', ASCII '{letter}', code unit U+{(int)codeUnit:X4} has an unexpected non-ordinal regex fold.");
                            }

                            Assert.IsTrue(
                                Operators.MayMatchAsciiLiteralUnderCulture(
                                    input,
                                    literal,
                                    LikePatternMatchKind.Exact,
                                    culture.TextInfo),
                                $"Culture '{culture.Name}', ASCII '{letter}', code unit U+{(int)codeUnit:X4}.");
                        }

                        if (codeUnit == char.MaxValue)
                            break;
                    }
                }
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    [DataRow("en-US")]
    [DataRow("pl-PL")]
    [DataRow("tr-TR")]
    public void SpecializedHelpers_AcrossCultures_ShouldMatchLegacyRegex(string cultureName)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            var input = string.Concat("İ", cultureName);
            var literal = string.Concat("I", cultureName);

            AssertMatchesLegacy(input, literal, Operators.LikeExact(input, literal));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void SpecializedHelpers_UnderTurkishCulture_ShouldNotUseOrdinalAsciiPositiveShortcut()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

            AssertMatchesLegacy("i", "I", Operators.LikeExact("i", "I"));
            AssertMatchesLegacy("item", "I%", Operators.LikePrefix("item", "I"));
            AssertMatchesLegacy("unit-i", "%I", Operators.LikeSuffix("unit-i", "I"));
            AssertMatchesLegacy("unit-i-value", "%I%", Operators.LikeContains("unit-i-value", "I"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void LegacyMatcher_RepeatedBoundaryPercents_ShouldMatchSpecializedHelpers()
    {
        Assert.AreEqual(LegacyLike("Google", "Google%%%"), Operators.LikePrefix("Google", "Google"));
        Assert.AreEqual(LegacyLike("Google", "%%%Google"), Operators.LikeSuffix("Google", "Google"));
        Assert.AreEqual(LegacyLike("Google", "%%%Google%%%"), Operators.LikeContains("Google", "Google"));
        Assert.AreEqual(LegacyLike("K", "%%%K%%%"), Operators.LikeContains("K", "K"));
    }

    private static void AssertMatchesLegacy(string input, string pattern, bool actual) =>
        Assert.AreEqual(LegacyLike(input, pattern), actual, $"Input '{input}', pattern '{pattern}'.");

    private static bool LegacyLike(string input, string pattern)
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
}
