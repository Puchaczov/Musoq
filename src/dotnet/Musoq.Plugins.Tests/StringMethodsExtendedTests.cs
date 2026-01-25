using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for string methods to improve branch coverage.
///     Tests ToSnakeCase, ToKebabCase, ToCamelCase, ToPascalCase,
///     WordCount, LineCount, SentenceCount, and other string utilities.
/// </summary>
[TestClass]
public class StringMethodsExtendedTests : LibraryBaseBaseTests
{
    #region ToSnakeCase Tests

    [TestMethod]
    public void ToSnakeCase_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ToSnakeCase(null));
    }

    [TestMethod]
    public void ToSnakeCase_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ToSnakeCase(string.Empty));
    }

    [TestMethod]
    public void ToSnakeCase_SingleLowerLetter_ReturnsSame()
    {
        Assert.AreEqual("a", Library.ToSnakeCase("a"));
    }

    [TestMethod]
    public void ToSnakeCase_SingleUpperLetter_ReturnsLower()
    {
        Assert.AreEqual("a", Library.ToSnakeCase("A"));
    }

    [TestMethod]
    public void ToSnakeCase_PascalCase_ReturnsSnakeCase()
    {
        Assert.AreEqual("hello_world", Library.ToSnakeCase("HelloWorld"));
    }

    [TestMethod]
    public void ToSnakeCase_CamelCase_ReturnsSnakeCase()
    {
        Assert.AreEqual("hello_world", Library.ToSnakeCase("helloWorld"));
    }

    [TestMethod]
    public void ToSnakeCase_XMLParser_ReturnsSnakeCaseWithAcronym()
    {
        Assert.AreEqual("xml_parser", Library.ToSnakeCase("XMLParser"));
    }

    [TestMethod]
    public void ToSnakeCase_ConsecutiveUppercase_HandlesCorrectly()
    {
        Assert.AreEqual("get_http_response", Library.ToSnakeCase("GetHTTPResponse"));
    }

    [TestMethod]
    public void ToSnakeCase_WithSpace_ReplacesWithUnderscore()
    {
        Assert.AreEqual("hello_world", Library.ToSnakeCase("hello world"));
    }

    [TestMethod]
    public void ToSnakeCase_WithDash_ReplacesWithUnderscore()
    {
        Assert.AreEqual("hello_world", Library.ToSnakeCase("hello-world"));
    }

    [TestMethod]
    public void ToSnakeCase_AlreadySnakeCase_ReturnsSame()
    {
        Assert.AreEqual("hello_world", Library.ToSnakeCase("hello_world"));
    }

    [TestMethod]
    public void ToSnakeCase_AllUppercase_ReturnsLowercase()
    {
        Assert.AreEqual("hello", Library.ToSnakeCase("HELLO"));
    }

    [TestMethod]
    public void ToSnakeCase_MixedWithNumbers_PreservesNumbers()
    {
        Assert.AreEqual("test123_value", Library.ToSnakeCase("Test123Value"));
    }

    [TestMethod]
    public void ToSnakeCase_StartsWithUpper_NoLeadingUnderscore()
    {
        Assert.AreEqual("test", Library.ToSnakeCase("Test"));
    }

    [TestMethod]
    public void ToSnakeCase_UpperAtEndFollowedByNothing_HandlesCorrectly()
    {
        Assert.AreEqual("hello_w", Library.ToSnakeCase("HelloW"));
    }

    #endregion

    #region ToKebabCase Tests

    [TestMethod]
    public void ToKebabCase_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ToKebabCase(null));
    }

    [TestMethod]
    public void ToKebabCase_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ToKebabCase(string.Empty));
    }

    [TestMethod]
    public void ToKebabCase_SingleLowerLetter_ReturnsSame()
    {
        Assert.AreEqual("a", Library.ToKebabCase("a"));
    }

    [TestMethod]
    public void ToKebabCase_SingleUpperLetter_ReturnsLower()
    {
        Assert.AreEqual("a", Library.ToKebabCase("A"));
    }

    [TestMethod]
    public void ToKebabCase_PascalCase_ReturnsKebabCase()
    {
        Assert.AreEqual("hello-world", Library.ToKebabCase("HelloWorld"));
    }

    [TestMethod]
    public void ToKebabCase_CamelCase_ReturnsKebabCase()
    {
        Assert.AreEqual("hello-world", Library.ToKebabCase("helloWorld"));
    }

    [TestMethod]
    public void ToKebabCase_XMLParser_ReturnsKebabCaseWithAcronym()
    {
        Assert.AreEqual("xml-parser", Library.ToKebabCase("XMLParser"));
    }

    [TestMethod]
    public void ToKebabCase_ConsecutiveUppercase_HandlesCorrectly()
    {
        Assert.AreEqual("get-http-response", Library.ToKebabCase("GetHTTPResponse"));
    }

    [TestMethod]
    public void ToKebabCase_WithSpace_ReplacesWithDash()
    {
        Assert.AreEqual("hello-world", Library.ToKebabCase("hello world"));
    }

    [TestMethod]
    public void ToKebabCase_WithUnderscore_ReplacesWithDash()
    {
        Assert.AreEqual("hello-world", Library.ToKebabCase("hello_world"));
    }

    [TestMethod]
    public void ToKebabCase_AlreadyKebabCase_ReturnsSame()
    {
        Assert.AreEqual("hello-world", Library.ToKebabCase("hello-world"));
    }

    [TestMethod]
    public void ToKebabCase_AllUppercase_ReturnsLowercase()
    {
        Assert.AreEqual("hello", Library.ToKebabCase("HELLO"));
    }

    [TestMethod]
    public void ToKebabCase_MixedWithNumbers_PreservesNumbers()
    {
        Assert.AreEqual("test123-value", Library.ToKebabCase("Test123Value"));
    }

    [TestMethod]
    public void ToKebabCase_UpperAtEnd_HandlesCorrectly()
    {
        Assert.AreEqual("hello-w", Library.ToKebabCase("HelloW"));
    }

    #endregion

    #region ToCamelCase Tests

    [TestMethod]
    public void ToCamelCase_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ToCamelCase(null));
    }

    [TestMethod]
    public void ToCamelCase_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ToCamelCase(string.Empty));
    }

    [TestMethod]
    public void ToCamelCase_SingleLowerLetter_ReturnsSame()
    {
        Assert.AreEqual("a", Library.ToCamelCase("a"));
    }

    [TestMethod]
    public void ToCamelCase_SingleUpperLetter_ReturnsLower()
    {
        Assert.AreEqual("a", Library.ToCamelCase("A"));
    }

    [TestMethod]
    public void ToCamelCase_SnakeCase_ReturnsCamelCase()
    {
        Assert.AreEqual("helloWorld", Library.ToCamelCase("hello_world"));
    }

    [TestMethod]
    public void ToCamelCase_KebabCase_ReturnsCamelCase()
    {
        Assert.AreEqual("helloWorld", Library.ToCamelCase("hello-world"));
    }

    [TestMethod]
    public void ToCamelCase_SpaceSeparated_ReturnsCamelCase()
    {
        Assert.AreEqual("helloWorld", Library.ToCamelCase("hello world"));
    }

    [TestMethod]
    public void ToCamelCase_PascalCase_ReturnsCamelCase()
    {
        Assert.AreEqual("helloWorld", Library.ToCamelCase("HelloWorld"));
    }

    [TestMethod]
    public void ToCamelCase_AllUppercase_ReturnsAllLowerCase()
    {
        Assert.AreEqual("hELLO", Library.ToCamelCase("HELLO"));
    }

    [TestMethod]
    public void ToCamelCase_WithNumbers_PreservesNumbers()
    {
        Assert.AreEqual("test123", Library.ToCamelCase("test123"));
    }

    [TestMethod]
    public void ToCamelCase_ConsecutiveDelimiters_HandlesCorrectly()
    {
        Assert.AreEqual("helloWorld", Library.ToCamelCase("hello__world"));
    }

    [TestMethod]
    public void ToCamelCase_TrailingDelimiter_HandlesCorrectly()
    {
        Assert.AreEqual("hello", Library.ToCamelCase("hello_"));
    }

    [TestMethod]
    public void ToCamelCase_LeadingDelimiter_HandlesCorrectly()
    {
        Assert.AreEqual("Hello", Library.ToCamelCase("_hello"));
    }

    #endregion

    #region ToPascalCase Tests

    [TestMethod]
    public void ToPascalCase_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ToPascalCase(null));
    }

    [TestMethod]
    public void ToPascalCase_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ToPascalCase(string.Empty));
    }

    [TestMethod]
    public void ToPascalCase_SingleLowerLetter_ReturnsUpper()
    {
        Assert.AreEqual("A", Library.ToPascalCase("a"));
    }

    [TestMethod]
    public void ToPascalCase_SingleUpperLetter_ReturnsSame()
    {
        Assert.AreEqual("A", Library.ToPascalCase("A"));
    }

    [TestMethod]
    public void ToPascalCase_SnakeCase_ReturnsPascalCase()
    {
        Assert.AreEqual("HelloWorld", Library.ToPascalCase("hello_world"));
    }

    [TestMethod]
    public void ToPascalCase_KebabCase_ReturnsPascalCase()
    {
        Assert.AreEqual("HelloWorld", Library.ToPascalCase("hello-world"));
    }

    [TestMethod]
    public void ToPascalCase_SpaceSeparated_ReturnsPascalCase()
    {
        Assert.AreEqual("HelloWorld", Library.ToPascalCase("hello world"));
    }

    [TestMethod]
    public void ToPascalCase_AlreadyPascalCase_ReturnsSame()
    {
        Assert.AreEqual("HelloWorld", Library.ToPascalCase("HelloWorld"));
    }

    [TestMethod]
    public void ToPascalCase_CamelCase_ReturnsPascalCase()
    {
        Assert.AreEqual("HelloWorld", Library.ToPascalCase("helloWorld"));
    }

    [TestMethod]
    public void ToPascalCase_WithNumbers_PreservesNumbers()
    {
        Assert.AreEqual("Test123Value", Library.ToPascalCase("test123_value"));
    }

    [TestMethod]
    public void ToPascalCase_ConsecutiveDelimiters_HandlesCorrectly()
    {
        Assert.AreEqual("HelloWorld", Library.ToPascalCase("hello__world"));
    }

    [TestMethod]
    public void ToPascalCase_OnlyDelimiters_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ToPascalCase("___"));
    }

    #endregion

    #region WordCount Tests

    [TestMethod]
    public void WordCount_Null_ReturnsNull()
    {
        Assert.IsNull(Library.WordCount(null));
    }

    [TestMethod]
    public void WordCount_EmptyString_ReturnsZero()
    {
        Assert.AreEqual(0, Library.WordCount(string.Empty));
    }

    [TestMethod]
    public void WordCount_OnlyWhitespace_ReturnsZero()
    {
        Assert.AreEqual(0, Library.WordCount("   \t\n  "));
    }

    [TestMethod]
    public void WordCount_SingleWord_ReturnsOne()
    {
        Assert.AreEqual(1, Library.WordCount("hello"));
    }

    [TestMethod]
    public void WordCount_TwoWords_ReturnsTwo()
    {
        Assert.AreEqual(2, Library.WordCount("hello world"));
    }

    [TestMethod]
    public void WordCount_MultipleSpaces_CountsCorrectly()
    {
        Assert.AreEqual(2, Library.WordCount("hello    world"));
    }

    [TestMethod]
    public void WordCount_LeadingSpaces_CountsCorrectly()
    {
        Assert.AreEqual(1, Library.WordCount("   hello"));
    }

    [TestMethod]
    public void WordCount_TrailingSpaces_CountsCorrectly()
    {
        Assert.AreEqual(1, Library.WordCount("hello   "));
    }

    [TestMethod]
    public void WordCount_MixedWhitespace_CountsCorrectly()
    {
        Assert.AreEqual(3, Library.WordCount("one\ttwo\nthree"));
    }

    [TestMethod]
    public void WordCount_SingleCharacter_ReturnsOne()
    {
        Assert.AreEqual(1, Library.WordCount("a"));
    }

    #endregion

    #region LineCount Tests

    [TestMethod]
    public void LineCount_Null_ReturnsNull()
    {
        Assert.IsNull(Library.LineCount(null));
    }

    [TestMethod]
    public void LineCount_EmptyString_ReturnsZero()
    {
        Assert.AreEqual(0, Library.LineCount(string.Empty));
    }

    [TestMethod]
    public void LineCount_SingleLine_ReturnsOne()
    {
        Assert.AreEqual(1, Library.LineCount("hello"));
    }

    [TestMethod]
    public void LineCount_TwoLinesUnix_ReturnsTwo()
    {
        Assert.AreEqual(2, Library.LineCount("line1\nline2"));
    }

    [TestMethod]
    public void LineCount_TwoLinesWindows_ReturnsTwo()
    {
        Assert.AreEqual(2, Library.LineCount("line1\r\nline2"));
    }

    [TestMethod]
    public void LineCount_ThreeLinesMixed_ReturnsThree()
    {
        Assert.AreEqual(3, Library.LineCount("line1\nline2\r\nline3"));
    }

    [TestMethod]
    public void LineCount_TrailingNewline_CountsExtraLine()
    {
        Assert.AreEqual(2, Library.LineCount("line1\n"));
    }

    [TestMethod]
    public void LineCount_OnlyNewlines_CountsCorrectly()
    {
        Assert.AreEqual(3, Library.LineCount("\n\n"));
    }

    [TestMethod]
    public void LineCount_CarriageReturnOnly_CountsAsLine()
    {
        Assert.AreEqual(2, Library.LineCount("line1\rline2"));
    }

    [TestMethod]
    public void LineCount_CarriageReturnAtEnd_CountsAsLine()
    {
        Assert.AreEqual(2, Library.LineCount("line1\r"));
    }

    #endregion

    #region SentenceCount Tests

    [TestMethod]
    public void SentenceCount_Null_ReturnsNull()
    {
        Assert.IsNull(Library.SentenceCount(null));
    }

    [TestMethod]
    public void SentenceCount_EmptyString_ReturnsZero()
    {
        Assert.AreEqual(0, Library.SentenceCount(string.Empty));
    }

    [TestMethod]
    public void SentenceCount_OnlyWhitespace_ReturnsZero()
    {
        Assert.AreEqual(0, Library.SentenceCount("   "));
    }

    [TestMethod]
    public void SentenceCount_SingleSentencePeriod_ReturnsOne()
    {
        Assert.AreEqual(1, Library.SentenceCount("Hello world."));
    }

    [TestMethod]
    public void SentenceCount_SingleSentenceExclamation_ReturnsOne()
    {
        Assert.AreEqual(1, Library.SentenceCount("Hello world!"));
    }

    [TestMethod]
    public void SentenceCount_SingleSentenceQuestion_ReturnsOne()
    {
        Assert.AreEqual(1, Library.SentenceCount("Hello world?"));
    }

    [TestMethod]
    public void SentenceCount_TwoSentences_ReturnsTwo()
    {
        Assert.AreEqual(2, Library.SentenceCount("First. Second."));
    }

    [TestMethod]
    public void SentenceCount_MixedDelimiters_CountsCorrectly()
    {
        Assert.AreEqual(3, Library.SentenceCount("Hello! How are you? Good."));
    }

    [TestMethod]
    public void SentenceCount_NoDelimiter_ReturnsOne()
    {
        Assert.AreEqual(1, Library.SentenceCount("Hello world"));
    }

    [TestMethod]
    public void SentenceCount_ConsecutiveDelimiters_CountsOnce()
    {
        Assert.AreEqual(1, Library.SentenceCount("Hello..."));
    }

    [TestMethod]
    public void SentenceCount_DelimiterAtStart_HandlesCorrectly()
    {
        Assert.AreEqual(1, Library.SentenceCount(".Hello"));
    }

    #endregion

    #region RegexExtract Tests

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

    #endregion

    #region RegexExtractAll Tests

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

    #endregion

    #region IsMatch Tests

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

    #endregion

    #region Truncate Tests

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

    #endregion

    #region Capitalize Tests

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

    #endregion

    #region Repeat Tests

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

    #endregion

    #region Wrap Tests

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

    #endregion

    #region RemovePrefix Tests

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

    #endregion

    #region RemoveSuffix Tests

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

    #endregion

    #region IsNumeric Tests

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

    #endregion

    #region IsAlpha Tests

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

    #endregion

    #region IsAlphaNumeric Tests

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

    #endregion

    #region CountOccurrences Tests

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

    #endregion

    #region RemoveWhitespace Tests

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

    #endregion
}
