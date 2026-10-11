using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [AggregateFunction(
        typeof(MinAggregateKernel<byte>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinAggregateKernel<byte>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MinWindowBatch<byte>))]
    public byte? Min(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<byte?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<sbyte>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinAggregateKernel<sbyte>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MinWindowBatch<sbyte>))]
    public sbyte? Min(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<sbyte?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<short>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinAggregateKernel<short>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MinWindowBatch<short>))]
    public short? Min(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<short?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<ushort>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinAggregateKernel<ushort>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MinWindowBatch<ushort>))]
    public ushort? Min(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ushort?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<int>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinAggregateKernel<int>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MinWindowBatch<int>))]
    public int? Min(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<int?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<uint>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinAggregateKernel<uint>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MinWindowBatch<uint>))]
    public uint? Min(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<uint?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<long>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinAggregateKernel<long>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MinWindowBatch<long>))]
    public long? Min(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<ulong>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinAggregateKernel<ulong>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MinWindowBatch<ulong>))]
    public ulong? Min(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ulong?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<float>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinAggregateKernel<float>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MinWindowBatch<float>))]
    public float? Min(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<float?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<double>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinAggregateKernel<double>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MinWindowBatch<double>))]
    public double? Min(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<double?>();
    [AggregateFunction(
        typeof(MinAggregateKernel<decimal>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinAggregateKernel<decimal>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MinWindowBatch<decimal>))]
    public decimal? Min(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();
    [AggregateFunction(
        typeof(MinStringAggregateKernel),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations |
            FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MinStringAggregateKernel), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public string? Min(string? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<string?>();

}
