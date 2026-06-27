using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionKeySetProbe(ExecutionVariable Set, ExecutionExpression Key, Type KeyType, ExecutionBlock Body, ExecutionBlock? NoMatchBody = null, ExecutionVariable? MatchFound = null, string? KeyVariableName = null) : ExecutionNode;
