using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class LibraryBaseStrictConversionsDecimalTests
{
    #region TryConvertNumericOnly Tests

    [TestMethod]
    public void TryConvertNumericOnly_WithInt_ShouldConvert()
    {
        var result = Library.TryConvertNumericOnly(42);

        Assert.IsNotNull(result);
        Assert.AreEqual(42m, result.Value);
    }

    [TestMethod]
    public void TryConvertNumericOnly_WithNull_ShouldReturnNull()
    {
        var result = Library.TryConvertNumericOnly(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertNumericOnly_WithString_ShouldReturnNull()
    {
        var result = Library.TryConvertNumericOnly("123");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertNumericOnly_WithDecimal_ShouldReturnSame()
    {
        var result = Library.TryConvertNumericOnly(123.456m);

        Assert.IsNotNull(result);
        Assert.AreEqual(123.456m, result.Value);
    }

    [TestMethod]
    public void TryConvertNumericOnly_WithDouble_ShouldConvert()
    {
        var result = Library.TryConvertNumericOnly(123.456);

        Assert.IsNotNull(result);
    }

    #endregion

    #region TryConvertToInt32NumericOnly Tests

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithInt_ShouldReturnSame()
    {
        var result = Library.TryConvertToInt32NumericOnly(42);

        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithNull_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithString_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt32NumericOnly("123");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithByte_ShouldConvert()
    {
        var result = Library.TryConvertToInt32NumericOnly((byte)255);

        Assert.IsNotNull(result);
        Assert.AreEqual(255, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithSByte_ShouldConvert()
    {
        var result = Library.TryConvertToInt32NumericOnly((sbyte)-128);

        Assert.IsNotNull(result);
        Assert.AreEqual(-128, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithShort_ShouldConvert()
    {
        var result = Library.TryConvertToInt32NumericOnly((short)12345);

        Assert.IsNotNull(result);
        Assert.AreEqual(12345, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithUShort_ShouldConvert()
    {
        var result = Library.TryConvertToInt32NumericOnly((ushort)65535);

        Assert.IsNotNull(result);
        Assert.AreEqual(65535, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithLongOutOfRange_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(9223372036854775807L);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithUIntOutOfRange_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(4294967295U);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithDoubleNaN_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(double.NaN);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_WithFloatInfinity_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(float.PositiveInfinity);

        Assert.IsNull(result);
    }

    #endregion
}
