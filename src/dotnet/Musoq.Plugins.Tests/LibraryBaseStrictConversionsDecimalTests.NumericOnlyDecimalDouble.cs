using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class LibraryBaseStrictConversionsDecimalTests
{
    #region TryConvertToInt64NumericOnly Tests

    [TestMethod]
    public void TryConvertToInt64NumericOnly_WithLong_ShouldReturnSame()
    {
        var result = Library.TryConvertToInt64NumericOnly(9223372036854775807L);

        Assert.IsNotNull(result);
        Assert.AreEqual(9223372036854775807L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_WithNull_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt64NumericOnly(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_WithString_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt64NumericOnly("123");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_WithULongOutOfRange_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt64NumericOnly(18446744073709551615UL);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_WithInt_ShouldConvert()
    {
        var result = Library.TryConvertToInt64NumericOnly(42);

        Assert.IsNotNull(result);
        Assert.AreEqual(42L, result.Value);
    }

    #endregion

    #region TryConvertToDecimalNumericOnly Tests

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_WithDecimal_ShouldReturnSame()
    {
        var result = Library.TryConvertToDecimalNumericOnly(123.456m);

        Assert.IsNotNull(result);
        Assert.AreEqual(123.456m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_WithNull_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalNumericOnly(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_WithString_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalNumericOnly("123.456");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_WithInt_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalNumericOnly(42);

        Assert.IsNotNull(result);
        Assert.AreEqual(42m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_WithDouble_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalNumericOnly(123.456);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_WithDoubleNaN_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalNumericOnly(double.NaN);

        Assert.IsNull(result);
    }

    #endregion

    #region TryConvertToDoubleNumericOnly Tests

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_WithDouble_ShouldReturnSame()
    {
        var result = Library.TryConvertToDoubleNumericOnly(123.456);

        Assert.IsNotNull(result);
        Assert.AreEqual(123.456, result.Value, 0.001);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_WithNull_ShouldReturnNull()
    {
        var result = Library.TryConvertToDoubleNumericOnly(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_WithString_ShouldReturnNull()
    {
        var result = Library.TryConvertToDoubleNumericOnly("123.456");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_WithInt_ShouldConvert()
    {
        var result = Library.TryConvertToDoubleNumericOnly(42);

        Assert.IsNotNull(result);
        Assert.AreEqual(42.0, result.Value);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_WithFloat_ShouldConvert()
    {
        var result = Library.TryConvertToDoubleNumericOnly(123.456f);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_WithDoubleNaN_ShouldReturnNull()
    {
        var result = Library.TryConvertToDoubleNumericOnly(double.NaN);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_WithDoubleInfinity_ShouldReturnNull()
    {
        var result = Library.TryConvertToDoubleNumericOnly(double.PositiveInfinity);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_WithFloatNaN_ShouldReturnNull()
    {
        var result = Library.TryConvertToDoubleNumericOnly(float.NaN);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_WithFloatInfinity_ShouldReturnNull()
    {
        var result = Library.TryConvertToDoubleNumericOnly(float.PositiveInfinity);

        Assert.IsNull(result);
    }

    #endregion
}
