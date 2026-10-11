using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod]
    public void TypedOrderKeyContext_WhenInvokedRepeatedly_ShouldAllocateNoBoxesOrInvocationObjects()
    {
        var (allocated, total) = AllocationTestThread.Run(MeasureOrderKeyContext);
        Assert.AreEqual(50_000m, total);
        Assert.AreEqual(0L, allocated);
    }

    private static (long Allocated, decimal Total) MeasureOrderKeyContext()
    {
        int?[] values = [1, 2];
        int[] keys = [1, 2];
        int[] indices = [0, 1];
        var warm = ConsumeOrderKeyContext(values, keys, indices);
        if (warm != 50_000m)
            throw new InvalidOperationException("Typed order-key warmup returned incorrect values.");
        var before = GC.GetAllocatedBytesForCurrentThread();
        var total = ConsumeOrderKeyContext(values, keys, indices);
        return (GC.GetAllocatedBytesForCurrentThread() - before, total);
    }

    private static decimal ConsumeOrderKeyContext(int?[] values, int[] keys, int[] indices)
    {
        decimal total = 0;
        for (var iteration = 0; iteration < 10_000; iteration++)
        {
            var context = new WindowFrameContext<int?, int>(values, keys, indices, 1, 0, 1);
            total += KeyWeightedKernel.Get(in context)!.Value;
        }
        return total;
    }

    [TestMethod]
    public void TypedOrderKeyContext_WhenFrameIsBounded_ShouldReadSortedKeysAndNullableValues()
    {
        const string query = "select Position, KeyWeighted(Value) over (order by Position rows between 1 preceding and current row) as Total from #schema.first() order by Position";
        NumericRow<int>[] input = [new(4, 40), new(1, 10), new(3, null), new(2, 20)];
        using var compiled = CreateAndRunVirtualMachine<NumericRow<int>, OrderKeyLibrary>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Total", typeof(decimal?)));
        Assert.HasCount(4, rows);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, rows.Select(row => row.GetValue<int>(0)).ToArray());
        CollectionAssert.AreEqual(new decimal?[] { 10m, 50m, 40m, 160m }, rows.Select(row => row.GetValue<decimal?>(1)).ToArray());
    }

    [TestMethod]
    public void TypedOrderKeyContext_WhenFrameIsEmpty_ShouldReturnNullAndRespectDescendingOrder()
    {
        const string query = "select Position, KeyWeighted(Value) over (order by Position desc rows between 1 following and 1 following) as Total from #schema.first() order by Position";
        NumericRow<int>[] input = [new(1, 10), new(2, 20), new(3, 30)];
        using var compiled = CreateAndRunVirtualMachine<NumericRow<int>, OrderKeyLibrary>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Total", typeof(decimal?)));
        Assert.HasCount(3, rows);
        CollectionAssert.AreEqual(new decimal?[] { null, 10m, 40m }, rows.Select(row => row.GetValue<decimal?>(1)).ToArray());
    }

    [TestMethod]
    public void TypedOrderKeyContext_WhenRangePeersShareKeys_ShouldReadEachPeersExactKey()
    {
        const string query = "select Position, KeyWeighted(Value) over (order by Position range between unbounded preceding and current row) as Total from #schema.first() order by Position, Value";
        NumericRow<int>[] input = [new(2, 30), new(1, 10), new(1, 20)];
        using var compiled = CreateAndRunVirtualMachine<NumericRow<int>, OrderKeyLibrary>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Total", typeof(decimal?)));
        Assert.HasCount(3, rows);
        CollectionAssert.AreEqual(new decimal?[] { 30m, 30m, 90m }, rows.Select(row => row.GetValue<decimal?>(1)).ToArray());
    }

    [TestMethod]
    public void TypedOrderKeyContext_WhenRangeOffsetsUseVolatileKeys_ShouldReuseTheSortingCapture()
    {
        const string query = "select Position, KeyWeighted(Value) over (order by RangeCountingKey(Position) range between 1 preceding and current row) as Total from #schema.first() order by Position";
        NumericRow<int>[] input = [new(1, 10), new(2, 20), new(3, 30)];
        OrderKeyLibrary.RangeKeyCalls = 0;
        using var compiled = CreateAndRunVirtualMachine<NumericRow<int>, OrderKeyLibrary>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Total", typeof(decimal?)));
        Assert.HasCount(3, rows);
        CollectionAssert.AreEqual(new decimal?[] { 10m, 50m, 130m }, rows.Select(row => row.GetValue<decimal?>(1)).ToArray());
        Assert.AreEqual(3, OrderKeyLibrary.RangeKeyCalls);
    }

    [TestMethod]
    public void TypedOrderKeyContext_WhenOrderExpressionIsVolatile_ShouldEvaluateItOncePerRow()
    {
        const string query = "select Position, KeyWeighted(Value) over (order by CountingKey(Position) rows between 1 preceding and current row) as Total from #schema.first() order by Position";
        NumericRow<int>[] input = [new(1, 10), new(2, 20), new(3, 30)];
        OrderKeyLibrary.KeyCalls = 0;
        using var compiled = CreateAndRunVirtualMachine<NumericRow<int>, OrderKeyLibrary>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Total", typeof(decimal?)));
        Assert.HasCount(3, rows);
        CollectionAssert.AreEqual(new decimal?[] { 10m, 50m, 130m }, rows.Select(row => row.GetValue<decimal?>(1)).ToArray());
        Assert.AreEqual(3, OrderKeyLibrary.KeyCalls);
    }

    [TestMethod]
    public void TypedOrderKeyContext_WhenKeysAreComposite_ShouldKeepTheConcreteTupleAndPartitionBoundaries()
    {
        const string query = "select Partition, Position, FirstLabel(Value) over (partition by Partition order by Position, Label rows between unbounded preceding and current row) as Label from #schema.first() order by Partition, Position, Label";
        OrderKeyRow[] input = [new(2, 1, 1, "z"), new(1, 2, 2, "b"), new(1, 1, 1, "a"), new(2, 2, 2, "y")];
        using var compiled = CreateAndRunVirtualMachine<OrderKeyRow, OrderKeyLibrary>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Partition", typeof(int)), ("Position", typeof(int)), ("Label", typeof(string)));
        Assert.HasCount(4, rows);
        CollectionAssert.AreEqual(new[] { "a", "a", "z", "z" }, rows.Select(row => row.GetValue<string>(2)).ToArray());
    }

    [TestMethod]
    public void TypedOrderKeyContext_WhenKeysAreNullable_ShouldPreserveNullAndConcreteKeyTypes()
    {
        const string query = "select Position, NullableKeyTotal(Value) over (order by Key rows between unbounded preceding and current row) as Total from #schema.first() order by Position";
        NullableOrderKeyRow[] input = [new(3, 2, 30), new(1, null, 10), new(2, 1, 20)];
        using var compiled = CreateAndRunVirtualMachine<NullableOrderKeyRow, OrderKeyLibrary>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Total", typeof(decimal?)));
        Assert.HasCount(3, rows);
        CollectionAssert.AreEqual(new decimal?[] { 0m, 20m, 80m }, rows.Select(row => row.GetValue<decimal?>(1)).ToArray());
    }

    [TestMethod]
    [DataRow("select KeyWeighted(Value) over () from #schema.first()", "concrete ORDER BY keys")]
    [DataRow("select KeyWeighted(Value) over (order by ToString(Position)) from #schema.first()", "Expected context")]
    public void TypedOrderKeyContext_WhenKeySignatureIsUnavailable_ShouldDiagnoseTheQuery(string query, string reason)
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine<NumericRow<int>, OrderKeyLibrary>(query, [new(1, 1)]));
        Assert.AreEqual(DiagnosticCode.MQ3071_SourceContractError, exception.PrimaryEnvelope.Code);
        Assert.Contains(reason, exception.Message);
    }

    public sealed record NullableOrderKeyRow(int Position, int? Key, int? Value);

    public sealed record OrderKeyRow(int Partition, int Position, int? Value, string Label);

    public sealed class OrderKeyLibrary : LibraryBase
    {
        public static int KeyCalls;
        public static int RangeKeyCalls;

        [BindableMethod, NonDeterministic]
        public static int RangeCountingKey(int value)
        {
            Interlocked.Increment(ref RangeKeyCalls);
            return value;
        }

        [BindableMethod, NonDeterministic]
        public static int CountingKey(int value)
        {
            Interlocked.Increment(ref KeyCalls);
            return value;
        }

        [WindowFunction(KernelType = typeof(KeyWeightedKernel), ContextType = typeof(WindowFrameContext<,>),
            Modes = WindowEvaluationModes.WholePartition | WindowEvaluationModes.Running | WindowEvaluationModes.RecomputeFrames,
            EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
        public decimal? KeyWeighted(int? value) => AggregateFunction.NotInvoked<decimal?>();

        [WindowFunction(KernelType = typeof(NullableKeyTotalKernel), ContextType = typeof(WindowFrameContext<,>),
            Modes = WindowEvaluationModes.Running | WindowEvaluationModes.RecomputeFrames,
            EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
        public decimal? NullableKeyTotal(int? value) => AggregateFunction.NotInvoked<decimal?>();

        [WindowFunction(KernelType = typeof(FirstLabelKernel), ContextType = typeof(WindowFrameContext<,>),
            Modes = WindowEvaluationModes.Running | WindowEvaluationModes.RecomputeFrames,
            EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
        public string? FirstLabel(int? value) => AggregateFunction.NotInvoked<string?>();
    }

    public static class KeyWeightedKernel
    {
        public static decimal? Get(in WindowFrameContext<int?, int> context)
        {
            decimal? total = null;
            for (var index = context.FrameStart; index <= context.FrameEnd; index++)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                var value = context.Read(index);
                if (!value.HasValue)
                    continue;
                var weighted = checked((decimal)value.Value * context.ReadOrderKey(index));
                total = checked((total ?? 0m) + weighted);
            }
            return total;
        }
    }

    public static class NullableKeyTotalKernel
    {
        public static decimal? Get(in WindowFrameContext<int?, int?> context)
        {
            decimal? total = null;
            for (var index = context.FrameStart; index <= context.FrameEnd; index++)
            {
                var value = context.Read(index);
                if (value.HasValue)
                    total = checked((total ?? 0m) + (decimal)value.Value * (context.ReadOrderKey(index) ?? 0));
            }
            return total;
        }
    }

    public static class FirstLabelKernel
    {
        public static string? Get(in WindowFrameContext<int?, (int, string)> context) =>
            context.IsEmpty ? null : context.ReadOrderKey(context.FrameStart).Item2;
    }
}
