using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [AggregateFunction(
        typeof(AvgAggregateKernel<byte>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public byte? Avg(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<byte?>();
    [AggregateFunction(
        typeof(AvgAggregateKernel<sbyte>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public sbyte? Avg(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<sbyte?>();
    [AggregateFunction(
        typeof(AvgAggregateKernel<short>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public short? Avg(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<short?>();
    [AggregateFunction(
        typeof(AvgAggregateKernel<ushort>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public ushort? Avg(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ushort?>();
    [AggregateFunction(
        typeof(AvgAggregateKernel<int>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public int? Avg(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<int?>();
    [AggregateFunction(
        typeof(AvgAggregateKernel<uint>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public uint? Avg(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<uint?>();
    [AggregateFunction(
        typeof(AvgAggregateKernel<long>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public long? Avg(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long?>();
    [AggregateFunction(
        typeof(AvgAggregateKernel<ulong>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public ulong? Avg(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ulong?>();
    [AggregateFunction(
        typeof(AvgAggregateKernel<float>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public float? Avg(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<float?>();
    [AggregateFunction(
        typeof(AvgAggregateKernel<double>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public double? Avg(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<double?>();
    [AggregateFunction(
        typeof(AvgAggregateKernel<decimal>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? Avg(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

}
