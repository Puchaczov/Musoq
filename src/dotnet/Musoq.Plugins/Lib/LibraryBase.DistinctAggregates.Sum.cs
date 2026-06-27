using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;
public partial class LibraryBase
{
    #pragma warning disable CS1591

    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<byte>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public byte? SumDistinct(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<byte?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<sbyte>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public sbyte? SumDistinct(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<sbyte?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<short>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public short? SumDistinct(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<short?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<ushort>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public ushort? SumDistinct(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ushort?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<int>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public int? SumDistinct(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<int?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<uint>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public uint? SumDistinct(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<uint?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<long>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public long? SumDistinct(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<ulong>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public ulong? SumDistinct(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ulong?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<float>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public float? SumDistinct(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<float?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<double>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public double? SumDistinct(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<double?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<decimal>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? SumDistinct(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();
}
