using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class RLikeLiteralPatternClassifierTests
{
    [TestMethod]
    public void Classify_SupportedAnchorShapes_ShouldRetainLiteralSpan()
    {
        var cases = new[]
        {
            ("alpha", RLikeLiteralMatchKind.Contains, RLikeLiteralAnchorKind.None, 0, 5),
            (@"\Aalpha", RLikeLiteralMatchKind.Prefix, RLikeLiteralAnchorKind.Start, 2, 5),
            (@"alpha\z", RLikeLiteralMatchKind.Suffix, RLikeLiteralAnchorKind.End, 0, 5),
            (@"\Aalpha\z", RLikeLiteralMatchKind.Exact, RLikeLiteralAnchorKind.StartAndEnd, 2, 5),
            (string.Empty, RLikeLiteralMatchKind.Contains, RLikeLiteralAnchorKind.None, 0, 0),
            (@"\A", RLikeLiteralMatchKind.Prefix, RLikeLiteralAnchorKind.Start, 2, 0),
            (@"\z", RLikeLiteralMatchKind.Suffix, RLikeLiteralAnchorKind.End, 0, 0),
            (@"\A\z", RLikeLiteralMatchKind.Exact, RLikeLiteralAnchorKind.StartAndEnd, 2, 0),
            ("Żółć\n😀", RLikeLiteralMatchKind.Contains, RLikeLiteralAnchorKind.None, 0, 7)
        };

        foreach (var (pattern, kind, anchors, start, length) in cases)
        {
            Assert.AreEqual(
                new RLikeLiteralClassification(kind, anchors, start, length),
                RLikeLiteralPatternClassifier.Classify(pattern).Classification,
                pattern);
        }
    }

    [TestMethod]
    public void Classify_RegexSyntaxOrUnsupportedEscape_ShouldRemainOnRegex()
    {
        var metacharacters = new[]
        {
            ".", "^alpha", "alpha$", "a|b", "a?", "a*", "a+", "(a)", "[a]", "a{2}", "a]", "a}"
        };
        foreach (var pattern in metacharacters)
        {
            var result = RLikeLiteralPatternClassifier.Classify(pattern);
            Assert.IsNull(result.Classification, pattern);
            Assert.AreEqual(RLikeLiteralRejectionReason.RegexMetacharacter, result.RejectionReason, pattern);
        }

        foreach (var pattern in new[] { @"a\d", @"a\.", @"\Zalpha", @"\Aalpha\z\z" })
        {
            var result = RLikeLiteralPatternClassifier.Classify(pattern);
            Assert.IsNull(result.Classification, pattern);
            Assert.AreEqual(RLikeLiteralRejectionReason.UnsupportedEscape, result.RejectionReason, pattern);
        }
    }

    [TestMethod]
    public void PreparedLiteralMatchers_ShouldUseOrdinalSpansWithoutConstructingRegex()
    {
        var regexConstructions = 0;
        foreach (var (pattern, input, expected) in FixedCases())
        {
            var matcher = new PreparedRLikeMatcher(pattern, (value, _) =>
            {
                regexConstructions++;
                return FreshHistoricalRegex(value);
            });

            Assert.AreEqual(expected, matcher.IsMatch(input), $"Pattern '{pattern}', input '{input}'.");
            Assert.AreEqual(0, matcher.RegexResolutionCount, pattern);
        }

        Assert.AreEqual(0, regexConstructions);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("en-US")]
    [DataRow("pl-PL")]
    [DataRow("tr-TR")]
    public void FixedAndGeneratedLiteralCorpus_ShouldMatchFreshHistoricalRegex(string cultureName)
    {
        RunUnderCulture(cultureName, () =>
        {
            foreach (var (pattern, input, _) in FixedCases())
                AssertMatchesHistorical(input, pattern, cultureName);

            var random = new Random(0x5210 + cultureName.Length);
            for (var sample = 0; sample < 64; sample++)
            {
                var literal = CreateLiteral(random, sample);
                var input = sample % 3 == 0
                    ? string.Concat("head-", literal, "-tail")
                    : CreateLiteral(random, sample + 1_000);
                foreach (var pattern in new[]
                         {
                             literal,
                             string.Concat(@"\A", literal),
                             string.Concat(literal, @"\z"),
                             string.Concat(@"\A", literal, @"\z")
                         })
                {
                    AssertMatchesHistorical(input, pattern, cultureName);
                }
            }
        });
    }

    private static (string Pattern, string Input, bool Expected)[] FixedCases() =>
    [
        ("alpha", "head-alpha-tail", true),
        (@"\Aalpha", "alphabet", true),
        (@"alpha\z", "betalpha", true),
        (@"\Aalpha\z", "alpha", true),
        (@"\Aalpha\z", "alphabet", false),
        ("Żółć", "head-Żółć-tail", true),
        (@"\A😀", "😀-tail", true),
        ("\n", "head\nend", true),
        (string.Empty, "anything", true),
        (@"\A\z", string.Empty, true),
        (@"\A\z", "value", false)
    ];

    private static void AssertMatchesHistorical(string input, string pattern, string cultureName)
    {
        var matcher = Operators.PrepareRLike(pattern);
        Assert.IsNotNull(matcher);
        Assert.AreEqual(
            FreshHistoricalRegex(pattern).IsMatch(input),
            matcher.IsMatch(input),
            $"Pattern '{Display(pattern)}', input '{Display(input)}', culture '{cultureName}'.");
    }

    private static Regex FreshHistoricalRegex(string pattern) =>
        new(pattern, RegexOptions.Compiled, RuntimeCacheOptions.DefaultRegexTimeout);

    private static string CreateLiteral(Random random, int sample)
    {
        string[] alphabet = ["a", "Z", "0", "-", "_", " ", "\t", "\n", "Ż", "ó", "İ", "ς", "é", "\u0301", "😀"];
        var builder = new StringBuilder();
        builder.Append("literal-");
        builder.Append(sample.ToString("X", CultureInfo.InvariantCulture));
        var count = 1 + random.Next(8);
        for (var index = 0; index < count; index++)
            builder.Append(alphabet[random.Next(alphabet.Length)]);
        return builder.ToString();
    }

    private static void RunUnderCulture(string cultureName, Action action)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = string.IsNullOrEmpty(cultureName)
                ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(cultureName);
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    private static string Display(string value) =>
        value.Replace("\n", "<newline>", StringComparison.Ordinal)
            .Replace("\t", "<tab>", StringComparison.Ordinal);
}
