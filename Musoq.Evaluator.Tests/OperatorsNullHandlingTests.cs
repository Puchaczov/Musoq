using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Tests for null handling in the Operators class.
/// Ensures operators return safe values instead of throwing exceptions when dealing with nulls.
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
        // Arrange
        string value = "test";
        string[] array = null;

        // Act
        var result = _operators.Contains(value, array);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Contains_WhenValueIsNullAndArrayIsNull_ShouldReturnFalse()
    {
        // Arrange
        string value = null;
        string[] array = null;

        // Act
        var result = _operators.Contains(value, array);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Contains_WhenValueIsNullAndArrayContainsNull_ShouldReturnTrue()
    {
        // Arrange
        string value = null;
        string[] array = new[] { "a", null, "c" };

        // Act
        var result = _operators.Contains(value, array);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Contains_WhenValueIsNullAndArrayDoesNotContainNull_ShouldReturnFalse()
    {
        // Arrange
        string value = null;
        string[] array = new[] { "a", "b", "c" };

        // Act
        var result = _operators.Contains(value, array);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Contains_WhenValueExistsInArray_ShouldReturnTrue()
    {
        // Arrange
        string value = "b";
        string[] array = new[] { "a", "b", "c" };

        // Act
        var result = _operators.Contains(value, array);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Contains_WhenValueDoesNotExistInArray_ShouldReturnFalse()
    {
        // Arrange
        string value = "d";
        string[] array = new[] { "a", "b", "c" };

        // Act
        var result = _operators.Contains(value, array);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Contains_WithIntegersWhenArrayIsNull_ShouldReturnFalse()
    {
        // Arrange
        int value = 5;
        int[] array = null;

        // Act
        var result = _operators.Contains(value, array);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Contains_WithNullableIntegersWhenValueIsNull_ShouldWork()
    {
        // Arrange
        int? value = null;
        int?[] array = new int?[] { 1, null, 3 };

        // Act
        var result = _operators.Contains(value, array);

        // Assert
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
