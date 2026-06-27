using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for NumericOnlyTypeConverter to improve branch coverage.
///     Tests TryConvertToInt32, TryConvertToInt64, TryConvertToDecimal, TryConvertToDouble methods.
/// </summary>
[TestClass]
public partial class NumericOnlyTypeConverterExtendedTests
{
    private dynamic _converter = null!;

    [TestInitialize]
    public void Setup()
    {
        var converterType =
            typeof(LibraryBase).Assembly.GetType("Musoq.Plugins.Lib.TypeConversion.NumericOnlyTypeConverter");
        Assert.IsNotNull(converterType, "NumericOnlyTypeConverter type should exist");
        _converter = Activator.CreateInstance(converterType)!;
    }

    #region TryConvertToInt32 Tests

    [TestMethod]
    public void TryConvertToInt32_Null_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_IntValue_ReturnsValue()
    {
        int? result = _converter.TryConvertToInt32(42);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void TryConvertToInt32_ByteValue_ReturnsConverted()
    {
        int? result = _converter.TryConvertToInt32((byte)255);
        Assert.AreEqual(255, result);
    }

    [TestMethod]
    public void TryConvertToInt32_SByteValue_ReturnsConverted()
    {
        int? result = _converter.TryConvertToInt32((sbyte)-128);
        Assert.AreEqual(-128, result);
    }

    [TestMethod]
    public void TryConvertToInt32_ShortValue_ReturnsConverted()
    {
        int? result = _converter.TryConvertToInt32((short)12345);
        Assert.AreEqual(12345, result);
    }

    [TestMethod]
    public void TryConvertToInt32_UShortValue_ReturnsConverted()
    {
        int? result = _converter.TryConvertToInt32((ushort)65535);
        Assert.AreEqual(65535, result);
    }

    [TestMethod]
    public void TryConvertToInt32_UIntWithinRange_ReturnsConverted()
    {
        int? result = _converter.TryConvertToInt32((uint)1000);
        Assert.AreEqual(1000, result);
    }

    [TestMethod]
    public void TryConvertToInt32_UIntOverMaxValue_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32(3000000000);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_LongWithinRange_ReturnsConverted()
    {
        int? result = _converter.TryConvertToInt32((long)12345678);
        Assert.AreEqual(12345678, result);
    }

    [TestMethod]
    public void TryConvertToInt32_LongOverMaxValue_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32((long)3000000000);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_LongUnderMinValue_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32(-3000000000);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_ULongWithinRange_ReturnsConverted()
    {
        int? result = _converter.TryConvertToInt32((ulong)1000);
        Assert.AreEqual(1000, result);
    }

    [TestMethod]
    public void TryConvertToInt32_ULongOverMaxValue_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32((ulong)3000000000);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_FloatExactInteger_ReturnsConverted()
    {
        int? result = _converter.TryConvertToInt32(42.0f);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void TryConvertToInt32_FloatWithFraction_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32(42.5f);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_FloatNaN_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32(float.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_FloatPositiveInfinity_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32(float.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_FloatNegativeInfinity_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32(float.NegativeInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_DoubleExactInteger_ReturnsConverted()
    {
        int? result = _converter.TryConvertToInt32(42.0);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void TryConvertToInt32_DoubleWithFraction_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32(42.5);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_DoubleNaN_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_DoublePositiveInfinity_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32(double.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_DoubleNegativeInfinity_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32(double.NegativeInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_DecimalExactInteger_ReturnsConverted()
    {
        int? result = _converter.TryConvertToInt32(42m);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void TryConvertToInt32_DecimalWithFraction_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32(42.5m);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_String_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32("42");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_Object_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32(new object());
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_DateTime_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32(DateTime.Now);
        Assert.IsNull(result);
    }

    #endregion
}
