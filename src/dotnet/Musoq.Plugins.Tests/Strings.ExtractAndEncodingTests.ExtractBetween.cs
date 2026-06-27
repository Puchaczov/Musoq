using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class StringsTests
{
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
