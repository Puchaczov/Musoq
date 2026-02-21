using BenchmarkDotNet.Attributes;
using Musoq.Plugins;

namespace Musoq.Benchmarks;

/// <summary>
///     Micro-benchmarks for the aggregation-set hot paths changed in this optimisation round.
///     Each "Before" variant re-implements the original two-step pattern inline; "After"
///     uses the new single-lookup <c>AddDecimalValue</c> / <c>IncrementIntValue</c> /
///     <c>UpdateDecimalIfGreater</c> / <c>UpdateDecimalIfLess</c> methods on <see cref="Group"/>.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class AggregationSetBenchmark
{
    private const int Iterations = 10_000;
    private const string KeyName = "agg";

    // ─── Sum (decimal accumulation) ───────────────────────────────────────────

    [Benchmark(Description = "SetSum (decimal) — before (GetOrCreate+SetValue)")]
    public decimal SetSumDecimal_Before()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        for (var i = 0; i < Iterations; i++)
        {
            var v = group.GetOrCreateValue<decimal>(KeyName);
            group.SetValue(KeyName, v + i);
        }
        return group.GetValue<decimal>(KeyName);
    }

    [Benchmark(Description = "SetSum (decimal) — after (AddDecimalValue)")]
    public decimal SetSumDecimal_After()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        for (var i = 0; i < Iterations; i++)
            group.AddDecimalValue(KeyName, i);
        return group.GetValue<decimal>(KeyName);
    }

    // ─── Count (int increment) ────────────────────────────────────────────────

    [Benchmark(Description = "SetCount (int) — before (GetOrCreate+SetValue)")]
    public int SetCountInt_Before()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        for (var i = 0; i < Iterations; i++)
        {
            var v = group.GetOrCreateValue<int>(KeyName);
            group.SetValue(KeyName, v + 1);
        }
        return group.GetValue<int>(KeyName);
    }

    [Benchmark(Description = "SetCount (int) — after (IncrementIntValue)")]
    public int SetCountInt_After()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        for (var i = 0; i < Iterations; i++)
            group.IncrementIntValue(KeyName);
        return group.GetValue<int>(KeyName);
    }

    // ─── Max (decimal conditional update) ────────────────────────────────────

    [Benchmark(Description = "SetMax (decimal) — before (GetOrCreate+SetValue)")]
    public decimal SetMaxDecimal_Before()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        for (var i = 0; i < Iterations; i++)
        {
            var storedValue = group.GetOrCreateValue(KeyName, decimal.MinValue);
            if (storedValue < i)
                group.SetValue(KeyName, (decimal)i);
        }
        return group.GetValue<decimal>(KeyName);
    }

    [Benchmark(Description = "SetMax (decimal) — after (UpdateDecimalIfGreater)")]
    public decimal SetMaxDecimal_After()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        for (var i = 0; i < Iterations; i++)
            group.UpdateDecimalIfGreater(KeyName, i, decimal.MinValue);
        return group.GetValue<decimal>(KeyName);
    }

    // ─── Min (decimal conditional update) ────────────────────────────────────

    [Benchmark(Description = "SetMin (decimal) — before (GetOrCreate+SetValue)")]
    public decimal SetMinDecimal_Before()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        for (var i = Iterations; i > 0; i--)
        {
            var storedValue = group.GetOrCreateValue(KeyName, decimal.MaxValue);
            if (storedValue > i)
                group.SetValue(KeyName, (decimal)i);
        }
        return group.GetValue<decimal>(KeyName);
    }

    [Benchmark(Description = "SetMin (decimal) — after (UpdateDecimalIfLess)")]
    public decimal SetMinDecimal_After()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        for (var i = Iterations; i > 0; i--)
            group.UpdateDecimalIfLess(KeyName, i, decimal.MaxValue);
        return group.GetValue<decimal>(KeyName);
    }
}