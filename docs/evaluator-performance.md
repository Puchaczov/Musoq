# Musoq.Evaluator performance

This document records the evaluator performance investigation in independently
validated waves. BenchmarkDotNet artifacts are intentionally kept out of source
control; this file stores the commands, environment, summarized medians, and
evidence needed to reproduce the measurements.

## Measurement policy

Every optimization wave must have a clean full-solution Release build and test
run, a wave-specific TRX, three isolated BenchmarkDotNet reports for the focused
benchmark group, and a before/after comparison through the existing
`BenchmarkComparison` command. The default time and allocation regression ceiling
is 1.03x. A benchmark improvement is not accepted if it compromises typed
execution, cache safety, semantics, or maintainability.

The reusable TRX report is run with:

```powershell
powershell -File scripts/report-trx-durations.ps1 `
  -Path TestResults/evaluator-wave-0 -Top 50
```

It reports wall-clock duration, the sum of test durations, the slowest tests, and
the generated-sample, repeated-compilation, integration, and runtime categories.
The generated-sample category includes shared-corpus tests so its one-time lazy
initialization cost is not mistaken for independent test work.

## Wave 0 baseline

Baseline commit: `9967dc690c7c441fc6bd650af13a2aa07f91202f`

Environment:

- Windows 11 Home, build 26200
- Intel Core Ultra 9 285K, 24 cores / 24 logical processors
- .NET SDK `10.0.302` (`10.0.10` runtime)

Build and test commands:

```powershell
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet

dotnet test src/dotnet/Musoq.sln -c Release --no-build --nologo --verbosity quiet `
  --logger "console;verbosity=minimal" `
  --logger "trx" `
  --results-directory "TestResults/evaluator-wave-0"
```

The solution-level TRX logger must not receive one fixed `LogFileName`: VSTest
runs each test project independently and otherwise overwrites the prior project's
file. The default TRX logger preserves one file per project, and the reporter
aggregates the directory.

The evaluator-only diagnostic run before the Wave 0 changes was:

```powershell
dotnet test src/dotnet/Musoq.Evaluator.Tests/Musoq.Evaluator.Tests.csproj -c Release `
  --no-build --no-restore `
  --logger "console;verbosity=detailed" `
  --logger "trx;LogFileName=evaluator-durations.trx" `
  --results-directory "TestResults/diagnosis-evaluator"
```

It recorded 9,439 tests: 9,435 passed and 4 skipped, with a test-run wall time
of approximately 6.56 minutes. The TRX sum of individual durations was 7,342.383
seconds because the test run used parallel workers. The exact diagnostic TRX is
`TestResults/diagnosis-evaluator/evaluator-durations.trx`.

The shared generated-sample corpus explains most apparent 20–30 second entries:
the catalog is generated through one lazy initialization, and other tests wait
for that work. The isolated catalog and chained-sample tests each took roughly
22–23 seconds, while the slower independent runtime tests included the repeated
compilation stress tests, eight-part correlation, and multi-stage window/set
operation cases.

### Existing benchmark baseline

These are short, isolated BenchmarkDotNet measurements captured before runtime
changes on the baseline commit. They are characterization values, not portable
throughput claims.

| Workload | Median time | Median allocation |
| --- | ---: | ---: |
| Simple cold compile | 161 ms | 12.65 MB |
| Complex cold compile | 175 ms | 15.6 MB |
| Simple generated C# only | 54 ms | 2.62 MB |
| Simple emitted DLL only | 133 ms | 12.63 MB |
| Table projection, 5k rows | 154 us | 391 KB |
| Public typed projection, 5k rows | 76 us | 124 KB |
| Private reflected join aggregate, 10k rows | 154 ms | 99 MB |
| CTE typed equivalent, 10k rows | 14 ms | 2.9 MB |

The compilation benchmark's former warm case made two compile calls inside one
operation, so it did not isolate a cache hit. Wave 3 splits that case into
explicit cold, eligible cache-hit, ineligible, artifact, generated-C#, and emit
measurements.

The weather aggregate path is protected as an existing success: one million rows
were approximately 15 ms serial and 5 ms parallel. It is not a first-wave redesign
target.

### Baseline benchmark commands and reports

