namespace Musoq.Plugins;

internal static class ReferenceAggregateCore
{
    public static void SetBest<T>(ref T? best, T? value, Func<T, T, bool> isBetter)
        where T : class
    {
        if (value is null)
            return;

        if (best is null || isBetter(value, best))
            best = value;
    }
}
