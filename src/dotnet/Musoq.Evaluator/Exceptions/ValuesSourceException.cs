using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when an inline VALUES source cannot be bound as a strongly typed table.
/// </summary>
public sealed class ValuesSourceException : Exception, IDiagnosticException
{
    public ValuesSourceException(string message, TextSpan span)
        : base(message)
    {
        Span = span;
    }

    public ValuesSourceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ValuesSourceException(string message)
        : base(message)
    {
    }

    public ValuesSourceException()
    {
    }

    public DiagnosticCode Code { get; } = DiagnosticCode.MQ3055_InvalidValuesSource;

    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.Error(Code, Message, Span ?? TextSpan.Empty);
    }
}
