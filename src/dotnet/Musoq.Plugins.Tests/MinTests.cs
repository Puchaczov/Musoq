using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class MinTests
{
    [TestMethod]
    public void MinAggregateKernel_SkipsNullsAndReturnsSmallest()
    {
        AssertMin<byte>(50, 10, 30, 10);
        AssertMin<sbyte>(50, -10, 30, -10);
        AssertMin<short>(500, 100, 300, 100);
        AssertMin<ushort>(500, 100, 300, 100);
        AssertMin(5, 4, -5, -5);
        AssertMin<uint>(500, 100, 300, 100);
        AssertMin<long>(1, 4, -4, -4);
        AssertMin<ulong>(500, 100, 300, 100);
        AssertMin(5.5f, 1.5f, 3.5f, 1.5f);
        AssertMin(5.5, 1.5, 3.5, 1.5);
        AssertMin(1m, 2m, -4m, -4m);
    }

    [TestMethod]
    public void MinAggregateKernel_EmptyStateReturnsNull()
    {
        var state = new MinAggregateKernel<int>.State();

        Assert.IsNull(MinAggregateKernel<int>.Get(in state));
    }

    [TestMethod]
    public void MinAggregateKernel_AllNullInputsReturnNull()
    {
        AssertAllNullMin<int>();
        AssertAllNullMin<decimal>();
    }

    [TestMethod]
    public void MinAggregateKernel_MergeUsesSmallestPartialValue()
    {
        var target = new MinAggregateKernel<int>.State();
        var source = new MinAggregateKernel<int>.State();

        MinAggregateKernel<int>.Set(ref target, 4);
        MinAggregateKernel<int>.Set(ref source, -3);
        MinAggregateKernel<int>.Merge(ref target, in source);

        Assert.AreEqual(-3, MinAggregateKernel<int>.Get(in target));
    }

    [TestMethod]
    public void MinAggregateKernel_MergeIgnoresEmptyPartialState()
    {
        var target = new MinAggregateKernel<decimal>.State();
        var source = new MinAggregateKernel<decimal>.State();

        MinAggregateKernel<decimal>.Set(ref target, 12m);
        MinAggregateKernel<decimal>.Merge(ref target, in source);

        Assert.AreEqual(12m, MinAggregateKernel<decimal>.Get(in target));
    }

    private static void AssertMin<T>(T first, T second, T third, T expected)
        where T : struct, INumber<T>
    {
        var state = new MinAggregateKernel<T>.State();

        MinAggregateKernel<T>.Set(ref state, first);
        MinAggregateKernel<T>.Set(ref state, null);
        MinAggregateKernel<T>.Set(ref state, second);
        MinAggregateKernel<T>.Set(ref state, third);

        Assert.AreEqual(expected, MinAggregateKernel<T>.Get(in state));
    }

    private static void AssertAllNullMin<T>()
        where T : struct, INumber<T>
    {
        var state = new MinAggregateKernel<T>.State();

        MinAggregateKernel<T>.Set(ref state, null);
        MinAggregateKernel<T>.Set(ref state, null);

        Assert.IsNull(MinAggregateKernel<T>.Get(in state));
    }
}
