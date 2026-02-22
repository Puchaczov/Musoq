using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Tests for ToHex, FromHex, FromBytes conversion methods to improve branch coverage.
/// </summary>
[TestClass]
public class ConversionMethodsTests : LibraryBaseBaseTests
{
    #region FromBytesToUInt16 Tests

    [TestMethod]
    public void FromBytesToUInt16_Value()
    {
        ushort expected = 65535;
        var result = Library.FromBytesToUInt16(BitConverter.GetBytes(expected));
        Assert.AreEqual(expected, result);
    }

    #endregion

    #region FromBytesToUInt32 Tests

    [TestMethod]
    public void FromBytesToUInt32_Value()
    {
        var expected = 4294967295u;
        var result = Library.FromBytesToUInt32(BitConverter.GetBytes(expected));
        Assert.AreEqual(expected, result);
    }

    #endregion

    #region FromBytesToUInt64 Tests

    [TestMethod]
    public void FromBytesToUInt64_Value()
    {
        var expected = 18446744073709551615UL;
        var result = Library.FromBytesToUInt64(BitConverter.GetBytes(expected));
        Assert.AreEqual(expected, result);
    }

    #endregion

    #region ToHex(byte[], delimiter) Tests

    [TestMethod]
    public void ToHex_ByteArray_Null_ReturnsNull()
    {
        var result = Library.ToHex(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToHex_ByteArray_Empty_ReturnsEmpty()
    {
        var result = Library.ToHex(Array.Empty<byte>());
        Assert.IsNotNull(result);
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void ToHex_ByteArray_SingleByte_NoDelimiter()
    {
        var result = Library.ToHex(new byte[] { 0xFF });
        Assert.AreEqual("FF", result);
    }

    [TestMethod]
    public void ToHex_ByteArray_MultipleBytes_NoDelimiter()
    {
        var result = Library.ToHex(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F });
        Assert.AreEqual("48656C6C6F", result);
    }

    [TestMethod]
    public void ToHex_ByteArray_WithSpaceDelimiter()
    {
        var result = Library.ToHex(new byte[] { 0x48, 0x65, 0x6C }, " ");
        Assert.AreEqual("48 65 6C", result);
    }

    [TestMethod]
    public void ToHex_ByteArray_WithDashDelimiter()
    {
        var result = Library.ToHex(new byte[] { 0x48, 0x65, 0x6C }, "-");
        Assert.AreEqual("48-65-6C", result);
    }

    [TestMethod]
    public void ToHex_ByteArray_WithColonDelimiter()
    {
        var result = Library.ToHex(new byte[] { 0x48, 0x65, 0x6C }, ":");
        Assert.AreEqual("48:65:6C", result);
    }

    #endregion

    #region ToHex<T>(T value) Generic Tests

    [TestMethod]
    public void ToHex_Boolean_True()
    {
        var result = Library.ToHex(true);
        Assert.IsNotNull(result);
        Assert.AreEqual(Library.ToHex(BitConverter.GetBytes(true)), result);
    }

    [TestMethod]
    public void ToHex_Boolean_False()
    {
        var result = Library.ToHex(false);
        Assert.IsNotNull(result);
        Assert.AreEqual(Library.ToHex(BitConverter.GetBytes(false)), result);
    }

    [TestMethod]
    public void ToHex_Byte()
    {
        var result = Library.ToHex((byte)255);
        Assert.IsNotNull(result);

        Assert.AreEqual(Library.ToHex(Library.GetBytes(255)), result);
    }

    [TestMethod]
    public void ToHex_Char()
    {
        var result = Library.ToHex('A');
        Assert.IsNotNull(result);
        Assert.AreEqual(Library.ToHex(BitConverter.GetBytes('A')), result);
    }

    [TestMethod]
    public void ToHex_Int16()
    {
        var result = Library.ToHex((short)1234);
        Assert.IsNotNull(result);
        Assert.AreEqual(Library.ToHex(BitConverter.GetBytes((short)1234)), result);
    }

    [TestMethod]
    public void ToHex_Int32()
    {
        var result = Library.ToHex(123456);
        Assert.IsNotNull(result);
        Assert.AreEqual(Library.ToHex(BitConverter.GetBytes(123456)), result);
    }

    [TestMethod]
    public void ToHex_Int64()
    {
        var result = Library.ToHex(123456789L);
        Assert.IsNotNull(result);
        Assert.AreEqual(Library.ToHex(BitConverter.GetBytes(123456789L)), result);
    }

    [TestMethod]
    public void ToHex_SByte()
    {
        var result = Library.ToHex((sbyte)-1);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ToHex_UInt16()
    {
        var result = Library.ToHex((ushort)65535);
        Assert.IsNotNull(result);
        Assert.AreEqual(Library.ToHex(BitConverter.GetBytes((ushort)65535)), result);
    }

    [TestMethod]
    public void ToHex_UInt32()
    {
        var result = Library.ToHex(123456u);
        Assert.IsNotNull(result);
        Assert.AreEqual(Library.ToHex(BitConverter.GetBytes(123456u)), result);
    }

    [TestMethod]
    public void ToHex_UInt64()
    {
        var result = Library.ToHex(123456789UL);
        Assert.IsNotNull(result);
        Assert.AreEqual(Library.ToHex(BitConverter.GetBytes(123456789UL)), result);
    }

    [TestMethod]
    public void ToHex_Single()
    {
        var result = Library.ToHex(3.14f);
        Assert.IsNotNull(result);
        Assert.AreEqual(Library.ToHex(BitConverter.GetBytes(3.14f)), result);
    }

    [TestMethod]
    public void ToHex_Double()
    {
        var result = Library.ToHex(3.14159);
        Assert.IsNotNull(result);
        Assert.AreEqual(Library.ToHex(BitConverter.GetBytes(3.14159)), result);
    }

    [TestMethod]
    public void ToHex_Decimal()
    {
        var result = Library.ToHex(123.456m);
        Assert.IsNotNull(result);
        Assert.AreEqual(Library.ToHex(Library.GetBytes(123.456m)), result);
    }

    [TestMethod]
    public void ToHex_String()
    {
        var result = Library.ToHex("Hello");
        Assert.IsNotNull(result);
        Assert.AreEqual(Library.ToHex(Encoding.UTF8.GetBytes("Hello")), result);
    }

    [TestMethod]
    public void ToHex_DateTime_ReturnsNotSupported()
    {
        var result = Library.ToHex(DateTime.Now);
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    [TestMethod]
    public void ToHex_DBNull_ReturnsNotSupported()
    {
        var result = Library.ToHex(DBNull.Value);
        Assert.AreEqual("CONVERSION_NOT_SUPPORTED", result);
    }

    #endregion

    #region FromHexToBytes Tests

    [TestMethod]
    public void FromHexToBytes_Null_ReturnsNull()
    {
        var result = Library.FromHexToBytes(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromHexToBytes_Empty_ReturnsNull()
    {
        var result = Library.FromHexToBytes("");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromHexToBytes_ValidHex()
    {
        var result = Library.FromHexToBytes("48656C6C6F");
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello", Encoding.UTF8.GetString(result));
    }

    [TestMethod]
    public void FromHexToBytes_WithSpaces()
    {
        var result = Library.FromHexToBytes("48 65 6C 6C 6F");
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello", Encoding.UTF8.GetString(result));
    }

    [TestMethod]
    public void FromHexToBytes_WithDashes()
    {
        var result = Library.FromHexToBytes("48-65-6C-6C-6F");
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello", Encoding.UTF8.GetString(result));
    }

    [TestMethod]
    public void FromHexToBytes_WithColons()
    {
        var result = Library.FromHexToBytes("48:65:6C:6C:6F");
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello", Encoding.UTF8.GetString(result));
    }

    [TestMethod]
    public void FromHexToBytes_With0xPrefix()
    {
        var result = Library.FromHexToBytes("0x48656C6C6F");
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello", Encoding.UTF8.GetString(result));
    }

    [TestMethod]
    public void FromHexToBytes_With0XPrefix()
    {
        var result = Library.FromHexToBytes("0X48656C6C6F");
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello", Encoding.UTF8.GetString(result));
    }

    [TestMethod]
    public void FromHexToBytes_OddLength_ReturnsNull()
    {
        var result = Library.FromHexToBytes("48656C6C6");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromHexToBytes_InvalidHex_ReturnsNull()
    {
        var result = Library.FromHexToBytes("ZZZZ");
        Assert.IsNull(result);
    }

    #endregion

    #region FromHexToString Tests

    [TestMethod]
    public void FromHexToString_Null_ReturnsNull()
    {
        var result = Library.FromHexToString(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromHexToString_ValidHex()
    {
        var result = Library.FromHexToString("48656C6C6F");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void FromHexToString_InvalidHex_ReturnsNull()
    {
        var result = Library.FromHexToString("ZZZZ");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromHexToString_WithEncoding_Null_ReturnsNull()
    {
        var result = Library.FromHexToString(null, "UTF-8");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void FromHexToString_WithEncoding_ValidHex()
    {
        var result = Library.FromHexToString("48656C6C6F", "UTF-8");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void FromHexToString_WithEncoding_ASCII()
    {
        var result = Library.FromHexToString("48656C6C6F", "ASCII");
        Assert.AreEqual("Hello", result);
    }

    #endregion

    #region ToHexFromString Tests

    [TestMethod]
    public void ToHexFromString_Null_ReturnsNull()
    {
        var result = Library.ToHexFromString(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToHexFromString_ValidString()
    {
        var result = Library.ToHexFromString("Hello");
        Assert.AreEqual("48656C6C6F", result);
    }

    [TestMethod]
    public void ToHexFromString_WithEncoding_Null_ReturnsNull()
    {
        var result = Library.ToHexFromString(null, "UTF-8");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToHexFromString_WithEncoding_UTF8()
    {
        var result = Library.ToHexFromString("Hello", "UTF-8");
        Assert.AreEqual("48656C6C6F", result);
    }

    [TestMethod]
    public void ToHexFromString_WithEncoding_ASCII()
    {
        var result = Library.ToHexFromString("Hello", "ASCII");
        Assert.AreEqual("48656C6C6F", result);
    }

    #endregion

    #region FromBytesToBool Tests

    [TestMethod]
    public void FromBytesToBool_True()
    {
        var result = Library.FromBytesToBool(BitConverter.GetBytes(true));
        Assert.IsTrue(result.HasValue);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void FromBytesToBool_False()
    {
        var result = Library.FromBytesToBool(BitConverter.GetBytes(false));
        Assert.IsTrue(result.HasValue);
        Assert.IsFalse(result.Value);
    }

    #endregion

    #region FromBytesToInt16 Tests

    [TestMethod]
    public void FromBytesToInt16_Positive()
    {
        short expected = 12345;
        var result = Library.FromBytesToInt16(BitConverter.GetBytes(expected));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void FromBytesToInt16_Negative()
    {
        short expected = -12345;
        var result = Library.FromBytesToInt16(BitConverter.GetBytes(expected));
        Assert.AreEqual(expected, result);
    }

    #endregion

    #region FromBytesToInt32 Tests

    [TestMethod]
    public void FromBytesToInt32_Positive()
    {
        var expected = 123456789;
        var result = Library.FromBytesToInt32(BitConverter.GetBytes(expected));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void FromBytesToInt32_Negative()
    {
        var expected = -123456789;
        var result = Library.FromBytesToInt32(BitConverter.GetBytes(expected));
        Assert.AreEqual(expected, result);
    }

    #endregion

    #region FromBytesToInt64 Tests

    [TestMethod]
    public void FromBytesToInt64_Positive()
    {
        var expected = 9223372036854775807L;
        var result = Library.FromBytesToInt64(BitConverter.GetBytes(expected));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void FromBytesToInt64_Negative()
    {
        var expected = -9223372036854775808L;
        var result = Library.FromBytesToInt64(BitConverter.GetBytes(expected));
        Assert.AreEqual(expected, result);
    }

    #endregion

    #region FromBytesToFloat Tests

    [TestMethod]
    public void FromBytesToFloat_Positive()
    {
        var expected = 3.14f;
        var result = Library.FromBytesToFloat(BitConverter.GetBytes(expected));
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(expected, result.Value, 0.0001f);
    }

    [TestMethod]
    public void FromBytesToFloat_Negative()
    {
        var expected = -3.14f;
        var result = Library.FromBytesToFloat(BitConverter.GetBytes(expected));
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(expected, result.Value, 0.0001f);
    }

    #endregion

    #region FromBytesToDouble Tests

    [TestMethod]
    public void FromBytesToDouble_Positive()
    {
        var expected = 3.14159265358979;
        var result = Library.FromBytesToDouble(BitConverter.GetBytes(expected));
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(expected, result.Value, 0.0000000001);
    }

    [TestMethod]
    public void FromBytesToDouble_Negative()
    {
        var expected = -3.14159265358979;
        var result = Library.FromBytesToDouble(BitConverter.GetBytes(expected));
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(expected, result.Value, 0.0000000001);
    }

    #endregion

    #region FromBytesToString Tests

    [TestMethod]
    public void FromBytesToString_UTF8()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello World");
        var result = Library.FromBytesToString(bytes);
        Assert.AreEqual("Hello World", result);
    }

    [TestMethod]
    public void FromBytesToString_Empty()
    {
        var result = Library.FromBytesToString(Array.Empty<byte>());
        Assert.AreEqual("", result);
    }

    #endregion

    #region ToText Tests

    [TestMethod]
    public void ToText_Null_ReturnsEmpty()
    {
        var result = Library.ToText(null!, "utf-8");
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void ToText_Empty_ReturnsEmpty()
    {
        var result = Library.ToText(Array.Empty<byte>(), "utf-8");
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void ToText_UTF8()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello");
        var result = Library.ToText(bytes, "utf-8");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_UTF8_Alt()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello");
        var result = Library.ToText(bytes, "utf8");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_UTF16()
    {
        var bytes = Encoding.Unicode.GetBytes("Hello");
        var result = Library.ToText(bytes, "utf-16");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_UTF16_Alt()
    {
        var bytes = Encoding.Unicode.GetBytes("Hello");
        var result = Library.ToText(bytes, "utf16");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_Unicode()
    {
        var bytes = Encoding.Unicode.GetBytes("Hello");
        var result = Library.ToText(bytes, "unicode");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_UTF16LE()
    {
        var bytes = Encoding.Unicode.GetBytes("Hello");
        var result = Library.ToText(bytes, "utf-16le");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_UTF16LE_Alt()
    {
        var bytes = Encoding.Unicode.GetBytes("Hello");
        var result = Library.ToText(bytes, "utf16le");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_UTF16BE()
    {
        var bytes = Encoding.BigEndianUnicode.GetBytes("Hello");
        var result = Library.ToText(bytes, "utf-16be");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_UTF16BE_Alt()
    {
        var bytes = Encoding.BigEndianUnicode.GetBytes("Hello");
        var result = Library.ToText(bytes, "utf16be");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_ASCII()
    {
        var bytes = Encoding.ASCII.GetBytes("Hello");
        var result = Library.ToText(bytes, "ascii");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_Latin1()
    {
        var bytes = Encoding.Latin1.GetBytes("Hello");
        var result = Library.ToText(bytes, "latin1");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_ISO88591()
    {
        var bytes = Encoding.Latin1.GetBytes("Hello");
        var result = Library.ToText(bytes, "iso-8859-1");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_UnknownEncoding_DefaultsToUTF8()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello");
        var result = Library.ToText(bytes, "unknown-encoding");
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public void ToText_NullEncoding_DefaultsToUTF8()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello");
        var result = Library.ToText(bytes, null!);
        Assert.AreEqual("Hello", result);
    }

    #endregion
}
