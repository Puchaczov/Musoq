using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

internal sealed class UnknownSourceException : Exception, IDiagnosticException
{
    public UnknownSourceException(
        string schemaName,
        string sourceName,
        TextSpan span,
        Exception? providerFailure = null)
        : base($"Source '{sourceName}' does not exist in schema '{schemaName}'.")
    {
        SchemaName = schemaName;
        SourceName = sourceName;
        Span = span;
        ProviderFailure = providerFailure;
    }

    public string SchemaName { get; }

    public string SourceName { get; }

    public DiagnosticCode Code => DiagnosticCode.MQ3085_UnknownSource;

    public TextSpan? Span { get; }

    /// <summary>
    /// Gets the original provider exception when diagnostics intentionally
    /// classify an unsupported provider lookup as an unknown source.
    /// QueryAnalyzer uses this to preserve its provider-failure contract.
    /// </summary>
    public Exception? ProviderFailure { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return SemanticDiagnosticFactory.Create(
            Code,
            Message,
            Span,
            sourceText,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["schema"] = SchemaName,
                ["source"] = SourceName
            });
    }
}
