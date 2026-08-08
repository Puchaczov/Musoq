# Musoq Benchmarks

## Recursive CTE performance gate

`RecursiveCteBenchmark` compares generated recursive execution with an equivalent typed handwritten semi-naive loop across chain, tree, diamond, cycle, duplicate-heavy keyed, wide-row, invariant-snapshot, indexed-edge, correlated-apply, and empty-anchor cases. Every scenario runs with both `ParallelizationMode.None` and `ParallelizationMode.Full`; the recursive fixed-point loop remains sequential in both modes. Schema metadata is reused by both operations, while source enumeration and recursive snapshots occur for each operation.

The release gate has six tiers:

- Sequential generated-versus-handwritten equivalence for chain, tree, diamond, cycle, duplicate-heavy keyed, wide rows, invariant snapshots, and indexed edges: `1.25x` time and `1.20x` allocation ceilings.
- Full-mode before/after regression for the same eight scenarios: `1.03x` ceilings.
- Separate correlated-apply and empty-anchor before/after overhead gates: `1.03x` ceilings.
- Recursive compilation before/after regression for chain, wide rows, and indexed edges in both modes: `1.03x` ceilings.
- Existing ordinary `CteSidecarIndexBenchmark.SingleHash*` before/after regression: `1.03x` ceilings.

Capture three isolated runtime reports at the baseline commit and three at the candidate commit. Each run takes several minutes because BenchmarkDotNet isolates 40 parameterized operations:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release -- `
  --filter "*RecursiveCteBenchmark*" --exporters json `
  --artifacts artifacts/recursive-cte-benchmark/baseline-runtime-1
```

At both commits also capture three compilation reports and three focused ordinary-CTE reports:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release -- `
  --filter "*RecursiveCteCompilationBenchmark*" --exporters json `
  --artifacts artifacts/recursive-cte-benchmark/baseline-compilation-1

dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release -- `
  --filter "*CteSidecarIndexBenchmark.SingleHash*" --exporters json `
  --artifacts artifacts/recursive-cte-benchmark/baseline-ordinary-1
```

Repeat each command for samples 2 and 3, then repeat all three cohorts at the candidate commit under `current-*` artifact directories. Apply the complete release gate:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release --no-build -- `
  gate-recursive `
  --baseline-runtime <baseline-runtime-1.json> --baseline-runtime <baseline-runtime-2.json> --baseline-runtime <baseline-runtime-3.json> `
  --current-runtime <current-runtime-1.json> --current-runtime <current-runtime-2.json> --current-runtime <current-runtime-3.json> `
  --baseline-compilation <baseline-compilation-1.json> --baseline-compilation <baseline-compilation-2.json> --baseline-compilation <baseline-compilation-3.json> `
  --current-compilation <current-compilation-1.json> --current-compilation <current-compilation-2.json> --current-compilation <current-compilation-3.json> `
  --baseline-ordinary <baseline-ordinary-1.json> --baseline-ordinary <baseline-ordinary-2.json> --baseline-ordinary <baseline-ordinary-3.json> `
  --current-ordinary <current-ordinary-1.json> --current-ordinary <current-ordinary-2.json> --current-ordinary <current-ordinary-3.json>
```

For a fast equivalence-only check, pass three current runtime reports with `--report`. The gate rejects partial cohorts, inconsistent method sets, missing mode/scenario combinations, and filters that select no methods.

On 2026-07-21 with .NET 10.0.10, the expanded three-cohort sequential equivalence gate measured these median ratios:

| Scenario | Time | Allocation |
| --- | ---: | ---: |
| Chain | 0.6282x | 0.6293x |
| Tree | 0.7056x | 0.7111x |
| Diamond | 0.7050x | 0.7340x |
| Cycle | 0.7104x | 0.6750x |
| DuplicateHeavyKeyed | 0.8459x | 0.8047x |
| WideRows | 0.4569x | 0.4835x |
| InvariantSnapshot | 0.9489x | 0.6257x |
| IndexedEdges | 0.6856x | 0.6751x |

The Wave 7 gate-only change used the same unchanged runtime cohort as both sides to validate all before/after selectors; every full-mode, overhead, compilation, and ordinary-CTE comparison reported `1.0000x`. A release decision must instead use distinct baseline and candidate cohorts. Compilation characterization medians for the six cases were approximately 69–99 ms and 6.3–8.3 MB per fresh compile.

These are local change evidence, not cross-machine throughput claims. Do not commit raw BenchmarkDotNet artifacts; store only commands and summarized median ratios.

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

## TableSymbol schema-binding transitions

`TableSymbolTransformationBenchmark` protects the metadata hot paths used when a
provider relation crosses nullable, ordinality, and multi-alias transitions. It
covers single-alias lookup, nullable and ordinality transformations, and
two/eight-alias merge-and-resolve operations. The benchmark source uses only APIs
available at the baseline commit, so copy that exact benchmark file into a clean
baseline worktree before collecting the baseline cohort.

Capture three isolated cohorts on each side with the same `ShortRun` settings:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release --no-build -- `
  --filter "*TableSymbolTransformationBenchmark*" --job short --memory --exporters json `
  --artifacts artifacts/table-symbol-wave4/baseline-1

dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release --no-build -- `
  --filter "*TableSymbolTransformationBenchmark*" --job short --memory --exporters json `
  --artifacts artifacts/table-symbol-wave4/current-1
```

Repeat each command for cohorts `2` and `3`, then compare the six compressed
JSON reports:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release --no-build -- compare-reports `
  --baseline artifacts/table-symbol-wave4/baseline-1/results/Musoq.Benchmarks.TableSymbolTransformationBenchmark-report-full-compressed.json `
  --baseline artifacts/table-symbol-wave4/baseline-2/results/Musoq.Benchmarks.TableSymbolTransformationBenchmark-report-full-compressed.json `
  --baseline artifacts/table-symbol-wave4/baseline-3/results/Musoq.Benchmarks.TableSymbolTransformationBenchmark-report-full-compressed.json `
  --current artifacts/table-symbol-wave4/current-1/results/Musoq.Benchmarks.TableSymbolTransformationBenchmark-report-full-compressed.json `
  --current artifacts/table-symbol-wave4/current-2/results/Musoq.Benchmarks.TableSymbolTransformationBenchmark-report-full-compressed.json `
  --current artifacts/table-symbol-wave4/current-3/results/Musoq.Benchmarks.TableSymbolTransformationBenchmark-report-full-compressed.json
```

The comparator uses the median of the three reports and rejects any method above
`1.03x` for either mean time or allocated bytes. Raw BenchmarkDotNet reports are
local ignored artifacts and MUST NOT be committed. The Wave 4 cohort passed with
ratios from `0.8018x` to `1.0000x` for time and `0.7274x` to `1.0000x` for
allocation.

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
