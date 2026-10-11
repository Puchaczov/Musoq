using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;
public partial class LibraryBase
{
    #pragma warning disable CS1591 // Repetitive aggregate declaration overloads are documented by their regions and attributes.

    [AggregateFunction(
        typeof(AverageDistinctKernel<byte, decimal>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AverageDistinctKernel<byte, decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? AvgDistinct(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(AverageDistinctKernel<sbyte, decimal>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AverageDistinctKernel<sbyte, decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? AvgDistinct(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(AverageDistinctKernel<short, decimal>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AverageDistinctKernel<short, decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? AvgDistinct(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(AverageDistinctKernel<ushort, decimal>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AverageDistinctKernel<ushort, decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? AvgDistinct(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(AverageDistinctKernel<int, decimal>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AverageDistinctKernel<int, decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? AvgDistinct(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(AverageDistinctKernel<uint, decimal>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AverageDistinctKernel<uint, decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? AvgDistinct(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(AverageDistinctKernel<long, decimal>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AverageDistinctKernel<long, decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? AvgDistinct(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(AverageDistinctKernel<ulong, decimal>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AverageDistinctKernel<ulong, decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? AvgDistinct(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    [AggregateFunction(
        typeof(AvgDistinctAggregateKernel<float>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AvgDistinctAggregateKernel<float>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public float? AvgDistinct(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<float?>();

    [AggregateFunction(
        typeof(AvgDistinctAggregateKernel<double>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AvgDistinctAggregateKernel<double>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public double? AvgDistinct(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<double?>();

    [AggregateFunction(
        typeof(AvgDistinctAggregateKernel<decimal>),
        Name = nameof(AvgDistinct),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(AvgDistinctAggregateKernel<decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public decimal? AvgDistinct(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    #pragma warning restore CS1591
}
