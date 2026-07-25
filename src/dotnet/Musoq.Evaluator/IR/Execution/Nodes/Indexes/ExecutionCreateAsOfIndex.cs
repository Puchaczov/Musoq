using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateAsOfIndex : ExecutionNode
{
    public ExecutionCreateAsOfIndex(
        ExecutionVariable Index,
        ExecutionVariable Candidate,
        ExecutionExpression Candidates,
        IReadOnlyList<ExecutionAsOfEqualityKey> EqualityKeys,
        ExecutionExpression CandidateKey,
        BinaryOpKind ComparisonKind,
        ExecutionTypeRef ComparisonKeyType,
        ExecutionAsOfTieBreak? TieBreak = null)
    {
        this.Index = Index;
        this.Candidate = Candidate;
        this.Candidates = Candidates;
        this.EqualityKeys = ExecutionIrCollections.Freeze(EqualityKeys);
        this.CandidateKey = CandidateKey;
        this.ComparisonKind = ComparisonKind;
        this.ComparisonKeyType = ComparisonKeyType;
        this.TieBreak = TieBreak;
    }

    public ExecutionVariable Index { get; init; }
    public ExecutionVariable Candidate { get; init; }
    public ExecutionExpression Candidates { get; init; }
    public IReadOnlyList<ExecutionAsOfEqualityKey> EqualityKeys { get; init; }
    public ExecutionExpression CandidateKey { get; init; }
    public BinaryOpKind ComparisonKind { get; init; }
    public ExecutionTypeRef ComparisonKeyType { get; init; }
    public ExecutionAsOfTieBreak? TieBreak { get; init; }

    internal ExecutionCreateAsOfIndex(
        ExecutionVariable index,
        ExecutionVariable candidate,
        ExecutionExpression candidates,
        IReadOnlyList<ExecutionAsOfEqualityKey> equalityKeys,
        ExecutionExpression candidateKey,
        BinaryOpKind comparisonKind,
        Type comparisonKeyType,
        ExecutionAsOfTieBreak? tieBreak = null)
        : this(index, candidate, candidates, equalityKeys, candidateKey, comparisonKind, ExecutionClrBindingFactory.FromClr(comparisonKeyType), tieBreak)
    {
    }
}
