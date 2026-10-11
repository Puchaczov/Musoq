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
    [WindowFunction(typeof(SumDistinctAggregateKernel<byte>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public byte? SumDistinct(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<byte?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<sbyte>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(SumDistinctAggregateKernel<sbyte>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public sbyte? SumDistinct(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<sbyte?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<short>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(SumDistinctAggregateKernel<short>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public short? SumDistinct(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<short?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<ushort>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(SumDistinctAggregateKernel<ushort>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public ushort? SumDistinct(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ushort?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<int>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(SumDistinctAggregateKernel<int>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public int? SumDistinct(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<int?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<uint>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(SumDistinctAggregateKernel<uint>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public uint? SumDistinct(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<uint?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<long>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(SumDistinctAggregateKernel<long>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public long? SumDistinct(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<ulong>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(SumDistinctAggregateKernel<ulong>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public ulong? SumDistinct(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ulong?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<float>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(SumDistinctAggregateKernel<float>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public float? SumDistinct(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<float?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<double>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(SumDistinctAggregateKernel<double>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public double? SumDistinct(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<double?>();
    [AggregateFunction(
        typeof(SumDistinctAggregateKernel<decimal>),
        Name = nameof(SumDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(SumDistinctAggregateKernel<decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? SumDistinct(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();
}
