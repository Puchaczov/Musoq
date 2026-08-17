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

        Assert.IsTrue(result.HasValue);
        Assert.AreEqual("\\n", result.Value.EscapeText);
        Assert.AreEqual(new TextSpan(26, 2), result.Value.Span);
        Assert.IsFalse(result.Value.IsRootedPath);
        Assert.IsTrue(result.Value.HasNonEscapeContent);
    }

    [TestMethod]
    public void Find_PreservesRootedAndRelativePathFacts()
    {
        var rooted = StringEscapeRiskDetector.Find("C:\\new\\file".AsSpan(), 3);
        var relative = StringEscapeRiskDetector.Find("folder\\new".AsSpan(), 7);

        Assert.IsTrue(rooted.HasValue);
        Assert.IsTrue(rooted.Value.IsRootedPath);
        Assert.IsTrue(relative.HasValue);
        Assert.IsFalse(relative.Value.IsRootedPath);
        Assert.IsTrue(relative.Value.HasNonEscapeContent);
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
