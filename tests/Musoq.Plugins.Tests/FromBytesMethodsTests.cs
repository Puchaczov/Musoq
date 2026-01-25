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
        Assert.IsTrue(result);

        var falseBytes = BitConverter.GetBytes(false);
        result = Library.FromBytesToBool(falseBytes);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void FromBytesToInt16_ShouldReturnShort()
    {
        var bytes = BitConverter.GetBytes((short)32767);
        var result = Library.FromBytesToInt16(bytes);
        Assert.AreEqual(32767, result);
    }

    [TestMethod]
    public void FromBytesToInt16_WithNegativeValue_ShouldReturnNegativeShort()
    {
        var bytes = BitConverter.GetBytes((short)-32768);
        var result = Library.FromBytesToInt16(bytes);
        Assert.AreEqual(-32768, result);
    }

    [TestMethod]
    public void FromBytesToUInt16_ShouldReturnUShort()
    {
        var bytes = BitConverter.GetBytes((ushort)65535);
        var result = Library.FromBytesToUInt16(bytes);
        Assert.AreEqual(65535, result);
    }

    [TestMethod]
    public void FromBytesToUInt16_WithSmallValue_ShouldReturnCorrectUShort()
    {
        var bytes = BitConverter.GetBytes((ushort)123);
        var result = Library.FromBytesToUInt16(bytes);
        Assert.AreEqual(123, result);
    }

    [TestMethod]
    public void FromBytesToInt32_ShouldReturnInt()
    {
        var bytes = BitConverter.GetBytes(2147483647);
        var result = Library.FromBytesToInt32(bytes);
        Assert.AreEqual(2147483647, result);
    }

    [TestMethod]
    public void FromBytesToInt32_WithNegativeValue_ShouldReturnNegativeInt()
    {
        var bytes = BitConverter.GetBytes(-2147483648);
        var result = Library.FromBytesToInt32(bytes);
        Assert.AreEqual(-2147483648, result);
    }

    [TestMethod]
    public void FromBytesToInt32_WithZero_ShouldReturnZero()
    {
        var bytes = BitConverter.GetBytes(0);
        var result = Library.FromBytesToInt32(bytes);
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void FromBytesToUInt32_ShouldReturnUInt()
    {
        var bytes = BitConverter.GetBytes(4294967295U);
        var result = Library.FromBytesToUInt32(bytes);
        Assert.AreEqual(4294967295U, result);
    }

    [TestMethod]
    public void FromBytesToUInt32_WithSmallValue_ShouldReturnCorrectUInt()
    {
        var bytes = BitConverter.GetBytes(12345U);
        var result = Library.FromBytesToUInt32(bytes);
        Assert.AreEqual(12345U, result);
    }

    [TestMethod]
    public void FromBytesToInt64_ShouldReturnLong()
    {
        var bytes = BitConverter.GetBytes(9223372036854775807L);
        var result = Library.FromBytesToInt64(bytes);
        Assert.AreEqual(9223372036854775807L, result);
    }

    [TestMethod]
    public void FromBytesToInt64_WithNegativeValue_ShouldReturnNegativeLong()
    {
        var bytes = BitConverter.GetBytes(-9223372036854775808L);
        var result = Library.FromBytesToInt64(bytes);
        Assert.AreEqual(-9223372036854775808L, result);
    }

    [TestMethod]
    public void FromBytesToInt64_WithSmallValue_ShouldReturnCorrectLong()
    {
        var bytes = BitConverter.GetBytes(123456789L);
        var result = Library.FromBytesToInt64(bytes);
        Assert.AreEqual(123456789L, result);
    }

    [TestMethod]
    public void FromBytesToUInt64_ShouldReturnULong()
    {
        var bytes = BitConverter.GetBytes(18446744073709551615UL);
        var result = Library.FromBytesToUInt64(bytes);
        Assert.AreEqual(18446744073709551615UL, result);
    }

    [TestMethod]
    public void FromBytesToUInt64_WithSmallValue_ShouldReturnCorrectULong()
    {
        var bytes = BitConverter.GetBytes(987654321UL);
        var result = Library.FromBytesToUInt64(bytes);
        Assert.AreEqual(987654321UL, result);
    }

    [TestMethod]
    public void FromBytesToFloat_ShouldReturnFloat()
    {
        var bytes = BitConverter.GetBytes(123.456f);
        var result = Library.FromBytesToFloat(bytes);
        Assert.AreEqual(123.456f, result, 0.001f);
    }

    [TestMethod]
    public void FromBytesToFloat_WithNegativeValue_ShouldReturnNegativeFloat()
    {
        var bytes = BitConverter.GetBytes(-789.123f);
        var result = Library.FromBytesToFloat(bytes);
        Assert.AreEqual(-789.123f, result, 0.001f);
    }

    [TestMethod]
    public void FromBytesToFloat_WithZero_ShouldReturnZero()
    {
        var bytes = BitConverter.GetBytes(0.0f);
        var result = Library.FromBytesToFloat(bytes);
        Assert.AreEqual(0.0f, result, 0.001f);
    }

    [TestMethod]
    public void FromBytesToFloat_WithInfinity_ShouldReturnInfinity()
    {
        var bytes = BitConverter.GetBytes(float.PositiveInfinity);
        var result = Library.FromBytesToFloat(bytes);
        Assert.AreEqual(float.PositiveInfinity, result);
    }

    [TestMethod]
    public void FromBytesToFloat_WithNaN_ShouldReturnNaN()
    {
        var bytes = BitConverter.GetBytes(float.NaN);
        var result = Library.FromBytesToFloat(bytes);
        Assert.IsTrue(float.IsNaN(result));
    }

    [TestMethod]
    public void FromBytesToDouble_ShouldReturnDouble()
    {
        var bytes = BitConverter.GetBytes(123.456789);
        var result = Library.FromBytesToDouble(bytes);
        Assert.AreEqual(123.456789, result, 0.000001);
    }

    [TestMethod]
    public void FromBytesToDouble_WithNegativeValue_ShouldReturnNegativeDouble()
    {
        var bytes = BitConverter.GetBytes(-987.654321);
        var result = Library.FromBytesToDouble(bytes);
        Assert.AreEqual(-987.654321, result, 0.000001);
    }

    [TestMethod]
    public void FromBytesToDouble_WithZero_ShouldReturnZero()
    {
        var bytes = BitConverter.GetBytes(0.0);
        var result = Library.FromBytesToDouble(bytes);
        Assert.AreEqual(0.0, result, 0.000001);
    }

    [TestMethod]
    public void FromBytesToDouble_WithInfinity_ShouldReturnInfinity()
    {
        var bytes = BitConverter.GetBytes(double.PositiveInfinity);
        var result = Library.FromBytesToDouble(bytes);
        Assert.AreEqual(double.PositiveInfinity, result);
    }

    [TestMethod]
    public void FromBytesToDouble_WithNaN_ShouldReturnNaN()
    {
        var bytes = BitConverter.GetBytes(double.NaN);
        var result = Library.FromBytesToDouble(bytes);
        Assert.IsTrue(double.IsNaN(result));
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
        Assert.AreEqual(original, result);
    }

    [TestMethod]
    public void ConversionRoundTrip_IntToBytes_ShouldBeReversible()
    {
        var original = 123456789;
        var bytes = BitConverter.GetBytes(original);
        var result = Library.FromBytesToInt32(bytes);
        Assert.AreEqual(original, result);
    }

    [TestMethod]
    public void ConversionRoundTrip_DoubleToBytes_ShouldBeReversible()
    {
        var original = 123.456789;
        var bytes = BitConverter.GetBytes(original);
        var result = Library.FromBytesToDouble(bytes);
        Assert.AreEqual(original, result, 0.000001);
    }
}
