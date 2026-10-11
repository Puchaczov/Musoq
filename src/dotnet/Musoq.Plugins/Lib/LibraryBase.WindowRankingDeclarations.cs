using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>Returns the one-based position within the ordered partition.</summary>
    [WindowFunction(KernelType = typeof(RowNumberKernel), RankingKind = WindowRankingKind.RowNumber, SupportsOrdinalUpperBound = true,
        RequiresPeerMetadata = false, RequiresOrderBy = true,
        Modes = WindowEvaluationModes.WholePartition | WindowEvaluationModes.Batch)]
    public long RowNumber() => AggregateFunction.NotInvoked<long>();

    /// <summary>Returns the one-based position of the first row in the current peer group.</summary>
    [WindowFunction(KernelType = typeof(RankKernel), RankingKind = WindowRankingKind.Rank, SupportsOrdinalUpperBound = true,
        RequiresOrderBy = true, Modes = WindowEvaluationModes.WholePartition | WindowEvaluationModes.Batch)]
    public long Rank() => AggregateFunction.NotInvoked<long>();

    /// <summary>Returns the one-based peer group number within the partition.</summary>
    [WindowFunction(KernelType = typeof(DenseRankKernel), RankingKind = WindowRankingKind.DenseRank, SupportsOrdinalUpperBound = true,
        RequiresOrderBy = true, Modes = WindowEvaluationModes.WholePartition | WindowEvaluationModes.Batch)]
    public long DenseRank() => AggregateFunction.NotInvoked<long>();

    /// <summary>Returns the relative rank within the ordered partition.</summary>
    [WindowFunction(KernelType = typeof(PercentRankKernel), RankingKind = WindowRankingKind.PercentRank,
        RequiresOrderBy = true, Modes = WindowEvaluationModes.WholePartition | WindowEvaluationModes.Batch)]
    public double PercentRank() => AggregateFunction.NotInvoked<double>();

    /// <summary>Returns the fraction of partition rows at or before the current peer group.</summary>
    [WindowFunction(KernelType = typeof(CumeDistKernel), RankingKind = WindowRankingKind.CumeDist,
        RequiresOrderBy = true, Modes = WindowEvaluationModes.WholePartition | WindowEvaluationModes.Batch)]
    public double CumeDist() => AggregateFunction.NotInvoked<double>();
}
