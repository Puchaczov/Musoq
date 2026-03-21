using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class StringsTests
{
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
}
