using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class NumericOnlyTypeConverterExtendedTests
{
    #region TryConvertToDecimal Tests

    [TestMethod]
    public void TryConvertToDecimal_Null_ReturnsNull()
    {
        decimal? result = _converter.TryConvertToDecimal(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimal_DecimalValue_ReturnsValue()
    {
        decimal? result = _converter.TryConvertToDecimal(123.456m);
        Assert.AreEqual(123.456m, result);
    }

    [TestMethod]
    public void TryConvertToDecimal_ByteValue_ReturnsConverted()
    {
        decimal? result = _converter.TryConvertToDecimal((byte)255);
        Assert.AreEqual(255m, result);
    }

    [TestMethod]
    public void TryConvertToDecimal_SByteValue_ReturnsConverted()
    {
        decimal? result = _converter.TryConvertToDecimal((sbyte)-128);
        Assert.AreEqual(-128m, result);
    }

    [TestMethod]
    public void TryConvertToDecimal_ShortValue_ReturnsConverted()
    {
        decimal? result = _converter.TryConvertToDecimal((short)12345);
        Assert.AreEqual(12345m, result);
    }

    [TestMethod]
    public void TryConvertToDecimal_UShortValue_ReturnsConverted()
    {
        decimal? result = _converter.TryConvertToDecimal((ushort)65535);
        Assert.AreEqual(65535m, result);
    }

    [TestMethod]
    public void TryConvertToDecimal_IntValue_ReturnsConverted()
    {
        decimal? result = _converter.TryConvertToDecimal(42);
        Assert.AreEqual(42m, result);
    }

    [TestMethod]
    public void TryConvertToDecimal_UIntValue_ReturnsConverted()
    {
        decimal? result = _converter.TryConvertToDecimal(3000000000);
        Assert.AreEqual(3000000000m, result);
    }

    [TestMethod]
    public void TryConvertToDecimal_LongValue_ReturnsConverted()
    {
        decimal? result = _converter.TryConvertToDecimal(9223372036854775807);
        Assert.AreEqual(9223372036854775807m, result);
    }

    [TestMethod]
    public void TryConvertToDecimal_ULongValue_ReturnsConverted()
    {
        decimal? result = _converter.TryConvertToDecimal(18446744073709551615);
        Assert.AreEqual(18446744073709551615m, result);
    }

    [TestMethod]
    public void TryConvertToDecimal_FloatValue_ReturnsConverted()
    {
        decimal? result = _converter.TryConvertToDecimal(42.5f);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimal_FloatNaN_ReturnsNull()
    {
        decimal? result = _converter.TryConvertToDecimal(float.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimal_FloatPositiveInfinity_ReturnsNull()
    {
        decimal? result = _converter.TryConvertToDecimal(float.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimal_FloatNegativeInfinity_ReturnsNull()
    {
        decimal? result = _converter.TryConvertToDecimal(float.NegativeInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimal_DoubleValue_ReturnsConverted()
    {
        decimal? result = _converter.TryConvertToDecimal(42.5);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimal_DoubleNaN_ReturnsNull()
    {
        decimal? result = _converter.TryConvertToDecimal(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimal_DoublePositiveInfinity_ReturnsNull()
    {
        decimal? result = _converter.TryConvertToDecimal(double.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimal_DoubleNegativeInfinity_ReturnsNull()
    {
        decimal? result = _converter.TryConvertToDecimal(double.NegativeInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimal_String_ReturnsNull()
    {
        decimal? result = _converter.TryConvertToDecimal("42.5");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimal_Object_ReturnsNull()
    {
        decimal? result = _converter.TryConvertToDecimal(new object());
        Assert.IsNull(result);
    }

    #endregion
}
