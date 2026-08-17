namespace Musoq.Evaluator.IR.Planning;

internal sealed record JoinStrategyDecision(
    JoinStrategyKind Kind,
    HashJoinDecomposition? HashJoin,
    SortMergeJoinDecomposition? SortMergeJoin,
    string Reason)
{
    public static JoinStrategyDecision Hash(HashJoinDecomposition hashJoin, string reason)
    {
        return new JoinStrategyDecision(JoinStrategyKind.HashJoin, hashJoin, null, reason);
    }

    public static JoinStrategyDecision SortMerge(SortMergeJoinDecomposition sortMergeJoin, string reason)
    {
        return new JoinStrategyDecision(JoinStrategyKind.SortMergeJoin, null, sortMergeJoin, reason);
    }

    public static JoinStrategyDecision NestedLoop(string reason)
    {
        return new JoinStrategyDecision(JoinStrategyKind.NestedLoop, null, null, reason);
    }
}
