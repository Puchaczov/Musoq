namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Identifies the source domain to which a diagnostic location and its
///     supporting information belong.
/// </summary>
public enum DiagnosticSourceKind
{
    /// <summary>The user's SQL query or script.</summary>
    Query = 1,

    /// <summary>Generated source such as emitted C#.</summary>
    GeneratedSource = 2,

    /// <summary>A schema declaration or schema provider.</summary>
    Schema = 3,

    /// <summary>A data-source lifecycle operation.</summary>
    DataSource = 4,

    /// <summary>Runtime expression or query execution.</summary>
    Runtime = 5,

    /// <summary>An engine-owned invariant or implementation failure.</summary>
    Internal = 6
}
