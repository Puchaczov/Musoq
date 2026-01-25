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
}
