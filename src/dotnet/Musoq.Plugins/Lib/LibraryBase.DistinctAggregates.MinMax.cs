using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;
public partial class LibraryBase
{
    #pragma warning disable CS1591 // Repetitive aggregate declaration overloads are documented by their regions and attributes.

    [AggregateFunction(
        typeof(MinDistinctAggregateKernel<byte>),
        Name = nameof(MinDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinDistinctAggregateKernel<byte>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public byte? MinDistinct(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<byte?>();

    [AggregateFunction(
        typeof(MinDistinctAggregateKernel<sbyte>),
        Name = nameof(MinDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinDistinctAggregateKernel<sbyte>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public sbyte? MinDistinct(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<sbyte?>();

    [AggregateFunction(
        typeof(MinDistinctAggregateKernel<short>),
        Name = nameof(MinDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinDistinctAggregateKernel<short>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public short? MinDistinct(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<short?>();

    [AggregateFunction(
        typeof(MinDistinctAggregateKernel<ushort>),
        Name = nameof(MinDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinDistinctAggregateKernel<ushort>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public ushort? MinDistinct(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ushort?>();

    [AggregateFunction(
        typeof(MinDistinctAggregateKernel<int>),
        Name = nameof(MinDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinDistinctAggregateKernel<int>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public int? MinDistinct(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<int?>();

    [AggregateFunction(
        typeof(MinDistinctAggregateKernel<uint>),
        Name = nameof(MinDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinDistinctAggregateKernel<uint>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public uint? MinDistinct(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<uint?>();

    [AggregateFunction(
        typeof(MinDistinctAggregateKernel<long>),
        Name = nameof(MinDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinDistinctAggregateKernel<long>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public long? MinDistinct(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long?>();

    [AggregateFunction(
        typeof(MinDistinctAggregateKernel<ulong>),
        Name = nameof(MinDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinDistinctAggregateKernel<ulong>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public ulong? MinDistinct(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ulong?>();

    [AggregateFunction(
        typeof(MinDistinctAggregateKernel<float>),
        Name = nameof(MinDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinDistinctAggregateKernel<float>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public float? MinDistinct(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<float?>();

    [AggregateFunction(
        typeof(MinDistinctAggregateKernel<double>),
        Name = nameof(MinDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinDistinctAggregateKernel<double>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public double? MinDistinct(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<double?>();

    [AggregateFunction(
        typeof(MinDistinctAggregateKernel<decimal>),
        Name = nameof(MinDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinDistinctAggregateKernel<decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? MinDistinct(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(MaxDistinctAggregateKernel<byte>),
        Name = nameof(MaxDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxDistinctAggregateKernel<byte>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public byte? MaxDistinct(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<byte?>();

    [AggregateFunction(
        typeof(MaxDistinctAggregateKernel<sbyte>),
        Name = nameof(MaxDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxDistinctAggregateKernel<sbyte>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public sbyte? MaxDistinct(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<sbyte?>();

    [AggregateFunction(
        typeof(MaxDistinctAggregateKernel<short>),
        Name = nameof(MaxDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxDistinctAggregateKernel<short>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public short? MaxDistinct(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<short?>();

    [AggregateFunction(
        typeof(MaxDistinctAggregateKernel<ushort>),
        Name = nameof(MaxDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxDistinctAggregateKernel<ushort>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public ushort? MaxDistinct(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ushort?>();

    [AggregateFunction(
        typeof(MaxDistinctAggregateKernel<int>),
        Name = nameof(MaxDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxDistinctAggregateKernel<int>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public int? MaxDistinct(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<int?>();

    [AggregateFunction(
        typeof(MaxDistinctAggregateKernel<uint>),
        Name = nameof(MaxDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxDistinctAggregateKernel<uint>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public uint? MaxDistinct(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<uint?>();

    [AggregateFunction(
        typeof(MaxDistinctAggregateKernel<long>),
        Name = nameof(MaxDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxDistinctAggregateKernel<long>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public long? MaxDistinct(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long?>();

    [AggregateFunction(
        typeof(MaxDistinctAggregateKernel<ulong>),
        Name = nameof(MaxDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxDistinctAggregateKernel<ulong>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public ulong? MaxDistinct(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ulong?>();

    [AggregateFunction(
        typeof(MaxDistinctAggregateKernel<float>),
        Name = nameof(MaxDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxDistinctAggregateKernel<float>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public float? MaxDistinct(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<float?>();

    [AggregateFunction(
        typeof(MaxDistinctAggregateKernel<double>),
        Name = nameof(MaxDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxDistinctAggregateKernel<double>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public double? MaxDistinct(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<double?>();

    [AggregateFunction(
        typeof(MaxDistinctAggregateKernel<decimal>),
        Name = nameof(MaxDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxDistinctAggregateKernel<decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? MaxDistinct(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    #pragma warning restore CS1591
}
