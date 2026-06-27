using System.Linq;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [AggregateFunction(
        typeof(SumTimeSpanAggregateKernel),
        Name = nameof(SumTimeSpan),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public TimeSpan? SumTimeSpan(TimeSpan? timeSpan, [AggregateParent] int parent = 0)
        => AggregateDeclaration<TimeSpan?>();
    [AggregateFunction(
        typeof(MinComparableAggregateKernel<TimeSpan>),
        Name = nameof(MinTimeSpan),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public TimeSpan? MinTimeSpan(TimeSpan? timeSpan, [AggregateParent] int parent = 0)
        => AggregateDeclaration<TimeSpan?>();
    [AggregateFunction(
        typeof(MaxComparableAggregateKernel<TimeSpan>),
        Name = nameof(MaxTimeSpan),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public TimeSpan? MaxTimeSpan(TimeSpan? timeSpan, [AggregateParent] int parent = 0)
        => AggregateDeclaration<TimeSpan?>();
    [BindableMethod]
    [MethodCategory(MethodCategories.TimeSpan)]
    public TimeSpan? AddTimeSpans(params TimeSpan?[] timeSpans)
    {
        ArgumentNullException.ThrowIfNull(timeSpans);
        var firstNonNull = timeSpans.Select((value, index) => new { TimeSpan = value, Index = index })
            .FirstOrDefault(pair => pair.TimeSpan.HasValue);

        if (firstNonNull == null)
            return null;

        var sum = firstNonNull.TimeSpan!.Value;

        for (var i = firstNonNull.Index + 1; i < timeSpans.Length; i++)
            if (timeSpans[i].HasValue)
                sum += timeSpans[i]!.Value;

        return sum;
    }
    [BindableMethod]
    [MethodCategory(MethodCategories.TimeSpan)]
    public TimeSpan? SubtractTimeSpans(params TimeSpan?[] timeSpans)
    {
        ArgumentNullException.ThrowIfNull(timeSpans);
        var firstNonNull = timeSpans.Select((value, index) => new { TimeSpan = value, Index = index })
            .FirstOrDefault(pair => pair.TimeSpan.HasValue);

        if (firstNonNull == null)
            return null;

        var sum = firstNonNull.TimeSpan!.Value;

        for (var i = firstNonNull.Index + 1; i < timeSpans.Length; i++)
            if (timeSpans[i].HasValue)
                sum -= timeSpans[i]!.Value;

        return sum;
    }
    [BindableMethod]
    [MethodCategory(MethodCategories.TimeSpan)]
    public TimeSpan? FromString(string timeSpan)
    {
        if (TimeSpan.TryParse(timeSpan, out var result))
            return result;

        return null;
    }

}
