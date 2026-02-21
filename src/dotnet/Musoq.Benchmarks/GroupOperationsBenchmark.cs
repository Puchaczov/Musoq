using BenchmarkDotNet.Attributes;
using Musoq.Plugins;

namespace Musoq.Benchmarks;

/// <summary>
///     Micro-benchmarks for the Group hot paths changed in the performance optimisation.
///     Each "Before" variant re-implements the original code in-line so both measurements
///     run inside the same benchmark binary, giving an apples-to-apples comparison.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class GroupOperationsBenchmark
{
    private const int Iterations = 10_000;
    private const string KeyName = "sum";

    // -------------------------------------------------------------------------
    // GetOrCreateValue — default-value overload
    // -------------------------------------------------------------------------

    [Benchmark(Description = "GetOrCreateValue(default) — before")]
    public decimal GetOrCreateValueDefault_Before()
    {
        var values = new Dictionary<string, object?>();
        decimal accumulator = 0;

        for (var i = 0; i < Iterations; i++)
        {
            // Original: ContainsKey check → Add → indexer lookup
            if (!values.ContainsKey(KeyName))
                values.Add(KeyName, default(decimal));
            var value = (decimal?)values[KeyName];
            accumulator += value ?? 0;
            values[KeyName] = accumulator;
        }

        return accumulator;
    }

    [Benchmark(Description = "GetOrCreateValue(default) — after")]
    public decimal GetOrCreateValueDefault_After()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        decimal accumulator = 0;

        for (var i = 0; i < Iterations; i++)
        {
            var value = group.GetOrCreateValue<decimal>(KeyName);
            accumulator += value;
            group.SetValue(KeyName, accumulator);
        }

        return accumulator;
    }

    // -------------------------------------------------------------------------
    // GetOrCreateValue — factory-delegate overload
    // -------------------------------------------------------------------------

    [Benchmark(Description = "GetOrCreateValue(factory) — before")]
    public int GetOrCreateValueFactory_Before()
    {
        var values = new Dictionary<string, object?>();
        var result = 0;

        for (var i = 0; i < Iterations; i++)
        {
            if (!values.ContainsKey(KeyName))
                values.Add(KeyName, new List<int>());
            var list = (List<int>?)values[KeyName]!;
            list!.Add(i);
            result = list.Count;
        }

        return result;
    }

    [Benchmark(Description = "GetOrCreateValue(factory) — after")]
    public int GetOrCreateValueFactory_After()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());

        for (var i = 0; i < Iterations; i++)
        {
            var list = group.GetOrCreateValue(KeyName, () => new List<int>());
            list!.Add(i);
        }

        return group.GetOrCreateValue<List<int>>(KeyName)!.Count;
    }

    // -------------------------------------------------------------------------
    // GetValue
    // -------------------------------------------------------------------------

    [Benchmark(Description = "GetValue — before")]
    public decimal GetValue_Before()
    {
        var values = new Dictionary<string, object?> { [KeyName] = (decimal)42 };
        var converters = new Dictionary<string, Func<object?, object?>>();

        decimal sum = 0;
        for (var i = 0; i < Iterations; i++)
        {
            // Original: TryGetValue → TryGetValue for converter → Values[name] indexer (redundant)
            if (!values.TryGetValue(KeyName, out _))
                throw new KeyNotFoundException();

            if (!converters.TryGetValue(KeyName, out _))
                sum += (decimal?)values[KeyName] ?? 0; // redundant lookup
        }

        return sum;
    }

    [Benchmark(Description = "GetValue — after")]
    public decimal GetValue_After()
    {
        var group = new Group(null, new[] { KeyName }, new object[] { (decimal)42 });

        decimal sum = 0;
        for (var i = 0; i < Iterations; i++)
            sum += group.GetValue<decimal>(KeyName);

        return sum;
    }

    // -------------------------------------------------------------------------
    // GetOrCreateValueWithConverter
    // -------------------------------------------------------------------------

    [Benchmark(Description = "GetOrCreateValueWithConverter — before")]
    public int GetOrCreateValueWithConverter_Before()
    {
        var values = new Dictionary<string, object?>();
        var converters = new Dictionary<string, Func<object?, object?>>();
        Func<object?, object?> converter = v => v is int n ? n * 2 : 0;

        var result = 0;
        for (var i = 0; i < Iterations; i++)
        {
            if (!values.ContainsKey(KeyName))
                values.Add(KeyName, i);
            // Original: TryAdd + two indexer lookups
            converters.TryAdd(KeyName, converter);
            result = (int)(converters[KeyName](values[KeyName]) ?? 0);
        }

        return result;
    }

    [Benchmark(Description = "GetOrCreateValueWithConverter — after")]
    public int GetOrCreateValueWithConverter_After()
    {
        var group = new Group(null, Array.Empty<string>(), Array.Empty<object>());
        Func<object?, object?> converter = v => v is int n ? n * 2 : 0;

        var result = 0;
        for (var i = 0; i < Iterations; i++)
            result = group.GetOrCreateValueWithConverter<int, int>(KeyName, i, converter);

        return result;
    }
}
