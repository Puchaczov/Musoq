namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Represents a suggested action to fix or address a diagnostic.
/// </summary>
public sealed class DiagnosticAction
{
    /// <summary>
    ///     Creates a new diagnostic action.
    /// </summary>
    /// <param name="title">The display title of the action.</param>
    /// <param name="kind">The kind of action.</param>
    /// <param name="textEdit">The text edit to apply, if any.</param>
    public DiagnosticAction(string title, DiagnosticActionKind kind, TextEdit? textEdit = null)
    {
        Title = title;
        Kind = kind;
        TextEdit = textEdit;
    }

    /// <summary>
    ///     Gets the display title of the action.
    /// </summary>
    public string Title { get; }

    /// <summary>
    ///     Gets the kind of action.
    /// </summary>
    public DiagnosticActionKind Kind { get; }

    /// <summary>
    ///     Gets the text edit to apply, if any.
    /// </summary>
    public TextEdit? TextEdit { get; }

    /// <summary>
    ///     Creates a quick fix action.
    /// </summary>
    public static DiagnosticAction QuickFix(string title, TextSpan span, string newText)
    {
        return new DiagnosticAction(title, DiagnosticActionKind.QuickFix, new TextEdit(span, newText));
    }

    /// <summary>
    ///     Creates a refactoring action.
    /// </summary>
    public static DiagnosticAction Refactor(string title, TextSpan span, string newText)
    {
        return new DiagnosticAction(title, DiagnosticActionKind.Refactor, new TextEdit(span, newText));
    }

    /// <summary>
    ///     Creates a suggestion action with no automatic fix.
    /// </summary>
    public static DiagnosticAction Suggestion(string title)
    {
        return new DiagnosticAction(title, DiagnosticActionKind.Suggestion);
    }
}
