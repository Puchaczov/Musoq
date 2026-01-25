using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for conversion methods in LibraryBaseConverting.cs to improve branch coverage
/// </summary>
[TestClass]
public class ConversionExtendedTests : LibraryBaseBaseTests
{
    #region ToBin (bytes) Tests

    [TestMethod]
    public void ToBin_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.ToBin(null));
    }

    [TestMethod]
    public void ToBin_EmptyBytes_ReturnsEmptyString()
    {
        Assert.AreEqual("", Library.ToBin(Array.Empty<byte>()));
    }

    [TestMethod]
    public void ToBin_SingleByte_ReturnsBinary()
    {
        Assert.AreEqual("11111111", Library.ToBin(new byte[] { 255 }));
    }

    [TestMethod]
    public void ToBin_MultipleBytes_ReturnsBinary()
    {
        Assert.AreEqual("0000000111111111", Library.ToBin(new byte[] { 1, 255 }));
    }

    [TestMethod]
    public void ToBin_WithDelimiter_ReturnsBinaryWithDelimiter()
    {
        var result = Library.ToBin(new byte[] { 1, 255 }, " ");
        Assert.AreEqual("00000001 11111111 ", result);
    }

    #endregion

    #region ToBin (generic) Tests

    [TestMethod]
    public void ToBin_Int_ReturnsBinary()
    {
        var result = Library.ToBin(10);
        Assert.AreEqual("1010", result);
    }

    [TestMethod]
    public void ToBin_Byte_ReturnsBinary()
    {
        var result = Library.ToBin((byte)255);
        Assert.AreEqual("11111111", result);
    }

    [TestMethod]
    public void ToBin_Boolean_ReturnsBinary()
    {
        Assert.AreEqual("1", Library.ToBin(true));
        Assert.AreEqual("0", Library.ToBin(false));
    }

    #endregion

    #region ToOcta Tests

    [TestMethod]
    public void ToOcta_Int_ReturnsOctal()
    {
        Assert.AreEqual("12", Library.ToOcta(10));
        Assert.AreEqual("144", Library.ToOcta(100));
    }

    [TestMethod]
    public void ToOcta_Byte_ReturnsOctal()
    {
        Assert.AreEqual("377", Library.ToOcta((byte)255));
    }

    #endregion

    #region ToDec Tests

    [TestMethod]
    public void ToDec_Int_ReturnsDecimal()
    {
        Assert.AreEqual("10", Library.ToDec(10));
        Assert.AreEqual("100", Library.ToDec(100));
    }

    [TestMethod]
    public void ToDec_Short_ReturnsDecimal()
    {
        Assert.AreEqual("1000", Library.ToDec((short)1000));
    }

    #endregion

    #region ToBase64 Tests (bytes)

    [TestMethod]
    public void ToBase64_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.ToBase64((byte[]?)null));
    }

    [TestMethod]
    public void ToBase64_EmptyBytes_ReturnsEmpty()
    {
        Assert.AreEqual("", Library.ToBase64(Array.Empty<byte>()));
    }

    [TestMethod]
    public void ToBase64_ValidBytes_ReturnsBase64()
    {
        var bytes = new byte[] { 72, 101, 108, 108, 111 };
        Assert.AreEqual("SGVsbG8=", Library.ToBase64(bytes));
    }

    [TestMethod]
    public void ToBase64_WithOffsetAndLength_ReturnsBase64()
    {
        var bytes = new byte[] { 72, 101, 108, 108, 111 };
        var result = Library.ToBase64(bytes, 0, 3);
        Assert.AreEqual("SGVs", result);
    }

    [TestMethod]
    public void ToBase64_WithOffsetAndLength_NullBytes_ReturnsNull()
    {
        Assert.IsNull(Library.ToBase64(null, 0, 3));
    }

    #endregion

    #region ToBase64 Tests (string)

    [TestMethod]
    public void ToBase64_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.ToBase64((string?)null));
    }

    [TestMethod]
    public void ToBase64_ValidString_ReturnsBase64()
    {
        Assert.AreEqual("SGVsbG8=", Library.ToBase64("Hello"));
    }

    [TestMethod]
    public void ToBase64_StringWithEncoding_ReturnsBase64()
    {
        Assert.AreEqual("SGVsbG8=", Library.ToBase64("Hello", "UTF-8"));
    }

    [TestMethod]
    public void ToBase64_StringWithEncoding_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.ToBase64(null, "UTF-8"));
    }

    #endregion

    #region FromBase64 Tests

    [TestMethod]
    public void FromBase64_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.FromBase64(null));
    }

    [TestMethod]
    public void FromBase64_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.FromBase64(""));
    }

    [TestMethod]
    public void FromBase64_ValidBase64_ReturnsBytes()
    {
        var result = Library.FromBase64("SGVsbG8=");
        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new byte[] { 72, 101, 108, 108, 111 }, result);
    }

    #endregion

    #region FromBase64ToString Tests

    [TestMethod]
    public void FromBase64ToString_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.FromBase64ToString(null));
    }

    [TestMethod]
    public void FromBase64ToString_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.FromBase64ToString(""));
    }

    [TestMethod]
    public void FromBase64ToString_ValidBase64_ReturnsString()
    {
        Assert.AreEqual("Hello", Library.FromBase64ToString("SGVsbG8="));
    }

    [TestMethod]
    public void FromBase64ToString_WithEncoding_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.FromBase64ToString(null, "UTF-8"));
    }

    [TestMethod]
    public void FromBase64ToString_WithEncoding_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.FromBase64ToString("", "UTF-8"));
    }

    [TestMethod]
    public void FromBase64ToString_WithEncoding_ValidBase64_ReturnsString()
    {
        Assert.AreEqual("Hello", Library.FromBase64ToString("SGVsbG8=", "UTF-8"));
    }

    #endregion

    #region FromHex Tests

    [TestMethod]
    public void FromHex_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.FromHex(null));
    }

    [TestMethod]
    public void FromHex_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.FromHex(""));
    }

    [TestMethod]
    public void FromHex_WhitespaceString_ReturnsNull()
    {
        Assert.IsNull(Library.FromHex("   "));
    }

    [TestMethod]
    public void FromHex_ValidHex_ReturnsLong()
    {
        Assert.AreEqual(255L, Library.FromHex("FF"));
    }

    [TestMethod]
    public void FromHex_ValidHexWith0xPrefix_ReturnsLong()
    {
        Assert.AreEqual(255L, Library.FromHex("0xFF"));
    }

    [TestMethod]
    public void FromHex_ValidHexWith0XPrefixUppercase_ReturnsLong()
    {
        Assert.AreEqual(255L, Library.FromHex("0XFF"));
    }

    [TestMethod]
    public void FromHex_InvalidHex_ReturnsNull()
    {
        Assert.IsNull(Library.FromHex("GGG"));
    }

    [TestMethod]
    public void FromHex_WithWhitespace_ReturnsLong()
    {
        Assert.AreEqual(255L, Library.FromHex("  FF  "));
    }

    #endregion

    #region FromBin Tests

    [TestMethod]
    public void FromBin_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.FromBin(null));
    }

    [TestMethod]
    public void FromBin_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.FromBin(""));
    }

    [TestMethod]
    public void FromBin_WhitespaceString_ReturnsNull()
    {
        Assert.IsNull(Library.FromBin("   "));
    }

    [TestMethod]
    public void FromBin_ValidBinary_ReturnsLong()
    {
        Assert.AreEqual(10L, Library.FromBin("1010"));
    }

    [TestMethod]
    public void FromBin_ValidBinaryWith0bPrefix_ReturnsLong()
    {
        Assert.AreEqual(10L, Library.FromBin("0b1010"));
    }

    [TestMethod]
    public void FromBin_ValidBinaryWith0BPrefixUppercase_ReturnsLong()
    {
        Assert.AreEqual(10L, Library.FromBin("0B1010"));
    }

    [TestMethod]
    public void FromBin_InvalidBinary_ReturnsNull()
    {
        Assert.IsNull(Library.FromBin("1234"));
    }

    [TestMethod]
    public void FromBin_WithWhitespace_ReturnsLong()
    {
        Assert.AreEqual(10L, Library.FromBin("  1010  "));
    }

    #endregion

    #region FromOct Tests

    [TestMethod]
    public void FromOct_NullString_ReturnsNull()
    {
        Assert.IsNull(Library.FromOct(null));
    }

    [TestMethod]
    public void FromOct_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.FromOct(""));
    }

    [TestMethod]
    public void FromOct_WhitespaceString_ReturnsNull()
    {
        Assert.IsNull(Library.FromOct("   "));
    }

    [TestMethod]
    public void FromOct_ValidOctal_ReturnsLong()
    {
        Assert.AreEqual(10L, Library.FromOct("12"));
    }

    [TestMethod]
    public void FromOct_ValidOctalWith0oPrefix_ReturnsLong()
    {
        Assert.AreEqual(10L, Library.FromOct("0o12"));
    }

    [TestMethod]
    public void FromOct_ValidOctalWith0OPrefixUppercase_ReturnsLong()
    {
        Assert.AreEqual(10L, Library.FromOct("0O12"));
    }

    [TestMethod]
    public void FromOct_InvalidOctal_ReturnsNull()
    {
        Assert.IsNull(Library.FromOct("99"));
    }

    [TestMethod]
    public void FromOct_WithWhitespace_ReturnsLong()
    {
        Assert.AreEqual(10L, Library.FromOct("  12  "));
    }

    #endregion

    #region ToBase Various Types Tests (to cover all TypeCodes)

    [TestMethod]
    public void ToBin_Char_ReturnsBinary()
    {
        Assert.AreEqual("1000001", Library.ToBin('A'));
    }

    [TestMethod]
    public void ToBin_Short_ReturnsBinary()
    {
        Assert.AreEqual("1111111111111111", Library.ToBin((short)-1));
    }

    [TestMethod]
    public void ToBin_Long_ReturnsBinary()
    {
        Assert.AreEqual("1010", Library.ToBin(10L));
    }

    [TestMethod]
    public void ToBin_SByte_ReturnsBinary()
    {
        Assert.AreEqual("1010", Library.ToBin((sbyte)10));
    }

    [TestMethod]
    public void ToBin_UInt16_ReturnsBinary()
    {
        Assert.AreEqual("1010", Library.ToBin((ushort)10));
    }

    [TestMethod]
    public void ToBin_UInt32_ReturnsBinary()
    {
        Assert.AreEqual("1010", Library.ToBin((uint)10));
    }

    [TestMethod]
    public void ToBin_Double_ReturnsBinary()
    {
        var result = Library.ToBin(1.0);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public void ToBin_Decimal_ReturnsNotSupported()
    {
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", Library.ToBin(10m));
    }

    [TestMethod]
    public void ToBin_DateTime_ReturnsNotSupported()
    {
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", Library.ToBin(DateTime.Now));
    }

    [TestMethod]
    public void ToBin_Float_ReturnsNotSupported()
    {
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", Library.ToBin(1.0f));
    }

    #endregion
}
