namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateKeySet(
    ExecutionVariable Set,
    ExecutionTypeRef KeyType,
    ExecutionCapacityHint? CapacityHint = null) : ExecutionNode
{
    internal ExecutionCreateKeySet(ExecutionVariable set, Type keyType, ExecutionCapacityHint? capacityHint = null)
        : this(set, ExecutionClrBindingFactory.FromClr(keyType), capacityHint)
    {
    }
}
