using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionSetOperation : ExecutionNode
{
    public ExecutionSetOperation(
        ExecutionVariable target,
        ExecutionVariable left,
        ExecutionVariable right,
        SetOpKind kind,
        IReadOnlyList<int> fieldIndexes,
        IReadOnlyList<ExecutionTypeRef> fieldTypes,
        ExecutionSetOperationStrategy strategy = ExecutionSetOperationStrategy.GeneratedEqualityLoop)
    {
        Target = target;
        Left = left;
        Right = right;
        Kind = kind;
        FieldIndexes = ExecutionIrCollections.Freeze(fieldIndexes);
        FieldTypes = ExecutionIrCollections.Freeze(fieldTypes);
        Strategy = strategy;
    }

    public ExecutionVariable Target { get; init; }
    public ExecutionVariable Left { get; init; }
    public ExecutionVariable Right { get; init; }
    public SetOpKind Kind { get; init; }
    public IReadOnlyList<int> FieldIndexes { get; init; }
    public IReadOnlyList<ExecutionTypeRef> FieldTypes { get; init; }
    public ExecutionSetOperationStrategy Strategy { get; init; }

    internal ExecutionSetOperation(
        ExecutionVariable target,
        ExecutionVariable left,
        ExecutionVariable right,
        SetOpKind kind,
        IReadOnlyList<int> fieldIndexes,
        IReadOnlyList<Type> fieldTypes,
        ExecutionSetOperationStrategy strategy = ExecutionSetOperationStrategy.GeneratedEqualityLoop)
        : this(target, left, right, kind, fieldIndexes, ExecutionClrBindingFactory.FromClrTypes(fieldTypes), strategy)
    {
    }
}
