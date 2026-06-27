using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [AggregateFunction(
        typeof(SumAggregateKernel<byte>),
        Name = nameof(Sum),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public byte? Sum(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<byte?>();
    [AggregateFunction(
        typeof(SumAggregateKernel<sbyte>),
        Name = nameof(Sum),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public sbyte? Sum(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<sbyte?>();
    [AggregateFunction(
        typeof(SumAggregateKernel<short>),
        Name = nameof(Sum),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public short? Sum(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<short?>();
    [AggregateFunction(
        typeof(SumAggregateKernel<ushort>),
        Name = nameof(Sum),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public ushort? Sum(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ushort?>();
    [AggregateFunction(
        typeof(SumAggregateKernel<int>),
        Name = nameof(Sum),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public int? Sum(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<int?>();
    [AggregateFunction(
        typeof(SumAggregateKernel<int>),
        Name = nameof(Sum),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public int? Sum([AggregateParent] int parent, int? value)
        => AggregateDeclaration<int?>();
    [AggregateFunction(
        typeof(SumAggregateKernel<uint>),
        Name = nameof(Sum),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public uint? Sum(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<uint?>();
    [AggregateFunction(
        typeof(SumAggregateKernel<long>),
        Name = nameof(Sum),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public long? Sum(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long?>();
    [AggregateFunction(
        typeof(SumAggregateKernel<ulong>),
        Name = nameof(Sum),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public ulong? Sum(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ulong?>();
    [AggregateFunction(
        typeof(SumAggregateKernel<float>),
        Name = nameof(Sum),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public float? Sum(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<float?>();
    [AggregateFunction(
        typeof(SumAggregateKernel<double>),
        Name = nameof(Sum),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public double? Sum(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<double?>();
    [AggregateFunction(
        typeof(SumAggregateKernel<decimal>),
        Name = nameof(Sum),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? Sum(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

}