Each focused cohort is captured three times in separate artifact directories. Raw
JSON and Speedscope files remain under the ignored `BenchmarkDotNet.Artifacts`
directory. The current baseline commands were:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release -- `
  --filter "*CompilationPipelineBenchmark*" --job short --memory --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/evaluator-baseline-compilation-1"

dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release -- `
  --filter "*JoinAggregateProjectionBenchmark*" --job short --memory --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/evaluator-baseline-reflected-join-1"

dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release -- `
  --filter "*TwoModeExecutionBenchmark*" --job short --memory --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/evaluator-baseline-table-1"
```

The same commands were repeated for `-2` and `-3`. The measured short-run
characterization above was cross-checked with the existing
`BenchmarkComparison` infrastructure; comparisons require three baseline and
three current JSON reports and use its 1.03x default ceilings.

The three baseline hotspot reports are:

- `BenchmarkDotNet.Artifacts/evaluator-baseline-reflected-join-1/results/Musoq.Benchmarks.JoinAggregateProjectionBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-baseline-reflected-join-2/results/Musoq.Benchmarks.JoinAggregateProjectionBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-baseline-reflected-join-3/results/Musoq.Benchmarks.JoinAggregateProjectionBenchmark-report-full-compressed.json`

The existing comparator was run with these three reports on both sides as a
measurement-harness self-check. It passed with 1.0000x time and allocation for
all four methods.

The median across the three isolated reports was:

| Benchmark case | Median time | Median allocation |
| --- | ---: | ---: |
| Direct reflected join, 1k rows | 1.869 ms | 1,555.71 KB |
| CTE typed equivalent, 1k rows | 613.526 us | 696.67 KB |
| Direct reflected join, 10k rows | 139.275 ms | 99,186.34 KB |
| CTE typed equivalent, 10k rows | 14.462 ms | 2,899.77 KB |

### Trace evidence

The reflected 10k-row direct join was traced with `dotnet-trace` attached to the
BenchmarkDotNet child process. The captured Speedscope file is intentionally not
tracked. The dominant inclusive path was:

`CompiledQuery.Run` -> generated `ComputeRows` -> `QueryTableEnumerable.AddTo` ->
`QueryRows.AddRowsToTable` -> `EvaluationHelper.GetNestedValue`.

In the trace, `EvaluationHelper.GetNestedValue` was approximately 5,990 ms
inclusive and `Monitor.Enter_Slowpath` approximately 5,946 ms inclusive. This is
the strongest confirmed hotspot and motivates Wave 1's lock-free successful cache
reads. The private benchmark entity is inaccessible to generated C#, so this is a
reflected-access fallback; it is not evidence that the general typed join algorithm
is intrinsically 11x slower.

## Wave records

The sections below are appended as each wave is completed. Each section must name
the commit, test/TRX result, three benchmark report paths (or their summarized
medians), comparison result, profiler evidence, and any disproven hypothesis.

### Wave 0 — measurement harness and hotspot corpus

Status: complete before the Wave 1 runtime changes.

- Four inspectable samples Q227–Q230 are tracked; the catalog and manifest now
  contain 233 samples.
- Full Release build passed with zero warnings and errors.
- The clean full-solution run passed 16,779 tests and skipped 4, for 16,783
  evaluator/solution results; wall clock was 6:30.29 and summed test durations
  were 7,992.875 seconds.
- Per-project TRXs are in `TestResults/evaluator-wave-0-full`; the directory
  report separates 1,334 generated-sample-category results from repeated
  compilation, integration, and runtime results.
- Three isolated hotspot reports and the 1.03x comparator self-check passed.
- The slow-query corpus is measurement-only at this stage; no runtime code was
  changed in Wave 0.

### Wave 1 — reflected accessor cache reads

Status: complete before Wave 2.

Implementation:

- `BoundedRuntimeCache<TKey,TValue>` now publishes immutable dictionary snapshots
  after locked mutation. Successful `TryGetValue` reads use a volatile snapshot
  without entering the mutation lock; factory-at-most-once, insertion-order
  eviction, clear, and bounded ownership remain locked.
