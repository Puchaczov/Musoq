using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowGeneratedKeyPart(
    ExecutionTypeRef Type,
    bool Descending,
    NullOrdering NullOrdering = NullOrdering.Default)
{
    internal ExecutionWindowGeneratedKeyPart(
        Type type,
        bool descending,
        NullOrdering nullOrdering = NullOrdering.Default)
        : this(ExecutionClrBindingFactory.FromClr(type), descending, nullOrdering)
    {
    }
}
