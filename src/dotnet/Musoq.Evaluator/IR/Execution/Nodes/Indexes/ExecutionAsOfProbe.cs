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
    ExecutionTypeRef? ComparisonKeyType = null,
    ExecutionAsOfTieBreak? TieBreak = null) : ExecutionNode
{
    internal ExecutionAsOfProbe(
        ExecutionVariable match,
        ExecutionVariable candidate,
        ExecutionExpression candidates,
        IReadOnlyList<ExecutionAsOfEqualityKey> equalityKeys,
        ExecutionExpression probeKey,
        ExecutionExpression candidateKey,
        BinaryOpKind comparisonKind,
        ExecutionBlock body,
        ExecutionBlock? noMatchBody,
        ExecutionVariable? index,
        Type comparisonKeyType,
        ExecutionAsOfTieBreak? tieBreak = null)
        : this(
            match,
            candidate,
            candidates,
            equalityKeys,
            probeKey,
            candidateKey,
            comparisonKind,
            body,
            noMatchBody,
            index,
            ExecutionTypeRef.FromClr(comparisonKeyType),
            tieBreak)
    {
    }
}
