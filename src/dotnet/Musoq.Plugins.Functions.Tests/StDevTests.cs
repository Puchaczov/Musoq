using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class StDevTests
{
    [TestMethod]
    public void StDevAggregateKernel_ReturnsPopulationStandardDeviation()
    {
        var state = new StDevAggregateKernel<decimal>.State();

        StDevAggregateKernel<decimal>.Set(ref state, 5m);
        StDevAggregateKernel<decimal>.Set(ref state, 6m);
        StDevAggregateKernel<decimal>.Set(ref state, 8m);
        StDevAggregateKernel<decimal>.Set(ref state, 9m);

        AssertClose(1.5811388300841898m, StDevAggregateKernel<decimal>.Get(in state));
    }

    [TestMethod]
    public void StDevAggregateKernel_SkipsNullInputs()
    {
        var state = new StDevAggregateKernel<int>.State();

        StDevAggregateKernel<int>.Set(ref state, 60000);
        StDevAggregateKernel<int>.Set(ref state, null);
        StDevAggregateKernel<int>.Set(ref state, 80000);

        AssertClose(10000m, StDevAggregateKernel<int>.Get(in state));
    }

    [TestMethod]
    public void StDevAggregateKernel_EmptyStateReturnsNull()
    {
        var state = new StDevAggregateKernel<int>.State();

        Assert.IsNull(StDevAggregateKernel<int>.Get(in state));
    }

    [TestMethod]
    public void StDevAggregateKernel_MergeCombinesPartialStates()
    {
        var target = new StDevAggregateKernel<int>.State();
        var source = new StDevAggregateKernel<int>.State();

        StDevAggregateKernel<int>.Set(ref target, 4);
        StDevAggregateKernel<int>.Set(ref target, 9);
        StDevAggregateKernel<int>.Set(ref source, 11);
        StDevAggregateKernel<int>.Set(ref source, 12);
        StDevAggregateKernel<int>.Set(ref source, 17);
        StDevAggregateKernel<int>.Set(ref source, 5);
        StDevAggregateKernel<int>.Set(ref source, 8);
        StDevAggregateKernel<int>.Set(ref source, 12);
        StDevAggregateKernel<int>.Set(ref source, 14);
        StDevAggregateKernel<int>.Merge(ref target, in source);

        AssertClose(3.9377878103709665m, StDevAggregateKernel<int>.Get(in target));
    }

    [TestMethod]
    public void StDevAggregateKernel_SupportsConcreteNumericInputTypes()
    {
        AssertStDev<byte>(1, 2, 0.5m);
        AssertStDev<sbyte>(-1, 1, 1m);
        AssertStDev<short>(1, 3, 1m);
        AssertStDev<ushort>(1, 3, 1m);
        AssertStDev<long>(1, 5, 2m);
        AssertStDev<ulong>(1, 5, 2m);
        AssertStDev(1.5f, 4.5f, 1.5m);
        AssertStDev(1.5, 4.5, 1.5m);
    }

    private static void AssertStDev<T>(T first, T second, decimal expected)
        where T : struct, INumber<T>
    {
        var state = new StDevAggregateKernel<T>.State();

        StDevAggregateKernel<T>.Set(ref state, first);
        StDevAggregateKernel<T>.Set(ref state, second);

        AssertClose(expected, StDevAggregateKernel<T>.Get(in state));
    }

    private static void AssertClose(decimal expected, decimal? actual)
    {
        Assert.IsNotNull(actual);
        Assert.IsLessThan(0.000000000001m, Math.Abs(actual.Value - expected));
    }
}
