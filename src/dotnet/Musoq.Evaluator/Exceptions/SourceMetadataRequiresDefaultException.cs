using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

internal sealed class SourceMetadataRequiresDefaultException : Exception, IDiagnosticException
{
    public SourceMetadataRequiresDefaultException(
        string schemaName,
        string sourceName,
        TextSpan span,
        Exception innerException)
        : base(
            $"Source metadata for '{schemaName}.{sourceName}' requires a value for a direct script parameter. " +
            "Declare a default value for that parameter or make the provider's metadata independent of runtime source arguments.",
            innerException)
    {
        SchemaName = schemaName;
        SourceName = sourceName;
        Span = span;
    }

    public string SchemaName { get; }

    public string SourceName { get; }

    public DiagnosticCode Code => DiagnosticCode.MQ3062_InvalidScriptParameterSourceArgument;

    public TextSpan? Span { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.Error(Code, Message, Span ?? TextSpan.Empty)
            .WithArgument("schema", SchemaName)
            .WithArgument("source", SourceName);
    }
}
