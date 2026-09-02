using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Schema.Exceptions;

public class TableNotFoundException : Exception, IDiagnosticException
{
    public TableNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public TableNotFoundException(string message)
        : base(message)
    {
    }

    public TableNotFoundException()
    {
    }

    public DiagnosticCode Code => DiagnosticCode.MQ3085_UnknownSource;

    /// <inheritdoc />
    public TextSpan? Span => null;

    /// <inheritdoc />
    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.ErrorUnknownLocation(Code, Message);
    }
}
