using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Tests for edge cases in type conversion to increase coverage of:
///     - NumericOnlyTypeConverter (currently 45%)
///     - ComparisonTypeConverter (currently 65.3%)
/// </summary>
[TestClass]
public class TypeConversionEdgeCasesTests : LibraryBaseBaseTests
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
