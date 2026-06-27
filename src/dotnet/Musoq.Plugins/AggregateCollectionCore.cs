using System.Collections.Generic;
using System.Numerics;

namespace Musoq.Plugins;

internal static class AggregateCollectionCore
{
    public static void AddOrdered<T>(ref List<T>? values, T value)
    {
        (values ??= []).Add(value);
    }

    public static void MergeOrdered<T>(ref List<T>? target, List<T>? source)
    {
        if (source is null || source.Count == 0)
            return;

        (target ??= []).AddRange(source);
    }

    public static string Join(List<string>? values, string delimiter)
    {
        return values is { Count: > 0 }
            ? string.Join(delimiter, values)
            : string.Empty;
    }

    public static void AddNullableDistinct<T>(ref HashSet<T>? values, T? value)
        where T : struct
    {
        if (value.HasValue)
            AddDistinct(ref values, value.GetValueOrDefault());
    }

    public static void AddReferenceDistinct<T>(ref HashSet<T>? values, T? value)
        where T : class
    {
        if (value is not null)
            AddDistinct(ref values, value);
    }

    public static void MergeDistinct<T>(ref HashSet<T>? target, HashSet<T>? source)
    {
        if (source is null)
            return;

        (target ??= []).UnionWith(source);
    }

    public static long CountDistinct<T>(HashSet<T>? values)
    {
        return values?.Count ?? 0L;
    }

    public static T? SumDistinct<T>(HashSet<T>? values)
        where T : struct, INumber<T>
    {
        if (values is not { Count: > 0 })
            return null;

        var sum = T.Zero;
        foreach (var value in values)
            sum = checked(sum + value);

        return sum;
    }

    public static T? AverageDistinct<T>(HashSet<T>? values)
        where T : struct, INumber<T>
    {
        if (values is not { Count: > 0 })
            return null;

        var sum = T.Zero;
        foreach (var value in values)
            sum = checked(sum + value);

        return sum / T.CreateChecked(values.Count);
    }

    private static void AddDistinct<T>(ref HashSet<T>? values, T value)
    {
        (values ??= []).Add(value);
    }
}
