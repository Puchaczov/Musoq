namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Represents the severity level of a diagnostic.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    ///     An error that prevents successful compilation/execution.
    /// </summary>
    Error = 1,

    /// <summary>
    ///     A warning about potential issues that don't prevent execution.
    /// </summary>
    Warning = 2,

    /// <summary>
    ///     Informational message about the code.
    /// </summary>
    Info = 3,

    /// <summary>
    ///     A hint or suggestion for improvement.
    /// </summary>
    Hint = 4
}
