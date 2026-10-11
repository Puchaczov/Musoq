using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;
public partial class LibraryBase
{
    #pragma warning disable CS1591

    [CountAggregateFunction(
        typeof(CountDistinctReferenceAggregateKernel<string>),
        Name = nameof(CountDistinct),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    [WindowFunction(typeof(CountDistinctReferenceAggregateKernel<string>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(string? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<decimal>),
        Name = nameof(CountDistinct),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    [WindowFunction(typeof(CountDistinctNullableAggregateKernel<decimal>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<DateTimeOffset>),
        Name = nameof(CountDistinct),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    [WindowFunction(typeof(CountDistinctNullableAggregateKernel<DateTimeOffset>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(DateTimeOffset? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<DateTime>),
        Name = nameof(CountDistinct),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    [WindowFunction(typeof(CountDistinctNullableAggregateKernel<DateTime>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(DateTime? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<byte>),
        Name = nameof(CountDistinct),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    [WindowFunction(typeof(CountDistinctNullableAggregateKernel<byte>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<sbyte>),
        Name = nameof(CountDistinct),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    [WindowFunction(typeof(CountDistinctNullableAggregateKernel<sbyte>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<short>),
        Name = nameof(CountDistinct),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    [WindowFunction(typeof(CountDistinctNullableAggregateKernel<short>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<ushort>),
        Name = nameof(CountDistinct),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    [WindowFunction(typeof(CountDistinctNullableAggregateKernel<ushort>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<int>),
        Name = nameof(CountDistinct),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    [WindowFunction(typeof(CountDistinctNullableAggregateKernel<int>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<uint>),
        Name = nameof(CountDistinct),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    [WindowFunction(typeof(CountDistinctNullableAggregateKernel<uint>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<long>),
        Name = nameof(CountDistinct),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    [WindowFunction(typeof(CountDistinctNullableAggregateKernel<long>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<ulong>),
        Name = nameof(CountDistinct),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    [WindowFunction(typeof(CountDistinctNullableAggregateKernel<ulong>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<float>),
        Name = nameof(CountDistinct),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    [WindowFunction(typeof(CountDistinctNullableAggregateKernel<float>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<double>),
        Name = nameof(CountDistinct),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    [WindowFunction(typeof(CountDistinctNullableAggregateKernel<double>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
    [CountAggregateFunction(
        typeof(CountDistinctNullableAggregateKernel<bool>),
        Name = nameof(CountDistinct),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations | FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    [WindowFunction(typeof(CountDistinctNullableAggregateKernel<bool>), NumericWindowModes, AggregateEmptyResultBehavior.Zero)]
    public long CountDistinct(bool? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long>();
}
