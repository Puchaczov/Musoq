namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Represents the compilation/execution phase where a diagnostic originated.
/// </summary>
public enum DiagnosticPhase
{
    /// <summary>
    ///     Lexer or parser phase — tokenization and syntax analysis.
    /// </summary>
    Parse = 1,

    /// <summary>
    ///     Semantic analysis phase — type checking, name resolution, validation.
    /// </summary>
    Bind = 2,

    /// <summary>
    ///     Execution phase — runtime errors during query evaluation.
    /// </summary>
    Runtime = 3,

    /// <summary>
    ///     Data source or schema provider phase — constructor binding, iterator failures.
    /// </summary>
    DataSource = 4,

    /// <summary>
    ///     Feature not available — known limitation flagged explicitly.
    /// </summary>
    FeatureGate = 5,

    /// <summary>Code generation phase.</summary>
    CodeGeneration = 6,

    /// <summary>Schema construction and schema-definition phase.</summary>
    Schema = 7,

    /// <summary>Internal engine phase.</summary>
    Internal = 8
}
