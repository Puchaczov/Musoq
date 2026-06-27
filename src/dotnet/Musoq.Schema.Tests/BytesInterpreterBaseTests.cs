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
public partial class BytesInterpreterBaseTests
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
            ParsePosition = offset;
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

        public short TestReadInt16Le(ReadOnlySpan<byte> data)
        {
            return ReadInt16Le(data);
        }

        public short TestReadInt16Be(ReadOnlySpan<byte> data)
        {
            return ReadInt16Be(data);
        }

        public ushort TestReadUInt16Le(ReadOnlySpan<byte> data)
        {
            return ReadUInt16Le(data);
        }

        public ushort TestReadUInt16Be(ReadOnlySpan<byte> data)
        {
            return ReadUInt16Be(data);
        }

        public int TestReadInt32Le(ReadOnlySpan<byte> data)
        {
            return ReadInt32Le(data);
        }

        public int TestReadInt32Be(ReadOnlySpan<byte> data)
        {
            return ReadInt32Be(data);
        }

        public uint TestReadUInt32Le(ReadOnlySpan<byte> data)
        {
            return ReadUInt32Le(data);
        }

        public uint TestReadUInt32Be(ReadOnlySpan<byte> data)
        {
            return ReadUInt32Be(data);
        }

        public long TestReadInt64Le(ReadOnlySpan<byte> data)
        {
            return ReadInt64Le(data);
        }

        public long TestReadInt64Be(ReadOnlySpan<byte> data)
        {
            return ReadInt64Be(data);
        }

        public ulong TestReadUInt64Le(ReadOnlySpan<byte> data)
        {
            return ReadUInt64Le(data);
        }

        public ulong TestReadUInt64Be(ReadOnlySpan<byte> data)
        {
            return ReadUInt64Be(data);
        }

        public float TestReadSingleLe(ReadOnlySpan<byte> data)
        {
            return ReadSingleLe(data);
        }

        public float TestReadSingleBe(ReadOnlySpan<byte> data)
        {
            return ReadSingleBe(data);
        }

        public double TestReadDoubleLe(ReadOnlySpan<byte> data)
        {
            return ReadDoubleLe(data);
        }

        public double TestReadDoubleBe(ReadOnlySpan<byte> data)
        {
            return ReadDoubleBe(data);
        }

        public byte[] TestReadBytes(ReadOnlySpan<byte> data, int length)
        {
            return ReadBytes(data, length);
        }

        public string? TestReadString(ReadOnlySpan<byte> data, int byteLength, Encoding encoding)
        {
            return ReadString(data, byteLength, encoding);
        }

        public string? TestReadNullTerminatedString(ReadOnlySpan<byte> data, int maxBytes, Encoding encoding)
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
            return ParsePosition;
        }

        public void SetPosition(int pos)
        {
            ParsePosition = pos;
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
}
