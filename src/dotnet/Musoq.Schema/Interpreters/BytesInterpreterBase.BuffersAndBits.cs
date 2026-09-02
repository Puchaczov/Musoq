using System.Runtime.CompilerServices;
using System.Text;

namespace Musoq.Schema.Interpreters;

/// <summary>
///     Abstract base class for binary data interpreters.
///     Generated interpreter classes inherit from this class.
/// </summary>
/// <typeparam name="TOut">The type of the parsed result object.</typeparam>
public abstract partial class BytesInterpreterBase<TOut>
{
    protected byte[] ReadBytes(ReadOnlySpan<byte> data, int length)
    {
        if (length < 0)
            throw new ParseException(ParseErrorCode.InvalidSize, SchemaName, _currentFieldName, ParsePosition,
                $"Negative byte array size: {length}");

        EnsureBytes(data, length);
        var result = data.Slice(ParsePosition, length).ToArray();
        ParsePosition += length;
        return result;
    }

    /// <summary>
    ///     Returns a bounded slice of <paramref name="length"/> bytes starting at the current
    ///     parse position without advancing it. Used to parse a nested substream payload that
    ///     must not read past its declared length.
    /// </summary>
    /// <exception cref="ParseException">Thrown when length is negative or exceeds available data.</exception>
    protected ReadOnlySpan<byte> ReadSubstreamSlice(ReadOnlySpan<byte> data, int length)
    {
        if (length < 0)
            throw new ParseException(ParseErrorCode.InvalidSize, SchemaName, _currentFieldName, ParsePosition,
                $"Negative substream size: {length}");

        EnsureBytes(data, length);
        return data.Slice(ParsePosition, length);
    }

    /// <summary>
    ///     Validates that a nested substream parser consumed the entire declared length.
    ///     Used by exact-mode substream fields.
    /// </summary>
    /// <exception cref="ParseException">Thrown when the nested parser consumed fewer bytes than declared.</exception>
    protected void EnsureSubstreamFullyConsumed(string fieldName, int length, int consumed)
    {
        if (consumed < length)
            throw new ParseException(ParseErrorCode.ValidationFailed, SchemaName, fieldName, ParsePosition,
                $"Substream '{fieldName}' declared {length} bytes but nested parser consumed only {consumed}.");
    }

    /// <summary>
    ///     Reads a string of the specified byte length using the given encoding.
    /// </summary>
    /// <exception cref="ParseException">Thrown when byteLength is negative.</exception>
    protected string ReadString(ReadOnlySpan<byte> data, int byteLength, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(encoding);
        if (byteLength < 0)
            throw new ParseException(ParseErrorCode.InvalidSize, SchemaName, _currentFieldName, ParsePosition,
                $"Negative string size: {byteLength}");

        EnsureBytes(data, byteLength);
        var startPosition = ParsePosition;
        var bytes = data.Slice(ParsePosition, byteLength);
        var result = DecodeString(bytes, encoding, startPosition);
        ParsePosition += byteLength;
        return result;
    }

    /// <summary>
    ///     Reads a null-terminated string using the given encoding, consuming up to maxBytes.
    /// </summary>
    /// <exception cref="ParseException">Thrown when maxBytes is negative.</exception>
    protected string ReadNullTerminatedString(ReadOnlySpan<byte> data, int maxBytes, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(encoding);
        if (maxBytes < 0)
            throw new ParseException(ParseErrorCode.InvalidSize, SchemaName, _currentFieldName, ParsePosition,
                $"Negative max string size: {maxBytes}");

        EnsureBytes(data, maxBytes);
        var startPosition = ParsePosition;
        var bytes = data.Slice(ParsePosition, maxBytes);

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

        var result = DecodeString(bytes.Slice(0, actualLength), encoding, startPosition);
        ParsePosition += maxBytes;
        return result;
    }

    private string DecodeString(ReadOnlySpan<byte> bytes, Encoding encoding, int startPosition)
    {
        try
        {
            var strictEncoding = (Encoding)encoding.Clone();
            strictEncoding.DecoderFallback = DecoderFallback.ExceptionFallback;
            return strictEncoding.GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            var invalidByteOffset = exception.Index < 0 ? 0 : exception.Index;
            throw new ParseException(
                ParseErrorCode.EncodingError,
                SchemaName,
                _currentFieldName,
                startPosition + invalidByteOffset,
                $"String data is not valid for encoding '{encoding.WebName}'.",
                exception);
        }
    }