- `WeakTypeRuntimeCache<TValue>` keeps `ConditionalWeakTable<Type,...>` ownership
  and uses its safe read path for successful type lookup. Clear publishes a new
  weak table, and mutation/eviction/pruning remain serialized.
- `EvaluationHelper.GetNestedValueAccessor` checks both cache levels before
  `GetOrAdd`. The hot `GetNestedValue` path avoids duplicate accessor validation
  and uses value-pattern checks for dictionary/dynamic/indexer fallback types.
- Added 21 focused tests for concurrent hits and first creation, clear/eviction,
  collectible types, nested CLR properties, dictionaries, dynamic objects,
  indexers, missing members, and throwing getters.

Wave 1 full gate:

- Release build passed with zero warnings and errors.
- Full solution: 16,791 recorded results, 16,787 passed, 4 skipped; wall clock
  6:27.41, summed individual durations 7,920.037 seconds.
- TRXs: `TestResults/evaluator-wave-1-full`; the directory reporter preserved
  per-project files and kept generated-sample initialization separate.

The three final post-Wave 1 hotspot reports are:

- `BenchmarkDotNet.Artifacts/evaluator-current-reflected-join-5/results/Musoq.Benchmarks.JoinAggregateProjectionBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-current-reflected-join-6/results/Musoq.Benchmarks.JoinAggregateProjectionBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-current-reflected-join-7/results/Musoq.Benchmarks.JoinAggregateProjectionBenchmark-report-full-compressed.json`

Compared with the three Wave 0 reports through `BenchmarkComparison`, the median
10k private reflected join is 59.329 ms and 74,614.94 KB versus 139.275 ms and
99,186.34 KB: 0.4244x time and 0.7523x allocation. This exceeds the 2x speedup
target while leaving the CTE typed equivalent and smaller join cases below the
1.03 regression ceiling.

The broader single-cohort `TwoModeExecutionBenchmark` characterization also
completed successfully. At 5k rows, direct table projection measured
148.59–154.98 us and 399–401 KB across chunk shapes; public typed direct
projection measured 73.10–74.04 us and 125–127 KB. Loaded typed artifacts stayed
at 73.26–73.59 us. These are characterization measurements rather than a
replacement for the three-report comparison gate.

The protected weather cohort remained healthy. For one million rows, serial
grouped aggregation measured 15.910–16.860 ms and parallel aggregation measured
5.028–5.417 ms across 512 and 4096 row chunks. Parallel execution therefore
retained the existing approximately 3x speedup; its allocation increase is the
known parallel aggregation trade-off and was not redesigned in this wave.

The weather reports are in
`BenchmarkDotNet.Artifacts/evaluator-wave1-weather/results/`, and the broader
characterization report is in
`BenchmarkDotNet.Artifacts/evaluator-wave1-two-mode/results/`.

The post-cache trace is
`BenchmarkDotNet.Artifacts/evaluator-wave1-trace-2/aggregate-10k-direct.speedscope.speedscope.json`.
The baseline `Monitor.Enter_Slowpath` frame was no longer dominant after the
cache change; the remaining inclusive cost was `GetNestedValue`, which motivated
the duplicate-validation and value-pattern fast paths. No second global cache or
public API was introduced.

### Wave 2 — typed/table execution paths

Status: complete as a measurement-only wave; no runtime change was accepted.

The controlled table cohort used three isolated reports on the Wave 1 commit as
the before side:

- `BenchmarkDotNet.Artifacts/evaluator-wave2-baseline-table-1/results/Musoq.Benchmarks.TwoModeExecutionBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-wave2-baseline-table-2/results/Musoq.Benchmarks.TwoModeExecutionBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-wave2-baseline-table-3/results/Musoq.Benchmarks.TwoModeExecutionBenchmark-report-full-compressed.json`

The three after reports were captured from the working tree with the proposed
table-row streaming renderer:

- `BenchmarkDotNet.Artifacts/evaluator-wave2-current-table-1/results/Musoq.Benchmarks.TwoModeExecutionBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-wave2-current-table-2/results/Musoq.Benchmarks.TwoModeExecutionBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-wave2-current-table-3/results/Musoq.Benchmarks.TwoModeExecutionBenchmark-report-full-compressed.json`

