using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class NumericOnlyTypeConverterExtendedTests
{
    #region Edge Cases Tests

    [TestMethod]
    public void TryConvertToInt32_NegativeIntMaxValue_ReturnsValue()
    {
        int? result = _converter.TryConvertToInt32(int.MinValue);
        Assert.AreEqual(int.MinValue, result);
    }

    [TestMethod]
    public void TryConvertToInt32_PositiveIntMaxValue_ReturnsValue()
    {
        int? result = _converter.TryConvertToInt32(int.MaxValue);
        Assert.AreEqual(int.MaxValue, result);
    }

    [TestMethod]
    public void TryConvertToInt64_NegativeLongMinValue_ReturnsValue()
    {
        long? result = _converter.TryConvertToInt64(long.MinValue);
        Assert.AreEqual(long.MinValue, result);
    }

    [TestMethod]
    public void TryConvertToInt64_PositiveLongMaxValue_ReturnsValue()
    {
        long? result = _converter.TryConvertToInt64(long.MaxValue);
        Assert.AreEqual(long.MaxValue, result);
    }

    [TestMethod]
    public void TryConvertToDecimal_MaxValue_ReturnsValue()
    {
        decimal? result = _converter.TryConvertToDecimal(decimal.MaxValue);
        Assert.AreEqual(decimal.MaxValue, result);
    }

    [TestMethod]
    public void TryConvertToDecimal_MinValue_ReturnsValue()
    {
        decimal? result = _converter.TryConvertToDecimal(decimal.MinValue);
        Assert.AreEqual(decimal.MinValue, result);
    }

    [TestMethod]
    public void TryConvertToDouble_MaxValue_ReturnsValue()
    {
        double? result = _converter.TryConvertToDouble(double.MaxValue);
        Assert.AreEqual(double.MaxValue, result);
    }

    [TestMethod]
    public void TryConvertToDouble_MinValue_ReturnsValue()
    {
        double? result = _converter.TryConvertToDouble(double.MinValue);
        Assert.AreEqual(double.MinValue, result);
    }

    [TestMethod]
    public void TryConvertToInt32_ZeroFloat_ReturnsZero()
    {
        int? result = _converter.TryConvertToInt32(0.0f);
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void TryConvertToInt32_ZeroDouble_ReturnsZero()
    {
        int? result = _converter.TryConvertToInt32(0.0);
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void TryConvertToInt32_ZeroDecimal_ReturnsZero()
    {
        int? result = _converter.TryConvertToInt32(0m);
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void TryConvertToInt32_NegativeFloat_ReturnsConverted()
    {
        int? result = _converter.TryConvertToInt32(-42.0f);
        Assert.AreEqual(-42, result);
    }

    [TestMethod]
    public void TryConvertToInt32_NegativeDouble_ReturnsConverted()
    {
        int? result = _converter.TryConvertToInt32(-42.0);
        Assert.AreEqual(-42, result);
    }

    [TestMethod]
    public void TryConvertToInt32_NegativeDecimal_ReturnsConverted()
    {
        int? result = _converter.TryConvertToInt32(-42m);
        Assert.AreEqual(-42, result);
    }

    [TestMethod]
    public void TryConvertToInt64_NegativeFloat_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64(-42.0f);
        Assert.AreEqual(-42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_NegativeDouble_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64(-42.0);
        Assert.AreEqual(-42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_NegativeDecimal_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64(-42m);
        Assert.AreEqual(-42L, result);
    }

    [TestMethod]
    public void TryConvertToDecimal_NegativeValue_ReturnsConverted()
    {
        decimal? result = _converter.TryConvertToDecimal(-123.456m);
        Assert.AreEqual(-123.456m, result);
    }

    [TestMethod]
    public void TryConvertToDouble_NegativeValue_ReturnsConverted()
    {
        double? result = _converter.TryConvertToDouble(-42.5);
        Assert.AreEqual(-42.5, result);
    }

    #endregion
}
