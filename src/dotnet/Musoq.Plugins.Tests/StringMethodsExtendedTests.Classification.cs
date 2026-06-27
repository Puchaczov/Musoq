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
    public void IsNumeric_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsNumeric(null));
    }

    [TestMethod]
    public void IsNumeric_EmptyString_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsNumeric(string.Empty));
    }

    [TestMethod]
    public void IsNumeric_AllDigits_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsNumeric("12345"));
    }

    [TestMethod]
    public void IsNumeric_ContainsLetter_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsNumeric("123a5"));
    }

    [TestMethod]
    public void IsNumeric_ContainsSpace_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsNumeric("123 45"));
    }

    [TestMethod]
    public void IsNumeric_SingleDigit_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsNumeric("5"));
    }

    [TestMethod]
    public void IsAlpha_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsAlpha(null));
    }

    [TestMethod]
    public void IsAlpha_EmptyString_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsAlpha(string.Empty));
    }

    [TestMethod]
    public void IsAlpha_AllLetters_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsAlpha("Hello"));
    }

    [TestMethod]
    public void IsAlpha_ContainsDigit_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsAlpha("Hello1"));
    }

    [TestMethod]
    public void IsAlpha_ContainsSpace_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsAlpha("Hello World"));
    }

    [TestMethod]
    public void IsAlpha_SingleLetter_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsAlpha("A"));
    }

    [TestMethod]
    public void IsAlphaNumeric_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsAlphaNumeric(null));
    }

    [TestMethod]
    public void IsAlphaNumeric_EmptyString_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsAlphaNumeric(string.Empty));
    }

    [TestMethod]
    public void IsAlphaNumeric_OnlyLetters_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsAlphaNumeric("Hello"));
    }

    [TestMethod]
    public void IsAlphaNumeric_OnlyDigits_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsAlphaNumeric("12345"));
    }

    [TestMethod]
    public void IsAlphaNumeric_Mixed_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsAlphaNumeric("Hello123"));
    }

    [TestMethod]
    public void IsAlphaNumeric_ContainsSpace_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsAlphaNumeric("Hello 123"));
    }

    [TestMethod]
    public void IsAlphaNumeric_ContainsSymbol_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsAlphaNumeric("Hello@123"));
    }

    [TestMethod]
    public void CountOccurrences_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.CountOccurrences(null, "a"));
    }

    [TestMethod]
    public void CountOccurrences_NullSubstring_ReturnsNull()
    {
        Assert.IsNull(Library.CountOccurrences("hello", null));
    }

    [TestMethod]
    public void CountOccurrences_EmptySubstring_ReturnsZero()
    {
        Assert.AreEqual(0, Library.CountOccurrences("hello", string.Empty));
    }

    [TestMethod]
    public void CountOccurrences_NoOccurrences_ReturnsZero()
    {
        Assert.AreEqual(0, Library.CountOccurrences("hello", "x"));
    }

    [TestMethod]
    public void CountOccurrences_SingleOccurrence_ReturnsOne()
    {
        Assert.AreEqual(1, Library.CountOccurrences("hello", "h"));
    }

    [TestMethod]
    public void CountOccurrences_MultipleOccurrences_ReturnsCount()
    {
        Assert.AreEqual(2, Library.CountOccurrences("hello", "l"));
    }

    [TestMethod]
    public void CountOccurrences_OverlappingNotCounted_ReturnsNonOverlapping()
    {
        Assert.AreEqual(1, Library.CountOccurrences("aaa", "aa"));
    }

    [TestMethod]
    public void CountOccurrences_MultiCharSubstring_ReturnsCount()
    {
        Assert.AreEqual(2, Library.CountOccurrences("abcabc", "abc"));
    }

    [TestMethod]
    public void RemoveWhitespace_Null_ReturnsNull()
    {
        Assert.IsNull(Library.RemoveWhitespace(null));
    }

    [TestMethod]
    public void RemoveWhitespace_NoWhitespace_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.RemoveWhitespace("hello"));
    }

    [TestMethod]
    public void RemoveWhitespace_WithSpaces_RemovesSpaces()
    {
        Assert.AreEqual("helloworld", Library.RemoveWhitespace("hello world"));
    }

    [TestMethod]
    public void RemoveWhitespace_WithTabs_RemovesTabs()
    {
        Assert.AreEqual("helloworld", Library.RemoveWhitespace("hello\tworld"));
    }

    [TestMethod]
    public void RemoveWhitespace_WithNewlines_RemovesNewlines()
    {
        Assert.AreEqual("helloworld", Library.RemoveWhitespace("hello\nworld"));
    }

    [TestMethod]
    public void RemoveWhitespace_AllWhitespace_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.RemoveWhitespace("   \t\n  "));
    }

}
