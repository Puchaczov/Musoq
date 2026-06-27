using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Tests to verify that all string methods use consistent, culture-independent
///     comparison behavior (OrdinalIgnoreCase for searches, InvariantCulture for casing).
///     These tests specifically validate the fixes for:
///     - NthIndexOf/LastIndexOf now using OrdinalIgnoreCase (was Ordinal)
///     - Replace now using OrdinalIgnoreCase (was default)
///     - RemovePrefix/RemoveSuffix now using OrdinalIgnoreCase (was Ordinal)
///     - ToUpper/ToLower/ToTitleCase now defaulting to InvariantCulture (was CurrentCulture)
///     - Soundex now using ToUpperInvariant (was ToUpper with CurrentCulture)
/// </summary>
[TestClass]
public partial class StringCultureConsistencyTests : PluginsTestBase
{
    #region Contains - Case Insensitivity

    [TestMethod]
    public void Contains_CaseInsensitive_LowercaseInUppercase_ShouldFind()
    {
        Assert.IsTrue(Library.Contains("HELLO WORLD", "hello"));
    }

    [TestMethod]
    public void Contains_CaseInsensitive_UppercaseInLowercase_ShouldFind()
    {
        Assert.IsTrue(Library.Contains("hello world", "WORLD"));
    }

    [TestMethod]
    public void Contains_CaseInsensitive_MixedCase_ShouldFind()
    {
        Assert.IsTrue(Library.Contains("Hello World", "hElLo"));
    }

    [TestMethod]
    public void Contains_NoMatch_ShouldReturnFalse()
    {
        Assert.IsFalse(Library.Contains("Hello World", "xyz"));
    }

    [TestMethod]
    public void Contains_NullContent_ShouldReturnNull()
    {
        Assert.IsNull(Library.Contains(null, "test"));
    }

    [TestMethod]
    public void Contains_NullSearch_ShouldReturnNull()
    {
        Assert.IsNull(Library.Contains("test", null));
    }

    #endregion

    #region IndexOf - Case Insensitivity

    [TestMethod]
    public void IndexOf_CaseInsensitive_UppercaseSearch_ShouldFind()
    {
        Assert.AreEqual(6, Library.IndexOf("hello WORLD", "world"));
    }

    [TestMethod]
    public void IndexOf_CaseInsensitive_LowercaseSearch_ShouldFind()
    {
        Assert.AreEqual(0, Library.IndexOf("HELLO world", "hello"));
    }

    [TestMethod]
    public void IndexOf_CaseInsensitive_MixedCase_ShouldFind()
    {
        Assert.AreEqual(6, Library.IndexOf("hello World", "wOrLd"));
    }

    #endregion

    #region NthIndexOf - Case Insensitivity (Fixed from Ordinal to OrdinalIgnoreCase)

    [TestMethod]
    public void NthIndexOf_CaseInsensitive_FindsUppercaseOccurrence()
    {
        var input = "hello HELLO Hello";
        Assert.AreEqual(0, Library.NthIndexOf(input, "hello", 0));
    }

    [TestMethod]
    public void NthIndexOf_CaseInsensitive_FindsSecondOccurrenceRegardlessOfCase()
    {
        var input = "hello HELLO Hello";
        Assert.AreEqual(6, Library.NthIndexOf(input, "hello", 1));
    }

    [TestMethod]
    public void NthIndexOf_CaseInsensitive_FindsThirdOccurrenceRegardlessOfCase()
    {
        var input = "hello HELLO Hello";
        Assert.AreEqual(12, Library.NthIndexOf(input, "hello", 2));
    }

    [TestMethod]
    public void NthIndexOf_CaseInsensitive_SearchWithUppercase()
    {
        var input = "abc def abc def abc";
        Assert.AreEqual(0, Library.NthIndexOf(input, "ABC", 0));
    }

    [TestMethod]
    public void NthIndexOf_CaseInsensitive_SearchWithUppercase_SecondOccurrence()
    {
        var input = "abc def abc def abc";
        Assert.AreEqual(8, Library.NthIndexOf(input, "ABC", 1));
    }

