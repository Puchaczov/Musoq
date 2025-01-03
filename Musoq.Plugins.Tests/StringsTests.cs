﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class StringsTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void SubstrTest()
    {
        Assert.AreEqual("lorem", Library.Substring("lorem ipsum dolor", 0, 5));
        Assert.AreEqual("lorem ipsum dolor", Library.Substring("lorem ipsum dolor", 0, 150));
        Assert.AreEqual(string.Empty, Library.Substring("lorem ipsum dolor", 0, 0));
        Assert.AreEqual(null, Library.Substring(null, 0, 5));

        Assert.AreEqual(string.Empty, Library.Substring("lorem ipsum dolor", 0));
        Assert.AreEqual("lorem", Library.Substring("lorem ipsum dolor", 5));
        Assert.AreEqual("lorem ipsum dolor", Library.Substring("lorem ipsum dolor", 150));
        Assert.AreEqual(null, Library.Substring(null, 150));
    }

    [TestMethod]
    public void ConcatTest()
    {
        Assert.AreEqual("lorem ipsum dolor", Library.Concat("lorem ", "ipsum ", "dolor"));
        Assert.AreEqual("lorem dolor", Library.Concat("lorem ", null, "dolor"));
        Assert.AreEqual("this is 1", Library.Concat("this ", "is ", 1));
    }

    [TestMethod]
    public void ContainsTest()
    {
        Assert.AreEqual(true, Library.Contains("lorem ipsum dolor", "ipsum"));
        Assert.AreEqual(true, Library.Contains("lorem ipsum dolor", "IPSUM"));
        Assert.AreEqual(false, Library.Contains("lorem ipsum dolor", "ratatata"));
    }

    [TestMethod]
    public void IndexOfTest()
    {
        Assert.AreEqual(6, Library.IndexOf("lorem ipsum dolor", "ipsum"));
        Assert.AreEqual(-1, Library.IndexOf("lorem ipsum dolor", "tatarata"));
    }

    [TestMethod]
    public void SoundexTest()
    {
        Assert.AreEqual("W355", Library.Soundex("Woda Mineralna"));
        Assert.AreEqual("T221", Library.Soundex("This is very long text that have to be soundexed"));
    }

    [TestMethod]
    public void ToHexTest()
    {
        Assert.AreEqual("01,05,07,09,0B,0D,0F,11,19", Library.ToHex([1, 5, 7, 9, 11, 13, 15, 17, 25], ","));
    }

    [TestMethod]
    public void LongestCommonSubstringTest()
    {
        Assert.AreEqual("vwtgw", Library.LongestCommonSubstring("svfvwtgwdsd", "vwtgw"));
        Assert.AreEqual("vwtgw", Library.LongestCommonSubstring("vwtgwdsd", "vwtgw"));
        Assert.AreEqual("vwtgw", Library.LongestCommonSubstring("svfvwtgw", "vwtgw"));
    }

    [TestMethod]
    public void SplitTest()
    {
        var values = Library.Split("test,test2", ",");
        Assert.AreEqual("test", values[0]);
        Assert.AreEqual("test2", values[1]);

        values = Library.Split("test,test2;test3", ",", ";");
        Assert.AreEqual("test", values[0]);
        Assert.AreEqual("test2", values[1]);
        Assert.AreEqual("test3", values[2]);
    }

    [TestMethod]
    public void HasFuzzyMatchedWordTest()
    {
        Assert.IsTrue(Library.HasFuzzyMatchedWord("this is the world first query", "wrd"));
        Assert.IsTrue(Library.HasFuzzyMatchedWord("this is the world first query", "wolrd"));
        Assert.IsTrue(Library.HasFuzzyMatchedWord("this is the world first query", "owlrd"));
        Assert.IsTrue(Library.HasFuzzyMatchedWord("this is the world first query", "worlded"));
    }
    
        [TestMethod]
    public void WhenSplitByLinuxNewLines_InputIsNull_ReturnsNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = Library.SplitByLinuxNewLines(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void WhenSplitByLinuxNewLines_InputIsEmpty_ReturnsEmptyArray()
    {
        // Arrange
        var input = "";

        // Act
        var result = Library.SplitByLinuxNewLines(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void WhenSplitByLinuxNewLines_InputIsWhitespace_ReturnsEmptyArray()
    {
        // Arrange
        var input = "   ";

        // Act
        var result = Library.SplitByLinuxNewLines(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Length);
    }

    [TestMethod]
    public void WhenSplitByLinuxNewLines_InputIsSingleLine_ReturnsSingleElement()
    {
        // Arrange
        var input = "Single line";

        // Act
        var result = Library.SplitByLinuxNewLines(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual("Single line", result[0]);
    }

    [TestMethod]
    public void WhenSplitByLinuxNewLines_InputHasMultipleLines_ReturnsCorrectElements()
    {
        // Arrange
        var input = "Line1\nLine2\nLine3";

        // Act
        var result = Library.SplitByLinuxNewLines(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Length);
        Assert.AreEqual("Line1", result[0]);
        Assert.AreEqual("Line2", result[1]);
        Assert.AreEqual("Line3", result[2]);
    }

    [TestMethod]
    public void WhenSplitByWindowsNewLines_InputIsNull_ReturnsNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = Library.SplitByWindowsNewLines(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void WhenSplitByWindowsNewLines_InputIsEmpty_ReturnsEmptyArray()
    {
        // Arrange
        var input = "";

        // Act
        var result = Library.SplitByWindowsNewLines(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void WhenSplitByWindowsNewLines_InputHasMultipleLines_ReturnsCorrectElements()
    {
        // Arrange
        var input = "Line1\r\nLine2\r\nLine3";

        // Act
        var result = Library.SplitByWindowsNewLines(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Length);
        Assert.AreEqual("Line1", result[0]);
        Assert.AreEqual("Line2", result[1]);
        Assert.AreEqual("Line3", result[2]);
    }

    [TestMethod]
    public void WhenSplitByNewLines_InputIsNull_ReturnsNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = Library.SplitByNewLines(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void WhenSplitByNewLines_InputIsEmpty_ReturnsEmptyArray()
    {
        // Arrange
        var input = "";

        // Act
        var result = Library.SplitByNewLines(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void WhenSplitByNewLines_InputHasMixedNewLines_ReturnsCorrectElements()
    {
        // Arrange
        var input = "Line1\nLine2\r\nLine3\nLine4\r\nLine5";

        // Act
        var result = Library.SplitByNewLines(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(5, result.Length);
        Assert.AreEqual("Line1", result[0]);
        Assert.AreEqual("Line2", result[1]);
        Assert.AreEqual("Line3", result[2]);
        Assert.AreEqual("Line4", result[3]);
        Assert.AreEqual("Line5", result[4]);
    }

    [TestMethod]
    public void WhenSplitByNewLines_InputHasConsecutiveNewLines_PreservesEmptyLines()
    {
        // Arrange
        var input = "Line1\n\nLine3\r\n\r\nLine5";

        // Act
        var result = Library.SplitByNewLines(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(5, result.Length);
        Assert.AreEqual("Line1", result[0]);
        Assert.AreEqual("", result[1]);
        Assert.AreEqual("Line3", result[2]);
        Assert.AreEqual("", result[3]);
        Assert.AreEqual("Line5", result[4]);
    }

    [TestMethod]
    [DataRow("Line1\nLine2", 2)]
    [DataRow("Line1\r\nLine2", 2)]
    [DataRow("Line1\nLine2\r\nLine3", 3)]
    [DataRow("\n\n\n", 4)]
    [DataRow("\r\n\r\n\r\n", 4)]
    public void WhenSplitByNewLines_InputHasVariousFormats_ReturnsCorrectCount(string input, int expectedCount)
    {
        // Act
        var result = Library.SplitByNewLines(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedCount, result.Length);
    }

    [TestMethod]
    public void WhenSplitByNewLines_InputHasOnlyNewlines_ReturnsArrayOfEmptyStrings()
    {
        // Arrange
        var input1 = "\n\n\n";
        var input2 = "\r\n\r\n\r\n";

        // Act
        var result1 = Library.SplitByNewLines(input1);
        var result2 = Library.SplitByNewLines(input2);

        // Assert
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.AreEqual(4, result1.Length);
        Assert.AreEqual(4, result2.Length);
    }
}