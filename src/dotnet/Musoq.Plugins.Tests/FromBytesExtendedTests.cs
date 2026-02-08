using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for FromBytes conversion methods to improve branch coverage
/// </summary>
[TestClass]
public class FromBytesExtendedTests : LibraryBaseBaseTests
{
    #region FromBytesToUInt64 Tests

    [TestMethod]
    public void FromBytesToUInt64_PositiveValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes(18446744073709551615UL);
        var result = Library.FromBytesToUInt64(bytes);
        Assert.AreEqual<ulong?>(18446744073709551615UL, result);
    }

    #endregion

    #region FromBytesToBool Tests

    [TestMethod]
    public void FromBytesToBool_True_ReturnsTrue()
    {
        var bytes = BitConverter.GetBytes(true);
        var result = Library.FromBytesToBool(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void FromBytesToBool_False_ReturnsFalse()
    {
        var bytes = BitConverter.GetBytes(false);
        var result = Library.FromBytesToBool(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.IsFalse(result.Value);
    }

    #endregion

    #region FromBytesToInt16 Tests

    [TestMethod]
    public void FromBytesToInt16_PositiveValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes((short)1234);
        var result = Library.FromBytesToInt16(bytes);
        Assert.AreEqual<short?>(1234, result);
    }

    [TestMethod]
    public void FromBytesToInt16_NegativeValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes((short)-1234);
        var result = Library.FromBytesToInt16(bytes);
        Assert.AreEqual<short?>(-1234, result);
    }

    [TestMethod]
    public void FromBytesToInt16_MaxValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes(short.MaxValue);
        var result = Library.FromBytesToInt16(bytes);
        Assert.AreEqual<short?>(short.MaxValue, result);
    }

    [TestMethod]
    public void FromBytesToInt16_MinValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes(short.MinValue);
        var result = Library.FromBytesToInt16(bytes);
        Assert.AreEqual<short?>(short.MinValue, result);
    }

    #endregion

    #region FromBytesToUInt16 Tests

    [TestMethod]
    public void FromBytesToUInt16_PositiveValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes((ushort)54321);
        var result = Library.FromBytesToUInt16(bytes);
        Assert.AreEqual<ushort?>(54321, result);
    }

    [TestMethod]
    public void FromBytesToUInt16_MaxValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes(ushort.MaxValue);
        var result = Library.FromBytesToUInt16(bytes);
        Assert.AreEqual<ushort?>(ushort.MaxValue, result);
    }

    #endregion

    #region FromBytesToInt32 Tests

    [TestMethod]
    public void FromBytesToInt32_PositiveValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes(12345678);
        var result = Library.FromBytesToInt32(bytes);
        Assert.AreEqual<int?>(12345678, result);
    }

    [TestMethod]
    public void FromBytesToInt32_NegativeValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes(-12345678);
        var result = Library.FromBytesToInt32(bytes);
        Assert.AreEqual<int?>(-12345678, result);
    }

    [TestMethod]
    public void FromBytesToInt32_Zero_ReturnsZero()
    {
        var bytes = BitConverter.GetBytes(0);
        var result = Library.FromBytesToInt32(bytes);
        Assert.AreEqual<int?>(0, result);
    }

    #endregion

    #region FromBytesToUInt32 Tests

    [TestMethod]
    public void FromBytesToUInt32_PositiveValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes(3000000000u);
        var result = Library.FromBytesToUInt32(bytes);
        Assert.AreEqual<uint?>(3000000000u, result);
    }

    [TestMethod]
    public void FromBytesToUInt32_MaxValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes(uint.MaxValue);
        var result = Library.FromBytesToUInt32(bytes);
        Assert.AreEqual<uint?>(uint.MaxValue, result);
    }

    #endregion

    #region FromBytesToInt64 Tests

    [TestMethod]
    public void FromBytesToInt64_PositiveValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes(9223372036854775807L);
        var result = Library.FromBytesToInt64(bytes);
        Assert.AreEqual<long?>(9223372036854775807L, result);
    }

    [TestMethod]
    public void FromBytesToInt64_NegativeValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes(-9223372036854775807L);
        var result = Library.FromBytesToInt64(bytes);
        Assert.AreEqual<long?>(-9223372036854775807L, result);
    }

    #endregion

    #region FromBytesToFloat Tests

    [TestMethod]
    public void FromBytesToFloat_PositiveValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes(3.14f);
        var result = Library.FromBytesToFloat(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(3.14f, result.Value, 0.001f);
    }

    [TestMethod]
    public void FromBytesToFloat_NegativeValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes(-3.14f);
        var result = Library.FromBytesToFloat(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(-3.14f, result.Value, 0.001f);
    }

    [TestMethod]
    public void FromBytesToFloat_Zero_ReturnsZero()
    {
        var bytes = BitConverter.GetBytes(0f);
        var result = Library.FromBytesToFloat(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(0f, result.Value);
    }

    #endregion

    #region FromBytesToDouble Tests

    [TestMethod]
    public void FromBytesToDouble_PositiveValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes(3.14159265358979);
        var result = Library.FromBytesToDouble(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(3.14159265358979, result.Value, 0.0000001);
    }

    [TestMethod]
    public void FromBytesToDouble_NegativeValue_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes(-3.14159265358979);
        var result = Library.FromBytesToDouble(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(-3.14159265358979, result.Value, 0.0000001);
    }

    #endregion

    #region FromBytesToString Tests

    [TestMethod]
    public void FromBytesToString_ValidUtf8_ReturnsCorrectString()
    {
        var original = "Hello, World!";
        var bytes = Encoding.UTF8.GetBytes(original);
        var result = Library.FromBytesToString(bytes);
        Assert.AreEqual(original, result);
    }

    [TestMethod]
    public void FromBytesToString_EmptyBytes_ReturnsEmptyString()
    {
        var bytes = Array.Empty<byte>();
        var result = Library.FromBytesToString(bytes);
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void FromBytesToString_SpecialCharacters_ReturnsCorrectString()
    {
        var original = "Привет мир! 你好世界!";
        var bytes = Encoding.UTF8.GetBytes(original);
        var result = Library.FromBytesToString(bytes);
        Assert.AreEqual(original, result);
    }

    #endregion

    #region ToText Tests (with encoding)

    [TestMethod]
    public void ToText_NullBytes_ReturnsEmptyString()
    {
        var result = Library.ToText(null, "utf-8");
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ToText_EmptyBytes_ReturnsEmptyString()
    {
        var result = Library.ToText(Array.Empty<byte>(), "utf-8");
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ToText_Utf8_ReturnsCorrectString()
    {
        var original = "Hello";
        var bytes = Encoding.UTF8.GetBytes(original);
        var result = Library.ToText(bytes, "utf-8");
        Assert.AreEqual(original, result);
    }

    [TestMethod]
    public void ToText_Utf8Variant_ReturnsCorrectString()
    {
        var original = "Hello";
        var bytes = Encoding.UTF8.GetBytes(original);
        var result = Library.ToText(bytes, "utf8");
        Assert.AreEqual(original, result);
    }

    [TestMethod]
    public void ToText_Ascii_ReturnsCorrectString()
    {
        var original = "Hello";
        var bytes = Encoding.ASCII.GetBytes(original);
        var result = Library.ToText(bytes, "ascii");
        Assert.AreEqual(original, result);
    }

    [TestMethod]
    public void ToText_Unicode_ReturnsCorrectString()
    {
        var original = "Hello";
        var bytes = Encoding.Unicode.GetBytes(original);
        var result = Library.ToText(bytes, "unicode");
        Assert.AreEqual(original, result);
    }

    [TestMethod]
    public void ToText_Utf16_ReturnsCorrectString()
    {
        var original = "Hello";
        var bytes = Encoding.Unicode.GetBytes(original);
        var result = Library.ToText(bytes, "utf-16");
        Assert.AreEqual(original, result);
    }

    [TestMethod]
    public void ToText_Utf16Le_ReturnsCorrectString()
    {
        var original = "Hello";
        var bytes = Encoding.Unicode.GetBytes(original);
        var result = Library.ToText(bytes, "utf-16le");
        Assert.AreEqual(original, result);
    }

    [TestMethod]
    public void ToText_Utf16Be_ReturnsCorrectString()
    {
        var original = "Hello";
        var bytes = Encoding.BigEndianUnicode.GetBytes(original);
        var result = Library.ToText(bytes, "utf-16be");
        Assert.AreEqual(original, result);
    }

    [TestMethod]
    public void ToText_Latin1_ReturnsCorrectString()
    {
        var original = "Hello";
        var bytes = Encoding.Latin1.GetBytes(original);
        var result = Library.ToText(bytes, "latin1");
        Assert.AreEqual(original, result);
    }

    [TestMethod]
    public void ToText_Iso8859_ReturnsCorrectString()
    {
        var original = "Hello";
        var bytes = Encoding.Latin1.GetBytes(original);
        var result = Library.ToText(bytes, "iso-8859-1");
        Assert.AreEqual(original, result);
    }

    [TestMethod]
    public void ToText_UnknownEncoding_DefaultsToUtf8()
    {
        var original = "Hello";
        var bytes = Encoding.UTF8.GetBytes(original);
        var result = Library.ToText(bytes, "unknown-encoding");
        Assert.AreEqual(original, result);
    }

    [TestMethod]
    public void ToText_NullEncoding_DefaultsToUtf8()
    {
        var original = "Hello";
        var bytes = Encoding.UTF8.GetBytes(original);
        var result = Library.ToText(bytes, null);
        Assert.AreEqual(original, result);
    }

    #endregion
}
