using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

internal sealed class PivotUsingNonAggregateException(string functionName, TextSpan span)
    : Exception($"PIVOT USING accepts aggregate function calls only, but '{functionName}' is not an aggregate function."),
        IDiagnosticException
{
    public DiagnosticCode Code => DiagnosticCode.MQ3051_FilterOnNonAggregate;

    public TextSpan? Span { get; } = span;

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.Error(Code, Message, Span ?? TextSpan.Empty);
    }
}
