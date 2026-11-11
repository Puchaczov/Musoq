using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class SumOutcomeTests
{
    [TestMethod]
    public void SumOutcomeAggregateKernel_SumsOnlyNegativeValues()
    {
        AssertOutcome(-1, -4, 6, 0, -5);
        AssertOutcome<long>(-1, -4, 6, 0, -5);
        AssertOutcome(-1.5f, -4.5f, 6.5f, 0f, -6f);
        AssertOutcome(-1.5, -4.5, 6.5, 0, -6);
        AssertOutcome(-1m, -2m, 4m, 0m, -3m);
    }

    [TestMethod]
    public void SumOutcomeAggregateKernel_EmptyStateReturnsNull()
    {
        var state = new SumOutcomeAggregateKernel<int>.State();

        Assert.IsNull(SumOutcomeAggregateKernel<int>.Get(in state));
    }

    [TestMethod]
    public void SumOutcomeAggregateKernel_AllNonNegativeOrNullInputsReturnNull()
    {
        var state = new SumOutcomeAggregateKernel<int>.State();

        SumOutcomeAggregateKernel<int>.Set(ref state, 1);
        SumOutcomeAggregateKernel<int>.Set(ref state, null);
        SumOutcomeAggregateKernel<int>.Set(ref state, 0);

        Assert.IsNull(SumOutcomeAggregateKernel<int>.Get(in state));
    }

    [TestMethod]
    public void SumOutcomeAggregateKernel_UnsignedInputsCannotQualify()
    {
        var state = new SumOutcomeAggregateKernel<uint>.State();

        SumOutcomeAggregateKernel<uint>.Set(ref state, 1u);
        SumOutcomeAggregateKernel<uint>.Set(ref state, 0u);

        Assert.IsNull(SumOutcomeAggregateKernel<uint>.Get(in state));
    }

    [TestMethod]
    public void SumOutcomeAggregateKernel_MergeCombinesOnlyQualifiedPartialStates()
    {
        var target = new SumOutcomeAggregateKernel<decimal>.State();
        var source = new SumOutcomeAggregateKernel<decimal>.State();

        SumOutcomeAggregateKernel<decimal>.Set(ref target, -2m);
        SumOutcomeAggregateKernel<decimal>.Set(ref target, 10m);
        SumOutcomeAggregateKernel<decimal>.Set(ref source, -3m);
        SumOutcomeAggregateKernel<decimal>.Merge(ref target, in source);

        Assert.AreEqual(-5m, SumOutcomeAggregateKernel<decimal>.Get(in target));
    }

    private static void AssertOutcome<T>(T first, T second, T third, T fourth, T expected)
        where T : struct, INumber<T>
    {
        var state = new SumOutcomeAggregateKernel<T>.State();

        SumOutcomeAggregateKernel<T>.Set(ref state, first);
        SumOutcomeAggregateKernel<T>.Set(ref state, null);
        SumOutcomeAggregateKernel<T>.Set(ref state, second);
        SumOutcomeAggregateKernel<T>.Set(ref state, third);
        SumOutcomeAggregateKernel<T>.Set(ref state, fourth);

        Assert.AreEqual(expected, SumOutcomeAggregateKernel<T>.Get(in state));
    }
}
