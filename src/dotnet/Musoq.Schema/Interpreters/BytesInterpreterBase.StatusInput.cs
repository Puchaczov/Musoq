using System.Runtime.CompilerServices;

namespace Musoq.Schema.Interpreters;

public abstract partial class BytesInterpreterBase<TOut>
{
    /// <summary>Initializes a generated parse cursor without throwing for an invalid offset.</summary>
    protected ParseFailure? InitializeParsePositionStatus(ReadOnlySpan<byte> data, int offset)
    {
        if ((uint)offset > (uint)data.Length)
            return ParseFailure.ForBytes(
                ParseErrorCode.InvalidPosition,
                SchemaName,
                null,
                offset,
                $"Parse position {offset} is {(offset < 0 ? "negative" : "past the end of the input")}; valid range is 0 through {data.Length}"
            );
        ParsePosition = offset;
        BitOffset = 0;
        return null;
    }

    /// <summary>Prepares a byte read and returns expected bounds errors as data.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ParseFailure? PrepareByteInputStatus(ReadOnlySpan<byte> data, int count)
    {
        if (count > 0 && BitOffset != 0 && AlignWithStatus(8) is { } alignment)
            return alignment;
        return ParsePosition < 0 || count < 0 || ParsePosition > data.Length - count
            ? InsufficientByteInput(count, data.Length)
            : null;
    }

    /// <summary>Consumes the region validated by the immediately preceding byte preparation.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ReadOnlySpan<byte> TakePreparedBytes(ReadOnlySpan<byte> data, int count)
    {
        var bytes = data.Slice(ParsePosition, count);
        ParsePosition += count;
        return bytes;
    }

    private ParseFailure InsufficientByteInput(int count, int length) =>
        ParseFailure.ForBytes(
            ParseErrorCode.InsufficientData,
            SchemaName,
            _currentFieldName,
            ParsePosition,
            $"Attempted to read {count} bytes at parse position {ParsePosition}, but only {Math.Max(0, length - ParsePosition)} bytes available"
        );

    /// <summary>Checks an owned byte field before its read.</summary>
    protected ParseFailure? PrepareByteArrayStatus(ReadOnlySpan<byte> data, int length) =>
        length < 0
            ? ParseFailure.ForBytes(
                ParseErrorCode.InvalidSize,
                SchemaName,
                _currentFieldName,
                ParsePosition,
                $"Negative byte array size: {length}"
            )
            : PrepareByteInputStatus(data, length);

    /// <summary>Checks a sized string field before its read.</summary>
    protected ParseFailure? PrepareStringStatus(ReadOnlySpan<byte> data, int length) =>
        length < 0
            ? ParseFailure.ForBytes(
                ParseErrorCode.InvalidSize,
                SchemaName,
                _currentFieldName,
                ParsePosition,
                $"Negative string size: {length}"
            )
            : PrepareByteInputStatus(data, length);

    /// <summary>Checks a null-terminated string's bounded region before its read.</summary>
    protected ParseFailure? PrepareNullTerminatedStringStatus(ReadOnlySpan<byte> data, int length) =>
        length < 0
            ? ParseFailure.ForBytes(
                ParseErrorCode.InvalidSize,
                SchemaName,
                _currentFieldName,
                ParsePosition,
                $"Negative max string size: {length}"
            )
            : PrepareByteInputStatus(data, length);

    /// <summary>Checks a nested substream's bounded region before its read.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ParseFailure? PrepareSubstreamStatus(ReadOnlySpan<byte> data, int length)
    {
        if (length < 0)
            return ParseFailure.ForBytes(
                ParseErrorCode.InvalidSize,
                SchemaName,
                _currentFieldName,
                ParsePosition,
                $"Negative substream size: {length}"
            );
        if (BitOffset != 0 && AlignWithStatus(8) is { } alignment)
            return alignment;
        return PrepareByteInputStatus(data, length);
    }
}
