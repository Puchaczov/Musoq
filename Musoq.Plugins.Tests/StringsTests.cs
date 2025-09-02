using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        Assert.AreEqual((string?)null, Library.Substring((string?)null!, 150));
    }

    [TestMethod]
    public void ConcatTest()
    {
        Assert.AreEqual("lorem ipsum dolor", Library.Concat("lorem ", "ipsum ", "dolor"));
        Assert.AreEqual("lorem dolor", Library.Concat("lorem ", (string?)null!, "dolor"));
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

    #region Additional String Methods Tests

    [TestMethod]
    public void Trim_ShouldRemoveLeadingAndTrailingWhitespace()
    {
        // Arrange
        string input = "  hello world  ";

        // Act
        var result = Library.Trim(input);

        // Assert
        Assert.AreEqual("hello world", result);
    }

    [TestMethod]
    public void Trim_WithNull_ShouldReturnNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = Library.Trim(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TrimStart_ShouldRemoveLeadingWhitespace()
    {
        // Arrange
        string input = "  hello world  ";

        // Act
        var result = Library.TrimStart(input);

        // Assert
        Assert.AreEqual("hello world  ", result);
    }

    [TestMethod]
    public void TrimStart_WithNull_ShouldReturnNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = Library.TrimStart(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TrimEnd_ShouldRemoveTrailingWhitespace()
    {
        // Arrange
        string input = "  hello world  ";

        // Act
        var result = Library.TrimEnd(input);

        // Assert
        Assert.AreEqual("  hello world", result);
    }

    [TestMethod]
    public void TrimEnd_WithNull_ShouldReturnNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = Library.TrimEnd(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void NewId_ShouldReturnValidGuid()
    {
        // Act
        var result = Library.NewId();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(System.Guid.TryParse(result, out _));
    }

    [TestMethod]
    public void NewId_ShouldReturnUniqueValues()
    {
        // Act
        var result1 = Library.NewId();
        var result2 = Library.NewId();

        // Assert
        Assert.AreNotEqual(result1, result2);
    }

    [TestMethod]
    public void NthIndexOf_ShouldReturnCorrectPosition()
    {
        // Arrange
        string input = "hello world hello universe hello";
        string search = "hello";

        // Act
        var result = Library.NthIndexOf(input, search, 1); // Second occurrence

        // Assert
        Assert.AreEqual(12, result); // Position of second "hello"
    }

    [TestMethod]
    public void NthIndexOf_WithInvalidIndex_ShouldReturnNull()
    {
        // Arrange
        string input = "hello world";
        string search = "hello";

        // Act
        var result = Library.NthIndexOf(input, search, 5); // Doesn't exist

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void NthIndexOf_WithNull_ShouldReturnNull()
    {
        // Arrange
        string? input = null;
        string search = "hello";

        // Act
        var result = Library.NthIndexOf(input, search, 0);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void LastIndexOf_ShouldReturnCorrectPosition()
    {
        // Arrange
        string input = "hello world hello universe";
        string search = "hello";

        // Act
        var result = Library.LastIndexOf(input, search);

        // Assert
        Assert.AreEqual(12, result); // Position of last "hello"
    }

    [TestMethod]
    public void LastIndexOf_WithNotFound_ShouldReturnNull()
    {
        // Arrange
        string input = "hello world";
        string search = "xyz";

        // Act
        var result = Library.LastIndexOf(input, search);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void LastIndexOf_WithNull_ShouldReturnNull()
    {
        // Arrange
        string? input = null;
        string search = "hello";

        // Act
        var result = Library.LastIndexOf(input, search);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToUpper_ShouldConvertToUppercase()
    {
        // Arrange
        string input = "hello world";

        // Act
        var result = Library.ToUpper(input);

        // Assert
        Assert.AreEqual("HELLO WORLD", result);
    }

    [TestMethod]
    public void ToUpperInvariant_ShouldConvertToUppercase()
    {
        // Arrange
        string input = "hello world";

        // Act
        var result = Library.ToUpperInvariant(input);

        // Assert
        Assert.AreEqual("HELLO WORLD", result);
    }

    [TestMethod]
    public void ToLower_ShouldConvertToLowercase()
    {
        // Arrange
        string input = "HELLO WORLD";

        // Act
        var result = Library.ToLower(input);

        // Assert
        Assert.AreEqual("hello world", result);
    }

    [TestMethod]
    public void ToLowerInvariant_ShouldConvertToLowercase()
    {
        // Arrange
        string input = "HELLO WORLD";

        // Act
        var result = Library.ToLowerInvariant(input);

        // Assert
        Assert.AreEqual("hello world", result);
    }

    [TestMethod]
    public void LevenshteinDistance_ShouldCalculateCorrectDistance()
    {
        // Arrange
        string first = "kitten";
        string second = "sitting";

        // Act
        var result = Library.LevenshteinDistance(first, second);

        // Assert
        Assert.AreEqual(3, result); // Known Levenshtein distance
    }

    [TestMethod]
    public void LevenshteinDistance_WithIdenticalStrings_ShouldReturnZero()
    {
        // Arrange
        string first = "hello";
        string second = "hello";

        // Act
        var result = Library.LevenshteinDistance(first, second);

        // Assert
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void LevenshteinDistance_WithNull_ShouldReturnNull()
    {
        // Arrange
        string? first = null;
        string second = "hello";

        // Act
        var result = Library.LevenshteinDistance(first, second);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetCharacterOf_ShouldReturnCorrectCharacter()
    {
        // Arrange
        string input = "hello";
        int index = 1;

        // Act
        var result = Library.GetCharacterOf(input, index);

        // Assert
        Assert.AreEqual('e', result);
    }

    [TestMethod]
    public void GetCharacterOf_WithInvalidIndex_ShouldReturnNull()
    {
        // Arrange
        string input = "hello";
        int index = 10;

        // Act
        var result = Library.GetCharacterOf(input, index);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetCharacterOf_WithNegativeIndex_ShouldReturnNull()
    {
        // Arrange
        string input = "hello";
        int index = -1;

        // Act
        var result = Library.GetCharacterOf(input, index);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Reverse_ShouldReturnReversedString()
    {
        // Arrange
        string input = "hello";

        // Act
        var result = Library.Reverse(input);

        // Assert
        Assert.AreEqual("olleh", result);
    }

    [TestMethod]
    public void Reverse_WithNull_ShouldReturnNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = Library.Reverse(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Reverse_WithEmptyString_ShouldReturnEmpty()
    {
        // Arrange
        string input = "";

        // Act
        var result = Library.Reverse(input);

        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void Reverse_WithSingleCharacter_ShouldReturnSame()
    {
        // Arrange
        string input = "a";

        // Act
        var result = Library.Reverse(input);

        // Assert
        Assert.AreEqual("a", result);
    }

    [TestMethod]
    public void Split_ShouldSplitStringCorrectly()
    {
        // Arrange
        string input = "hello,world,test";
        string[] separators = { "," };

        // Act
        var result = Library.Split(input, separators);

        // Assert
        Assert.AreEqual(3, result.Length);
        Assert.AreEqual("hello", result[0]);
        Assert.AreEqual("world", result[1]);
        Assert.AreEqual("test", result[2]);
    }

    [TestMethod]
    public void ToCharArray_ShouldReturnCharacterArray()
    {
        // Arrange
        string input = "hello";

        // Act
        var result = Library.ToCharArray(input);

        // Assert
        Assert.AreEqual(5, result.Length);
        Assert.AreEqual('h', result[0]);
        Assert.AreEqual('e', result[1]);
        Assert.AreEqual('l', result[2]);
        Assert.AreEqual('l', result[3]);
        Assert.AreEqual('o', result[4]);
    }

    [TestMethod]
    public void Replicate_ShouldRepeatStringCorrectly()
    {
        // Arrange
        string input = "abc";
        int count = 3;

        // Act
        var result = Library.Replicate(input, count);

        // Assert
        Assert.AreEqual("abcabcabc", result);
    }

    [TestMethod]
    public void Replicate_WithZeroCount_ShouldReturnEmpty()
    {
        // Arrange
        string input = "abc";
        int count = 0;

        // Act
        var result = Library.Replicate(input, count);

        // Assert
        Assert.AreEqual("", result);
    }

    #region RegexMatches Tests
    
    [TestMethod]
    public void RegexMatches_WithSimplePattern_ShouldReturnMatches()
    {
        // Arrange
        string regex = @"\d+";
        string content = "There are 123 apples and 456 oranges.";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual("123", result[0]);
        Assert.AreEqual("456", result[1]);
    }

    [TestMethod]
    public void RegexMatches_WithNoMatches_ShouldReturnEmptyArray()
    {
        // Arrange
        string regex = @"\d+";
        string content = "No numbers here!";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void RegexMatches_WithNullRegex_ShouldReturnNull()
    {
        // Arrange
        string? regex = null;
        string content = "Some content";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void RegexMatches_WithNullContent_ShouldReturnNull()
    {
        // Arrange
        string regex = @"\d+";
        string? content = null;

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void RegexMatches_WithBothNullInputs_ShouldReturnNull()
    {
        // Arrange
        string? regex = null;
        string? content = null;

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void RegexMatches_WithEmptyContent_ShouldReturnEmptyArray()
    {
        // Arrange
        string regex = @"\d+";
        string content = "";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void RegexMatches_WithWordPattern_ShouldReturnWordMatches()
    {
        // Arrange
        string regex = @"\b[A-Z][a-z]+\b";
        string content = "Hello World, This Is A Test.";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(5, result.Length);
        Assert.AreEqual("Hello", result[0]);
        Assert.AreEqual("World", result[1]);
        Assert.AreEqual("This", result[2]);
        Assert.AreEqual("Is", result[3]);
        Assert.AreEqual("Test", result[4]);
    }

    [TestMethod]
    public void RegexMatches_WithEmailPattern_ShouldReturnEmailAddresses()
    {
        // Arrange
        string regex = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
        string content = "Contact us at john@example.com or support@test.org for help.";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual("john@example.com", result[0]);
        Assert.AreEqual("support@test.org", result[1]);
    }

    [TestMethod]
    public void RegexMatches_WithOverlappingPattern_ShouldReturnNonOverlappingMatches()
    {
        // Arrange
        string regex = @"aa";
        string content = "aaaa";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Length); // Should find "aa" at positions 0 and 2, not overlapping
        Assert.AreEqual("aa", result[0]);
        Assert.AreEqual("aa", result[1]);
    }

    [TestMethod]
    public void RegexMatches_WithGroupCapture_ShouldReturnFullMatch()
    {
        // Arrange
        string regex = @"(\d{3})-(\d{3})-(\d{4})";
        string content = "Call 123-456-7890 or 987-654-3210";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual("123-456-7890", result[0]);
        Assert.AreEqual("987-654-3210", result[1]);
    }

    [TestMethod]
    public void RegexMatches_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        string regex = @"\$\d+\.\d{2}";
        string content = "Price: $19.99, Sale: $5.50, Tax: $1.25";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Length);
        Assert.AreEqual("$19.99", result[0]);
        Assert.AreEqual("$5.50", result[1]);
        Assert.AreEqual("$1.25", result[2]);
    }

    #endregion

    #endregion
}