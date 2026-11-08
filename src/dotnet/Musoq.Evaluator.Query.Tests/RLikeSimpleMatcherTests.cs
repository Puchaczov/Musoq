using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RLikeSimpleMatcherTests
{
    [TestMethod]
    public void CachedSpecializations_ShouldSeparateMatchBudgets()
    {
        const string pattern = @"\Abudget-isolation-[0-9]+\z";
        var first = Operators.GetOrCreateSimpleRLikeMatcher(pattern, TimeSpan.FromSeconds(1));
        var second = Operators.GetOrCreateSimpleRLikeMatcher(pattern, TimeSpan.FromSeconds(3));

        Assert.IsNotNull(first);
        Assert.IsNotNull(second);
        Assert.AreNotSame(first, second);
        Assert.AreEqual(TimeSpan.FromSeconds(1), first.MatchTimeout);
        Assert.AreEqual(TimeSpan.FromSeconds(3), second.MatchTimeout);
        Assert.AreSame(first, Operators.GetOrCreateSimpleRLikeMatcher(pattern, TimeSpan.FromSeconds(1)));

    }

    [TestMethod]
    public void CachedSpecializations_ShouldRemainBounded()
    {
        PopulateCacheWithUniquePatterns();

        Assert.IsTrue(Operators.SimpleRLikeMatcherCacheCount <= Musoq.Evaluator.Runtime.RuntimeCacheOptions.PatternCacheSize);
    }

    [TestMethod]
    public void LinearSpecializations_ShouldAgreeWithRegexForGeneratedInputs()
    {
        string[] patterns = [@".*\.co\.uk$", @".*$", @".*a\$$", @"\Aouter-.+\z",
            @"\Achild-[0-9]{2}\z", @"\A[0-9]{0}\z", @"\A(?:target|alternate)-[0-9]+\z",
            @"\A(?:a|ab|)[0-9]*\z", @"\Aa\.[0-9]+\z", @"\A.*\z"];
        AssertReferenceAgreement(patterns, CreateGeneratedInputs());
    }

    private static void PopulateCacheWithUniquePatterns()
    {
        for (var index = 0; index < 1024; index++)
            Operators.GetOrCreateSimpleRLikeMatcher($@"\Avalue-{index}-[0-9]+\z", TimeSpan.FromSeconds(2));
    }

    private static IReadOnlyList<string> CreateGeneratedInputs()
    {
        var inputs = new List<string> { "", "\n", "a$\n", "example.co.uk", "example.co.uk\n",
            "example.co.uk\n\n", "\nexample.co.uk", "outer-value", "outer-\n", "child-12",
            "child-١٢", "child-１２", "target-12345", "alternate-", "a.12", "ab12" };
        string[] prefixes = ["", "a", "ab", "outer-", "child-", "target-", "alternate-"];
        string[] alphabet = ["0", "9", "x", "\n", "\r", "١", "\0", "\ud800"];
        foreach (var prefix in prefixes)
        foreach (var first in alphabet)
        foreach (var second in alphabet)
            inputs.Add(prefix + first + second);

        return inputs;
    }

    private static void AssertReferenceAgreement(IEnumerable<string> patterns, IReadOnlyList<string> inputs)
    {
        foreach (var pattern in patterns)
        {
            var matcher = Operators.PrepareRLike(pattern)!;
            var reference = new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(2));
            foreach (var input in inputs)
                Assert.AreEqual(reference.IsMatch(input), matcher.IsMatch(input), $"Pattern: {pattern}; input: {input}");
            Assert.IsNull(matcher.ResolvedRegex, $"Expected direct matching for {pattern}");
        }
    }

    [TestMethod]
    [DataRow(@"(?i)\Aa[0-9]+\z")]
    [DataRow(@"\A(a|b)[0-9]+\z")]
    [DataRow(@"\A(?:a\|b|c)[0-9]+\z")]
    [DataRow(@"\Aa.[0-9]+\z")]
    [DataRow(@"\Aa[0-9]{2,3}\z")]
    [DataRow(@"\Aa[0-9]+?\z")]
    [DataRow(@"\Aa\.\z")]
    [DataRow(@".*a+$")]
    [DataRow(@"\A[0-9]{2147483648}\z")]
    public void UnsupportedGrammar_ShouldRemainWithRegex(string pattern)
    {
        Assert.IsNull(RLikeSimpleMatcher.TryCreate(pattern));
    }
}
