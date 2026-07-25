using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionKeySetProbe(
    ExecutionVariable Set,
    ExecutionExpression Key,
    ExecutionTypeRef KeyType,
    ExecutionBlock Body,
    ExecutionBlock? NoMatchBody = null,
    ExecutionVariable? MatchFound = null,
    string? KeyVariableName = null) : ExecutionNode
{
    internal ExecutionKeySetProbe(
        ExecutionVariable set,
        ExecutionExpression key,
        Type keyType,
        ExecutionBlock body,
        ExecutionBlock? noMatchBody = null,
        ExecutionVariable? matchFound = null,
        string? keyVariableName = null)
        : this(set, key, ExecutionClrBindingFactory.FromClr(keyType), body, noMatchBody, matchFound, keyVariableName)
    {
    }
}
