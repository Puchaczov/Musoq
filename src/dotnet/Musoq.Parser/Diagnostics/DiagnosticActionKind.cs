namespace Musoq.Parser.Diagnostics;

/// <summary>
///     The kind of diagnostic action.
/// </summary>
public enum DiagnosticActionKind
{
    /// <summary>
    ///     A quick fix that can be applied automatically.
    /// </summary>
    QuickFix,

    /// <summary>
    ///     A refactoring suggestion.
    /// </summary>
    Refactor,

    /// <summary>
    ///     A manual suggestion or hint.
    /// </summary>
    Suggestion
}
