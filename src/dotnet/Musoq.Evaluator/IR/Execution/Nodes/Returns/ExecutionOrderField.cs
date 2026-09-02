namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionOrderField(
    string FieldName,
    int OutputIndex,
    ExecutionTypeRef Type,
    bool Descending, Bindings.NullOrdering NullOrdering = Bindings.NullOrdering.Default)
{
    internal ExecutionOrderField(
        string fieldName,
        int outputIndex,
        Type type,
        bool descending,
        Bindings.NullOrdering nullOrdering = Bindings.NullOrdering.Default)
        : this(fieldName, outputIndex, ExecutionClrBindingFactory.FromClr(type), descending, nullOrdering)
    {
    }
}
