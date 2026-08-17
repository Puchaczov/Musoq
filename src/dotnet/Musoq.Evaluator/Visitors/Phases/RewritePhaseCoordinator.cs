using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

/// <summary>
/// Owns the immutable semantic-artifact to rewrite-phase handoff.
/// </summary>
internal sealed class SemanticRewritePhaseCoordinator
{
    public RootNode Rewrite(
        RootNode query,
        SemanticScopeArtifact scopeArtifact,
        CompilationOptions compilationOptions)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(scopeArtifact);
        ArgumentNullException.ThrowIfNull(compilationOptions);

        var rewriter = new RewriteQueryVisitor(
            new RewriteQueryPhaseInput(query, scopeArtifact),
            compilationOptions);
        var traversal = new RewriteQueryTraverseVisitor(rewriter, scopeArtifact.CreateScopeWalker());
        query.Accept(traversal);
        return rewriter.RootScript;
    }
}
