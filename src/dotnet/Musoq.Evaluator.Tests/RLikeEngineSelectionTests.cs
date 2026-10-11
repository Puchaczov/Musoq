using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RLikeEngineSelectionTests
{
    [TestMethod]
    public void OriginalPathPattern_ShouldUseNonBacktrackingEngine()
    {
        var matcher = Operators.PrepareRLike(@"C:\\logs\\.*\.log")!;

        Assert.IsTrue(matcher.IsMatch(@"C:\logs\app.log"));

        Assert.AreEqual(RegexOptions.NonBacktracking, matcher.ResolvedRegex!.Options);
    }

    [TestMethod]
    [DataRow(@"\A(?=value)value\z", "value")]
    [DataRow(@"\A(v)\1\z", "vv")]
    [DataRow(@"\A(?>value)\z", "value")]
    [DataRow(@"\Gvalue", "value")]
    public void UnsupportedConstruct_ShouldUseBoundedCompiledFallback(string pattern, string input)
    {
        var timeout = TimeSpan.FromMilliseconds(321);
        var regex = Operators.CreateRLikeRegex(pattern, timeout);

        Assert.IsTrue(regex.IsMatch(input));
        Assert.AreEqual(RegexOptions.Compiled, regex.Options);
        Assert.AreEqual(timeout, regex.MatchTimeout);
    }

    [TestMethod]
    [DataRow(@"C:\\logs\\.*\.log", @"C:\logs\app.log")]
    [DataRow(@"C:\\logs\\.*\.log", @"C:\logs\app.txt")]
    [DataRow(@"\A(?:target|alternate)-[0-9]+\z", "target-12345")]
    [DataRow(@"\A(?:target|alternate)-[0-9]+\z", "alternate-x")]
    [DataRow(@"^value$", "value\n")]
    [DataRow(@"(?m)^value$", "before\nvalue\nafter")]
    [DataRow(@"a.*b", "a\nb")]
    [DataRow(@"(?s)a.*b", "a\nb")]
    [DataRow(@"(?i)\Ai\z", "I")]
    [DataRow(@"\A\d\w\s\z", "١Ł\u00a0")]
    [DataRow(@"(?x) \A value \z", "value")]
    [DataRow(@"\A[a-z]+?\z", "value")]
    public void AdaptiveEngine_ShouldPreserveBooleanRegexSemantics(string pattern, string input)
    {
        var timeout = TimeSpan.FromSeconds(2);
        var reference = new Regex(pattern, RegexOptions.Compiled, timeout);
        var actual = Operators.CreateRLikeRegex(pattern, timeout);

        Assert.AreEqual(reference.IsMatch(input), actual.IsMatch(input));
    }

    [TestMethod]
    public void AutomatonLimit_ShouldFallBackWithoutChangingBudget()
    {
        var attempts = new List<RegexOptions>();
        var timeout = TimeSpan.FromMilliseconds(321);

        var regex = Operators.CreateRLikeRegex("v.lue", timeout, (pattern, options, budget) =>
        {
            attempts.Add(options);
            if (options == RegexOptions.NonBacktracking)
                throw new NotSupportedException("Injected automaton limit");
            return new Regex(pattern, options, budget);
        });

        CollectionAssert.AreEqual(new[] { RegexOptions.NonBacktracking, RegexOptions.Compiled }, attempts);
        Assert.AreEqual(timeout, regex.MatchTimeout);
        Assert.IsTrue(regex.IsMatch("value"));
    }

    [TestMethod]
    public void InvalidPattern_ShouldPreserveSyntaxFailureWithoutTryingFallback()
    {
        var attempts = 0;

        Assert.Throws<RegexParseException>(() => Operators.CreateRLikeRegex("[", TimeSpan.FromSeconds(2),
            (pattern, options, timeout) =>
            {
                attempts++;
                return new Regex(pattern, options, timeout);
            }));

        Assert.AreEqual(1, attempts);
    }

    [TestMethod]
    public void PreparingInvalidPattern_ShouldRemainLazyForNullInput()
    {
        var matcher = Operators.PrepareRLike("[")!;

        Assert.IsFalse(Operators.RLikePrepared(null, matcher));
        Assert.IsNull(matcher.ResolvedRegex);
        Assert.Throws<InvalidRuntimeRegexException>(() => matcher.IsMatch("value"));
    }
}
