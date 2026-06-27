using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record TakeStrategyDecision(
    TakeStrategyKind Kind,
    SortNode? Sort,
    SkipNode? Skip,
    TakeNode Take,
    string Reason)
{
    public static TakeStrategyDecision TopOffset(SortNode sort, SkipNode skip, TakeNode take, string reason)
    {
        return new TakeStrategyDecision(TakeStrategyKind.TopOffset, sort, skip, take, reason);
    }

    public static TakeStrategyDecision TopN(SortNode sort, TakeNode take, string reason)
    {
        return new TakeStrategyDecision(TakeStrategyKind.TopN, sort, null, take, reason);
    }

    public static TakeStrategyDecision PlainTake(TakeNode take, string reason)
    {
        return new TakeStrategyDecision(TakeStrategyKind.Take, null, null, take, reason);
    }
}
