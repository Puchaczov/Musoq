using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class StringEscapeRiskDetectorTests
{
    [TestMethod]
    public void Find_ReturnsFirstValueChangingEscapeWithSourceSpan()
    {
        var result = StringEscapeRiskDetector.Find("prefix\\n\\tvalue".AsSpan(), 20);

        var risk = result ?? throw new AssertFailedException("Expected an escape risk.");
        Assert.AreEqual("\\n", risk.EscapeText);
        Assert.AreEqual(new TextSpan(26, 2), risk.Span);
        Assert.IsFalse(risk.IsRootedPath);
        Assert.IsTrue(risk.HasNonEscapeContent);
    }

    [TestMethod]
    public void Find_PreservesRootedAndRelativePathFacts()
    {
        var rooted = StringEscapeRiskDetector.Find("C:\\new\\file".AsSpan(), 3);
        var relative = StringEscapeRiskDetector.Find("folder\\new".AsSpan(), 7);

        var rootedRisk = rooted ?? throw new AssertFailedException("Expected a rooted-path escape risk.");
        var relativeRisk = relative ?? throw new AssertFailedException("Expected a relative-path escape risk.");
        Assert.IsTrue(rootedRisk.IsRootedPath);
        Assert.IsFalse(relativeRisk.IsRootedPath);
        Assert.IsTrue(relativeRisk.HasNonEscapeContent);
    }

    [TestMethod]
    public void Find_IgnoresDoubledUnknownStandaloneAndMalformedEscapes()
    {
        Assert.IsFalse(StringEscapeRiskDetector.Find("folder\\\\file".AsSpan(), 0).HasValue);
        Assert.IsFalse(StringEscapeRiskDetector.Find("folder\\q".AsSpan(), 0).HasValue);
        Assert.IsFalse(StringEscapeRiskDetector.Find("folder\\".AsSpan(), 0).HasValue);
        Assert.IsFalse(StringEscapeRiskDetector.Find("value\\u12".AsSpan(), 0).HasValue);
    }
}
