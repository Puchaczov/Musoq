using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

internal sealed class UnknownSourceException : Exception, IDiagnosticException
{
    public UnknownSourceException(string schemaName, string sourceName, TextSpan span)
        : base($"Source '{sourceName}' does not exist in schema '{schemaName}'.")
    {
        SchemaName = schemaName;
        SourceName = sourceName;
        Span = span;
    }

    public string SchemaName { get; }

    public string SourceName { get; }

    public DiagnosticCode Code => DiagnosticCode.MQ3085_UnknownSource;

    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.Error(Code, Message, Span ?? TextSpan.Empty)
            .WithArgument("schema", SchemaName)
            .WithArgument("source", SourceName);
    }
}
