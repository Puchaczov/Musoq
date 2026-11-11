using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class ByteBufferExpressionRewrite
{
    internal static ExecutionExpression Children(
        ExecutionExpression expression,
        Func<ExecutionExpression, ExecutionExpression> rewrite
    ) =>
        expression switch
        {
            ExecutionByteBufferSnapshot { Program: { } program } snapshot => snapshot with
            {
                Program = program.Rewrite(rewrite),
            },
            ExecutionByteBufferSnapshot snapshot => snapshot with
            {
                Arguments = snapshot.Arguments.Select(rewrite).ToArray(),
            },
            ExecutionByteBufferOperation { Program: { } program } operation => operation with
            {
                Program = program.Rewrite(rewrite),
            },
            ExecutionByteBufferOperation operation => operation with { Original = rewrite(operation.Original) },
            _ => throw new ArgumentException("A byte-buffer expression is required.", nameof(expression)),
        };
}
