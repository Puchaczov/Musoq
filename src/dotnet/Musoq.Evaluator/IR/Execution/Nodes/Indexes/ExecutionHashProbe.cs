using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionHashProbe(
    ExecutionVariable Hash,
    ExecutionVariable Matches,
    ExecutionExpression Key,
    Type KeyType,
    Type RowType,
    ExecutionBlock Body,
    ExecutionBlock? NoMatchBody = null,
    ExecutionVariable? MatchFound = null,
    string? GeneratedRowTypeName = null,
    string? KeyVariableName = null) : ExecutionNode;
