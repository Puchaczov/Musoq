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
    public void Truncate_Null_ReturnsNull()
    {
        Assert.IsNull(Library.Truncate(null, 10));
    }

    [TestMethod]
    public void Truncate_ShorterThanMax_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.Truncate("hello", 10));
    }

    [TestMethod]
    public void Truncate_ExactLength_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.Truncate("hello", 5));
    }

    [TestMethod]
    public void Truncate_LongerThanMax_TruncatesWithEllipsis()
    {
        Assert.AreEqual("hel...", Library.Truncate("hello world", 6));
    }

    [TestMethod]
    public void Truncate_MaxLengthZero_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.Truncate("hello", 0));
    }

    [TestMethod]
    public void Truncate_NegativeMaxLength_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.Truncate("hello", -1));
    }

    [TestMethod]
    public void Truncate_MaxLengthLessThanEllipsis_NoEllipsis()
    {
        Assert.AreEqual("he", Library.Truncate("hello", 2));
    }

    [TestMethod]
    public void Truncate_MaxLengthEqualToEllipsis_NoEllipsis()
    {
        Assert.AreEqual("hel", Library.Truncate("hello", 3));
    }

    [TestMethod]
    public void Truncate_CustomEllipsis_UsesCustom()
    {
        Assert.AreEqual("hel..", Library.Truncate("hello world", 5, ".."));
    }

    [TestMethod]
    public void Truncate_EmptyEllipsis_TruncatesWithoutEllipsis()
    {
        Assert.AreEqual("hello", Library.Truncate("hello world", 5, ""));
    }

    [TestMethod]
    public void Capitalize_Null_ReturnsNull()
    {
        Assert.IsNull(Library.Capitalize(null));
    }

    [TestMethod]
    public void Capitalize_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.Capitalize(string.Empty));
    }

    [TestMethod]
    public void Capitalize_SingleLowerLetter_ReturnsUpper()
    {
        Assert.AreEqual("A", Library.Capitalize("a"));
    }

    [TestMethod]
    public void Capitalize_SingleUpperLetter_ReturnsSame()
    {
        Assert.AreEqual("A", Library.Capitalize("A"));
    }

    [TestMethod]
    public void Capitalize_LowerCase_CapitalizesFirst()
    {
        Assert.AreEqual("Hello", Library.Capitalize("hello"));
    }

    [TestMethod]
    public void Capitalize_UpperCase_KeepsRest()
    {
        Assert.AreEqual("HELLO", Library.Capitalize("hELLO"));
    }

    [TestMethod]
    public void Capitalize_AlreadyCapitalized_ReturnsSame()
    {
        Assert.AreEqual("Hello", Library.Capitalize("Hello"));
    }

    [TestMethod]
    public void Repeat_Null_ReturnsNull()
    {
        Assert.IsNull(Library.Repeat(null, 3));
    }

    [TestMethod]
    public void Repeat_ZeroCount_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.Repeat("hello", 0));
    }

    [TestMethod]
    public void Repeat_NegativeCount_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.Repeat("hello", -1));
    }

    [TestMethod]
    public void Repeat_CountOne_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.Repeat("hello", 1));
    }

    [TestMethod]
    public void Repeat_CountTwo_ReturnsDouble()
    {
        Assert.AreEqual("hellohello", Library.Repeat("hello", 2));
    }

    [TestMethod]
    public void Repeat_WithSeparator_IncludesSeparator()
    {
        Assert.AreEqual("hello, hello, hello", Library.Repeat("hello", 3, ", "));
    }

    [TestMethod]
    public void Repeat_EmptySeparator_NoSeparator()
    {
        Assert.AreEqual("aaa", Library.Repeat("a", 3));
    }

    [TestMethod]
    public void Repeat_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.Repeat(string.Empty, 3));
    }

    [TestMethod]
    public void Wrap_Null_ReturnsNull()
    {
        Assert.IsNull(Library.Wrap(null, "[", "]"));
    }

    [TestMethod]
    public void Wrap_WithPrefixAndSuffix_Wraps()
    {
        Assert.AreEqual("[hello]", Library.Wrap("hello", "[", "]"));
    }

    [TestMethod]
    public void Wrap_NullPrefix_UsesSuffixOnly()
    {
        Assert.AreEqual("hello]", Library.Wrap("hello", null, "]"));
    }

    [TestMethod]
    public void Wrap_NullSuffix_UsesPrefixOnly()
    {
        Assert.AreEqual("[hello", Library.Wrap("hello", "[", null));
    }

    [TestMethod]
    public void Wrap_BothNull_ReturnsValue()
    {
        Assert.AreEqual("hello", Library.Wrap("hello", null, null));
    }

    [TestMethod]
    public void Wrap_EmptyValue_ReturnsWrappers()
    {
        Assert.AreEqual("[]", Library.Wrap(string.Empty, "[", "]"));
    }

    [TestMethod]
    public void RemovePrefix_Null_ReturnsNull()
    {
        Assert.IsNull(Library.RemovePrefix(null, "pre"));
    }

    [TestMethod]
    public void RemovePrefix_HasPrefix_RemovesPrefix()
    {
        Assert.AreEqual("fix", Library.RemovePrefix("prefix", "pre"));
    }

    [TestMethod]
    public void RemovePrefix_NoPrefix_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.RemovePrefix("hello", "pre"));
    }

    [TestMethod]
    public void RemovePrefix_NullPrefix_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.RemovePrefix("hello", null));
    }

    [TestMethod]
    public void RemovePrefix_EmptyPrefix_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.RemovePrefix("hello", string.Empty));
    }

    [TestMethod]
    public void RemovePrefix_EntireString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.RemovePrefix("hello", "hello"));
    }

    [TestMethod]
    public void RemoveSuffix_Null_ReturnsNull()
    {
        Assert.IsNull(Library.RemoveSuffix(null, "fix"));
    }

    [TestMethod]
    public void RemoveSuffix_HasSuffix_RemovesSuffix()
    {
        Assert.AreEqual("suf", Library.RemoveSuffix("suffix", "fix"));
    }

    [TestMethod]
    public void RemoveSuffix_NoSuffix_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.RemoveSuffix("hello", "fix"));
    }

    [TestMethod]
    public void RemoveSuffix_NullSuffix_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.RemoveSuffix("hello", null));
    }

    [TestMethod]
    public void RemoveSuffix_EmptySuffix_ReturnsSame()
    {
        Assert.AreEqual("hello", Library.RemoveSuffix("hello", string.Empty));
    }

    [TestMethod]
    public void RemoveSuffix_EntireString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.RemoveSuffix("hello", "hello"));
    }

}
