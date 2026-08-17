using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

public class ConstructionNotYetSupported : Exception, IDiagnosticException
{

    public ConstructionNotYetSupported(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = DiagnosticCode.MQ4016_UnsupportedSchemaConstruction;
    }

    public ConstructionNotYetSupported()
    {
        Code = DiagnosticCode.MQ4016_UnsupportedSchemaConstruction;
    }
    public ConstructionNotYetSupported(string message)
        : base(message)
    {
        Code = DiagnosticCode.MQ4016_UnsupportedSchemaConstruction;
    }

    public ConstructionNotYetSupported(string message, TextSpan span)
        : base(message)
    {
        Code = DiagnosticCode.MQ4016_UnsupportedSchemaConstruction;
        Span = span;
    }

    public ConstructionNotYetSupported(string message, DiagnosticCode code)
        : base(message)
    {
        Code = code;
    }

    public ConstructionNotYetSupported(string message, DiagnosticCode code, TextSpan span)
        : base(message)
    {
        Code = code;
        Span = span;
    }

    public DiagnosticCode Code { get; }
    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? TextSpan.Empty;
        return Diagnostic.Error(Code, Message, span);
    }
}
