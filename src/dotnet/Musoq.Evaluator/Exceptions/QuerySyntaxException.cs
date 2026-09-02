using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Identifies a query-shape failure discovered by an evaluator-owned rewrite
///     before semantic binding. The exception is typed so it cannot be mistaken
///     for an engine invariant or classified from its message text.
/// </summary>
internal sealed class QuerySyntaxException : Exception, IDiagnosticException
{
    public QuerySyntaxException(string message, TextSpan span, Exception? innerException = null)
        : this(message, span, DiagnosticCode.MQ2030_UnsupportedSyntax, innerException)
    {
    }

    public QuerySyntaxException(
        string message,
        TextSpan span,
        DiagnosticCode code,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        Span = span;
    }

    public DiagnosticCode Code { get; }

    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.Error(Code, Message, Span ?? TextSpan.Empty);
    }
}
