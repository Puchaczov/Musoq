using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class LikeUnicodePatternSemanticsTests
{
    [TestMethod]
    [DataRow("")]
    [DataRow("en-US")]
    [DataRow("pl-PL")]
    [DataRow("tr-TR")]
    public void UnicodeSimpleShapes_ShouldMatchFreshHistoricalRegex(string cultureName)
    {
        var cases = new (string Input, string Pattern)[]
        {
            ("Żółć", "Żółć"),
            ("Żółć-value", "Żółć%"),
            ("value-Żółć", "%Żółć"),
            ("value-Żółć-tail", "%Żółć%"),
            ("i", "İ"),
            ("ı", "I"),
            ("Σ", "ς"),
            ("Kelvin", "k%"),
            ("é", "e\u0301"),
            ("\uD83D\uDE00-smile", "\uD83D\uDE00%")
        };

        RunUnderCulture(cultureName, () =>
        {
            var operators = new Operators();
            foreach (var (input, pattern) in cases)
            {
                Assert.AreEqual(
                    FreshHistoricalLike(input, pattern),
                    operators.Like(input, pattern),
                    $"Input '{Display(input)}', pattern '{Display(pattern)}', culture '{cultureName}'.");
            }
        });
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("en-US")]
    [DataRow("pl-PL")]
    [DataRow("tr-TR")]
    public void GeneratedUnicodePatternCorpus_ShouldMatchFreshHistoricalRegex(string cultureName)
    {
        RunUnderCulture(cultureName, () =>
        {
            var operators = new Operators();
            var random = new Random(cultureName switch
            {
                "" => 0x51A7,
                "en-US" => 0x51A8,
                "pl-PL" => 0x51A9,
                "tr-TR" => 0x51AA,
                _ => throw new AssertFailedException($"Unexpected culture '{cultureName}'.")
            });
            for (var sample = 0; sample < 256; sample++)
            {
                var literal = CreateUnicodeLiteral(random, sample);
                var pattern = (sample % 4) switch
                {
                    0 => literal,
                    1 => string.Concat(literal, "%"),
                    2 => string.Concat("%", literal),
                    _ => string.Concat("%", literal, "%")
                };
                var input = sample % 3 == 0
                    ? string.Concat("head-", literal, "-tail")
                    : CreateUnicodeLiteral(random, sample + 1_000);

                Assert.AreEqual(
                    FreshHistoricalLike(input, pattern),
                    operators.Like(input, pattern),
                    $"Sample {sample}, input '{Display(input)}', pattern '{Display(pattern)}', culture '{cultureName}'.");
            }
        });
    }

    [TestMethod]
    public void UnicodeSimpleShapes_WithSameCase_ShouldTakeSemanticsPreservingPositivePath()
    {
        var operators = new Operators();

        Assert.IsTrue(operators.Like("Żółć", "Żółć"));
        Assert.IsTrue(operators.Like("Żółć-tail", "Żółć%"));
        Assert.IsTrue(operators.Like("head-Żółć", "%Żółć"));
        Assert.IsTrue(operators.Like("head-Żółć-tail", "%Żółć%"));
    }

    private static string CreateUnicodeLiteral(Random random, int sample)
    {
        char[] alphabet = ['Ż', 'ó', 'ł', 'ć', 'İ', 'ı', 'Σ', 'ς', 'K', 'é', '\u0301', 'a', 'K'];
        var length = 1 + random.Next(1, 12);
        var builder = new StringBuilder(length + 8);
        builder.Append('Ż');
        builder.Append(sample.ToString("X", CultureInfo.InvariantCulture));
        while (builder.Length < length)
            builder.Append(alphabet[random.Next(alphabet.Length)]);
        return builder.ToString();
    }

    private static bool FreshHistoricalLike(string input, string pattern)
    {
        var escaped = Regex.Escape(pattern)
            .Replace("_", ".", StringComparison.Ordinal)
            .Replace("%", ".*", StringComparison.Ordinal);
        var regex = new Regex(
            string.Concat(@"\A", escaped, @"\z"),
            RegexOptions.Singleline |
            RegexOptions.IgnoreCase |
            RegexOptions.Compiled |
            RegexOptions.NonBacktracking,
            TimeSpan.FromSeconds(5));
        return regex.IsMatch(input);
    }

    private static void RunUnderCulture(string cultureName, Action action)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var culture = string.IsNullOrEmpty(cultureName)
                ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static string Display(string value) =>
        value.Replace("\u0301", "<combining-acute>", StringComparison.Ordinal);
}
