using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;

namespace Musoq.Plugins.Tests;

[TestClass]
public class LibraryBaseStrictConversionsDecimalTests : LibraryBaseBaseTests
{
    #region TryConvertToDecimalStrict Tests

    [TestMethod]
    public void TryConvertToDecimalStrict_WithStringDecimal_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict("100,50");
        
        Assert.IsNotNull(result, "Result should not be null");
        Assert.AreEqual(100.50m, result.Value, "Should parse 100,50 correctly");
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithStringDecimal_MatchesLiteral()
    {
        var result = Library.TryConvertToDecimalStrict("100,50");
        decimal literal = 100.50m;
        
        Assert.IsNotNull(result);
        Assert.AreEqual(literal, result.Value, "Parsed value should match literal");
        Assert.AreEqual(literal, result.Value, "Equality comparison should work");
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithStringInteger_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict("100");
        
        Assert.IsNotNull(result);
        Assert.AreEqual(100m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithNull_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalStrict(null);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithInt_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict(42);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(42m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithLong_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict(9223372036854775807L);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(9223372036854775807m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithDouble_WhenExact_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict(123.0);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(123m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithFloat_WhenExact_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict(42.0f);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(42m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithByte_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict((byte)255);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(255m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithSByte_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict((sbyte)-128);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(-128m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithShort_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict((short)12345);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(12345m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithUShort_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict((ushort)65535);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(65535m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithUInt_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict(4294967295U);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(4294967295m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithULong_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict(18446744073709551615UL);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(18446744073709551615m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithBool_True_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict(true);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(1m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithBool_False_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalStrict(false);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(0m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithDecimal_ShouldReturnSame()
    {
        var result = Library.TryConvertToDecimalStrict(123.456m);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(123.456m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithDoubleNaN_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalStrict(double.NaN);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithDoubleInfinity_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalStrict(double.PositiveInfinity);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithNegativeDoubleInfinity_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalStrict(double.NegativeInfinity);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithFloatNaN_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalStrict(float.NaN);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithFloatInfinity_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalStrict(float.PositiveInfinity);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_WithInvalidString_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalStrict("not a number");
        
        Assert.IsNull(result);
    }

    #endregion

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

    #region TryConvertToInt32Comparison Tests

    [TestMethod]
    public void TryConvertToInt32Comparison_WithDoubleWithFraction_ShouldRound()
    {
        var result = Library.TryConvertToInt32Comparison(42.7);
        
        Assert.IsNotNull(result);
        
        Assert.AreEqual(43, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithNull_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt32Comparison(null);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithInt_ShouldReturnSame()
    {
        var result = Library.TryConvertToInt32Comparison(42);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithLongOutOfRange_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt32Comparison(9223372036854775807L);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_WithString_ShouldConvert()
    {
        var result = Library.TryConvertToInt32Comparison("12345");
        
        Assert.IsNotNull(result);
        Assert.AreEqual(12345, result.Value);
    }

    #endregion

    #region TryConvertToInt64Comparison Tests

    [TestMethod]
    public void TryConvertToInt64Comparison_WithDoubleWithFraction_ShouldRound()
    {
        var result = Library.TryConvertToInt64Comparison(42.9);
        
        Assert.IsNotNull(result);
        
        Assert.AreEqual(43L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_WithNull_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt64Comparison(null);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_WithULongOutOfRange_ShouldReturnNull()
    {
        var result = Library.TryConvertToInt64Comparison(18446744073709551615UL);
        
        Assert.IsNull(result);
    }

    #endregion

    #region TryConvertToDecimalComparison Tests

    [TestMethod]
    public void TryConvertToDecimalComparison_WithDouble_ShouldConvert()
    {
        var result = Library.TryConvertToDecimalComparison(123.456);
        
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_WithNull_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalComparison(null);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_WithDoubleNaN_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalComparison(double.NaN);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_WithDoubleInfinity_ShouldReturnNull()
    {
        var result = Library.TryConvertToDecimalComparison(double.PositiveInfinity);
        
        Assert.IsNull(result);
    }

    #endregion

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

    #region Runtime Operator Tests

    [TestMethod]
    public void InternalApplyAddOperator_WithInts_ShouldAdd()
    {
        var result = Library.InternalApplyAddOperator(2, 3);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(5L, result);
    }

    [TestMethod]
    public void InternalApplyAddOperator_WithNulls_ShouldReturnNull()
    {
        var result = Library.InternalApplyAddOperator(null, null);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void InternalApplyAddOperator_WithOneNull_ShouldReturnNull()
    {
        var result = Library.InternalApplyAddOperator(2, null);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void InternalApplyAddOperator_WithDecimals_ShouldAdd()
    {
        var result = Library.InternalApplyAddOperator(2.5m, 3.5m);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(6.0m, result);
    }

    [TestMethod]
    public void InternalApplyAddOperator_WithDoubles_ShouldAdd()
    {
        var result = Library.InternalApplyAddOperator(2.5, 3.5);
        
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void InternalApplySubtractOperator_WithInts_ShouldSubtract()
    {
        var result = Library.InternalApplySubtractOperator(5, 3);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(2L, result);
    }

    [TestMethod]
    public void InternalApplySubtractOperator_WithNulls_ShouldReturnNull()
    {
        var result = Library.InternalApplySubtractOperator(null, null);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void InternalApplyMultiplyOperator_WithInts_ShouldMultiply()
    {
        var result = Library.InternalApplyMultiplyOperator(4, 5);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(20L, result);
    }

    [TestMethod]
    public void InternalApplyMultiplyOperator_WithNulls_ShouldReturnNull()
    {
        var result = Library.InternalApplyMultiplyOperator(null, null);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void InternalApplyDivideOperator_WithInts_ShouldDivide()
    {
        var result = Library.InternalApplyDivideOperator(10, 2);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(5L, result);
    }

    [TestMethod]
    public void InternalApplyDivideOperator_WithNulls_ShouldReturnNull()
    {
        var result = Library.InternalApplyDivideOperator(null, null);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void InternalApplyDivideOperator_DivisionByZero_ThrowsException()
    {
        
        bool exceptionThrown = false;
        try
        {
            Library.InternalApplyDivideOperator(10, 0);
        }
        catch (DivideByZeroException)
        {
            exceptionThrown = true;
        }
        
        Assert.IsTrue(exceptionThrown, "DivideByZeroException should be thrown");
    }

    [TestMethod]
    public void InternalApplyModuloOperator_WithInts_ShouldGetRemainder()
    {
        var result = Library.InternalApplyModuloOperator(10, 3);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(1L, result);
    }

    [TestMethod]
    public void InternalApplyModuloOperator_WithNulls_ShouldReturnNull()
    {
        var result = Library.InternalApplyModuloOperator(null, null);
        
        Assert.IsNull(result);
    }

    #endregion

    #region Comparison Operator Tests

    [TestMethod]
    public void InternalGreaterThanOperator_WhenGreater_ShouldReturnTrue()
    {
        var result = Library.InternalGreaterThanOperator(5, 3);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void InternalGreaterThanOperator_WhenLess_ShouldReturnFalse()
    {
        var result = Library.InternalGreaterThanOperator(3, 5);
        
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void InternalGreaterThanOperator_WhenEqual_ShouldReturnFalse()
    {
        var result = Library.InternalGreaterThanOperator(5, 5);
        
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void InternalGreaterThanOperator_WithNulls_ShouldReturnNull()
    {
        var result = Library.InternalGreaterThanOperator(null, null);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void InternalLessThanOperator_WhenLess_ShouldReturnTrue()
    {
        var result = Library.InternalLessThanOperator(3, 5);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void InternalLessThanOperator_WhenGreater_ShouldReturnFalse()
    {
        var result = Library.InternalLessThanOperator(5, 3);
        
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void InternalLessThanOperator_WithNulls_ShouldReturnNull()
    {
        var result = Library.InternalLessThanOperator(null, null);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void InternalGreaterThanOrEqualOperator_WhenGreater_ShouldReturnTrue()
    {
        var result = Library.InternalGreaterThanOrEqualOperator(5, 3);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void InternalGreaterThanOrEqualOperator_WhenEqual_ShouldReturnTrue()
    {
        var result = Library.InternalGreaterThanOrEqualOperator(5, 5);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void InternalGreaterThanOrEqualOperator_WhenLess_ShouldReturnFalse()
    {
        var result = Library.InternalGreaterThanOrEqualOperator(3, 5);
        
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void InternalLessThanOrEqualOperator_WhenLess_ShouldReturnTrue()
    {
        var result = Library.InternalLessThanOrEqualOperator(3, 5);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void InternalLessThanOrEqualOperator_WhenEqual_ShouldReturnTrue()
    {
        var result = Library.InternalLessThanOrEqualOperator(5, 5);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void InternalLessThanOrEqualOperator_WhenGreater_ShouldReturnFalse()
    {
        var result = Library.InternalLessThanOrEqualOperator(5, 3);
        
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void InternalEqualOperator_WhenEqual_ShouldReturnTrue()
    {
        var result = Library.InternalEqualOperator(5, 5);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void InternalEqualOperator_WhenNotEqual_ShouldReturnFalse()
    {
        var result = Library.InternalEqualOperator(5, 3);
        
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void InternalEqualOperator_WithNulls_ReturnsNotEqual()
    {
        var result = Library.InternalEqualOperator(null, null);
        
        
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void InternalNotEqualOperator_WhenNotEqual_ShouldReturnTrue()
    {
        var result = Library.InternalNotEqualOperator(5, 3);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void InternalNotEqualOperator_WhenEqual_ShouldReturnFalse()
    {
        var result = Library.InternalNotEqualOperator(5, 5);
        
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void InternalNotEqualOperator_WithNulls_ReturnsNotEqual()
    {
        var result = Library.InternalNotEqualOperator(null, null);
        
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    #endregion
}
