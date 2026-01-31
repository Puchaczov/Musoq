using System;
using System.Diagnostics;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser;

/// <summary>
///     This class allows to store association between source text and parsed tree.
///     Every single token must contains TextSpan to allow determine which part of source it concers
/// </summary>
[DebuggerDisplay("Start: {Start}, Length: {Length}, End: {End}")]
public struct TextSpan : IEquatable<TextSpan>, IComparable<TextSpan>
{
    /// <summary>
    ///     An empty span at position 0.
    /// </summary>
    public static TextSpan Empty => new(0, 0);

    /// <summary>
    ///     Initialize instance.
    /// </summary>
    /// <param name="start">The start.</param>
    /// <param name="lenght">The length.</param>
    public TextSpan(int start, int lenght)
    {
        Start = start;
        Length = lenght;
    }

    /// <summary>
    ///     Returns end of span string.
    /// </summary>
    public int End => Start + Length;

    /// <summary>
    ///     Lenght of span text.
    /// </summary>
    public int Length { get; }

    /// <summary>
    ///     Point somewhere in source code.
    /// </summary>
    public int Start { get; }

    /// <summary>
    ///     Returns true if this span is empty (zero length).
    /// </summary>
    public bool IsEmpty => Length == 0;

    /// <summary>
    ///     Creates a span that covers from this span through another span.
    /// </summary>
    /// <param name="other">The other span to extend through.</param>
    /// <returns>A new span covering both spans.</returns>
    public TextSpan Through(TextSpan other)
    {
        if (IsEmpty) return other;
        if (other.IsEmpty) return this;

        var start = Math.Min(Start, other.Start);
        var end = Math.Max(End, other.End);
        return new TextSpan(start, end - start);
    }

    /// <summary>
    ///     Creates a span that covers the intersection of two spans.
    /// </summary>
    public TextSpan? Intersection(TextSpan other)
    {
        var start = Math.Max(Start, other.Start);
        var end = Math.Min(End, other.End);

        if (start >= end)
            return null;

        return new TextSpan(start, end - start);
    }

    /// <summary>
    ///     Returns true if this span contains the given position.
    /// </summary>
    public bool Contains(int position)
    {
        return position >= Start && position < End;
    }

    /// <summary>
    ///     Returns true if this span fully contains another span.
    /// </summary>
    public bool Contains(TextSpan other)
    {
        return Start <= other.Start && End >= other.End;
    }

    /// <summary>
    ///     Returns true if this span overlaps with another span.
    /// </summary>
    public bool Overlaps(TextSpan other)
    {
        return Start < other.End && other.Start < End;
    }

    /// <summary>
    ///     Gets the source location for the start of this span.
    /// </summary>
    /// <param name="sourceText">The source text.</param>
    /// <returns>The source location.</returns>
    public SourceLocation GetStartLocation(SourceText sourceText)
    {
        return sourceText.GetLocation(Start);
    }

    /// <summary>
    ///     Gets the source location for the end of this span.
    /// </summary>
    /// <param name="sourceText">The source text.</param>
    /// <returns>The source location.</returns>
    public SourceLocation GetEndLocation(SourceText sourceText)
    {
        return sourceText.GetLocation(End);
    }

    /// <summary>
    ///     Creates a new span with the same start but different length.
    /// </summary>
    public TextSpan WithLength(int newLength)
    {
        return new TextSpan(Start, newLength);
    }

    /// <summary>
    ///     Creates a new span with the same length but different start.
    /// </summary>
    public TextSpan WithStart(int newStart)
    {
        return new TextSpan(newStart, Length);
    }

    /// <summary>
    ///     Creates a span from start and end positions.
    /// </summary>
    public static TextSpan FromBounds(int start, int end)
    {
        return new TextSpan(start, end - start);
    }

    /// <summary>
    ///     Compares this span to another for ordering.
    /// </summary>
    public int CompareTo(TextSpan other)
    {
        var startComparison = Start.CompareTo(other.Start);
        return startComparison != 0 ? startComparison : Length.CompareTo(other.Length);
    }

    /// <summary>
    ///     The difference comparsion operator.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True if both are not equal, otherwise false.</returns>
    public static bool operator !=(TextSpan left, TextSpan right)
    {
        return !(left == right);
    }

    /// <summary>
    ///     The equality comparsion operator.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True if both are equal, otherwise false.</returns>
    public static bool operator ==(TextSpan left, TextSpan right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Performs equality comparsion with object.
    /// </summary>
    /// <param name="obj">The object.</param>
    /// <returns>True if both are equals, otherwise false.</returns>
    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;

        if (obj is TextSpan span) return span.Start == Start && span.Length == Length;

        return base.Equals(obj);
    }

    /// <summary>
    ///     Performs equality comparison with another TextSpan.
    /// </summary>
    public bool Equals(TextSpan other)
    {
        return Start == other.Start && Length == other.Length;
    }

    /// <summary>
    ///     Computes the hashcode.
    /// </summary>
    /// <returns>The hashcode.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Start, Length);
    }

    /// <summary>
    ///     Returns a string representation of the span.
    /// </summary>
    public override string ToString()
    {
        return $"[{Start}..{End})";
    }
}
