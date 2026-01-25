using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Tests for StrictTypeConverter through the LibraryBase strict conversion methods.
///     Tests cover all switch branches for precision-preserving type conversion.
/// </summary>
[TestClass]
public class StrictTypeConversionTests : LibraryBaseBaseTests
{
    #region TryConvertToInt32Strict Tests

    [TestMethod]
    public void TryConvertToInt32Strict_Null_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Int32_ReturnsSame()
    {
        var result = Library.TryConvertToInt32Strict(42);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Byte_Converts()
    {
        var result = Library.TryConvertToInt32Strict((byte)255);
        Assert.AreEqual(255, result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_SByte_Converts()
    {
        var result = Library.TryConvertToInt32Strict((sbyte)-128);
        Assert.AreEqual(-128, result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Short_Converts()
    {
        var result = Library.TryConvertToInt32Strict((short)32000);
        Assert.AreEqual(32000, result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_UShort_Converts()
    {
        var result = Library.TryConvertToInt32Strict((ushort)65535);
        Assert.AreEqual(65535, result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_UInt_WithinRange_Converts()
    {
        var result = Library.TryConvertToInt32Strict((uint)1000);
        Assert.AreEqual(1000, result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_UInt_ExceedsMaxValue_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict((uint)int.MaxValue + 1);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Long_WithinRange_Converts()
    {
        var result = Library.TryConvertToInt32Strict(1000L);
        Assert.AreEqual(1000, result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Long_ExceedsMaxValue_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict((long)int.MaxValue + 1);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Long_BelowMinValue_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict((long)int.MinValue - 1);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_ULong_WithinRange_Converts()
    {
        var result = Library.TryConvertToInt32Strict((ulong)1000);
        Assert.AreEqual(1000, result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_ULong_ExceedsMaxValue_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict((ulong)int.MaxValue + 1);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Float_Exact_Converts()
    {
        var result = Library.TryConvertToInt32Strict(42.0f);
        Assert.AreEqual(42, result);
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
    public void TryConvertToInt32Strict_Float_NegativeInfinity_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(float.NegativeInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Float_Fractional_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(42.5f);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Double_Exact_Converts()
    {
        var result = Library.TryConvertToInt32Strict(42.0);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Double_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Double_Infinity_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(double.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Double_NegativeInfinity_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(double.NegativeInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Double_Fractional_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(42.5);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Decimal_Exact_Converts()
    {
        var result = Library.TryConvertToInt32Strict(42m);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Decimal_Fractional_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict(42.5m);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_String_Valid_Converts()
    {
        var result = Library.TryConvertToInt32Strict("42");
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_String_Invalid_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict("not-a-number");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_String_Fractional_ReturnsNull()
    {
        var result = Library.TryConvertToInt32Strict("42.5");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Boolean_True_Returns1()
    {
        var result = Library.TryConvertToInt32Strict(true);
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void TryConvertToInt32Strict_Boolean_False_Returns0()
    {
        var result = Library.TryConvertToInt32Strict(false);
        Assert.AreEqual(0, result);
    }

    #endregion

    #region TryConvertToInt64Strict Tests

    [TestMethod]
    public void TryConvertToInt64Strict_Null_ReturnsNull()
    {
        var result = Library.TryConvertToInt64Strict(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Int64_ReturnsSame()
    {
        var result = Library.TryConvertToInt64Strict(42L);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Byte_Converts()
    {
        var result = Library.TryConvertToInt64Strict((byte)255);
        Assert.AreEqual(255L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_SByte_Converts()
    {
        var result = Library.TryConvertToInt64Strict((sbyte)-128);
        Assert.AreEqual(-128L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Short_Converts()
    {
        var result = Library.TryConvertToInt64Strict((short)32000);
        Assert.AreEqual(32000L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_UShort_Converts()
    {
        var result = Library.TryConvertToInt64Strict((ushort)65535);
        Assert.AreEqual(65535L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Int_Converts()
    {
        var result = Library.TryConvertToInt64Strict(1000);
        Assert.AreEqual(1000L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_UInt_Converts()
    {
        var result = Library.TryConvertToInt64Strict((uint)1000);
        Assert.AreEqual(1000L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_ULong_WithinRange_Converts()
    {
        var result = Library.TryConvertToInt64Strict((ulong)1000);
        Assert.AreEqual(1000L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_ULong_ExceedsMaxValue_ReturnsNull()
    {
        var result = Library.TryConvertToInt64Strict((ulong)long.MaxValue + 1);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Float_Exact_Converts()
    {
        var result = Library.TryConvertToInt64Strict(42.0f);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Float_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToInt64Strict(float.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Float_Infinity_ReturnsNull()
    {
        var result = Library.TryConvertToInt64Strict(float.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Double_Exact_Converts()
    {
        var result = Library.TryConvertToInt64Strict(42.0);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Double_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToInt64Strict(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Double_Infinity_ReturnsNull()
    {
        var result = Library.TryConvertToInt64Strict(double.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Double_Fractional_ReturnsNull()
    {
        var result = Library.TryConvertToInt64Strict(42.5);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Decimal_Exact_Converts()
    {
        var result = Library.TryConvertToInt64Strict(42m);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Decimal_Fractional_ReturnsNull()
    {
        var result = Library.TryConvertToInt64Strict(42.5m);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_String_Valid_Converts()
    {
        var result = Library.TryConvertToInt64Strict("42");
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_String_Invalid_ReturnsNull()
    {
        var result = Library.TryConvertToInt64Strict("not-a-number");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Boolean_True_Returns1()
    {
        var result = Library.TryConvertToInt64Strict(true);
        Assert.AreEqual(1L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Strict_Boolean_False_Returns0()
    {
        var result = Library.TryConvertToInt64Strict(false);
        Assert.AreEqual(0L, result);
    }

    #endregion

    #region TryConvertToDecimalStrict Tests

    [TestMethod]
    public void TryConvertToDecimalStrict_Null_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalStrict(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Decimal_ReturnsSame()
    {
        var result = Library.TryConvertToDecimalStrict(42.5m);
        Assert.AreEqual(42.5m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Byte_Converts()
    {
        var result = Library.TryConvertToDecimalStrict((byte)255);
        Assert.AreEqual(255m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_SByte_Converts()
    {
        var result = Library.TryConvertToDecimalStrict((sbyte)-128);
        Assert.AreEqual(-128m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Short_Converts()
    {
        var result = Library.TryConvertToDecimalStrict((short)32000);
        Assert.AreEqual(32000m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_UShort_Converts()
    {
        var result = Library.TryConvertToDecimalStrict((ushort)65535);
        Assert.AreEqual(65535m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Int_Converts()
    {
        var result = Library.TryConvertToDecimalStrict(1000);
        Assert.AreEqual(1000m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_UInt_Converts()
    {
        var result = Library.TryConvertToDecimalStrict((uint)1000);
        Assert.AreEqual(1000m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Long_Converts()
    {
        var result = Library.TryConvertToDecimalStrict(1000L);
        Assert.AreEqual(1000m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_ULong_Converts()
    {
        var result = Library.TryConvertToDecimalStrict((ulong)1000);
        Assert.AreEqual(1000m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Float_Valid_Converts()
    {
        var result = Library.TryConvertToDecimalStrict(3.14f);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Float_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalStrict(float.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Float_Infinity_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalStrict(float.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Float_NegativeInfinity_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalStrict(float.NegativeInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Double_Valid_Converts()
    {
        var result = Library.TryConvertToDecimalStrict(3.14159);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Double_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalStrict(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Double_Infinity_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalStrict(double.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Double_NegativeInfinity_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalStrict(double.NegativeInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_String_Valid_Converts()
    {
        var result = Library.TryConvertToDecimalStrict("42");
        Assert.AreEqual(42m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_String_Invalid_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalStrict("not-a-number");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Boolean_True_Returns1()
    {
        var result = Library.TryConvertToDecimalStrict(true);
        Assert.AreEqual(1m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalStrict_Boolean_False_Returns0()
    {
        var result = Library.TryConvertToDecimalStrict(false);
        Assert.AreEqual(0m, result);
    }

    #endregion
}
