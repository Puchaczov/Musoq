using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins.Attributes;

using static Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTestsFixtures;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
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
}
