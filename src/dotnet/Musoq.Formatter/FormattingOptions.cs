namespace Musoq.Formatter;

/// <summary>Immutable presentation options for complete Musoq scripts.</summary>
public sealed record FormattingOptions
{
    /// <summary>Spaces per indentation level. Must be positive; defaults to four.</summary>
    public int IndentSize { get; init; } = 4;
    /// <summary>Preferred line length in UTF-16 code units. Must be positive; defaults to 100.</summary>
    public int LineWidth { get; init; } = 100;
    /// <summary>Structural keyword spelling. Identifiers, types, and literals remain unchanged.</summary>
    public KeywordCasing KeywordCasing { get; init; } = KeywordCasing.Upper;
    /// <summary>Generated line endings. Preserve selects the first external newline, with LF fallback.</summary>
    public LineEnding LineEnding { get; init; } = LineEnding.Preserve;

    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(IndentSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(LineWidth);
        if (!Enum.IsDefined(KeywordCasing))
            throw new ArgumentOutOfRangeException(nameof(KeywordCasing));
        if (!Enum.IsDefined(LineEnding))
            throw new ArgumentOutOfRangeException(nameof(LineEnding));
    }
}
