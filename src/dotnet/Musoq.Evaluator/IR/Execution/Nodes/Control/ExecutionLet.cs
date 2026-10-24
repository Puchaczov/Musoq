namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionLet(
    ExecutionVariable Variable,
    ExecutionExpression Value,
    ExecutionLetCacheMode CacheMode = ExecutionLetCacheMode.Default) : ExecutionNode
{
    /// <summary>Controls whether helper extraction may move this declaration into a helper.</summary>
    public ExecutionDeclarationScope DeclarationScope { get; init; } = ExecutionDeclarationScope.Block;
}
