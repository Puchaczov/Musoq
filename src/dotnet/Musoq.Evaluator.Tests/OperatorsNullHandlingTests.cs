using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for null handling in the Operators class.
///     Ensures operators return safe values instead of throwing exceptions when dealing with nulls.
/// </summary>
[TestClass]
public class OperatorsNullHandlingTests
{
    private Operators _operators;

    [TestInitialize]
    public void Setup()
    {
        _operators = new Operators();
    }

    #region Contains Operator Tests

    [TestMethod]
    public void Contains_WhenArrayIsNull_ShouldReturnFalse()
    {
        var value = "test";
        string[] array = null;


        var result = _operators.Contains(value, array);


        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Contains_WhenValueIsNullAndArrayIsNull_ShouldReturnFalse()
    {
        string value = null;
        string[] array = null;


        var result = _operators.Contains(value, array);


        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Contains_WhenValueIsNullAndArrayContainsNull_ShouldReturnTrue()
    {
        string value = null;
        var array = new[] { "a", null, "c" };


        var result = _operators.Contains(value, array);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Contains_WhenValueIsNullAndArrayDoesNotContainNull_ShouldReturnFalse()
    {
        string value = null;
        var array = new[] { "a", "b", "c" };


        var result = _operators.Contains(value, array);


        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Contains_WhenValueExistsInArray_ShouldReturnTrue()
    {
        var value = "b";
        var array = new[] { "a", "b", "c" };


        var result = _operators.Contains(value, array);


        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Contains_WhenValueDoesNotExistInArray_ShouldReturnFalse()
    {
        var value = "d";
        var array = new[] { "a", "b", "c" };


        var result = _operators.Contains(value, array);


        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Contains_WithIntegersWhenArrayIsNull_ShouldReturnFalse()
    {
        var value = 5;
        int[] array = null;


        var result = _operators.Contains(value, array);


        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Contains_WithNullableIntegersWhenValueIsNull_ShouldWork()
    {
        int? value = null;
        var array = new int?[] { 1, null, 3 };


        var result = _operators.Contains(value, array);


        Assert.IsTrue(result);
    }

    #endregion

    #region LIKE Operator Tests (existing functionality verification)

    [TestMethod]
    public void Like_WhenContentIsNull_ShouldReturnFalse()
    {
        var result = _operators.Like(null, "pattern");
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Like_WhenPatternIsNull_ShouldReturnFalse()
    {
        var result = _operators.Like("content", null);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Like_WhenBothAreNull_ShouldReturnFalse()
    {
        var result = _operators.Like(null, null);
        Assert.IsFalse(result);
    }

    #endregion

    #region RLIKE Operator Tests (existing functionality verification)

    [TestMethod]
    public void RLike_WhenContentIsNull_ShouldReturnFalse()
    {
        var result = _operators.RLike(null, "pattern");
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void RLike_WhenPatternIsNull_ShouldReturnFalse()
    {
        var result = _operators.RLike("content", null);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void RLike_WhenBothAreNull_ShouldReturnFalse()
    {
        var result = _operators.RLike(null, null);
        Assert.IsFalse(result);
    }

    #endregion

    #region LIKE Fast Path Tests

    [TestMethod]
    public void Like_WhenPatternHasNoWildcards_ShouldMatchExact()
    {
        Assert.IsTrue(_operators.Like("hello", "hello"));
        Assert.IsFalse(_operators.Like("hello", "world"));
    }

    [TestMethod]
    public void Like_WhenPatternHasNoWildcards_ShouldBeCaseInsensitive()
    {
        Assert.IsTrue(_operators.Like("Hello", "HELLO"));
        Assert.IsTrue(_operators.Like("WORLD", "world"));
    }

    [TestMethod]
    public void Like_WhenPatternStartsWithPercent_ShouldMatchSuffix()
    {
        Assert.IsTrue(_operators.Like("hello world", "%world"));
        Assert.IsFalse(_operators.Like("hello world", "%xyz"));
    }

    [TestMethod]
    public void Like_WhenPatternEndsWithPercent_ShouldMatchPrefix()
    {
        Assert.IsTrue(_operators.Like("hello world", "hello%"));
        Assert.IsFalse(_operators.Like("hello world", "xyz%"));
    }

    [TestMethod]
    public void Like_WhenPatternWrappedWithPercent_ShouldMatchContains()
    {
        Assert.IsTrue(_operators.Like("hello world", "%lo wo%"));
        Assert.IsFalse(_operators.Like("hello world", "%xyz%"));
    }

    [TestMethod]
    public void Like_WhenPatternIsSinglePercent_ShouldMatchAnything()
    {
        Assert.IsTrue(_operators.Like("anything", "%"));
        Assert.IsTrue(_operators.Like("", "%"));
    }

    [TestMethod]
    public void Like_WhenPatternIsDoublePercent_ShouldMatchAnything()
    {
        Assert.IsTrue(_operators.Like("anything", "%%"));
        Assert.IsTrue(_operators.Like("", "%%"));
    }

    [TestMethod]
    public void Like_WhenPatternHasMultiplePercents_ShouldFallbackToRegex()
    {
        Assert.IsTrue(_operators.Like("hello world foo", "hello%world%foo"));
        Assert.IsFalse(_operators.Like("hello world foo", "hello%bar%foo"));
    }

    [TestMethod]
    public void Like_WhenPatternHasUnderscore_ShouldFallbackToRegex()
    {
        Assert.IsTrue(_operators.Like("hat", "h_t"));
        Assert.IsFalse(_operators.Like("hoot", "h_t"));
    }

    [TestMethod]
    public void Like_WhenPatternHasNonAscii_ShouldFallbackToRegex()
    {
        Assert.IsTrue(_operators.Like("café", "caf%"));
        Assert.IsTrue(_operators.Like("naïve", "%ïve"));
    }

    [TestMethod]
    public void Like_WhenPatternIsEmpty_ShouldMatchEmptyString()
    {
        Assert.IsTrue(_operators.Like("", ""));
        Assert.IsFalse(_operators.Like("notempty", ""));
    }

    [TestMethod]
    public void Like_WhenSuffixMatchIsCaseInsensitive_ShouldWork()
    {
        Assert.IsTrue(_operators.Like("Hello World", "%WORLD"));
        Assert.IsTrue(_operators.Like("HELLO WORLD", "%world"));
    }

    [TestMethod]
    public void Like_WhenPrefixMatchIsCaseInsensitive_ShouldWork()
    {
        Assert.IsTrue(_operators.Like("Hello World", "HELLO%"));
        Assert.IsTrue(_operators.Like("HELLO WORLD", "hello%"));
    }

    [TestMethod]
    public void Like_WhenContainsMatchIsCaseInsensitive_ShouldWork()
    {
        Assert.IsTrue(_operators.Like("Hello World", "%LO WO%"));
        Assert.IsTrue(_operators.Like("HELLO WORLD", "%lo wo%"));
    }

    [TestMethod]
    public void Like_WhenPatternHasRegexSpecialChars_ShouldTreatAsLiteral()
    {
        Assert.IsTrue(_operators.Like("price is $100", "%$100"));
        Assert.IsTrue(_operators.Like("file.txt", "file.txt"));
        Assert.IsTrue(_operators.Like("(test)", "%(test)%"));
    }

    [TestMethod]
    public void Like_WhenCalledMultipleTimes_ShouldUseCachedMatcher()
    {
        var pattern = "cached_test%";

        Assert.IsTrue(_operators.Like("cached_test_value1", pattern));
        Assert.IsTrue(_operators.Like("cached_test_value2", pattern));
        Assert.IsFalse(_operators.Like("other_value", pattern));
    }

    #endregion
}
