namespace Musoq.Schema.Interpreters;

public abstract partial class BytesInterpreterBase<TOut>
{
    /// <summary>Aligns the cursor, returning expected position errors without throwing.</summary>
    protected ParseFailure? AlignWithStatus(int bits)
    {
        if (bits <= 0)
            return ParseFailure.ForBytes(ParseErrorCode.InvalidSize, SchemaName, _currentFieldName, ParsePosition,
                $"Alignment boundary must be at least 1 bit, got {bits}");
        var current = (long)ParsePosition * 8 + BitOffset;
        var remainder = current % bits;
        var target = remainder == 0 ? current : current + bits - remainder;
        if (target > (long)int.MaxValue * 8 + 7)
            return ParseFailure.ForBytes(ParseErrorCode.InvalidPosition, SchemaName, _currentFieldName, ParsePosition,
                $"Alignment boundary {bits} advances beyond the supported parse position");
        ParsePosition = (int)(target / 8);
        BitOffset = (int)(target % 8);
        return null;
    }

    /// <summary>Checks a bit read while preserving the cursor consumed by a truncated read.</summary>
    protected ParseFailure? PrepareBitsStatus(ReadOnlySpan<byte> data, int count)
    {
        if (count is < 1 or > 64)
            return ParseFailure.ForBytes(ParseErrorCode.InvalidSize, SchemaName, _currentFieldName, ParsePosition,
                $"Bit count must be between 1 and 64, got {count}");
        if (ParsePosition < 0 || ParsePosition >= data.Length)
            return InsufficientByteInput(1, data.Length);
        if (((long)data.Length - ParsePosition) * 8 - BitOffset >= count)
            return null;
        ParsePosition = data.Length;
        BitOffset = 0;
        return InsufficientByteInput(1, data.Length);
    }

    /// <summary>Applies a generated seek while retaining its established validation order.</summary>
    protected ParseFailure? SeekWithStatus(int position)
    {
        if (position < 0)
            return ParseFailure.ForBytes(ParseErrorCode.InvalidPosition, SchemaName, null, position,
                $"Cannot seek to negative position: {position}");
        ParsePosition = position;
        BitOffset = 0;
        return null;
    }

    /// <summary>Checks a field validation without throwing for an ordinary mismatch.</summary>
    protected ParseFailure? ValidateWithStatus(bool condition, string fieldName, string message) => condition ? null :
        ParseFailure.ForBytes(ParseErrorCode.ValidationFailed, SchemaName, fieldName, ParsePosition, message);
}
