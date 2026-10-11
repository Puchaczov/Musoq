using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class LibraryExecutionAggregateQueryTests : GenericEntityTestBase
{
    [TestMethod] public void ByteAggregates_ShouldPreserveSumAndReturnDecimalAverage() => AssertNumeric<byte, decimal>(1.5m);
    [TestMethod] public void SByteAggregates_ShouldPreserveSumAndReturnDecimalAverage() => AssertNumeric<sbyte, decimal>(1.5m);
    [TestMethod] public void Int16Aggregates_ShouldPreserveSumAndReturnDecimalAverage() => AssertNumeric<short, decimal>(1.5m);
    [TestMethod] public void UInt16Aggregates_ShouldPreserveSumAndReturnDecimalAverage() => AssertNumeric<ushort, decimal>(1.5m);
    [TestMethod] public void Int32Aggregates_ShouldPreserveSumAndReturnDecimalAverage() => AssertNumeric<int, decimal>(1.5m);
    [TestMethod] public void UInt32Aggregates_ShouldPreserveSumAndReturnDecimalAverage() => AssertNumeric<uint, decimal>(1.5m);
    [TestMethod] public void Int64Aggregates_ShouldPreserveSumAndReturnDecimalAverage() => AssertNumeric<long, decimal>(1.5m);
    [TestMethod] public void UInt64Aggregates_ShouldPreserveSumAndReturnDecimalAverage() => AssertNumeric<ulong, decimal>(1.5m);
    [TestMethod] public void DecimalAggregates_ShouldPreserveTheirConcreteResult() => AssertNumeric<decimal, decimal>(1.5m);
    [TestMethod] public void SingleAggregates_ShouldPreserveTheirConcreteResult() => AssertNumeric<float, float>(1.5f);
    [TestMethod] public void DoubleAggregates_ShouldPreserveTheirConcreteResult() => AssertNumeric<double, double>(1.5d);

    [TestMethod]
    public void IntegralAverage_WhenInputSumExceedsInt32_ShouldAccumulateInDecimal()
    {
        const string query = "select Avg(Value) as Average, Avg(distinct Value) as UniqueAverage from #schema.first()";
        NumericValue<int>[] input = [new(int.MaxValue, int.MaxValue), new(int.MaxValue, int.MaxValue)];
        using var compiled = CreateAndRunVirtualMachine(query, input);
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Average", typeof(decimal?)), ("UniqueAverage", typeof(decimal?)));
        Assert.HasCount(1, rows);
        Assert.AreEqual((decimal)int.MaxValue, rows[0].GetValue<decimal?>(0));
        Assert.AreEqual((decimal)int.MaxValue, rows[0].GetValue<decimal?>(1));
    }

    [TestMethod]
    public void CustomReset_WhenGroupsAndExecutionsDiffer_ShouldInitializeEachOwnedState()
    {
        const string query = """
            select Partition, SeededTotal(Amount) as Total, PositiveCount(Amount) as Positive
            from #schema.first() group by Partition order by Partition
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(query,
            LibraryExecutionFixture.CreateRows());

        AssertSeededGroups(compiled.Run(TestContext.CancellationToken));
        AssertSeededGroups(compiled.Run(TestContext.CancellationToken));
    }

    [TestMethod]
    public void CustomReset_WhenSourceIsEmpty_ShouldUseTheInitializedState()
    {
        const string query = "select SeededTotal(Amount) as Total from #schema.first()";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(query, []);
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Total", typeof(decimal)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(100m, rows[0].GetValue<decimal>(0));
    }

    [TestMethod]
    public void CustomReset_WhenScalarAggregateSubqueryIsEmpty_ShouldUseTheInitializedState()
    {
        const string query = """
            select (select SeededTotal(Amount) from #schema.first() where Position < 0) as Total
            from #schema.first() where Position = 1
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(query,
            LibraryExecutionFixture.CreateRows());
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Total", typeof(decimal?)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(100m, rows[0].GetValue<decimal?>(0));
    }

    [TestMethod]
    public void ReferenceKernelState_WhenGroupsAreIndependent_ShouldKeepTypedOwnedInstances()
    {
        const string query = """
            select Partition, ReferencePositiveCount(Amount) as Positive
            from #schema.first() group by Partition order by Partition
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(query,
            LibraryExecutionFixture.CreateRows());
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Partition", typeof(string)), ("Positive", typeof(long)));
        Assert.HasCount(2, rows);
        Assert.AreEqual("A", rows[0].GetValue<string>(0));
        Assert.AreEqual(2L, rows[0].GetValue<long>(1));
        Assert.AreEqual("B", rows[1].GetValue<string>(0));
        Assert.AreEqual(1L, rows[1].GetValue<long>(1));
    }

    [TestMethod]
    public void GenericCustomKernel_WhenDeclarationIsConcretized_ShouldReturnTypedTotals()
    {
        const string query = "select GenericTotal(Integral) as Integers, GenericTotal(Amount) as Decimals from #schema.first()";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(query,
            LibraryExecutionFixture.CreateRows());
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Integers", typeof(int?)), ("Decimals", typeof(decimal?)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(5, rows[0].GetValue<int?>(0));
        Assert.AreEqual(6m, rows[0].GetValue<decimal?>(1));
    }

    [TestMethod]
    public void DerivedLibrary_WhenAggregateSignatureIsOverridden_ShouldUseItsKernelAndInheritOtherOverloads()
    {
        const string query = "select Sum(Amount) as Positive, Sum(Integral) as Integers from #schema.first()";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, PositiveSumLibrary>(query,
            LibraryExecutionFixture.CreateRows());
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Positive", typeof(decimal?)), ("Integers", typeof(int?)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(9m, rows[0].GetValue<decimal?>(0));
        Assert.AreEqual(5, rows[0].GetValue<int?>(1));
    }

    [TestMethod]
    public void AggregateFilter_WhenAllValuesAreRejected_ShouldReturnNullAndZeroCount()
    {
        const string query = """
            select Sum(Amount) filter (where Position < 0) as Total,
                Avg(Integral) filter (where Position < 0) as Average,
                Count(Amount) filter (where Position < 0) as Values,
                Count(*) filter (where Position < 0) as Rows
            from #schema.first()
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(query,
            LibraryExecutionFixture.CreateRows());
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Total", typeof(decimal?)), ("Average", typeof(decimal?)),
            ("Values", typeof(long)), ("Rows", typeof(long)));
        Assert.HasCount(1, rows);
        Assert.IsNull(rows[0].GetValue<decimal?>(0));
        Assert.IsNull(rows[0].GetValue<decimal?>(1));
        Assert.AreEqual(0L, rows[0].GetValue<long>(2));
        Assert.AreEqual(0L, rows[0].GetValue<long>(3));
    }

    private void AssertNumeric<T, TResult>(TResult average)
        where T : struct, INumber<T>
        where TResult : struct, INumber<TResult>
    {
        const string query = """
            select Sum(Value) as Total, Sum(NullableValue) as NullableTotal, Avg(Value) as Average,
                Min(Value) as Minimum, Max(Value) as Maximum, Count(Value) as Values,
                Count(*) as Rows, CountDistinct(NullableValue) as UniqueValues,
                Avg(distinct Value) as UniqueAverage
            from #schema.first()
            """;
        NumericValue<T>[] input = [new(T.One, T.One), new(T.CreateChecked(2), null)];
        using var compiled = CreateAndRunVirtualMachine(query, input);
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Total", typeof(T?)), ("NullableTotal", typeof(T?)),
            ("Average", typeof(TResult?)), ("Minimum", typeof(T?)), ("Maximum", typeof(T?)), ("Values", typeof(long)),
            ("Rows", typeof(long)), ("UniqueValues", typeof(long)), ("UniqueAverage", typeof(TResult?)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(T.CreateChecked(3), rows[0].GetValue<T?>(0));
        Assert.AreEqual(T.One, rows[0].GetValue<T?>(1));
        Assert.AreEqual(average, rows[0].GetValue<TResult?>(2));
        Assert.AreEqual(T.One, rows[0].GetValue<T?>(3));
        Assert.AreEqual(T.CreateChecked(2), rows[0].GetValue<T?>(4));
        Assert.AreEqual(2L, rows[0].GetValue<long>(5));
        Assert.AreEqual(2L, rows[0].GetValue<long>(6));
        Assert.AreEqual(1L, rows[0].GetValue<long>(7));
        Assert.AreEqual(average, rows[0].GetValue<TResult?>(8));

        AssertEmptyNumeric<T, TResult>([]);
        AssertEmptyNumeric<T, TResult>([new(T.One, null)]);
    }

    private void AssertEmptyNumeric<T, TResult>(NumericValue<T>[] input)
        where T : struct, INumber<T>
        where TResult : struct, INumber<TResult>
    {
        const string query = """
            select Sum(NullableValue) as Total, Avg(NullableValue) as Average,
                Min(NullableValue) as Minimum, Max(NullableValue) as Maximum,
                Count(NullableValue) as Values, Count(*) as Rows,
                CountDistinct(NullableValue) as UniqueValues, Avg(distinct NullableValue) as UniqueAverage
            from #schema.first()
            """;
        using var compiled = CreateAndRunVirtualMachine(query, input);
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Total", typeof(T?)), ("Average", typeof(TResult?)),
            ("Minimum", typeof(T?)), ("Maximum", typeof(T?)), ("Values", typeof(long)), ("Rows", typeof(long)),
            ("UniqueValues", typeof(long)), ("UniqueAverage", typeof(TResult?)));
        Assert.HasCount(1, rows);
        Assert.IsNull(rows[0].GetValue<T?>(0));
        Assert.IsNull(rows[0].GetValue<TResult?>(1));
        Assert.IsNull(rows[0].GetValue<T?>(2));
        Assert.IsNull(rows[0].GetValue<T?>(3));
        Assert.AreEqual(0L, rows[0].GetValue<long>(4));
        Assert.AreEqual((long)input.Length, rows[0].GetValue<long>(5));
        Assert.AreEqual(0L, rows[0].GetValue<long>(6));
        Assert.IsNull(rows[0].GetValue<TResult?>(7));
    }

    private static void AssertSeededGroups(Tables.Table result)
    {
        var rows = result.ToArray();
        TableMaterializationTestHelper.AssertColumns(result, ("Partition", typeof(string)), ("Total", typeof(decimal)),
            ("Positive", typeof(long)));
        Assert.HasCount(2, rows);
        Assert.AreEqual("A", rows[0].GetValue<string>(0));
        Assert.AreEqual(104m, rows[0].GetValue<decimal>(1));
        Assert.AreEqual(2L, rows[0].GetValue<long>(2));
        Assert.AreEqual("B", rows[1].GetValue<string>(0));
        Assert.AreEqual(102m, rows[1].GetValue<decimal>(1));
        Assert.AreEqual(1L, rows[1].GetValue<long>(2));
    }

    public sealed record NumericValue<T>(T Value, T? NullableValue) where T : struct;

    public sealed class PositiveSumLibrary : Musoq.Plugins.LibraryBase
    {
        [Musoq.Plugins.Attributes.AggregateFunction(typeof(LibraryExecutionLibrary.PositiveTotalKernel))]
        public new decimal? Sum(decimal? value, [Musoq.Plugins.Attributes.AggregateParent] int parent = 0)
            => Musoq.Plugins.AggregateFunction.NotInvoked<decimal?>();
    }
}
