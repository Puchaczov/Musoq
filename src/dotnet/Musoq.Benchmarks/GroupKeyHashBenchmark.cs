using BenchmarkDotNet.Attributes;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

/// <summary>
///     Benchmarks for GroupKey.GetHashCode and dictionary lookup throughput,
///     comparing the old additive hash with the new polynomial hash.
///     <para>
///         OldGroupKey re-implements the original additive algorithm inline so both
///         variants run inside the same binary without reverting commits, giving a
///         strictly fair comparison.
///     </para>
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class GroupKeyHashBenchmark
{
    // 10 000 keys with 2-element payloads (string + int) — simulates GROUP BY Gender, Count
    private static readonly GroupKey[] TwoFieldKeys =
        Enumerable.Range(0, 10_000)
            .Select(i => new GroupKey(i % 5 == 0 ? "Male" : "Female", i % 50))
            .ToArray();

    private static readonly OldGroupKey[] TwoFieldOldKeys =
        Enumerable.Range(0, 10_000)
            .Select(i => new OldGroupKey(i % 5 == 0 ? "Male" : "Female", i % 50))
            .ToArray();

    // Permutation-heavy keys — the worst case for additive hashing.
    // Each pair ("a","b") and ("b","a") are logically distinct but hash to the
    // same bucket under additive hash, forcing extra equality comparisons.
    private static readonly GroupKey[] PermutationKeys =
        Enumerable.Range(0, 10_000)
            .SelectMany(i => new[]
            {
                new GroupKey($"key{i % 50}", i % 100),
                new GroupKey(i % 100, $"key{i % 50}") // swapped — additive collision
            })
            .ToArray();

    private static readonly OldGroupKey[] PermutationOldKeys =
        Enumerable.Range(0, 10_000)
            .SelectMany(i => new[]
            {
                new OldGroupKey($"key{i % 50}", i % 100),
                new OldGroupKey(i % 100, $"key{i % 50}")
            })
            .ToArray();

    // -------------------------------------------------------------------------
    // Raw hash computation (pure throughput, no dictionary)
    // -------------------------------------------------------------------------

    [Benchmark(Description = "GetHashCode (2-field) — before (additive)")]
    public int HashCode_TwoField_Before()
    {
        var sum = 0;
        foreach (var key in TwoFieldOldKeys)
            sum += key.GetHashCode();
        return sum;
    }

    [Benchmark(Description = "GetHashCode (2-field) — after (polynomial)")]
    public int HashCode_TwoField_After()
    {
        var sum = 0;
        foreach (var key in TwoFieldKeys)
            sum += key.GetHashCode();
        return sum;
    }

    // -------------------------------------------------------------------------
    // Dictionary GROUP BY simulation — regular keys, no permutation collisions
    // -------------------------------------------------------------------------

    [Benchmark(Description = "Dictionary GROUP BY (normal keys) — before")]
    public int DictionaryGroupBy_Normal_Before()
    {
        var dict = new Dictionary<OldGroupKey, int>(TwoFieldOldKeys.Length);
        foreach (var key in TwoFieldOldKeys)
        {
            if (!dict.TryGetValue(key, out var count))
                count = 0;
            dict[key] = count + 1;
        }

        return dict.Count;
    }

    [Benchmark(Description = "Dictionary GROUP BY (normal keys) — after")]
    public int DictionaryGroupBy_Normal_After()
    {
        var dict = new Dictionary<GroupKey, int>(TwoFieldKeys.Length);
        foreach (var key in TwoFieldKeys)
        {
            if (!dict.TryGetValue(key, out var count))
                count = 0;
            dict[key] = count + 1;
        }

        return dict.Count;
    }

    // -------------------------------------------------------------------------
    // Dictionary GROUP BY simulation — permutation-heavy keys
    // additive hash collapses ("a","b") and ("b","a") into the same bucket,
    // forcing O(n) equality scans; polynomial hash keeps them in different buckets.
    // -------------------------------------------------------------------------

    [Benchmark(Description = "Dictionary GROUP BY (permutation keys) — before")]
    public int DictionaryGroupBy_Permutation_Before()
    {
        var dict = new Dictionary<OldGroupKey, int>(PermutationOldKeys.Length);
        foreach (var key in PermutationOldKeys)
        {
            if (!dict.TryGetValue(key, out var count))
                count = 0;
            dict[key] = count + 1;
        }

        return dict.Count;
    }

    [Benchmark(Description = "Dictionary GROUP BY (permutation keys) — after")]
    public int DictionaryGroupBy_Permutation_After()
    {
        var dict = new Dictionary<GroupKey, int>(PermutationKeys.Length);
        foreach (var key in PermutationKeys)
        {
            if (!dict.TryGetValue(key, out var count))
                count = 0;
            dict[key] = count + 1;
        }

        return dict.Count;
    }

    // -------------------------------------------------------------------------
    // OldGroupKey — identical to GroupKey except GetHashCode uses additive hash
    // -------------------------------------------------------------------------

    private sealed class OldGroupKey(params object[] values) : IEquatable<OldGroupKey>
    {
        private readonly object[] _values = values;

        public bool Equals(OldGroupKey? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (_values.Length != other._values.Length) return false;
            for (var i = 0; i < _values.Length; i++)
            {
                var a = _values[i];
                var b = other._values[i];
                if (a == null && b == null) continue;
                if (a == null || b == null) return false;
                if (!a.Equals(b)) return false;
            }

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as OldGroupKey);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 0;
                foreach (var val in _values)
                    if (val != null)
                        hash += val.GetHashCode();
                return hash;
            }
        }
    }
}

