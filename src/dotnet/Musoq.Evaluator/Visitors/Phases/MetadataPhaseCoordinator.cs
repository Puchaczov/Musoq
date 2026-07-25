using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

/// <summary>
/// Owns the traversal-to-snapshot handoff for semantic metadata analysis.
/// The visitor remains the compatibility implementation; callers consume only
/// this immutable phase result.
/// </summary>
internal sealed class SemanticMetadataPhaseCoordinator
{
    public SemanticMetadataPhaseResult Analyze(
        RootNode query,
        BuildMetadataAndInferTypesVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(visitor);

        var traversal = new BuildMetadataAndInferTypesTraverseVisitor(visitor);
        query.Accept(traversal);

        return new SemanticMetadataPhaseResult(
            visitor.Root,
            visitor.CreateSemanticMetadataSnapshot(),
            SemanticScopeArtifact.Capture(traversal.Scope));
    }
}

internal sealed record SemanticMetadataPhaseResult(
    RootNode Query,
    SemanticMetadataSnapshot Metadata,
    SemanticScopeArtifact Scope);
