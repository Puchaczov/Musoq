using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [AggregateFunction(
        typeof(MinAggregateKernel<byte>),
        Name = nameof(Min),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public byte? Min(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<byte?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<sbyte>),
        Name = nameof(Min),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public sbyte? Min(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<sbyte?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<short>),
        Name = nameof(Min),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public short? Min(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<short?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<ushort>),
        Name = nameof(Min),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public ushort? Min(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ushort?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<int>),
        Name = nameof(Min),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public int? Min(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<int?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<uint>),
        Name = nameof(Min),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public uint? Min(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<uint?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<long>),
        Name = nameof(Min),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public long? Min(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<ulong>),
        Name = nameof(Min),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public ulong? Min(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ulong?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<float>),
        Name = nameof(Min),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public float? Min(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<float?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<double>),
        Name = nameof(Min),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public double? Min(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<double?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<decimal>),
        Name = nameof(Min),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? Min(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

}
