using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator;

internal static class SemanticDiagnosticFactory
{
    public static Diagnostic Create(
        DiagnosticCode code,
        string message,
        TextSpan? span,
        SourceText? sourceText,
        IEnumerable<KeyValuePair<string, string>>? arguments = null,
        IReadOnlyList<DiagnosticAction>? explicitActions = null)
    {
        var metadata = ErrorMetadataCatalog.Get(code);
        var actions = explicitActions is { Count: > 0 }
            ? explicitActions
            : metadata?.SuggestedFixes.Select(DiagnosticAction.Suggestion).ToArray();
        var (location, endLocation) = GetLocations(span, sourceText);
        var contextSnippet = span is { } knownSpan && sourceText != null && location.IsValid
            ? sourceText.GetContextSnippet(knownSpan)
            : null;

        return new Diagnostic(
            code,
            DiagnosticSeverity.Error,
            message,
            location,
            endLocation,
            contextSnippet,
            suggestedFixes: actions,
            explanation: metadata?.Explanation,
            docsReference: metadata?.DocsReference,
            phase: metadata?.Phase,
            arguments: arguments);
    }

    private static (SourceLocation Start, SourceLocation End) GetLocations(
        TextSpan? span,
        SourceText? sourceText)
    {
        if (span is not { } knownSpan)
            return (SourceLocation.None, SourceLocation.None);

        return sourceText != null
            ? sourceText.GetLocations(knownSpan)
            : (new SourceLocation(knownSpan.Start, 1, knownSpan.Start + 1),
                new SourceLocation(knownSpan.End, 1, knownSpan.End + 1));
    }
}