The benchmark command was:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release -- `
  --filter "*TwoModeExecutionBenchmark*" --job short --memory --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/evaluator-wave2-current-table-1"
```

It was repeated for `-2` and `-3`; the baseline used the same command and
artifact layout with `evaluator-wave2-baseline-table-N`. The median table cases
were:

| Case | Before time / allocation | After time / allocation | Ratio |
| --- | ---: | ---: | ---: |
| Chunk512, 5k rows | 144.9 us / 391.16 KB | 150.9 us / 391.16 KB | 1.0411x / 1.0000x |
| Chunk4096, 5k rows | 154.1 us / 389.78 KB | 155.6 us / 389.78 KB | 1.0102x / 1.0000x |
| SingleGiant, 5k rows | 148.8 us / 389.64 KB | 151.1 us / 389.64 KB | 1.0155x / 1.0000x |

`BenchmarkComparison` correctly rejected the cohort because Chunk512 exceeded
the 1.03x time ceiling. Allocation did not improve. Inspection showed that this
benchmark selects the existing `TryCreateTableDirectProjectionMethod`, so the
new shape-to-row renderer did not lie on the measured path. The hypothesis that
the public typed table projection could be improved by removing an intermediate
shape was therefore disproven for this workload. The proposed renderer and
generated snapshot changes were discarded to preserve the existing typed/direct
execution boundary and maintainability.

Wave 2 full gate:

- Release build passed with zero warnings and errors.
- Full solution: 16,791 recorded results, 16,787 passed, 4 skipped; wall clock
  6:32.14, summed individual durations 8,011.389 seconds.
- TRXs: `TestResults/evaluator-wave-2-full`.
- The TRX report recorded 1,334 generated-sample results, 799 repeated-
  compilation results, 14,649 runtime results, and 9 integration results. The
  slowest individual entries were shared generated-sample waits of about 29–32
  seconds, while the evaluator project completed in 6:29.
- No new profiler trace was needed: no runtime path survived the comparison.
  The Wave 1 post-cache trace remains the applicable reflected-access evidence.

### Wave 3 — compilation cost separation

Status: complete as a measurement and benchmark-harness wave; no runtime cache
change was accepted.

The compilation benchmark now exposes these independent cases:

- cold simple and complex execution compilation;
- eligible execution-compilation cache hit;
- cache-ineligible compilation using a non-default source-runtime-settings
  resolver;
- typed artifact load and run;
- generated C# inspection; and
- Roslyn DLL emission.

The cache-hit setup reuses one schema-provider instance and warms the existing
`InstanceCreator.ExecutionCompilationCache`. It does not add another global
cache. New tests cover provider identity separation and prove that changing
source-runtime settings does not reuse cached execution.

Three pre-wave reports were captured with the refactored benchmark:

- `BenchmarkDotNet.Artifacts/evaluator-wave3-before-1/results/Musoq.Benchmarks.CompilationPipelineBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-wave3-before-2/results/Musoq.Benchmarks.CompilationPipelineBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-wave3-before-3/results/Musoq.Benchmarks.CompilationPipelineBenchmark-report-full-compressed.json`

Three post-wave reports were captured from the unchanged runtime:

- `BenchmarkDotNet.Artifacts/evaluator-wave3-after-1/results/Musoq.Benchmarks.CompilationPipelineBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-wave3-after-2/results/Musoq.Benchmarks.CompilationPipelineBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-wave3-after-3/results/Musoq.Benchmarks.CompilationPipelineBenchmark-report-full-compressed.json`

The command, repeated for each isolated artifact directory, was:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release -- `
  --filter "*CompilationPipelineBenchmark*" --job short --memory --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/evaluator-wave3-before-1"
```

Median pre-wave and post-wave characterization was:

| Case | Pre-wave time / allocation | Post-wave time / allocation |
| --- | ---: | ---: |
| Simple cold execution compilation | 150.539 ms / 12,974.07 KB | 154.070 ms / 12,974.72 KB |
| Eligible execution-compilation cache hit | 603.5 us / 756.53 KB | 583.8 us / 757.12 KB |
| Complex cold execution compilation | 193.716 ms / 15,953.17 KB | 190.673 ms / 15,948.13 KB |
| Cache-ineligible compilation | 134.430 ms / 12,941.61 KB | 144.734 ms / 12,939.55 KB |
| Typed artifact load and run | 16.456 ms / 4,841.23 KB | 16.215 ms / 4,841.23 KB |
| Simple generated C# | 53.489 ms / 2,702.03 KB | 53.562 ms / 2,694.70 KB |
| Simple emitted DLL | 134.680 ms / 12,966.49 KB | 122.831 ms / 12,963.50 KB |

