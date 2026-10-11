using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    [AggregateFunction(
        typeof(MaxAggregateKernel<byte>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxAggregateKernel<byte>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MaxWindowBatch<byte>))]
    public byte? Max(byte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<byte?>();
    [AggregateFunction(
        typeof(MaxAggregateKernel<sbyte>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxAggregateKernel<sbyte>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MaxWindowBatch<sbyte>))]
    public sbyte? Max(sbyte? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<sbyte?>();
    [AggregateFunction(
        typeof(MaxAggregateKernel<short>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxAggregateKernel<short>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MaxWindowBatch<short>))]
    public short? Max(short? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<short?>();
    [AggregateFunction(
        typeof(MaxAggregateKernel<ushort>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxAggregateKernel<ushort>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MaxWindowBatch<ushort>))]
    public ushort? Max(ushort? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ushort?>();
    [AggregateFunction(
        typeof(MaxAggregateKernel<int>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxAggregateKernel<int>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MaxWindowBatch<int>))]
    public int? Max(int? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<int?>();
    [AggregateFunction(
        typeof(MaxAggregateKernel<uint>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxAggregateKernel<uint>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MaxWindowBatch<uint>))]
    public uint? Max(uint? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<uint?>();
    [AggregateFunction(
        typeof(MaxAggregateKernel<long>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxAggregateKernel<long>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MaxWindowBatch<long>))]
    public long? Max(long? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<long?>();
    [AggregateFunction(
        typeof(MaxAggregateKernel<ulong>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxAggregateKernel<ulong>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MaxWindowBatch<ulong>))]
    public ulong? Max(ulong? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<ulong?>();
    [AggregateFunction(
        typeof(MaxAggregateKernel<float>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxAggregateKernel<float>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MaxWindowBatch<float>))]
    public float? Max(float? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<float?>();
    [AggregateFunction(
        typeof(MaxAggregateKernel<double>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxAggregateKernel<double>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MaxWindowBatch<double>))]
    public double? Max(double? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<double?>();
    [AggregateFunction(
        typeof(MaxAggregateKernel<decimal>),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxAggregateKernel<decimal>), NumericWindowModes | WindowEvaluationModes.Batch,
        AggregateEmptyResultBehavior.Null, BatchKernelType = typeof(MaxWindowBatch<decimal>))]
    public decimal? Max(decimal? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<decimal?>();
    [AggregateFunction(
        typeof(MaxStringAggregateKernel),
        Inline = true,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations |
            FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(typeof(MaxStringAggregateKernel), NumericWindowModes, AggregateEmptyResultBehavior.Null)]
    public string? Max(string? value, [AggregateParent] int parent = 0)
        => AggregateDeclaration<string?>();

}
