using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionSetOperation(
    ExecutionVariable Target,
    ExecutionVariable Left,
    ExecutionVariable Right,
    SetOpKind Kind,
    IReadOnlyList<int> FieldIndexes,
    IReadOnlyList<ExecutionTypeRef> FieldTypes,
    ExecutionSetOperationStrategy Strategy = ExecutionSetOperationStrategy.GeneratedEqualityLoop) : ExecutionNode
{
    internal ExecutionSetOperation(
        ExecutionVariable target,
        ExecutionVariable left,
        ExecutionVariable right,
        SetOpKind kind,
        IReadOnlyList<int> fieldIndexes,
        IReadOnlyList<Type> fieldTypes,
        ExecutionSetOperationStrategy strategy = ExecutionSetOperationStrategy.GeneratedEqualityLoop)
        : this(target, left, right, kind, fieldIndexes, ExecutionTypeRef.FromClrTypes(fieldTypes), strategy)
    {
    }
}
