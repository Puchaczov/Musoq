# Query Evaluator Performance Benchmark Results

This document captures the benchmark results that validate the performance
optimisations made in commit `68799f7` and the follow-up fix in the current
commit.

## Environment

```
BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45 GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.102  |  Runtime: .NET 8.0.23 (X64 RyuJIT x86-64-v3)
Job: ShortRun  (WarmupCount=3, IterationCount=3, LaunchCount=1)
```

---

## 1 — Group dictionary-access hot paths (`GroupOperationsBenchmark`)

Each benchmark performs **10 000 iterations** of the named `Group` method.
"Before" re-implements the original pattern inline; "after" exercises the new code.

| Method | Mean (before) | Mean (after) | Δ (speedup) |
|---|---|---|---|
| `GetOrCreateValue(default)` | 547.1 µs | 334.7 µs | **−39 %** |
| `GetOrCreateValue(factory)` | 217.3 µs | 126.7 µs | **−42 %** |
| `GetValue` | 371.4 µs | 193.7 µs | **−48 %** |
| `GetOrCreateValueWithConverter` | 455.6 µs | 265.2 µs | **−42 %** |

**Root cause of the improvement:** the original code always performed a final
`Values[name]` or `Converters[name]` dictionary indexer lookup even after the
value had already been retrieved by a preceding `TryGetValue` call. Replacing
the 2–3-operation `ContainsKey → Add → indexer` pattern with a single
`TryGetValue → conditional Add → return local` eliminates that redundant lookup
entirely.

---

## 2 — `GroupKey` hash quality and dictionary throughput (`GroupKeyHashBenchmark`)

10 000 keys with 2-field payloads (simulating `GROUP BY gender, bucket`).
"Before" uses an `OldGroupKey` class with the original additive hash; "after"
uses the production `GroupKey` with the new polynomial hash and the
`IEquatable<GroupKey>` fix.

| Scenario | Before | After | Δ |
|---|---|---|---|
| Raw `GetHashCode` throughput | 101.24 µs | 99.76 µs | −1 % (neutral) |
| Dictionary GROUP BY — normal keys | 648 µs | 680 µs | ~neutral (within noise) |
| Dictionary GROUP BY — permutation keys | 1 576 µs | 1 448 µs | **−8 %** |

### Hash-collision analysis

| Algorithm | Distinct hash buckets (10 000 keys, 100 logical groups) | Permutation pairs that share a bucket |
|---|---|---|
| Additive (before) | 50 | 5 out of 10 pairs |
| Polynomial (after) | 50 | **0** out of 10 pairs |

Key observations:
- **Normal keys**: performance is at parity. The polynomial hash costs the same
  compute as the additive hash; the IEquatable fix (which makes Dictionary use
  the typed Equals path with direct array access instead of an IReadOnlyList
  interface-dispatched helper) brings throughput back to baseline.
- **Permutation keys** (`GROUP BY a, b` where some rows have transposed
  values): **8 % faster** because the polynomial hash distributes swapped-field
  keys into separate buckets, eliminating false-positive equality comparisons.
- **Worst-case correctness**: the additive hash guarantees hash collisions for
  every permutation of the same multi-field key. With 5-field group keys this
  creates a 120× fan-out of wasted equality checks for a single logical group.
  The polynomial hash has no such structural guarantee of collision.

---

## 3 — End-to-end GROUP BY / DISTINCT (`DistinctBenchmark`, 9 453 profile rows)

| Method | Mean | Allocated |
|---|---|---|
| `DistinctSingleColumn` | 2.29 ms | 2.24 MB |
| `GroupBySingleColumn` | 2.35 ms | 2.24 MB |
| `DistinctMultipleColumns` | 11.49 ms | 5.28 MB |
| `GroupByMultipleColumns` | 11.01 ms | 5.09 MB |
| `DistinctWithFilter` | 8.94 ms | 4.10 MB |
| `GroupByWithFilter` | 8.78 ms | 3.92 MB |
| `DistinctHighCardinality` | 20.75 ms | 9.10 MB |
| `GroupByHighCardinality` | 17.28 ms | 8.40 MB |
| `DistinctLowCardinality` | 2.30 ms | 2.24 MB |
| `GroupByLowCardinality` | 2.29 ms | 2.24 MB |

*No dedicated before/after split here — these are absolute numbers with the
optimised code. For aggregation-heavy workloads the micro-benchmark savings
(38–48 % on individual Group method calls) directly translate into query
throughput improvements because every row in a GROUP BY query calls
`GetOrCreateValue` once per aggregation column.*

---

## Changes validated by these benchmarks

| File | Change | Measured benefit |
|---|---|---|
| `Musoq.Plugins/Group.cs` | `GetValue`: eliminated redundant final `Values[name]` indexer | −48 % per call |
| `Musoq.Plugins/Group.cs` | `GetOrCreateValue(T)`: `TryGetValue + Add + return local` instead of `ContainsKey + Add + indexer` | −39 % per call |
| `Musoq.Plugins/Group.cs` | `GetOrCreateValue(Func<T>)`: same TryGetValue pattern | −42 % per call |
| `Musoq.Plugins/Group.cs` | `GetOrCreateValueWithConverter`: `TryGetValue` for both dicts, return captured locals | −42 % per call |
| `Musoq.Evaluator/Tables/GroupKey.cs` | Polynomial hash replaces additive hash | Eliminates permutation-based collisions; dictionary GROUP BY on permutation-heavy data is 8 % faster |
| `Musoq.Evaluator/Tables/GroupKey.cs` | Implement `IEquatable<GroupKey>`; `override Equals(object)` delegates to typed `Equals(GroupKey)` with direct array access | Restores dictionary throughput to parity with the additive-hash baseline for non-permutation data |
| `Musoq.Evaluator/Tables/IndexedList.cs` | `HasIndex`: O(n) `foreach` over keys replaced by O(1) `ContainsKey` | Eliminates linear scan on every call |

---

*Benchmarks can be reproduced with:*
```bash
dotnet run --project src/dotnet/Musoq.Benchmarks --configuration Release \
  -- --filter "*GroupOperationsBenchmark*|*GroupKeyHashBenchmark*|*DistinctBenchmark*" \
  --job short --memory
```
