using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

public partial class BytesInterpreterBaseTests
{
    #region ReadBytes Tests

    [TestMethod]
    public void ReadBytes_ValidLength_ReturnsArray()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadBytes([0x01, 0x02, 0x03, 0x04, 0x05], 3);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, result);
        Assert.AreEqual(3, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadBytes_ZeroLength_ReturnsEmptyArray()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadBytes([0x01, 0x02], 0);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void ReadBytes_NegativeLength_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadBytes([0x01], -1));
    }

    [TestMethod]
    public void ReadBytes_InsufficientData_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadBytes([0x01, 0x02], 10));
    }

    #endregion

    #region ReadString Tests

    [TestMethod]
    public void ReadString_ValidData_ReturnsString()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadString("hello"u8.ToArray(), 5, Encoding.ASCII);
        Assert.AreEqual("hello", result);
        Assert.AreEqual(5, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadString_UTF8_ReturnsString()
    {
        var interpreter = new TestBytesInterpreter();
        var bytes = "héllo"u8.ToArray();
        var result = interpreter.TestReadString(bytes, bytes.Length, Encoding.UTF8);
        Assert.AreEqual("héllo", result);
    }

    [TestMethod]
    public void ReadString_NegativeLength_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadString([0x00], -1, Encoding.ASCII));
    }

    [TestMethod]
    public void ReadString_InsufficientData_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadString([0x41], 10, Encoding.ASCII));
    }

    #endregion

    #region ReadNullTerminatedString Tests

    [TestMethod]
    public void ReadNullTerminatedString_WithNull_ReadsUntilNull()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadNullTerminatedString("AB\u0000CD"u8.ToArray(), 5, Encoding.ASCII);
        Assert.AreEqual("AB", result);
        Assert.AreEqual(5, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadNullTerminatedString_NoNull_ReadsMaxBytes()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadNullTerminatedString("ABC"u8.ToArray(), 3, Encoding.ASCII);
        Assert.AreEqual("ABC", result);
    }

    [TestMethod]
    public void ReadNullTerminatedString_NegativeMaxBytes_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadNullTerminatedString([0x41], -1, Encoding.ASCII));
    }

    [TestMethod]
    public void ReadNullTerminatedString_InsufficientData_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadNullTerminatedString([0x41], 10, Encoding.ASCII));
    }

    #endregion

    #region ReadBits Tests

    [TestMethod]
    public void ReadBits_SingleBit_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadBits([0b00000001], 1);
        Assert.AreEqual(1uL, result);
    }

    [TestMethod]
    public void ReadBits_EightBits_ReturnsByte()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadBits([0xFF], 8);
        Assert.AreEqual(0xFFuL, result);
        Assert.AreEqual(1, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadBits_CrossBytesBoundary_Works()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.SetBitOffset(4);
        var result = interpreter.TestReadBits([0xF0, 0x0F], 8);


        Assert.AreEqual(0xFFuL, result);
    }

    [TestMethod]
    public void ReadBits_TooFewBits_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadBits([0x00], 0));
    }

    [TestMethod]
    public void ReadBits_TooManyBits_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadBits([0x00], 65));
    }

    [TestMethod]
    public void ReadBits_InsufficientData_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.SetPosition(0);
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadBits(Array.Empty<byte>(), 8));
    }

    #endregion
}
