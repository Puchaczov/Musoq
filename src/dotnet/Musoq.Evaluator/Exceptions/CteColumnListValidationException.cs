using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

public sealed class CteColumnListValidationException(
    DiagnosticCode code,
    string message,
    TextSpan span) : Exception(message), IDiagnosticException
{
    public DiagnosticCode Code { get; } = code;

    public TextSpan? Span { get; } = span;

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.Error(Code, Message, Span ?? TextSpan.Empty);
    }
}
