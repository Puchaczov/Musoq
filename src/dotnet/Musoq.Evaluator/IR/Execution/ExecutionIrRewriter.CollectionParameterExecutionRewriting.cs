namespace Musoq.Evaluator.IR.Execution;

internal abstract partial class ExecutionIrRewriter
{
    protected virtual ExecutionExpression RewriteCollectionInCheck(ExecutionCollectionInCheck expression)
        => RewriteExpressionOwner(expression, expression.Expression,
            value => expression with { Expression = value });
}
