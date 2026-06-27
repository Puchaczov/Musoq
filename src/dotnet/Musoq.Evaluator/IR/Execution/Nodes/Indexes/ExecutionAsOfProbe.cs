using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAsOfProbe(
    ExecutionVariable Match,
    ExecutionVariable Candidate,
    ExecutionExpression Candidates,
    IReadOnlyList<ExecutionAsOfEqualityKey> EqualityKeys,
    ExecutionExpression ProbeKey,
    ExecutionExpression CandidateKey,
    BinaryOpKind ComparisonKind,
    ExecutionBlock Body,
    ExecutionBlock? NoMatchBody = null,
    ExecutionVariable? Index = null,
    Type? ComparisonKeyType = null,
    ExecutionAsOfTieBreak? TieBreak = null) : ExecutionNode;
