#nullable enable

using System;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Represents a precise location in source code with line and column information.
/// </summary>
public readonly struct SourceLocation : IEquatable<SourceLocation>, IComparable<SourceLocation>
{
    /// <summary>
    ///     An empty/invalid source location.
    /// </summary>
    public static readonly SourceLocation None = new(-1, -1, -1);

    /// <summary>
    ///     Creates a new source location.
    /// </summary>
    /// <param name="offset">The byte offset from the start of the source.</param>
    /// <param name="line">The 1-based line number.</param>
    /// <param name="column">The 1-based column number.</param>
    /// <param name="filePath">Optional file path.</param>
    public SourceLocation(int offset, int line, int column, string? filePath = null)
    {
        Offset = offset;
        Line = line;
        Column = column;
        FilePath = filePath;
    }

    /// <summary>
    ///     Gets the byte offset from the start of the source.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    ///     Gets the 1-based line number.
    /// </summary>
    public int Line { get; }

    /// <summary>
    ///     Gets the 1-based column number.
    /// </summary>
    public int Column { get; }

    /// <summary>
    ///     Gets the optional file path.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    ///     Returns true if this location is valid.
    /// </summary>
    public bool IsValid => Offset >= 0 && Line >= 1 && Column >= 1;

    /// <summary>
    ///     Converts to 0-based line number (for LSP compatibility).
    /// </summary>
    public int Line0 => Line - 1;

    /// <summary>
    ///     Converts to 0-based column number (for LSP compatibility).
    /// </summary>
    public int Column0 => Column - 1;

    /// <summary>
    ///     Creates a new location with a different file path.
    /// </summary>
    public SourceLocation WithFilePath(string? filePath)
    {
        return new SourceLocation(Offset, Line, Column, filePath);
    }

    public override string ToString()
    {
        if (!IsValid)
            return "(unknown)";

        var location = $"({Line},{Column})";
        return string.IsNullOrEmpty(FilePath) ? location : $"{FilePath}{location}";
    }

    /// <summary>
    ///     Returns a string in LSP-compatible format (0-based).
    /// </summary>
    public string ToLspString()
    {
        if (!IsValid)
            return "(unknown)";

        return $"({Line0},{Column0})";
    }

    public bool Equals(SourceLocation other)
    {
        return Offset == other.Offset &&
               Line == other.Line &&
               Column == other.Column &&
               FilePath == other.FilePath;
    }

    public override bool Equals(object? obj)
    {
        return obj is SourceLocation other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Offset, Line, Column, FilePath);
    }

    public int CompareTo(SourceLocation other)
    {
        var offsetComparison = Offset.CompareTo(other.Offset);
        if (offsetComparison != 0) return offsetComparison;

        var lineComparison = Line.CompareTo(other.Line);
        if (lineComparison != 0) return lineComparison;

        return Column.CompareTo(other.Column);
    }

    public static bool operator ==(SourceLocation left, SourceLocation right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SourceLocation left, SourceLocation right)
    {
        return !left.Equals(right);
    }

    public static bool operator <(SourceLocation left, SourceLocation right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(SourceLocation left, SourceLocation right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(SourceLocation left, SourceLocation right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(SourceLocation left, SourceLocation right)
    {
        return left.CompareTo(right) >= 0;
    }
}
