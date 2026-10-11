using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [AggregateFunction(
        typeof(AverageKernel<byte, decimal>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AverageKernel<byte, decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? Avg(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();
    [AggregateFunction(
        typeof(AverageKernel<sbyte, decimal>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AverageKernel<sbyte, decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? Avg(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();
    [AggregateFunction(
        typeof(AverageKernel<short, decimal>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AverageKernel<short, decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? Avg(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();
    [AggregateFunction(
        typeof(AverageKernel<ushort, decimal>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AverageKernel<ushort, decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? Avg(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();
    [AggregateFunction(
        typeof(AverageKernel<int, decimal>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AverageKernel<int, decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? Avg(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();
    [AggregateFunction(
        typeof(AverageKernel<uint, decimal>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AverageKernel<uint, decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? Avg(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();
    [AggregateFunction(
        typeof(AverageKernel<long, decimal>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AverageKernel<long, decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? Avg(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();
    [AggregateFunction(
        typeof(AverageKernel<ulong, decimal>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AverageKernel<ulong, decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? Avg(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();
    [AggregateFunction(
        typeof(AvgAggregateKernel<float>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AvgAggregateKernel<float>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public float? Avg(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<float?>();
    [AggregateFunction(
        typeof(AvgAggregateKernel<double>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AvgAggregateKernel<double>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public double? Avg(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<double?>();
    [AggregateFunction(
        typeof(AvgAggregateKernel<decimal>),
        Name = nameof(Avg),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AvgAggregateKernel<decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? Avg(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

}
