using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class TimeSpanTests
{
    private readonly LibraryBase _library = new();

    [TestMethod]
    public void SumTimeSpanAggregateKernel_SumsNonNullValues()
    {
        var state = new SumTimeSpanAggregateKernel.State();

        SumTimeSpanAggregateKernel.Set(ref state, TimeSpan.FromHours(1));
        SumTimeSpanAggregateKernel.Set(ref state, null);
        SumTimeSpanAggregateKernel.Set(ref state, TimeSpan.FromHours(2));
        SumTimeSpanAggregateKernel.Set(ref state, TimeSpan.FromHours(3));

        Assert.AreEqual(TimeSpan.FromHours(6), SumTimeSpanAggregateKernel.Get(in state));
    }

    [TestMethod]
    public void SumTimeSpanAggregateKernel_EmptyStateReturnsNull()
    {
        var state = new SumTimeSpanAggregateKernel.State();

        Assert.IsNull(SumTimeSpanAggregateKernel.Get(in state));
    }

    [TestMethod]
    public void SumTimeSpanAggregateKernel_MergeCombinesPartialStates()
    {
        var target = new SumTimeSpanAggregateKernel.State();
        var source = new SumTimeSpanAggregateKernel.State();

        SumTimeSpanAggregateKernel.Set(ref target, TimeSpan.FromHours(1));
        SumTimeSpanAggregateKernel.Set(ref source, TimeSpan.FromHours(2));
        SumTimeSpanAggregateKernel.Merge(ref target, in source);

        Assert.AreEqual(TimeSpan.FromHours(3), SumTimeSpanAggregateKernel.Get(in target));
    }

    [TestMethod]
    public void MinTimeSpanAggregateKernel_SkipsNullsAndReturnsMinimum()
    {
        var state = new MinComparableAggregateKernel<TimeSpan>.State();

        MinComparableAggregateKernel<TimeSpan>.Set(ref state, TimeSpan.FromHours(3));
        MinComparableAggregateKernel<TimeSpan>.Set(ref state, null);
        MinComparableAggregateKernel<TimeSpan>.Set(ref state, TimeSpan.FromHours(1));
        MinComparableAggregateKernel<TimeSpan>.Set(ref state, TimeSpan.FromHours(2));

        Assert.AreEqual(TimeSpan.FromHours(1), MinComparableAggregateKernel<TimeSpan>.Get(in state));
    }

    [TestMethod]
    public void MaxTimeSpanAggregateKernel_SkipsNullsAndReturnsMaximum()
    {
        var state = new MaxComparableAggregateKernel<TimeSpan>.State();

        MaxComparableAggregateKernel<TimeSpan>.Set(ref state, TimeSpan.FromHours(1));
        MaxComparableAggregateKernel<TimeSpan>.Set(ref state, null);
        MaxComparableAggregateKernel<TimeSpan>.Set(ref state, TimeSpan.FromHours(3));
        MaxComparableAggregateKernel<TimeSpan>.Set(ref state, TimeSpan.FromHours(2));

        Assert.AreEqual(TimeSpan.FromHours(3), MaxComparableAggregateKernel<TimeSpan>.Get(in state));
    }

    [TestMethod]
    public void TimeSpanComparableAggregateKernels_EmptyStatesReturnNull()
    {
        var minState = new MinComparableAggregateKernel<TimeSpan>.State();
        var maxState = new MaxComparableAggregateKernel<TimeSpan>.State();

        Assert.IsNull(MinComparableAggregateKernel<TimeSpan>.Get(in minState));
        Assert.IsNull(MaxComparableAggregateKernel<TimeSpan>.Get(in maxState));
    }

    [TestMethod]
    public void AddTimeSpansTest()
    {
        var timeSpan = _library.AddTimeSpans(TimeSpan.Zero, TimeSpan.FromHours(1));

        Assert.AreEqual(TimeSpan.FromHours(1), timeSpan);
    }

    [TestMethod]
    public void WhenFirstTimeSpanIsNull_Add_ShouldReturnRightOne()
    {
        var timeSpan = _library.AddTimeSpans(null, TimeSpan.FromMinutes(30));

        Assert.AreEqual(TimeSpan.FromMinutes(30), timeSpan);
    }

    [TestMethod]
    public void WhenSecondTimeSpanIsNull_Add_ShouldReturnLeftOne()
    {
        var timeSpan = _library.AddTimeSpans(TimeSpan.FromMinutes(30), null);

        Assert.AreEqual(TimeSpan.FromMinutes(30), timeSpan);
    }

    [TestMethod]
    public void SubtractTimeSpansTest()
    {
        var timeSpan = _library.SubtractTimeSpans(TimeSpan.FromHours(1), TimeSpan.FromMinutes(30));

        Assert.AreEqual(TimeSpan.FromMinutes(30), timeSpan);
    }

    [TestMethod]
    public void WhenFirstTimeSpanIsNull_Subtract_ShouldReturnRightOne()
    {
        var timeSpan = _library.SubtractTimeSpans(null, TimeSpan.FromMinutes(30));

        Assert.AreEqual(TimeSpan.FromMinutes(30), timeSpan);
    }

    [TestMethod]
    public void WhenSecondTimeSpanIsNull_Subtract_ShouldReturnLeftOne()
    {
        var timeSpan = _library.SubtractTimeSpans(TimeSpan.FromMinutes(30), null);

        Assert.AreEqual(TimeSpan.FromMinutes(30), timeSpan);
    }
}
