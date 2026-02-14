#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

public class FromNodeIsNull : Exception, IDiagnosticException
{
    public FromNodeIsNull()
        : base("FROM clause is missing. Every SELECT query requires a FROM clause specifying the data source.")
    {
        Code = DiagnosticCode.MQ9999_Unknown;
    }

    public FromNodeIsNull(TextSpan span)
        : base("FROM clause is missing. Every SELECT query requires a FROM clause specifying the data source.")
    {
        Code = DiagnosticCode.MQ9999_Unknown;
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
