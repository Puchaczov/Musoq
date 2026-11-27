namespace Musoq.Evaluator.IR.Execution;

internal class ExecutionRowAliasRewriter(string sourceName, ExecutionVariable target, StringComparison comparison)
    : ExecutionIrRewriter
{
    protected ExecutionVariable Target => target;

    protected bool HasSourceName(string? name) => string.Equals(name, sourceName, comparison);

    protected override ExecutionExpression RewriteFieldRead(ExecutionFieldRead expression) =>
        HasSourceName(expression.Alias) ? expression with { Alias = target.Name } : expression;

    protected override ExecutionExpression RewriteVariableRead(ExecutionVariableRead expression) =>
        HasSourceName(expression.Variable.Name) ? expression with { Variable = target } : expression;

    protected override ExecutionExpression RewriteRowContextsRead(ExecutionRowContextsRead expression) =>
        HasSourceName(expression.Row.Name) ? expression with { Row = target } : expression;
}
