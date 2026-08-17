namespace Musoq.Evaluator.IR.Execution;

public sealed record AggregateCapturedField(
    string Name,
    string FieldName,
    ExecutionTypeRef Type)
{
    internal AggregateCapturedField(string name, string fieldName, Type type)
        : this(name, fieldName, ExecutionClrBindingFactory.FromClr(type))
    {
    }
}