    [TestMethod]
    public void NthIndexOf_CaseInsensitive_NotFound_ReturnsNull()
    {
        Assert.IsNull(Library.NthIndexOf("hello world", "XYZ", 0));
    }

    [TestMethod]
    public void NthIndexOf_NullInput_ReturnsNull()
    {
        Assert.IsNull(Library.NthIndexOf(null, "test", 0));
    }

    [TestMethod]
    public void NthIndexOf_NullSearch_ReturnsNull()
    {
        Assert.IsNull(Library.NthIndexOf("test", null, 0));
    }

    [TestMethod]
    public void NthIndexOf_NegativeIndex_ReturnsNull()
    {
        Assert.IsNull(Library.NthIndexOf("hello", "hello", -1));
    }

    #endregion

    #region LastIndexOf - Case Insensitivity (Fixed from Ordinal to OrdinalIgnoreCase)

    [TestMethod]
    public void LastIndexOf_CaseInsensitive_FindsLastOccurrenceRegardlessOfCase()
    {
        var result = Library.LastIndexOf("hello HELLO Hello", "hello");
        Assert.AreEqual(12, result);
    }

    [TestMethod]
    public void LastIndexOf_CaseInsensitive_SearchWithUppercase()
    {
        var result = Library.LastIndexOf("abc def abc def ABC", "abc");
        Assert.AreEqual(16, result);
    }

    [TestMethod]
    public void LastIndexOf_CaseInsensitive_SingleOccurrence()
    {
        var result = Library.LastIndexOf("Hello World", "WORLD");
        Assert.AreEqual(6, result);
    }

    [TestMethod]
    public void LastIndexOf_NotFound_ReturnsNull()
    {
        Assert.IsNull(Library.LastIndexOf("hello world", "xyz"));
    }

    [TestMethod]
    public void LastIndexOf_NullInput_ReturnsNull()
    {
        Assert.IsNull(Library.LastIndexOf(null, "test"));
    }

    [TestMethod]
    public void LastIndexOf_NullSearch_ReturnsNull()
    {
        Assert.IsNull(Library.LastIndexOf("test", null));
    }

    [TestMethod]
    public void LastIndexOf_EmptySearch_ReturnsNull()
    {
        Assert.IsNull(Library.LastIndexOf("test", ""));
    }

    #endregion

    #region StartsWith - Case Insensitivity

    [TestMethod]
    public void StartsWith_CaseInsensitive_UppercasePrefix()
    {
        Assert.IsTrue(Library.StartsWith("hello world", "HELLO"));
    }

    [TestMethod]
    public void StartsWith_CaseInsensitive_LowercasePrefix()
    {
        Assert.IsTrue(Library.StartsWith("HELLO WORLD", "hello"));
    }

    [TestMethod]
    public void StartsWith_CaseInsensitive_MixedCase()
    {
        Assert.IsTrue(Library.StartsWith("Hello World", "hElLo"));
    }

    [TestMethod]
    public void StartsWith_NoMatch_ReturnsFalse()
    {
        Assert.IsFalse(Library.StartsWith("hello world", "world"));
    }

    #endregion

    #region EndsWith - Case Insensitivity

    [TestMethod]
    public void EndsWith_CaseInsensitive_UppercaseSuffix()
    {
        Assert.IsTrue(Library.EndsWith("hello world", "WORLD"));
    }

    [TestMethod]
    public void EndsWith_CaseInsensitive_LowercaseSuffix()
    {
        Assert.IsTrue(Library.EndsWith("HELLO WORLD", "world"));
    }

    [TestMethod]
    public void EndsWith_CaseInsensitive_MixedCase()
    {
        Assert.IsTrue(Library.EndsWith("Hello World", "wOrLd"));
    }

    [TestMethod]
    public void EndsWith_NoMatch_ReturnsFalse()
    {
        Assert.IsFalse(Library.EndsWith("hello world", "hello"));
    }

    #endregion

}
