using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using System.Numerics;

namespace Musoq.Evaluator.Tests.Schema.LibraryExecution;

public sealed class LibraryExecutionLibrary : LibraryBase
{
    [BindableMethod]
    public int SourcePosition([InjectSpecificSource(typeof(LibraryExecutionEntity))] LibraryExecutionEntity entity)
        => entity.Position;

    [BindableMethod]
    public int ValueSourcePosition([InjectSpecificSource(typeof(LibraryExecutionValueEntity))] LibraryExecutionValueEntity entity)
        => entity.Position;

    [BindableMethod]
    public string ErasedScalar(object value) => value.ToString()!;

    [BindableMethod]
    public int ErasedInterfaceScalar(System.IComparable value) => value.CompareTo(0);

    [AggregateFunction(typeof(PositiveTotalKernel), EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? PositiveTotal(decimal? amount)
        => AggregateFunction.NotInvoked<decimal?>();

    [AggregateFunction(typeof(ScaledTotalKernel), EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(KernelType = typeof(ScaledTotalKernel),
        Modes = WindowEvaluationModes.WholePartition | WindowEvaluationModes.Running | WindowEvaluationModes.RecomputeFrames,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public decimal? ScaledTotal(decimal? amount, int factor)
        => AggregateFunction.NotInvoked<decimal?>();

    [AggregateFunction(typeof(SeededTotalKernel))]
    [WindowFunction(KernelType = typeof(SeededTotalKernel),
        Modes = WindowEvaluationModes.WholePartition | WindowEvaluationModes.Running | WindowEvaluationModes.RecomputeFrames)]
    public decimal SeededTotal(decimal? amount) => AggregateFunction.NotInvoked<decimal>();

    [AggregateFunction(typeof(PositiveCountKernel), EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    [WindowFunction(KernelType = typeof(PositiveCountKernel),
        Modes = WindowEvaluationModes.WholePartition | WindowEvaluationModes.Running | WindowEvaluationModes.RecomputeFrames,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long PositiveCount(decimal? amount) => AggregateFunction.NotInvoked<long>();

    [AggregateFunction(typeof(ReferencePositiveCountKernel), EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public long ReferencePositiveCount(decimal? amount) => AggregateFunction.NotInvoked<long>();

    public static class ReferencePositiveCountKernel
    {
        private static readonly decimal Minimum = 0m;
        public sealed class State { public long Count; }
        public static void Reset(ref State state) => state = new();
        public static void Set(ref State state, decimal? amount)
        {
            if (amount > Minimum)
                state.Count = checked(state.Count + 1);
        }
        public static long Get(in State state) => state.Count;
    }

    [AggregateFunction(typeof(GenericTotalKernel<>), EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    [WindowFunction(KernelType = typeof(GenericTotalKernel<>),
        Modes = WindowEvaluationModes.WholePartition | WindowEvaluationModes.Running | WindowEvaluationModes.RecomputeFrames,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Null)]
    public T? GenericTotal<T>(T? value, [AggregateParent] int parent = 0) where T : struct, INumber<T>
        => AggregateFunction.NotInvoked<T?>();

    public static class GenericTotalKernel<T> where T : struct, INumber<T>
    {
        public struct State { public SumAggregateKernel<T>.State Total; }
        public static void Reset(ref State state) => state = default;
        public static void Set(ref State state, T? value) => SumAggregateKernel<T>.Set(ref state.Total, value);
        public static T? Get(in State state) => SumAggregateKernel<T>.Get(in state.Total);
    }

    public static class SeededTotalKernel
    {
        public struct State { public decimal Total; }
        public static void Reset(ref State state) => state.Total = 100m;
        public static void Set(ref State state, decimal? amount)
        {
            if (amount.HasValue)
                state.Total = checked(state.Total + amount.Value);
        }
        public static decimal Get(in State state) => state.Total;
    }

    public static class PositiveCountKernel
    {
        public struct State { public long Count; }
        public static void Reset(ref State state) => state = default;
        public static void Set(ref State state, decimal? amount)
        {
            if (amount > 0m)
                state.Count = checked(state.Count + 1);
        }
        public static long Get(in State state) => state.Count;
    }

    public static class PositiveTotalKernel
    {
        public struct State
        {
            public bool HasValue;
            public decimal Total;
        }

        public static void Reset(ref State state) => state = default;

        public static void Set(ref State state, decimal? amount)
        {
            if (!amount.HasValue || amount.Value <= 0m)
                return;

            state.Total = checked(state.Total + amount.Value);
            state.HasValue = true;
        }

        public static decimal? Get(in State state) => state.HasValue ? state.Total : null;
    }

    public static class ScaledTotalKernel
    {
        public struct State
        {
            public bool HasValue;
            public decimal Total;
        }

        public static void Reset(ref State state) => state = default;

        public static void Set(ref State state, decimal? amount, int factor)
        {
            if (!amount.HasValue)
                return;

            state.Total = checked(state.Total + checked(amount.Value * factor));
            state.HasValue = true;
        }

        public static decimal? Get(in State state) => state.HasValue ? state.Total : null;
    }
}
