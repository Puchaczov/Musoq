namespace Musoq.Evaluator.IR.Execution;

internal sealed class FusedSiblingAliasRewriter(
    ExecutionVariable source,
    ExecutionVariable target) : ExecutionIrRewriter
{
    protected override ExecutionExpression RewriteFieldRead(ExecutionFieldRead expression)
    {
        return HasSourceName(expression.Alias)
            ? expression with { Alias = target.Name }
            : expression;
    }

    protected override ExecutionExpression RewriteVariableRead(ExecutionVariableRead expression)
    {
        return HasSourceName(expression.Variable.Name)
            ? expression with { Variable = target }
            : expression;
    }

    protected override ExecutionExpression RewriteRowContextsRead(ExecutionRowContextsRead expression)
    {
        return HasSourceName(expression.Row.Name)
            ? expression with { Row = target }
            : expression;
    }

    protected override ExecutionNode RewriteHashAdd(ExecutionHashAdd node)
    {
        var rewritten = (ExecutionHashAdd)base.RewriteHashAdd(node);
        var precomputedKey = RewriteOptionalVariable(rewritten.PrecomputedKey);
        return ReferenceEquals(precomputedKey, rewritten.PrecomputedKey)
            ? rewritten
            : rewritten with { PrecomputedKey = precomputedKey };
    }

    protected override ExecutionNode RewriteKeySetAdd(ExecutionKeySetAdd node)
    {
        var rewritten = (ExecutionKeySetAdd)base.RewriteKeySetAdd(node);
        var precomputedKey = RewriteOptionalVariable(rewritten.PrecomputedKey);
        return ReferenceEquals(precomputedKey, rewritten.PrecomputedKey)
            ? rewritten
            : rewritten with { PrecomputedKey = precomputedKey };
    }

    private ExecutionVariable? RewriteOptionalVariable(ExecutionVariable? variable)
    {
        return variable != null && HasSourceName(variable.Name) ? target : variable;
    }

    private bool HasSourceName(string? name)
    {
        return string.Equals(name, source.Name, StringComparison.Ordinal);
    }
}
