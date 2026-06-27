using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591 // Repetitive aggregate declaration overloads are documented by their region and attributes.

    [AggregateFunction(
        typeof(SumOutcomeAggregateKernel<byte>),
        Name = nameof(SumOutcome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public byte? SumOutcome(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<byte?>();

    [AggregateFunction(
        typeof(SumOutcomeAggregateKernel<sbyte>),
        Name = nameof(SumOutcome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public sbyte? SumOutcome(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<sbyte?>();

    [AggregateFunction(
        typeof(SumOutcomeAggregateKernel<short>),
        Name = nameof(SumOutcome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public short? SumOutcome(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<short?>();

    [AggregateFunction(
        typeof(SumOutcomeAggregateKernel<ushort>),
        Name = nameof(SumOutcome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public ushort? SumOutcome(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ushort?>();

    [AggregateFunction(
        typeof(SumOutcomeAggregateKernel<int>),
        Name = nameof(SumOutcome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public int? SumOutcome(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<int?>();

    [AggregateFunction(
        typeof(SumOutcomeAggregateKernel<uint>),
        Name = nameof(SumOutcome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public uint? SumOutcome(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<uint?>();

    [AggregateFunction(
        typeof(SumOutcomeAggregateKernel<long>),
        Name = nameof(SumOutcome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public long? SumOutcome(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long?>();

    [AggregateFunction(
        typeof(SumOutcomeAggregateKernel<ulong>),
        Name = nameof(SumOutcome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public ulong? SumOutcome(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ulong?>();

    [AggregateFunction(
        typeof(SumOutcomeAggregateKernel<float>),
        Name = nameof(SumOutcome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public float? SumOutcome(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<float?>();

    [AggregateFunction(
        typeof(SumOutcomeAggregateKernel<double>),
        Name = nameof(SumOutcome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public double? SumOutcome(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<double?>();

    [AggregateFunction(
        typeof(SumOutcomeAggregateKernel<decimal>),
        Name = nameof(SumOutcome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? SumOutcome(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    #pragma warning restore CS1591

}
