using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionOrderField(
    string FieldName,
    int OutputIndex,
    ExecutionTypeRef Type,
    bool Descending, Musoq.Evaluator.IR.Bindings.NullOrdering NullOrdering = Musoq.Evaluator.IR.Bindings.NullOrdering.Default)
{
    internal ExecutionOrderField(
        string fieldName,
        int outputIndex,
        Type type,
        bool descending,
        Musoq.Evaluator.IR.Bindings.NullOrdering nullOrdering = Musoq.Evaluator.IR.Bindings.NullOrdering.Default)
        : this(fieldName, outputIndex, ExecutionClrBindingFactory.FromClr(type), descending, nullOrdering)
    {
    }
}
