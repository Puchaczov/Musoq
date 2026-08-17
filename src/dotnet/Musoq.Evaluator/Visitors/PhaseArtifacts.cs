using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

/// <summary>
/// Immutable handoff between parsing, normalization, metadata binding, and query rewriting.
/// <see cref="RewrittenQuery"/> is null for analysis-only consumers that stop before rewriting.
/// </summary>
internal sealed record SemanticPhaseArtifacts
{
    public required RootNode ParsedQuery { get; init; }

    public required RootNode NormalizedQuery { get; init; }

    public required RootNode MetadataQuery { get; init; }

    public RootNode? RewrittenQuery { get; init; }

    public required SemanticMetadataSnapshot Metadata { get; init; }

    public required SemanticScopeArtifact Scope { get; init; }

    public SemanticResultShapeSnapshot ResultShape => Metadata.ResultShape;

    public required IReadOnlyList<Diagnostic> Diagnostics { get; init; }
}

internal sealed record RewriteQueryPhaseInput(RootNode Root, SemanticScopeArtifact ScopeArtifact)
{
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Root);
        ArgumentNullException.ThrowIfNull(ScopeArtifact);
    }
}
