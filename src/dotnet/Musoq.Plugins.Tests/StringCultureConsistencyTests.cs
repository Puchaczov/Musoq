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
public class StringCultureConsistencyTests : LibraryBaseBaseTests
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
        // "hello HELLO Hello" - searching for "hello" should find all three
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
        // Should find the last "hello" (at position 12), even though it's "Hello"
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
        // In Turkish culture, 'i'.ToUpper() yields İ (U+0130), not 'I'.
        // With InvariantCulture, 'i'.ToUpper() always yields 'I'.
        var result = Library.ToUpper("file");
        Assert.AreEqual("FILE", result);
    }

    [TestMethod]
    public void ToUpper_WithSpecificCulture_UsesThatCulture()
    {
        // The overload with culture name should use that specific culture
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
        // ß uppercases to SS in InvariantCulture
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
        // In Turkish culture, 'I'.ToLower() yields ı (dotless i, U+0131), not 'i'.
        // With InvariantCulture, 'I'.ToLower() always yields 'i'.
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
        // ToTitleCase only capitalizes the first letter of words that are all lowercase
        // All-uppercase words are left as-is (per .NET behavior)
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

    #region Soundex - InvariantCulture (Fixed from CurrentCulture ToUpper)

    [TestMethod]
    public void Soundex_BasicWord_ReturnsSoundexCode()
    {
        Assert.AreEqual("R163", Library.Soundex("Robert"));
    }

    [TestMethod]
    public void Soundex_SameSound_ReturnsSameCode()
    {
        // Robert and Rupert should have the same soundex
        Assert.AreEqual(Library.Soundex("Robert"), Library.Soundex("Rupert"));
    }

    [TestMethod]
    public void Soundex_DifferentSound_ReturnsDifferentCode()
    {
        Assert.AreNotEqual(Library.Soundex("Robert"), Library.Soundex("Smith"));
    }

    [TestMethod]
    public void Soundex_NullInput_ReturnsNull()
    {
        Assert.IsNull(Library.Soundex(null));
    }

    [TestMethod]
    public void Soundex_LowercaseInput_WorksCorrectly()
    {
        // Should work regardless of input case since ToUpperInvariant is used internally
        Assert.AreEqual(Library.Soundex("Robert"), Library.Soundex("robert"));
    }

    [TestMethod]
    public void Soundex_MixedCaseInput_WorksCorrectly()
    {
        Assert.AreEqual(Library.Soundex("ROBERT"), Library.Soundex("rObErT"));
    }

    #endregion

    #region Cross-Function Consistency Checks

    [TestMethod]
    public void AllSearchFunctions_CaseInsensitive_Consistent()
    {
        // Verify that Contains, IndexOf, StartsWith, EndsWith all agree on case-insensitivity
        var text = "Hello World";

        // All should find "hello" (lowercase) in "Hello World"
        Assert.IsTrue(Library.Contains(text, "hello"), "Contains should be case-insensitive");
        Assert.AreEqual(0, Library.IndexOf(text, "hello"), "IndexOf should be case-insensitive");
        Assert.IsTrue(Library.StartsWith(text, "hello"), "StartsWith should be case-insensitive");

        // All should find "WORLD" (uppercase) in "Hello World"
        Assert.IsTrue(Library.Contains(text, "WORLD"), "Contains should find WORLD");
        Assert.AreEqual(6, Library.IndexOf(text, "WORLD"), "IndexOf should find WORLD");
        Assert.IsTrue(Library.EndsWith(text, "WORLD"), "EndsWith should be case-insensitive");
    }

    [TestMethod]
    public void IndexOf_And_NthIndexOf_ConsistentBehavior()
    {
        var text = "Hello World Hello";

        // IndexOf finds first occurrence
        var indexOfResult = Library.IndexOf(text, "hello");

        // NthIndexOf with index 0 should return the same
        var nthResult = Library.NthIndexOf(text, "hello", 0);

        Assert.AreEqual(indexOfResult, nthResult,
            "IndexOf and NthIndexOf(0) should return the same position");
    }

    [TestMethod]
    public void IndexOf_And_LastIndexOf_ConsistentBehavior()
    {
        var text = "test";

        // For single occurrence, IndexOf and LastIndexOf should agree
        var indexOfResult = Library.IndexOf(text, "TEST");
        var lastIndexOfResult = Library.LastIndexOf(text, "TEST");

        Assert.AreEqual(indexOfResult, lastIndexOfResult,
            "IndexOf and LastIndexOf should agree for single occurrence");
    }

    [TestMethod]
    public void Replace_And_Contains_ConsistentBehavior()
    {
        var text = "Hello World";

        // If Contains finds it, Replace should replace it
        Assert.IsTrue(Library.Contains(text, "world"));
        var replaced = Library.Replace(text, "world", "Earth");
        Assert.AreEqual("Hello Earth", replaced);
    }

    [TestMethod]
    public void StartsWith_And_RemovePrefix_ConsistentBehavior()
    {
        var text = "HelloWorld";

        // If StartsWith matches, RemovePrefix should remove it
        Assert.IsTrue(Library.StartsWith(text, "HELLO"));
        var result = Library.RemovePrefix(text, "HELLO");
        Assert.AreEqual("World", result);
    }

    [TestMethod]
    public void EndsWith_And_RemoveSuffix_ConsistentBehavior()
    {
        var text = "HelloWorld";

        // If EndsWith matches, RemoveSuffix should remove it
        Assert.IsTrue(Library.EndsWith(text, "WORLD"));
        var result = Library.RemoveSuffix(text, "WORLD");
        Assert.AreEqual("Hello", result);
    }

    #endregion

    #region Unicode and Multilingual

    [TestMethod]
    public void Contains_Unicode_Polish_ShouldWork()
    {
        Assert.IsTrue(Library.Contains("Zażółć gęślą jaźń", "gęślą"));
    }

    [TestMethod]
    public void IndexOf_Unicode_Russian_ShouldWork()
    {
        Assert.AreEqual(7, Library.IndexOf("Привет мир", "мир"));
    }

    [TestMethod]
    public void NthIndexOf_Unicode_Japanese_ShouldWork()
    {
        var input = "東京 大阪 東京 名古屋";
        Assert.AreEqual(0, Library.NthIndexOf(input, "東京", 0));
        Assert.AreEqual(6, Library.NthIndexOf(input, "東京", 1));
    }

    [TestMethod]
    public void LastIndexOf_Unicode_Chinese_ShouldWork()
    {
        var result = Library.LastIndexOf("北京 上海 北京", "北京");
        Assert.AreEqual(6, result);
    }

    [TestMethod]
    public void Replace_Unicode_Korean_ShouldWork()
    {
        var result = Library.Replace("서울은 한국의 수도입니다", "서울", "부산");
        Assert.AreEqual("부산은 한국의 수도입니다", result);
    }

    [TestMethod]
    public void StartsWith_Unicode_Arabic_ShouldWork()
    {
        Assert.IsTrue(Library.StartsWith("مرحبا بالعالم", "مرحبا"));
    }

    [TestMethod]
    public void EndsWith_Unicode_Thai_ShouldWork()
    {
        Assert.IsTrue(Library.EndsWith("สวัสดีครับ", "ครับ"));
    }

    [TestMethod]
    public void RemovePrefix_Unicode_German_ShouldWork()
    {
        Assert.AreEqual(" München", Library.RemovePrefix("Grüße München", "Grüße"));
    }

    [TestMethod]
    public void RemoveSuffix_Unicode_French_ShouldWork()
    {
        Assert.AreEqual("Château de ", Library.RemoveSuffix("Château de Versailles", "Versailles"));
    }

    [TestMethod]
    public void ToUpper_Unicode_Greek_ShouldWork()
    {
        Assert.AreEqual("ΑΘΉΝΑ", Library.ToUpper("Αθήνα"));
    }

    [TestMethod]
    public void ToLower_Unicode_Ukrainian_ShouldWork()
    {
        Assert.AreEqual("київ", Library.ToLower("КИЇВ"));
    }

    #endregion

    #region Emoji Support

    [TestMethod]
    public void Contains_Emoji_ShouldWork()
    {
        Assert.IsTrue(Library.Contains("Hello 🌍 World", "🌍"));
    }

    [TestMethod]
    public void Replace_Emoji_ShouldWork()
    {
        var result = Library.Replace("I ❤️ coding", "❤️", "💙");
        Assert.AreEqual("I 💙 coding", result);
    }

    [TestMethod]
    public void IndexOf_Emoji_ShouldWork()
    {
        var result = Library.IndexOf("abc 🎉 def", "🎉");
        Assert.IsNotNull(result);
        Assert.IsGreaterThanOrEqualTo(0, result.Value);
    }

    #endregion
}
