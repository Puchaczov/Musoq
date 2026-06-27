using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class LibraryBaseStrictConversionsDecimalTests
{
    #region TryConvertToInt32Strict Tests

    [TestMethod]
    public void TryConvertToInt32Strict_WithInt_ShouldReturnSame()
    {
        var result = Library.TryConvertToInt32Strict(42);

        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_WithNull_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt32Strict(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_WithString_ShouldConvert()
    {
        var result = Library.TryConvertToInt32Strict("12345");

        Assert.IsNotNull(result);
        Assert.AreEqual(12345, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_WithNegativeInt_ShouldConvert()
    {
        var result = Library.TryConvertToInt32Strict(-2147483648);

        Assert.IsNotNull(result);
        Assert.AreEqual(-2147483648, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_WithMaxInt_ShouldConvert()
    {
        var result = Library.TryConvertToInt32Strict(int.MaxValue);

        Assert.IsNotNull(result);
        Assert.AreEqual(int.MaxValue, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_WithLongInRange_ShouldConvert()
    {
        var result = Library.TryConvertToInt32Strict(12345L);

        Assert.IsNotNull(result);
        Assert.AreEqual(12345, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_WithLongOutOfRange_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt32Strict(9223372036854775807L);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_WithDoubleWithFraction_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt32Strict(42.5);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_WithDoubleExact_ShouldConvert()
    {
        var result = Library.TryConvertToInt32Strict(42.0);

        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_WithByte_ShouldConvert()
    {
        var result = Library.TryConvertToInt32Strict((byte)255);

        Assert.IsNotNull(result);
        Assert.AreEqual(255, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_WithBool_ShouldConvert()
    {
        var resultTrue = Library.TryConvertToInt32Strict(true);
        var resultFalse = Library.TryConvertToInt32Strict(false);

        Assert.IsNotNull(resultTrue);
        Assert.AreEqual(1, resultTrue.Value);
        Assert.IsNotNull(resultFalse);
        Assert.AreEqual(0, resultFalse.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_WithInvalidString_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt32Strict("abc");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_WithDoubleNaN_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt32Strict(double.NaN);

        Assert.IsNull(result);
    }

    #endregion

    #region TryConvertToInt64Strict Tests

    [TestMethod]
    public void TryConvertToInt64Strict_WithLong_ShouldReturnSame()
    {
        var result = Library.TryConvertToInt64Strict(9223372036854775807L);

        Assert.IsNotNull(result);
        Assert.AreEqual(9223372036854775807L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_WithNull_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt64Strict(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_WithInt_ShouldConvert()
    {
        var result = Library.TryConvertToInt64Strict(42);

        Assert.IsNotNull(result);
        Assert.AreEqual(42L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_WithString_ShouldConvert()
    {
        var result = Library.TryConvertToInt64Strict("9223372036854775807");

        Assert.IsNotNull(result);
        Assert.AreEqual(9223372036854775807L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_WithMinLong_ShouldConvert()
    {
        var result = Library.TryConvertToInt64Strict(long.MinValue);

        Assert.IsNotNull(result);
        Assert.AreEqual(long.MinValue, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_WithULongOutOfRange_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt64Strict(18446744073709551615UL);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_WithDoubleWithFraction_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt64Strict(42.5);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_WithDoubleExact_ShouldConvert()
    {
        var result = Library.TryConvertToInt64Strict(42.0);

        Assert.IsNotNull(result);
        Assert.AreEqual(42L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_WithInvalidString_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt64Strict("not a number");

        Assert.IsNull(result);
    }

    #endregion
}
