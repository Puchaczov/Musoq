using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [AggregateFunction(
        typeof(CountAllAggregateKernel),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count()
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountReferenceAggregateKernel<string>),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count(string? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountNullableAggregateKernel<decimal>),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountNullableAggregateKernel<DateTimeOffset>),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count(DateTimeOffset? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountNullableAggregateKernel<DateTime>),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count(DateTime? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountNullableAggregateKernel<TimeSpan>),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count(TimeSpan? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountNullableAggregateKernel<byte>),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountNullableAggregateKernel<sbyte>),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountNullableAggregateKernel<short>),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountNullableAggregateKernel<ushort>),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountNullableAggregateKernel<int>),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountNullableAggregateKernel<uint>),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountNullableAggregateKernel<long>),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountNullableAggregateKernel<ulong>),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountNullableAggregateKernel<float>),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountNullableAggregateKernel<double>),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountNullableAggregateKernel<bool>),
        Name = nameof(Count),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long Count(bool? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();

}
