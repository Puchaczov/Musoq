using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591 // Repetitive aggregate declaration overloads are documented by their region and attributes.

    [AggregateFunction(
        typeof(StDevAggregateKernel<byte>),
        Name = nameof(StDev),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? StDev(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(StDevAggregateKernel<sbyte>),
        Name = nameof(StDev),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? StDev(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(StDevAggregateKernel<short>),
        Name = nameof(StDev),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? StDev(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(StDevAggregateKernel<ushort>),
        Name = nameof(StDev),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? StDev(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(StDevAggregateKernel<int>),
        Name = nameof(StDev),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? StDev(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(StDevAggregateKernel<uint>),
        Name = nameof(StDev),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? StDev(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(StDevAggregateKernel<long>),
        Name = nameof(StDev),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? StDev(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(StDevAggregateKernel<ulong>),
        Name = nameof(StDev),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? StDev(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(StDevAggregateKernel<float>),
        Name = nameof(StDev),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? StDev(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(StDevAggregateKernel<double>),
        Name = nameof(StDev),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? StDev(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(StDevAggregateKernel<decimal>),
        Name = nameof(StDev),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? StDev(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    #pragma warning restore CS1591

}
