using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class SumTests
{
    [TestMethod]
    public void SumAggregateKernel_SumsConcreteNumericTypes()
    {
        AssertSum<byte>(10, 20, 30, 60);
        AssertSum<sbyte>(10, -5, 15, 20);
        AssertSum<short>(100, 200, -50, 250);
        AssertSum<ushort>(100, 200, 300, 600);
        AssertSum(1, 4, 6, 11);
        AssertSum(1000u, 2000u, 3000u, 6000u);
        AssertSum(1L, 4L, 6L, 11L);
        AssertSum(10000UL, 20000UL, 30000UL, 60000UL);
        AssertSum(1.5f, 2.5f, 3.0f, 7.0f);
        AssertSum(1.5d, 2.5d, 3.0d, 7.0d);
        AssertSum(1m, 2m, 3m, 6m);
    }

    [TestMethod]
    public void SumAggregateKernel_EmptyStateReturnsNull()
    {
        var state = new SumAggregateKernel<int>.State();

        Assert.IsNull(SumAggregateKernel<int>.Get(in state));
    }

    [TestMethod]
    public void SumAggregateKernel_AllNullInputsReturnNull()
    {
        AssertAllNullSum<int>();
        AssertAllNullSum<decimal>();
    }

    [TestMethod]
    public void SumAggregateKernel_MergeCombinesPartialStates()
    {
        var target = new SumAggregateKernel<int>.State();
        var source = new SumAggregateKernel<int>.State();

        SumAggregateKernel<int>.Set(ref target, 1);
        SumAggregateKernel<int>.Set(ref target, 4);
        SumAggregateKernel<int>.Set(ref source, 6);
        SumAggregateKernel<int>.Merge(ref target, in source);

        Assert.AreEqual(11, SumAggregateKernel<int>.Get(in target));
    }

    [TestMethod]
    public void SumAggregateKernel_MergeIgnoresEmptyPartialState()
    {
        var target = new SumAggregateKernel<decimal>.State();
        var source = new SumAggregateKernel<decimal>.State();

        SumAggregateKernel<decimal>.Set(ref target, 12m);
        SumAggregateKernel<decimal>.Merge(ref target, in source);

        Assert.AreEqual(12m, SumAggregateKernel<decimal>.Get(in target));
    }

    [TestMethod]
    public void SumAggregateKernel_CheckedIntegralOverflowThrows()
    {
        var state = new SumAggregateKernel<int>.State();

        SumAggregateKernel<int>.Set(ref state, int.MaxValue);

        Assert.Throws<OverflowException>(() => SumAggregateKernel<int>.Set(ref state, 1));
    }

    private static void AssertSum<T>(T first, T second, T third, T expected)
        where T : struct, INumber<T>
    {
        var state = new SumAggregateKernel<T>.State();

        SumAggregateKernel<T>.Set(ref state, first);
        SumAggregateKernel<T>.Set(ref state, null);
        SumAggregateKernel<T>.Set(ref state, second);
        SumAggregateKernel<T>.Set(ref state, third);

        Assert.AreEqual(expected, SumAggregateKernel<T>.Get(in state));
    }

    private static void AssertAllNullSum<T>()
        where T : struct, INumber<T>
    {
        var state = new SumAggregateKernel<T>.State();

        SumAggregateKernel<T>.Set(ref state, null);
        SumAggregateKernel<T>.Set(ref state, null);

        Assert.IsNull(SumAggregateKernel<T>.Get(in state));
    }
}
