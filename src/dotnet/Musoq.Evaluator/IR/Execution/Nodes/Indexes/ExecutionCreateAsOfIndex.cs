using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateAsOfIndex(
    ExecutionVariable Index,
    ExecutionVariable Candidate,
    ExecutionExpression Candidates,
    IReadOnlyList<ExecutionAsOfEqualityKey> EqualityKeys,
    ExecutionExpression CandidateKey,
    BinaryOpKind ComparisonKind,
    ExecutionTypeRef ComparisonKeyType,
    ExecutionAsOfTieBreak? TieBreak = null) : ExecutionNode
{
    internal ExecutionCreateAsOfIndex(
        ExecutionVariable index,
        ExecutionVariable candidate,
        ExecutionExpression candidates,
        IReadOnlyList<ExecutionAsOfEqualityKey> equalityKeys,
        ExecutionExpression candidateKey,
        BinaryOpKind comparisonKind,
        Type comparisonKeyType,
        ExecutionAsOfTieBreak? tieBreak = null)
        : this(index, candidate, candidates, equalityKeys, candidateKey, comparisonKind, ExecutionTypeRef.FromClr(comparisonKeyType), tieBreak)
    {
    }
}
