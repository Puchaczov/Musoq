using System.Diagnostics;
using Musoq.Evaluator.Helpers;

namespace Musoq.Playground.Brc;

// Isolates the per-row column access cost that dominates Musoq's generated
// aggregation loop. The generated code reads each column with
// EvaluationHelper.GetNestedValue(row, "Column") where row is typed `object`,
// which performs a ConcurrentDictionary lookup, type checks, a delegate call and
// boxing/unboxing per column per row. This micro-experiment compares that against
// direct typed field access, the theoretical best the engine could emit when the
// source CLR type is known.

internal struct AggState
{
    public bool HasValue;
    public double Min;
    public double Max;
    public double Sum;
    public long Count;

    public void Add(double v)
    {
        if (!HasValue)
        {
            Min = v;
            Max = v;
            HasValue = true;
        }
        else
        {
            if (v < Min) Min = v;
            if (v > Max) Max = v;
        }

        Sum += v;
        Count++;
    }
}

internal static class BrcMicro
{
    public static void Run()
    {
        const int rowCount = 20_000_000;
        const int stationCount = 413;

        Console.WriteLine("=== BRC per-row access micro-experiment ===");
        Console.WriteLine($"Cores: {Environment.ProcessorCount}, Rows: {rowCount:N0}, Stations: {stationCount}");
        Console.WriteLine();

        var stations = new string[stationCount];
        for (var i = 0; i < stationCount; i++)
            stations[i] = $"Station_{i:D4}";

        var random = new Random(12345);
        var rows = new object[rowCount];
        for (var i = 0; i < rowCount; i++)
        {
            var station = stations[random.Next(stationCount)];
            var temperature = Math.Round(random.NextDouble() * 199.8 - 99.9, 1);
            rows[i] = new BrcEntity(station, temperature);
        }

        // Warm up the cached reflection accessor and JIT.
        _ = EvaluationHelper.GetNestedValue(rows[0], "Temperature");
        Reflected(rows.AsSpan(0, 1000));
        Typed(rows.AsSpan(0, 1000));

        Console.WriteLine($"{"Path",-28} {"Time(ms)",10} {"Rows/sec",15}");
        Measure("Reflection access (current)", () => Reflected(rows), rowCount);
        Measure("Typed access (best serial)", () => Typed(rows), rowCount);
        Measure("Typed access (parallel)", () => TypedParallel(rows), rowCount);
    }

    private static void Measure(string label, Func<int> action, int rowCount)
    {
        var sw = Stopwatch.StartNew();
        var groups = action();
        sw.Stop();
        var rowsPerSec = rowCount / (sw.Elapsed.TotalMilliseconds / 1000.0);
        Console.WriteLine($"{label,-28} {sw.Elapsed.TotalMilliseconds,10:N1} {rowsPerSec,15:N0}  (groups={groups})");
    }

    private static int Reflected(ReadOnlySpan<object> rows)
    {
        var groups = new Dictionary<string, AggState>();
        foreach (var row in rows)
        {
            var key = (string)EvaluationHelper.GetNestedValue(row, "Station")!;
            var temperature = (double)EvaluationHelper.GetNestedValue(row, "Temperature")!;
            ref var state = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, key, out _);
            state.Add(temperature);
        }

        return groups.Count;
    }

    private static int Typed(ReadOnlySpan<object> rows)
    {
        var groups = new Dictionary<string, AggState>();
        foreach (var row in rows)
        {
            var entity = (BrcEntity)row;
            ref var state = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, entity.Station, out _);
            state.Add(entity.Temperature);
        }

        return groups.Count;
    }

    private static int TypedParallel(object[] rows)
    {
        var workerCount = Environment.ProcessorCount;
        var shards = new Dictionary<string, AggState>[workerCount];
        Parallel.For(0, workerCount, shardIndex =>
        {
            var start = (long)rows.Length * shardIndex / workerCount;
            var end = (long)rows.Length * (shardIndex + 1) / workerCount;
            var shard = new Dictionary<string, AggState>();
            for (var i = start; i < end; i++)
            {
                var entity = (BrcEntity)rows[i];
                ref var state = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(shard, entity.Station, out _);
                state.Add(entity.Temperature);
            }

            shards[shardIndex] = shard;
        });

        var merged = shards[0];
        for (var s = 1; s < shards.Length; s++)
        foreach (var kvp in shards[s])
        {
            ref var state = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(merged, kvp.Key, out var exists);
            if (!exists)
            {
                state = kvp.Value;
            }
            else
            {
                state.Sum += kvp.Value.Sum;
                state.Count += kvp.Value.Count;
                if (kvp.Value.Min < state.Min) state.Min = kvp.Value.Min;
                if (kvp.Value.Max > state.Max) state.Max = kvp.Value.Max;
            }
        }

        return merged.Count;
    }
}
