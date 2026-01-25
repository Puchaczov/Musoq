using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for NumericOnlyTypeConverter to improve branch coverage.
///     Tests TryConvertToInt32, TryConvertToInt64, TryConvertToDecimal, TryConvertToDouble methods.
/// </summary>
[TestClass]
public class NumericOnlyTypeConverterExtendedTests
{
    private dynamic _converter;

    [TestInitialize]
    public void Setup()
    {
        var converterType =
            typeof(LibraryBase).Assembly.GetType("Musoq.Plugins.Lib.TypeConversion.NumericOnlyTypeConverter");
        Assert.IsNotNull(converterType, "NumericOnlyTypeConverter type should exist");
        _converter = Activator.CreateInstance(converterType);
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

    #region TryConvertToInt64 Tests

    [TestMethod]
    public void TryConvertToInt64_Null_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_LongValue_ReturnsValue()
    {
        long? result = _converter.TryConvertToInt64(9223372036854775807L);
        Assert.AreEqual(9223372036854775807L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_ByteValue_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64((byte)255);
        Assert.AreEqual(255L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_SByteValue_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64((sbyte)-128);
        Assert.AreEqual(-128L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_ShortValue_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64((short)12345);
        Assert.AreEqual(12345L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_UShortValue_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64((ushort)65535);
        Assert.AreEqual(65535L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_IntValue_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64(42);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_UIntValue_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64(3000000000);
        Assert.AreEqual(3000000000L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_ULongWithinRange_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64((ulong)1000);
        Assert.AreEqual(1000L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_ULongOverMaxValue_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(ulong.MaxValue);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_FloatExactInteger_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64(42.0f);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_FloatWithFraction_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(42.5f);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_FloatNaN_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(float.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_FloatPositiveInfinity_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(float.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_DoubleExactInteger_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64(42.0);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_DoubleWithFraction_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(42.5);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_DoubleNaN_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_DoublePositiveInfinity_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(double.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_DoubleNegativeInfinity_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(double.NegativeInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_DecimalExactInteger_ReturnsConverted()
    {
        long? result = _converter.TryConvertToInt64(42m);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64_DecimalWithFraction_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(42.5m);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_String_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64("42");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_Object_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64(new object());
        Assert.IsNull(result);
    }

    #endregion

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

    #region Char and Other Types Tests

    [TestMethod]
    public void TryConvertToInt32_CharValue_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32('A');
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64_CharValue_ReturnsNull()
    {
        long? result = _converter.TryConvertToInt64('A');
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimal_CharValue_ReturnsNull()
    {
        decimal? result = _converter.TryConvertToDecimal('A');
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDouble_CharValue_ReturnsNull()
    {
        double? result = _converter.TryConvertToDouble('A');
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32_GuidValue_ReturnsNull()
    {
        int? result = _converter.TryConvertToInt32(Guid.NewGuid());
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDouble_TimeSpan_ReturnsNull()
    {
        double? result = _converter.TryConvertToDouble(TimeSpan.FromDays(1));
        Assert.IsNull(result);
    }

    #endregion
}
