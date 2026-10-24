namespace Musoq.Evaluator.IR.Execution.Facts;

internal static partial class ExecutionNodeFacts
{
    private static ExecutionExpression? AsOfCreateAt(ExecutionCreateAsOfIndex node, int index)
    {
        if (index == 0)
            return node.Candidates;
        index--;
        if (index < node.EqualityKeys.Count)
            return node.EqualityKeys[index].Right;
        index -= node.EqualityKeys.Count;
        return index switch { 0 => node.CandidateKey, 1 => node.TieBreak?.Key, _ => null };
    }

    private static ExecutionExpression? AsOfProbeAt(ExecutionAsOfProbe node, int index)
    {
        if (index == 0)
            return node.Candidates;
        index--;
        if (index / 2 < node.EqualityKeys.Count)
            return index % 2 == 0 ? node.EqualityKeys[index / 2].Left : node.EqualityKeys[index / 2].Right;
        index -= node.EqualityKeys.Count * 2;
        return index switch { 0 => node.ProbeKey, 1 => node.CandidateKey, 2 => node.TieBreak?.Key, _ => null };
    }

    private static ExecutionExpression? RangeCreateAt(ExecutionCreateRangeIndex node, int index)
    {
        if (index == 0)
            return node.Candidates;
        index--;
        var count = node.PartitionKeys?.Count ?? 0;
        if (index < count)
            return node.PartitionKeys![index].Right;
        return index == count ? node.CandidateKey : null;
    }

    private static ExecutionExpression? RangeProbeAt(ExecutionRangeProbe node, int index)
    {
        var count = node.PartitionKeys?.Count ?? 0;
        if (index / 2 < count)
            return index % 2 == 0 ? node.PartitionKeys![index / 2].Left : node.PartitionKeys![index / 2].Right;
        return index == count * 2 ? node.ProbeKey : null;
    }
}
