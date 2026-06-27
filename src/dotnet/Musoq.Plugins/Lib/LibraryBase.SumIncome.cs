using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591 // Repetitive aggregate declaration overloads are documented by their region and attributes.

    [AggregateFunction(
        typeof(SumIncomeAggregateKernel<byte>),
        Name = nameof(SumIncome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public byte? SumIncome(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<byte?>();

    [AggregateFunction(
        typeof(SumIncomeAggregateKernel<sbyte>),
        Name = nameof(SumIncome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public sbyte? SumIncome(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<sbyte?>();

    [AggregateFunction(
        typeof(SumIncomeAggregateKernel<short>),
        Name = nameof(SumIncome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public short? SumIncome(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<short?>();

    [AggregateFunction(
        typeof(SumIncomeAggregateKernel<ushort>),
        Name = nameof(SumIncome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public ushort? SumIncome(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ushort?>();

    [AggregateFunction(
        typeof(SumIncomeAggregateKernel<int>),
        Name = nameof(SumIncome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public int? SumIncome(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<int?>();

    [AggregateFunction(
        typeof(SumIncomeAggregateKernel<uint>),
        Name = nameof(SumIncome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public uint? SumIncome(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<uint?>();

    [AggregateFunction(
        typeof(SumIncomeAggregateKernel<long>),
        Name = nameof(SumIncome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public long? SumIncome(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long?>();

    [AggregateFunction(
        typeof(SumIncomeAggregateKernel<ulong>),
        Name = nameof(SumIncome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public ulong? SumIncome(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ulong?>();

    [AggregateFunction(
        typeof(SumIncomeAggregateKernel<float>),
        Name = nameof(SumIncome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public float? SumIncome(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<float?>();

    [AggregateFunction(
        typeof(SumIncomeAggregateKernel<double>),
        Name = nameof(SumIncome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public double? SumIncome(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<double?>();

    [AggregateFunction(
        typeof(SumIncomeAggregateKernel<decimal>),
        Name = nameof(SumIncome),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? SumIncome(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();

    #pragma warning restore CS1591

}
