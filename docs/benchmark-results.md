# Query Evaluator Performance Benchmark Results

This document captures benchmark results from two rounds of optimisations.

## Environment

```
BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45 GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.102  |  Runtime: .NET 8.0.23 (X64 RyuJIT x86-64-v3)
Job: ShortRun  (WarmupCount=3, IterationCount=3, LaunchCount=1)
```

---

## Round 1 — Group dictionary-access hot paths (`GroupOperationsBenchmark`)

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
value had already been retrieved by a preceding `TryGetValue` call.

---

## Round 1 — `GroupKey` hash quality and dictionary throughput (`GroupKeyHashBenchmark`)

| Scenario | Before | After | Δ |
|---|---|---|---|
| Raw `GetHashCode` throughput | 101.24 µs | 99.76 µs | −1 % (neutral) |
| Dictionary GROUP BY — normal keys | 648 µs | 680 µs | ~neutral (within noise) |
| Dictionary GROUP BY — permutation keys | 1 576 µs | 1 448 µs | **−8 %** |

**Permutation-pair hash collisions:** 5 / 10 (additive) → **0 / 10** (polynomial).

---

## Round 2 — Aggregation set methods (`AggregationSetBenchmark`)

Each benchmark performs **10 000 iterations**. "Before" uses the two-step
`GetOrCreateValue + SetValue` pattern (two dictionary lookups). "After" uses
the new single-lookup `CollectionsMarshal.GetValueRefOrAddDefault` methods.

| Method | Mean (before) | Mean (after) | Δ |
|---|---|---|---|
| `SetSum` (decimal accumulation) | 320.8 µs | 221.9 µs | **−31 %** |
| `SetCount` (int increment) | 269.2 µs | 161.7 µs | **−40 %** |
| `SetMax` (decimal conditional max) | 313.0 µs | 220.1 µs | **−30 %** |
| `SetMin` (decimal conditional min) | 311.8 µs | 220.1 µs | **−29 %** |

**Root cause:** every aggregation row previously required two separate
dictionary lookups — `GetOrCreateValue` (TryGetValue + optional Add) and then
`SetValue` (indexer = second hash lookup + write).  `CollectionsMarshal.GetValueRefOrAddDefault`
gives a `ref` to the exact slot, so a single lookup both reads the old value
and allows writing the new one through the reference.  This also required
changing `Group.Values` / `Group.Converters` from `IDictionary<>` to concrete
`Dictionary<>` (a prerequisite for `CollectionsMarshal`), which additionally
enables JIT devirtualization of all dictionary operations.

---

## Round 2 — SQL `LIKE` operator (`LikeBenchmark`)

Benchmarks perform **10 000 calls** per measurement. "Regex baseline"
re-implements the original compiled-Regex path inline.

### Operator micro-benchmarks

| Pattern | Regex baseline | Fast path (string method) | Δ |
|---|---|---|---|
| `%suffix` (`EndsWith`) | 464.0 µs | 171.2 µs | **−63 %** |
| `prefix%` (`StartsWith`) | 435.2 µs | 177.6 µs | **−59 %** |
| `%middle%` (`Contains`) | 484.3 µs | 220.0 µs | **−55 %** |
| `exact` (no wildcards, `Equals`) | 434.0 µs | 121.5 µs | **−72 %** |

### End-to-end query benchmarks (9 453 profile rows)

| Query | Mean |
|---|---|
| `WHERE Email LIKE '%.com'` | 1 618 µs |
| `WHERE Email LIKE 'a%'` | 1 080 µs |
| `WHERE Email LIKE '%john%'` | 1 013 µs |

**Root cause:** compiled `Regex` objects carry significant per-call overhead
even after compilation.  For the four most common SQL LIKE shapes
(`%suffix`, `prefix%`, `%middle%`, exact match) the optimised code detects
the shape at first use and caches a `Func<string, bool>` that calls
`string.EndsWith` / `StartsWith` / `Contains` / `Equals` with
`OrdinalIgnoreCase`.  Patterns with non-ASCII characters or `_` wildcards
fall through to the original compiled-Regex path unchanged.

---

## Round 1+2 combined — End-to-end GROUP BY / DISTINCT (`DistinctBenchmark`, 9 453 rows)

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

---

## Summary of all changes validated by these benchmarks

| File | Change | Measured benefit |
|---|---|---|
| `Musoq.Plugins/Group.cs` | `IDictionary<>` → `Dictionary<>` for `Values`/`Converters` | Devirtualizes all dictionary calls; prerequisite for `CollectionsMarshal` |
| `Musoq.Plugins/Group.cs` | `GetValue`: eliminated redundant final indexer lookup | −48 % per call |
| `Musoq.Plugins/Group.cs` | `GetOrCreateValue(T)`: `TryGetValue+Add+return` instead of `ContainsKey+Add+indexer` | −39 % per call |
| `Musoq.Plugins/Group.cs` | `GetOrCreateValue(Func<T>)`: same TryGetValue pattern | −42 % per call |
| `Musoq.Plugins/Group.cs` | `GetOrCreateValueWithConverter`: `TryGetValue` for both dicts | −42 % per call |
| `Musoq.Plugins/Group.cs` | New `AddDecimalValue` via `CollectionsMarshal` | −31 % vs SetSum's GetOrCreate+SetValue |
| `Musoq.Plugins/Group.cs` | New `IncrementIntValue` via `CollectionsMarshal` | −40 % vs SetCount's GetOrCreate+SetValue |
| `Musoq.Plugins/Group.cs` | New `UpdateDecimalIfGreater/Less` via `CollectionsMarshal` | −29–30 % vs SetMax/Min's GetOrCreate+SetValue |
| `Musoq.Plugins/LibraryBaseSum.cs` | `SetSum(decimal)` → `AddDecimalValue` | 1 dict lookup saved per row per SUM column |
| `Musoq.Plugins/LibraryBaseMax.cs` | `SetMax(decimal)` → `UpdateDecimalIfGreater` | 1 dict lookup saved per row per MAX column |
| `Musoq.Plugins/LibraryBaseMin.cs` | `SetMin(decimal)` → `UpdateDecimalIfLess` | 1 dict lookup saved per row per MIN column |
| `Musoq.Plugins/LibraryBaseCount.cs` | All `SetCount` overloads → `IncrementIntValue` | 1 dict lookup saved per row per COUNT column |
| `Musoq.Evaluator/Tables/GroupKey.cs` | Polynomial hash + `IEquatable<GroupKey>` | Eliminates permutation collisions; −8 % on permutation-heavy GROUP BY |
| `Musoq.Evaluator/Tables/GroupKey.cs` | `override Equals(object)` → delegates to typed `Equals(GroupKey)` | Restores dictionary throughput to parity |
| `Musoq.Evaluator/Tables/IndexedList.cs` | `HasIndex`: O(n) scan → O(1) `ContainsKey` | Eliminates linear scan |
| `Musoq.Evaluator/Operators.cs` | `Like`: fast string methods for `%suffix`, `prefix%`, `%middle%`, exact patterns | −55 to −72 % per LIKE call for ASCII patterns |

---

*Reproduce with:*
```bash
dotnet run --project src/dotnet/Musoq.Benchmarks --configuration Release \
  -- --filter "*GroupOperationsBenchmark*|*GroupKeyHashBenchmark*|*AggregationSetBenchmark*|*LikeBenchmark*|*DistinctBenchmark*" \
  --job short --memory
```
