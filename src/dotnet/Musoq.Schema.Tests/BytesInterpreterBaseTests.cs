using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

/// <summary>
///     Tests for BytesInterpreterBase helper methods to improve branch coverage.
///     Uses a test-specific interpreter class that exposes protected methods.
/// </summary>
[TestClass]
public class BytesInterpreterBaseTests
{
    #region Test Interpreter

    /// <summary>
    ///     Test result class.
    /// </summary>
    public class TestResult
    {
        public int Value { get; set; }
        public string? Name { get; set; }
    }

    /// <summary>
    ///     Test interpreter that exposes protected methods for testing.
    /// </summary>
    private sealed class TestBytesInterpreter : BytesInterpreterBase<TestResult>
    {
        public override string SchemaName => "TestBytesSchema";

        public override TestResult InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            _parsePosition = offset;
            return new TestResult
            {
                Value = data.Length > 0 ? ReadByte(data) : 0,
                Name = "Test"
            };
        }

        // Expose protected methods for testing
        public byte TestReadByte(ReadOnlySpan<byte> data)
        {
            return ReadByte(data);
        }

        public sbyte TestReadSByte(ReadOnlySpan<byte> data)
        {
            return ReadSByte(data);
        }

        public short TestReadInt16LE(ReadOnlySpan<byte> data)
        {
            return ReadInt16LE(data);
        }

        public short TestReadInt16BE(ReadOnlySpan<byte> data)
        {
            return ReadInt16BE(data);
        }

        public ushort TestReadUInt16LE(ReadOnlySpan<byte> data)
        {
            return ReadUInt16LE(data);
        }

        public ushort TestReadUInt16BE(ReadOnlySpan<byte> data)
        {
            return ReadUInt16BE(data);
        }

        public int TestReadInt32LE(ReadOnlySpan<byte> data)
        {
            return ReadInt32LE(data);
        }

        public int TestReadInt32BE(ReadOnlySpan<byte> data)
        {
            return ReadInt32BE(data);
        }

        public uint TestReadUInt32LE(ReadOnlySpan<byte> data)
        {
            return ReadUInt32LE(data);
        }

        public uint TestReadUInt32BE(ReadOnlySpan<byte> data)
        {
            return ReadUInt32BE(data);
        }

        public long TestReadInt64LE(ReadOnlySpan<byte> data)
        {
            return ReadInt64LE(data);
        }

        public long TestReadInt64BE(ReadOnlySpan<byte> data)
        {
            return ReadInt64BE(data);
        }

        public ulong TestReadUInt64LE(ReadOnlySpan<byte> data)
        {
            return ReadUInt64LE(data);
        }

        public ulong TestReadUInt64BE(ReadOnlySpan<byte> data)
        {
            return ReadUInt64BE(data);
        }

        public float TestReadSingleLE(ReadOnlySpan<byte> data)
        {
            return ReadSingleLE(data);
        }

        public float TestReadSingleBE(ReadOnlySpan<byte> data)
        {
            return ReadSingleBE(data);
        }

        public double TestReadDoubleLE(ReadOnlySpan<byte> data)
        {
            return ReadDoubleLE(data);
        }

        public double TestReadDoubleBE(ReadOnlySpan<byte> data)
        {
            return ReadDoubleBE(data);
        }

        public byte[] TestReadBytes(ReadOnlySpan<byte> data, int length)
        {
            return ReadBytes(data, length);
        }

        public string TestReadString(ReadOnlySpan<byte> data, int byteLength, Encoding encoding)
        {
            return ReadString(data, byteLength, encoding);
        }

        public string TestReadNullTerminatedString(ReadOnlySpan<byte> data, int maxBytes, Encoding encoding)
        {
            return ReadNullTerminatedString(data, maxBytes, encoding);
        }

        public ulong TestReadBits(ReadOnlySpan<byte> data, int bitCount)
        {
            return ReadBits(data, bitCount);
        }

        public void TestAlignToBits(ReadOnlySpan<byte> data, int bits)
        {
            AlignToBits(data, bits);
        }

        public void TestEnsureBytes(ReadOnlySpan<byte> data, int count)
        {
            EnsureBytes(data, count);
        }

        public void TestSeekTo(int position)
        {
            SeekTo(position);
        }

        public void TestValidate(bool condition, string fieldName, string message)
        {
            Validate(condition, fieldName, message);
        }

        public int GetPosition()
        {
            return _parsePosition;
        }

        public void SetPosition(int pos)
        {
            _parsePosition = pos;
        }

        public int GetBitOffset()
        {
            return BitOffset;
        }

