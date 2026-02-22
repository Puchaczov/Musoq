#nullable enable

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Musoq.Schema.Interpreters;

/// <summary>
///     Abstract base class for binary data interpreters.
///     Generated interpreter classes inherit from this class.
/// </summary>
/// <typeparam name="TOut">The type of the parsed result object.</typeparam>
public abstract class BytesInterpreterBase<TOut> : IBytesInterpreter<TOut>
{
    /// <summary>
    ///     Current _parsePosition in the byte array during interpretation.
    /// </summary>
    protected int _parsePosition;

    /// <summary>
    ///     Current bit offset within the current byte (0-7).
    ///     Used for bit field parsing.
    /// </summary>
    protected int BitOffset;

    /// <inheritdoc />
    public abstract string SchemaName { get; }

    /// <inheritdoc />
    public int BytesConsumed => _parsePosition;

    /// <inheritdoc />
    public TOut Interpret(ReadOnlySpan<byte> data)
    {
        return InterpretAt(data, 0);
    }

    /// <inheritdoc />
    public TOut Interpret(byte[] data)
    {
        return Interpret(data.AsSpan());
    }

    /// <inheritdoc />
    public abstract TOut InterpretAt(ReadOnlySpan<byte> data, int offset);

    /// <inheritdoc />
    public bool TryInterpret(ReadOnlySpan<byte> data, out TOut? result)
    {
        try
        {
            result = Interpret(data);
            return true;
        }
        catch (ParseException)
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    ///     Interprets binary data with partial result capture for debugging malformed data.
    /// </summary>
    /// <param name="data">The binary data to interpret.</param>
    /// <returns>A PartialInterpretResult containing either the full result or error information with partial fields.</returns>
    public virtual PartialInterpretResult<TOut> PartialInterpret(ReadOnlySpan<byte> data)
    {
        var parsedFields = new Dictionary<string, object?>();

        try
        {
            var result = Interpret(data);


            foreach (var property in typeof(TOut).GetProperties())
                if (property.CanRead)
                    parsedFields[property.Name] = property.GetValue(result);

            return new PartialInterpretResult<TOut>(result, parsedFields, BytesConsumed);
        }
        catch (ParseException ex)
        {
            return new PartialInterpretResult<TOut>(parsedFields, BytesConsumed, ex.FieldName ?? "Unknown", ex.Message);
        }
        catch (Exception ex)
        {
            return new PartialInterpretResult<TOut>(parsedFields, BytesConsumed, "Unknown", ex.Message);
        }
    }

    /// <summary>
    ///     Interprets binary data with partial result capture for debugging malformed data.
    /// </summary>
    /// <param name="data">The binary data to interpret.</param>
    /// <returns>A PartialInterpretResult containing either the full result or error information with partial fields.</returns>
    public PartialInterpretResult<TOut> PartialInterpret(byte[] data)
    {
        return PartialInterpret(data.AsSpan());
    }

    #region Primitive Reading Helpers

    /// <summary>
    ///     Reads a single byte and advances the _parsePosition.
    /// </summary>
    protected byte ReadByte(ReadOnlySpan<byte> data)
    {
        if (_parsePosition >= data.Length)
            ThrowInsufficientData(1, data.Length);
        return data[_parsePosition++];
    }

    /// <summary>
    ///     Reads a signed byte and advances the _parsePosition.
    /// </summary>
    protected sbyte ReadSByte(ReadOnlySpan<byte> data)
    {
        if (_parsePosition >= data.Length)
            ThrowInsufficientData(1, data.Length);
        return (sbyte)data[_parsePosition++];
    }

    private void ThrowInsufficientData(int count, int dataLength)
    {
        throw new ParseException(
            ParseErrorCode.InsufficientData,
            SchemaName,
            null,
            _parsePosition,
            $"Attempted to read {count} bytes at _parsePosition {_parsePosition}, but only {dataLength - _parsePosition} bytes available");
    }

    /// <summary>
    ///     Reads a 16-bit signed integer in little-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected short ReadInt16LE(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 2);
        var value = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(_parsePosition, 2));
        _parsePosition += 2;
        return value;
    }

    /// <summary>
    ///     Reads a 16-bit signed integer in big-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected short ReadInt16BE(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 2);
        var value = BinaryPrimitives.ReadInt16BigEndian(data.Slice(_parsePosition, 2));
        _parsePosition += 2;
        return value;
    }

    /// <summary>
    ///     Reads a 16-bit unsigned integer in little-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ushort ReadUInt16LE(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 2);
        var value = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(_parsePosition, 2));
        _parsePosition += 2;
        return value;
    }

    /// <summary>
    ///     Reads a 16-bit unsigned integer in big-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ushort ReadUInt16BE(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 2);
        var value = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(_parsePosition, 2));
        _parsePosition += 2;
        return value;
    }

    /// <summary>
    ///     Reads a 32-bit signed integer in little-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected int ReadInt32LE(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 4);
        var value = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(_parsePosition, 4));
        _parsePosition += 4;
        return value;
    }

    /// <summary>
    ///     Reads a 32-bit signed integer in big-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected int ReadInt32BE(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 4);
        var value = BinaryPrimitives.ReadInt32BigEndian(data.Slice(_parsePosition, 4));
        _parsePosition += 4;
        return value;
    }

    /// <summary>
    ///     Reads a 32-bit unsigned integer in little-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected uint ReadUInt32LE(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 4);
        var value = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(_parsePosition, 4));
        _parsePosition += 4;
        return value;
    }

    /// <summary>
    ///     Reads a 32-bit unsigned integer in big-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected uint ReadUInt32BE(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 4);
        var value = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(_parsePosition, 4));
        _parsePosition += 4;
        return value;
    }

    /// <summary>
    ///     Reads a 64-bit signed integer in little-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected long ReadInt64LE(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 8);
        var value = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(_parsePosition, 8));
        _parsePosition += 8;
        return value;
    }

    /// <summary>
    ///     Reads a 64-bit signed integer in big-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected long ReadInt64BE(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 8);
        var value = BinaryPrimitives.ReadInt64BigEndian(data.Slice(_parsePosition, 8));
        _parsePosition += 8;
        return value;
    }

    /// <summary>
    ///     Reads a 64-bit unsigned integer in little-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ulong ReadUInt64LE(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 8);
        var value = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(_parsePosition, 8));
        _parsePosition += 8;
        return value;
    }

    /// <summary>
    ///     Reads a 64-bit unsigned integer in big-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ulong ReadUInt64BE(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 8);
        var value = BinaryPrimitives.ReadUInt64BigEndian(data.Slice(_parsePosition, 8));
        _parsePosition += 8;
        return value;
    }

    /// <summary>
    ///     Reads a 32-bit float in little-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected float ReadSingleLE(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 4);
        var value = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(_parsePosition, 4));
        _parsePosition += 4;
        return value;
    }

    /// <summary>
    ///     Reads a 32-bit float in big-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected float ReadSingleBE(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 4);
        var value = BinaryPrimitives.ReadSingleBigEndian(data.Slice(_parsePosition, 4));
        _parsePosition += 4;
        return value;
    }

    /// <summary>
    ///     Reads a 64-bit double in little-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected double ReadDoubleLE(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 8);
        var value = BinaryPrimitives.ReadDoubleLittleEndian(data.Slice(_parsePosition, 8));
        _parsePosition += 8;
        return value;
    }

    /// <summary>
    ///     Reads a 64-bit double in big-endian format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected double ReadDoubleBE(ReadOnlySpan<byte> data)
    {
        EnsureBytes(data, 8);
        var value = BinaryPrimitives.ReadDoubleBigEndian(data.Slice(_parsePosition, 8));
        _parsePosition += 8;
        return value;
    }

    /// <summary>
    ///     Reads a byte array of the specified length.
    /// </summary>
    /// <exception cref="ParseException">Thrown when length is negative.</exception>
    protected byte[] ReadBytes(ReadOnlySpan<byte> data, int length)
    {
        if (length < 0)
            throw new ParseException(ParseErrorCode.InvalidSize, SchemaName, null, _parsePosition,
                $"Negative byte array size: {length}");

        EnsureBytes(data, length);
        var result = data.Slice(_parsePosition, length).ToArray();
        _parsePosition += length;
        return result;
    }

    /// <summary>
    ///     Reads a string of the specified byte length using the given encoding.
    /// </summary>
    /// <exception cref="ParseException">Thrown when byteLength is negative.</exception>
    protected string ReadString(ReadOnlySpan<byte> data, int byteLength, Encoding encoding)
    {
        if (byteLength < 0)
            throw new ParseException(ParseErrorCode.InvalidSize, SchemaName, null, _parsePosition,
                $"Negative string size: {byteLength}");

        EnsureBytes(data, byteLength);
        var bytes = data.Slice(_parsePosition, byteLength);
        _parsePosition += byteLength;

        return encoding.GetString(bytes);
    }

    /// <summary>
    ///     Reads a null-terminated string using the given encoding, consuming up to maxBytes.
    /// </summary>
    /// <exception cref="ParseException">Thrown when maxBytes is negative.</exception>
    protected string ReadNullTerminatedString(ReadOnlySpan<byte> data, int maxBytes, Encoding encoding)
    {
        if (maxBytes < 0)
            throw new ParseException(ParseErrorCode.InvalidSize, SchemaName, null, _parsePosition,
                $"Negative max string size: {maxBytes}");

        EnsureBytes(data, maxBytes);
        var bytes = data.Slice(_parsePosition, maxBytes);

        int actualLength;


        if (encoding == Encoding.Unicode || encoding == Encoding.BigEndianUnicode)
        {
            actualLength = maxBytes;
            for (var i = 0; i <= maxBytes - 2; i += 2)
                if (bytes[i] == 0 && bytes[i + 1] == 0)
                {
                    actualLength = i;
                    break;
                }
        }
        else
        {
            var nullIndex = bytes.IndexOf((byte)0);
            actualLength = nullIndex >= 0 ? nullIndex : maxBytes;
        }

        _parsePosition += maxBytes;

        return encoding.GetString(bytes.Slice(0, actualLength));
    }

    #endregion

    #region Bit Field Helpers

    /// <summary>
    ///     Reads the specified number of bits as an unsigned value.
    /// </summary>
    protected ulong ReadBits(ReadOnlySpan<byte> data, int bitCount)
    {
        if (bitCount < 1 || bitCount > 64)
            throw new ParseException(ParseErrorCode.InvalidSize, SchemaName, null, _parsePosition,
                $"Bit count must be between 1 and 64, got {bitCount}");

        ulong result = 0;
        var bitsRead = 0;

        while (bitsRead < bitCount)
        {
            EnsureBytes(data, 1);
            var bitsAvailable = 8 - BitOffset;
            var bitsToRead = Math.Min(bitsAvailable, bitCount - bitsRead);

            var mask = (byte)((1 << bitsToRead) - 1);
            var value = (byte)((data[_parsePosition] >> BitOffset) & mask);

            result |= (ulong)value << bitsRead;
            bitsRead += bitsToRead;
            BitOffset += bitsToRead;

            if (BitOffset >= 8)
            {
                BitOffset = 0;
                _parsePosition++;
            }
        }

        return result;
    }

    /// <summary>
    ///     Aligns the _parsePosition to the specified bit boundary.
    /// </summary>
    protected void AlignToBits(ReadOnlySpan<byte> data, int bits)
    {
        if (bits <= 0 || bits > 64)
            throw new ArgumentOutOfRangeException(nameof(bits), "Alignment must be between 1 and 64 bits");

        if (BitOffset > 0)
        {
            BitOffset = 0;
            _parsePosition++;
        }


        if (bits == 8) return;


        var byteAlignment = bits / 8;
        if (byteAlignment > 0)
        {
            var remainder = _parsePosition % byteAlignment;
            if (remainder > 0) _parsePosition += byteAlignment - remainder;
        }
    }

    #endregion

    #region Validation Helpers

    /// <summary>
    ///     Ensures sufficient bytes are available for reading.
    ///     Uses AggressiveInlining to allow JIT to optimize hot paths.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureBytes(ReadOnlySpan<byte> data, int count)
    {
        if (_parsePosition + count > data.Length)
            ThrowInsufficientData(count, data.Length);
    }

    /// <summary>
    ///     Seeks to an absolute position in the data.
    /// </summary>
    protected void SeekTo(int position)
    {
        if (position < 0)
            throw new ParseException(ParseErrorCode.InvalidPosition, SchemaName, null, position,
                $"Cannot seek to negative position: {position}");

        _parsePosition = position;
        BitOffset = 0;
    }

    /// <summary>
    ///     Validates a condition and throws if it fails.
    /// </summary>
    protected void Validate(bool condition, string fieldName, string message)
    {
        if (!condition)
            throw new ParseException(
                ParseErrorCode.ValidationFailed,
                SchemaName,
                fieldName,
                _parsePosition,
                message);
    }

    #endregion
}
