namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowValueRead(
    ExecutionVariable Results,
    ExecutionVariable Index,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionWindowValueRead(ExecutionVariable results, ExecutionVariable index, Type returnType)
        : this(results, index, ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