        public void SetBitOffset(int offset)
        {
            BitOffset = offset;
        }
    }

    #endregion

    #region ReadByte Tests

    [TestMethod]
    public void ReadByte_ValidData_ReturnsByte()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadByte([0x42, 0x43, 0x44]);
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
            interpreter.TestReadByte([0x42, 0x43]));
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
        var result = interpreter.TestReadInt16LE([0x01, 0x02]);
        Assert.AreEqual((short)0x0201, result);
        Assert.AreEqual(2, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadInt16BE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadInt16BE([0x01, 0x02]);
        Assert.AreEqual((short)0x0102, result);
    }

    [TestMethod]
    public void ReadInt16LE_InsufficientData_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadInt16LE([0x01]));
    }

    #endregion

    #region ReadUInt16 Tests

    [TestMethod]
    public void ReadUInt16LE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadUInt16LE([0xFF, 0xFF]);
        Assert.AreEqual((ushort)0xFFFF, result);
    }

    [TestMethod]
    public void ReadUInt16BE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadUInt16BE([0x00, 0x01]);
        Assert.AreEqual((ushort)0x0001, result);
    }

    #endregion

    #region ReadInt32 Tests

    [TestMethod]
    public void ReadInt32LE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadInt32LE([0x01, 0x02, 0x03, 0x04]);
        Assert.AreEqual(0x04030201, result);
        Assert.AreEqual(4, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadInt32BE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadInt32BE([0x01, 0x02, 0x03, 0x04]);
        Assert.AreEqual(0x01020304, result);
    }

    [TestMethod]
    public void ReadInt32LE_InsufficientData_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadInt32LE([0x01, 0x02]));
    }

    #endregion

    #region ReadUInt32 Tests

    [TestMethod]
    public void ReadUInt32LE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadUInt32LE([0xFF, 0xFF, 0xFF, 0xFF]);
        Assert.AreEqual(0xFFFFFFFF, result);
    }

    [TestMethod]
    public void ReadUInt32BE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadUInt32BE([0x00, 0x00, 0x00, 0x01]);
        Assert.AreEqual(1u, result);
    }

    #endregion

    #region ReadInt64 Tests

    [TestMethod]
    public void ReadInt64LE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadInt64LE([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]);
        Assert.AreEqual(0x0807060504030201L, result);
        Assert.AreEqual(8, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadInt64BE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadInt64BE([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]);
        Assert.AreEqual(0x0102030405060708L, result);
    }

    [TestMethod]
    public void ReadInt64LE_InsufficientData_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadInt64LE([0x01, 0x02, 0x03, 0x04]));
    }

    #endregion

    #region ReadUInt64 Tests

    [TestMethod]
    public void ReadUInt64LE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadUInt64LE([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
        Assert.AreEqual(0xFFFFFFFFFFFFFFFF, result);
    }

    [TestMethod]
    public void ReadUInt64BE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadUInt64BE([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01]);
        Assert.AreEqual(1uL, result);
    }

    #endregion

    #region ReadSingle Tests

    [TestMethod]
    public void ReadSingleLE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();

        var result = interpreter.TestReadSingleLE([0x00, 0x00, 0x80, 0x3F]);
        Assert.AreEqual(1.0f, result);
        Assert.AreEqual(4, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadSingleBE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();

        var result = interpreter.TestReadSingleBE([0x3F, 0x80, 0x00, 0x00]);
        Assert.AreEqual(1.0f, result);
    }

    [TestMethod]
    public void ReadSingleLE_InsufficientData_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadSingleLE([0x00, 0x00]));
    }

    #endregion

    #region ReadDouble Tests

    [TestMethod]
    public void ReadDoubleLE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();

        var result = interpreter.TestReadDoubleLE([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F]);
        Assert.AreEqual(1.0, result);
        Assert.AreEqual(8, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadDoubleBE_ValidData_ReturnsValue()
    {
        var interpreter = new TestBytesInterpreter();

        var result = interpreter.TestReadDoubleBE([0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        Assert.AreEqual(1.0, result);
    }

    [TestMethod]
    public void ReadDoubleLE_InsufficientData_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestReadDoubleLE([0x00, 0x00, 0x00, 0x00]));
    }

    #endregion

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
        var result = interpreter.TestReadString(Encoding.ASCII.GetBytes("hello"), 5, Encoding.ASCII);
        Assert.AreEqual("hello", result);
        Assert.AreEqual(5, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadString_UTF8_ReturnsString()
    {
        var interpreter = new TestBytesInterpreter();
        var bytes = Encoding.UTF8.GetBytes("héllo");
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
        var result = interpreter.TestReadNullTerminatedString([0x41, 0x42, 0x00, 0x43, 0x44], 5, Encoding.ASCII);
        Assert.AreEqual("AB", result);
        Assert.AreEqual(5, interpreter.GetPosition());
    }

    [TestMethod]
    public void ReadNullTerminatedString_NoNull_ReadsMaxBytes()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.TestReadNullTerminatedString([0x41, 0x42, 0x43], 3, Encoding.ASCII);
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

    #region AlignToBits Tests

    [TestMethod]
    public void AlignToBits_ByteAlign_ResetsOffset()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.SetBitOffset(5);
        interpreter.TestAlignToBits(new byte[10], 8);
        Assert.AreEqual(0, interpreter.GetBitOffset());
        Assert.AreEqual(1, interpreter.GetPosition());
    }

    [TestMethod]
    public void AlignToBits_AlreadyAligned_NoChange()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.SetBitOffset(0);
        interpreter.TestAlignToBits(new byte[10], 8);
        Assert.AreEqual(0, interpreter.GetPosition());
    }

    [TestMethod]
    public void AlignToBits_16BitAlign_AlignsPosition()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.SetPosition(1);
        interpreter.TestAlignToBits(new byte[10], 16);
        Assert.AreEqual(2, interpreter.GetPosition());
    }

    [TestMethod]
    public void AlignToBits_32BitAlign_AlignsPosition()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.SetPosition(1);
        interpreter.TestAlignToBits(new byte[10], 32);
        Assert.AreEqual(4, interpreter.GetPosition());
    }

    [TestMethod]
    public void AlignToBits_ZeroBits_ThrowsArgumentOutOfRangeException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            interpreter.TestAlignToBits(new byte[10], 0));
    }

    [TestMethod]
    public void AlignToBits_NegativeBits_ThrowsArgumentOutOfRangeException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            interpreter.TestAlignToBits(new byte[10], -1));
    }

    [TestMethod]
    public void AlignToBits_OverMaxBits_ThrowsArgumentOutOfRangeException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            interpreter.TestAlignToBits(new byte[10], 65));
    }

    #endregion

    #region Validate Tests

    [TestMethod]
    public void Validate_ConditionTrue_NoException()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.TestValidate(true, "field", "should not throw");
    }

    [TestMethod]
    public void Validate_ConditionFalse_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        var ex = Assert.Throws<ParseException>(() =>
            interpreter.TestValidate(false, "testField", "validation failed"));
        Assert.AreEqual("testField", ex.FieldName);
        Assert.Contains("validation failed", ex.Message);
    }

    #endregion

    #region Interface Tests

    [TestMethod]
    public void Interpret_FromSpan_Works()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.Interpret(new byte[] { 0x42 });
        Assert.AreEqual(0x42, result.Value);
    }

    [TestMethod]
    public void Interpret_FromArray_Works()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.Interpret(new byte[] { 0x42 });
        Assert.AreEqual(0x42, result.Value);
    }

    [TestMethod]
    public void TryInterpret_Success_ReturnsTrue()
    {
        var interpreter = new TestBytesInterpreter();
        var success = interpreter.TryInterpret(new byte[] { 0x42 }, out var result);
        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        Assert.AreEqual(0x42, result!.Value);
    }

    [TestMethod]
    public void TryInterpret_Empty_ReturnsTrue()
    {
        var interpreter = new TestBytesInterpreter();
        var success = interpreter.TryInterpret(Array.Empty<byte>(), out var result);
        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result!.Value);
    }

    [TestMethod]
    public void BytesConsumed_AfterInterpret_ReturnsCorrectValue()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.Interpret(new byte[] { 0x42 });
        Assert.AreEqual(1, interpreter.BytesConsumed);
    }

    [TestMethod]
    public void PartialInterpret_Success_ReturnsResult()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.PartialInterpret(new byte[] { 0x42 });
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Result);
        Assert.AreEqual(0x42, result.Result!.Value);
        Assert.IsNull(result.ErrorMessage);
        Assert.AreEqual("Test", result.ParsedFields["Name"]);
    }

    [TestMethod]
    public void PartialInterpret_FromArray_ReturnsResult()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.PartialInterpret(new byte[] { 0x42 });
        Assert.IsTrue(result.IsSuccess);
    }

    #endregion

    #region EnsureBytes Tests

    [TestMethod]
    public void EnsureBytes_Enough_NoException()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.TestEnsureBytes([0x01, 0x02, 0x03], 2);
    }

    [TestMethod]
    public void EnsureBytes_NotEnough_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestEnsureBytes([0x01], 10));
    }

    #endregion

    #region SeekTo Tests

    [TestMethod]
    public void SeekTo_ValidPosition_SetsPosition()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.TestSeekTo(5);
        Assert.AreEqual(5, interpreter.GetPosition());
    }

    [TestMethod]
    public void SeekTo_ResetsBitOffset()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.SetBitOffset(4);
        interpreter.TestSeekTo(5);
        Assert.AreEqual(0, interpreter.GetBitOffset());
    }

    #endregion
}
