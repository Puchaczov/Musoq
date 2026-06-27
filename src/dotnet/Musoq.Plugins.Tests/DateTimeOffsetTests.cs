using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class DateTimeOffsetTests
{
    private readonly LibraryBase _library = new();

    [TestMethod]
    public void DateTimeOffsetComparableAggregateKernels_SkipNullsAndReturnMinMax()
    {
        var minState = new MinComparableAggregateKernel<DateTimeOffset>.State();
        var maxState = new MaxComparableAggregateKernel<DateTimeOffset>.State();
        var middle = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var min = new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var max = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);

        MinComparableAggregateKernel<DateTimeOffset>.Set(ref minState, middle);
        MinComparableAggregateKernel<DateTimeOffset>.Set(ref minState, null);
        MinComparableAggregateKernel<DateTimeOffset>.Set(ref minState, min);
        MaxComparableAggregateKernel<DateTimeOffset>.Set(ref maxState, middle);
        MaxComparableAggregateKernel<DateTimeOffset>.Set(ref maxState, null);
        MaxComparableAggregateKernel<DateTimeOffset>.Set(ref maxState, max);

        Assert.AreEqual(min, MinComparableAggregateKernel<DateTimeOffset>.Get(in minState));
        Assert.AreEqual(max, MaxComparableAggregateKernel<DateTimeOffset>.Get(in maxState));
    }

    [TestMethod]
    public void DateTimeComparableAggregateKernels_SkipNullsAndReturnMinMax()
    {
        var minState = new MinComparableAggregateKernel<DateTime>.State();
        var maxState = new MaxComparableAggregateKernel<DateTime>.State();
        var middle = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var min = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var max = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        MinComparableAggregateKernel<DateTime>.Set(ref minState, middle);
        MinComparableAggregateKernel<DateTime>.Set(ref minState, null);
        MinComparableAggregateKernel<DateTime>.Set(ref minState, min);
        MaxComparableAggregateKernel<DateTime>.Set(ref maxState, middle);
        MaxComparableAggregateKernel<DateTime>.Set(ref maxState, null);
        MaxComparableAggregateKernel<DateTime>.Set(ref maxState, max);

        Assert.AreEqual(min, MinComparableAggregateKernel<DateTime>.Get(in minState));
        Assert.AreEqual(max, MaxComparableAggregateKernel<DateTime>.Get(in maxState));
    }

    [TestMethod]
    public void ComparableAggregateKernels_EmptyStatesReturnNull()
    {
        var minDateTimeOffsetState = new MinComparableAggregateKernel<DateTimeOffset>.State();
        var maxDateTimeOffsetState = new MaxComparableAggregateKernel<DateTimeOffset>.State();
        var minDateTimeState = new MinComparableAggregateKernel<DateTime>.State();
        var maxDateTimeState = new MaxComparableAggregateKernel<DateTime>.State();

        Assert.IsNull(MinComparableAggregateKernel<DateTimeOffset>.Get(in minDateTimeOffsetState));
        Assert.IsNull(MaxComparableAggregateKernel<DateTimeOffset>.Get(in maxDateTimeOffsetState));
        Assert.IsNull(MinComparableAggregateKernel<DateTime>.Get(in minDateTimeState));
        Assert.IsNull(MaxComparableAggregateKernel<DateTime>.Get(in maxDateTimeState));
    }

    [TestMethod]
    public void ComparableAggregateKernels_MergeKeepsOuterMinimumAndMaximum()
    {
        var minTarget = new MinComparableAggregateKernel<DateTime>.State();
        var minSource = new MinComparableAggregateKernel<DateTime>.State();
        var maxTarget = new MaxComparableAggregateKernel<DateTime>.State();
        var maxSource = new MaxComparableAggregateKernel<DateTime>.State();
        var middle = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var min = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var max = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        MinComparableAggregateKernel<DateTime>.Set(ref minTarget, middle);
        MinComparableAggregateKernel<DateTime>.Set(ref minSource, min);
        MinComparableAggregateKernel<DateTime>.Merge(ref minTarget, in minSource);
        MaxComparableAggregateKernel<DateTime>.Set(ref maxTarget, middle);
        MaxComparableAggregateKernel<DateTime>.Set(ref maxSource, max);
        MaxComparableAggregateKernel<DateTime>.Merge(ref maxTarget, in maxSource);

        Assert.AreEqual(min, MinComparableAggregateKernel<DateTime>.Get(in minTarget));
        Assert.AreEqual(max, MaxComparableAggregateKernel<DateTime>.Get(in maxTarget));
    }

    [TestMethod]
    public void WhenInvalidDateString_ShouldReturnNull()
    {
        var result = _library.ToDateTimeOffset("invalid date");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void WhenValidDateString_ShouldReturnDateTimeOffset()
    {
        var result = _library.ToDateTimeOffset("2020-01-01T00:00:00+00:00");

        Assert.AreEqual(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), result);
    }

    [TestMethod]
    public void WhenValidDateStringWithCulture_ShouldReturnDateTimeOffset()
    {
        var result = _library.ToDateTimeOffset("01/01/2020 00:00:00", "en-US");

        Assert.AreEqual(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).DateTime, result!.Value.DateTime);
    }

    [TestMethod]
    public void WhenTwoDateTimeOffsetsSubtracted_ShouldReturnTimeSpan()
    {
        var result = _library.SubtractDateTimeOffsets(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.AreEqual(TimeSpan.Zero, result);
    }

    [TestMethod]
    public void WhenOneDateTimeOffsetNull_ShouldReturnNull()
    {
        var result = _library.SubtractDateTimeOffsets(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void WhenBothDateTimeOffsetsNull_ShouldReturnNull()
    {
        var result = _library.SubtractDateTimeOffsets(null, null);

        Assert.IsNull(result);
    }
}
