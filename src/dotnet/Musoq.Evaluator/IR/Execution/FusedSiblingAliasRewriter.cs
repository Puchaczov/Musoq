namespace Musoq.Evaluator.IR.Execution;

internal sealed class FusedSiblingAliasRewriter(ExecutionVariable source, ExecutionVariable target)
    : ExecutionRowAliasRewriter(source.Name, target, StringComparison.Ordinal)
{
    protected override ExecutionNode RewriteHashAdd(ExecutionHashAdd node)
    {
        var rewritten = (ExecutionHashAdd)base.RewriteHashAdd(node);
        var precomputedKey = RewriteOptionalVariable(rewritten.PrecomputedKey);
        return ReferenceEquals(precomputedKey, rewritten.PrecomputedKey)
            ? rewritten
            : rewritten with
            {
                PrecomputedKey = precomputedKey,
            };
    }

    protected override ExecutionNode RewriteKeySetAdd(ExecutionKeySetAdd node)
    {
        var rewritten = (ExecutionKeySetAdd)base.RewriteKeySetAdd(node);
        var precomputedKey = RewriteOptionalVariable(rewritten.PrecomputedKey);
        return ReferenceEquals(precomputedKey, rewritten.PrecomputedKey)
            ? rewritten
            : rewritten with
            {
                PrecomputedKey = precomputedKey,
            };
    }

    private ExecutionVariable? RewriteOptionalVariable(ExecutionVariable? variable)
    {
        return variable != null && HasSourceName(variable.Name) ? Target : variable;
    }
}
