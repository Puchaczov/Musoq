using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

public sealed class PagingCountException(string clause, string name)
    : InvalidOperationException($"{clause} count '${name}' must be an integral value between 0 and {int.MaxValue}, inclusive; null is not allowed."), IDiagnosticException
{
    public DiagnosticCode Code => DiagnosticCode.MQ7019_InvalidRuntimePagingCount;
    public TextSpan? Span => null;
    public Diagnostic ToDiagnostic(SourceText? sourceText = null) =>
        Diagnostic.ErrorUnknownLocation(Code, Message, sourceKind: DiagnosticSourceKind.Runtime);
}
