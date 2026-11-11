using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionWindowQueryTestsFixtures
{
    public sealed class InvalidWindowLibrary : LibraryBase
    {
        [WindowFunction(
            KernelType = typeof(BoxedContextKernel),
            ContextType = typeof(WindowFrameContext<>),
            Modes = WindowEvaluationModes.WholePartition
        )]
        public long BoxedWindow(System.IComparable value) => AggregateFunction.NotInvoked<long>();

        [WindowFunction(
            KernelType = typeof(ErasedContextKernel),
            ContextType = typeof(WindowFrameContext<>),
            Modes = WindowEvaluationModes.RecomputeFrames
        )]
        public decimal? BrokenContext(decimal? amount) => AggregateFunction.NotInvoked<decimal?>();

        [WindowFunction(
            KernelType = typeof(SumAggregateKernel<decimal>),
            BatchKernelType = typeof(ErasedBatchKernel),
            Modes = WindowEvaluationModes.RecomputeFrames | WindowEvaluationModes.Batch
        )]
        public decimal? BrokenBatch(decimal? amount) => AggregateFunction.NotInvoked<decimal?>();

        [WindowFunction(
            KernelType = typeof(LibraryExecutionAggregateQueryTestsFixtures.MissingResetKernel),
            Modes = WindowEvaluationModes.WholePartition
        )]
        public decimal? BrokenWindow(decimal? amount) => AggregateFunction.NotInvoked<decimal?>();

        [WindowFunction(KernelType = typeof(SumAggregateKernel<decimal>), Modes = WindowEvaluationModes.WholePartition)]
        public decimal? PartitionOnly(decimal? amount) => AggregateFunction.NotInvoked<decimal?>();
    }

    public static class ErasedContextKernel
    {
        public static decimal? Get(in WindowFrameContext<object> context) => null;
    }

    public static class BoxedContextKernel
    {
        public static long Get(in WindowFrameContext<System.IComparable> context) => context.Count;
    }

    public static class ErasedBatchKernel
    {
        public static bool TryEvaluate(in WindowBatchContext<object, decimal?> context) => false;
    }
}