    /// <summary>
    ///     Reads the specified number of bits as an unsigned value.
    /// </summary>
    protected ulong ReadBits(ReadOnlySpan<byte> data, int bitCount)
    {
        if (bitCount < 1 || bitCount > 64)
            throw new ParseException(ParseErrorCode.InvalidSize, SchemaName, _currentFieldName, ParsePosition,
                $"Bit count must be between 1 and 64, got {bitCount}");

        ulong result = 0;
        var bitsRead = 0;

        while (bitsRead < bitCount)
        {
            EnsureBytes(data, 1);
            var bitsAvailable = 8 - BitOffset;
            var bitsToRead = Math.Min(bitsAvailable, bitCount - bitsRead);

            var mask = (byte)((1 << bitsToRead) - 1);
            var value = (byte)((data[ParsePosition] >> BitOffset) & mask);

            result |= (ulong)value << bitsRead;
            bitsRead += bitsToRead;
            BitOffset += bitsToRead;

            if (BitOffset >= 8)
            {
                BitOffset = 0;
                ParsePosition++;
            }
        }

        return result;
    }

    /// <summary>
    ///     Aligns the parse position to the specified bit boundary.
    /// </summary>
    protected void AlignToBits(ReadOnlySpan<byte> data, int bits)
    {
        if (bits <= 0)
            throw new ParseException(
                ParseErrorCode.InvalidSize,
                SchemaName,
                _currentFieldName,
                ParsePosition,
                $"Alignment boundary must be at least 1 bit, got {bits}");

        var currentBit = (long)ParsePosition * 8 + BitOffset;
        var remainder = currentBit % bits;
        var targetBit = remainder == 0 ? currentBit : currentBit + bits - remainder;
        var maximumBit = (long)int.MaxValue * 8 + 7;

        if (targetBit > maximumBit)
            throw new ParseException(
                ParseErrorCode.InvalidPosition,
                SchemaName,
                _currentFieldName,
                ParsePosition,
                $"Alignment boundary {bits} advances beyond the supported parse position");

        ParsePosition = (int)(targetBit / 8);
        BitOffset = (int)(targetBit % 8);
    }

    /// <summary>
    ///     Ensures sufficient bytes are available for reading.
    ///     Uses AggressiveInlining to allow JIT to optimize hot paths.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureBytes(ReadOnlySpan<byte> data, int count)
    {
        // Use subtraction instead of ParsePosition + count so an intentionally large
        // length cannot wrap around and bypass the bounds check before Slice is called.
        if (ParsePosition < 0 || count < 0 || ParsePosition > data.Length - count)
            ThrowInsufficientData(count, data.Length);
    }

    /// <summary>
    ///     Returns true when the current parse position has reached the end of the input span.
    ///     Used by <c>repeat until eof</c> fields, where eof means the end of the current
    ///     interpreter input (a bounded substream slice when nested inside a substream).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool IsAtEnd(ReadOnlySpan<byte> data)
    {
        return ParsePosition >= data.Length;
    }

    /// <summary>
    ///     Guards a repeat iteration count. A condition-based repeat must still be bounded when
    ///     its stop condition never becomes true, and the same bound protects EOF repeats whose
    ///     element size is unexpectedly very small.
    /// </summary>
    /// <exception cref="ParseException">Thrown when the implementation-defined repeat limit is reached.</exception>
    protected void EnsureRepeatIteration(string fieldName, int iteration)
    {
        const int maxIterations = 10_000;
        if (iteration < maxIterations)
            return;

        throw new ParseException(
            ParseErrorCode.MaxIterationsExceeded,
            SchemaName,
            fieldName,
            ParsePosition,
            $"Repeat field '{fieldName}' exceeded the maximum of {maxIterations} iterations.");
    }

    /// <summary>
    ///     Guards a <c>repeat until eof</c> iteration against making no forward progress.
    ///     Without this guard a zero-byte element would loop forever on non-empty input.
    /// </summary>
    /// <exception cref="ParseException">
    ///     Thrown when neither the parse position nor the bit offset advanced during an iteration.
    /// </exception>
    protected void EnsureRepeatMadeProgress(string fieldName, int startPosition, int startBitOffset)
    {
        if (ParsePosition == startPosition && BitOffset == startBitOffset)
            throw new ParseException(ParseErrorCode.MaxIterationsExceeded, SchemaName, fieldName, ParsePosition,
                $"Repeat-until-eof field '{fieldName}' made no progress reading an element; the element type consumes zero bytes.");
    }

    /// <summary>
    ///     Seeks to an absolute position in the data.
    /// </summary>
    protected void SeekTo(int position)
    {
        if (position < 0)
            throw new ParseException(ParseErrorCode.InvalidPosition, SchemaName, null, position,
                $"Cannot seek to negative position: {position}");

        ParsePosition = position;
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
                ParsePosition,
                message);
    }

    /// <summary>
    ///     Determines whether a read byte buffer matches an expected byte sequence.
    /// </summary>
    protected static bool BytesEqual(byte[] actual, byte[] expected)
    {
        ArgumentNullException.ThrowIfNull(expected);

        if (actual is null)
            return false;

        return actual.AsSpan().SequenceEqual(expected);
    }
}
