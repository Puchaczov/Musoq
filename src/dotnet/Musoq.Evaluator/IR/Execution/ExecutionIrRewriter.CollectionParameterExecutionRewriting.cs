namespace Musoq.Evaluator.IR.Execution;

internal abstract partial class ExecutionIrRewriter
{
    protected virtual ExecutionExpression RewriteCollectionInCheck(ExecutionCollectionInCheck expression)
    {
        var value = RewriteExpression(expression.Expression);
        return ReferenceEquals(value, expression.Expression)
            ? expression
            : expression with { Expression = value };
    }
}
