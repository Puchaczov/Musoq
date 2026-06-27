using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateHash(
    ExecutionVariable Hash,
    Type KeyType,
    Type RowType,
    ExecutionCapacityHint? CapacityHint = null,
    string? GeneratedRowTypeName = null) : ExecutionNode;
