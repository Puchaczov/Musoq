using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Tests for Comparison type converters that allow lossy conversions (e.g., 3.7 becomes 3).
///     Used for comparison operations where approximate values are acceptable.
/// </summary>
[TestClass]
public class ComparisonTypeConversionTests
{
    private readonly LibraryBase _library = new();

    #region TryConvertToInt32Comparison Tests

    [TestMethod]
    public void TryConvertToInt32Comparison_Null_ReturnsNull()
    {
        var result = _library.TryConvertToInt32Comparison(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Int_ReturnsSame()
    {
        var result = _library.TryConvertToInt32Comparison(42);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Byte_Converts()
    {
        var result = _library.TryConvertToInt32Comparison((byte)200);
        Assert.AreEqual(200, result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_SByte_Converts()
    {
        var result = _library.TryConvertToInt32Comparison((sbyte)-100);
        Assert.AreEqual(-100, result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Short_Converts()
    {
        var result = _library.TryConvertToInt32Comparison((short)30000);
        Assert.AreEqual(30000, result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_UShort_Converts()
    {
        var result = _library.TryConvertToInt32Comparison((ushort)60000);
        Assert.AreEqual(60000, result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_UInt_ValidRange_Converts()
    {
        var result = _library.TryConvertToInt32Comparison((uint)1000);
        Assert.AreEqual(1000, result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_UInt_OverMaxInt_ReturnsNull()
    {
        var result = _library.TryConvertToInt32Comparison(3000000000);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Long_ValidRange_Converts()
    {
        var result = _library.TryConvertToInt32Comparison(1000L);
        Assert.AreEqual(1000, result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Long_OverMaxInt_ReturnsNull()
    {
        var result = _library.TryConvertToInt32Comparison(10000000000L);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Long_UnderMinInt_ReturnsNull()
    {
        var result = _library.TryConvertToInt32Comparison(-10000000000L);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_ULong_ValidRange_Converts()
    {
        var result = _library.TryConvertToInt32Comparison((ulong)1000);
        Assert.AreEqual(1000, result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_ULong_OverMaxInt_ReturnsNull()
    {
        var result = _library.TryConvertToInt32Comparison((ulong)10000000000);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Float_Valid_Converts()
    {
        var result = _library.TryConvertToInt32Comparison(42.7f);
        Assert.AreEqual(43, result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Float_NaN_ReturnsNull()
    {
        var result = _library.TryConvertToInt32Comparison(float.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Float_Infinity_ReturnsNull()
    {
        var result = _library.TryConvertToInt32Comparison(float.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Float_NegativeInfinity_ReturnsNull()
    {
        var result = _library.TryConvertToInt32Comparison(float.NegativeInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Double_Valid_Converts()
    {
        var result = _library.TryConvertToInt32Comparison(42.9);
        Assert.AreEqual(43, result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Double_NaN_ReturnsNull()
    {
        var result = _library.TryConvertToInt32Comparison(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Double_Infinity_ReturnsNull()
    {
        var result = _library.TryConvertToInt32Comparison(double.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Decimal_Valid_Converts()
    {
        var result = _library.TryConvertToInt32Comparison(42.5m);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_String_Valid_Converts()
    {
        var result = _library.TryConvertToInt32Comparison("42");
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_String_Invalid_ReturnsNull()
    {
        var result = _library.TryConvertToInt32Comparison("not-a-number");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Boolean_True_Returns1()
    {
        var result = _library.TryConvertToInt32Comparison(true);
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void TryConvertToInt32Comparison_Boolean_False_Returns0()
    {
        var result = _library.TryConvertToInt32Comparison(false);
        Assert.AreEqual(0, result);
    }

    #endregion

    #region TryConvertToInt64Comparison Tests

    [TestMethod]
    public void TryConvertToInt64Comparison_Null_ReturnsNull()
    {
        var result = _library.TryConvertToInt64Comparison(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_Long_ReturnsSame()
    {
        var result = _library.TryConvertToInt64Comparison(42L);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_Byte_Converts()
    {
        var result = _library.TryConvertToInt64Comparison((byte)200);
        Assert.AreEqual(200L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_SByte_Converts()
    {
        var result = _library.TryConvertToInt64Comparison((sbyte)-100);
        Assert.AreEqual(-100L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_Short_Converts()
    {
        var result = _library.TryConvertToInt64Comparison((short)30000);
        Assert.AreEqual(30000L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_UShort_Converts()
    {
        var result = _library.TryConvertToInt64Comparison((ushort)60000);
        Assert.AreEqual(60000L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_Int_Converts()
    {
        var result = _library.TryConvertToInt64Comparison(42);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_UInt_Converts()
    {
        var result = _library.TryConvertToInt64Comparison(3000000000);
        Assert.AreEqual(3000000000L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_ULong_ValidRange_Converts()
    {
        var result = _library.TryConvertToInt64Comparison((ulong)1000);
        Assert.AreEqual(1000L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_ULong_OverMaxLong_ReturnsNull()
    {
        var result = _library.TryConvertToInt64Comparison(ulong.MaxValue);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_Float_Valid_Converts()
    {
        var result = _library.TryConvertToInt64Comparison(42.7f);
        Assert.AreEqual(43L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_Float_NaN_ReturnsNull()
    {
        var result = _library.TryConvertToInt64Comparison(float.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_Float_Infinity_ReturnsNull()
    {
        var result = _library.TryConvertToInt64Comparison(float.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_Double_Valid_Converts()
    {
        var result = _library.TryConvertToInt64Comparison(42.9);
        Assert.AreEqual(43L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_Double_NaN_ReturnsNull()
    {
        var result = _library.TryConvertToInt64Comparison(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_Double_Infinity_ReturnsNull()
    {
        var result = _library.TryConvertToInt64Comparison(double.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_Decimal_Valid_Converts()
    {
        var result = _library.TryConvertToInt64Comparison(42.5m);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_String_Valid_Converts()
    {
        var result = _library.TryConvertToInt64Comparison("42");
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_String_Invalid_ReturnsNull()
    {
        var result = _library.TryConvertToInt64Comparison("not-a-number");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_Boolean_True_Returns1()
    {
        var result = _library.TryConvertToInt64Comparison(true);
        Assert.AreEqual(1L, result);
    }

    [TestMethod]
    public void TryConvertToInt64Comparison_Boolean_False_Returns0()
    {
        var result = _library.TryConvertToInt64Comparison(false);
        Assert.AreEqual(0L, result);
    }

    #endregion

    #region TryConvertToDecimalComparison Tests

    [TestMethod]
    public void TryConvertToDecimalComparison_Null_ReturnsNull()
    {
        var result = _library.TryConvertToDecimalComparison(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_Decimal_ReturnsSame()
    {
        var result = _library.TryConvertToDecimalComparison(42.5m);
        Assert.AreEqual(42.5m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_Byte_Converts()
    {
        var result = _library.TryConvertToDecimalComparison((byte)200);
        Assert.AreEqual(200m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_SByte_Converts()
    {
        var result = _library.TryConvertToDecimalComparison((sbyte)-100);
        Assert.AreEqual(-100m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_Short_Converts()
    {
        var result = _library.TryConvertToDecimalComparison((short)30000);
        Assert.AreEqual(30000m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_UShort_Converts()
    {
        var result = _library.TryConvertToDecimalComparison((ushort)60000);
        Assert.AreEqual(60000m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_Int_Converts()
    {
        var result = _library.TryConvertToDecimalComparison(42);
        Assert.AreEqual(42m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_UInt_Converts()
    {
        var result = _library.TryConvertToDecimalComparison(3000000000);
        Assert.AreEqual(3000000000m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_Long_Converts()
    {
        var result = _library.TryConvertToDecimalComparison(9000000000L);
        Assert.AreEqual(9000000000m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_ULong_Converts()
    {
        var result = _library.TryConvertToDecimalComparison((ulong)9000000000);
        Assert.AreEqual(9000000000m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_Float_Valid_Converts()
    {
        var result = _library.TryConvertToDecimalComparison(42.5f);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_Float_NaN_ReturnsNull()
    {
        var result = _library.TryConvertToDecimalComparison(float.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_Float_Infinity_ReturnsNull()
    {
        var result = _library.TryConvertToDecimalComparison(float.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_Double_Valid_Converts()
    {
        var result = _library.TryConvertToDecimalComparison(42.5);
        Assert.AreEqual(42.5m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_Double_NaN_ReturnsNull()
    {
        var result = _library.TryConvertToDecimalComparison(double.NaN);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_Double_Infinity_ReturnsNull()
    {
        var result = _library.TryConvertToDecimalComparison(double.PositiveInfinity);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_String_Valid_Converts()
    {
        var result = _library.TryConvertToDecimalComparison("42");
        Assert.AreEqual(42m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_String_Invalid_ReturnsNull()
    {
        var result = _library.TryConvertToDecimalComparison("not-a-number");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_Boolean_True_Returns1()
    {
        var result = _library.TryConvertToDecimalComparison(true);
        Assert.AreEqual(1m, result);
    }

    [TestMethod]
    public void TryConvertToDecimalComparison_Boolean_False_Returns0()
    {
        var result = _library.TryConvertToDecimalComparison(false);
        Assert.AreEqual(0m, result);
    }

    #endregion
}
