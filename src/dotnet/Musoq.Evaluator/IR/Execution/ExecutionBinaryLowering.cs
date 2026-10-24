using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionBinaryLowering
{
    internal static ExecutionExpression Create(BinaryOp binary, ExecutionExpression left, ExecutionExpression right)
    {
        if (binary.Kind == BinaryOpKind.SqlStringConcatenate)
            return new ExecutionStringConcatenation([left, right]);
        return new ExecutionBinary(binary.Kind, left, right, binary.ReturnType)
        {
            UsesSqlNullSemantics = binary.UsesSqlNullSemantics
        };
    }
}
