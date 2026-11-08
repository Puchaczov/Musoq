using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionWindowQueryTestsFixtures
{

    internal static void AssertValueAccessRow<T>(Tables.Row row, T? first, T? last, T? second, T? previous, T? next)
        where T : struct, INumber<T>
    {
        Assert.AreEqual(first, row.GetValue<T?>(8));
        Assert.AreEqual(last, row.GetValue<T?>(9));
        Assert.AreEqual(second, row.GetValue<T?>(10));
        Assert.AreEqual(previous, row.GetValue<T?>(11));
        Assert.AreEqual(next, row.GetValue<T?>(12));
        Assert.AreEqual(row.GetValue<T?>(1), row.GetValue<T?>(13));
    }

    internal static void AssertNumericRow<T, TResult>(Tables.Row row, int position, T total, TResult average,
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
