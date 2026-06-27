using System.Numerics;

namespace Musoq.Plugins;

/// <summary>
///     Typed nullable standard-deviation kernel used by runtime-v2 generated aggregate code.
/// </summary>
/// <typeparam name="T">Concrete numeric input value type.</typeparam>
public static class StDevAggregateKernel<T>
    where T : struct, INumber<T>
{
    /// <summary>
    ///     Query-specific aggregate state stored directly on generated group classes.
    /// </summary>
    public struct State
    {
        /// <summary>Number of non-null values accumulated.</summary>
        public long Count;

        /// <summary>Accumulated input sum represented as double for variance calculation.</summary>
        public double Sum;

        /// <summary>Accumulated input square sum represented as double for variance calculation.</summary>
        public double SumOfSquares;
    }

    /// <summary>
    ///     Adds a nullable value to the state, ignoring null inputs.
    /// </summary>
    public static void Set(ref State state, T? value)
    {
        if (!value.HasValue)
            return;

        var converted = double.CreateChecked(value.GetValueOrDefault());
        state.Count = checked(state.Count + 1);
        state.Sum += converted;
        state.SumOfSquares += converted * converted;
    }

    /// <summary>
    ///     Gets the nullable population standard deviation.
    /// </summary>
    public static decimal? Get(in State state)
    {
        if (state.Count == 0)
            return null;

        var variance = (state.SumOfSquares - (state.Sum * state.Sum / state.Count)) / state.Count;
        if (variance is < 0d and > -1e-12d)
            variance = 0d;

        return (decimal)Math.Sqrt(variance);
    }

    /// <summary>
    ///     Merges another partial standard deviation state into the target state.
    /// </summary>
    public static void Merge(ref State target, in State source)
    {
        target.Count = checked(target.Count + source.Count);
        target.Sum += source.Sum;
        target.SumOfSquares += source.SumOfSquares;
    }
}
