using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionSetOperation(
    ExecutionVariable Target,
    ExecutionVariable Left,
    ExecutionVariable Right,
    SetOpKind Kind,
    IReadOnlyList<int> FieldIndexes,
    IReadOnlyList<Type> FieldTypes,
    ExecutionSetOperationStrategy Strategy = ExecutionSetOperationStrategy.GeneratedEqualityLoop) : ExecutionNode;
