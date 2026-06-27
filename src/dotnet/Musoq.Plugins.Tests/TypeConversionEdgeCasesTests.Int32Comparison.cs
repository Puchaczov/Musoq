using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class TypeConversionEdgeCasesTests
{
    #region TryConvertToInt32Comparison Tests

    [TestMethod]
    public void TryConvertToInt32Comparison_WithByte_ShouldConvert()
    {
        // Arrange
        object input = (byte)42;

        // Act
        var result = Library.TryConvertToInt32Comparison(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithSByte_ShouldConvert()
    {
        // Arrange
        object input = (sbyte)-42;

        // Act
        var result = Library.TryConvertToInt32Comparison(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(-42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithUInt_OutOfRange_ShouldReturnNull()
    {
        // Arrange
        object input = (uint)int.MaxValue + 1;

        // Act
        var result = Library.TryConvertToInt32Comparison(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithULong_OutOfRange_ShouldReturnNull()
    {
        // Arrange
        object input = (ulong)int.MaxValue + 1;

        // Act
        var result = Library.TryConvertToInt32Comparison(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithFloat_OutOfRange_ShouldReturnNull()
    {
        // Arrange
        object input = int.MaxValue + 10000.0f;

        // Act
        var result = Library.TryConvertToInt32Comparison(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithDouble_OutOfRange_ShouldReturnNull()
    {
        // Arrange
        object input = (double)int.MaxValue + 1;

        // Act
        var result = Library.TryConvertToInt32Comparison(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithDecimal_OutOfRange_ShouldReturnNull()
    {
        // Arrange
        object input = (decimal)int.MaxValue + 1;

        // Act
        var result = Library.TryConvertToInt32Comparison(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithString_Valid_ShouldConvert()
    {
        // Arrange - ComparisonConverter allows string parsing
        object input = "42";

        // Act
        var result = Library.TryConvertToInt32Comparison(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithString_Invalid_ShouldReturnNull()
    {
        // Arrange
        object input = "not a number";

        // Act
        var result = Library.TryConvertToInt32Comparison(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithBool_True_ShouldConvert()
    {
        // Arrange
        object input = true;

        // Act
        var result = Library.TryConvertToInt32Comparison(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithBool_False_ShouldConvert()
    {
        // Arrange
        object input = false;

        // Act
        var result = Library.TryConvertToInt32Comparison(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithFloat_NaN_ShouldReturnNull()
    {
        // Arrange
        object input = float.NaN;

        // Act
        var result = Library.TryConvertToInt32Comparison(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithFloat_Infinity_ShouldReturnNull()
    {
        // Arrange
        object input = float.PositiveInfinity;

        // Act
        var result = Library.TryConvertToInt32Comparison(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithDouble_NaN_ShouldReturnNull()
    {
        // Arrange
        object input = double.NaN;

        // Act
        var result = Library.TryConvertToInt32Comparison(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithDouble_Infinity_ShouldReturnNull()
    {
        // Arrange
        object input = double.PositiveInfinity;

        // Act
        var result = Library.TryConvertToInt32Comparison(input);

        // Assert
        Assert.IsNull(result);
    }

    #endregion
}
