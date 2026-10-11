using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod]
    public void WindowContext_WhenSignatureRequiresBoxing_ShouldRejectTheSqlCall()
    {
        const string query = "select BoxedWindow(Position) over () as Value from #schema.first()";
        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<LibraryExecutionEntity, InvalidWindowLibrary>(query,
                LibraryExecutionFixture.CreateRows()));

        Assert.Contains("MQ3071", error.Message);
        Assert.Contains("BoxedWindow", error.Message);
        Assert.Contains("boxed argument", error.Message);
    }

    [TestMethod]
    [DataRow("BrokenContext", "Get(in WindowFrameContext")]
    [DataRow("BrokenBatch", "TryEvaluate(in WindowBatchContext")]
    public void WindowOperation_WhenContextSignatureIsErased_ShouldReportTheContract(string function, string operation)
    {
        var query = $"select {function}(Amount) over (order by Position rows between 1 preceding and current row) as Value from #schema.first()";
        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<LibraryExecutionEntity, InvalidWindowLibrary>(query,
                LibraryExecutionFixture.CreateRows()));

        Assert.Contains("MQ3071", error.Message);
        Assert.Contains(function, error.Message);
        Assert.Contains(operation, error.Message);
    }

    [TestMethod]
    public void WindowKernel_WhenResetIsMissing_ShouldReportTheKernelContract()
    {
        const string query = "select BrokenWindow(Amount) over () as Value from #schema.first()";

        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<LibraryExecutionEntity, InvalidWindowLibrary>(query,
                LibraryExecutionFixture.CreateRows()));

        Assert.Contains("MQ3071", error.Message);
        Assert.Contains("BrokenWindow", error.Message);
        Assert.Contains("Reset(ref State)", error.Message);
    }

    [TestMethod]
    public void WindowKernel_WhenFrameModeIsNotRegistered_ShouldReportTheUnsupportedMode()
    {
        const string query = """
            select PartitionOnly(Amount) over (order by Position rows between 1 preceding and current row) as Value
            from #schema.first()
            """;

        var error = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<LibraryExecutionEntity, InvalidWindowLibrary>(query,
                LibraryExecutionFixture.CreateRows()));

        Assert.Contains("MQ3071", error.Message);
        Assert.Contains("PartitionOnly", error.Message);
        Assert.Contains("RecomputeFrames", error.Message);
    }

    public sealed class InvalidWindowLibrary : LibraryBase
    {
        [WindowFunction(KernelType = typeof(BoxedContextKernel), ContextType = typeof(WindowFrameContext<>),
            Modes = WindowEvaluationModes.WholePartition)]
        public long BoxedWindow(System.IComparable value) => AggregateFunction.NotInvoked<long>();

        [WindowFunction(KernelType = typeof(ErasedContextKernel), ContextType = typeof(WindowFrameContext<>),
            Modes = WindowEvaluationModes.RecomputeFrames)]
        public decimal? BrokenContext(decimal? amount) => AggregateFunction.NotInvoked<decimal?>();

        [WindowFunction(KernelType = typeof(SumAggregateKernel<decimal>), BatchKernelType = typeof(ErasedBatchKernel),
            Modes = WindowEvaluationModes.RecomputeFrames | WindowEvaluationModes.Batch)]
        public decimal? BrokenBatch(decimal? amount) => AggregateFunction.NotInvoked<decimal?>();

        [WindowFunction(KernelType = typeof(LibraryExecutionAggregateQueryTests.MissingResetKernel),
            Modes = WindowEvaluationModes.WholePartition)]
        public decimal? BrokenWindow(decimal? amount) => AggregateFunction.NotInvoked<decimal?>();

        [WindowFunction(KernelType = typeof(SumAggregateKernel<decimal>),
            Modes = WindowEvaluationModes.WholePartition)]
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
