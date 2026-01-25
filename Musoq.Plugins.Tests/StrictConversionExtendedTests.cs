using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for strict conversions and type conversion methods to improve branch coverage
/// </summary>
[TestClass]
public class StrictConversionExtendedTests : LibraryBaseBaseTests
{
    #region TryConvertToInt32Strict Tests

    [TestMethod]
    public void TryConvertToInt32Strict_Null_ReturnsNull()
    {
        Assert.IsNull(Library.TryConvertToInt32Strict(null));
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Int32_ReturnsSameValue()
    {
        var result = Library.TryConvertToInt32Strict(42);
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Byte_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt32Strict((byte)100);
        Assert.IsNotNull(result);
        Assert.AreEqual(100, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_SByte_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt32Strict((sbyte)-50);
        Assert.IsNotNull(result);
        Assert.AreEqual(-50, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Short_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt32Strict((short)1000);
        Assert.IsNotNull(result);
        Assert.AreEqual(1000, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_UShort_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt32Strict((ushort)65000);
        Assert.IsNotNull(result);
        Assert.AreEqual(65000, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Long_WithinRange_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt32Strict(1000L);
        Assert.IsNotNull(result);
        Assert.AreEqual(1000, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Long_OutOfRange_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(long.MaxValue);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_ULong_WithinRange_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt32Strict((ulong)500);
        Assert.IsNotNull(result);
        Assert.AreEqual(500, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_ULong_OutOfRange_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(ulong.MaxValue);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Float_ExactInteger_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt32Strict(42.0f);
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Float_WithFraction_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(42.5f);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Float_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(float.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Float_Infinity_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(float.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Double_ExactInteger_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt32Strict(42.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Double_WithFraction_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(42.7);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Double_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Double_NegativeInfinity_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(double.NegativeInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Decimal_ExactInteger_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt32Strict(42m);
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Decimal_WithFraction_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(42.5m);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_String_ValidNumber_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt32Strict("42");
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_String_InvalidNumber_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict("not a number");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Bool_True_ReturnsOne()
    {
        var result = Library.TryConvertToInt32Strict(true);
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Bool_False_ReturnsZero()
    {
        var result = Library.TryConvertToInt32Strict(false);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Value);
    }

    #endregion

    #region TryConvertToInt64Strict Tests

    [TestMethod]
    public void TryConvertToInt64Strict_Null_ReturnsNull()
    {
        Assert.IsNull(Library.TryConvertToInt64Strict(null));
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Int64_ReturnsSameValue()
    {
        var result = Library.TryConvertToInt64Strict(42L);
        Assert.IsNotNull(result);
        Assert.AreEqual(42L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Int32_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt64Strict(42);
        Assert.IsNotNull(result);
        Assert.AreEqual(42L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_ULong_WithinRange_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt64Strict((ulong)1000);
        Assert.IsNotNull(result);
        Assert.AreEqual(1000L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_ULong_GreaterThanLongMax_ReturnsNull()
    {
        var result = Library.TryConvertToInt64Strict((ulong)long.MaxValue + 1);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Double_ExactInteger_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt64Strict(42.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(42L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Double_WithFraction_ReturnsNull()
    {
        var result = Library.TryConvertToInt64Strict(42.9);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_String_ValidNumber_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt64Strict("9223372036854775807");
        Assert.IsNotNull(result);
        Assert.AreEqual(long.MaxValue, result.Value);
    }

    #endregion

    #region TryConvertToDecimalStrict Tests

    [TestMethod]
    public void TryConvertToDecimalStrict_Null_ReturnsNull()
    {
        Assert.IsNull(Library.TryConvertToDecimalStrict(null));
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Decimal_ReturnsSameValue()
    {
        var result = Library.TryConvertToDecimalStrict(42.5m);
        Assert.IsNotNull(result);
        Assert.AreEqual(42.5m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Int32_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToDecimalStrict(42);
        Assert.IsNotNull(result);
        Assert.AreEqual(42m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Double_ExactValue_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToDecimalStrict(42.5);
        Assert.IsNotNull(result);
        Assert.AreEqual(42.5m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Double_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalStrict(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_String_ValidNumber_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToDecimalStrict("123456");
        Assert.IsNotNull(result);
        Assert.AreEqual(123456m, result.Value);
    }

    #endregion

    #region TryConvertToInt32Comparison Tests

    [TestMethod]
    public void TryConvertToInt32Comparison_Null_ReturnsNull()
    {
        Assert.IsNull(Library.TryConvertToInt32Comparison(null));
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Int32_ReturnsSameValue()
    {
        var result = Library.TryConvertToInt32Comparison(42);
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Float_WithFraction_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt32Comparison(42.7f);
        Assert.IsNotNull(result);
        Assert.AreEqual(43, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Double_WithFraction_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt32Comparison(42.9);
        Assert.IsNotNull(result);
        Assert.AreEqual(43, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Double_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Comparison(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Long_OutOfRange_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Comparison(long.MaxValue);
        Assert.IsNull(result);
    }

    #endregion

    #region TryConvertToInt64Comparison Tests

    [TestMethod]
    public void TryConvertToInt64Comparison_Null_ReturnsNull()
    {
        Assert.IsNull(Library.TryConvertToInt64Comparison(null));
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_Int64_ReturnsSameValue()
    {
        var result = Library.TryConvertToInt64Comparison(42L);
        Assert.IsNotNull(result);
        Assert.AreEqual(42L, result.Value);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_Double_WithFraction_ReturnsConvertedValue()
    {
        var result = Library.TryConvertToInt64Comparison(42.9);
        Assert.IsNotNull(result);
        Assert.AreEqual(43L, result.Value);
    }

    #endregion

    #region TryConvertToDecimalComparison Tests

    [TestMethod]
    public void TryConvertToDecimalComparison_Null_ReturnsNull()
    {
        Assert.IsNull(Library.TryConvertToDecimalComparison(null));
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_Decimal_ReturnsSameValue()
    {
        var result = Library.TryConvertToDecimalComparison(42.5m);
        Assert.IsNotNull(result);
        Assert.AreEqual(42.5m, result.Value);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_Double_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalComparison(double.NaN);
        Assert.IsNull(result);
    }

    #endregion

    #region TryConvertNumericOnly Tests

    [TestMethod]
    public void TryConvertNumericOnly_Null_ReturnsNull()
    {
        Assert.IsNull(Library.TryConvertNumericOnly(null));
    }

    [TestMethod]
    public void TryConvertNumericOnly_Int32_ReturnsDecimal()
    {
        var result = Library.TryConvertNumericOnly(42);
        Assert.IsNotNull(result);
        Assert.AreEqual(42m, result.Value);
    }

    [TestMethod]
    public void TryConvertNumericOnly_Int64_ReturnsDecimal()
    {
        var result = Library.TryConvertNumericOnly(42L);
        Assert.IsNotNull(result);
        Assert.AreEqual(42m, result.Value);
    }

    [TestMethod]
    public void TryConvertNumericOnly_Decimal_ReturnsDecimal()
    {
        var result = Library.TryConvertNumericOnly(42.5m);
        Assert.IsNotNull(result);
        Assert.AreEqual(42.5m, result.Value);
    }

    [TestMethod]
    public void TryConvertNumericOnly_Double_ReturnsDecimal()
    {
        var result = Library.TryConvertNumericOnly(42.5);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TryConvertNumericOnly_String_ReturnsNull()
    {
        var result = Library.TryConvertNumericOnly("42");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertNumericOnly_Bool_ReturnsNull()
    {
        var result = Library.TryConvertNumericOnly(true);
        Assert.IsNull(result);
    }

    #endregion
}
