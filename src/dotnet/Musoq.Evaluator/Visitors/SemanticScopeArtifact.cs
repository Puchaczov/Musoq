using Musoq.Evaluator.Utils;

namespace Musoq.Evaluator.Visitors;

/// <summary>
/// Immutable semantic scope handoff. Each materialization produces an
/// independent mutable scope for legacy visitors and planning adapters.
/// </summary>
internal sealed class SemanticScopeArtifact
{
    private readonly ScopeSnapshot _snapshot;

    private SemanticScopeArtifact(ScopeSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public static SemanticScopeArtifact Capture(Scope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return new SemanticScopeArtifact(scope.Snapshot());
    }

    public Scope CreateScope()
    {
        return _snapshot.CreateScope();
    }

    public ScopeWalker CreateScopeWalker()
    {
        return new ScopeWalker(CreateScope());
    }
}
