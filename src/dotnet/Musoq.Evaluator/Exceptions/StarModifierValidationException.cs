#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

public class StarModifierValidationException : Exception, IDiagnosticException
{
    public StarModifierValidationException(string message, DiagnosticCode code, TextSpan span)
        : base(message)
    {
        Code = code;
        Span = span;
    }

    public StarModifierValidationException(string message, DiagnosticCode code)
        : base(message)
    {
        Code = code;
    }

    public DiagnosticCode Code { get; }

    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? TextSpan.Empty;
        return Diagnostic.Error(Code, Message, span);
    }
}
