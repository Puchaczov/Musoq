using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAsOfProbe : ExecutionNode
{
    public ExecutionAsOfProbe(
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
        ExecutionAsOfTieBreak? TieBreak = null)
    {
        this.Match = Match;
        this.Candidate = Candidate;
        this.Candidates = Candidates;
        this.EqualityKeys = ExecutionIrCollections.Freeze(EqualityKeys);
        this.ProbeKey = ProbeKey;
        this.CandidateKey = CandidateKey;
        this.ComparisonKind = ComparisonKind;
        this.Body = Body;
        this.NoMatchBody = NoMatchBody;
        this.Index = Index;
        this.ComparisonKeyType = ComparisonKeyType;
        this.TieBreak = TieBreak;
    }

    public ExecutionVariable Match { get; init; }
    public ExecutionVariable Candidate { get; init; }
    public ExecutionExpression Candidates { get; init; }
    public IReadOnlyList<ExecutionAsOfEqualityKey> EqualityKeys { get; init; }
    public ExecutionExpression ProbeKey { get; init; }
    public ExecutionExpression CandidateKey { get; init; }
    public BinaryOpKind ComparisonKind { get; init; }
    public ExecutionBlock Body { get; init; }
    public ExecutionBlock? NoMatchBody { get; init; }
    public ExecutionVariable? Index { get; init; }
    public ExecutionTypeRef? ComparisonKeyType { get; init; }
    public ExecutionAsOfTieBreak? TieBreak { get; init; }

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
            ExecutionClrBindingFactory.FromClr(comparisonKeyType),
            tieBreak)
    {
    }
}
