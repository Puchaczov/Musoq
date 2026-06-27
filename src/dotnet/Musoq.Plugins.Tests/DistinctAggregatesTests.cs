using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class DistinctAggregatesTests
{
    [TestMethod]
    public void CountDistinctNullableAggregateKernel_CountsUniqueNonNullValues()
    {
        var state = new CountDistinctNullableAggregateKernel<int>.State();

        CountDistinctNullableAggregateKernel<int>.Set(ref state, 1);
        CountDistinctNullableAggregateKernel<int>.Set(ref state, 1);
        CountDistinctNullableAggregateKernel<int>.Set(ref state, null);
        CountDistinctNullableAggregateKernel<int>.Set(ref state, 2);
        CountDistinctNullableAggregateKernel<int>.Set(ref state, 3);
        CountDistinctNullableAggregateKernel<int>.Set(ref state, 2);

        Assert.AreEqual(3L, CountDistinctNullableAggregateKernel<int>.Get(in state));
    }

    [TestMethod]
    public void CountDistinctReferenceAggregateKernel_CountsUniqueNonNullValues()
    {
        var state = new CountDistinctReferenceAggregateKernel<string>.State();

        CountDistinctReferenceAggregateKernel<string>.Set(ref state, "a");
        CountDistinctReferenceAggregateKernel<string>.Set(ref state, "a");
        CountDistinctReferenceAggregateKernel<string>.Set(ref state, null);
        CountDistinctReferenceAggregateKernel<string>.Set(ref state, "b");
        CountDistinctReferenceAggregateKernel<string>.Set(ref state, "c");

        Assert.AreEqual(3L, CountDistinctReferenceAggregateKernel<string>.Get(in state));
    }

    [TestMethod]
    public void CountDistinctAggregateKernels_EmptyStatesReturnZero()
    {
        var nullableState = new CountDistinctNullableAggregateKernel<int>.State();
        var referenceState = new CountDistinctReferenceAggregateKernel<string>.State();

        Assert.AreEqual(0L, CountDistinctNullableAggregateKernel<int>.Get(in nullableState));
        Assert.AreEqual(0L, CountDistinctReferenceAggregateKernel<string>.Get(in referenceState));
    }

    [TestMethod]
    public void CountDistinctAggregateKernels_MergeCombinesUniqueValues()
    {
        var target = new CountDistinctNullableAggregateKernel<int>.State();
        var source = new CountDistinctNullableAggregateKernel<int>.State();

        CountDistinctNullableAggregateKernel<int>.Set(ref target, 1);
        CountDistinctNullableAggregateKernel<int>.Set(ref target, 2);
        CountDistinctNullableAggregateKernel<int>.Set(ref source, 2);
        CountDistinctNullableAggregateKernel<int>.Set(ref source, 3);
        CountDistinctNullableAggregateKernel<int>.Merge(ref target, in source);

        Assert.AreEqual(3L, CountDistinctNullableAggregateKernel<int>.Get(in target));
    }

    [TestMethod]
    public void SumDistinctAggregateKernel_SumsUniqueNonNullValues()
    {
        AssertSumDistinct(1, 1, 2, 3, 6);
        AssertSumDistinct(100L, 100L, 200L, 300L, 600L);
        AssertSumDistinct(1.5m, 1.5m, 2.5m, 3.5m, 7.5m);
    }

    [TestMethod]
    public void SumDistinctAggregateKernel_EmptyStateReturnsNull()
    {
        var state = new SumDistinctAggregateKernel<int>.State();

        Assert.IsNull(SumDistinctAggregateKernel<int>.Get(in state));
    }

    [TestMethod]
    public void SumDistinctAggregateKernel_MergeCombinesUniqueValues()
    {
        var target = new SumDistinctAggregateKernel<int>.State();
        var source = new SumDistinctAggregateKernel<int>.State();

        SumDistinctAggregateKernel<int>.Set(ref target, 10);
        SumDistinctAggregateKernel<int>.Set(ref target, 10);
        SumDistinctAggregateKernel<int>.Set(ref source, 10);
        SumDistinctAggregateKernel<int>.Set(ref source, 5);
        SumDistinctAggregateKernel<int>.Merge(ref target, in source);

        Assert.AreEqual(15, SumDistinctAggregateKernel<int>.Get(in target));
    }

    [TestMethod]
    public void AvgDistinctAggregateKernel_AveragesUniqueNonNullValues()
    {
        AssertAvgDistinct(10, 10, 20, 30, 20);
        AssertAvgDistinct(1m, 1m, 2m, 3m, 2m);
    }

    [TestMethod]
    public void AvgDistinctAggregateKernel_EmptyStateReturnsNull()
    {
        var state = new AvgDistinctAggregateKernel<int>.State();

        Assert.IsNull(AvgDistinctAggregateKernel<int>.Get(in state));
    }

    [TestMethod]
    public void MinMaxDistinctAggregateKernels_SkipNullsAndReturnExtremes()
    {
        AssertMinMaxDistinct(5, 5, 10, 3, 3, 10);
        AssertMinMaxDistinct(5.5m, 5.5m, 2.2m, 7.7m, 2.2m, 7.7m);
    }

    [TestMethod]
    public void MinMaxDistinctAggregateKernels_EmptyStatesReturnNull()
    {
        var minState = new MinDistinctAggregateKernel<int>.State();
        var maxState = new MaxDistinctAggregateKernel<int>.State();

        Assert.IsNull(MinDistinctAggregateKernel<int>.Get(in minState));
        Assert.IsNull(MaxDistinctAggregateKernel<int>.Get(in maxState));
    }

    [TestMethod]
    public void MinMaxDistinctAggregateKernels_MergeCombinesExtremes()
    {
        var minTarget = new MinDistinctAggregateKernel<int>.State();
        var minSource = new MinDistinctAggregateKernel<int>.State();
        var maxTarget = new MaxDistinctAggregateKernel<int>.State();
        var maxSource = new MaxDistinctAggregateKernel<int>.State();

        MinDistinctAggregateKernel<int>.Set(ref minTarget, 5);
        MinDistinctAggregateKernel<int>.Set(ref minSource, -2);
        MinDistinctAggregateKernel<int>.Merge(ref minTarget, in minSource);
        MaxDistinctAggregateKernel<int>.Set(ref maxTarget, 5);
        MaxDistinctAggregateKernel<int>.Set(ref maxSource, 12);
        MaxDistinctAggregateKernel<int>.Merge(ref maxTarget, in maxSource);

        Assert.AreEqual(-2, MinDistinctAggregateKernel<int>.Get(in minTarget));
        Assert.AreEqual(12, MaxDistinctAggregateKernel<int>.Get(in maxTarget));
    }

    private static void AssertSumDistinct<T>(T first, T duplicate, T second, T third, T expected)
        where T : struct, INumber<T>
    {
        var state = new SumDistinctAggregateKernel<T>.State();

        SumDistinctAggregateKernel<T>.Set(ref state, first);
        SumDistinctAggregateKernel<T>.Set(ref state, duplicate);
        SumDistinctAggregateKernel<T>.Set(ref state, null);
        SumDistinctAggregateKernel<T>.Set(ref state, second);
        SumDistinctAggregateKernel<T>.Set(ref state, third);

        Assert.AreEqual(expected, SumDistinctAggregateKernel<T>.Get(in state));
    }

    private static void AssertAvgDistinct<T>(T first, T duplicate, T second, T third, T expected)
        where T : struct, INumber<T>
    {
        var state = new AvgDistinctAggregateKernel<T>.State();

        AvgDistinctAggregateKernel<T>.Set(ref state, first);
        AvgDistinctAggregateKernel<T>.Set(ref state, duplicate);
        AvgDistinctAggregateKernel<T>.Set(ref state, null);
        AvgDistinctAggregateKernel<T>.Set(ref state, second);
        AvgDistinctAggregateKernel<T>.Set(ref state, third);

        Assert.AreEqual(expected, AvgDistinctAggregateKernel<T>.Get(in state));
    }

    private static void AssertMinMaxDistinct<T>(
        T first,
        T duplicate,
        T second,
        T third,
        T expectedMin,
        T expectedMax)
        where T : struct, INumber<T>
    {
        var minState = new MinDistinctAggregateKernel<T>.State();
        var maxState = new MaxDistinctAggregateKernel<T>.State();

        MinDistinctAggregateKernel<T>.Set(ref minState, first);
        MinDistinctAggregateKernel<T>.Set(ref minState, duplicate);
        MinDistinctAggregateKernel<T>.Set(ref minState, null);
        MinDistinctAggregateKernel<T>.Set(ref minState, second);
        MinDistinctAggregateKernel<T>.Set(ref minState, third);
        MaxDistinctAggregateKernel<T>.Set(ref maxState, first);
        MaxDistinctAggregateKernel<T>.Set(ref maxState, duplicate);
        MaxDistinctAggregateKernel<T>.Set(ref maxState, null);
        MaxDistinctAggregateKernel<T>.Set(ref maxState, second);
        MaxDistinctAggregateKernel<T>.Set(ref maxState, third);

        Assert.AreEqual(expectedMin, MinDistinctAggregateKernel<T>.Get(in minState));
        Assert.AreEqual(expectedMax, MaxDistinctAggregateKernel<T>.Get(in maxState));
    }
}
