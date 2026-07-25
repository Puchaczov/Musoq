using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionKeySetAdd(
    ExecutionVariable Set,
    ExecutionExpression Key,
    ExecutionTypeRef KeyType,
    ExecutionVariable? PrecomputedKey = null,
    string? KeyVariableName = null,
    ExecutionKeyBuildNullHandling NullHandling = ExecutionKeyBuildNullHandling.Continue) : ExecutionNode
{
    internal ExecutionKeySetAdd(
        ExecutionVariable set,
        ExecutionExpression key,
        Type keyType,
        ExecutionVariable? precomputedKey = null,
        string? keyVariableName = null,
        ExecutionKeyBuildNullHandling nullHandling = ExecutionKeyBuildNullHandling.Continue)
        : this(set, key, ExecutionClrBindingFactory.FromClr(keyType), precomputedKey, keyVariableName, nullHandling)
    {
    }
}
