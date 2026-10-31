using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Converter.Tests.Schema;

public class EmptyLibrary : LibraryBase
{
    [BindableMethod(ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations)]
    public int ExpensiveMethod(int value)
    {
        return value * 2;
    }

    [AggregateFunction(
        typeof(CustomLengthTotalAggregate),
        Name = nameof(CustomLengthTotal),
        Inline = true,
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero)]
    public int CustomLengthTotal(int? value, [AggregateParent] int parent = 0)
    {
        return AggregateFunction.NotInvoked<int>();
    }

    [WindowFunction(typeof(RunningProductKernel), WindowEvaluationModes.Running | WindowEvaluationModes.WholePartition | WindowEvaluationModes.RecomputeFrames,
        AggregateEmptyResultBehavior.Zero, Name = "RunningProduct")]
    public decimal RunningProduct(int value) => AggregateFunction.NotInvoked<decimal>();

    [WindowFunction(typeof(ScaledRunningProductKernel), WindowEvaluationModes.Running | WindowEvaluationModes.WholePartition | WindowEvaluationModes.RecomputeFrames,
        AggregateEmptyResultBehavior.Zero, Name = "ScaledRunningProduct")]
    public decimal ScaledRunningProduct(int value, int scale) => AggregateFunction.NotInvoked<decimal>();

    [WindowFunction(KernelType = typeof(ObjectRunningProductKernel), ContextType = typeof(WindowFrameContext<>),
        Modes = WindowEvaluationModes.Running | WindowEvaluationModes.WholePartition)]
    public decimal ObjectRunningProduct(object value) => AggregateFunction.NotInvoked<decimal>();

    public static class CustomLengthTotalAggregate
    {
        public struct State
        {
            public int Sum;
        }

        public static void Reset(ref State state) => state = default;

        public static void Set(ref State state, int? value)
        {
            if (value.HasValue)
                state.Sum = checked(state.Sum + value.GetValueOrDefault());
        }

        public static int Get(in State state)
        {
            return state.Sum;
        }

        public static void Merge(ref State target, in State source)
        {
            target.Sum = checked(target.Sum + source.Sum);
        }
    }

    public static class RunningProductKernel
    {
        public struct State { public decimal Product; }
        public static void Reset(ref State state) => state.Product = 1m;
        public static void Set(ref State state, int value) => state.Product *= value;
        public static decimal Get(in State state) => state.Product;
    }

    public static class ScaledRunningProductKernel
    {
        public struct State { public decimal Product; }
        public static void Reset(ref State state) => state.Product = 1m;
        public static void Set(ref State state, int value, int scale) => state.Product *= value * scale;
        public static decimal Get(in State state) => state.Product;
    }

    public static class ObjectRunningProductKernel
    {
        public static decimal Get(in WindowFrameContext<object> context) => 0m;
    }
}
