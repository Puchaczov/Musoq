using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Tests for ToHex, FromHex, FromBytes conversion methods to improve branch coverage.
/// </summary>
[TestClass]
public class ConversionMethodsTests : PluginsTestBase
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
    [DynamicData(nameof(ToHexByteArrayCases))]
    public void ToHex_ByteArray_ReturnsExpected(byte[]? bytes, string delimiter, string expected)
    {
        var result = delimiter == null ? Library.ToHex(bytes) : Library.ToHex(bytes, delimiter);

        if (expected == null)
            Assert.IsNull(result);
        else
            Assert.AreEqual(expected, result);
    }

    #endregion

    #region ToHex<T>(T value) Generic Tests

    [TestMethod]
    [DynamicData(nameof(ToHexValueCases))]
    public void ToHex_Value_ReturnsExpected(Func<LibraryBase, string?> actualFactory, Func<LibraryBase, string?> expectedFactory)
    {
        var result = actualFactory(Library);
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedFactory(Library), result);
    }

    [TestMethod]
    public void ToHex_SByte_ReturnsValue()
    {
        Assert.IsNotNull(Library.ToHex((sbyte)-1));
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
    [DynamicData(nameof(FromHexToBytesCases))]
    public void FromHexToBytes_ReturnsExpectedString(string hex, string expected)
    {
        var result = Library.FromHexToBytes(hex);

        if (expected == null)
        {
            Assert.IsNull(result);
            return;
        }

        Assert.IsNotNull(result);
        Assert.AreEqual(expected, Encoding.UTF8.GetString(result));
    }

    #endregion

    #region FromHexToString Tests

    [TestMethod]
    [DataRow(null, null, null)]
    [DataRow("48656C6C6F", null, "Hello")]
    [DataRow("ZZZZ", null, null)]
    [DataRow(null, "UTF-8", null)]
    [DataRow("48656C6C6F", "UTF-8", "Hello")]
    [DataRow("48656C6C6F", "ASCII", "Hello")]
    public void FromHexToString_ReturnsExpected(string? hex, string? encoding, string? expected)
    {
        var result = encoding == null ? Library.FromHexToString(hex) : Library.FromHexToString(hex, encoding);
        Assert.AreEqual(expected, result);
    }

    #endregion

    #region ToHexFromString Tests

    [TestMethod]
    [DataRow(null, null, null)]
    [DataRow("Hello", null, "48656C6C6F")]
    [DataRow(null, "UTF-8", null)]
    [DataRow("Hello", "UTF-8", "48656C6C6F")]
    [DataRow("Hello", "ASCII", "48656C6C6F")]
    public void ToHexFromString_ReturnsExpected(string text, string encoding, string expected)
    {
        var result = encoding == null ? Library.ToHexFromString(text) : Library.ToHexFromString(text, encoding);
        Assert.AreEqual(expected, result);
    }

    #endregion

    #region FromBytesToBool Tests

    [TestMethod]
    public void FromBytesToBool_True()
    {
        var result = Library.FromBytesToBool(BitConverter.GetBytes(true));
        Assert.IsTrue(result.HasValue);
        Assert.IsTrue(result.GetValueOrDefault());
    }

    [TestMethod]
    public void FromBytesToBool_False()
    {
        var result = Library.FromBytesToBool(BitConverter.GetBytes(false));
        Assert.IsTrue(result.HasValue);
        Assert.IsFalse(result.GetValueOrDefault());
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
        Assert.AreEqual(expected, result.GetValueOrDefault(), 0.0001f);
    }

    [TestMethod]
    public void FromBytesToFloat_Negative()
    {
        var expected = -3.14f;
        var result = Library.FromBytesToFloat(BitConverter.GetBytes(expected));
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(expected, result.GetValueOrDefault(), 0.0001f);
    }

    #endregion

    #region FromBytesToDouble Tests

    [TestMethod]
    public void FromBytesToDouble_Positive()
    {
        var expected = 3.14159265358979;
        var result = Library.FromBytesToDouble(BitConverter.GetBytes(expected));
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(expected, result.GetValueOrDefault(), 0.0000000001);
    }

    [TestMethod]
    public void FromBytesToDouble_Negative()
    {
        var expected = -3.14159265358979;
        var result = Library.FromBytesToDouble(BitConverter.GetBytes(expected));
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(expected, result.GetValueOrDefault(), 0.0000000001);
    }

    #endregion

    #region FromBytesToString Tests

    [TestMethod]
    public void FromBytesToString_UTF8()
    {
        var bytes = "Hello World"u8.ToArray();
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
    [DataRow("utf-8", "utf8")]
    [DataRow("utf8", "utf8")]
    [DataRow("utf-16", "utf16")]
    [DataRow("utf16", "utf16")]
    [DataRow("unicode", "utf16")]
    [DataRow("utf-16le", "utf16")]
    [DataRow("utf16le", "utf16")]
    [DataRow("utf-16be", "utf16be")]
    [DataRow("utf16be", "utf16be")]
    [DataRow("ascii", "ascii")]
    [DataRow("latin1", "latin1")]
    [DataRow("iso-8859-1", "latin1")]
    [DataRow("unknown-encoding", "utf8")]
    [DataRow(null, "utf8")]
    public void ToText_WithEncoding_ReturnsHello(string encoding, string byteEncoding)
    {
        var bytes = GetEncodedHello(byteEncoding);
        var result = Library.ToText(bytes, encoding);
        Assert.AreEqual("Hello", result);
    }

    #endregion

    public static IEnumerable<object?[]> ToHexByteArrayCases()
    {
        yield return [null, null, null];
        yield return [Array.Empty<byte>(), null, ""];
        yield return [new byte[] { 0xFF }, null, "FF"];
        yield return ["Hello"u8.ToArray(), null, "48656C6C6F"];
        yield return ["Hel"u8.ToArray(), " ", "48 65 6C"];
        yield return ["Hel"u8.ToArray(), "-", "48-65-6C"];
        yield return ["Hel"u8.ToArray(), ":", "48:65:6C"];
    }

    public static IEnumerable<object[]> ToHexValueCases()
    {
        yield return [Hex((LibraryBase lib) => lib.ToHex(true)), Hex(lib => lib.ToHex(BitConverter.GetBytes(true)))];
        yield return [Hex(lib => lib.ToHex(false)), Hex(lib => lib.ToHex(BitConverter.GetBytes(false)))];
        yield return [Hex(lib => lib.ToHex((byte)255)), Hex(lib => lib.ToHex(lib.GetBytes(255)))];
        yield return [Hex(lib => lib.ToHex('A')), Hex(lib => lib.ToHex(BitConverter.GetBytes('A')))];
        yield return [Hex(lib => lib.ToHex((short)1234)), Hex(lib => lib.ToHex(BitConverter.GetBytes((short)1234)))];
        yield return [Hex(lib => lib.ToHex(123456)), Hex(lib => lib.ToHex(BitConverter.GetBytes(123456)))];
        yield return [Hex(lib => lib.ToHex(123456789L)), Hex(lib => lib.ToHex(BitConverter.GetBytes(123456789L)))];
        yield return [Hex(lib => lib.ToHex((ushort)65535)), Hex(lib => lib.ToHex(BitConverter.GetBytes((ushort)65535)))];
        yield return [Hex(lib => lib.ToHex(123456u)), Hex(lib => lib.ToHex(BitConverter.GetBytes(123456u)))];
        yield return [Hex(lib => lib.ToHex(123456789UL)), Hex(lib => lib.ToHex(BitConverter.GetBytes(123456789UL)))];
        yield return [Hex(lib => lib.ToHex(3.14f)), Hex(lib => lib.ToHex(BitConverter.GetBytes(3.14f)))];
        yield return [Hex(lib => lib.ToHex(3.14159)), Hex(lib => lib.ToHex(BitConverter.GetBytes(3.14159)))];
        yield return [Hex(lib => lib.ToHex(123.456m)), Hex(lib => lib.ToHex(lib.GetBytes(123.456m)))];
        yield return [Hex(lib => lib.ToHex("Hello")), Hex(lib => lib.ToHex("Hello"u8.ToArray()))];
    }

    public static IEnumerable<object?[]> FromHexToBytesCases()
    {
        yield return [null, null];
        yield return ["", null];
        yield return ["48656C6C6F", "Hello"];
        yield return ["48 65 6C 6C 6F", "Hello"];
        yield return ["48-65-6C-6C-6F", "Hello"];
        yield return ["48:65:6C:6C:6F", "Hello"];
        yield return ["0x48656C6C6F", "Hello"];
        yield return ["0X48656C6C6F", "Hello"];
        yield return ["48656C6C6", null];
        yield return ["ZZZZ", null];
    }

    private static Func<LibraryBase, string?> Hex(Func<LibraryBase, string?> factory)
    {
        return factory;
    }

    private static byte[] GetEncodedHello(string encoding)
    {
        return encoding switch
        {
            "utf16" => Encoding.Unicode.GetBytes("Hello"),
            "utf16be" => Encoding.BigEndianUnicode.GetBytes("Hello"),
            "ascii" => "Hello"u8.ToArray(),
            "latin1" => Encoding.Latin1.GetBytes("Hello"),
            _ => "Hello"u8.ToArray()
        };
    }
}
