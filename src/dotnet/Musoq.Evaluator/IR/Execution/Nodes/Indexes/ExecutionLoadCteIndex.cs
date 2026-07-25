using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionLoadCteIndex(
    ExecutionVariable Index,
    int IndexSlot,
    ExecutionCteSidecarIndexKind Kind,
    ExecutionTypeRef KeyType,
    ExecutionTypeRef? RowType = null,
    string? GeneratedRowTypeName = null) : ExecutionNode
{
    internal ExecutionLoadCteIndex(
        ExecutionVariable index,
        int indexSlot,
        ExecutionCteSidecarIndexKind kind,
        Type keyType,
        Type? rowType = null,
        string? generatedRowTypeName = null)
        : this(index, indexSlot, kind, ExecutionClrBindingFactory.FromClr(keyType), ExecutionClrBindingFactory.FromOptionalClr(rowType), generatedRowTypeName)
    {
    }
}
