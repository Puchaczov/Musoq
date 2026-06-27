using System.Numerics;

namespace Musoq.Plugins;

internal static class NullableAggregateCore
{
    public static void SetSum<T>(ref bool hasValue, ref T sum, T? value)
        where T : struct, INumber<T>
    {
        SetSum(ref hasValue, ref sum, value, static _ => true);
    }

    public static void SetSum<T>(ref bool hasValue, ref T sum, T? value, Func<T, bool> shouldInclude)
        where T : struct, INumber<T>
    {
        if (!value.HasValue)
            return;

        var current = value.GetValueOrDefault();
        if (!shouldInclude(current))
            return;

        sum = hasValue
            ? checked(sum + current)
            : current;
        hasValue = true;
    }

    public static void MergeSum<T>(ref bool targetHasValue, ref T targetSum, bool sourceHasValue, T sourceSum)
        where T : struct, INumber<T>
    {
        if (!sourceHasValue)
            return;

        targetSum = targetHasValue
            ? checked(targetSum + sourceSum)
            : sourceSum;
        targetHasValue = true;
    }

    public static void SetBest<T>(ref bool hasValue, ref T best, T? value, Func<T, T, bool> isBetter)
        where T : struct
    {
        if (!value.HasValue)
            return;

        var current = value.GetValueOrDefault();
        if (!hasValue || isBetter(current, best))
            best = current;

        hasValue = true;
    }

    public static void MergeBest<T>(ref bool targetHasValue, ref T targetBest, bool sourceHasValue, T sourceBest, Func<T, T, bool> isBetter)
        where T : struct
    {
        if (!sourceHasValue)
            return;

        if (!targetHasValue || isBetter(sourceBest, targetBest))
            targetBest = sourceBest;

        targetHasValue = true;
    }

    public static T? GetNullable<T>(bool hasValue, T value)
        where T : struct
    {
        return hasValue ? value : null;
    }
}
