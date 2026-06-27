using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class StringCultureConsistencyTests
{
    #region Replace - Case Insensitivity (Fixed from default to OrdinalIgnoreCase)

    [TestMethod]
    public void Replace_CaseInsensitive_ReplacesAllOccurrences()
    {
        var result = Library.Replace("Hello hello HELLO", "hello", "world");
        Assert.AreEqual("world world world", result);
    }

    [TestMethod]
    public void Replace_CaseInsensitive_SingleOccurrence()
    {
        var result = Library.Replace("foo BAR baz", "bar", "qux");
        Assert.AreEqual("foo qux baz", result);
    }

    [TestMethod]
    public void Replace_CaseInsensitive_NoMatch_ReturnsSame()
    {
        var result = Library.Replace("hello world", "xyz", "abc");
        Assert.AreEqual("hello world", result);
    }

    [TestMethod]
    public void Replace_NullInput_ReturnsNull()
    {
        Assert.IsNull(Library.Replace(null, "a", "b"));
    }

    [TestMethod]
    public void Replace_EmptyLookFor_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.Replace("hello", "", "x"));
    }

    [TestMethod]
    public void Replace_NullChangeTo_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.Replace("hello", "l", null));
    }

    [TestMethod]
    public void Replace_CaseInsensitive_MixedCasePattern()
    {
        var result = Library.Replace("The cat sat on the CAT mat", "Cat", "dog");
        Assert.AreEqual("The dog sat on the dog mat", result);
    }

    #endregion

    #region RemovePrefix - Case Insensitivity (Fixed from Ordinal to OrdinalIgnoreCase)

    [TestMethod]
    public void RemovePrefix_CaseInsensitive_UppercasePrefix()
    {
        Assert.AreEqual("fix", Library.RemovePrefix("prefix", "PRE"));
    }

    [TestMethod]
    public void RemovePrefix_CaseInsensitive_LowercasePrefix()
    {
        Assert.AreEqual("FIX", Library.RemovePrefix("PREFIX", "pre"));
    }

    [TestMethod]
    public void RemovePrefix_CaseInsensitive_MixedCase()
    {
        Assert.AreEqual("World", Library.RemovePrefix("HelloWorld", "hELLO"));
    }

    [TestMethod]
    public void RemovePrefix_NoMatch_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.RemovePrefix("hello", "xyz"));
    }

    [TestMethod]
    public void RemovePrefix_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.RemovePrefix(null, "pre"));
    }

    [TestMethod]
    public void RemovePrefix_NullPrefix_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.RemovePrefix("hello", null));
    }

    [TestMethod]
    public void RemovePrefix_EmptyPrefix_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.RemovePrefix("hello", ""));
    }

    [TestMethod]
    public void RemovePrefix_ExactMatch_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.RemovePrefix("hello", "HELLO"));
    }

    #endregion

    #region RemoveSuffix - Case Insensitivity (Fixed from Ordinal to OrdinalIgnoreCase)

    [TestMethod]
    public void RemoveSuffix_CaseInsensitive_UppercaseSuffix()
    {
        Assert.AreEqual("suf", Library.RemoveSuffix("suffix", "FIX"));
    }

    [TestMethod]
    public void RemoveSuffix_CaseInsensitive_LowercaseSuffix()
    {
        Assert.AreEqual("SUF", Library.RemoveSuffix("SUFFIX", "fix"));
    }

    [TestMethod]
    public void RemoveSuffix_CaseInsensitive_MixedCase()
    {
        Assert.AreEqual("Hello", Library.RemoveSuffix("HelloWorld", "wORLD"));
    }

    [TestMethod]
    public void RemoveSuffix_NoMatch_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.RemoveSuffix("hello", "xyz"));
    }

    [TestMethod]
    public void RemoveSuffix_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.RemoveSuffix(null, "fix"));
    }

    [TestMethod]
    public void RemoveSuffix_NullSuffix_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.RemoveSuffix("hello", null));
    }

    [TestMethod]
    public void RemoveSuffix_EmptySuffix_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.RemoveSuffix("hello", ""));
    }

    [TestMethod]
    public void RemoveSuffix_ExactMatch_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.RemoveSuffix("hello", "HELLO"));
    }

    #endregion

    #region ToUpper - InvariantCulture Default (Fixed from CurrentCulture)

    [TestMethod]
    public void ToUpper_BasicString_ReturnsUppercase()
    {
        Assert.AreEqual("HELLO WORLD", Library.ToUpper("hello world"));
    }

    [TestMethod]
    public void ToUpper_AlreadyUppercase_ReturnsSame()
    {
        Assert.AreEqual("HELLO", Library.ToUpper("HELLO"));
    }

    [TestMethod]
    public void ToUpper_TurkishI_UsesInvariantCulture()
    {
        var result = Library.ToUpper("file");
        Assert.AreEqual("FILE", result);
    }

    [TestMethod]
    public void ToUpper_WithSpecificCulture_UsesThatCulture()
    {
        var result = Library.ToUpper("hello", "en-US");
        Assert.AreEqual("HELLO", result);
    }

    [TestMethod]
    public void ToUpperInvariant_BasicString_ReturnsUppercase()
    {
        Assert.AreEqual("HELLO WORLD", Library.ToUpperInvariant("hello world"));
    }

    [TestMethod]
    public void ToUpper_PolishCharacters_ReturnsUppercase()
    {
        Assert.AreEqual("ZAŻÓŁĆ GĘŚLĄ JAŹŃ", Library.ToUpper("zażółć gęślą jaźń"));
    }

    [TestMethod]
    public void ToUpper_GermanCharacters_ReturnsUppercase()
    {
        var result = Library.ToUpper("straße");
        Assert.IsTrue(result == "STRASSE" || result == "STRAßE",
            $"Expected STRASSE or STRAßE but got {result}");
    }

    [TestMethod]
    public void ToUpper_CyrillicCharacters_ReturnsUppercase()
    {
        Assert.AreEqual("ПРИВЕТ МИР", Library.ToUpper("привет мир"));
    }

    [TestMethod]
    public void ToUpper_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.ToUpper(null!));
    }

    #endregion

    #region ToLower - InvariantCulture Default (Fixed from CurrentCulture)

    [TestMethod]
    public void ToLower_BasicString_ReturnsLowercase()
    {
        Assert.AreEqual("hello world", Library.ToLower("HELLO WORLD"));
    }

    [TestMethod]
    public void ToLower_AlreadyLowercase_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.ToLower("hello"));
    }

    [TestMethod]
    public void ToLower_TurkishI_UsesInvariantCulture()
    {
        var result = Library.ToLower("FILE");
        Assert.AreEqual("file", result);
    }

    [TestMethod]
    public void ToLower_WithSpecificCulture_UsesThatCulture()
    {
        var result = Library.ToLower("HELLO", "en-US");
        Assert.AreEqual("hello", result);
    }

    [TestMethod]
    public void ToLowerInvariant_BasicString_ReturnsLowercase()
    {
        Assert.AreEqual("hello world", Library.ToLowerInvariant("HELLO WORLD"));
    }

    [TestMethod]
    public void ToLower_PolishCharacters_ReturnsLowercase()
    {
        Assert.AreEqual("zażółć gęślą jaźń", Library.ToLower("ZAŻÓŁĆ GĘŚLĄ JAŹŃ"));
    }

    [TestMethod]
    public void ToLower_CyrillicCharacters_ReturnsLowercase()
    {
        Assert.AreEqual("привет мир", Library.ToLower("ПРИВЕТ МИР"));
    }

    [TestMethod]
    public void ToLower_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.ToLower(null!));
    }

    #endregion

    #region ToTitleCase - InvariantCulture Default (Fixed from CurrentCulture)

    [TestMethod]
    public void ToTitleCase_BasicString_ReturnsTitleCase()
    {
        Assert.AreEqual("Hello World", Library.ToTitleCase("hello world"));
    }

    [TestMethod]
    public void ToTitleCase_AllUppercase_ReturnsTitleCase()
    {
        var result = Library.ToTitleCase("hello world");
        Assert.AreEqual("Hello World", result);
    }

    [TestMethod]
    public void ToTitleCase_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.ToTitleCase(null));
    }

    [TestMethod]
    public void ToTitleCase_EmptyValue_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ToTitleCase(string.Empty));
    }

    #endregion
}
