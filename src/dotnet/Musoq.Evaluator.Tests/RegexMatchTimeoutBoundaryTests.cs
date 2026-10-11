using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RegexMatchTimeoutBoundaryTests
{
    [TestMethod]
    [DataRow("LIKE")]
    [DataRow("RLIKE")]
    public void Match_WhenRegexTimesOut_ShouldPreserveCauseAndOperator(string patternOperator)
    {
        var regex = new TimeoutRegex();

        var failure = Assert.Throws<RegexExecutionTimeoutException>(() =>
            RegexMatchExecutor.IsMatch(regex, "private input", patternOperator));

        Assert.AreEqual(patternOperator, failure.PatternOperator);
        Assert.AreEqual("Backtracking", failure.Engine);
        Assert.AreSame(regex.Cause, failure.InnerException);
    }

    [TestMethod]
    public void PreparedRLike_WhenRegexTimesOut_ShouldFailRatherThanReturnFalse()
    {
        var matcher = new PreparedRLikeMatcher("v.lue", static (_, _) => new TimeoutRegex());

        Assert.Throws<RegexExecutionTimeoutException>(() => matcher.IsMatch("value"));
    }

    private sealed class TimeoutRegex : Regex
    {
        internal RegexMatchTimeoutException Cause { get; } =
            new("private input", "private pattern", TimeSpan.FromSeconds(1));

        internal TimeoutRegex() : base("private pattern", RegexOptions.None, TimeSpan.FromSeconds(1))
        {
            factory = new TimeoutRunnerFactory(Cause);
        }

        private sealed class TimeoutRunnerFactory(RegexMatchTimeoutException cause) : RegexRunnerFactory
        {
            protected override RegexRunner CreateInstance() => new TimeoutRunner(cause);
        }

        private sealed class TimeoutRunner(RegexMatchTimeoutException cause) : RegexRunner
        {
            protected override void Scan(ReadOnlySpan<char> text) => throw cause;
        }
    }
}
