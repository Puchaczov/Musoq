using System.Diagnostics;

namespace Musoq.Parser;

/// <summary>
///     This class allows to store association between source text and parsed tree.
///     Every single token must contains TextSpan to allow determine which part of source it concers
/// </summary>
[DebuggerDisplay("Start: {Start}, Length: {Length}, End: {End}")]
public struct TextSpan
{
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
    ///     Computes the hashcode.
    /// </summary>
    /// <returns>The hashcode.</returns>
    public override int GetHashCode()
    {
        return Start.GetHashCode() ^ Length.GetHashCode();
    }
}