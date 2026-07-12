using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateHash(
    ExecutionVariable Hash,
    ExecutionTypeRef KeyType,
    ExecutionTypeRef RowType,
    ExecutionCapacityHint? CapacityHint = null,
    string? GeneratedRowTypeName = null) : ExecutionNode
{
    internal ExecutionCreateHash(
        ExecutionVariable hash,
        Type keyType,
        Type rowType,
        ExecutionCapacityHint? capacityHint = null,
        string? generatedRowTypeName = null)
        : this(
            hash,
            ExecutionTypeRef.FromClr(keyType),
            ExecutionTypeRef.FromClr(rowType),
            capacityHint,
            generatedRowTypeName)
    {
    }
}
