using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class TypeConversionEdgeCasesTests
{
    #region TryConvertToInt64NumericOnly Tests

    [TestMethod]
    public void TryConvertToInt64NumericOnly_WithByte_ShouldConvert()
    {
        // Arrange
        object input = (byte)255;

        // Act
        var result = Library.TryConvertToInt64NumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(255L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_WithSByte_ShouldConvert()
    {
        // Arrange
        object input = (sbyte)-128;

        // Act
        var result = Library.TryConvertToInt64NumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(-128L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_WithULong_OutOfRange_ShouldReturnNull()
    {
        // Arrange - ulong larger than long.MaxValue
        object input = (ulong)long.MaxValue + 1;

        // Act
        var result = Library.TryConvertToInt64NumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_WithFloat_Exact_ShouldConvert()
    {
        // Arrange
        object input = 1000.0f;

        // Act
        var result = Library.TryConvertToInt64NumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1000L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_WithDouble_Exact_ShouldConvert()
    {
        // Arrange
        object input = 123456789.0;

        // Act
        var result = Library.TryConvertToInt64NumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(123456789L, result.Value);
    }

    #endregion

    #region TryConvertToDecimalNumericOnly Tests

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_WithByte_ShouldConvert()
    {
        // Arrange
        object input = (byte)100;

        // Act
        var result = Library.TryConvertToDecimalNumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(100m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_WithDouble_NaN_ShouldReturnNull()
    {
        // Arrange
        object input = double.NaN;

        // Act
        var result = Library.TryConvertToDecimalNumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_WithDouble_Infinity_ShouldReturnNull()
    {
        // Arrange
        object input = double.PositiveInfinity;

        // Act
        var result = Library.TryConvertToDecimalNumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region TryConvertToDoubleNumericOnly Tests

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_WithDouble_NaN_ShouldReturnNull()
    {
        // Arrange
        object input = double.NaN;

        // Act
        var result = Library.TryConvertToDoubleNumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_WithDouble_Infinity_ShouldReturnNull()
    {
        // Arrange
        object input = double.PositiveInfinity;

        // Act
        var result = Library.TryConvertToDoubleNumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_WithFloat_NaN_ShouldReturnNull()
    {
        // Arrange
        object input = float.NaN;

        // Act
        var result = Library.TryConvertToDoubleNumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_WithFloat_Infinity_ShouldReturnNull()
    {
        // Arrange
        object input = float.PositiveInfinity;

        // Act
        var result = Library.TryConvertToDoubleNumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_WithString_ShouldReturnNull()
    {
        // Arrange
        object input = "42.5";

        // Act
        var result = Library.TryConvertToDoubleNumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_WithInt_ShouldConvert()
    {
        // Arrange
        object input = 42;

        // Act
        var result = Library.TryConvertToDoubleNumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(42.0, result.Value);
    }

    #endregion

    #region TryConvertNumericOnly Tests

    [TestMethod]
    public void TryConvertNumericOnly_WithNull_ShouldReturnNull()
    {
        // Arrange
        object? input = null;

        // Act
        var result = Library.TryConvertNumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertNumericOnly_WithInt_ShouldReturnDecimal()
    {
        // Arrange
        object input = 42;

        // Act
        var result = Library.TryConvertNumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(42m, result.Value);
    }

    [TestMethod]
    public void TryConvertNumericOnly_WithLong_ShouldReturnDecimal()
    {
        // Arrange
        object input = 123456789012345L;

        // Act
        var result = Library.TryConvertNumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(123456789012345m, result.Value);
    }

    [TestMethod]
    public void TryConvertNumericOnly_WithDecimal_ShouldReturnDecimal()
    {
        // Arrange
        object input = 123.456m;

        // Act
        var result = Library.TryConvertNumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(123.456m, result.Value);
    }

    [TestMethod]
    public void TryConvertNumericOnly_WithString_ShouldReturnNull()
    {
        // Arrange - strings are rejected
        object input = "42";

        // Act
        var result = Library.TryConvertNumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    #endregion
}
