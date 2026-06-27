using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionLoadCteIndex(
    ExecutionVariable Index,
    int IndexSlot,
    ExecutionCteSidecarIndexKind Kind,
    Type KeyType,
    Type? RowType = null,
    string? GeneratedRowTypeName = null) : ExecutionNode;
