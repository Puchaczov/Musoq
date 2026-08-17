using System.Collections.Generic;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     The immutable definition of one diagnostic code.
/// </summary>
internal sealed record DiagnosticDescriptor(
    DiagnosticCode Code,
    string MessageTemplate,
    DiagnosticSeverity DefaultSeverity,
    DiagnosticPhase DefaultPhase,
    string Category,
    string? Explanation,
    IReadOnlyList<string> SuggestedFixes,
    IReadOnlyList<DiagnosticAction> DefaultActions,
    string? DocsReference)
{
    public ErrorMetadata? Metadata => Explanation == null && DocsReference == null && SuggestedFixes.Count == 0
        ? null
        : new ErrorMetadata(
            Code,
            DefaultPhase,
            Explanation ?? string.Empty,
            [..SuggestedFixes],
            DocsReference ?? string.Empty);
}
