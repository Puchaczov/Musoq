using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class FromBytesMethodsTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void FromBytesToBool_ShouldReturnBoolean()
    {
        var trueBytes = BitConverter.GetBytes(true);
        var result = Library.FromBytesToBool(trueBytes);
        Assert.IsTrue(result.HasValue);
        Assert.IsTrue(result.Value);

        var falseBytes = BitConverter.GetBytes(false);
        result = Library.FromBytesToBool(falseBytes);
        Assert.IsTrue(result.HasValue);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void FromBytesToInt16_ShouldReturnShort()
    {
        var bytes = BitConverter.GetBytes((short)32767);
        var result = Library.FromBytesToInt16(bytes);
        Assert.AreEqual<short?>(32767, result);
    }

    [TestMethod]
    public void FromBytesToInt16_WithNegativeValue_ShouldReturnNegativeShort()
    {
        var bytes = BitConverter.GetBytes((short)-32768);
        var result = Library.FromBytesToInt16(bytes);
        Assert.AreEqual<short?>(-32768, result);
    }

    [TestMethod]
    public void FromBytesToUInt16_ShouldReturnUShort()
    {
        var bytes = BitConverter.GetBytes((ushort)65535);
        var result = Library.FromBytesToUInt16(bytes);
        Assert.AreEqual<ushort?>(65535, result);
    }

    [TestMethod]
    public void FromBytesToUInt16_WithSmallValue_ShouldReturnCorrectUShort()
    {
        var bytes = BitConverter.GetBytes((ushort)123);
        var result = Library.FromBytesToUInt16(bytes);
        Assert.AreEqual<ushort?>(123, result);
    }

    [TestMethod]
    public void FromBytesToInt32_ShouldReturnInt()
    {
        var bytes = BitConverter.GetBytes(2147483647);
        var result = Library.FromBytesToInt32(bytes);
        Assert.AreEqual<int?>(2147483647, result);
    }

    [TestMethod]
    public void FromBytesToInt32_WithNegativeValue_ShouldReturnNegativeInt()
    {
        var bytes = BitConverter.GetBytes(-2147483648);
        var result = Library.FromBytesToInt32(bytes);
        Assert.AreEqual<int?>(-2147483648, result);
    }

    [TestMethod]
    public void FromBytesToInt32_WithZero_ShouldReturnZero()
    {
        var bytes = BitConverter.GetBytes(0);
        var result = Library.FromBytesToInt32(bytes);
        Assert.AreEqual<int?>(0, result);
    }

    [TestMethod]
    public void FromBytesToUInt32_ShouldReturnUInt()
    {
        var bytes = BitConverter.GetBytes(4294967295U);
        var result = Library.FromBytesToUInt32(bytes);
        Assert.AreEqual<uint?>(4294967295U, result);
    }

    [TestMethod]
    public void FromBytesToUInt32_WithSmallValue_ShouldReturnCorrectUInt()
    {
        var bytes = BitConverter.GetBytes(12345U);
        var result = Library.FromBytesToUInt32(bytes);
        Assert.AreEqual<uint?>(12345U, result);
    }

    [TestMethod]
    public void FromBytesToInt64_ShouldReturnLong()
    {
        var bytes = BitConverter.GetBytes(9223372036854775807L);
        var result = Library.FromBytesToInt64(bytes);
        Assert.AreEqual<long?>(9223372036854775807L, result);
    }

    [TestMethod]
    public void FromBytesToInt64_WithNegativeValue_ShouldReturnNegativeLong()
    {
        var bytes = BitConverter.GetBytes(-9223372036854775808L);
        var result = Library.FromBytesToInt64(bytes);
        Assert.AreEqual<long?>(-9223372036854775808L, result);
    }

    [TestMethod]
    public void FromBytesToInt64_WithSmallValue_ShouldReturnCorrectLong()
    {
        var bytes = BitConverter.GetBytes(123456789L);
        var result = Library.FromBytesToInt64(bytes);
        Assert.AreEqual<long?>(123456789L, result);
    }

    [TestMethod]
    public void FromBytesToUInt64_ShouldReturnULong()
    {
        var bytes = BitConverter.GetBytes(18446744073709551615UL);
        var result = Library.FromBytesToUInt64(bytes);
        Assert.AreEqual<ulong?>(18446744073709551615UL, result);
    }

    [TestMethod]
    public void FromBytesToUInt64_WithSmallValue_ShouldReturnCorrectULong()
    {
        var bytes = BitConverter.GetBytes(987654321UL);
        var result = Library.FromBytesToUInt64(bytes);
        Assert.AreEqual<ulong?>(987654321UL, result);
    }

    [TestMethod]
    public void FromBytesToFloat_ShouldReturnFloat()
    {
        var bytes = BitConverter.GetBytes(123.456f);
        var result = Library.FromBytesToFloat(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(123.456f, result.Value, 0.001f);
    }

    [TestMethod]
    public void FromBytesToFloat_WithNegativeValue_ShouldReturnNegativeFloat()
    {
        var bytes = BitConverter.GetBytes(-789.123f);
        var result = Library.FromBytesToFloat(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(-789.123f, result.Value, 0.001f);
    }

    [TestMethod]
    public void FromBytesToFloat_WithZero_ShouldReturnZero()
    {
        var bytes = BitConverter.GetBytes(0.0f);
        var result = Library.FromBytesToFloat(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(0.0f, result.Value, 0.001f);
    }

    [TestMethod]
    public void FromBytesToFloat_WithInfinity_ShouldReturnInfinity()
    {
        var bytes = BitConverter.GetBytes(float.PositiveInfinity);
        var result = Library.FromBytesToFloat(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(float.PositiveInfinity, result.Value);
    }

    [TestMethod]
    public void FromBytesToFloat_WithNaN_ShouldReturnNaN()
    {
        var bytes = BitConverter.GetBytes(float.NaN);
        var result = Library.FromBytesToFloat(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.IsTrue(float.IsNaN(result.Value));
    }

    [TestMethod]
    public void FromBytesToDouble_ShouldReturnDouble()
    {
        var bytes = BitConverter.GetBytes(123.456789);
        var result = Library.FromBytesToDouble(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(123.456789, result.Value, 0.000001);
    }

    [TestMethod]
    public void FromBytesToDouble_WithNegativeValue_ShouldReturnNegativeDouble()
    {
        var bytes = BitConverter.GetBytes(-987.654321);
        var result = Library.FromBytesToDouble(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(-987.654321, result.Value, 0.000001);
    }

    [TestMethod]
    public void FromBytesToDouble_WithZero_ShouldReturnZero()
    {
        var bytes = BitConverter.GetBytes(0.0);
        var result = Library.FromBytesToDouble(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(0.0, result.Value, 0.000001);
    }

    [TestMethod]
    public void FromBytesToDouble_WithInfinity_ShouldReturnInfinity()
    {
        var bytes = BitConverter.GetBytes(double.PositiveInfinity);
        var result = Library.FromBytesToDouble(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(double.PositiveInfinity, result.Value);
    }

    [TestMethod]
    public void FromBytesToDouble_WithNaN_ShouldReturnNaN()
    {
        var bytes = BitConverter.GetBytes(double.NaN);
        var result = Library.FromBytesToDouble(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.IsTrue(double.IsNaN(result.Value));
    }

    [TestMethod]
    public void FromBytesToString_ShouldReturnString()
    {
        var text = "Hello, World!";
        var bytes = Encoding.UTF8.GetBytes(text);
        var result = Library.FromBytesToString(bytes);
        Assert.AreEqual(text, result);
    }

    [TestMethod]
    public void FromBytesToString_WithEmptyBytes_ShouldReturnEmptyString()
    {
        var bytes = new byte[0];
        var result = Library.FromBytesToString(bytes);
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void FromBytesToString_WithUnicodeText_ShouldReturnCorrectString()
    {
        var text = "Hello 世界 🌍";
        var bytes = Encoding.UTF8.GetBytes(text);
        var result = Library.FromBytesToString(bytes);
        Assert.AreEqual(text, result);
    }

    [TestMethod]
    public void FromBytesToString_WithSpecialCharacters_ShouldReturnCorrectString()
    {
        var text = "Line1\nLine2\tTab\r\nWindows";
        var bytes = Encoding.UTF8.GetBytes(text);
        var result = Library.FromBytesToString(bytes);
        Assert.AreEqual(text, result);
    }

    [TestMethod]
    public void FromBytesToString_WithNumbers_ShouldReturnNumbersAsString()
    {
        var text = "123456789";
        var bytes = Encoding.UTF8.GetBytes(text);
        var result = Library.FromBytesToString(bytes);
        Assert.AreEqual(text, result);
    }

    [TestMethod]
    public void FromBytesToString_WithJsonString_ShouldReturnJsonString()
    {
        var text = "{\"name\":\"John\",\"age\":30}";
        var bytes = Encoding.UTF8.GetBytes(text);
        var result = Library.FromBytesToString(bytes);
        Assert.AreEqual(text, result);
    }

    [TestMethod]
    public void ConversionRoundTrip_BoolToBytes_ShouldBeReversible()
    {
        var original = true;
        var bytes = BitConverter.GetBytes(original);
        var result = Library.FromBytesToBool(bytes);
        Assert.AreEqual<bool?>(original, result);
    }

    [TestMethod]
    public void ConversionRoundTrip_IntToBytes_ShouldBeReversible()
    {
        var original = 123456789;
        var bytes = BitConverter.GetBytes(original);
        var result = Library.FromBytesToInt32(bytes);
        Assert.AreEqual<int?>(original, result);
    }

    [TestMethod]
    public void ConversionRoundTrip_DoubleToBytes_ShouldBeReversible()
    {
        var original = 123.456789;
        var bytes = BitConverter.GetBytes(original);
        var result = Library.FromBytesToDouble(bytes);
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(original, result.Value, 0.000001);
    }

    [TestMethod]
    public void FromBytesToBool_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToBool(null);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToBool_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToBool(new byte[0]);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToInt16_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToInt16(null);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToInt16_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToInt16(new byte[1]);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToUInt16_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToUInt16(null);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToUInt16_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToUInt16(new byte[1]);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToInt32_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToInt32(null);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToInt32_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToInt32(new byte[3]);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToUInt32_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToUInt32(null);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToUInt32_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToUInt32(new byte[3]);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToInt64_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToInt64(null);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToInt64_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToInt64(new byte[7]);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToUInt64_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToUInt64(null);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToUInt64_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToUInt64(new byte[7]);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToFloat_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToFloat(null);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToFloat_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToFloat(new byte[3]);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToDouble_WithNull_ShouldReturnNull()
    {
        var result = Library.FromBytesToDouble(null);
        Assert.IsFalse(result.HasValue);
    }

    [TestMethod]
    public void FromBytesToDouble_WithInsufficientBytes_ShouldReturnNull()
    {
        var result = Library.FromBytesToDouble(new byte[7]);
        Assert.IsFalse(result.HasValue);
    }
}
