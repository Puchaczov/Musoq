using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class AvgTests
{
    [TestMethod]
    public void AvgAggregateKernel_AveragesConcreteNumericTypes()
    {
        AssertAvg<byte>(10, 20, 30, 20);
        AssertAvg<sbyte>(10, -10, 20, 6);
        AssertAvg<short>(100, 200, 300, 200);
        AssertAvg<ushort>(100, 200, 300, 200);
        AssertAvg(5, 4, -5, 1);
        AssertAvg(1000u, 2000u, 3000u, 2000u);
        AssertAvg(1L, 4L, -4L, 0L);
        AssertAvg(10000UL, 20000UL, 30000UL, 20000UL);
        AssertAvg(1.5f, 2.5f, 5.0f, 3.0f);
        AssertAvg(1.5d, 2.5d, 5.0d, 3.0d);
        AssertAvg(1m, 2m, 3m, 2m);
    }

    [TestMethod]
    public void AvgAggregateKernel_EmptyStateReturnsNull()
    {
        var state = new AvgAggregateKernel<int>.State();

        Assert.IsNull(AvgAggregateKernel<int>.Get(in state));
    }

    [TestMethod]
    public void AvgAggregateKernel_AllNullInputsReturnNull()
    {
        AssertAllNullAvg<int>();
        AssertAllNullAvg<decimal>();
    }

    [TestMethod]
    public void AvgAggregateKernel_MergeCombinesPartialStates()
    {
        var target = new AvgAggregateKernel<int>.State();
        var source = new AvgAggregateKernel<int>.State();

        AvgAggregateKernel<int>.Set(ref target, 10);
        AvgAggregateKernel<int>.Set(ref target, 20);
        AvgAggregateKernel<int>.Set(ref source, 30);
        AvgAggregateKernel<int>.Set(ref source, 40);
        AvgAggregateKernel<int>.Merge(ref target, in source);

        Assert.AreEqual(25, AvgAggregateKernel<int>.Get(in target));
    }

    [TestMethod]
    public void AvgAggregateKernel_MergeIgnoresEmptyPartialState()
    {
        var target = new AvgAggregateKernel<decimal>.State();
        var source = new AvgAggregateKernel<decimal>.State();

        AvgAggregateKernel<decimal>.Set(ref target, 12m);
        AvgAggregateKernel<decimal>.Merge(ref target, in source);

        Assert.AreEqual(12m, AvgAggregateKernel<decimal>.Get(in target));
    }

    [TestMethod]
    public void AvgAggregateKernel_CheckedIntegralOverflowThrows()
    {
        var state = new AvgAggregateKernel<int>.State();

        AvgAggregateKernel<int>.Set(ref state, int.MaxValue);

        Assert.Throws<OverflowException>(() => AvgAggregateKernel<int>.Set(ref state, 1));
    }

    private static void AssertAvg<T>(T first, T second, T third, T expected)
        where T : struct, INumber<T>
    {
        var state = new AvgAggregateKernel<T>.State();

        AvgAggregateKernel<T>.Set(ref state, first);
        AvgAggregateKernel<T>.Set(ref state, null);
        AvgAggregateKernel<T>.Set(ref state, second);
        AvgAggregateKernel<T>.Set(ref state, third);

        Assert.AreEqual(expected, AvgAggregateKernel<T>.Get(in state));
    }

    private static void AssertAllNullAvg<T>()
        where T : struct, INumber<T>
    {
        var state = new AvgAggregateKernel<T>.State();

        AvgAggregateKernel<T>.Set(ref state, null);
        AvgAggregateKernel<T>.Set(ref state, null);

        Assert.IsNull(AvgAggregateKernel<T>.Get(in state));
    }
}
