using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [CountAggregateFunction(
        typeof(CountAllAggregateKernel))]
    [WindowFunction(typeof(CountAllAggregateKernel), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count()
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountReferenceAggregateKernel<string>))]
    [WindowFunction(typeof(CountReferenceAggregateKernel<string>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count(string? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountNullableAggregateKernel<decimal>))]
    [WindowFunction(typeof(CountNullableAggregateKernel<decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountNullableAggregateKernel<DateTimeOffset>))]
    [WindowFunction(typeof(CountNullableAggregateKernel<DateTimeOffset>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count(DateTimeOffset? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountNullableAggregateKernel<DateTime>))]
    [WindowFunction(typeof(CountNullableAggregateKernel<DateTime>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count(DateTime? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountNullableAggregateKernel<TimeSpan>))]
    [WindowFunction(typeof(CountNullableAggregateKernel<TimeSpan>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count(TimeSpan? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountNullableAggregateKernel<byte>))]
    [WindowFunction(typeof(CountNullableAggregateKernel<byte>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountNullableAggregateKernel<sbyte>))]
    [WindowFunction(typeof(CountNullableAggregateKernel<sbyte>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountNullableAggregateKernel<short>))]
    [WindowFunction(typeof(CountNullableAggregateKernel<short>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountNullableAggregateKernel<ushort>))]
    [WindowFunction(typeof(CountNullableAggregateKernel<ushort>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountNullableAggregateKernel<int>))]
    [WindowFunction(typeof(CountNullableAggregateKernel<int>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountNullableAggregateKernel<uint>))]
    [WindowFunction(typeof(CountNullableAggregateKernel<uint>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountNullableAggregateKernel<long>))]
    [WindowFunction(typeof(CountNullableAggregateKernel<long>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountNullableAggregateKernel<ulong>))]
    [WindowFunction(typeof(CountNullableAggregateKernel<ulong>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountNullableAggregateKernel<float>))]
    [WindowFunction(typeof(CountNullableAggregateKernel<float>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountNullableAggregateKernel<double>))]
    [WindowFunction(typeof(CountNullableAggregateKernel<double>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountNullableAggregateKernel<bool>))]
    [WindowFunction(typeof(CountNullableAggregateKernel<bool>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long Count(bool? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();

}
