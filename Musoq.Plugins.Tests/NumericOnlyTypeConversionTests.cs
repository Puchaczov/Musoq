using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Tests for NumericOnly type converters that reject strings and only accept boxed numeric types.
///     Used for arithmetic operations on System.Object columns.
/// </summary>
[TestClass]
public class NumericOnlyTypeConversionTests
{
    private readonly LibraryBase Library = new();

    #region TryConvertToInt32NumericOnly Tests

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Null_ReturnsNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Int_ReturnsSame()
    {
        var result = Library.TryConvertToInt32NumericOnly(42);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Byte_Converts()
    {
        var result = Library.TryConvertToInt32NumericOnly((byte)200);
        Assert.AreEqual(200, result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_SByte_Converts()
    {
        var result = Library.TryConvertToInt32NumericOnly((sbyte)-100);
        Assert.AreEqual(-100, result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Short_Converts()
    {
        var result = Library.TryConvertToInt32NumericOnly((short)30000);
        Assert.AreEqual(30000, result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_UShort_Converts()
    {
        var result = Library.TryConvertToInt32NumericOnly((ushort)60000);
        Assert.AreEqual(60000, result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_UInt_ValidRange_Converts()
    {
        var result = Library.TryConvertToInt32NumericOnly((uint)1000);
        Assert.AreEqual(1000, result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_UInt_OverMaxInt_ReturnsNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(3000000000);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Long_ValidRange_Converts()
    {
        var result = Library.TryConvertToInt32NumericOnly(1000L);
        Assert.AreEqual(1000, result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Long_OverMaxInt_ReturnsNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(10000000000L);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Long_UnderMinInt_ReturnsNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(-10000000000L);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_ULong_ValidRange_Converts()
    {
        var result = Library.TryConvertToInt32NumericOnly((ulong)1000);
        Assert.AreEqual(1000, result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_ULong_OverMaxInt_ReturnsNull()
    {
        var result = Library.TryConvertToInt32NumericOnly((ulong)10000000000);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Float_ExactInt_Converts()
    {
        var result = Library.TryConvertToInt32NumericOnly(42.0f);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Float_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(float.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Float_Infinity_ReturnsNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(float.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Float_NegativeInfinity_ReturnsNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(float.NegativeInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Float_Fractional_ReturnsNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(42.5f);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Double_ExactInt_Converts()
    {
        var result = Library.TryConvertToInt32NumericOnly(42.0);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Double_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Double_Infinity_ReturnsNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(double.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Double_Fractional_ReturnsNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(42.5);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Decimal_ExactInt_Converts()
    {
        var result = Library.TryConvertToInt32NumericOnly(42m);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Decimal_Fractional_ReturnsNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(42.5m);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_String_ReturnsNull()
    {
        var result = Library.TryConvertToInt32NumericOnly("42");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32NumericOnly_Boolean_ReturnsNull()
    {
        var result = Library.TryConvertToInt32NumericOnly(true);
        Assert.IsNull(result);
    }

    #endregion

    #region TryConvertToInt64NumericOnly Tests

    [TestMethod]
    public void TryConvertToInt64NumericOnly_Null_ReturnsNull()
    {
        var result = Library.TryConvertToInt64NumericOnly(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_Long_ReturnsSame()
    {
        var result = Library.TryConvertToInt64NumericOnly(42L);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_Byte_Converts()
    {
        var result = Library.TryConvertToInt64NumericOnly((byte)200);
        Assert.AreEqual(200L, result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_SByte_Converts()
    {
        var result = Library.TryConvertToInt64NumericOnly((sbyte)-100);
        Assert.AreEqual(-100L, result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_Short_Converts()
    {
        var result = Library.TryConvertToInt64NumericOnly((short)30000);
        Assert.AreEqual(30000L, result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_UShort_Converts()
    {
        var result = Library.TryConvertToInt64NumericOnly((ushort)60000);
        Assert.AreEqual(60000L, result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_Int_Converts()
    {
        var result = Library.TryConvertToInt64NumericOnly(42);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_UInt_Converts()
    {
        var result = Library.TryConvertToInt64NumericOnly(3000000000);
        Assert.AreEqual(3000000000L, result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_ULong_ValidRange_Converts()
    {
        var result = Library.TryConvertToInt64NumericOnly((ulong)1000);
        Assert.AreEqual(1000L, result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_ULong_OverMaxLong_ReturnsNull()
    {
        var result = Library.TryConvertToInt64NumericOnly(ulong.MaxValue);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_Float_ExactLong_Converts()
    {
        var result = Library.TryConvertToInt64NumericOnly(42.0f);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_Float_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToInt64NumericOnly(float.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_Float_Infinity_ReturnsNull()
    {
        var result = Library.TryConvertToInt64NumericOnly(float.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_Float_Fractional_ReturnsNull()
    {
        var result = Library.TryConvertToInt64NumericOnly(42.5f);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_Double_ExactLong_Converts()
    {
        var result = Library.TryConvertToInt64NumericOnly(42.0);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_Double_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToInt64NumericOnly(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_Double_Infinity_ReturnsNull()
    {
        var result = Library.TryConvertToInt64NumericOnly(double.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_Double_Fractional_ReturnsNull()
    {
        var result = Library.TryConvertToInt64NumericOnly(42.5);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_Decimal_ExactLong_Converts()
    {
        var result = Library.TryConvertToInt64NumericOnly(42m);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_Decimal_Fractional_ReturnsNull()
    {
        var result = Library.TryConvertToInt64NumericOnly(42.5m);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_String_ReturnsNull()
    {
        var result = Library.TryConvertToInt64NumericOnly("42");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64NumericOnly_Boolean_ReturnsNull()
    {
        var result = Library.TryConvertToInt64NumericOnly(true);
        Assert.IsNull(result);
    }

    #endregion

    #region TryConvertToDecimalNumericOnly Tests

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_Null_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalNumericOnly(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_Decimal_ReturnsSame()
    {
        var result = Library.TryConvertToDecimalNumericOnly(42.5m);
        Assert.AreEqual(42.5m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_Byte_Converts()
    {
        var result = Library.TryConvertToDecimalNumericOnly((byte)200);
        Assert.AreEqual(200m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_SByte_Converts()
    {
        var result = Library.TryConvertToDecimalNumericOnly((sbyte)-100);
        Assert.AreEqual(-100m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_Short_Converts()
    {
        var result = Library.TryConvertToDecimalNumericOnly((short)30000);
        Assert.AreEqual(30000m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_UShort_Converts()
    {
        var result = Library.TryConvertToDecimalNumericOnly((ushort)60000);
        Assert.AreEqual(60000m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_Int_Converts()
    {
        var result = Library.TryConvertToDecimalNumericOnly(42);
        Assert.AreEqual(42m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_UInt_Converts()
    {
        var result = Library.TryConvertToDecimalNumericOnly(3000000000);
        Assert.AreEqual(3000000000m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_Long_Converts()
    {
        var result = Library.TryConvertToDecimalNumericOnly(9000000000L);
        Assert.AreEqual(9000000000m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_ULong_Converts()
    {
        var result = Library.TryConvertToDecimalNumericOnly((ulong)9000000000);
        Assert.AreEqual(9000000000m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_Float_Converts()
    {
        var result = Library.TryConvertToDecimalNumericOnly(42.5f);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_Float_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalNumericOnly(float.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_Float_Infinity_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalNumericOnly(float.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_Double_Converts()
    {
        var result = Library.TryConvertToDecimalNumericOnly(42.5);
        Assert.AreEqual(42.5m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_Double_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalNumericOnly(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_Double_Infinity_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalNumericOnly(double.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_String_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalNumericOnly("42");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalNumericOnly_Boolean_ReturnsNull()
    {
        var result = Library.TryConvertToDecimalNumericOnly(true);
        Assert.IsNull(result);
    }

    #endregion

    #region TryConvertToDoubleNumericOnly Tests

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_Null_ReturnsNull()
    {
        var result = Library.TryConvertToDoubleNumericOnly(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_Double_ReturnsSame()
    {
        var result = Library.TryConvertToDoubleNumericOnly(42.5);
        Assert.AreEqual(42.5, result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_Double_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToDoubleNumericOnly(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_Double_Infinity_ReturnsNull()
    {
        var result = Library.TryConvertToDoubleNumericOnly(double.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_Double_NegativeInfinity_ReturnsNull()
    {
        var result = Library.TryConvertToDoubleNumericOnly(double.NegativeInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_Float_Valid_Converts()
    {
        var result = Library.TryConvertToDoubleNumericOnly(42.5f);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_Float_NaN_ReturnsNull()
    {
        var result = Library.TryConvertToDoubleNumericOnly(float.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_Float_Infinity_ReturnsNull()
    {
        var result = Library.TryConvertToDoubleNumericOnly(float.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_Int_Converts()
    {
        var result = Library.TryConvertToDoubleNumericOnly(42);
        Assert.AreEqual(42.0, result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_Long_Converts()
    {
        var result = Library.TryConvertToDoubleNumericOnly(9000000000L);
        Assert.AreEqual(9000000000.0, result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_Decimal_Converts()
    {
        var result = Library.TryConvertToDoubleNumericOnly(42.5m);
        Assert.AreEqual(42.5, result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_String_ReturnsNull()
    {
        var result = Library.TryConvertToDoubleNumericOnly("42");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDoubleNumericOnly_Boolean_Converts()
    {
        var result = Library.TryConvertToDoubleNumericOnly(true);
        Assert.AreEqual(1.0, result);
    }

    #endregion

    #region TryConvertNumericOnly (Smart Conversion) Tests

    [TestMethod]
    public void TryConvertNumericOnly_Null_ReturnsNull()
    {
        var result = Library.TryConvertNumericOnly(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertNumericOnly_Int_Converts()
    {
        var result = Library.TryConvertNumericOnly(42);
        Assert.AreEqual(42m, result);
    }

    [TestMethod]
    public void TryConvertNumericOnly_Long_Converts()
    {
        var result = Library.TryConvertNumericOnly(9000000000L);
        Assert.AreEqual(9000000000m, result);
    }

    [TestMethod]
    public void TryConvertNumericOnly_Decimal_Converts()
    {
        var result = Library.TryConvertNumericOnly(42.5m);
        Assert.AreEqual(42.5m, result);
    }

    [TestMethod]
    public void TryConvertNumericOnly_Float_Converts()
    {
        var result = Library.TryConvertNumericOnly(42.5f);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TryConvertNumericOnly_Double_Converts()
    {
        var result = Library.TryConvertNumericOnly(42.5);
        Assert.AreEqual(42.5m, result);
    }

    [TestMethod]
    public void TryConvertNumericOnly_String_ReturnsNull()
    {
        var result = Library.TryConvertNumericOnly("42");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertNumericOnly_ULong_Large_Converts()
    {
        var result = Library.TryConvertNumericOnly(10000000000000000000);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TryConvertNumericOnly_LargeDouble_Converts()
    {
        var result = Library.TryConvertNumericOnly(1e20);
        Assert.IsNotNull(result);
    }

    #endregion
}
