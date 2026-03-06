namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Rich metadata for a diagnostic code — provides explanation, suggested fixes, and docs reference.
/// </summary>
/// <param name="Code">The diagnostic code this metadata applies to.</param>
/// <param name="Phase">The compilation phase where this error originates.</param>
/// <param name="Explanation">Plain-language explanation of why this error occurs.</param>
/// <param name="SuggestedFixes">Concrete fix suggestions the user can apply (max 2-3).</param>
/// <param name="DocsReference">Documentation section or page reference.</param>
public sealed record ErrorMetadata(
    DiagnosticCode Code,
    DiagnosticPhase Phase,
    string Explanation,
    string[] SuggestedFixes,
    string DocsReference);
