namespace Musoq.Plugins;

#pragma warning disable CS1591

public static class SumTimeSpanAggregateKernel
{
    public struct State
    {
        public bool HasValue;
        public TimeSpan Value;
    }

    public static void Set(ref State state, TimeSpan? value)
    {
        if (!value.HasValue)
            return;

        state.Value = state.HasValue
            ? checked(state.Value + value.GetValueOrDefault())
            : value.GetValueOrDefault();
        state.HasValue = true;
    }

    public static TimeSpan? Get(in State state)
        => NullableAggregateCore.GetNullable(state.HasValue, state.Value);

    public static void Merge(ref State target, in State source)
    {
        if (!source.HasValue)
            return;

        target.Value = target.HasValue
            ? checked(target.Value + source.Value)
            : source.Value;
        target.HasValue = true;
    }
}
