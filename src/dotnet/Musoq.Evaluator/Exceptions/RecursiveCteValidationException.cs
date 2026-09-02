using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

public sealed class RecursiveCteValidationException(
    DiagnosticCode code,
    string message,
    TextSpan span) : Exception(message), IDiagnosticException
{
    public DiagnosticCode Code { get; } = code;

    public TextSpan? Span { get; } = span;

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? TextSpan.Empty;
        var (location, endLocation) = sourceText is null
            ? (new SourceLocation(span.Start, 1, span.Start + 1),
                new SourceLocation(span.End, 1, span.End + 1))
            : sourceText.GetLocations(span);

        return new Diagnostic(
            Code,
            DiagnosticSeverity.Error,
            Message,
            location,
            endLocation,
            sourceText?.GetContextSnippet(span));
    }
}
