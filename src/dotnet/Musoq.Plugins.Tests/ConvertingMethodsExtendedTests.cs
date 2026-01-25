using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for LibraryBaseConverting, LibraryBaseConvertingToHex, and LibraryBaseConvertingFromBytes
///     to improve branch coverage by testing all type branches in switch statements.
/// </summary>
[TestClass]
public class ConvertingMethodsExtendedTests
{
    private readonly LibraryBase _library = new();

    #region ToHex<T> - All Type Branches

    [TestMethod]
    public void ToHex_Boolean_True_Converts()
    {
        var result = _library.ToHex(true);
        Assert.IsNotNull(result);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_Boolean_False_Converts()
    {
        var result = _library.ToHex(false);
        Assert.IsNotNull(result);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_Byte_Converts()
    {
        var result = _library.ToHex((byte)255);
        Assert.IsNotNull(result);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_Char_Converts()
    {
        var result = _library.ToHex('A');
        Assert.IsNotNull(result);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_DateTime_ReturnsNotSupported()
    {
        var result = _library.ToHex(DateTime.Now);
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_Decimal_Converts()
    {
        var result = _library.ToHex(123.45m);
        Assert.IsNotNull(result);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_Double_Converts()
    {
        var result = _library.ToHex(3.14159);
        Assert.IsNotNull(result);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_Int16_Converts()
    {
        var result = _library.ToHex((short)12345);
        Assert.IsNotNull(result);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_Int32_Converts()
    {
        var result = _library.ToHex(123456789);
        Assert.IsNotNull(result);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_Int64_Converts()
    {
        var result = _library.ToHex(123456789012345L);
        Assert.IsNotNull(result);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_SByte_Converts()
    {
        var result = _library.ToHex((sbyte)-50);
        Assert.IsNotNull(result);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_Single_Converts()
    {
        var result = _library.ToHex(3.14f);
        Assert.IsNotNull(result);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_String_Converts()
    {
        var result = _library.ToHex("Hello");
        Assert.IsNotNull(result);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_UInt16_Converts()
    {
        var result = _library.ToHex((ushort)12345);
        Assert.IsNotNull(result);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_UInt32_Converts()
    {
        var result = _library.ToHex((uint)123456);
        Assert.IsNotNull(result);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_UInt64_Converts()
    {
        var result = _library.ToHex((ulong)123456789);
        Assert.IsNotNull(result);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_Bytes_NullBytes_ReturnsNull()
    {
        var result = _library.ToHex(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToHex_Bytes_EmptyBytes_ReturnsEmpty()
    {
        var result = _library.ToHex(Array.Empty<byte>());
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ToHex_Bytes_WithDelimiter_Converts()
    {
        var result = _library.ToHex(new byte[] { 0x48, 0x65, 0x6C }, "-");
        Assert.AreEqual("48-65-6C", result);
    }

    #endregion

    #region ToBin<T> - All Type Branches

    [TestMethod]
    public void ToBin_Boolean_True_Converts()
    {
        var result = _library.ToBin(true);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToBin_Boolean_False_Converts()
    {
        var result = _library.ToBin(false);
        Assert.AreEqual("0", result);
    }

    [TestMethod]
    public void ToBin_Byte_Converts()
    {
        var result = _library.ToBin((byte)5);
        Assert.AreEqual("101", result);
    }

    [TestMethod]
    public void ToBin_Char_Converts()
    {
        var result = _library.ToBin('A');
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToBin_DateTime_ReturnsNotSupported()
    {
        var result = _library.ToBin(DateTime.Now);
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToBin_DBNull_ReturnsNotSupported()
    {
        var result = _library.ToBin(DBNull.Value);
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToBin_Decimal_ReturnsNotSupported()
    {
        var result = _library.ToBin(123.45m);
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToBin_Double_Converts()
    {
        var result = _library.ToBin(1.0);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToBin_Int16_Converts()
    {
        var result = _library.ToBin((short)10);
        Assert.AreEqual("1010", result);
    }

    [TestMethod]
    public void ToBin_Int32_Converts()
    {
        var result = _library.ToBin(10);
        Assert.AreEqual("1010", result);
    }

    [TestMethod]
    public void ToBin_Int64_Converts()
    {
        var result = _library.ToBin(10L);
        Assert.AreEqual("1010", result);
    }

    [TestMethod]
    public void ToBin_SByte_Converts()
    {
        var result = _library.ToBin((sbyte)5);
        Assert.AreEqual("101", result);
    }

    [TestMethod]
    public void ToBin_Single_ReturnsNotSupported()
    {
        var result = _library.ToBin(3.14f);
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToBin_String_ReturnsNotSupported()
    {
        var result = _library.ToBin("test");
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToBin_UInt16_Converts()
    {
        var result = _library.ToBin((ushort)10);
        Assert.AreEqual("1010", result);
    }

    [TestMethod]
    public void ToBin_UInt32_Converts()
    {
        var result = _library.ToBin((uint)10);
        Assert.AreEqual("1010", result);
    }

    [TestMethod]
    public void ToBin_UInt64_ReturnsNotSupported()
    {
        var result = _library.ToBin((ulong)10);
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToBin_Bytes_NullBytes_ReturnsNull()
    {
        var result = _library.ToBin(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToBin_Bytes_WithDelimiter_Converts()
    {
        var result = _library.ToBin(new byte[] { 0x01, 0x02 }, " ");
        Assert.IsNotNull(result);
        Assert.Contains(" ", result);
    }

    #endregion

    #region ToOcta<T> - All Type Branches

    [TestMethod]
    public void ToOcta_Boolean_Converts()
    {
        var result = _library.ToOcta(true);
        Assert.AreEqual("1", result);
    }

    [TestMethod]
    public void ToOcta_Byte_Converts()
    {
        var result = _library.ToOcta((byte)8);
        Assert.AreEqual("10", result);
    }

    [TestMethod]
    public void ToOcta_Char_Converts()
    {
        var result = _library.ToOcta('A');
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToOcta_DateTime_ReturnsNotSupported()
    {
        var result = _library.ToOcta(DateTime.Now);
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToOcta_Decimal_ReturnsNotSupported()
    {
        var result = _library.ToOcta(123.45m);
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToOcta_Double_Converts()
    {
        var result = _library.ToOcta(1.0);
        Assert.AreNotEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToOcta_Int16_Converts()
    {
        var result = _library.ToOcta((short)64);
        Assert.AreEqual("100", result);
    }

    [TestMethod]
    public void ToOcta_Int32_Converts()
    {
        var result = _library.ToOcta(64);
        Assert.AreEqual("100", result);
    }

    [TestMethod]
    public void ToOcta_Int64_Converts()
    {
        var result = _library.ToOcta(64L);
        Assert.AreEqual("100", result);
    }

    [TestMethod]
    public void ToOcta_SByte_Converts()
    {
        var result = _library.ToOcta((sbyte)8);
        Assert.AreEqual("10", result);
    }

    [TestMethod]
    public void ToOcta_Single_ReturnsNotSupported()
    {
        var result = _library.ToOcta(3.14f);
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToOcta_String_ReturnsNotSupported()
    {
        var result = _library.ToOcta("test");
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToOcta_UInt16_Converts()
    {
        var result = _library.ToOcta((ushort)64);
        Assert.AreEqual("100", result);
    }

    [TestMethod]
    public void ToOcta_UInt32_Converts()
    {
        var result = _library.ToOcta((uint)64);
        Assert.AreEqual("100", result);
    }

    [TestMethod]
    public void ToOcta_UInt64_ReturnsNotSupported()
    {
        var result = _library.ToOcta((ulong)64);
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    #endregion

    #region ToDec<T> - All Type Branches

    [TestMethod]
    public void ToDec_Boolean_Converts()
    {
        var result = _library.ToDec(true);
        Assert.AreEqual("1", result);
    }

    [TestMethod]
    public void ToDec_Byte_Converts()
    {
        var result = _library.ToDec((byte)255);
        Assert.AreEqual("255", result);
    }

    [TestMethod]
    public void ToDec_Char_Converts()
    {
        var result = _library.ToDec('A');
        Assert.AreEqual("65", result);
    }

    [TestMethod]
    public void ToDec_DateTime_ReturnsNotSupported()
    {
        var result = _library.ToDec(DateTime.Now);
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToDec_Decimal_ReturnsNotSupported()
    {
        var result = _library.ToDec(123.45m);
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToDec_Int16_Converts()
    {
        var result = _library.ToDec((short)12345);
        Assert.AreEqual("12345", result);
    }

    [TestMethod]
    public void ToDec_Int32_Converts()
    {
        var result = _library.ToDec(12345);
        Assert.AreEqual("12345", result);
    }

    [TestMethod]
    public void ToDec_Int64_Converts()
    {
        var result = _library.ToDec(12345L);
        Assert.AreEqual("12345", result);
    }

    [TestMethod]
    public void ToDec_SByte_Converts()
    {
        var result = _library.ToDec((sbyte)127);
        Assert.AreEqual("127", result);
    }

    [TestMethod]
    public void ToDec_Single_ReturnsNotSupported()
    {
        var result = _library.ToDec(3.14f);
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToDec_String_ReturnsNotSupported()
    {
        var result = _library.ToDec("test");
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToDec_UInt16_Converts()
    {
        var result = _library.ToDec((ushort)65535);
        Assert.AreEqual("65535", result);
    }

    [TestMethod]
    public void ToDec_UInt32_Converts()
    {
        var result = _library.ToDec((uint)12345);
        Assert.AreEqual("12345", result);
    }

    [TestMethod]
    public void ToDec_UInt64_ReturnsNotSupported()
    {
        var result = _library.ToDec((ulong)12345);
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    #endregion

    #region FromHex - Edge Cases

    [TestMethod]
    public void FromHex_NullValue_ReturnsNull()
    {
        var result = _library.FromHex(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromHex_EmptyValue_ReturnsNull()
    {
        var result = _library.FromHex(string.Empty);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromHex_WhitespaceOnly_ReturnsNull()
    {
        var result = _library.FromHex("   ");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromHex_WithPrefix_Converts()
    {
        var result = _library.FromHex("0xFF");
        Assert.AreEqual(255L, result);
    }

    [TestMethod]
    public void FromHex_WithoutPrefix_Converts()
    {
        var result = _library.FromHex("FF");
        Assert.AreEqual(255L, result);
    }

    [TestMethod]
    public void FromHex_InvalidChars_ReturnsNull()
    {
        var result = _library.FromHex("ZZZZ");
        Assert.IsNull(result);
    }

    #endregion

    #region FromBin - Edge Cases

    [TestMethod]
    public void FromBin_NullValue_ReturnsNull()
    {
        var result = _library.FromBin(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromBin_EmptyValue_ReturnsNull()
    {
        var result = _library.FromBin(string.Empty);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromBin_WhitespaceOnly_ReturnsNull()
    {
        var result = _library.FromBin("   ");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromBin_WithPrefix_Converts()
    {
        var result = _library.FromBin("0b1010");
        Assert.AreEqual(10L, result);
    }

    [TestMethod]
    public void FromBin_WithoutPrefix_Converts()
    {
        var result = _library.FromBin("1010");
        Assert.AreEqual(10L, result);
    }

    [TestMethod]
    public void FromBin_InvalidChars_ReturnsNull()
    {
        var result = _library.FromBin("102");
        Assert.IsNull(result);
    }

    #endregion

    #region FromOct - Edge Cases

    [TestMethod]
    public void FromOct_NullValue_ReturnsNull()
    {
        var result = _library.FromOct(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromOct_EmptyValue_ReturnsNull()
    {
        var result = _library.FromOct(string.Empty);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromOct_WhitespaceOnly_ReturnsNull()
    {
        var result = _library.FromOct("   ");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromOct_WithPrefix_Converts()
    {
        var result = _library.FromOct("0o17");
        Assert.AreEqual(15L, result);
    }

    [TestMethod]
    public void FromOct_WithoutPrefix_Converts()
    {
        var result = _library.FromOct("17");
        Assert.AreEqual(15L, result);
    }

    [TestMethod]
    public void FromOct_InvalidChars_ReturnsNull()
    {
        var result = _library.FromOct("89");
        Assert.IsNull(result);
    }

    #endregion

    #region ToText - All Encoding Branches

    [TestMethod]
    public void ToText_NullBytes_ReturnsEmpty()
    {
        var result = _library.ToText(null!, "utf-8");
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ToText_EmptyBytes_ReturnsEmpty()
    {
        var result = _library.ToText(Array.Empty<byte>(), "utf-8");
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ToText_Utf8_Decodes()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello");
        var result = _library.ToText(bytes, "utf-8");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_Utf8Short_Decodes()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello");
        var result = _library.ToText(bytes, "utf8");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_Utf16_Decodes()
    {
        var bytes = Encoding.Unicode.GetBytes("Hello");
        var result = _library.ToText(bytes, "utf-16");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_Utf16Short_Decodes()
    {
        var bytes = Encoding.Unicode.GetBytes("Hello");
        var result = _library.ToText(bytes, "utf16");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_Unicode_Decodes()
    {
        var bytes = Encoding.Unicode.GetBytes("Hello");
        var result = _library.ToText(bytes, "unicode");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_Utf16LE_Decodes()
    {
        var bytes = Encoding.Unicode.GetBytes("Hello");
        var result = _library.ToText(bytes, "utf-16le");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_Utf16LEShort_Decodes()
    {
        var bytes = Encoding.Unicode.GetBytes("Hello");
        var result = _library.ToText(bytes, "utf16le");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_Utf16BE_Decodes()
    {
        var bytes = Encoding.BigEndianUnicode.GetBytes("Hello");
        var result = _library.ToText(bytes, "utf-16be");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_Utf16BEShort_Decodes()
    {
        var bytes = Encoding.BigEndianUnicode.GetBytes("Hello");
        var result = _library.ToText(bytes, "utf16be");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_Ascii_Decodes()
    {
        var bytes = Encoding.ASCII.GetBytes("Hello");
        var result = _library.ToText(bytes, "ascii");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_Latin1_Decodes()
    {
        var bytes = Encoding.Latin1.GetBytes("Hello");
        var result = _library.ToText(bytes, "latin1");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_Iso88591_Decodes()
    {
        var bytes = Encoding.Latin1.GetBytes("Hello");
        var result = _library.ToText(bytes, "iso-8859-1");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_UnknownEncoding_FallsBackToUtf8()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello");
        var result = _library.ToText(bytes, "unknown-encoding");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_NullEncoding_FallsBackToUtf8()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello");
        var result = _library.ToText(bytes, null!);
        Assert.AreEqual("Hello", result);
    }

    #endregion

    #region Base64 - All Branches

    [TestMethod]
    public void ToBase64_NullBytes_ReturnsNull()
    {
        var result = _library.ToBase64((byte[]?)null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToBase64_EmptyBytes_ReturnsEmpty()
    {
        var result = _library.ToBase64(Array.Empty<byte>());
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ToBase64_NullString_ReturnsNull()
    {
        var result = _library.ToBase64((string?)null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToBase64_WithOffsetAndLength_Converts()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var result = _library.ToBase64(bytes, 1, 3);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ToBase64_WithOffsetAndLength_NullBytes_ReturnsNull()
    {
        var result = _library.ToBase64(null, 0, 0);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToBase64_StringWithEncoding_Converts()
    {
        var result = _library.ToBase64("Hello", "UTF-8");
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ToBase64_NullStringWithEncoding_ReturnsNull()
    {
        var result = _library.ToBase64(null, "UTF-8");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromBase64_NullValue_ReturnsNull()
    {
        var result = _library.FromBase64(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromBase64_EmptyValue_ReturnsNull()
    {
        var result = _library.FromBase64(string.Empty);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromBase64ToString_NullValue_ReturnsNull()
    {
        var result = _library.FromBase64ToString(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromBase64ToString_EmptyValue_ReturnsNull()
    {
        var result = _library.FromBase64ToString(string.Empty);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromBase64ToString_WithEncoding_NullValue_ReturnsNull()
    {
        var result = _library.FromBase64ToString(null, "UTF-8");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromBase64ToString_WithEncoding_EmptyValue_ReturnsNull()
    {
        var result = _library.FromBase64ToString(string.Empty, "UTF-8");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromBase64ToString_WithEncoding_Converts()
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("Hello"));
        var result = _library.FromBase64ToString(encoded, "UTF-8");
        Assert.AreEqual("Hello", result);
    }

    #endregion

    #region FromHexToBytes - All Branches

    [TestMethod]
    public void FromHexToBytes_NullValue_ReturnsNull()
    {
        var result = _library.FromHexToBytes(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromHexToBytes_EmptyValue_ReturnsNull()
    {
        var result = _library.FromHexToBytes(string.Empty);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromHexToBytes_WithSpaces_Converts()
    {
        var result = _library.FromHexToBytes("48 65 6C 6C 6F");
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello", Encoding.ASCII.GetString(result));
    }

    [TestMethod]
    public void FromHexToBytes_WithDashes_Converts()
    {
        var result = _library.FromHexToBytes("48-65-6C-6C-6F");
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello", Encoding.ASCII.GetString(result));
    }

    [TestMethod]
    public void FromHexToBytes_WithColons_Converts()
    {
        var result = _library.FromHexToBytes("48:65:6C:6C:6F");
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello", Encoding.ASCII.GetString(result));
    }

    [TestMethod]
    public void FromHexToBytes_With0xPrefix_Converts()
    {
        var result = _library.FromHexToBytes("0x48656C6C6F");
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello", Encoding.ASCII.GetString(result));
    }

    [TestMethod]
    public void FromHexToBytes_With0XPrefix_Converts()
    {
        var result = _library.FromHexToBytes("0X48656C6C6F");
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello", Encoding.ASCII.GetString(result));
    }

    [TestMethod]
    public void FromHexToBytes_OddLength_ReturnsNull()
    {
        var result = _library.FromHexToBytes("123");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromHexToBytes_InvalidChars_ReturnsNull()
    {
        var result = _library.FromHexToBytes("ZZZZ");
        Assert.IsNull(result);
    }

    #endregion
}
