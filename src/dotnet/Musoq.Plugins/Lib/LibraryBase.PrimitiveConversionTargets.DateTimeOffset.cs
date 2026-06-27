using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [AggregateFunction(
        typeof(MaxComparableAggregateKernel<DateTimeOffset>),
        Name = nameof(MaxDateTimeOffset),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public DateTimeOffset? MaxDateTimeOffset(DateTimeOffset? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<DateTimeOffset?>();
    [AggregateFunction(
        typeof(MinComparableAggregateKernel<DateTimeOffset>),
        Name = nameof(MinDateTimeOffset),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public DateTimeOffset? MinDateTimeOffset(DateTimeOffset? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<DateTimeOffset?>();
    [AggregateFunction(
        typeof(MaxComparableAggregateKernel<DateTime>),
        Name = nameof(MaxDateTime),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public DateTime? MaxDateTime(DateTime? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<DateTime?>();
    [AggregateFunction(
        typeof(MinComparableAggregateKernel<DateTime>),
        Name = nameof(MinDateTime),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public DateTime? MinDateTime(DateTime? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<DateTime?>();
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public DateTimeOffset? ToDateTimeOffset(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
            ? result
            : null;
    }
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public DateTimeOffset? ToDateTimeOffset(string? value, string culture)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (!DateTimeOffset.TryParse(value, CultureInfo.GetCultureInfo(culture), DateTimeStyles.None, out var result))
            return null;

        return result;
    }
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public DateTimeOffset? ToDateTimeOffset(DateTimeOffset? value)
    {
        return value;
    }
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public DateTimeOffset? ToDateTimeOffset(DateTime? value)
    {
        return value.HasValue ? new DateTimeOffset(value.Value) : null;
    }
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public DateTimeOffset? ToDateTimeOffset(object? value)
    {
        if (IsNullConversionInput(value))
            return null;

        if (value is DateTimeOffset dateTimeOffset)
            return dateTimeOffset;

        if (value is DateTime dateTime)
            return new DateTimeOffset(dateTime);

        return ToDateTimeOffset(value!.ToString());
    }
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public DateTimeOffset? ToDateTimeOffsetWithFormat(string value, string format)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (!DateTimeOffset.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            return null;

        return result;
    }
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public DateTimeOffset? ToDateTimeOffsetWithFormat(string value, string format, string culture)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (!DateTimeOffset.TryParseExact(value, format, CultureInfo.GetCultureInfo(culture), DateTimeStyles.None, out var result))
            return null;

        return result;
    }
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public TimeSpan? SubtractDateTimeOffsets(DateTimeOffset? first, DateTimeOffset? second)
    {
        if (first is null || second is null)
            return null;

        return first.Value - second.Value;
    }

}
