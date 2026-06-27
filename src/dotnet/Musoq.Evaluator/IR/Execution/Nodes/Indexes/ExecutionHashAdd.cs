using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionHashAdd(
    ExecutionVariable Hash,
    ExecutionExpression Key,
    ExecutionVariable Row,
    Type KeyType,
    Type RowType,
    string? GeneratedRowTypeName = null,
    ExecutionVariable? PrecomputedKey = null,
    string? KeyVariableName = null,
    string? BucketVariableName = null,
    ExecutionKeyBuildNullHandling NullHandling = ExecutionKeyBuildNullHandling.Continue) : ExecutionNode;
