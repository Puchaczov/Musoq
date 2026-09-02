using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

internal sealed class FilterOnNonAggregateException(string functionName, TextSpan span)
    : Exception($"FILTER clause can only be applied to aggregate functions, but '{functionName}' is not an aggregate function."),
        IDiagnosticException
{
    public DiagnosticCode Code => DiagnosticCode.MQ3051_FilterOnNonAggregate;

    public TextSpan? Span { get; } = span;

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.Error(Code, Message, Span ?? TextSpan.Empty);
    }
}
