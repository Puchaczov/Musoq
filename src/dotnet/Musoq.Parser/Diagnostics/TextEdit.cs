namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Represents a text edit operation.
/// </summary>
public sealed class TextEdit
{
    /// <summary>
    ///     Creates a new text edit.
    /// </summary>
    /// <param name="span">The span to replace.</param>
    /// <param name="newText">The replacement text.</param>
    public TextEdit(TextSpan span, string newText)
    {
        Span = span;
        NewText = newText;
    }

    /// <summary>
    ///     Gets the span to replace.
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    ///     Gets the replacement text.
    /// </summary>
    public string NewText { get; }

    /// <summary>
    ///     Creates an insertion edit.
    /// </summary>
    public static TextEdit Insert(int position, string text)
    {
        return new TextEdit(new TextSpan(position, 0), text);
    }

    /// <summary>
    ///     Creates a deletion edit.
    /// </summary>
    public static TextEdit Delete(TextSpan span)
    {
        return new TextEdit(span, string.Empty);
    }

    /// <summary>
    ///     Creates a replacement edit.
    /// </summary>
    public static TextEdit Replace(TextSpan span, string newText)
    {
        return new TextEdit(span, newText);
    }
}
