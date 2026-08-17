namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionGroupKeyRead(
    ExecutionVariable Group,
    string KeyName,
    ExecutionTypeRef ReturnType,
    AggregateGroupKeyField? Key = null) : ExecutionExpression(ReturnType)
{
    internal ExecutionGroupKeyRead(
        ExecutionVariable group,
        string keyName,
        Type returnType,
        AggregateGroupKeyField? key = null)
        : this(group, keyName, ExecutionClrBindingFactory.FromClr(returnType), key)
    {
    }
}
