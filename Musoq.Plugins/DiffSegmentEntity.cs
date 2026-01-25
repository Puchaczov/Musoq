namespace Musoq.Plugins;

/// <summary>
///     Represents a segment of a character-level diff result.
/// </summary>
public class DiffSegmentEntity
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DiffSegmentEntity" /> class.
    /// </summary>
    /// <param name="text">The segment content.</param>
    /// <param name="kind">The kind of segment (Unchanged, Deleted, or Inserted).</param>
    /// <param name="position">The start position in the source (for Deleted/Unchanged) or target (for Inserted).</param>
    /// <param name="length">The character count.</param>
    public DiffSegmentEntity(string text, string kind, int position, int length)
    {
        Text = text;
        Kind = kind;
        Position = position;
        Length = length;
    }

    /// <summary>
    ///     Gets the segment content.
    /// </summary>
    public string Text { get; }

    /// <summary>
    ///     Gets the kind of segment: "Unchanged", "Deleted", or "Inserted".
    /// </summary>
    public string Kind { get; }

    /// <summary>
    ///     Gets the start position (in source for Deleted/Unchanged, in target for Inserted).
    /// </summary>
    public int Position { get; }

    /// <summary>
    ///     Gets the character count.
    /// </summary>
    public int Length { get; }
}
