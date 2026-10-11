using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionAggregateQueryTests
{
    [TestMethod] public void ByteSum_WhenLimitIsExceeded_ShouldThrowCheckedOverflow() => AssertSumOverflow(byte.MaxValue);
    [TestMethod] public void SByteSum_WhenLimitIsExceeded_ShouldThrowCheckedOverflow() => AssertSumOverflow(sbyte.MaxValue);
    [TestMethod] public void Int16Sum_WhenLimitIsExceeded_ShouldThrowCheckedOverflow() => AssertSumOverflow(short.MaxValue);
    [TestMethod] public void UInt16Sum_WhenLimitIsExceeded_ShouldThrowCheckedOverflow() => AssertSumOverflow(ushort.MaxValue);
    [TestMethod] public void Int32Sum_WhenLimitIsExceeded_ShouldThrowCheckedOverflow() => AssertSumOverflow(int.MaxValue);
    [TestMethod] public void UInt32Sum_WhenLimitIsExceeded_ShouldThrowCheckedOverflow() => AssertSumOverflow(uint.MaxValue);
    [TestMethod] public void Int64Sum_WhenLimitIsExceeded_ShouldThrowCheckedOverflow() => AssertSumOverflow(long.MaxValue);
    [TestMethod] public void UInt64Sum_WhenLimitIsExceeded_ShouldThrowCheckedOverflow() => AssertSumOverflow(ulong.MaxValue);
    [TestMethod] public void DecimalSum_WhenLimitIsExceeded_ShouldThrowCheckedOverflow() => AssertSumOverflow(decimal.MaxValue);

    [TestMethod]
    public void SignedSum_WhenLargeValuesCancelInSerialOrder_ShouldReturnExactResult()
    {
        const string query = "select Sum(Value) as Total from #schema.first()";
        NumericValue<long>[] input = [new(long.MaxValue, null), new(-long.MaxValue, null), new(1, null)];
        using var compiled = CreateAndRunVirtualMachine(query, input);
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Total", typeof(long?)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(1L, rows[0].GetValue<long?>(0));
    }

    [TestMethod]
    public void FloatingAggregates_WhenValuesAreOrderSensitive_ShouldPreserveSerialRounding()
    {
        const string query = "select Sum(Value) as Total, Avg(Value) as Average from #schema.first()";
        NumericValue<double>[] input = [new(1e16, null), new(1d, null), new(-1e16, null)];
        using var compiled = CreateAndRunVirtualMachine(query, input);
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Total", typeof(double?)), ("Average", typeof(double?)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(0d, rows[0].GetValue<double?>(0));
        Assert.AreEqual(0d, rows[0].GetValue<double?>(1));
    }

    [TestMethod]
    public void GroupedIntegralAggregates_WhenInputsAreSeeded_ShouldMatchIndependentDecimalReference()
    {
        const string query = """
            select Key, Sum(Value) as Total, Avg(Value) as Average, Min(Value) as Minimum,
                Max(Value) as Maximum, Count(Value) as Values, CountDistinct(Value) as UniqueValues,
                Avg(distinct Value) as UniqueAverage
            from #schema.first() group by Key order by Key
            """;
        var input = Enumerable.Range(0, 1_000).Select(static index => new GroupedNumericValue(index % 11,
            index % 7 == 0 ? null : index * 7919 % 2001 - 1000)).ToArray();
        using var compiled = CreateAndRunVirtualMachine(query, input);
        var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();
        var expected = input.GroupBy(static row => row.Key).OrderBy(static group => group.Key).ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Key", typeof(int)), ("Total", typeof(int?)),
            ("Average", typeof(decimal?)), ("Minimum", typeof(int?)), ("Maximum", typeof(int?)),
            ("Values", typeof(long)), ("UniqueValues", typeof(long)), ("UniqueAverage", typeof(decimal?)));
        Assert.HasCount(11, rows);
        for (var index = 0; index < expected.Length; index++)
            AssertReferenceGroup(rows[index], expected[index]);
    }

    private static void AssertReferenceGroup(Tables.Row row, IGrouping<int, GroupedNumericValue> expected)
    {
        var values = expected.Where(static item => item.Value.HasValue).Select(static item => item.Value!.Value).ToArray();
        var distinct = values.Distinct().ToArray();
        Assert.AreEqual(expected.Key, row.GetValue<int>(0));
        Assert.AreEqual(values.Sum(), row.GetValue<int?>(1));
        Assert.AreEqual(values.Select(static value => (decimal)value).Average(), row.GetValue<decimal?>(2));
        Assert.AreEqual(values.Min(), row.GetValue<int?>(3));
        Assert.AreEqual(values.Max(), row.GetValue<int?>(4));
        Assert.AreEqual((long)values.Length, row.GetValue<long>(5));
        Assert.AreEqual((long)distinct.Length, row.GetValue<long>(6));
        Assert.AreEqual(distinct.Select(static value => (decimal)value).Average(), row.GetValue<decimal?>(7));
    }

    private void AssertSumOverflow<T>(T maximum) where T : struct, INumber<T>
    {
        const string query = "select Sum(Value) as Total from #schema.first()";
        NumericValue<T>[] input = [new(maximum, null), new(T.One, null)];
        using var compiled = CreateAndRunVirtualMachine(query, input);

        var error = Assert.Throws<QueryExecutionException>(() => compiled.Run(TestContext.CancellationToken).ToArray());

        Assert.IsInstanceOfType<OverflowException>(error.InnerException);
    }

    public sealed record GroupedNumericValue(int Key, int? Value);
}
