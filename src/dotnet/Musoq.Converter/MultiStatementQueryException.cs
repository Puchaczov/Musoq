using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter;

public sealed class MultiStatementQueryException : Exception, IDiagnosticException
{
    public MultiStatementQueryException(string message, Exception innerException)
        : base(message, innerException)
    {
        Span = null;
    }

    public MultiStatementQueryException(string message)
        : base(message)
    {
        Span = null;
    }

    public MultiStatementQueryException()
        : base("Multi-statement queries are not supported. Submit one statement at a time.")
    {
        Span = null;
    }

    public MultiStatementQueryException(TextSpan span)
        : base("Multiple executable statements are not supported by this compilation entry point. Submit one statement at a time.")
    {
        Span = span;
    }

    public DiagnosticCode Code => DiagnosticCode.MQ2036_MultipleExecutableStatements;

    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return SyntaxDiagnosticFactory.CreateDiagnostic(Code, Message, Span ?? TextSpan.Empty, currentToken: null, sourceText);
    }
}
