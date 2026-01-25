using System;
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
        Assert.IsNull(Library.Substring(null, 0, 5));

        Assert.AreEqual(string.Empty, Library.Substring("lorem ipsum dolor", 0));
        Assert.AreEqual("lorem", Library.Substring("lorem ipsum dolor", 5));
        Assert.AreEqual("lorem ipsum dolor", Library.Substring("lorem ipsum dolor", 150));
        Assert.IsNull(Library.Substring(null, 150));
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
        Assert.IsTrue(Library.Contains("lorem ipsum dolor", "ipsum"));
        Assert.IsTrue(Library.Contains("lorem ipsum dolor", "IPSUM"));
        Assert.IsFalse(Library.Contains("lorem ipsum dolor", "ratatata"));
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
        Assert.IsEmpty(result);
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
        Assert.HasCount(1, result);
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
        Assert.HasCount(1, result);
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
        Assert.HasCount(3, result);
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
        Assert.IsEmpty(result);
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
        Assert.HasCount(3, result);
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
        Assert.IsEmpty(result);
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
        Assert.HasCount(5, result);
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
        Assert.HasCount(5, result);
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
        Assert.HasCount(expectedCount, result);
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
        Assert.HasCount(4, result1);
        Assert.HasCount(4, result2);
    }

    #region Additional String Methods Tests

    [TestMethod]
    public void Trim_ShouldRemoveLeadingAndTrailingWhitespace()
    {
        // Arrange
        var input = "  hello world  ";

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
        var input = "  hello world  ";

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
        var input = "  hello world  ";

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
        Assert.IsTrue(Guid.TryParse(result, out _));
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
        var input = "hello world hello universe hello";
        var search = "hello";

        // Act
        var result = Library.NthIndexOf(input, search, 1); // Second occurrence

        // Assert
        Assert.AreEqual(12, result); // Position of second "hello"
    }

    [TestMethod]
    public void NthIndexOf_WithInvalidIndex_ShouldReturnNull()
    {
        // Arrange
        var input = "hello world";
        var search = "hello";

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
        var search = "hello";

        // Act
        var result = Library.NthIndexOf(input, search, 0);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void LastIndexOf_ShouldReturnCorrectPosition()
    {
        // Arrange
        var input = "hello world hello universe";
        var search = "hello";

        // Act
        var result = Library.LastIndexOf(input, search);

        // Assert
        Assert.AreEqual(12, result); // Position of last "hello"
    }

    [TestMethod]
    public void LastIndexOf_WithNotFound_ShouldReturnNull()
    {
        // Arrange
        var input = "hello world";
        var search = "xyz";

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
        var search = "hello";

        // Act
        var result = Library.LastIndexOf(input, search);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToUpper_ShouldConvertToUppercase()
    {
        // Arrange
        var input = "hello world";

        // Act
        var result = Library.ToUpper(input);

        // Assert
        Assert.AreEqual("HELLO WORLD", result);
    }

    [TestMethod]
    public void ToUpperInvariant_ShouldConvertToUppercase()
    {
        // Arrange
        var input = "hello world";

        // Act
        var result = Library.ToUpperInvariant(input);

        // Assert
        Assert.AreEqual("HELLO WORLD", result);
    }

    [TestMethod]
    public void ToLower_ShouldConvertToLowercase()
    {
        // Arrange
        var input = "HELLO WORLD";

        // Act
        var result = Library.ToLower(input);

        // Assert
        Assert.AreEqual("hello world", result);
    }

    [TestMethod]
    public void ToLowerInvariant_ShouldConvertToLowercase()
    {
        // Arrange
        var input = "HELLO WORLD";

        // Act
        var result = Library.ToLowerInvariant(input);

        // Assert
        Assert.AreEqual("hello world", result);
    }

    [TestMethod]
    public void LevenshteinDistance_ShouldCalculateCorrectDistance()
    {
        // Arrange
        var first = "kitten";
        var second = "sitting";

        // Act
        var result = Library.LevenshteinDistance(first, second);

        // Assert
        Assert.AreEqual(3, result); // Known Levenshtein distance
    }

    [TestMethod]
    public void LevenshteinDistance_WithIdenticalStrings_ShouldReturnZero()
    {
        // Arrange
        var first = "hello";
        var second = "hello";

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
        var second = "hello";

        // Act
        var result = Library.LevenshteinDistance(first, second);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetCharacterOf_ShouldReturnCorrectCharacter()
    {
        // Arrange
        var input = "hello";
        var index = 1;

        // Act
        var result = Library.GetCharacterOf(input, index);

        // Assert
        Assert.AreEqual('e', result);
    }

    [TestMethod]
    public void GetCharacterOf_WithInvalidIndex_ShouldReturnNull()
    {
        // Arrange
        var input = "hello";
        var index = 10;

        // Act
        var result = Library.GetCharacterOf(input, index);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetCharacterOf_WithNegativeIndex_ShouldReturnNull()
    {
        // Arrange
        var input = "hello";
        var index = -1;

        // Act
        var result = Library.GetCharacterOf(input, index);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Reverse_ShouldReturnReversedString()
    {
        // Arrange
        var input = "hello";

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
        var input = "";

        // Act
        var result = Library.Reverse(input);

        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void Reverse_WithSingleCharacter_ShouldReturnSame()
    {
        // Arrange
        var input = "a";

        // Act
        var result = Library.Reverse(input);

        // Assert
        Assert.AreEqual("a", result);
    }

    [TestMethod]
    public void Split_WithCustomSeparators_ShouldSplitCorrectly()
    {
        // Arrange
        var input = "hello,world,test";
        string[] separators = { "," };

        // Act
        var result = Library.Split(input, separators);

        // Assert
        Assert.HasCount(3, result);
        Assert.AreEqual("hello", result[0]);
        Assert.AreEqual("world", result[1]);
        Assert.AreEqual("test", result[2]);
    }

    [TestMethod]
    public void ToCharArray_ShouldReturnCharacterArray()
    {
        // Arrange
        var input = "hello";

        // Act
        var result = Library.ToCharArray(input);

        // Assert
        Assert.HasCount(5, result);
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
        var input = "abc";
        var count = 3;

        // Act
        var result = Library.Replicate(input, count);

        // Assert
        Assert.AreEqual("abcabcabc", result);
    }

    [TestMethod]
    public void Replicate_WithZeroCount_ShouldReturnEmpty()
    {
        // Arrange
        var input = "abc";
        var count = 0;

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
        var regex = @"\d+";
        var content = "There are 123 apples and 456 oranges.";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNotNull(result);
        Assert.HasCount(2, result);
        Assert.AreEqual("123", result[0]);
        Assert.AreEqual("456", result[1]);
    }

    [TestMethod]
    public void RegexMatches_WithNoMatches_ShouldReturnEmptyArray()
    {
        // Arrange
        var regex = @"\d+";
        var content = "No numbers here!";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void RegexMatches_WithNullRegex_ShouldReturnNull()
    {
        // Arrange
        string? regex = null;
        var content = "Some content";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void RegexMatches_WithNullContent_ShouldReturnNull()
    {
        // Arrange
        var regex = @"\d+";
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
        var regex = @"\d+";
        var content = "";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void RegexMatches_WithWordPattern_ShouldReturnWordMatches()
    {
        // Arrange
        var regex = @"\b[A-Z][a-z]+\b";
        var content = "Hello World, This Is A Test.";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNotNull(result);
        Assert.HasCount(5, result);
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
        var regex = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
        var content = "Contact us at john@example.com or support@test.org for help.";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNotNull(result);
        Assert.HasCount(2, result);
        Assert.AreEqual("john@example.com", result[0]);
        Assert.AreEqual("support@test.org", result[1]);
    }

    [TestMethod]
    public void RegexMatches_WithOverlappingPattern_ShouldReturnNonOverlappingMatches()
    {
        // Arrange
        var regex = @"aa";
        var content = "aaaa";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNotNull(result);
        Assert.HasCount(2, result); // Should find "aa" at positions 0 and 2, not overlapping
        Assert.AreEqual("aa", result[0]);
        Assert.AreEqual("aa", result[1]);
    }

    [TestMethod]
    public void RegexMatches_WithGroupCapture_ShouldReturnFullMatch()
    {
        // Arrange
        var regex = @"(\d{3})-(\d{3})-(\d{4})";
        var content = "Call 123-456-7890 or 987-654-3210";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNotNull(result);
        Assert.HasCount(2, result);
        Assert.AreEqual("123-456-7890", result[0]);
        Assert.AreEqual("987-654-3210", result[1]);
    }

    [TestMethod]
    public void RegexMatches_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var regex = @"\$\d+\.\d{2}";
        var content = "Price: $19.99, Sale: $5.50, Tax: $1.25";

        // Act
        var result = Library.RegexMatches(regex, content);

        // Assert
        Assert.IsNotNull(result);
        Assert.HasCount(3, result);
        Assert.AreEqual("$19.99", result[0]);
        Assert.AreEqual("$5.50", result[1]);
        Assert.AreEqual("$1.25", result[2]);
    }

    #endregion

    #endregion

    #region Html Encoding Tests

    [TestMethod]
    public void HtmlEncode_WhenSpecialCharacters_ShouldEncodeCorrectly()
    {
        var result = Library.HtmlEncode("<script>alert('test')</script>");

        Assert.AreEqual("&lt;script&gt;alert(&#39;test&#39;)&lt;/script&gt;", result);
    }

    [TestMethod]
    public void HtmlEncode_WhenNull_ShouldReturnNull()
    {
        var result = Library.HtmlEncode(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void HtmlEncode_WhenAmpersand_ShouldEncode()
    {
        var result = Library.HtmlEncode("Tom & Jerry");

        Assert.AreEqual("Tom &amp; Jerry", result);
    }

    [TestMethod]
    public void HtmlDecode_WhenEncodedCharacters_ShouldDecodeCorrectly()
    {
        var result = Library.HtmlDecode("&lt;script&gt;alert(&#39;test&#39;)&lt;/script&gt;");

        Assert.AreEqual("<script>alert('test')</script>", result);
    }

    [TestMethod]
    public void HtmlDecode_WhenNull_ShouldReturnNull()
    {
        var result = Library.HtmlDecode(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void HtmlDecode_WhenAmpersand_ShouldDecode()
    {
        var result = Library.HtmlDecode("Tom &amp; Jerry");

        Assert.AreEqual("Tom & Jerry", result);
    }

    [TestMethod]
    public void HtmlRoundTrip_ShouldPreserveContent()
    {
        const string original = "<div class=\"test\">Hello & Goodbye</div>";

        var encoded = Library.HtmlEncode(original);
        var decoded = Library.HtmlDecode(encoded);

        Assert.AreEqual(original, decoded);
    }

    #endregion

    #region ExtractBetween Tests

    [TestMethod]
    public void ExtractBetween_WhenDelimitersFound_ShouldReturnContent()
    {
        var result = Library.ExtractBetween("Hello [World] Test", "[", "]");

        Assert.AreEqual("World", result);
    }

    [TestMethod]
    public void ExtractBetween_WithXmlTags_ShouldReturnContent()
    {
        var result = Library.ExtractBetween("<tag>content</tag>", "<tag>", "</tag>");

        Assert.AreEqual("content", result);
    }

    [TestMethod]
    public void ExtractBetween_WhenStartDelimiterNotFound_ShouldReturnNull()
    {
        var result = Library.ExtractBetween("Hello World Test", "[", "]");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractBetween_WhenEndDelimiterNotFound_ShouldReturnNull()
    {
        var result = Library.ExtractBetween("Hello [World Test", "[", "]");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractBetween_WhenNull_ShouldReturnNull()
    {
        Assert.IsNull(Library.ExtractBetween(null, "[", "]"));
        Assert.IsNull(Library.ExtractBetween("test", null, "]"));
        Assert.IsNull(Library.ExtractBetween("test", "[", null));
    }

    [TestMethod]
    public void ExtractBetween_WhenEmpty_ShouldReturnNull()
    {
        Assert.IsNull(Library.ExtractBetween("", "[", "]"));
        Assert.IsNull(Library.ExtractBetween("test", "", "]"));
        Assert.IsNull(Library.ExtractBetween("test", "[", ""));
    }

    [TestMethod]
    public void ExtractBetween_WithMultipleOccurrences_ShouldReturnFirst()
    {
        var result = Library.ExtractBetween("[first] and [second]", "[", "]");

        Assert.AreEqual("first", result);
    }

    [TestMethod]
    public void ExtractBetween_WithEmptyContent_ShouldReturnEmptyString()
    {
        var result = Library.ExtractBetween("Hello [] Test", "[", "]");

        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void ExtractBetweenAll_ShouldReturnAllMatches()
    {
        var result = Library.ExtractBetweenAll("[first] and [second] and [third]", "[", "]");

        Assert.HasCount(3, result);
        Assert.AreEqual("first", result[0]);
        Assert.AreEqual("second", result[1]);
        Assert.AreEqual("third", result[2]);
    }

    [TestMethod]
    public void ExtractBetweenAll_WhenNoMatches_ShouldReturnEmptyArray()
    {
        var result = Library.ExtractBetweenAll("Hello World", "[", "]");

        Assert.HasCount(0, result);
    }

    [TestMethod]
    public void ExtractBetweenAll_WhenNull_ShouldReturnEmptyArray()
    {
        var result = Library.ExtractBetweenAll(null, "[", "]");

        Assert.HasCount(0, result);
    }

    [TestMethod]
    public void ExtractBetweenIncluding_ShouldIncludeDelimiters()
    {
        var result = Library.ExtractBetweenIncluding("Hello [World] Test", "[", "]");

        Assert.AreEqual("[World]", result);
    }

    [TestMethod]
    public void ExtractBetweenIncluding_WithXmlTags_ShouldIncludeTags()
    {
        var result = Library.ExtractBetweenIncluding("prefix<tag>content</tag>suffix", "<tag>", "</tag>");

        Assert.AreEqual("<tag>content</tag>", result);
    }

    [TestMethod]
    public void ExtractBetweenIncluding_WhenNotFound_ShouldReturnNull()
    {
        var result = Library.ExtractBetweenIncluding("Hello World", "[", "]");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractAfter_ShouldReturnTextAfterDelimiter()
    {
        var result = Library.ExtractAfter("Hello World Test", "World");

        Assert.AreEqual(" Test", result);
    }

    [TestMethod]
    public void ExtractAfter_IncludingDelimiter_ShouldIncludeDelimiter()
    {
        var result = Library.ExtractAfter("Hello World Test", "World", true);

        Assert.AreEqual("World Test", result);
    }

    [TestMethod]
    public void ExtractAfter_WhenNotFound_ShouldReturnNull()
    {
        var result = Library.ExtractAfter("Hello World Test", "XYZ");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractAfter_WhenNull_ShouldReturnNull()
    {
        Assert.IsNull(Library.ExtractAfter(null, "test"));
        Assert.IsNull(Library.ExtractAfter("test", null));
    }

    [TestMethod]
    public void ExtractBefore_ShouldReturnTextBeforeDelimiter()
    {
        var result = Library.ExtractBefore("Hello World Test", "World");

        Assert.AreEqual("Hello ", result);
    }

    [TestMethod]
    public void ExtractBefore_IncludingDelimiter_ShouldIncludeDelimiter()
    {
        var result = Library.ExtractBefore("Hello World Test", "World", true);

        Assert.AreEqual("Hello World", result);
    }

    [TestMethod]
    public void ExtractBefore_WhenNotFound_ShouldReturnNull()
    {
        var result = Library.ExtractBefore("Hello World Test", "XYZ");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractBefore_WhenNull_ShouldReturnNull()
    {
        Assert.IsNull(Library.ExtractBefore(null, "test"));
        Assert.IsNull(Library.ExtractBefore("test", null));
    }

    [TestMethod]
    public void ExtractBetween_RealWorldXmlExample()
    {
        var xml = "<?xml version=\"1.0\"?><data><value>12345</value></data>";

        var result = Library.ExtractBetween(xml, "<value>", "</value>");

        Assert.AreEqual("12345", result);
    }

    [TestMethod]
    public void ExtractBetween_RealWorldJsonExample()
    {
        var json = "{\"name\": \"John\", \"age\": 30}";

        var result = Library.ExtractBetween(json, "\"name\": \"", "\"");

        Assert.AreEqual("John", result);
    }

    #endregion

    #region IsNumeric Tests

    [TestMethod]
    public void IsNumeric_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.IsNumeric(null));
    }

    [TestMethod]
    public void IsNumeric_WhenEmpty_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsNumeric(string.Empty));
    }

    [TestMethod]
    public void IsNumeric_WhenAllDigits_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsNumeric("12345"));
    }

    [TestMethod]
    public void IsNumeric_WhenContainsLetters_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsNumeric("123a45"));
    }

    [TestMethod]
    public void IsNumeric_WhenContainsSpecialChars_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsNumeric("123-45"));
    }

    [TestMethod]
    public void IsNumeric_WhenDecimal_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsNumeric("123.45"));
    }

    #endregion

    #region IsAlpha Tests

    [TestMethod]
    public void IsAlpha_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.IsAlpha(null));
    }

    [TestMethod]
    public void IsAlpha_WhenEmpty_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsAlpha(string.Empty));
    }

    [TestMethod]
    public void IsAlpha_WhenAllLetters_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsAlpha("HelloWorld"));
    }

    [TestMethod]
    public void IsAlpha_WhenContainsDigits_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsAlpha("Hello123"));
    }

    [TestMethod]
    public void IsAlpha_WhenContainsSpaces_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsAlpha("Hello World"));
    }

    #endregion

    #region IsAlphaNumeric Tests

    [TestMethod]
    public void IsAlphaNumeric_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.IsAlphaNumeric(null));
    }

    [TestMethod]
    public void IsAlphaNumeric_WhenEmpty_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsAlphaNumeric(string.Empty));
    }

    [TestMethod]
    public void IsAlphaNumeric_WhenAllLetters_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsAlphaNumeric("HelloWorld"));
    }

    [TestMethod]
    public void IsAlphaNumeric_WhenAllDigits_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsAlphaNumeric("12345"));
    }

    [TestMethod]
    public void IsAlphaNumeric_WhenMixed_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsAlphaNumeric("Hello123"));
    }

    [TestMethod]
    public void IsAlphaNumeric_WhenContainsSpaces_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsAlphaNumeric("Hello 123"));
    }

    [TestMethod]
    public void IsAlphaNumeric_WhenContainsSpecialChars_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsAlphaNumeric("Hello@123"));
    }

    #endregion

    #region CountOccurrences Tests

    [TestMethod]
    public void CountOccurrences_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.CountOccurrences(null, "test"));
        Assert.IsNull(Library.CountOccurrences("test", null));
    }

    [TestMethod]
    public void CountOccurrences_WhenSubstringEmpty_ReturnsZero()
    {
        Assert.AreEqual(0, Library.CountOccurrences("Hello World", string.Empty));
    }

    [TestMethod]
    public void CountOccurrences_WhenSingleOccurrence_ReturnsOne()
    {
        Assert.AreEqual(1, Library.CountOccurrences("Hello World", "World"));
    }

    [TestMethod]
    public void CountOccurrences_WhenMultipleOccurrences_ReturnsCount()
    {
        Assert.AreEqual(3, Library.CountOccurrences("ababab", "ab"));
    }

    [TestMethod]
    public void CountOccurrences_WhenNoOccurrences_ReturnsZero()
    {
        Assert.AreEqual(0, Library.CountOccurrences("Hello World", "xyz"));
    }

    [TestMethod]
    public void CountOccurrences_WhenSingleChar_CountsCorrectly()
    {
        Assert.AreEqual(3, Library.CountOccurrences("aaa", "a"));
    }

    #endregion

    #region RemoveWhitespace Tests

    [TestMethod]
    public void RemoveWhitespace_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.RemoveWhitespace(null));
    }

    [TestMethod]
    public void RemoveWhitespace_WhenNoWhitespace_ReturnsSame()
    {
        Assert.AreEqual("HelloWorld", Library.RemoveWhitespace("HelloWorld"));
    }

    [TestMethod]
    public void RemoveWhitespace_WithSpaces_RemovesThem()
    {
        Assert.AreEqual("HelloWorld", Library.RemoveWhitespace("Hello World"));
    }

    [TestMethod]
    public void RemoveWhitespace_WithTabs_RemovesThem()
    {
        Assert.AreEqual("HelloWorld", Library.RemoveWhitespace("Hello\tWorld"));
    }

    [TestMethod]
    public void RemoveWhitespace_WithNewlines_RemovesThem()
    {
        Assert.AreEqual("HelloWorld", Library.RemoveWhitespace("Hello\nWorld"));
    }

    [TestMethod]
    public void RemoveWhitespace_WithMixed_RemovesAll()
    {
        Assert.AreEqual("HelloWorld", Library.RemoveWhitespace("  Hello \t World \n "));
    }

    #endregion

    #region Truncate Tests

    [TestMethod]
    public void Truncate_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.Truncate(null, 10));
    }

    [TestMethod]
    public void Truncate_WhenShorterThanMax_ReturnsSame()
    {
        Assert.AreEqual("Hello", Library.Truncate("Hello", 10));
    }

    [TestMethod]
    public void Truncate_WhenExactLength_ReturnsSame()
    {
        Assert.AreEqual("Hello", Library.Truncate("Hello", 5));
    }

    [TestMethod]
    public void Truncate_WhenLonger_TruncatesWithEllipsis()
    {
        Assert.AreEqual("Hel...", Library.Truncate("Hello World", 6));
    }

    [TestMethod]
    public void Truncate_WhenMaxLengthZero_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.Truncate("Hello", 0));
    }

    [TestMethod]
    public void Truncate_WithCustomEllipsis_UsesIt()
    {
        Assert.AreEqual("Hell…", Library.Truncate("Hello World", 5, "…"));
    }

    [TestMethod]
    public void Truncate_WhenMaxSmallerThanEllipsis_JustTruncates()
    {
        Assert.AreEqual("He", Library.Truncate("Hello", 2));
    }

    #endregion

    #region Capitalize Tests

    [TestMethod]
    public void Capitalize_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.Capitalize(null));
    }

    [TestMethod]
    public void Capitalize_WhenEmpty_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.Capitalize(string.Empty));
    }

    [TestMethod]
    public void Capitalize_WhenSingleChar_Capitalizes()
    {
        Assert.AreEqual("A", Library.Capitalize("a"));
    }

    [TestMethod]
    public void Capitalize_WhenLowercase_CapitalizesFirst()
    {
        Assert.AreEqual("Hello", Library.Capitalize("hello"));
    }

    [TestMethod]
    public void Capitalize_WhenAlreadyCapitalized_ReturnsSame()
    {
        Assert.AreEqual("Hello", Library.Capitalize("Hello"));
    }

    #endregion

    #region Repeat Tests

    [TestMethod]
    public void Repeat_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.Repeat(null, 3));
    }

    [TestMethod]
    public void Repeat_WhenZeroCount_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.Repeat("test", 0));
    }

    [TestMethod]
    public void Repeat_WhenCountOne_ReturnsSame()
    {
        Assert.AreEqual("test", Library.Repeat("test", 1));
    }

    [TestMethod]
    public void Repeat_WithoutSeparator_Concatenates()
    {
        Assert.AreEqual("aaa", Library.Repeat("a", 3));
    }

    [TestMethod]
    public void Repeat_WithSeparator_JoinsThem()
    {
        Assert.AreEqual("a-a-a", Library.Repeat("a", 3, "-"));
    }

    #endregion

    #region Wrap Tests

    [TestMethod]
    public void Wrap_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.Wrap(null, "[", "]"));
    }

    [TestMethod]
    public void Wrap_WithBothPrefixAndSuffix_Wraps()
    {
        Assert.AreEqual("[test]", Library.Wrap("test", "[", "]"));
    }

    [TestMethod]
    public void Wrap_WithNullPrefix_UsesEmpty()
    {
        Assert.AreEqual("test]", Library.Wrap("test", null, "]"));
    }

    [TestMethod]
    public void Wrap_WithNullSuffix_UsesEmpty()
    {
        Assert.AreEqual("[test", Library.Wrap("test", "[", null));
    }

    #endregion

    #region RemovePrefix Tests

    [TestMethod]
    public void RemovePrefix_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.RemovePrefix(null, "test"));
    }

    [TestMethod]
    public void RemovePrefix_WhenPrefixNull_ReturnsSame()
    {
        Assert.AreEqual("test", Library.RemovePrefix("test", null));
    }

    [TestMethod]
    public void RemovePrefix_WhenHasPrefix_RemovesIt()
    {
        Assert.AreEqual("World", Library.RemovePrefix("HelloWorld", "Hello"));
    }

    [TestMethod]
    public void RemovePrefix_WhenNoPrefix_ReturnsSame()
    {
        Assert.AreEqual("HelloWorld", Library.RemovePrefix("HelloWorld", "Test"));
    }

    #endregion

    #region RemoveSuffix Tests

    [TestMethod]
    public void RemoveSuffix_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.RemoveSuffix(null, "test"));
    }

    [TestMethod]
    public void RemoveSuffix_WhenSuffixNull_ReturnsSame()
    {
        Assert.AreEqual("test", Library.RemoveSuffix("test", null));
    }

    [TestMethod]
    public void RemoveSuffix_WhenHasSuffix_RemovesIt()
    {
        Assert.AreEqual("Hello", Library.RemoveSuffix("HelloWorld", "World"));
    }

    [TestMethod]
    public void RemoveSuffix_WhenNoSuffix_ReturnsSame()
    {
        Assert.AreEqual("HelloWorld", Library.RemoveSuffix("HelloWorld", "Test"));
    }

    #endregion

    #region Case Conversion Tests

    [TestMethod]
    public void ToSnakeCase_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.ToSnakeCase(null));
    }

    [TestMethod]
    public void ToSnakeCase_WhenCamelCase_ConvertsCorrectly()
    {
        Assert.AreEqual("hello_world", Library.ToSnakeCase("helloWorld"));
    }

    [TestMethod]
    public void ToSnakeCase_WhenPascalCase_ConvertsCorrectly()
    {
        Assert.AreEqual("hello_world", Library.ToSnakeCase("HelloWorld"));
    }

    [TestMethod]
    public void ToSnakeCase_WhenMultipleUppercase_ConvertsCorrectly()
    {
        Assert.AreEqual("my_api_response", Library.ToSnakeCase("MyAPIResponse"));
    }

    [TestMethod]
    public void ToKebabCase_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.ToKebabCase(null));
    }

    [TestMethod]
    public void ToKebabCase_WhenCamelCase_ConvertsCorrectly()
    {
        Assert.AreEqual("hello-world", Library.ToKebabCase("helloWorld"));
    }

    [TestMethod]
    public void ToKebabCase_WhenPascalCase_ConvertsCorrectly()
    {
        Assert.AreEqual("hello-world", Library.ToKebabCase("HelloWorld"));
    }

    [TestMethod]
    public void ToCamelCase_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.ToCamelCase(null));
    }

    [TestMethod]
    public void ToCamelCase_WhenSnakeCase_ConvertsCorrectly()
    {
        Assert.AreEqual("helloWorld", Library.ToCamelCase("hello_world"));
    }

    [TestMethod]
    public void ToCamelCase_WhenKebabCase_ConvertsCorrectly()
    {
        Assert.AreEqual("helloWorld", Library.ToCamelCase("hello-world"));
    }

    [TestMethod]
    public void ToCamelCase_WhenPascalCase_ConvertsCorrectly()
    {
        Assert.AreEqual("helloWorld", Library.ToCamelCase("HelloWorld"));
    }

    [TestMethod]
    public void ToPascalCase_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.ToPascalCase(null));
    }

    [TestMethod]
    public void ToPascalCase_WhenSnakeCase_ConvertsCorrectly()
    {
        Assert.AreEqual("HelloWorld", Library.ToPascalCase("hello_world"));
    }

    [TestMethod]
    public void ToPascalCase_WhenKebabCase_ConvertsCorrectly()
    {
        Assert.AreEqual("HelloWorld", Library.ToPascalCase("hello-world"));
    }

    [TestMethod]
    public void ToPascalCase_WhenCamelCase_ConvertsCorrectly()
    {
        Assert.AreEqual("HelloWorld", Library.ToPascalCase("helloWorld"));
    }

    #endregion

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
