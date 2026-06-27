using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class FromBytesMethodsTests
{
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
        var bytes = Array.Empty<byte>();
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
        Assert.IsTrue(result.HasValue);
        Assert.AreEqual(original, result.GetValueOrDefault(), 0.000001);
    }
}
