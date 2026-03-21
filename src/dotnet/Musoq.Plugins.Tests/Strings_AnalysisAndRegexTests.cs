using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class StringsTests
{
    #region Text Analysis Tests

    [TestMethod]
    public void WordCount_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.WordCount(null));
    }

    [TestMethod]
    public void WordCount_WhenEmpty_ReturnsZero()
    {
        Assert.AreEqual(0, Library.WordCount(""));
    }

    [TestMethod]
    public void WordCount_WhenSingleWord_ReturnsOne()
    {
        Assert.AreEqual(1, Library.WordCount("Hello"));
    }

    [TestMethod]
    public void WordCount_WhenMultipleWords_ReturnsCorrectCount()
    {
        Assert.AreEqual(5, Library.WordCount("Hello world this is test"));
    }

    [TestMethod]
    public void WordCount_WhenMultipleSpaces_ReturnsCorrectCount()
    {
        Assert.AreEqual(3, Library.WordCount("Hello   world   test"));
    }

    [TestMethod]
    public void LineCount_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.LineCount(null));
    }

    [TestMethod]
    public void LineCount_WhenEmpty_ReturnsZero()
    {
        Assert.AreEqual(0, Library.LineCount(""));
    }

    [TestMethod]
    public void LineCount_WhenSingleLine_ReturnsOne()
    {
        Assert.AreEqual(1, Library.LineCount("Hello World"));
    }

    [TestMethod]
    public void LineCount_WhenMultipleLines_ReturnsCorrectCount()
    {
        Assert.AreEqual(3, Library.LineCount("Line 1\nLine 2\nLine 3"));
    }

    [TestMethod]
    public void LineCount_WhenWindowsLineEndings_ReturnsCorrectCount()
    {
        Assert.AreEqual(3, Library.LineCount("Line 1\r\nLine 2\r\nLine 3"));
    }

    [TestMethod]
    public void SentenceCount_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.SentenceCount(null));
    }

    [TestMethod]
    public void SentenceCount_WhenEmpty_ReturnsZero()
    {
        Assert.AreEqual(0, Library.SentenceCount(""));
    }

    [TestMethod]
    public void SentenceCount_WhenSingleSentence_ReturnsOne()
    {
        Assert.AreEqual(1, Library.SentenceCount("Hello World."));
    }

    [TestMethod]
    public void SentenceCount_WhenMultipleSentences_ReturnsCorrectCount()
    {
        Assert.AreEqual(3, Library.SentenceCount("Hello. How are you? I am fine!"));
    }

    #endregion

    #region Regex Tests

    [TestMethod]
    public void RegexExtract_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.RegexExtract(null, @"\d+"));
    }

    [TestMethod]
    public void RegexExtract_WhenPatternNull_ReturnsNull()
    {
        Assert.IsNull(Library.RegexExtract("test123", null));
    }

    [TestMethod]
    public void RegexExtract_WhenMatch_ReturnsFirstMatch()
    {
        Assert.AreEqual("123", Library.RegexExtract("abc123def456", @"\d+"));
    }

    [TestMethod]
    public void RegexExtract_WhenNoMatch_ReturnsNull()
    {
        Assert.IsNull(Library.RegexExtract("abcdef", @"\d+"));
    }

    [TestMethod]
    public void RegexExtractAll_WhenNull_ReturnsEmpty()
    {
        var result = Library.RegexExtractAll(null, @"\d+");
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void RegexExtractAll_WhenPatternNull_ReturnsEmpty()
    {
        var result = Library.RegexExtractAll("test123", null);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void RegexExtractAll_WhenMatches_ReturnsAllMatches()
    {
        var result = Library.RegexExtractAll("abc123def456ghi789", @"\d+");
        CollectionAssert.AreEqual(new[] { "123", "456", "789" }, result);
    }

    [TestMethod]
    public void RegexExtractAll_WhenNoMatch_ReturnsEmpty()
    {
        var result = Library.RegexExtractAll("abcdef", @"\d+");
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void RegexExtractAll_WithGroupIndex_ReturnsCorrectGroup()
    {
        var result = Library.RegexExtractAll("a1b2c3", @"(\d)", 1);
        CollectionAssert.AreEqual(new[] { "1", "2", "3" }, result);
    }

    [TestMethod]
    public void IsMatch_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.IsMatch(null, @"\d+"));
    }

    [TestMethod]
    public void IsMatch_WhenPatternNull_ReturnsNull()
    {
        Assert.IsNull(Library.IsMatch("test123", null));
    }

    [TestMethod]
    public void IsMatch_WhenMatches_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsMatch("abc123", @"\d+"));
    }

    [TestMethod]
    public void IsMatch_WhenNoMatch_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsMatch("abcdef", @"\d+"));
    }

    [TestMethod]
    public void IsMatch_WithComplexPattern_MatchesCorrectly()
    {
        Assert.IsTrue(Library.IsMatch("test@example.com", @"^[\w\.-]+@[\w\.-]+\.\w+$"));
    }

    #endregion
}
