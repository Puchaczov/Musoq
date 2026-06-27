using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class TypeConversionEdgeCasesTests
{
    #region TryConvertToInt32NumericOnly Tests

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithByte_ShouldConvert()
    {
        // Arrange
        object input = (byte)42;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithSByte_ShouldConvert()
    {
        // Arrange
        object input = (sbyte)-42;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(-42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithShort_ShouldConvert()
    {
        // Arrange
        object input = (short)1234;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1234, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithUShort_ShouldConvert()
    {
        // Arrange
        object input = (ushort)65000;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(65000, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithUInt_InRange_ShouldConvert()
    {
        // Arrange
        object input = 100u;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(100, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithUInt_OutOfRange_ShouldReturnNull()
    {
        // Arrange - uint larger than int.MaxValue
        object input = (uint)int.MaxValue + 1;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithLong_InRange_ShouldConvert()
    {
        // Arrange
        object input = 12345L;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(12345, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithLong_OutOfRange_ShouldReturnNull()
    {
        // Arrange - long larger than int.MaxValue
        object input = (long)int.MaxValue + 1;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithULong_InRange_ShouldConvert()
    {
        // Arrange
        object input = 12345UL;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(12345, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithULong_OutOfRange_ShouldReturnNull()
    {
        // Arrange - ulong larger than int.MaxValue
        object input = (ulong)int.MaxValue + 1;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithFloat_Exact_ShouldConvert()
    {
        // Arrange - exact float value
        object input = 42.0f;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithFloat_NaN_ShouldReturnNull()
    {
        // Arrange
        object input = float.NaN;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithFloat_Infinity_ShouldReturnNull()
    {
        // Arrange
        object input = float.PositiveInfinity;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithFloat_NegativeInfinity_ShouldReturnNull()
    {
        // Arrange
        object input = float.NegativeInfinity;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithFloat_NotExact_ShouldReturnNull()
    {
        // Arrange - float with fraction that cannot be exactly represented
        object input = 42.5f;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithDouble_Exact_ShouldConvert()
    {
        // Arrange
        object input = 42.0;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithDouble_NaN_ShouldReturnNull()
    {
        // Arrange
        object input = double.NaN;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithDouble_Infinity_ShouldReturnNull()
    {
        // Arrange
        object input = double.PositiveInfinity;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithDouble_NotExact_ShouldReturnNull()
    {
        // Arrange
        object input = 42.5;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithDecimal_Exact_ShouldConvert()
    {
        // Arrange
        object input = 42m;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithDecimal_NotExact_ShouldReturnNull()
    {
        // Arrange
        object input = 42.5m;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithString_ShouldReturnNull()
    {
        // Arrange - strings are rejected by NumericOnly converter
        object input = "42";

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithNull_ShouldReturnNull()
    {
        // Arrange
        object? input = null;

        // Act
        var result = Library.TryConvertToInt32NumericOnly(input);

        // Assert
        Assert.IsNull(result);
    }

    #endregion
}
