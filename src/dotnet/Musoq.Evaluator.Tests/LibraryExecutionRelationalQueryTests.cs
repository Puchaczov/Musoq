using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using Musoq.Evaluator.Helpers;
using Musoq.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LibraryExecutionRelationalQueryTests : GenericEntityTestBase
{
    [TestMethod]
    public void Join_WhenSourceContextsAreInjected_ShouldRetainEachConcreteSource()
    {
        const string query = """
            select a.SourcePosition() as FirstPosition, b.SourcePosition() as SecondPosition,
                a.Amount + b.Amount as Total
            from #schema.first() a inner join #schema.first() b on a.Position = b.Position
            order by FirstPosition
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result,
            ("FirstPosition", typeof(int)), ("SecondPosition", typeof(int)), ("Total", typeof(decimal?)));
        Assert.HasCount(5, rows);
        decimal?[] expected = [2.5m, 5.5m, null, -6m, 10m];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(1));
            Assert.AreEqual(expected[index], rows[index].GetValue<decimal?>(2));
        }
    }

    [TestMethod]
    public void ValueSource_WhenJoinedAndSorted_ShouldKeepItsTypedInjectedContext()
    {
        const string query = """
            select a.ValueSourcePosition() as FirstPosition, b.ValueSourcePosition() as SecondPosition,
                a.Amount + b.Amount as Total
            from #schema.first() a inner join #schema.first() b on a.Position = b.Position
            order by FirstPosition
            """;
        LibraryExecutionValueEntity[] input = [new(1, 0, 1.25m), new(2, 1, 2.75m), new(3, 0, -3m)];
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionValueEntity, LibraryExecutionLibrary>(query, input);
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result,
            ("FirstPosition", typeof(int)), ("SecondPosition", typeof(int)), ("Total", typeof(decimal)));
        Assert.HasCount(3, rows);
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(1));
            Assert.AreEqual(input[index].Amount * 2, rows[index].GetValue<decimal>(2));
        }
    }
    [TestMethod]
    public void MaterializedCte_WhenJoined_ShouldPreserveInjectedValues()
    {
        const string query = """
            with positions as (select SourcePosition() as Position, Amount from #schema.first())
            select a.Position as FirstPosition, b.Position as SecondPosition,
                a.Amount + b.Amount as Total
            from positions a inner join positions b on a.Position = b.Position
            order by FirstPosition
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result,
            ("FirstPosition", typeof(int)), ("SecondPosition", typeof(int)), ("Total", typeof(decimal?)));
        Assert.HasCount(5, rows);
        decimal?[] expected = [2.5m, 5.5m, null, -6m, 10m];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(1));
            Assert.AreEqual(expected[index], rows[index].GetValue<decimal?>(2));
        }
    }

    [TestMethod]
    public void LeftJoin_WhenNoSourceMatches_ShouldPreserveNullableValuesAndSurvivingContext()
    {
        const string query = """
            select a.SourcePosition() as Position, b.Amount as Amount
            from #schema.first() a left join #schema.first() b
                on a.Position = b.Position and b.Position > 2
            order by Position
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Amount", typeof(decimal?)));
        Assert.HasCount(5, rows);
        decimal?[] expected = [null, null, null, -3m, 5m];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(expected[index], rows[index].GetValue<decimal?>(1));
        }
    }

    [TestMethod]
    public void ValueSource_WhenOuterJoined_ShouldDistinguishMissingContextFromDefaultValue()
    {
        const string query = """
            select a.Position as Position, b.ValueSourcePosition() as OtherPosition
            from #schema.first() a left outer join #schema.first() b
                on a.Position = b.Position and b.Position > 0
            order by Position
            """;
        LibraryExecutionValueEntity[] input = [new(0, 0, 1m), new(1, 0, 2m)];
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionValueEntity, LibraryExecutionLibrary>(query, input);
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("OtherPosition", typeof(int?)));
        Assert.HasCount(2, rows);
        Assert.AreEqual(0, rows[0].GetValue<int>(0));
        Assert.IsNull(rows[0].GetValue<int?>(1));
        Assert.AreEqual(1, rows[1].GetValue<int>(0));
        Assert.AreEqual(1, rows[1].GetValue<int?>(1));
    }

    [TestMethod]
    public void NumericScalar_WhenTypeNameIsRequested_ShouldPreserveNullableTypeSemantics()
    {
        const string query = "select Position, GetTypeName(Position) as PositionType, GetTypeName(Integral) as IntegralType from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result,
            ("Position", typeof(int)), ("PositionType", typeof(string)), ("IntegralType", typeof(string)));
        Assert.HasCount(5, rows);
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual("System.Int32", rows[index].GetValue<string>(1));
            Assert.AreEqual(index == 2 ? null : "System.Int32", rows[index].GetValue<string>(2));
        }
    }

    [TestMethod]
    public void TypedScalar_WhenItsSignatureErasesNumericArguments_ShouldRejectTheDeclaration()
    {
        const string query = "select ErasedScalar(Position) as Value from #schema.first()";
        var error = Assert.Throws<InvalidOperationException>(() =>
            InstanceCreator.CompileForTypedExecution<ScalarOutput>(query, Guid.NewGuid().ToString(),
                LibraryExecutionFixture.CreateProvider(), LoggerResolver));

        Assert.Contains("MQ3071", error.Message);
        Assert.Contains("ErasedScalar", error.Message);
        Assert.Contains("parameter", error.Message);
    }

    [TestMethod]
    public void CompositeDistinctSet_WhenMaterializedAndPaged_ShouldRetainNullableConcreteKeys()
    {
        const string query = """
            with keys as (
                select distinct Partition, Integral from #schema.first()
                union (Partition, Integral)
                select Partition, Integral from #schema.first()
            )
            select Partition, Integral from keys order by Partition, Integral skip 1 take 3
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Partition", typeof(string)), ("Integral", typeof(int?)));
        Assert.HasCount(3, rows);
        Assert.AreEqual("A", rows[0].GetValue<string>(0));
        Assert.AreEqual(1, rows[0].GetValue<int?>(1));
        Assert.AreEqual("A", rows[1].GetValue<string>(0));
        Assert.AreEqual(2, rows[1].GetValue<int?>(1));
        Assert.AreEqual("B", rows[2].GetValue<string>(0));
        Assert.AreEqual(-3, rows[2].GetValue<int?>(1));
    }

    [TestMethod]
    public void RecursiveMaterialization_WhenFullyConsumed_ShouldKeepConcreteIntegerRows()
    {
        const string query = """
            with recursive counter (Value) as (
                select Value from values {(Value: 1)} seed
                union all select c.Value + 1 from counter c where c.Value < 5
            ) select Value from counter order by Value
            """;
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, LibraryExecutionLibrary>(
            query, LibraryExecutionFixture.CreateRows());
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(int)));
        Assert.HasCount(5, rows);
        for (var index = 0; index < rows.Length; index++)
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
    }

    [TestMethod]
    public void TypedScalar_WhenAnInterfaceBoxesNumericArguments_ShouldRejectTheConversion()
    {
        const string query = "select ErasedInterfaceScalar(Position) as Value from #schema.first()";
        var error = Assert.Throws<InvalidOperationException>(() =>
            InstanceCreator.CompileForTypedExecution<IntegerOutput>(query, Guid.NewGuid().ToString(),
                LibraryExecutionFixture.CreateProvider(), LoggerResolver));

        Assert.Contains("MQ3071", error.Message);
        Assert.Contains("ErasedInterfaceScalar", error.Message);
        Assert.Contains("boxing", error.Message);
    }

    [TestMethod]
    public void TypedIndexedValues_WhenRead_ShouldPreserveDefaultsAndNullableElements()
    {
        const string query = """
            select Position, Numbers[-1] as Last, Numbers[8] as Missing,
                Amounts[1] as NullableAmount, Costs['present'] as Cost, Costs['missing'] as MissingCost
            from #schema.first() order by Position
            """;
        IndexedEntity[] input =
        [
            new(1, [2, 3], [1m, null], new Dictionary<string, decimal> { ["present"] = 4m }),
            new(2, [], [], new Dictionary<string, decimal>())
        ];
        using var tableQuery = CreateAndRunVirtualMachine<IndexedEntity, LibraryExecutionLibrary>(query, input);
        var table = tableQuery.Run(TestContext.CancellationToken);
        var tableRows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Last", typeof(int)),
            ("Missing", typeof(int)), ("NullableAmount", typeof(decimal?)), ("Cost", typeof(decimal)), ("MissingCost", typeof(decimal)));
        Assert.HasCount(2, tableRows);
        Assert.AreEqual(3, tableRows[0].GetValue<int>(1));
        Assert.AreEqual(0, tableRows[1].GetValue<int>(1));
        Assert.IsNull(tableRows[0].GetValue<decimal?>(3));
        Assert.AreEqual(4m, tableRows[0].GetValue<decimal>(4));

        var compiled = InstanceCreator.CompileForTypedProfile<IndexedOutput>(query, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(input), LoggerResolver);
        var rows = compiled.RunWithProfile(TestContext.CancellationToken).Rows.ToArray();

        Assert.HasCount(2, rows);
        Assert.AreEqual(new IndexedOutput(1, 3, 0, null, 4m, 0m), rows[0]);
        Assert.AreEqual(new IndexedOutput(2, 0, 0, null, 0m, 0m), rows[1]);
    }

    [TestMethod]
    public void TypedIndexedSource_WhenOnlyEnumerableIsDeclared_ShouldRejectErasedAccess()
    {
        const string query = "select Items[0] as Value from #schema.first()";
        var error = Assert.Throws<InvalidOperationException>(() =>
            InstanceCreator.CompileForTypedExecution<IntegerOutput>(query, Guid.NewGuid().ToString(),
                LibraryExecutionFixture.CreateProvider<EnumerableEntity>([new(Enumerable.Range(1, 3))]), LoggerResolver));

        Assert.Contains("MQ3017", error.Message);
        Assert.Contains("not an array or indexable type", error.Message);
    }

    [TestMethod]
    public void TypedIndexedStruct_WhenExecuted_ShouldRetainItsConcreteCollection()
    {
        const string query = "select Amounts[0] as Amount, Amounts[-1] as Last from #schema.first()";
        StructListEntity[] input = [new(new NumericList([1m, 2m]))];
        using var compiled = CreateAndRunVirtualMachine<StructListEntity, LibraryExecutionLibrary>(query, input);
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Amount", typeof(decimal)), ("Last", typeof(decimal)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(1m, rows[0].GetValue<decimal>(0));
        Assert.AreEqual(2m, rows[0].GetValue<decimal>(1));
    }

    [TestMethod]
    public void TypedIndexedNumericOperations_WhenRepeated_ShouldAllocateNoBoxedValuesOrArguments()
    {
        var (allocated, total) = AllocationTestThread.Run(MeasureIndexedNumericOperations);
        Assert.AreEqual(50_000m, total);
        Assert.AreEqual(0L, allocated);
    }

    private static (long Allocated, decimal Total) MeasureIndexedNumericOperations()
    {
        var values = new NumericList([1m, 2m]);
        var dictionary = new Dictionary<int, decimal> { [1] = 3m };
        var warmTotal = ConsumeIndexedNumericValues(values, dictionary);
        if (warmTotal != 50_000m)
            throw new InvalidOperationException("Indexed numeric warmup returned incorrect values.");
        var before = GC.GetAllocatedBytesForCurrentThread();
        var total = ConsumeIndexedNumericValues(values, dictionary);
        return (GC.GetAllocatedBytesForCurrentThread() - before, total);
    }

    private static decimal ConsumeIndexedNumericValues(NumericList values, Dictionary<int, decimal> dictionary)
    {
        decimal total = 0;
        for (var index = 0; index < 10_000; index++)
        {
            total += SafeArrayAccess.GetTypedReadOnlyListElement<NumericList, decimal>(values, -1);
            total += SafeArrayAccess.GetTypedDictionaryValue<Dictionary<int, decimal>, int, decimal>(dictionary, 1);
        }
        return total;
    }

    public sealed record StructListEntity(NumericList Amounts);
    public readonly struct NumericList(decimal[] values) : IReadOnlyList<decimal>
    {
        public int Count => values.Length;
        public decimal this[int index] => values[index];
        public IEnumerator<decimal> GetEnumerator() => ((IEnumerable<decimal>)values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed record IndexedEntity(int Position, int[] Numbers, decimal?[] Amounts, Dictionary<string, decimal> Costs);
    public readonly record struct IndexedOutput(int Position, int Last, int Missing, decimal? NullableAmount, decimal Cost, decimal MissingCost);
    public sealed record EnumerableEntity(IEnumerable<int> Items);
    public readonly record struct IntegerOutput(int Value);
    public readonly record struct ScalarOutput(string Value);

}
