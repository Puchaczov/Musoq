namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowResultRef(
    int WindowIndex,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionWindowResultRef(int windowIndex, Type returnType)
        : this(windowIndex, ExecutionTypeRef.FromClr(returnType))
    {
    }
}
