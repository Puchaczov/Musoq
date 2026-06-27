using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class TypeConversionEdgeCasesTests
{
    #region TryConvertToInt64Comparison Tests

    [TestMethod]
    public void TryConvertToInt64Comparison_WithByte_ShouldConvert()
    {
        // Arrange
        object input = (byte)255;

        // Act
        var result = Library.TryConvertToInt64Comparison(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(255L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_WithULong_OutOfRange_ShouldReturnNull()
    {
        // Arrange
        object input = (ulong)long.MaxValue + 1;

        // Act
        var result = Library.TryConvertToInt64Comparison(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_WithFloat_OutOfRange_ShouldReturnNull()
    {
        // Arrange
        object input = (float)long.MaxValue * 2;

        // Act
        var result = Library.TryConvertToInt64Comparison(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_WithDouble_OutOfRange_ShouldReturnNull()
    {
        // Arrange
        object input = long.MaxValue + 10000.0;

        // Act
        var result = Library.TryConvertToInt64Comparison(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_WithDecimal_OutOfRange_ShouldReturnNull()
    {
        // Arrange
        object input = (decimal)long.MaxValue + 1;

        // Act
        var result = Library.TryConvertToInt64Comparison(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_WithString_Valid_ShouldConvert()
    {
        // Arrange
        object input = "9223372036854775807"; // long.MaxValue

        // Act
        var result = Library.TryConvertToInt64Comparison(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(long.MaxValue, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_WithBool_True_ShouldConvert()
    {
        // Arrange
        object input = true;

        // Act
        var result = Library.TryConvertToInt64Comparison(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_WithBool_False_ShouldConvert()
    {
        // Arrange
        object input = false;

        // Act
        var result = Library.TryConvertToInt64Comparison(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0L, result.Value);
    }

    #endregion

    #region TryConvertToDecimalComparison Tests

    [TestMethod]
    public void TryConvertToDecimalComparison_WithByte_ShouldConvert()
    {
        // Arrange
        object input = (byte)100;

        // Act
        var result = Library.TryConvertToDecimalComparison(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(100m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_WithString_Valid_ShouldConvert()
    {
        // Arrange
        object input = "123,45";

        // Act
        var result = Library.TryConvertToDecimalComparison(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(123.45m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_WithBool_True_ShouldConvert()
    {
        // Arrange
        object input = true;

        // Act
        var result = Library.TryConvertToDecimalComparison(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_WithFloat_NaN_ShouldReturnNull()
    {
        // Arrange
        object input = float.NaN;

        // Act
        var result = Library.TryConvertToDecimalComparison(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_WithDouble_NaN_ShouldReturnNull()
    {
        // Arrange
        object input = double.NaN;

        // Act
        var result = Library.TryConvertToDecimalComparison(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_WithDouble_Infinity_ShouldReturnNull()
    {
        // Arrange
        object input = double.PositiveInfinity;

        // Act
        var result = Library.TryConvertToDecimalComparison(input);

        // Assert
        Assert.IsNull(result);
    }

    #endregion
}
