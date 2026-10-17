using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod]
    public void CustomOrdinalKernel_Qualify_ShouldKeepNonPositiveResultsWithoutAnOrdinalGuarantee()
    {
        const string sql = "select Position, RowNumber() over (order by Value) as Ordinal from #schema.first() qualify Ordinal <= 0 order by Position";
        NumericRow<int>[] input = [new(1, 1), new(2, 2), new(3, 3)];
        using var compiled = CreateAndRunVirtualMachine<NumericRow<int>, ShiftedOrdinalLibrary>(sql, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Ordinal", typeof(long)));
        var rows = result.Select(row => (row.GetValue<int>(0), row.GetValue<long>(1))).ToArray();
        CollectionAssert.AreEqual(new (int, long)[] { (1, -1L), (2, 0L) }, rows);
    }

    [TestMethod]
    public void CustomOrdinalKernel_FusedRanking_ShouldInvokeLibraryOperationsForEveryResult()
    {
        const string sql = "select Position, RowNumber() over (order by Value) as Ordinal, Rank() over (order by Value) as Rank from #schema.first() order by Position";
        NumericRow<int>[] input = [new(1, 1), new(2, 2), new(3, 2)];
        using var compiled = CreateAndRunVirtualMachine<NumericRow<int>, ShiftedOrdinalLibrary>(sql, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Ordinal", typeof(long)), ("Rank", typeof(long)));
        var rows = result.Select(row => (row.GetValue<int>(0), row.GetValue<long>(1), row.GetValue<long>(2))).ToArray();
        CollectionAssert.AreEqual(new (int, long, long)[] { (1, -1L, 1L), (2, 0L, 2L), (3, 1L, 2L) }, rows);
    }

    [TestMethod]
    public void AliasedPartitionKernel_ShouldUseDeclaredFrameSemanticsInsteadOfItsSqlName()
    {
        const string sql = "select Position, PartitionBuckets(2) over (order by Value rows between current row and current row) as Bucket from #schema.first() order by Position";
        NumericRow<int>[] input = [new(1, 1), new(2, 2), new(3, 3)];
        using var compiled = CreateAndRunVirtualMachine<NumericRow<int>, ShiftedOrdinalLibrary>(sql, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Bucket", typeof(long)));
        var rows = result.Select(row => (row.GetValue<int>(0), row.GetValue<long>(1))).ToArray();
        CollectionAssert.AreEqual(new (int, long)[] { (1, 1L), (2, 1L), (3, 2L) }, rows);
    }

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
