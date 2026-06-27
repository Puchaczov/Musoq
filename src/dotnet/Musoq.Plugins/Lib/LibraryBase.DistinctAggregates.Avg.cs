using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;
public partial class LibraryBase
{
    #pragma warning disable CS1591 // Repetitive aggregate declaration overloads are documented by their regions and attributes.

    [AggregateFunction(
        typeof(AvgDistinctAggregateKernel<byte>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public byte? AvgDistinct(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<byte?>();

    [AggregateFunction(
        typeof(AvgDistinctAggregateKernel<sbyte>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public sbyte? AvgDistinct(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<sbyte?>();

    [AggregateFunction(
        typeof(AvgDistinctAggregateKernel<short>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public short? AvgDistinct(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<short?>();

    [AggregateFunction(
        typeof(AvgDistinctAggregateKernel<ushort>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public ushort? AvgDistinct(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ushort?>();

    [AggregateFunction(
        typeof(AvgDistinctAggregateKernel<int>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public int? AvgDistinct(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<int?>();

    [AggregateFunction(
        typeof(AvgDistinctAggregateKernel<uint>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public uint? AvgDistinct(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<uint?>();

    [AggregateFunction(
        typeof(AvgDistinctAggregateKernel<long>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public long? AvgDistinct(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long?>();

    [AggregateFunction(
        typeof(AvgDistinctAggregateKernel<ulong>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public ulong? AvgDistinct(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ulong?>();

    [AggregateFunction(
        typeof(AvgDistinctAggregateKernel<float>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public float? AvgDistinct(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<float?>();

    [AggregateFunction(
        typeof(AvgDistinctAggregateKernel<double>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public double? AvgDistinct(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<double?>();

    [AggregateFunction(
        typeof(AvgDistinctAggregateKernel<decimal>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? AvgDistinct(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    #pragma warning restore CS1591
}
