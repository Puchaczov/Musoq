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
    public void RegexExtract_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.RegexExtract(null, @"\d+"));
    }

    [TestMethod]
    public void RegexExtract_NullPattern_ReturnsNull()
    {
        Assert.IsNull(Library.RegexExtract("test123", null));
    }

    [TestMethod]
    public void RegexExtract_EmptyValue_ReturnsNull()
    {
        Assert.IsNull(Library.RegexExtract(string.Empty, @"\d+"));
    }

    [TestMethod]
    public void RegexExtract_EmptyPattern_ReturnsNull()
    {
        Assert.IsNull(Library.RegexExtract("test123", string.Empty));
    }

    [TestMethod]
    public void RegexExtract_NoMatch_ReturnsNull()
    {
        Assert.IsNull(Library.RegexExtract("hello", @"\d+"));
    }

    [TestMethod]
    public void RegexExtract_MatchGroup0_ReturnsWholeMatch()
    {
        Assert.AreEqual("123", Library.RegexExtract("Hello 123 World", @"\d+"));
    }

    [TestMethod]
    public void RegexExtract_MatchGroup1_ReturnsCaptureGroup()
    {
        Assert.AreEqual("123", Library.RegexExtract("Hello 123 World", @"(\d+)", 1));
    }

    [TestMethod]
    public void RegexExtract_InvalidGroupIndex_ReturnsNull()
    {
        Assert.IsNull(Library.RegexExtract("Hello 123 World", @"(\d+)", 5));
    }

    [TestMethod]
    public void RegexExtract_NegativeGroupIndex_ReturnsNull()
    {
        Assert.IsNull(Library.RegexExtract("Hello 123 World", @"(\d+)", -1));
    }

    [TestMethod]
    public void RegexExtract_InvalidRegex_ReturnsNull()
    {
        Assert.IsNull(Library.RegexExtract("test", @"[invalid"));
    }

    [TestMethod]
    public void RegexExtract_MultipleGroups_ReturnsCorrectGroup()
    {
        Assert.AreEqual("example", Library.RegexExtract("test@example.com", @"(\w+)@(\w+)\.(\w+)", 2));
    }

    [TestMethod]
    public void RegexExtractAll_NullValue_ReturnsEmpty()
    {
        Assert.IsEmpty(Library.RegexExtractAll(null, @"\d+"));
    }

    [TestMethod]
    public void RegexExtractAll_NullPattern_ReturnsEmpty()
    {
        Assert.IsEmpty(Library.RegexExtractAll("test123", null));
    }

    [TestMethod]
    public void RegexExtractAll_EmptyValue_ReturnsEmpty()
    {
        Assert.IsEmpty(Library.RegexExtractAll(string.Empty, @"\d+"));
    }

    [TestMethod]
    public void RegexExtractAll_EmptyPattern_ReturnsEmpty()
    {
        Assert.IsEmpty(Library.RegexExtractAll("test123", string.Empty));
    }

    [TestMethod]
    public void RegexExtractAll_MultipleMatches_ReturnsAll()
    {
        var result = Library.RegexExtractAll("a1b2c3", @"(\d)", 1);
        Assert.HasCount(3, result);
        Assert.AreEqual("1", result[0]);
        Assert.AreEqual("2", result[1]);
        Assert.AreEqual("3", result[2]);
    }

    [TestMethod]
    public void RegexExtractAll_NoMatch_ReturnsEmpty()
    {
        Assert.IsEmpty(Library.RegexExtractAll("hello", @"\d+"));
    }

    [TestMethod]
    public void RegexExtractAll_InvalidGroupIndex_ReturnsEmpty()
    {
        var result = Library.RegexExtractAll("a1b2", @"(\d)", 5);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void RegexExtractAll_InvalidRegex_ReturnsEmpty()
    {
        Assert.IsEmpty(Library.RegexExtractAll("test", @"[invalid"));
    }

    [TestMethod]
    public void IsMatch_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.IsMatch(null, @"\d+"));
    }

    [TestMethod]
    public void IsMatch_NullPattern_ReturnsNull()
    {
        Assert.IsNull(Library.IsMatch("test123", null));
    }

    [TestMethod]
    public void IsMatch_Matches_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsMatch("test123", @"\d+"));
    }

    [TestMethod]
    public void IsMatch_NoMatch_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsMatch("test", @"\d+"));
    }

    [TestMethod]
    public void IsMatch_InvalidRegex_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsMatch("test", @"[invalid"));
    }

}
