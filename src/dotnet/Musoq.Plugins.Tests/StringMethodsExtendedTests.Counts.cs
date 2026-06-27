using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for string methods to improve branch coverage.
///     Tests ToSnakeCase, ToKebabCase, ToCamelCase, ToPascalCase,
///     WordCount, LineCount, SentenceCount, and other string utilities.
/// </summary>
public partial class StringMethodsExtendedTests
{

    [TestMethod]
    public void WordCount_Null_ReturnsNull()
    {
        Assert.IsNull(Library.WordCount(null));
    }

    [TestMethod]
    public void WordCount_EmptyString_ReturnsZero()
    {
        Assert.AreEqual(0, Library.WordCount(string.Empty));
    }

    [TestMethod]
    public void WordCount_OnlyWhitespace_ReturnsZero()
    {
        Assert.AreEqual(0, Library.WordCount("   \t\n  "));
    }

    [TestMethod]
    public void WordCount_SingleWord_ReturnsOne()
    {
        Assert.AreEqual(1, Library.WordCount("hello"));
    }

    [TestMethod]
    public void WordCount_TwoWords_ReturnsTwo()
    {
        Assert.AreEqual(2, Library.WordCount("hello world"));
    }

    [TestMethod]
    public void WordCount_MultipleSpaces_CountsCorrectly()
    {
        Assert.AreEqual(2, Library.WordCount("hello    world"));
    }

    [TestMethod]
    public void WordCount_LeadingSpaces_CountsCorrectly()
    {
        Assert.AreEqual(1, Library.WordCount("   hello"));
    }

    [TestMethod]
    public void WordCount_TrailingSpaces_CountsCorrectly()
    {
        Assert.AreEqual(1, Library.WordCount("hello   "));
    }

    [TestMethod]
    public void WordCount_MixedWhitespace_CountsCorrectly()
    {
        Assert.AreEqual(3, Library.WordCount("one\ttwo\nthree"));
    }

    [TestMethod]
    public void WordCount_SingleCharacter_ReturnsOne()
    {
        Assert.AreEqual(1, Library.WordCount("a"));
    }

    [TestMethod]
    public void LineCount_Null_ReturnsNull()
    {
        Assert.IsNull(Library.LineCount(null));
    }

    [TestMethod]
    public void LineCount_EmptyString_ReturnsZero()
    {
        Assert.AreEqual(0, Library.LineCount(string.Empty));
    }

    [TestMethod]
    public void LineCount_SingleLine_ReturnsOne()
    {
        Assert.AreEqual(1, Library.LineCount("hello"));
    }

    [TestMethod]
    public void LineCount_TwoLinesUnix_ReturnsTwo()
    {
        Assert.AreEqual(2, Library.LineCount("line1\nline2"));
    }

    [TestMethod]
    public void LineCount_TwoLinesWindows_ReturnsTwo()
    {
        Assert.AreEqual(2, Library.LineCount("line1\r\nline2"));
    }

    [TestMethod]
    public void LineCount_ThreeLinesMixed_ReturnsThree()
    {
        Assert.AreEqual(3, Library.LineCount("line1\nline2\r\nline3"));
    }

    [TestMethod]
    public void LineCount_TrailingNewline_CountsExtraLine()
    {
        Assert.AreEqual(2, Library.LineCount("line1\n"));
    }

    [TestMethod]
    public void LineCount_OnlyNewlines_CountsCorrectly()
    {
        Assert.AreEqual(3, Library.LineCount("\n\n"));
    }

    [TestMethod]
    public void LineCount_CarriageReturnOnly_CountsAsLine()
    {
        Assert.AreEqual(2, Library.LineCount("line1\rline2"));
    }

    [TestMethod]
    public void LineCount_CarriageReturnAtEnd_CountsAsLine()
    {
        Assert.AreEqual(2, Library.LineCount("line1\r"));
    }

    [TestMethod]
    public void SentenceCount_Null_ReturnsNull()
    {
        Assert.IsNull(Library.SentenceCount(null));
    }

    [TestMethod]
    public void SentenceCount_EmptyString_ReturnsZero()
    {
        Assert.AreEqual(0, Library.SentenceCount(string.Empty));
    }

    [TestMethod]
    public void SentenceCount_OnlyWhitespace_ReturnsZero()
    {
        Assert.AreEqual(0, Library.SentenceCount("   "));
    }

    [TestMethod]
    public void SentenceCount_SingleSentencePeriod_ReturnsOne()
    {
        Assert.AreEqual(1, Library.SentenceCount("Hello world."));
    }

    [TestMethod]
    public void SentenceCount_SingleSentenceExclamation_ReturnsOne()
    {
        Assert.AreEqual(1, Library.SentenceCount("Hello world!"));
    }

    [TestMethod]
    public void SentenceCount_SingleSentenceQuestion_ReturnsOne()
    {
        Assert.AreEqual(1, Library.SentenceCount("Hello world?"));
    }

    [TestMethod]
    public void SentenceCount_TwoSentences_ReturnsTwo()
    {
        Assert.AreEqual(2, Library.SentenceCount("First. Second."));
    }

    [TestMethod]
    public void SentenceCount_MixedDelimiters_CountsCorrectly()
    {
        Assert.AreEqual(3, Library.SentenceCount("Hello! How are you? Good."));
    }

    [TestMethod]
    public void SentenceCount_NoDelimiter_ReturnsOne()
    {
        Assert.AreEqual(1, Library.SentenceCount("Hello world"));
    }

    [TestMethod]
    public void SentenceCount_ConsecutiveDelimiters_CountsOnce()
    {
        Assert.AreEqual(1, Library.SentenceCount("Hello..."));
    }

    [TestMethod]
    public void SentenceCount_DelimiterAtStart_HandlesCorrectly()
    {
        Assert.AreEqual(1, Library.SentenceCount(".Hello"));
    }

}
