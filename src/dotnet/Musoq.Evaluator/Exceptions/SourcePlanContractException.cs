using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

internal sealed class SourcePlanContractException : Exception, IDiagnosticException
{
    public SourcePlanContractException(string detail, TextSpan span)
        : base($"Source contract error: {detail}")
    {
        Detail = detail;
        Span = span;
    }

    public string Detail { get; }

    public DiagnosticCode Code => DiagnosticCode.MQ3071_SourceContractError;

    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.Error(Code, Message, Span ?? TextSpan.Empty)
            .WithArgument("detail", Detail);
    }
}
