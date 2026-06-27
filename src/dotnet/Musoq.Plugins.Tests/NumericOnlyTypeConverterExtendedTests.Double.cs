using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class NumericOnlyTypeConverterExtendedTests
{
    #region TryConvertToDouble Tests

    [TestMethod]
    public void TryConvertToDouble_Null_ReturnsNull()
    {
        double? result = _converter.TryConvertToDouble(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDouble_DoubleValue_ReturnsValue()
    {
        double? result = _converter.TryConvertToDouble(42.5);
        Assert.AreEqual(42.5, result);
    }

    [TestMethod]
    public void TryConvertToDouble_DoubleNaN_ReturnsNull()
    {
        double? result = _converter.TryConvertToDouble(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDouble_DoublePositiveInfinity_ReturnsNull()
    {
        double? result = _converter.TryConvertToDouble(double.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDouble_DoubleNegativeInfinity_ReturnsNull()
    {
        double? result = _converter.TryConvertToDouble(double.NegativeInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDouble_FloatValue_ReturnsConverted()
    {
        double? result = _converter.TryConvertToDouble(42.5f);
        Assert.IsNotNull(result);
        Assert.AreEqual(42.5, result.Value, 0.001);
    }

    [TestMethod]
    public void TryConvertToDouble_FloatNaN_ReturnsNull()
    {
        double? result = _converter.TryConvertToDouble(float.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDouble_FloatPositiveInfinity_ReturnsNull()
    {
        double? result = _converter.TryConvertToDouble(float.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDouble_FloatNegativeInfinity_ReturnsNull()
    {
        double? result = _converter.TryConvertToDouble(float.NegativeInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDouble_IntValue_ReturnsConverted()
    {
        double? result = _converter.TryConvertToDouble(42);
        Assert.AreEqual(42.0, result);
    }

    [TestMethod]
    public void TryConvertToDouble_LongValue_ReturnsConverted()
    {
        double? result = _converter.TryConvertToDouble(42L);
        Assert.AreEqual(42.0, result);
    }

    [TestMethod]
    public void TryConvertToDouble_DecimalValue_ReturnsConverted()
    {
        double? result = _converter.TryConvertToDouble(42.5m);
        Assert.IsNotNull(result);
        Assert.AreEqual(42.5, result.Value, 0.001);
    }

    [TestMethod]
    public void TryConvertToDouble_ByteValue_ReturnsConverted()
    {
        double? result = _converter.TryConvertToDouble((byte)255);
        Assert.AreEqual(255.0, result);
    }

    [TestMethod]
    public void TryConvertToDouble_SByteValue_ReturnsConverted()
    {
        double? result = _converter.TryConvertToDouble((sbyte)-128);
        Assert.AreEqual(-128.0, result);
    }

    [TestMethod]
    public void TryConvertToDouble_ShortValue_ReturnsConverted()
    {
        double? result = _converter.TryConvertToDouble((short)12345);
        Assert.AreEqual(12345.0, result);
    }

    [TestMethod]
    public void TryConvertToDouble_UShortValue_ReturnsConverted()
    {
        double? result = _converter.TryConvertToDouble((ushort)65535);
        Assert.AreEqual(65535.0, result);
    }

    [TestMethod]
    public void TryConvertToDouble_UIntValue_ReturnsConverted()
    {
        double? result = _converter.TryConvertToDouble(3000000000);
        Assert.AreEqual(3000000000.0, result);
    }

    [TestMethod]
    public void TryConvertToDouble_ULongValue_ReturnsConverted()
    {
        double? result = _converter.TryConvertToDouble((ulong)1000);
        Assert.AreEqual(1000.0, result);
    }

    [TestMethod]
    public void TryConvertToDouble_String_ReturnsNull()
    {
        double? result = _converter.TryConvertToDouble("42.5");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDouble_Object_ReturnsNull()
    {
        double? result = _converter.TryConvertToDouble(new object());
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDouble_DateTime_ReturnsNull()
    {
        var date = new DateTime(2020, 1, 1);
        double? result = _converter.TryConvertToDouble(date);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDouble_BoolTrue_ReturnsOne()
    {
        double? result = _converter.TryConvertToDouble(true);
        Assert.AreEqual(1.0, result);
    }

    [TestMethod]
    public void TryConvertToDouble_BoolFalse_ReturnsZero()
    {
        double? result = _converter.TryConvertToDouble(false);
        Assert.AreEqual(0.0, result);
    }

    #endregion
}
