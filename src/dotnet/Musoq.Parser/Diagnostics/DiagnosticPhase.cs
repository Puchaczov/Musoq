namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Represents the compilation/execution phase where a diagnostic originated.
/// </summary>
public enum DiagnosticPhase
{
    /// <summary>
    ///     Lexer or parser phase — tokenization and syntax analysis.
    /// </summary>
    Parse,

    /// <summary>
    ///     Semantic analysis phase — type checking, name resolution, validation.
    /// </summary>
    Bind,

    /// <summary>
    ///     Execution phase — runtime errors during query evaluation.
    /// </summary>
    Runtime,

    /// <summary>
    ///     Data source or schema provider phase — constructor binding, iterator failures.
    /// </summary>
    DataSource,

    /// <summary>
    ///     Feature not available — known limitation flagged explicitly.
    /// </summary>
    FeatureGate
}
