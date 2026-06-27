using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateRangeIndex(
    ExecutionVariable Index,
    ExecutionVariable Candidate,
    ExecutionExpression Candidates,
    ExecutionExpression CandidateKey,
    Type KeyType,
    BinaryOpKind ComparisonKind) : ExecutionNode;
