using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class MaxTests
{
    [TestMethod]
    public void MaxAggregateKernel_SkipsNullsAndReturnsLargest()
    {
        AssertMax<byte>(10, 50, 30, 50);
        AssertMax<sbyte>(-10, 50, -30, 50);
        AssertMax<short>(100, 500, 300, 500);
        AssertMax<ushort>(100, 500, 300, 500);
        AssertMax(5, 4, 6, 6);
        AssertMax<uint>(100, 500, 300, 500);
        AssertMax<long>(1, 4, 6, 6);
        AssertMax<ulong>(100, 500, 300, 500);
        AssertMax(1.5f, 5.5f, 3.5f, 5.5f);
        AssertMax(1.5, 5.5, 3.5, 5.5);
        AssertMax(1m, 2m, 3m, 3m);
    }

    [TestMethod]
    public void MaxAggregateKernel_EmptyStateReturnsNull()
    {
        var state = new MaxAggregateKernel<int>.State();

        Assert.IsNull(MaxAggregateKernel<int>.Get(in state));
    }

    [TestMethod]
    public void MaxAggregateKernel_AllNullInputsReturnNull()
    {
        AssertAllNullMax<int>();
        AssertAllNullMax<decimal>();
    }

    [TestMethod]
    public void MaxAggregateKernel_MergeUsesLargestPartialValue()
    {
        var target = new MaxAggregateKernel<int>.State();
        var source = new MaxAggregateKernel<int>.State();

        MaxAggregateKernel<int>.Set(ref target, 4);
        MaxAggregateKernel<int>.Set(ref source, 9);
        MaxAggregateKernel<int>.Merge(ref target, in source);

        Assert.AreEqual(9, MaxAggregateKernel<int>.Get(in target));
    }

    [TestMethod]
    public void MaxAggregateKernel_MergeIgnoresEmptyPartialState()
    {
        var target = new MaxAggregateKernel<decimal>.State();
        var source = new MaxAggregateKernel<decimal>.State();

        MaxAggregateKernel<decimal>.Set(ref target, 12m);
        MaxAggregateKernel<decimal>.Merge(ref target, in source);

        Assert.AreEqual(12m, MaxAggregateKernel<decimal>.Get(in target));
    }

    private static void AssertMax<T>(T first, T second, T third, T expected)
        where T : struct, INumber<T>
    {
        var state = new MaxAggregateKernel<T>.State();

        MaxAggregateKernel<T>.Set(ref state, first);
        MaxAggregateKernel<T>.Set(ref state, null);
        MaxAggregateKernel<T>.Set(ref state, second);
        MaxAggregateKernel<T>.Set(ref state, third);

        Assert.AreEqual(expected, MaxAggregateKernel<T>.Get(in state));
    }

    private static void AssertAllNullMax<T>()
        where T : struct, INumber<T>
    {
        var state = new MaxAggregateKernel<T>.State();

        MaxAggregateKernel<T>.Set(ref state, null);
        MaxAggregateKernel<T>.Set(ref state, null);

        Assert.IsNull(MaxAggregateKernel<T>.Get(in state));
    }
}
