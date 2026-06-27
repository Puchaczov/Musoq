using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionKeySetAdd(
    ExecutionVariable Set,
    ExecutionExpression Key,
    Type KeyType,
    ExecutionVariable? PrecomputedKey = null,
    string? KeyVariableName = null,
    ExecutionKeyBuildNullHandling NullHandling = ExecutionKeyBuildNullHandling.Continue) : ExecutionNode;
