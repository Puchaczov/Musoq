# Musoq Benchmarks

## Correlated Subquery Performance Gate

Capture three isolated `SubqueryLoweringBenchmark` reports before and after a change. Compare them with:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- compare-reports `
  --baseline <before-1.json> --baseline <before-2.json> --baseline <before-3.json> `
  --current <after-1.json> --current <after-2.json> --current <after-3.json>
```

The command compares the median BenchmarkDotNet mean and allocation count for every baseline method,
rejects incomplete reports, and fails when either metric regresses by more than 3%. Override the ratios
only for a separately documented equivalence gate, such as the 1.10 limit for a new correlated rewrite
against its hand-written set-based equivalent.

The typed composite range-partition path was gated with three isolated `ShortRun` reports against the
Wave 9 object-key implementation, using the same `PredicateCompositeRangeMark` harness in both cohorts:

| Cohort | Median mean | Median allocation | Ratio vs. Wave 9 |
| --- | ---: | ---: | ---: |
| Wave 9 object composite key (`9540ebd71`) | 6.092 ms | 3.64 MB | 1.0000x / 1.0000x |
| Typed nullable tuple key (`d85ac3fae`) | 5.071 ms | 2.83 MB | 0.8325x / 0.7765x |

The repository comparator passed its default 1.03 time and allocation ceilings. These local numbers were
captured on .NET 10.0.10 and should be treated as change evidence, not as a cross-machine throughput claim.

## Two-Mode Execution Baselines

`TwoModeExecutionBenchmark` protects the typed enumerable work from silent performance drift. It measures table execution, public typed execution versus equivalent LINQ, compile-from-scratch versus reusable typed queries, caller-owned artifact load/run paths, table-backed typed profiling, serial versus parallel typed final projection, typed fallback materialization for `distinct/order/skip/take`, and the typed post-operation row helper path for `distinct/order/skip/take`.

Run it with:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release -- --filter "*TwoModeExecutionBenchmark*" --job short --memory
```

These benchmarks are characterization only; unit tests must not assert timing thresholds.

## Streaming Chunk-Parallel Projection

`StreamingChunkParallelProjectionBenchmark` compares the old serial streaming projection path
(`ParallelizationMode.None`) with chunk-parallel streaming projection (`ParallelizationMode.Full`)
for a CPU-heavy row-local filter/project query over `RowSourceBase<T>` chunks.

Run it with:

```powershell
dotnet run -c Release --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj --filter "*StreamingChunkParallelProjectionBenchmark*"
```

Interpret the `ChunkParallelStreamingProjection` ratio against `SerialStreamingProjection`.
For CPU-heavy rows, ratios below `0.50` indicate a 2x+ speedup; local short-run results showed
roughly `0.35-0.40` at MaxDegree 4 and `0.28-0.32` at MaxDegree 8, with allocation ratios below
the serial baseline.

## RuntimeV2 Weather Grouped Aggregate

`WeatherAggregateBenchmark` measures the generated code for:

```sql
select City, Min(Temperature::Single), Max(Temperature::Single), Avg(Temperature::Single)
from #weather.measurements()
group by City
```

Run it with:

```powershell
dotnet run -c Release --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -- --filter "*WeatherAggregateBenchmark*" --join
```

Local ShortRun result from 2026-07-05 on .NET 10.0.9, comparing original `HEAD`
(`73ff96a4d` plus this benchmark harness) with the chunk-native aggregate and typed cast changes:

| Rows | Chunk | Mode | Before mean | Before alloc | After mean | After alloc |
| ---: | ---: | --- | ---: | ---: | ---: | ---: |
| 1000000 | 512 | Serial | 17.580 ms | 31.63 MB | 14.742 ms | 8.74 MB |
| 1000000 | 512 | Parallel | 17.694 ms | 32.87 MB | 8.831 ms | 9.92 MB |
| 1000000 | 4096 | Serial | 17.069 ms | 30.94 MB | 15.219 ms | 8.05 MB |
| 1000000 | 4096 | Parallel | 16.979 ms | 32.17 MB | 5.409 ms | 9.30 MB |

The serial allocation drop comes mostly from avoiding boxed `Temperature::Single` casts. The
parallel speedup comes from consuming source chunks directly with thread-local aggregate groups
instead of first bridging chunked input into a retained row list.