The repository comparator self-check passed at `1.0000x` for time and allocation.
The independent before/after comparison flagged only the cache-ineligible case
at `1.0766x`; its three post-wave samples ranged from 134.930 ms to 164.427 ms
with no allocation increase. Since the runtime was unchanged, this is benchmark
noise in a high-variance cold compilation stage, not an accepted regression or
optimization. No profiler change was applicable; the Wave 1 reflected-access
trace remains the relevant runtime profile.

Wave 3 full gate:

- Release build passed with zero warnings and errors.
- Focused cache-key tests: 33 passed.
- Full solution: 16,793 recorded results, 16,789 passed, 4 skipped; wall clock
  6:28.26, summed individual durations 7,957.673 seconds.
- TRXs: `TestResults/evaluator-wave-3-full`.
- The TRX report recorded 1,334 generated-sample results, 801 repeated-
  compilation results, 14,649 runtime results, and 9 integration results. The
  slowest entries were shared generated-sample waits of about 29–32 seconds.

### Wave 4 — real-workload validation and hardening

Status: complete.

No production workload was supplied, so the final acceptance workload is the
curated Q227-Q230 corpus plus the existing evaluator benchmark suite. The
curated snapshot, manifest, and execution tests passed: 285 passed and 2
expected refresh helpers skipped.

The final reflected-access benchmark was run in three isolated BenchmarkDotNet
processes. Reports are stored at:

- `BenchmarkDotNet.Artifacts/evaluator-wave4-reflected-join-1/results/Musoq.Benchmarks.JoinAggregateProjectionBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-wave4-reflected-join-2/results/Musoq.Benchmarks.JoinAggregateProjectionBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-wave4-reflected-join-3/results/Musoq.Benchmarks.JoinAggregateProjectionBenchmark-report-full-compressed.json`

The final median for the 10k-row private reflected join aggregate was
58.988-60.338 ms and 74,614.94 KB allocated across the three runs. Against the
Wave 0 median of 139.275 ms and 99,186.34 KB, the three-run comparison was
0.4235x time and 0.7523x allocation. The 1k reflected case was 0.5385x time
and 0.8329x allocation; the typed CTE equivalents were 0.7063-0.8138x time
and 0.8923-0.9552x allocation. This preserves the Wave 1 result and exceeds
the required 2x improvement for the reflected hotspot.

The protected weather aggregate was also run three times. The 1M-row results
were 15.960-16.553 ms serial and 4.832-5.540 ms parallel across chunk sizes,
with the parallel path retaining roughly a 3x speedup. No weather execution
code was redesigned.

Weather reports are stored at:

- `BenchmarkDotNet.Artifacts/evaluator-wave4-weather-1/results/Musoq.Benchmarks.WeatherAggregateBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-wave4-weather-2/results/Musoq.Benchmarks.WeatherAggregateBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-wave4-weather-3/results/Musoq.Benchmarks.WeatherAggregateBenchmark-report-full-compressed.json`

Final Wave 4 gate:

- Release build passed with zero warnings and errors.
- Full solution: 16,793 recorded results, 16,789 passed, 4 skipped; wall clock
  6:27.03, summed individual durations 7,911.274 seconds.
- TRXs: `TestResults/evaluator-wave-4-full`.
- The duration report identified the same shared generated-sample lazy
  initialization waits: 1,334 generated-sample results totaling 1,165.036
  seconds, with a 32.114-second slowest entry. Runtime results totaled
  6,142.901 seconds across 14,649 tests. This separates shared initialization
  from actual test work in the TRX report.

The final implementation remains localized to the existing weak/bounded runtime
cache and benchmark/test infrastructure. No public API changes or second global
compilation cache were introduced. Every wave was completed in a separate
commit, with the commit identifiers recorded in the repository history.
