using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

internal sealed class UnknownAliasException : Exception, IDiagnosticException
{
    public UnknownAliasException(string alias, TextSpan span)
        : base($"Unknown alias '{alias}'.")
    {
        Alias = alias;
        Span = span;
    }

    public string Alias { get; }

    public DiagnosticCode Code => DiagnosticCode.MQ3015_UnknownAlias;

    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.Error(Code, Message, Span ?? TextSpan.Empty)
            .WithArgument("alias", Alias);
    }
}
