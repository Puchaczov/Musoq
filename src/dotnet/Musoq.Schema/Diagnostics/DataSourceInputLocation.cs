namespace Musoq.Schema.Diagnostics;

/// <summary>
/// Identifies a position in datasource input independently from a query source span.
/// Line and column values are one based; byte and character offsets are zero based.
/// </summary>
public sealed record DataSourceInputLocation
{
    public DataSourceInputLocation(
        long? line = null,
        long? column = null,
        long? byteOffset = null,
        long? characterOffset = null,
        long? bytePositionInLine = null)
    {
        ValidateOneBased(line, nameof(line));
        ValidateOneBased(column, nameof(column));
        ValidateZeroBased(byteOffset, nameof(byteOffset));
        ValidateZeroBased(characterOffset, nameof(characterOffset));
        ValidateZeroBased(bytePositionInLine, nameof(bytePositionInLine));

        Line = line;
        Column = column;
        ByteOffset = byteOffset;
        CharacterOffset = characterOffset;
        BytePositionInLine = bytePositionInLine;
    }

    public long? Line { get; }

    public long? Column { get; }

    public long? ByteOffset { get; }

    public long? CharacterOffset { get; }

    public long? BytePositionInLine { get; }

    private static void ValidateOneBased(long? value, string parameterName)
    {
        if (value is <= 0)
            throw new ArgumentOutOfRangeException(parameterName, value, "Line and column values must be one based.");
    }

    private static void ValidateZeroBased(long? value, string parameterName)
    {
        if (value is < 0)
            throw new ArgumentOutOfRangeException(parameterName, value, "Input offsets must be zero based.");
    }
}
