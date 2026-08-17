using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class ExecutionExpressionSubstitutionRewriter(
    Func<ExecutionExpression, ExecutionExpression?> substitute) : ExecutionIrRewriter
{
    public override ExecutionExpression RewriteExpression(ExecutionExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return substitute(expression) ?? base.RewriteExpression(expression);
    }

    public IReadOnlyList<ExecutionExpression> RewriteExpressions(IReadOnlyList<ExecutionExpression> expressions)
    {
        return RewriteExpressionList(expressions);
    }

    public IReadOnlyList<ExecutionRowValue> RewriteRows(IReadOnlyList<ExecutionRowValue> values)
    {
        return RewriteRowValues(values);
    }

    public ExecutionContextLayout? RewriteLayout(ExecutionContextLayout? layout)
    {
        return RewriteContextLayout(layout);
    }
}
