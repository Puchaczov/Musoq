using System.Numerics;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.TypedLibraryExamples;

public sealed class InvoiceLibrary : LibraryBase
{
    [AggregateFunction(typeof(SumKernel<decimal>), Name = "Sum",
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations |
            FunctionExecutionPermissions.IndependentGroups)]
    [WindowFunction(Name = "Sum", KernelType = typeof(SumKernel<decimal>),
        Modes = WindowEvaluationModes.WholePartition | WindowEvaluationModes.Running |
            WindowEvaluationModes.RecomputeFrames,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null,
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations |
            FunctionExecutionPermissions.IndependentPartitions)]
    public new decimal? Sum(decimal? value, [AggregateParent] int parent = 0)
        => AggregateFunction.NotInvoked<decimal?>();

    [AggregateFunction(typeof(PositiveCountKernel), Name = "PositiveCount",
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long PositiveCount(decimal? amount) => AggregateFunction.NotInvoked<long>();

    public static class PositiveCountKernel
    {
        public struct State { public long Count; }
        public static void Reset(ref State state) => state = default;
        public static void Set(ref State state, decimal? amount)
        {
            if (amount.HasValue && amount.Value > 0m)
                state.Count = checked(state.Count + 1);
        }
        public static long Get(in State state) => state.Count;
    }
}

public static class SumKernel<T> where T : struct, INumber<T>
{
    public struct State
    {
        public bool HasValue;
        public T Total;
    }

    public static void Reset(ref State state) => state = default;
    public static void Set(ref State state, T? value)
    {
        if (!value.HasValue)
            return;
        state.Total = state.HasValue ? checked(state.Total + value.Value) : value.Value;
        state.HasValue = true;
    }
    public static T? Get(in State state) => state.HasValue ? state.Total : null;
    public static void Merge(ref State target, in State source)
    {
        if (!source.HasValue)
            return;
        target.Total = target.HasValue ? checked(target.Total + source.Total) : source.Total;
        target.HasValue = true;
    }
}
