using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class WordExtractionTests
{
    private readonly LibraryBase _library = new();

    #region GetNthWord Tests

    [TestMethod]
    public void GetNthWord_WhenNullText_ShouldReturnNull()
    {
        Assert.IsNull(_library.GetNthWord(null, 0, " "));
    }

    [TestMethod]
    public void GetNthWord_WhenNullSeparator_ShouldReturnNull()
    {
        Assert.IsNull(_library.GetNthWord("hello world", 0, null));
    }

    [TestMethod]
    public void GetNthWord_WhenIndexZero_ShouldReturnFirstWord()
    {
        Assert.AreEqual("hello", _library.GetNthWord("hello world", 0, " "));
    }

    [TestMethod]
    public void GetNthWord_WhenIndexOne_ShouldReturnSecondWord()
    {
        Assert.AreEqual("world", _library.GetNthWord("hello world", 1, " "));
    }

    [TestMethod]
    public void GetNthWord_WhenIndexOutOfRange_ShouldReturnNull()
    {
        Assert.IsNull(_library.GetNthWord("hello world", 5, " "));
    }

    [TestMethod]
    public void GetNthWord_WhenSingleWord_ShouldReturnThatWord()
    {
        Assert.AreEqual("hello", _library.GetNthWord("hello", 0, " "));
    }

    #endregion

    #region GetFirstWord Tests

    [TestMethod]
    public void GetFirstWord_WhenTwoWords_ShouldReturnFirst()
    {
        Assert.AreEqual("Alice", _library.GetFirstWord("Alice Johnson", " "));
    }

    [TestMethod]
    public void GetFirstWord_WhenThreeWords_ShouldReturnFirst()
    {
        Assert.AreEqual("John", _library.GetFirstWord("John Paul Jones", " "));
    }

    [TestMethod]
    public void GetFirstWord_WhenSingleWord_ShouldReturnThatWord()
    {
        Assert.AreEqual("Alice", _library.GetFirstWord("Alice", " "));
    }

    [TestMethod]
    public void GetFirstWord_WhenCommaSeparated_ShouldReturnFirst()
    {
        Assert.AreEqual("one", _library.GetFirstWord("one,two,three", ","));
    }

    #endregion

    #region GetSecondWord Tests

    [TestMethod]
    public void GetSecondWord_WhenTwoWords_ShouldReturnSecond()
    {
        Assert.AreEqual("Johnson", _library.GetSecondWord("Alice Johnson", " "));
    }

    [TestMethod]
    public void GetSecondWord_WhenThreeWords_ShouldReturnSecond()
    {
        Assert.AreEqual("Paul", _library.GetSecondWord("John Paul Jones", " "));
    }

    [TestMethod]
    public void GetSecondWord_WhenSingleWord_ShouldReturnNull()
    {
        Assert.IsNull(_library.GetSecondWord("Alice", " "));
    }

    #endregion

    #region GetThirdWord Tests

    [TestMethod]
    public void GetThirdWord_WhenThreeWords_ShouldReturnThird()
    {
        Assert.AreEqual("Jones", _library.GetThirdWord("John Paul Jones", " "));
    }

    [TestMethod]
    public void GetThirdWord_WhenTwoWords_ShouldReturnNull()
    {
        Assert.IsNull(_library.GetThirdWord("Alice Johnson", " "));
    }

    #endregion

    #region GetLastWord Tests

    [TestMethod]
    public void GetLastWord_WhenNullText_ShouldReturnNull()
    {
        Assert.IsNull(_library.GetLastWord(null, " "));
    }

    [TestMethod]
    public void GetLastWord_WhenNullSeparator_ShouldReturnNull()
    {
        Assert.IsNull(_library.GetLastWord("hello world", null));
    }

    [TestMethod]
    public void GetLastWord_WhenTwoWords_ShouldReturnLast()
    {
        Assert.AreEqual("Johnson", _library.GetLastWord("Alice Johnson", " "));
    }

    [TestMethod]
    public void GetLastWord_WhenThreeWords_ShouldReturnLast()
    {
        Assert.AreEqual("Jones", _library.GetLastWord("John Paul Jones", " "));
    }

    [TestMethod]
    public void GetLastWord_WhenSingleWord_ShouldReturnThatWord()
    {
        Assert.AreEqual("Alice", _library.GetLastWord("Alice", " "));
    }

    #endregion

    #region GetFirstWord and GetLastWord Combined

    [TestMethod]
    public void GetFirstWordAndLastWord_WhenTwoWords_ShouldReturnDifferentWords()
    {
        var text = "Alice Johnson";
        var first = _library.GetFirstWord(text, " ");
        var last = _library.GetLastWord(text, " ");

        Assert.AreEqual("Alice", first);
        Assert.AreEqual("Johnson", last);
        Assert.AreNotEqual(first, last);
    }

    [TestMethod]
    public void GetFirstWordAndLastWord_WhenSingleWord_ShouldReturnSameWord()
    {
        var text = "Alice";
        var first = _library.GetFirstWord(text, " ");
        var last = _library.GetLastWord(text, " ");

        Assert.AreEqual("Alice", first);
        Assert.AreEqual("Alice", last);
    }

    #endregion
}
