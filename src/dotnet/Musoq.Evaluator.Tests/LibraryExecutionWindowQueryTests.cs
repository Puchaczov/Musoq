using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class LibraryExecutionWindowQueryTests : GenericEntityTestBase
{
    [TestMethod] public void ByteWindows_ShouldKeepConcreteNumericResults() => AssertNumeric<byte, decimal>(1.5m);
    [TestMethod] public void SByteWindows_ShouldKeepConcreteNumericResults() => AssertNumeric<sbyte, decimal>(1.5m);
    [TestMethod] public void Int16Windows_ShouldKeepConcreteNumericResults() => AssertNumeric<short, decimal>(1.5m);
    [TestMethod] public void UInt16Windows_ShouldKeepConcreteNumericResults() => AssertNumeric<ushort, decimal>(1.5m);
    [TestMethod] public void Int32Windows_ShouldKeepConcreteNumericResults() => AssertNumeric<int, decimal>(1.5m);
    [TestMethod] public void UInt32Windows_ShouldKeepConcreteNumericResults() => AssertNumeric<uint, decimal>(1.5m);
    [TestMethod] public void Int64Windows_ShouldKeepConcreteNumericResults() => AssertNumeric<long, decimal>(1.5m);
    [TestMethod] public void UInt64Windows_ShouldKeepConcreteNumericResults() => AssertNumeric<ulong, decimal>(1.5m);
    [TestMethod] public void DecimalWindows_ShouldKeepConcreteNumericResults() => AssertNumeric<decimal, decimal>(1.5m);
    [TestMethod] public void SingleWindows_ShouldKeepConcreteNumericResults() => AssertNumeric<float, float>(1.5f);
    [TestMethod] public void DoubleWindows_ShouldKeepConcreteNumericResults() => AssertNumeric<double, double>(1.5d);

    private void AssertNumeric<T, TResult>(TResult average)
        where T : struct, INumber<T>
        where TResult : struct, INumber<TResult>
    {
        const string query = """
            select Position,
                Sum(Value) over (order by Position rows between unbounded preceding and current row) as Total,
                Avg(Value) over (order by Position rows between unbounded preceding and current row) as Average,
                Min(Value) over (order by Position rows between unbounded preceding and current row) as Minimum,
                Max(Value) over (order by Position rows between unbounded preceding and current row) as Maximum,
                Count(Value) over (order by Position rows between unbounded preceding and current row) as Values,
                Sum(Value) over () as WholeTotal,
                Sum(Value) over (order by Position rows between 1 preceding and current row) as FrameTotal,
                FirstValue(Value) over (order by Position rows between 1 preceding and current row) as First,
                LastValue(Value) over (order by Position rows between 1 preceding and current row) as Last,
                NthValue(Value, 2) over (order by Position rows between 1 preceding and current row) as Second,
                Lag(Value) over (order by Position) as Previous,
                Lead(Value) over (order by Position) as Next,
                Sum(NonNullValue) over (order by Position) as NonNullTotal,
                Avg(NonNullValue) over (order by Position) as NonNullAverage
            from #schema.first() order by Position
            """;
        NumericRow<T>[] input = [new(1, T.One), new(2, T.CreateChecked(2)), new(3, null)];
        using var compiled = CreateAndRunVirtualMachine(query, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Total", typeof(T?)),
            ("Average", typeof(TResult?)), ("Minimum", typeof(T?)), ("Maximum", typeof(T?)),
            ("Values", typeof(long)), ("WholeTotal", typeof(T?)), ("FrameTotal", typeof(T?)),
            ("First", typeof(T?)), ("Last", typeof(T?)), ("Second", typeof(T?)),
            ("Previous", typeof(T?)), ("Next", typeof(T?)),
            ("NonNullTotal", typeof(T?)), ("NonNullAverage", typeof(TResult?)));
        Assert.HasCount(3, rows);
        AssertNumericRow<T, TResult>(rows[0], 1, T.One, TResult.One, T.One, 1L, T.One);
        AssertNumericRow<T, TResult>(rows[1], 2, T.CreateChecked(3), average, T.CreateChecked(2), 2L, T.CreateChecked(3));
        AssertNumericRow<T, TResult>(rows[2], 3, T.CreateChecked(3), average, T.CreateChecked(2), 2L, T.CreateChecked(2));
        AssertValueAccessRow<T>(rows[0], T.One, T.One, null, null, T.CreateChecked(2));
        AssertValueAccessRow<T>(rows[1], T.One, T.CreateChecked(2), T.CreateChecked(2), T.One, null);
        AssertValueAccessRow<T>(rows[2], T.CreateChecked(2), null, null, T.CreateChecked(2), null);
        Assert.AreEqual(TResult.One, rows[0].GetValue<TResult?>(14));
        Assert.AreEqual(average, rows[1].GetValue<TResult?>(14));
        Assert.AreEqual(TResult.One, rows[2].GetValue<TResult?>(14));
    }

    private static void AssertValueAccessRow<T>(Tables.Row row, T? first, T? last, T? second, T? previous, T? next)
        where T : struct, INumber<T>
    {
        Assert.AreEqual(first, row.GetValue<T?>(8));
        Assert.AreEqual(last, row.GetValue<T?>(9));
        Assert.AreEqual(second, row.GetValue<T?>(10));
        Assert.AreEqual(previous, row.GetValue<T?>(11));
        Assert.AreEqual(next, row.GetValue<T?>(12));
        Assert.AreEqual(row.GetValue<T?>(1), row.GetValue<T?>(13));
    }

    private static void AssertNumericRow<T, TResult>(Tables.Row row, int position, T total, TResult average,
        T maximum, long count, T frameTotal)
        where T : struct, INumber<T>
        where TResult : struct, INumber<TResult>
    {
        Assert.AreEqual(position, row.GetValue<int>(0));
        Assert.AreEqual(total, row.GetValue<T?>(1));
        Assert.AreEqual(average, row.GetValue<TResult?>(2));
        Assert.AreEqual(T.One, row.GetValue<T?>(3));
        Assert.AreEqual(maximum, row.GetValue<T?>(4));
        Assert.AreEqual(count, row.GetValue<long>(5));
        Assert.AreEqual(T.CreateChecked(3), row.GetValue<T?>(6));
        Assert.AreEqual(frameTotal, row.GetValue<T?>(7));
    }

    public sealed record NumericRow<T>(int Position, T? Value) where T : struct
    {
        public T NonNullValue => Value.GetValueOrDefault();
    }
}
