using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class StringsTests
{
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
}
