using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class CountTests
{
    [TestMethod]
    public void CountAllKernel_WhenRowsAreAccumulated_ShouldCountEveryRow()
    {
        var state = new CountAllAggregateKernel.State();

        CountAllAggregateKernel.Set(ref state);
        CountAllAggregateKernel.Set(ref state);
        CountAllAggregateKernel.Set(ref state);

        Assert.AreEqual(3L, CountAllAggregateKernel.Get(in state));
    }

    [TestMethod]
    public void CountAllKernel_WhenMerged_ShouldReturnCombinedCount()
    {
        var first = new CountAllAggregateKernel.State();
        var second = new CountAllAggregateKernel.State();

        CountAllAggregateKernel.Set(ref first);
        CountAllAggregateKernel.Set(ref second);
        CountAllAggregateKernel.Set(ref second);
        CountAllAggregateKernel.Merge(ref first, in second);

        Assert.AreEqual(3L, CountAllAggregateKernel.Get(in first));
    }

    [TestMethod]
    public void CountNullableKernel_WhenValuesContainNulls_ShouldCountOnlyPresentValues()
    {
        var state = new CountNullableAggregateKernel<int>.State();

        CountNullableAggregateKernel<int>.Set(ref state, 1);
        CountNullableAggregateKernel<int>.Set(ref state, null);
        CountNullableAggregateKernel<int>.Set(ref state, 6);

        Assert.AreEqual(2L, CountNullableAggregateKernel<int>.Get(in state));
    }

    [TestMethod]
    public void CountNullableKernel_WhenMerged_ShouldReturnCombinedCount()
    {
        var first = new CountNullableAggregateKernel<decimal>.State();
        var second = new CountNullableAggregateKernel<decimal>.State();

        CountNullableAggregateKernel<decimal>.Set(ref first, 1m);
        CountNullableAggregateKernel<decimal>.Set(ref first, null);
        CountNullableAggregateKernel<decimal>.Set(ref second, 2m);
        CountNullableAggregateKernel<decimal>.Set(ref second, 3m);
        CountNullableAggregateKernel<decimal>.Merge(ref first, in second);

        Assert.AreEqual(3L, CountNullableAggregateKernel<decimal>.Get(in first));
    }

    [TestMethod]
    public void CountNullableKernel_WhenTemporalValuesContainNulls_ShouldCountOnlyPresentValues()
    {
        var offsetState = new CountNullableAggregateKernel<DateTimeOffset>.State();
        var dateState = new CountNullableAggregateKernel<DateTime>.State();

        CountNullableAggregateKernel<DateTimeOffset>.Set(ref offsetState, DateTimeOffset.Parse("01/01/2010"));
        CountNullableAggregateKernel<DateTimeOffset>.Set(ref offsetState, null);
        CountNullableAggregateKernel<DateTime>.Set(ref dateState, DateTime.Parse("01/01/2010"));
        CountNullableAggregateKernel<DateTime>.Set(ref dateState, null);

        Assert.AreEqual(1L, CountNullableAggregateKernel<DateTimeOffset>.Get(in offsetState));
        Assert.AreEqual(1L, CountNullableAggregateKernel<DateTime>.Get(in dateState));
    }

    [TestMethod]
    public void CountNullableKernel_WhenBooleanValuesContainNulls_ShouldCountOnlyPresentValues()
    {
        var state = new CountNullableAggregateKernel<bool>.State();

        CountNullableAggregateKernel<bool>.Set(ref state, true);
        CountNullableAggregateKernel<bool>.Set(ref state, false);
        CountNullableAggregateKernel<bool>.Set(ref state, null);

        Assert.AreEqual(2L, CountNullableAggregateKernel<bool>.Get(in state));
    }

    [TestMethod]
    public void CountReferenceKernel_WhenValuesContainNulls_ShouldCountOnlyNonNullValues()
    {
        var state = new CountReferenceAggregateKernel<string>.State();

        CountReferenceAggregateKernel<string>.Set(ref state, "first");
        CountReferenceAggregateKernel<string>.Set(ref state, null);
        CountReferenceAggregateKernel<string>.Set(ref state, "second");

        Assert.AreEqual(2L, CountReferenceAggregateKernel<string>.Get(in state));
    }

    [TestMethod]
    public void CountReferenceKernel_WhenMerged_ShouldReturnCombinedCount()
    {
        var first = new CountReferenceAggregateKernel<string>.State();
        var second = new CountReferenceAggregateKernel<string>.State();

        CountReferenceAggregateKernel<string>.Set(ref first, "first");
        CountReferenceAggregateKernel<string>.Set(ref second, "second");
        CountReferenceAggregateKernel<string>.Set(ref second, null);
        CountReferenceAggregateKernel<string>.Merge(ref first, in second);

        Assert.AreEqual(2L, CountReferenceAggregateKernel<string>.Get(in first));
    }
}
