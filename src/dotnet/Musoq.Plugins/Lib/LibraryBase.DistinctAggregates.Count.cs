using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;
public partial class LibraryBase
{
    #pragma warning disable CS1591

    [AggregateFunction(
        typeof(CountDistinctReferenceAggregateKernel<string>),
        Name = nameof(CountDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(string? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<decimal>),
        Name = nameof(CountDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<DateTimeOffset>),
        Name = nameof(CountDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(DateTimeOffset? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<DateTime>),
        Name = nameof(CountDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(DateTime? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<byte>),
        Name = nameof(CountDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<sbyte>),
        Name = nameof(CountDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<short>),
        Name = nameof(CountDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<ushort>),
        Name = nameof(CountDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<int>),
        Name = nameof(CountDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<uint>),
        Name = nameof(CountDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<long>),
        Name = nameof(CountDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<ulong>),
        Name = nameof(CountDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<float>),
        Name = nameof(CountDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<double>),
        Name = nameof(CountDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [AggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<bool>),
        Name = nameof(CountDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(bool? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
}
