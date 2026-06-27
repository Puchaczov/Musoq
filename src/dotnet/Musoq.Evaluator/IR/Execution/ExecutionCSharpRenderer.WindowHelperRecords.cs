using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed record RankingWindowKeyExtractionHelper(
        string FunctionName,
        ExecutionComputeRankingWindow Ranking,
        string? BufferItemGeneratedRowTypeName,
        ExecutionWindowKeyArray? PartitionKeys,
        ExecutionVariable? PartitionBuilder,
        ExecutionWindowKeyArray OrderKeys,
        IReadOnlyList<CapturedLocal> Captures);

    private sealed record WindowAppendRowsHelper(
        string FunctionName,
        string RowsParameterName,
        string? BufferItemGeneratedRowTypeName,
        ExecutionForEachIndexed Loop,
        IReadOnlyList<ExecutionVariable> AppendTargets,
        IReadOnlyList<CapturedLocal> Captures);

    private sealed record SortedCopyHelper(
        string FunctionName,
        ExecutionSortTable Sort);
}
