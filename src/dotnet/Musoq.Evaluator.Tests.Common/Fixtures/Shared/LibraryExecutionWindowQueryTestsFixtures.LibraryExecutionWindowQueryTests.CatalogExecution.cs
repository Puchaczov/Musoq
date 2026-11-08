using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionWindowQueryTestsFixtures
{

    public sealed class ShiftedOrdinalLibrary : LibraryBase
    {
        [WindowFunction(KernelType = typeof(ShiftedOrdinalKernel), RankingKind = WindowRankingKind.RowNumber,
            RequiresPeerMetadata = false, RequiresOrderBy = true,
            Modes = WindowEvaluationModes.WholePartition | WindowEvaluationModes.Batch)]
        public new long RowNumber() => AggregateFunction.NotInvoked<long>();

        [WindowFunction(Name = "PartitionBuckets", KernelType = typeof(NtileKernel),
            ContextType = typeof(WindowFrameContext<>), IgnoresFrame = true, RequiresOrderBy = true,
            Modes = WindowEvaluationModes.WholePartition)]
        public long DeclarePartitionBuckets(int buckets) => AggregateFunction.NotInvoked<long>();
    }

    public static class ShiftedOrdinalKernel
    {
        public static long Get(in WindowRankingContext context) => context.Position - 1L;
    }

}
