using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal sealed class SemanticAdvisoryContext
{
    private readonly HashSet<DiagnosticKey> _reported = [];

    public SemanticAdvisoryContext(
        RootNode query,
        SemanticMetadataSnapshot metadata,
        DiagnosticContext diagnosticContext,
        RootNode? sourceQuery = null,
        RootNode? authoredQuery = null)
    {
        Query = query;
        SourceQuery = sourceQuery ?? query;
        AuthoredQuery = authoredQuery ?? SourceQuery;
        Metadata = metadata;
        Diagnostics = diagnosticContext;
        Literals = new LiteralOriginResolver(query, diagnosticContext.SourceText);
    }

    public RootNode Query { get; }

    public RootNode SourceQuery { get; }

    public RootNode AuthoredQuery { get; }

    public SemanticMetadataSnapshot Metadata { get; }

    public DiagnosticContext Diagnostics { get; }

    public LiteralOriginResolver Literals { get; }

    public void Report(DiagnosticCode code, string message, TextSpan span)
    {
        if (!_reported.Add(new DiagnosticKey(code, span.Start, span.End)))
            return;

        if (Diagnostics.Warnings.Any(diagnostic =>
                diagnostic.Code == code &&
                diagnostic.Span.Start == span.Start &&
                diagnostic.Span.End == span.End))
            return;

        Diagnostics.ReportWarning(code, message, span);
    }

    public void ReportError(DiagnosticCode code, string message, TextSpan span)
    {
        if (!_reported.Add(new DiagnosticKey(code, span.Start, span.End)))
            return;

        if (Diagnostics.Diagnostics.Any(diagnostic =>
                diagnostic.Code == code &&
                diagnostic.Span.Start == span.Start &&
                diagnostic.Span.End == span.End))
            return;

        Diagnostics.ReportError(code, message, span);
    }

    private readonly record struct DiagnosticKey(DiagnosticCode Code, int Start, int End);
}
