#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

public class ConstructionNotYetSupported : Exception, IDiagnosticException
{
    public ConstructionNotYetSupported(string message)
        : base(message)
    {
        Code = DiagnosticCode.MQ3030_ConstructionNotSupported;
    }

    public ConstructionNotYetSupported(string message, TextSpan span)
        : base(message)
    {
        Code = DiagnosticCode.MQ3030_ConstructionNotSupported;
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
