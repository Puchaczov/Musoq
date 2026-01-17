using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class MathTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void AbsDecimalTest()
    {
        Assert.AreEqual(112.5734m, Library.Abs(112.5734m));
        Assert.AreEqual(112.5734m, Library.Abs(-112.5734m));
        Assert.IsNull(Library.Abs((decimal?)null));
    }

    [TestMethod]
    public void AbsLongTest()
    {
        Assert.AreEqual(112L, Library.Abs(112L));
        Assert.AreEqual(112L, Library.Abs(-112L));
        Assert.IsNull(Library.Abs((long?)null));
    }

    [TestMethod]
    public void AbsIntTest()
    {
        Assert.AreEqual(112, Library.Abs(112));
        Assert.AreEqual(112, Library.Abs(-112));
        Assert.IsNull(Library.Abs(null));
    }

    [TestMethod]
    public void CeilTest()
    {
        Assert.AreEqual(113m, Library.Ceil(112.5734m));
        Assert.AreEqual(-112m, Library.Ceil(-112.5734m));
        Assert.IsNull(Library.Ceil(null));
    }

    [TestMethod]
    public void FloorTest()
    {
        Assert.AreEqual(112m, Library.Floor(112.5734m));
        Assert.AreEqual(-113m, Library.Floor(-112.5734m));
        Assert.IsNull(Library.Floor(null));
    }

    [TestMethod]
    public void SignDecimalTest()
    {
        Assert.AreEqual(1m, Library.Sign(13m));
        Assert.AreEqual(0m, Library.Sign(0m));
        Assert.AreEqual(-1m, Library.Sign(-13m));
        Assert.IsNull(Library.Sign((decimal?)null));
    }

    [TestMethod]
    public void SignLongTest()
    {
        Assert.AreEqual(1, Library.Sign(13));
        Assert.AreEqual(0, Library.Sign(0));
        Assert.AreEqual(-1, Library.Sign(-13));
        Assert.IsNull(Library.Sign(null));
    }

    [TestMethod]
    public void RoundTest()
    {
        Assert.AreEqual(2.1m, Library.Round(2.1351m, 1));
        Assert.IsNull(Library.Round(null, 1));
    }

    [TestMethod]
    public void PercentOfTest()
    {
        Assert.AreEqual(25m, Library.PercentOf(25, 100));
        Assert.IsNull(Library.PercentOf(null, 100));
        Assert.IsNull(Library.PercentOf(25, null));
        Assert.IsNull(Library.PercentOf(null, null));
    }

    [TestMethod]
    public void FromHexTest()
    {
        // Basic hex parsing
        Assert.AreEqual(255L, Library.FromHex("FF"));
        Assert.AreEqual(255L, Library.FromHex("ff"));
        Assert.AreEqual(10L, Library.FromHex("A"));
        Assert.AreEqual(16L, Library.FromHex("10"));
        
        // With 0x prefix
        Assert.AreEqual(255L, Library.FromHex("0xFF"));
        Assert.AreEqual(255L, Library.FromHex("0xff"));
        Assert.AreEqual(255L, Library.FromHex("0XFF"));
        
        // Negative values
        Assert.AreEqual(-1L, Library.FromHex("FFFFFFFFFFFFFFFF"));
        
        // Edge cases
        Assert.AreEqual(0L, Library.FromHex("0"));
        Assert.AreEqual(0L, Library.FromHex("0x0"));
        
        // Invalid inputs
        Assert.IsNull(Library.FromHex(null));
        Assert.IsNull(Library.FromHex(""));
        Assert.IsNull(Library.FromHex("   "));
        Assert.IsNull(Library.FromHex("GG"));
        Assert.IsNull(Library.FromHex("0xGG"));
    }

    [TestMethod]
    public void FromBinTest()
    {
        // Basic binary parsing
        Assert.AreEqual(5L, Library.FromBin("101"));
        Assert.AreEqual(10L, Library.FromBin("1010"));
        Assert.AreEqual(15L, Library.FromBin("1111"));
        
        // With 0b prefix
        Assert.AreEqual(5L, Library.FromBin("0b101"));
        Assert.AreEqual(5L, Library.FromBin("0B101"));
        
        // Edge cases
        Assert.AreEqual(0L, Library.FromBin("0"));
        Assert.AreEqual(0L, Library.FromBin("0b0"));
        Assert.AreEqual(1L, Library.FromBin("1"));
        
        // Invalid inputs
        Assert.IsNull(Library.FromBin(null));
        Assert.IsNull(Library.FromBin(""));
        Assert.IsNull(Library.FromBin("   "));
        Assert.IsNull(Library.FromBin("102"));
        Assert.IsNull(Library.FromBin("0b102"));
    }

    [TestMethod]
    public void FromOctTest()
    {
        // Basic octal parsing
        Assert.AreEqual(8L, Library.FromOct("10"));
        Assert.AreEqual(64L, Library.FromOct("100"));
        Assert.AreEqual(7L, Library.FromOct("7"));
        Assert.AreEqual(511L, Library.FromOct("777"));
        
        // With 0o prefix
        Assert.AreEqual(8L, Library.FromOct("0o10"));
        Assert.AreEqual(8L, Library.FromOct("0O10"));
        
        // Edge cases
        Assert.AreEqual(0L, Library.FromOct("0"));
        Assert.AreEqual(0L, Library.FromOct("0o0"));
        
        // Invalid inputs
        Assert.IsNull(Library.FromOct(null));
        Assert.IsNull(Library.FromOct(""));
        Assert.IsNull(Library.FromOct("   "));
        Assert.IsNull(Library.FromOct("8"));
        Assert.IsNull(Library.FromOct("0o8"));
    }

    #region Hex String Conversion Tests

    [TestMethod]
    public void FromHexToBytes_ShouldConvertHexToBytes()
    {
        var result = Library.FromHexToBytes("48656C6C6F");
        
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello", System.Text.Encoding.UTF8.GetString(result));
    }

    [TestMethod]
    public void FromHexToBytes_WithDelimiters_ShouldConvertCorrectly()
    {
        // Space-delimited
        var result1 = Library.FromHexToBytes("48 65 6C 6C 6F");
        Assert.IsNotNull(result1);
        Assert.AreEqual("Hello", System.Text.Encoding.UTF8.GetString(result1));

        // Dash-delimited
        var result2 = Library.FromHexToBytes("48-65-6C-6C-6F");
        Assert.IsNotNull(result2);
        Assert.AreEqual("Hello", System.Text.Encoding.UTF8.GetString(result2));

        // Colon-delimited (MAC address style)
        var result3 = Library.FromHexToBytes("48:65:6C:6C:6F");
        Assert.IsNotNull(result3);
        Assert.AreEqual("Hello", System.Text.Encoding.UTF8.GetString(result3));
    }

    [TestMethod]
    public void FromHexToBytes_With0xPrefix_ShouldConvertCorrectly()
    {
        var result = Library.FromHexToBytes("0x48656C6C6F");
        
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello", System.Text.Encoding.UTF8.GetString(result));
    }

    [TestMethod]
    public void FromHexToBytes_WhenNull_ShouldReturnNull()
    {
        var result = Library.FromHexToBytes(null);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromHexToBytes_WhenEmpty_ShouldReturnNull()
    {
        var result = Library.FromHexToBytes(string.Empty);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromHexToBytes_WhenOddLength_ShouldReturnNull()
    {
        var result = Library.FromHexToBytes("48656");
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromHexToString_ShouldConvertHexToString()
    {
        var result = Library.FromHexToString("48656C6C6F");
        
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void FromHexToString_WhenNull_ShouldReturnNull()
    {
        var result = Library.FromHexToString(null);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromHexToString_WithEncoding_ShouldConvertCorrectly()
    {
        var result = Library.FromHexToString("48656C6C6F", "UTF-8");
        
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToHexFromString_ShouldConvertStringToHex()
    {
        var result = Library.ToHexFromString("Hello");
        
        Assert.AreEqual("48656C6C6F", result);
    }

    [TestMethod]
    public void ToHexFromString_WhenNull_ShouldReturnNull()
    {
        var result = Library.ToHexFromString(null);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToHexFromString_WithEncoding_ShouldConvertCorrectly()
    {
        var result = Library.ToHexFromString("Hello", "UTF-8");
        
        Assert.AreEqual("48656C6C6F", result);
    }

    [TestMethod]
    public void HexRoundTrip_ShouldPreserveContent()
    {
        const string original = "Hello, World! 日本語";
        
        var hex = Library.ToHexFromString(original);
        var decoded = Library.FromHexToString(hex);
        
        Assert.AreEqual(original, decoded);
    }

    #endregion

    #region Tan Tests

    [TestMethod]
    public void Tan_Decimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Tan((decimal?)null));
    }

    [TestMethod]
    public void Tan_Decimal_Zero_ReturnsZero()
    {
        var result = LibraryBase.Tan(0m);
        Assert.IsNotNull(result);
        Assert.AreEqual(0m, result);
    }

    [TestMethod]
    public void Tan_Double_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Tan((double?)null));
    }

    [TestMethod]
    public void Tan_Double_Zero_ReturnsZero()
    {
        var result = LibraryBase.Tan(0.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(0.0, result);
    }

    #endregion

    #region Exp Tests

    [TestMethod]
    public void Exp_Decimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Exp((decimal?)null));
    }

    [TestMethod]
    public void Exp_Decimal_Zero_ReturnsOne()
    {
        var result = LibraryBase.Exp(0m);
        Assert.IsNotNull(result);
        Assert.AreEqual(1m, result);
    }

    [TestMethod]
    public void Exp_Double_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Exp((double?)null));
    }

    [TestMethod]
    public void Exp_Double_Zero_ReturnsOne()
    {
        var result = LibraryBase.Exp(0.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, result);
    }

    [TestMethod]
    public void Exp_Double_One_ReturnsE()
    {
        var result = LibraryBase.Exp(1.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(System.Math.E, result.Value, 0.0001);
    }

    #endregion

    #region Ln Tests

    [TestMethod]
    public void Ln_Decimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Ln((decimal?)null));
    }

    [TestMethod]
    public void Ln_Decimal_One_ReturnsZero()
    {
        var result = LibraryBase.Ln(1m);
        Assert.IsNotNull(result);
        Assert.AreEqual(0m, result);
    }

    [TestMethod]
    public void Ln_Double_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Ln((double?)null));
    }

    [TestMethod]
    public void Ln_Double_One_ReturnsZero()
    {
        var result = LibraryBase.Ln(1.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(0.0, result);
    }

    [TestMethod]
    public void Ln_Double_E_ReturnsOne()
    {
        var result = LibraryBase.Ln(System.Math.E);
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, result.Value, 0.0001);
    }

    #endregion

    #region Clamp Tests

    [TestMethod]
    public void Clamp_Int_WhenValueNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp((int?)null, 0, 10));
    }

    [TestMethod]
    public void Clamp_Int_WhenMinNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5, (int?)null, 10));
    }

    [TestMethod]
    public void Clamp_Int_WhenMaxNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5, 0, (int?)null));
    }

    [TestMethod]
    public void Clamp_Int_ValueInRange_ReturnsValue()
    {
        Assert.AreEqual(5, LibraryBase.Clamp(5, 0, 10));
    }

    [TestMethod]
    public void Clamp_Int_ValueBelowMin_ReturnsMin()
    {
        Assert.AreEqual(0, LibraryBase.Clamp(-5, 0, 10));
    }

    [TestMethod]
    public void Clamp_Int_ValueAboveMax_ReturnsMax()
    {
        Assert.AreEqual(10, LibraryBase.Clamp(15, 0, 10));
    }

    [TestMethod]
    public void Clamp_Decimal_ValueInRange_ReturnsValue()
    {
        Assert.AreEqual(5.5m, LibraryBase.Clamp(5.5m, 0m, 10m));
    }

    [TestMethod]
    public void Clamp_Double_ValueInRange_ReturnsValue()
    {
        Assert.AreEqual(5.5, LibraryBase.Clamp(5.5, 0.0, 10.0));
    }

    #endregion

    #region LogBase Tests

    [TestMethod]
    public void LogBase_WhenValueNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.LogBase((double?)null, 10.0));
    }

    [TestMethod]
    public void LogBase_WhenBaseNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.LogBase(100.0, (double?)null));
    }

    [TestMethod]
    public void LogBase_Base10_ReturnsCorrectValue()
    {
        var result = LibraryBase.LogBase(100.0, 10.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(2.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void LogBase_Base2_ReturnsCorrectValue()
    {
        var result = LibraryBase.LogBase(8.0, 2.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(3.0, result.Value, 0.0001);
    }

    #endregion

    #region Log10 Tests

    [TestMethod]
    public void Log10_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log10((double?)null));
    }

    [TestMethod]
    public void Log10_Of100_Returns2()
    {
        var result = LibraryBase.Log10(100.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(2.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Log10_Of1_ReturnsZero()
    {
        var result = LibraryBase.Log10(1.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(0.0, result);
    }

    #endregion

    #region Log2 Tests

    [TestMethod]
    public void Log2_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log2((double?)null));
    }

    [TestMethod]
    public void Log2_Of8_Returns3()
    {
        var result = LibraryBase.Log2(8.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(3.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Log2_Of1_ReturnsZero()
    {
        var result = LibraryBase.Log2(1.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(0.0, result);
    }

    #endregion

    #region IsBetween Tests

    [TestMethod]
    public void IsBetween_Int_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween((int?)null, 1, 10));
        Assert.IsNull(LibraryBase.IsBetween(5, (int?)null, 10));
        Assert.IsNull(LibraryBase.IsBetween(5, 1, (int?)null));
    }

    [TestMethod]
    public void IsBetween_Int_WhenInRange_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetween(5, 1, 10));
        Assert.IsTrue(LibraryBase.IsBetween(1, 1, 10)); // inclusive min
        Assert.IsTrue(LibraryBase.IsBetween(10, 1, 10)); // inclusive max
    }

    [TestMethod]
    public void IsBetween_Int_WhenOutOfRange_ReturnsFalse()
    {
        Assert.IsFalse(LibraryBase.IsBetween(0, 1, 10));
        Assert.IsFalse(LibraryBase.IsBetween(11, 1, 10));
    }

    [TestMethod]
    public void IsBetween_Long_WhenInRange_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetween(5L, 1L, 10L));
    }

    [TestMethod]
    public void IsBetween_Decimal_WhenInRange_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetween(5.5m, 1.0m, 10.0m));
    }

    [TestMethod]
    public void IsBetween_Decimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween((decimal?)null, 1.0m, 10.0m));
    }

    [TestMethod]
    public void IsBetween_Double_WhenInRange_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetween(5.5, 1.0, 10.0));
    }

    [TestMethod]
    public void IsBetween_Double_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween((double?)null, 1.0, 10.0));
    }

    [TestMethod]
    public void IsBetweenExclusive_Int_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetweenExclusive((int?)null, 1, 10));
    }

    [TestMethod]
    public void IsBetweenExclusive_Int_WhenInRange_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetweenExclusive(5, 1, 10));
    }

    [TestMethod]
    public void IsBetweenExclusive_Int_WhenAtBoundary_ReturnsFalse()
    {
        Assert.IsFalse(LibraryBase.IsBetweenExclusive(1, 1, 10)); // exclusive min
        Assert.IsFalse(LibraryBase.IsBetweenExclusive(10, 1, 10)); // exclusive max
    }

    [TestMethod]
    public void IsBetweenExclusive_Decimal_WhenInRange_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetweenExclusive(5.5m, 1.0m, 10.0m));
    }

    [TestMethod]
    public void IsBetweenExclusive_Decimal_WhenAtBoundary_ReturnsFalse()
    {
        Assert.IsFalse(LibraryBase.IsBetweenExclusive(1.0m, 1.0m, 10.0m));
        Assert.IsFalse(LibraryBase.IsBetweenExclusive(10.0m, 1.0m, 10.0m));
    }

    [TestMethod]
    public void IsBetweenExclusive_Decimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetweenExclusive((decimal?)null, 1.0m, 10.0m));
        Assert.IsNull(LibraryBase.IsBetweenExclusive(5.0m, (decimal?)null, 10.0m));
        Assert.IsNull(LibraryBase.IsBetweenExclusive(5.0m, 1.0m, (decimal?)null));
    }

    [TestMethod]
    public void IsBetweenExclusive_Int_WhenMinNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetweenExclusive(5, (int?)null, 10));
    }

    [TestMethod]
    public void IsBetweenExclusive_Int_WhenMaxNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetweenExclusive(5, 1, (int?)null));
    }

    [TestMethod]
    public void IsBetween_Long_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween((long?)null, 1L, 10L));
        Assert.IsNull(LibraryBase.IsBetween(5L, (long?)null, 10L));
        Assert.IsNull(LibraryBase.IsBetween(5L, 1L, (long?)null));
    }

    #endregion

    #region Rand Tests

    [TestMethod]
    public void Rand_ReturnsInteger()
    {
        var result = Library.Rand();
        Assert.IsGreaterThanOrEqualTo(0, result);
    }

    [TestMethod]
    public void Rand_WithRange_ReturnsValueInRange()
    {
        var result = Library.Rand(5, 10);
        Assert.IsNotNull(result);
        Assert.IsTrue(result >= 5 && result < 10);
    }

    [TestMethod]
    public void Rand_WithMinNull_ReturnsNull()
    {
        Assert.IsNull(Library.Rand(null, 10));
    }

    [TestMethod]
    public void Rand_WithMaxNull_ReturnsNull()
    {
        Assert.IsNull(Library.Rand(5, null));
    }

    [TestMethod]
    public void Rand_WithBothNull_ReturnsNull()
    {
        Assert.IsNull(Library.Rand(null, null));
    }

    #endregion

    #region Pow Tests

    [TestMethod]
    public void Pow_Decimal_WhenXNull_ReturnsNull()
    {
        Assert.IsNull(Library.Pow((decimal?)null, 2m));
    }

    [TestMethod]
    public void Pow_Decimal_WhenYNull_ReturnsNull()
    {
        Assert.IsNull(Library.Pow(2m, (decimal?)null));
    }

    [TestMethod]
    public void Pow_Decimal_WhenBothNull_ReturnsNull()
    {
        Assert.IsNull(Library.Pow((decimal?)null, (decimal?)null));
    }

    [TestMethod]
    public void Pow_Decimal_ValidValues_ReturnsCorrectResult()
    {
        var result = Library.Pow(2m, 3m);
        Assert.IsNotNull(result);
        Assert.AreEqual(8.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Pow_Double_WhenXNull_ReturnsNull()
    {
        Assert.IsNull(Library.Pow((double?)null, 2.0));
    }

    [TestMethod]
    public void Pow_Double_WhenYNull_ReturnsNull()
    {
        Assert.IsNull(Library.Pow(2.0, (double?)null));
    }

    [TestMethod]
    public void Pow_Double_WhenBothNull_ReturnsNull()
    {
        Assert.IsNull(Library.Pow((double?)null, (double?)null));
    }

    [TestMethod]
    public void Pow_Double_ValidValues_ReturnsCorrectResult()
    {
        var result = Library.Pow(2.0, 3.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(8.0, result);
    }

    [TestMethod]
    public void Pow_Double_Zero_ReturnsOne()
    {
        var result = Library.Pow(5.0, 0.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, result);
    }

    [TestMethod]
    public void Pow_Double_Fractional_ReturnsCorrectResult()
    {
        var result = Library.Pow(4.0, 0.5);
        Assert.IsNotNull(result);
        Assert.AreEqual(2.0, result.Value, 0.0001);
    }

    #endregion

    #region Sqrt Tests

    [TestMethod]
    public void Sqrt_Decimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.Sqrt((decimal?)null));
    }

    [TestMethod]
    public void Sqrt_Decimal_ValidValue_ReturnsCorrectResult()
    {
        var result = Library.Sqrt(4m);
        Assert.IsNotNull(result);
        Assert.AreEqual(2.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Sqrt_Decimal_Zero_ReturnsZero()
    {
        var result = Library.Sqrt(0m);
        Assert.IsNotNull(result);
        Assert.AreEqual(0.0, result);
    }

    [TestMethod]
    public void Sqrt_Double_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.Sqrt((double?)null));
    }

    [TestMethod]
    public void Sqrt_Double_ValidValue_ReturnsCorrectResult()
    {
        var result = Library.Sqrt(9.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(3.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Sqrt_Long_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.Sqrt((long?)null));
    }

    [TestMethod]
    public void Sqrt_Long_ValidValue_ReturnsCorrectResult()
    {
        var result = Library.Sqrt(16L);
        Assert.IsNotNull(result);
        Assert.AreEqual(4.0, result.Value, 0.0001);
    }

    #endregion

    #region PercentRank Tests

    [TestMethod]
    public void PercentRank_WhenWindowNull_ReturnsNull()
    {
        Assert.IsNull(Library.PercentRank<int>(null, 5));
    }

    [TestMethod]
    public void PercentRank_WhenValueNull_ReturnsNull()
    {
        string[] window = ["a", "b", "c"];
        Assert.IsNull(Library.PercentRank(window, null));
    }

    [TestMethod]
    public void PercentRank_ValidValues_ReturnsCorrectResult()
    {
        var window = new[] { 1, 2, 3, 4, 5 };
        var result = Library.PercentRank(window, 3);
        Assert.IsNotNull(result);
    }

    #endregion

    #region Log Tests

    [TestMethod]
    public void Log_WhenBaseNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log((decimal?)null, 100m));
    }

    [TestMethod]
    public void Log_WhenValueNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(10m, (decimal?)null));
    }

    [TestMethod]
    public void Log_WhenBaseIsZero_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(0m, 100m));
    }

    [TestMethod]
    public void Log_WhenBaseIsOne_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(1m, 100m));
    }

    [TestMethod]
    public void Log_WhenBaseIsNegative_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(-10m, 100m));
    }

    [TestMethod]
    public void Log_WhenValueIsZero_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(10m, 0m));
    }

    [TestMethod]
    public void Log_WhenValueIsNegative_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(10m, -100m));
    }

    [TestMethod]
    public void Log_ValidValues_ReturnsCorrectResult()
    {
        var result = LibraryBase.Log(10m, 100m);
        Assert.IsNotNull(result);
        Assert.AreEqual(2.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Log_Base2_ReturnsCorrectResult()
    {
        var result = LibraryBase.Log(2m, 8m);
        Assert.IsNotNull(result);
        Assert.AreEqual(3.0, result.Value, 0.0001);
    }

    #endregion

    #region Sin Tests

    [TestMethod]
    public void Sin_Decimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Sin((decimal?)null));
    }

    [TestMethod]
    public void Sin_Decimal_Zero_ReturnsZero()
    {
        var result = LibraryBase.Sin(0m);
        Assert.IsNotNull(result);
        Assert.AreEqual(0m, result);
    }

    [TestMethod]
    public void Sin_Double_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Sin((double?)null));
    }

    [TestMethod]
    public void Sin_Double_Zero_ReturnsZero()
    {
        var result = LibraryBase.Sin(0.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(0.0, result);
    }

    [TestMethod]
    public void Sin_Double_PiOver2_ReturnsOne()
    {
        var result = LibraryBase.Sin(System.Math.PI / 2);
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Sin_Float_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Sin((float?)null));
    }

    [TestMethod]
    public void Sin_Float_Zero_ReturnsZero()
    {
        var result = LibraryBase.Sin(0f);
        Assert.IsNotNull(result);
        Assert.AreEqual(0f, result);
    }

    #endregion

    #region Cos Tests

    [TestMethod]
    public void Cos_Decimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Cos((decimal?)null));
    }

    [TestMethod]
    public void Cos_Decimal_Zero_ReturnsOne()
    {
        var result = LibraryBase.Cos(0m);
        Assert.IsNotNull(result);
        Assert.AreEqual(1m, result);
    }

    [TestMethod]
    public void Cos_Double_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Cos((double?)null));
    }

    [TestMethod]
    public void Cos_Double_Zero_ReturnsOne()
    {
        var result = LibraryBase.Cos(0.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, result);
    }

    [TestMethod]
    public void Cos_Double_Pi_ReturnsNegativeOne()
    {
        var result = LibraryBase.Cos(System.Math.PI);
        Assert.IsNotNull(result);
        Assert.AreEqual(-1.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Cos_Float_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Cos((float?)null));
    }

    [TestMethod]
    public void Cos_Float_Zero_ReturnsOne()
    {
        var result = LibraryBase.Cos(0f);
        Assert.IsNotNull(result);
        Assert.AreEqual(1f, result);
    }

    #endregion

    #region Additional Clamp Tests

    [TestMethod]
    public void Clamp_Long_WhenValueNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp((long?)null, 0L, 10L));
    }

    [TestMethod]
    public void Clamp_Long_WhenMinNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5L, (long?)null, 10L));
    }

    [TestMethod]
    public void Clamp_Long_WhenMaxNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5L, 0L, (long?)null));
    }

    [TestMethod]
    public void Clamp_Long_ValueInRange_ReturnsValue()
    {
        Assert.AreEqual(5L, LibraryBase.Clamp(5L, 0L, 10L));
    }

    [TestMethod]
    public void Clamp_Long_ValueBelowMin_ReturnsMin()
    {
        Assert.AreEqual(0L, LibraryBase.Clamp(-5L, 0L, 10L));
    }

    [TestMethod]
    public void Clamp_Long_ValueAboveMax_ReturnsMax()
    {
        Assert.AreEqual(10L, LibraryBase.Clamp(15L, 0L, 10L));
    }

    [TestMethod]
    public void Clamp_Decimal_WhenValueNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp((decimal?)null, 0m, 10m));
    }

    [TestMethod]
    public void Clamp_Decimal_WhenMinNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5m, (decimal?)null, 10m));
    }

    [TestMethod]
    public void Clamp_Decimal_WhenMaxNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5m, 0m, (decimal?)null));
    }

    [TestMethod]
    public void Clamp_Double_WhenValueNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp((double?)null, 0.0, 10.0));
    }

    [TestMethod]
    public void Clamp_Double_WhenMinNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5.0, (double?)null, 10.0));
    }

    [TestMethod]
    public void Clamp_Double_WhenMaxNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5.0, 0.0, (double?)null));
    }

    #endregion

    #region Additional Tan Tests

    [TestMethod]
    public void Tan_Decimal_PiOver4_ReturnsOne()
    {
        var result = LibraryBase.Tan((decimal)(System.Math.PI / 4));
        Assert.IsNotNull(result);
        Assert.AreEqual(1m, System.Math.Round(result.Value, 0));
    }

    [TestMethod]
    public void Tan_Double_PiOver4_ReturnsOne()
    {
        var result = LibraryBase.Tan(System.Math.PI / 4);
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, result.Value, 0.0001);
    }

    #endregion
}