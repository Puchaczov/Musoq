using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

public partial class BytesInterpreterBaseTests
{
    #region ReadByte Tests

    [TestMethod]
    public void ReadByte_ValidData_ReturnsByte()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadByte("BCD"u8.ToArray());
        Assert.AreEqual(0x42, result);
        Assert.AreEqual(1, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadByte_EmptyData_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadByte(Array.Empty<byte>()));
    }

    [TestMethod]
    public void ReadByte_AtEnd_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.SetPosition(5);
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadByte("BC"u8.ToArray()));
    }

    #endregion

    #region ReadSByte Tests

    [TestMethod]
    public void ReadSByte_PositiveValue_ReturnsSByte()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadSByte([0x42]);
        Assert.AreEqual((sbyte)0x42, result);
    }

    [TestMethod]
    public void ReadSByte_NegativeValue_ReturnsSByte()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadSByte([0xFF]);
        Assert.AreEqual((sbyte)-1, result);
    }

    [TestMethod]
    public void ReadSByte_EmptyData_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadSByte(Array.Empty<byte>()));
    }

    #endregion

    #region ReadInt16 Tests

    [TestMethod]
    public void ReadInt16LE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadInt16Le([0x01, 0x02]);
        Assert.AreEqual((short)0x0201, result);
        Assert.AreEqual(2, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadInt16BE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadInt16Be([0x01, 0x02]);
        Assert.AreEqual((short)0x0102, result);
    }

    [TestMethod]
    public void ReadInt16LE_InsufficientData_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadInt16Le([0x01]));
    }

    #endregion

    #region ReadUInt16 Tests

    [TestMethod]
    public void ReadUInt16LE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadUInt16Le([0xFF, 0xFF]);
        Assert.AreEqual((ushort)0xFFFF, result);
    }

    [TestMethod]
    public void ReadUInt16BE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadUInt16Be([0x00, 0x01]);
        Assert.AreEqual((ushort)0x0001, result);
    }

    #endregion

    #region ReadInt32 Tests

    [TestMethod]
    public void ReadInt32LE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadInt32Le([0x01, 0x02, 0x03, 0x04]);
        Assert.AreEqual(0x04030201, result);
        Assert.AreEqual(4, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadInt32BE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadInt32Be([0x01, 0x02, 0x03, 0x04]);
        Assert.AreEqual(0x01020304, result);
    }

    [TestMethod]
    public void ReadInt32LE_InsufficientData_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadInt32Le([0x01, 0x02]));
    }

    #endregion

    #region ReadUInt32 Tests

    [TestMethod]
    public void ReadUInt32LE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadUInt32Le([0xFF, 0xFF, 0xFF, 0xFF]);
        Assert.AreEqual(0xFFFFFFFF, result);
    }

    [TestMethod]
    public void ReadUInt32BE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadUInt32Be([0x00, 0x00, 0x00, 0x01]);
        Assert.AreEqual(1u, result);
    }

    #endregion

    #region ReadInt64 Tests

    [TestMethod]
    public void ReadInt64LE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadInt64Le([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]);
        Assert.AreEqual(0x0807060504030201L, result);
        Assert.AreEqual(8, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadInt64BE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadInt64Be([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]);
        Assert.AreEqual(0x0102030405060708L, result);
    }

    [TestMethod]
    public void ReadInt64LE_InsufficientData_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadInt64Le([0x01, 0x02, 0x03, 0x04]));
    }

    #endregion

    #region ReadUInt64 Tests

    [TestMethod]
    public void ReadUInt64LE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadUInt64Le([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
        Assert.AreEqual(0xFFFFFFFFFFFFFFFF, result);
    }

    [TestMethod]
    public void ReadUInt64BE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadUInt64Be([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01]);
        Assert.AreEqual(1uL, result);
    }

    #endregion

    #region ReadSingle Tests

    [TestMethod]
    public void ReadSingleLE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();

        var result = interpreter.TestReadSingleLe([0x00, 0x00, 0x80, 0x3F]);
        Assert.AreEqual(1.0f, result);
        Assert.AreEqual(4, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadSingleBE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();

        var result = interpreter.TestReadSingleBe([0x3F, 0x80, 0x00, 0x00]);
        Assert.AreEqual(1.0f, result);
    }

    [TestMethod]
    public void ReadSingleLE_InsufficientData_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadSingleLe("\u0000\u0000"u8.ToArray()));
    }

    #endregion

    #region ReadDouble Tests

    [TestMethod]
    public void ReadDoubleLE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();

        var result = interpreter.TestReadDoubleLe([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F]);
        Assert.AreEqual(1.0, result);
        Assert.AreEqual(8, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadDoubleBE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();

        var result = interpreter.TestReadDoubleBe([0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        Assert.AreEqual(1.0, result);
    }

    [TestMethod]
    public void ReadDoubleLE_InsufficientData_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadDoubleLe("\u0000\u0000\u0000\u0000"u8.ToArray()));
    }

    #endregion
}
