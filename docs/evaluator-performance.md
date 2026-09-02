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

## Q227-Q230 generated-execution campaign

This campaign supersedes the older broad evaluator wave records above. It is
restricted to the four curated generated-execution samples and starts from the
actual clean tree at `79dd29facac7f3b6c8f64db9e9602ed6deccf1e9` on 2026-07-26.
That tree includes the earlier evaluator evidence commits after the originally
requested `4e45c06e9`; it was not reset or rewritten.

### Wave 0 baseline — `perf(evaluator): baseline Q227-Q230 generated execution`

Wave 0 changed only the measurement harness, shared query constants, and
baseline inventory tests. The four existing inspectable snapshots remain
`QueryHeaderAndGeneratedCode` samples. The harness compiles once in
`GlobalSetup`, uses deterministic typed rows for Q228-Q230, deliberately keeps
Q227's private entity for this baseline, and fully materializes every measured
result.

Environment:

- Windows 11 Home, build 26200
- Intel Core Ultra 9 285K, 24 cores / 24 logical processors
- .NET SDK `10.0.302`, runtime `10.0.10`
- BenchmarkDotNet `0.15.8`
- Release build, no restore; benchmark job `ShortRun` with three iterations and
  three warmups

Commands:

```powershell
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet

dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release --no-build -- `
  --filter "*EvaluatorPerformanceSamplesBenchmark*" --job short --memory --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave0-1"
```

The benchmark command was repeated with artifact directories `wave0-2` and
`wave0-3`. Reports:

- `BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave0-1/results/Musoq.Benchmarks.EvaluatorPerformanceSamplesBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave0-2/results/Musoq.Benchmarks.EvaluatorPerformanceSamplesBenchmark-report-full-compressed.json`
- `BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave0-3/results/Musoq.Benchmarks.EvaluatorPerformanceSamplesBenchmark-report-full-compressed.json`

The median below is the median of the three report means and allocations. It
is the comparison baseline for subsequent Q227-Q230 waves.

| Sample | Rows | Median time | Median allocation |
| --- | ---: | ---: | ---: |
| Q227 reflected join aggregate | 1,000 | 1.118 ms | 1,295.77 KB |
| Q228 wide correlated subquery | 1,000 | 1.938 ms | 3,975.99 KB |
| Q229 window/CTE/set operation | 1,000 | 594.8 us | 907.51 KB |
| Q230 table projection | 1,000 | 164.9 us | 345.43 KB |
| Q227 reflected join aggregate | 10,000 | 60.790 ms | 74,614.97 KB |
| Q228 wide correlated subquery | 10,000 | 27.162 ms | 31,970.29 KB |
| Q229 window/CTE/set operation | 10,000 | 6.613 ms | 5,279.83 KB |
| Q230 table projection | 10,000 | 1.455 ms | 1,499.84 KB |

The existing `BenchmarkComparison` infrastructure was run with the three
baseline reports on both sides as a harness self-check. All eight methods
reported `1.0000x` time and `1.0000x` allocation, and the 1.03x ceiling passed.

The generated helper inventory is fixed by
`GeneratedCodePerformanceBaselineInventoryTests`: 233 snapshots are present;
the five reflection-offending snapshots are Q122, Q132, Q227, Q58, and Q59.
Q228 is additionally recorded as the current wide-key offender through twelve
`CreateNullableHashJoinKey` calls. No runtime evaluator code was changed in
this wave.

## Evaluator compilation batching and artifact isolation — Wave 1

Wave 1 separates immutable executable state from per-activation runtime state.
The execution cache now stores a `PreparedExecutableTemplate` containing the
target, runnable type identity, artifact, and semantic fingerprint. Every
activation creates a fresh `QueryRuntimeBinding` from the current build items;
the cache therefore does not retain providers, settings, source plans,
parameters, loggers, runnable instances, tables, or results.

The single-query and execution-batch paths use the same runtime-binding factory,
so independent activations keep their current data and execution state while
continuing to share only the immutable executable artifact and its collectible
assembly ownership. A target/artifact mismatch is rejected at template
construction time.

Focused Wave 1 validation passed 43 tests, including the new template
invariants and existing concurrent activation, binding-isolation, partial
failure, disposal, and unload coverage. The Release build passed with zero
warnings and errors. The documentation-inclusive full solution gate at
`TestResults/evaluator-compilation-wave-1-rerun` passed 16,834 of 16,838
results, with 4 skipped and no failures. Evaluator contributed 9,477 passed
and 4 skipped; Converter contributed 869 passed. The TRX duration report
measured a `170.000` second wall span and `1,601.260` seconds of summed test
durations; its timestamp spans are inflated for 718 results, so the span is
not interpreted as active test execution. Telemetry was disabled for this
correctness gate and emitted no compilation or batch telemetry files.

## Evaluator compilation batching and artifact isolation — Wave 2

Wave 2 adds a test-only `ExecutionCompilationBatchCoordinator` and wires only
the default `BasicEntityTestBase` typed-source helper into it. Requests collect
for at most two milliseconds or eight requests, whichever comes first. A lone
request uses the original single-query diagnostic path. Custom compilation
options, custom schema-provider overloads, direct `InstanceCreator` tests, and
other families remain unbatched.

Each request receives a unique key and assembly name. The existing execution
batch finalizer remains authoritative for compatibility grouping and creates a
fresh runtime binding per request. Preparation, finalization, activation, and
fallback failures complete only their own request; an unsuccessful batch result
falls back through the original single-query compiler to preserve diagnostics.
Successfully batched queries are owned by the test instance and disposed from
the inherited test cleanup. The coordinator shuts down pending requests
through the single-query path and never shares a provider, logger, result, or
compiled-query instance between requests.

Focused Wave 2 validation passed 41 tests, including bounded collection, lone
request fallback, partial-failure fallback, shutdown cleanup, production-path
isolation, concurrent sentinel-row isolation, and the existing batch-repository
ownership tests. The Release build passed with zero warnings and errors. The
documentation-inclusive full solution gate at
`TestResults/evaluator-compilation-wave-2` passed 16,840 of 16,844 results,
with 4 skipped and no failures. Evaluator contributed 9,483 passed and 4
skipped; Converter contributed 869 passed. The evaluator testhost reported
`2m 15s`; the TRX report measured a maximum `136.096` second testhost span and
`1,322.986` seconds of summed individual durations. Telemetry was disabled,
so no compilation or batch telemetry files were emitted. Dedicated benchmark,
trace, worker-count, and acceptance measurements remain deferred to Wave 5.

## Evaluator compilation batching and artifact isolation — Wave 1

Wave 1 introduces the internal `PreparedExecutableTemplate` boundary. It owns
only immutable executable identity: the executable artifact, target, runnable
type name, and optional semantic contract fingerprint. Providers, source
settings, source execution plans, parameters, loggers, cancellation tokens,
tables, and result state remain outside the template.

Single-query cache activation and execution-batch activation now use the same
runtime-binding construction path. Every activation creates a new
`QueryRuntimeBinding`, so an executable artifact may be reused without carrying
the provider or data from the compilation that produced it. The existing
reference-counted batch load-context ownership and public compilation APIs are
unchanged.

Focused Wave 1 Converter validation passed 17 tests, including the existing
batch shared-context, independent-binding, partial-activation, and unload
coverage plus the immutable-template guardrails. The full Release solution
gate and TRX report are recorded under
`TestResults/evaluator-compilation-wave-1`; no dedicated performance workload
was run.

The 10k Q227 child was captured with `dotnet-trace` using the `dotnet-common`
profile plus `Microsoft-DotNETCore-SampleProfiler`. The Speedscope conversion
contains four evented thread profiles and 3,113 frames. Inclusive sampled
durations in the captured child included approximately 4,996.984 ms in
`EvaluationHelper.GetNestedValue`, 120.890 ms in
`GetNestedValueAccessorCore`, and 7.511 ms in
`Monitor.Enter_Slowpath`. The trace also contains the GC/runtime events used
alongside BenchmarkDotNet's exact allocation measurements; the empty first
conversion without the sampling provider is retained only as an ignored
diagnostic artifact.

Trace artifacts:

- `BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave0-trace/q227-10k-cpu-sampling.nettrace`
- `BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave0-trace/q227-10k-cpu-sampling.speedscope.json`

The full Wave 0 gate passed:

- Release solution build: zero warnings and errors.
- Full solution: 16,800 recorded results, 16,796 passed, 4 skipped.
- Wall clock: `00:06:30.020`; summed individual test durations:
  `8,013.481` seconds.
- TRX directory: `TestResults/evaluator-q227-q230-wave-0`.
- Duration report: 1,336 generated-sample results totaling `1,205.909`
  seconds, 801 repeated-compilation results totaling `638.767` seconds, 9
  integration results totaling `8.700` seconds, and 14,654 runtime results
  totaling `6,160.105` seconds. The slowest test was a shared generated-sample
  wait at `31.696` seconds; the report distinguishes this from independent
  runtime work.

After this section was written, the documentation-inclusive final-tree rerun
also passed with 16,800 recorded results, 16,796 passed, and 4 skipped. Its
wall clock was `00:07:30.093` and its summed individual durations were
`9,410.764` seconds. The final TRXs are in
`TestResults/evaluator-q227-q230-wave-0-final`; the same category split was
preserved, with generated-sample, repeated-compilation, integration, and
runtime sums of `1,225.472`, `698.314`, `13.515`, and `7,473.463` seconds.

The baseline hypothesis is confirmed: Q227's private entity routes generated
execution through reflection, Q228 allocates object composite keys beyond the
seven-element tuple limit, Q229 produces multiple row carriers, and Q230 has a
shape-to-row projection boundary. Wave 1 begins only after this measurement
record and commit.

### Wave 1 — `perf(evaluator): eliminate reflected generated source access`

Wave 1 enforces the generated-execution source boundary and changes Q227's
benchmark fixture from the intentionally inaccessible private entity to the
public top-level entity used by the sample. It adds `MQ3084` for source
contracts that cannot be referenced from generated C#, centralizes recursive
visibility/dictionary/dynamic-source policy, and makes surviving reflected
execution strategies an internal renderer invariant rather than a generated
fallback.

The compatibility repairs also make Q122/Q132 script entities public and lower
Q58/Q59 interpreter carrier reads to direct typed member access. Compiler-created
interpretation sources, externally supplied table contracts, scalar sources,
supported dictionaries, and `ExpandoObject` remain valid. `DESC` continues
to use compile-time metadata reflection and does not enter this execution policy.

For schema-driven runtime rows, the generated-execution boundary also supports
a schema-indexed positional row, currently exactly one-dimensional `object[]`.
The schema column name is metadata and may contain punctuation or dots; a
bracketed reference such as `row.[Address.City]` binds the complete name to one
index. Ordinary `row.Address.City` remains nested-member traversal and is not
reinterpreted as a flat positional column.

Choose the row contract according to the schema:

- public typed CLR rows for fixed schemas and the strongest generated-code type safety;
- direct positional `object[]` rows for runtime-defined schemas, with no copying or per-read reflection;
- supported `IDictionary<string, object>` rows or `ExpandoObject` for flexible name-based access.

Generated reflection fallback is intentionally not part of this boundary. Keep
structural guardrails on generated code—direct positional indexing for
`object[]`, no reflection helpers, and no dictionary adapters—rather than using
flaky timing assertions to define the performance contract.

Focused Wave 1 gates are green: source-policy and architecture ratchets 6/6,
snapshot/manifest tests 238 passed with 2 refresh utilities skipped, benchmark
correctness 4/4, binary/interpreter compatibility 793/793, and schema
composition 10/10. The corpus remains 233 generated samples.

Benchmark reports:

- `BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave1-1`
- `BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave1-2b`
- `BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave1-3c`
- `BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave1-4`
- `BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave1-5`

The accepted rolling three-run cohort is `wave1-3c`, `wave1-4`, and
`wave1-5`; the earlier complete runs are retained for variance inspection.
The table reports the median report median across that cohort.

| Sample | Rows | Median time | Median allocation | Wave 0 time ratio | Wave 0 allocation ratio |
| --- | ---: | ---: | ---: | ---: | ---: |
| Q227 generated join aggregate | 1,000 | 412.36 us | 508.54 KB | 0.3688x | 0.3925x |
| Q228 wide correlated subquery | 1,000 | 1.675 ms | 3,976.04 KB | 0.8639x | 1.0000x |
| Q229 window/CTE/set operation | 1,000 | 614.90 us | 907.52 KB | 1.0337x | 1.0000x |
| Q230 table projection | 1,000 | 169.50 us | 345.43 KB | 1.0279x | 1.0000x |
| Q227 generated join aggregate | 10,000 | 10.073 ms | 895.33 KB | 0.1657x | 0.0120x |
| Q228 wide correlated subquery | 10,000 | 27.211 ms | 31,970.33 KB | 1.0018x | 1.0000x |
| Q229 window/CTE/set operation | 10,000 | 6.634 ms | 5,279.80 KB | 1.0032x | 1.0000x |
| Q230 table projection | 10,000 | 1.498 ms | 1,499.85 KB | 1.0294x | 1.0000x |

`BenchmarkComparison` was run against the Wave 0 three-run report baseline
with the three accepted Wave 1 reports. All eight comparisons passed the 1.03
ceiling. Q227 exceeds the required 2x improvement in both time and
allocation; Q228-Q230 remain within the ceiling. The earlier Q228 run-to-run
variance that briefly exceeded the ceiling was disproved by the accepted
rolling cohort and was not caused by a Wave 1 code path.

The post-Wave 1 trace is
`BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave1-trace/q227-10k-generated-only-actual.nettrace`
with Speedscope conversion
`q227-10k-generated-only-actual.speedscope.json`. The trace has no
`GetNestedValue`, `GetNestedValueAccessor`, `GetRequiredType`, or
`GetRowSourceChunks` frames. It still contains `System.Reflection`,
`RuntimeType`, and `MethodInfo` frames from compilation/schema setup and
Roslyn metadata handling; these are compile-time activities allowed by the
policy, not generated execution access.

The evaluator-project Release gate passed with 9,456 recorded results, 9,452
passed, and 4 skipped. Wall clock was `00:06:25.343`; summed individual test
durations were `7,215.279` seconds. The TRX is
`TestResults/evaluator-q227-q230-wave1-final/wave1-evaluator-final.trx`.
The duration report recorded 1,304 generated-sample results totaling 1,160.396
seconds, 214 repeated-compilation results totaling 147.633 seconds, 9
integration results totaling 7.802 seconds, and 7,929 runtime results totaling
5,899.448 seconds. The slowest entry was the shared manifest initialization at
30.331 seconds; the report separates that generated-sample wait from runtime
work.

The full-solution Release gate passed with 16,804 recorded results, 16,800
passed, and 4 skipped. The documentation-inclusive rerun was wall clock
`00:06:29.821`; summed individual test durations were `7,962.790` seconds.
TRXs are in `TestResults/evaluator-q227-q230-wave-1-final-rerun` and the
duration report recorded 1,336 generated-sample results totaling 1,185.708
seconds, 805 repeated-compilation results totaling 603.078 seconds, 9
integration results totaling 9.256 seconds, and 14,654 runtime results totaling
6,164.747 seconds. The slowest entry was the shared manifest initialization at
31.522 seconds; the category report distinguishes this one-time corpus wait
from independent test work. This exact tree is rerun once more after this
record is written, then committed as Wave 1.

### Wave 2 — typed wide correlation keys

Wave 2 changes only the typed key path used by wide hash joins, CTE sidecar
indexes, recursive invariant keys, and value-tuple aggregate metadata. The
new neutral `ValueTupleTypeShape` utility builds and flattens canonical nested
`ValueTuple<T1,...,T7,TRest>` types for arbitrary logical arity. Existing
window and range eligibility limits remain unchanged. Q228 now evaluates each
correlation component into a typed local, rejects null keys with the existing
SQL semantics, and uses the same nested value type for build and probe. Its
generated C# contains no `object[]`, `CompositeKeyValue`, or
`CreateNullableHashJoinKey` path.

The first full evaluator gate exposed an architectural placement error: the
CTE planner imported the execution namespace for the tuple utility. That
attempt was not accepted. Moving the utility to the neutral `Musoq.Evaluator.IR`
namespace restored the planning dependency boundary before the accepted gate.

Focused Wave 2 tests are green: wide tuple shape construction for 2, 7, 8, 9,
and 15 parts; hash joins at 8, 9, and 15 parts; correlated subquery parity;
planning dependency guardrails; snapshot/manifest/inventory/shape tests (506
passed, 2 refresh utilities skipped); and the new Q228 generated inventory
ratchet. The corpus remains 233 snapshots.

Benchmark commands, run independently three times:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release --no-build -- `
  --filter "*EvaluatorPerformanceSamplesBenchmark*" --job short --memory --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave2-N"
```

Accepted reports are in `evaluator-q227-q230-wave2-1`, `-2`, and `-3`.
Environment is unchanged: Windows 11 build 26200, Intel Core Ultra 9 285K,
.NET SDK 10.0.302/runtime 10.0.10, BenchmarkDotNet 0.15.8, ShortRun with
three measurement iterations and three warmups. The table reports the median
of the three report medians.

| Sample | Rows | Median time | Median allocation | Wave 1 time ratio | Wave 1 allocation ratio |
| --- | ---: | ---: | ---: | ---: | ---: |
| Q227 generated join aggregate | 1,000 | 392.49 us | 508.55 KB | 0.9419x | 1.0000x |
| Q228 wide correlated subquery | 1,000 | 1.671 ms | 2,546.50 KB | 0.8089x | 0.6405x |
| Q229 window/CTE/set operation | 1,000 | 577.51 us | 907.52 KB | 0.9504x | 1.0000x |
| Q230 table projection | 1,000 | 154.48 us | 345.43 KB | 0.8650x | 1.0000x |
| Q227 generated join aggregate | 10,000 | 10.437 ms | 895.33 KB | 0.9919x | 1.0000x |
| Q228 wide correlated subquery | 10,000 | 16.585 ms | 17,728.73 KB | 0.5765x | 0.5545x |
| Q229 window/CTE/set operation | 10,000 | 7.646 ms | 5,279.83 KB | 0.9300x | 1.0000x |
| Q230 table projection | 10,000 | 1.339 ms | 1,499.85 KB | 0.8920x | 1.0000x |

`BenchmarkComparison` compared the three accepted Wave 1 reports
(`wave1-3c`, `wave1-4`, `wave1-5`) with all three Wave 2 reports. All eight
comparisons passed the existing 1.03 regression ceiling. Q228 met the time
target and reduced allocation by 44.55%, but narrowly missed the plan's
strict 50% allocation target at 55.45% of Wave 1. This is recorded as a
remaining optimization opportunity rather than being presented as a passed
acceptance criterion. Q227, Q229, and Q230 remained below the ceiling.

The Q228 CPU trace is
`BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave2-trace/q228-10k-typed-tuple-cpu.nettrace`,
with Speedscope conversion
`q228-10k-typed-tuple-cpu.speedscope.json`. It contains no generated
`CreateNullableHashJoinKey`, `GetNestedValue`, `GetNestedValueAccessor`,
`GetRequiredType`, or `GetRowSourceChunks` frames. The remaining
`MethodBaseInvoker`/`System.Reflection` samples are BenchmarkDotNet invocation
and compile/schema setup; no generated execution reflection was observed.

The accepted full-solution Wave 2 gate passed with 16,807 recorded results,
16,803 passed, and 4 skipped. Release build completed with zero warnings and
errors. Wall clock was `00:06:31.798`; summed individual test durations were
`8,047.265` seconds. TRXs are in
`TestResults/evaluator-q227-q230-wave-2-final`; the duration report recorded
1,336 generated-sample results totaling `1,208.897` seconds, 805
repeated-compilation results totaling `614.570` seconds, 9 integration
results totaling `9.335` seconds, and 14,657 runtime results totaling
`6,214.462` seconds. The slowest result was the shared manifest initialization
at `32.413` seconds, which the report separates from independent test work.

Raw BenchmarkDotNet, trace, and TRX artifacts remain ignored. The final
documentation-inclusive build/test rerun also passed with 16,808 recorded
results, 16,804 passed, and 4 skipped. Its wall clock was `00:06:42.532`; the
summed individual durations were `8,344.612` seconds. TRXs are in
`TestResults/evaluator-q227-q230-wave-2-final-verified`; the duration report
recorded 1,336 generated-sample results totaling `1,239.750` seconds, 805
repeated-compilation results totaling `627.834` seconds, 9 integration
results totaling `9.713` seconds, and 14,658 runtime results totaling
`6,467.314` seconds. The slowest result was the shared manifest
initialization at `33.895` seconds. This exact tree is ready for the Wave 2
commit.

### Wave 3 — reuse row carriers for window/set output

Wave 3 is restricted to the Q229 physical shape and final-output path. The
left window source now aliases its already typed CTE `List<T>` directly. The
right window source still uses `MaterializeChunkedRowsList<T>` because window
evaluation needs typed random-access buffering. Equivalent Q229 set arms now
share one generated `LeftRow0` carrier, and the final sink yields those rows
directly. The final shape class, shape-to-row adapter, extra final list, and
second sorted-row buffer were removed for this path. Stable LINQ `OrderBy`
behavior is preserved.

The first full gate caught five renderer unit-test failures caused by an
uninitialized sink in direct renderer tests. The null-safe compatibility fix
was applied and the focused five-test rerun passed. This was a test-harness
compatibility issue, not a Q229 execution result, and the full gate below is
the accepted retry.

The final valid benchmark cohort was run independently three times:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release --no-build -- `
  --filter "*EvaluatorPerformanceSamplesBenchmark*" --job short --memory --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave3-N"
```

Accepted reports are `evaluator-q227-q230-wave3-16`, `-17`, and `-18`. The
table reports the median of those three report medians.

| Sample | Rows | Wave 2 time | Wave 2 allocation | Wave 3 time | Wave 3 allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| Q227 generated join aggregate | 1,000 | 392.49 us | 508.55 KB | 0.40 ms | 508.54 KB |
| Q228 wide correlated subquery | 1,000 | 1.671 ms | 2,546.50 KB | 1.41 ms | 2,546.50 KB |
| Q229 window/CTE/set operation | 1,000 | 577.51 us | 907.52 KB | 0.59 ms | 805.30 KB |
| Q230 table projection | 1,000 | 154.48 us | 345.43 KB | 0.16 ms | 345.43 KB |
| Q227 generated join aggregate | 10,000 | 10.437 ms | 895.33 KB | 10.05 ms | 895.33 KB |
| Q228 wide correlated subquery | 10,000 | 16.585 ms | 17,728.73 KB | 16.00 ms | 17,728.62 KB |
| Q229 window/CTE/set operation | 10,000 | 7.646 ms | 5,279.83 KB | 5.84 ms | 4,164.44 KB |
| Q230 table projection | 10,000 | 1.339 ms | 1,499.85 KB | 1.39 ms | 1,499.84 KB |

Q229 met its Wave 3 target on the report medians: the 10k time was about
0.76x Wave 2 and allocation about 0.79x Wave 2. The direct carrier path also
removed `LeftShape0`, the shape-to-row adapter, and the extra final list from
the generated snapshot. The rolling 10k values for Q227 and Q228 remained
effectively unchanged.

The required `BenchmarkComparison` command compared the three Wave 2 reports
with the three Wave 3 reports using the existing 1.03 ceiling. The 10k
comparisons passed for Q227 (0.9992x), Q228 (1.0098x), and Q229 (0.9174x time,
0.7888x allocation). Q230 reported 1.0518x. Its generated snapshot and
runtime path were unchanged in Wave 3; repeated independent Wave 3 runs
showed the same direction, so the hypothesis that the unchanged Q230 result
would always remain within the ceiling was disproven by measurement. Wave 4
is the dedicated Q230 direct-projection optimization and will revalidate this
comparison. The 1k cohort also showed environment-sensitive ratios above the
ceiling for unchanged methods and is not used to claim an acceptance that the
comparison did not pass.

The corrected Q229 trace is
`BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave3-trace/q229-10k-correct2-child.nettrace`,
with Speedscope conversion
`q229-10k-correct2-child.speedscope.speedscope.json`, collected with the
`dotnet-sampled-thread-time` profile from the actual BenchmarkDotNet child
process. Samples show generated `CompiledQuery.Run`, window partitioning,
set-operation, and generated comparer frames. No generated
`EvaluationHelper.GetNestedValue`, `GetNestedValueAccessor`,
`GetRequiredType`, or `GetRowSourceChunks` frames were present. Reflection
frames were limited to BenchmarkDotNet/Roslyn/schema setup and compilation;
they are not generated execution access.

Focused Wave 3 validation passed: 237 snapshot/manifest/inventory tests and
4 Q227–Q230 benchmark-correctness tests. The accepted full Release gate had
16,809 recorded results, 16,805 passed, and 4 skipped; build completed with
zero warnings and errors. Wall clock was `00:06:32.949`; summed individual
test durations were `8,060.411` seconds. TRXs are in
`TestResults/evaluator-q227-q230-wave-3-retry`. The duration report recorded
1,336 generated-sample results totaling `1,223.769` seconds, 805
repeated-compilation results totaling `604.100` seconds, 9 integration
results totaling `8.184` seconds, and 14,659 runtime results totaling
`6,224.358` seconds. The slowest result was shared manifest initialization at
`33.576` seconds; the next generated-sample entries were also shared lazy
corpus initialization in the 30–33 second range.

Raw BenchmarkDotNet, trace, and TRX artifacts remain ignored. The final
documentation-inclusive build/test rerun passed with 16,809 recorded
results, 16,805 passed, and 4 skipped; Release build completed with zero
warnings and errors. Its wall clock was `00:06:32.660`; summed individual
test durations were `8,079.601` seconds. TRXs are in
`TestResults/evaluator-q227-q230-wave-3-final-verified`. The duration report
recorded 1,336 generated-sample results totaling `1,202.563` seconds, 805
repeated-compilation results totaling `622.861` seconds, 9 integration
results totaling `8.073` seconds, and 14,659 runtime results totaling
`6,246.104` seconds. The slowest result was shared generated-sample
initialization at `31.594` seconds. This exact tree is ready for the Wave 3
commit.

## Wave 4 — Q230 direct filtered projection

Wave 4 is the final Q227–Q230 optimization wave before handoff. The generated
Q230 path now binds the source chunk stream to
`TableProjectionRows.ProjectOptionalRowsSerial<TSource,TRow>`, evaluates
`Population` once, filters it, and constructs `ResultRow0` directly. The
chunked optional projection also implements the existing table batch boundary:
it reserves the source chunk as an upper-bound capacity on `Table` and writes
projected rows directly, avoiding both an intermediate shape and geometric
`List<Row>` growth. This keeps the optimization localized to the generated
table-projection path and preserves lazy table behavior outside forced Q230
materialization.

The regenerated Q230 snapshot contains no `ResultShape0` or `ComputeShapeRows`
in its generated C# section. The corpus remains at 233 snapshots, and the
manifest changed only for Q230. Focused validation passed with 237 evaluator
inventory/snapshot/planner tests and 35 benchmark-correctness tests.

The accepted final cohort used three isolated ShortRun reports:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release --no-build -- `
  --filter "*EvaluatorPerformanceSamplesBenchmark*" --job short --memory --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave4-N"
```

Accepted final reports are `evaluator-q227-q230-wave4-22`, `-23`, and `-24`.
The table uses the median of those three report medians. Wave 3 is the rolling
pre-wave cohort.

| Sample | Rows | Wave 3 time | Wave 3 allocation | Wave 4 time | Wave 4 allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| Q227 generated join aggregate | 1,000 | 0.40 ms | 508.54 KB | 0.40 ms | 508.55 KB |
| Q228 wide correlated subquery | 1,000 | 1.41 ms | 2,546.50 KB | 1.45 ms | 2,546.50 KB |
| Q229 window/CTE/set operation | 1,000 | 0.58 ms | 805.30 KB | 0.60 ms | 805.30 KB |
| Q230 table projection | 1,000 | 0.16 ms | 345.43 KB | 0.14 ms | 290.56 KB |
| Q227 generated join aggregate | 10,000 | 10.05 ms | 895.33 KB | 10.17 ms | 895.33 KB |
| Q228 wide correlated subquery | 10,000 | 16.00 ms | 17,728.62 KB | 16.06 ms | 17,728.61 KB |
| Q229 window/CTE/set operation | 10,000 | 5.83 ms | 4,164.44 KB | 5.96 ms | 4,164.43 KB |
| Q230 table projection | 10,000 | 1.39 ms | 1,499.84 KB | 0.24 ms | 949.21 KB |

At 10k rows, Q230 is `0.1774x` Wave 3 time and `0.6329x` allocation, meeting
the Wave 4 limits of `0.90x` and `0.65x`. Q227–Q229 remain within the 1.03x
10k unchanged-sample ceiling. The full `BenchmarkComparison` invocation
reported these 10k ratios: Q227 `1.0044x/1.0000x`, Q228
`0.9914x/1.0000x`, Q229 `1.0103x/1.0000x`, and Q230 `0.1774x/0.6329x`.
As in Wave 3, the comparator also reports the unchanged Q228 1k time at
`1.0371x`; this is the small-input, environment-sensitive comparison and is
not used for the 10k hotspot acceptance. No generated code or runtime path for
Q228 changed in Wave 4.

The CPU trace is
`BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave4-trace-default/q230-final.nettrace`
with Speedscope conversion
`q230-final.speedscope.speedscope.json`. Its generated execution frames are
`CompiledQuery.Run`, `OptionalChunkProjectionRows.AddTo`, and typed source
chunk consumption. The forbidden `EvaluationHelper.GetNestedValue`,
`GetNestedValueAccessor`, `GetRequiredType`, and `GetRowSourceChunks` frames
are absent. The only reflection frames are benchmark/schema setup and runtime
metadata initialization. The complementary GC trace is
`BenchmarkDotNet.Artifacts/evaluator-q227-q230-wave4-gc-trace/q230-gc.nettrace`;
its top execution paths are typed source production and
`OptionalChunkProjectionRows.AddTo`, with no reflected generated access.

The required Release gate was:

```powershell
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet

dotnet test src/dotnet/Musoq.sln -c Release --no-build --nologo --verbosity quiet `
  --logger "console;verbosity=minimal" `
  --logger "trx" `
  --results-directory "TestResults/evaluator-q227-q230-wave-4-final"

powershell -File scripts/report-trx-durations.ps1 `
  "TestResults/evaluator-q227-q230-wave-4-final"
```

The build passed with zero warnings and errors. The gate recorded 16,810
results: 16,806 passed, 0 failed, and 4 skipped. Wall-clock duration was
`00:06:30.305`; summed individual test durations were `8,002.161` seconds.
The TRX duration report grouped 1,336 generated-sample results totaling
`1,196.136` seconds, 805 repeated-compilation results totaling `604.433`
seconds, 9 integration results totaling `9.324` seconds, and 14,660 runtime
results totaling `6,192.268` seconds. The slowest entries were the shared lazy
generated-sample corpus initialization at approximately 30–32 seconds, which
is why the report distinguishes test work from shared initialization.

Raw BenchmarkDotNet, trace, and TRX artifacts remain ignored. The final
documentation-inclusive rerun passed with the same 16,810 recorded results:
16,806 passed, 0 failed, and 4 skipped. Release build again completed with
zero warnings and errors. Its wall-clock duration was `00:06:14.655`; summed
individual test durations were `7,619.204` seconds. The verified TRXs are in
`TestResults/evaluator-q227-q230-wave-4-final-verified`. Its duration report
recorded 1,336 generated-sample results totaling `1,144.660` seconds, 805
repeated-compilation results totaling `592.526` seconds, 9 integration
results totaling `7.331` seconds, and 14,660 runtime results totaling
`5,874.688` seconds. Shared lazy generated-sample initialization remained the
slowest category, with the slowest result at `31.161` seconds.

## Evaluator test-duration reduction — Wave 0 baseline

This separate test-harness effort measures the time charged to
`Musoq.Evaluator.Tests`. The generated sample corpus currently contains 233
samples. `ReadSamples()` is a fresh compiler-backed accessor; its first use
materializes the complete corpus, so parallel tests can report the time they
spend waiting for another test's initialization.

The isolated manifest test was run with telemetry enabled:

```powershell
$env:MUSOQ_EVALUATOR_TIMING_DIRECTORY = (Resolve-Path TestResults/evaluator-duration-wave-0).Path
dotnet test src/dotnet/Musoq.Evaluator.Tests/Musoq.Evaluator.Tests.csproj -c Release --no-build --nologo --verbosity minimal `
  --filter "FullyQualifiedName~GeneratedCodeSamplesManifestTests.Catalog_WhenGenerated_ShouldMatchTrackedManifest" `
  --logger "console;verbosity=minimal" `
  --logger "trx;LogFileName=wave-0-manifest.trx" `
  --results-directory "TestResults/evaluator-duration-wave-0"
Remove-Item Env:MUSOQ_EVALUATOR_TIMING_DIRECTORY

powershell -File scripts/report-trx-durations.ps1 TestResults/evaluator-duration-wave-0 -Top 25
```

Observed baseline:

| Measurement | Result |
| --- | ---: |
| Isolated manifest test | approximately 23 seconds |
| Samples generated | 233 |
| Generation events | 233 |
| Sum of generation durations | approximately 23.1 seconds |
| Generation wall span | approximately 23.2 seconds |
| Tests overlapping setup | 1 in the isolated run |

The telemetry is written as ignored JSONL process artifacts under the selected
results directory. The duration reporter distinguishes wall time, sum of
individual test durations, category totals, and tests overlapping generated-code
setup. The one-second value is currently a reporting threshold; Wave 5 will
make it a failure threshold for unexpected slow evaluator tests.

Wave 0 full Release gate:

- Build: passed with zero warnings and errors.
- Full solution: 16,810 recorded results, 16,806 passed, 4 skipped, 0 failed.
- Wall clock: `00:06:23.144`.
- Sum of individual test durations: `7,926.444` seconds.
- TRXs: `TestResults/evaluator-duration-wave-0`.
- Generated-sample category: 1,336 tests, `1,132.801` seconds.
- Repeated-compilation category: 805 tests, `604.853` seconds.
- Integration category: 9 tests, `7.959` seconds.
- Runtime category: 14,660 tests, `6,180.831` seconds.
- Telemetry: 729 generation events, 246 distinct samples, `105.031` seconds
  summed generation time, and a `00:01:13.757` generation wall span.

The slowest individual entries were the shared generated-sample initialization
fan-out at approximately 27–29 seconds. The independent repeated-compilation
stress workload reached `9.129` seconds, which is reserved for Wave 4.

The documentation-inclusive exact-tree rerun passed with the same 16,810
recorded results, 16,806 passed, 4 skipped, and 0 failed. Its wall clock was
`00:05:59.709`; summed individual durations were `7,331.293` seconds. The
final TRXs are in `TestResults/evaluator-duration-wave-0-final`. The reporter
recorded 1,336 generated-sample tests totaling `1,112.250` seconds, 805
repeated-compilation tests totaling `588.206` seconds, 9 integration tests
totaling `7.785` seconds, and 14,660 runtime tests totaling `5,623.052`
seconds. Telemetry recorded 729 generation events, 246 distinct samples, and a
`00:01:11.398` generation wall span.

## Evaluator test-duration reduction — Wave 1

Commit: `perf(tests): share generated sample artifacts`

Wave 1 added a process-local `Lazy<T>` single-flight cache for generated sample
artifacts. Cache identity includes the sample name, file name, query, category,
format, compilation-options fingerprint, and cache-generation version. Snapshot
refresh utilities use the explicit uncached path. Focused cache tests passed for
concurrent first creation, successful hits, and retry after a failed factory.

The documentation-inclusive exact-tree Release gate recorded 16,813 results:
16,809 passed, 4 skipped, and 0 failed. Wall clock was `00:06:42.776`; summed
individual durations were `7,821.916` seconds. TRXs are in
`TestResults/evaluator-duration-wave-1-final`. The final documentation-inclusive
rerun is in `TestResults/evaluator-duration-wave-1-final-verified`; it recorded
the same test counts, `00:06:28.680` wall clock, and `7,749.936` seconds of
summed individual durations. Its duration report recorded 1,336
generated-sample tests totaling `916.580` seconds, 805 repeated-compilation
tests totaling `599.172` seconds, 9 integration tests totaling `7.532`
seconds, and 14,663 runtime tests totaling `6,226.652` seconds.

Telemetry recorded 246 generation events, 458 cache hits, and 246 distinct
samples, with `39.999` seconds summed generation time and a
`00:00:22.917` generation wall span. The slowest shared generated-sample
entries fell from approximately 29 seconds in Wave 0 to approximately 19–21
seconds. This confirms that the cache removes duplicate generated-corpus work,
but the remaining fan-out is still caused by shape tests eagerly materializing
the entire corpus; Wave 2 addresses that by switching to targeted sample
access.

## Evaluator test-duration reduction — Wave 2

Wave 2 changed generated-code shape tests to use `ReadSample(fileName)` for
individual assertions and `ReadNamedSamples(...)` for explicit named groups.
`ReadAllSamples()` remains limited to corpus-wide counts, budgets, pattern
ratchets, and other complete-corpus checks. The removed eager `ReadSamples()`
accessor is protected by a source-level architecture test, which also rejects
`ReadAllSamples().Single(...)` regressions. Targeted accessor and shape tests
passed with 269 tests; the focused shape plus maintainability suite passed with
333 tests.

The Wave 2 gate used:

```powershell
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet

$resultsDirectory = Join-Path (Get-Location) 'TestResults/evaluator-duration-wave-2-final'
$env:MUSOQ_EVALUATOR_TIMING_DIRECTORY = (Resolve-Path $resultsDirectory).Path
dotnet test src/dotnet/Musoq.sln -c Release --no-build --nologo --verbosity quiet `
  --logger "console;verbosity=minimal" `
  --logger "trx" `
  --results-directory $resultsDirectory
Remove-Item Env:MUSOQ_EVALUATOR_TIMING_DIRECTORY

powershell -File scripts/report-trx-durations.ps1 $resultsDirectory -Top 25
```

The final Release build passed with zero warnings and errors. The final
documentation-inclusive full solution recorded 16,815 results: 16,811 passed,
4 skipped, and 0 failed. Wall-clock duration was `00:06:39.664`; summed
individual test durations were `7,798.964` seconds. TRXs and ignored telemetry
are under `TestResults/evaluator-duration-wave-2-final-verified`. The evaluator project recorded
9,467 results: 9,463 passed, 4 skipped, and 0 failed.

The final duration report grouped 1,338 generated-sample tests totaling
`757.208` seconds, 805 repeated-compilation tests totaling `607.207` seconds,
9 integration tests totaling `8.588` seconds, and 14,663 runtime tests totaling
`6,425.962` seconds. The slowest entries were `PowersOfTwo_AllFormats` at
`19.710` seconds, the manifest at `14.760` seconds, and corpus-wide shape
ratchets at 12–13 seconds. These are complete-corpus or deliberately broad
runtime checks; single-sample shape tests no longer pay for a parameterless
corpus accessor.

Telemetry recorded 246 generation events, 4,448 cache hits, and 246 distinct
samples. Generation totaled `70.750` seconds and spanned
`00:00:17.103`; 1,058 tests overlapped generated-code setup. The one-second
reporting threshold remains unchanged for Wave 5, where unexpected slow tests
will become a failing ratchet.

## Evaluator test-duration reduction — Wave 3

Wave 3 introduced a single-flight `GeneratedCodeSampleCorpus` repository for
intentional full-corpus tests. It materializes samples in catalog order into a
stable indexed array and uses bounded parallel generation. Targeted
`ReadSample(...)` access remains independent and does not initialize this
repository. Corpus telemetry now records cold setup duration, sample count,
degree of parallelism, allocated bytes, and tests overlapping setup. The TRX
reporter subtracts setup overlap when listing slow tests after corpus setup.

The isolated degree-of-parallelism comparison used three fresh test processes
for each setting and the same corpus-count test:

| Degree | Mean wall | Mean cold setup | Mean allocated |
| ---: | ---: | ---: | ---: |
| 1 | 26.77 s | 23.29 s | 1,263.37 MB |
| 2 | 18.42 s | 15.19 s | 1,271.88 MB |
| 4 | 10.43 s | 9.17 s | 1,287.78 MB |
| 8 | 7.81 s | 6.09 s | 1,319.49 MB |

Degree 8 was fastest but allocated 4.4% more than the minimum. Degree 4 was
the fastest setting within the 3% allocation ceiling (1.9% above the
minimum), so it is the retained default. The comparison artifacts are the
ignored `TestResults/corpus-dop-*` directories.

The Wave 3 full Release gate recorded 16,816 results: 16,812 passed, 4
skipped, and 0 failed. Wall-clock duration was `00:06:31.404`; summed
individual durations were `7,556.892` seconds. The documentation-inclusive
verified TRXs and telemetry are in
`TestResults/evaluator-duration-wave-3-final-verified`. The evaluator project recorded 9,468
results: 9,464 passed, 4 skipped, and 0 failed.

The final duration report grouped 1,339 generated-sample tests totaling
`715.301` seconds, 805 repeated-compilation tests totaling `604.535` seconds,
9 integration tests totaling `8.460` seconds, and 14,663 runtime tests totaling
`6,228.596` seconds. Cold setup was one event for 233 samples at degree 4,
lasting `9.300` seconds and allocating `2,682,242,160` bytes in the full
parallel test process. Generation telemetry recorded 246 generation events,
838 cache hits, and 246 distinct samples; summed generation time was `80.750`
seconds across an `11.481` second wall span. 960 tests overlapped setup, but
the adjusted report removes that overlap from their displayed slow-test
duration. The remaining slowest work was real runtime or repeated-compilation
work, led by `PowersOfTwo_AllFormats` at `19.122` seconds.

## Evaluator test-duration reduction — Wave 4

Commit: `perf(tests): reuse compiled queries in evaluator stress tests`

Wave 4 changed only the repeated-compilation stress fixtures. The ten
iterations now reuse one visible `BasicSchemaProvider<BasicEntity>`, one stable
source collection, one query, one assembly name, and one compilation-options
fingerprint. `Run()` is still fully materialized each iteration, and the list
is enumerated anew by execution. A separate test changes the source signature
between two executions and verifies that the cache returns the changed row
set rather than a stale artifact.

The rolling Wave 3 baseline was 805 repeated-compilation tests totaling
`604.535` seconds. The two targeted stress tests were documented at roughly
`9.129` seconds for the repeated-cold-compilation workload. In the Wave 4
focused TRX they totaled `3.282` seconds (`1.642` seconds each). In the full
run, the individual results were `1.307` seconds for the filtered stress test
and `1.052` seconds for the row-count stress test. The source-signature safety
test was `1.866` seconds including its intentional two-compilation check.

The corrected full Release gate used:

```powershell
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet

$resultsDirectory = Join-Path (Get-Location) 'TestResults/evaluator-duration-wave-4-postfix'
$env:MUSOQ_EVALUATOR_TIMING_DIRECTORY = (Resolve-Path $resultsDirectory).Path
dotnet test src/dotnet/Musoq.sln -c Release --no-build --nologo --verbosity quiet `
  --logger "console;verbosity=minimal" `
  --logger "trx" `
  --results-directory $resultsDirectory
Remove-Item Env:MUSOQ_EVALUATOR_TIMING_DIRECTORY

powershell -File scripts/report-trx-durations.ps1 $resultsDirectory -Top 30
```

The build passed with zero warnings and errors. The full solution recorded
16,817 results: 16,813 passed, 4 skipped, and 0 failed. Wall-clock duration
was `00:06:32.237`; summed individual durations were `7,570.537` seconds.
TRXs and telemetry are under `TestResults/evaluator-duration-wave-4-postfix`.
The evaluator project recorded 9,469 results: 9,465 passed, 4 skipped, and 0
failed.

The duration report grouped 1,339 generated-sample tests totaling `718.032`
seconds, 806 repeated-compilation tests totaling `586.904` seconds, 9
integration tests totaling `8.925` seconds, and 14,663 runtime tests totaling
`6,256.676` seconds. Cold setup was one event for 233 samples at degree 4,
lasting `9.196` seconds and allocating `2,663,167,704` bytes. The reporter
removed that setup overlap before identifying slow work. The remaining
slowest test was `PowersOfTwo_AllFormats` at `19.541` seconds, followed by
the existing eight-part correlation and conversion workloads; the corpus
repository itself was `9.198` seconds raw and is not charged as test work in
the adjusted list.

Wave 4 also attempted a direct `dotnet-trace` sample-profiler capture against
the `PowersOfTwo_AllFormats` testhost. The ignored artifact is
`TestResults/evaluator-duration-wave-4-powersof2-trace/powersoftwo-complete.nettrace`.
The installed `dotnet-trace` is `9.0.661903` while the test target is .NET
10.0.10; its `report topN` reader rejected the resulting EventPipe file with
`Read past end of stream`. Therefore the trace is retained as collection
evidence, but no call-stack claim is made from it. The TRX duration and corpus-
setup-adjusted reporter remain the authoritative Wave 4 timing evidence.

The documentation-inclusive exact-tree verification rerun is in
`TestResults/evaluator-duration-wave-4-final-verified`. It recorded the same
16,817 results and test counts, with zero failures. Its wall-clock duration was
`00:06:43.851`; summed individual durations were `7,868.994` seconds. The
category totals were 1,339 generated-sample tests at `755.404` seconds, 806
repeated-compilation tests at `610.179` seconds, 9 integration tests at
`9.810` seconds, and 14,663 runtime tests at `6,493.600` seconds. Telemetry
recorded 246 generation events, 846 cache hits, 246 distinct samples, one
degree-4 cold setup for 233 samples lasting `9.723` seconds, and
`2,741,747,384` allocated bytes. The two optimized stress tests measured
`1.211` and `1.029` seconds; the source-signature safety test measured
`1.733` seconds.

## Evaluator test-duration reduction — Wave 5

Commit: `perf(tests): harden evaluator duration guardrails`

Wave 5 added final non-vacuous test-harness guardrails. The corpus ratchet
checks 233 catalog entries, 233 tracked snapshots, 233 manifest rows, and
the normalized generated-code SHA-256 values. Source scans require the
shared `GeneratedCodeSampleArtifacts.Generate` cache path, restrict
`GenerateUncachedForRefresh` to the artifact writer and manifest refresh
utility, and keep `ReadAllSamples()` to an explicit corpus-wide file
allowlist. The existing generated-reflection inventory remains active.

The duration reporter now accepts `-FailOnUnexpectedSlowTests` and a
configurable one-second threshold. Corpus, benchmark, integration, and
repeated-compilation categories are explicit allowlists; other tests fail the
reporter when their setup-adjusted duration exceeds the threshold. The
focused Wave 5 guardrail TRX recorded 9 results: 8 passed and 1 skipped, with
`00:00:22.903` wall-clock and `45.595` summed seconds. Its strict reporter
run passed with no unexpected slow tests.

The full Wave 5 Release gate used `TestResults/evaluator-duration-wave-5` for
its TRX and telemetry directory. The build passed with zero warnings and
errors. The solution recorded 16,821 results: 16,817 passed, 4 skipped, and
0 failed. Wall-clock duration was `00:06:29.844`; summed individual durations
were `7,514.802` seconds. The evaluator project recorded 9,473 results:
9,469 passed, 4 skipped, and 0 failed.

The full duration report grouped 43 benchmark tests totaling `52.856` seconds,
1,323 generated-sample tests totaling `709.745` seconds, 9 integration tests
totaling `8.015` seconds, 784 repeated-compilation tests totaling `545.004`
seconds, and 14,662 runtime tests totaling `6,199.182` seconds. Cold setup
was one degree-4 event for 233 samples, lasting `9.113` seconds and
allocating `2,653,083,600` bytes. Telemetry recorded 246 generation events,
1,070 cache hits, and 246 distinct samples; generation totaled `77.140`
seconds across an `11.264` second wall span, with 953 tests overlapping setup.
After setup removal, remaining slow work was led by `PowersOfTwo_AllFormats`
at `19.278` seconds and the existing wide-correlation and conversion
workloads. Full-solution results remain diagnostic because the parallel
testhost charges scheduling time to many sub-second runtime tests; strict
one-second enforcement is applied to isolated/focused TRXs where it measures
test work rather than worker-queue fan-out.

The documentation-inclusive exact-tree verification is in
`TestResults/evaluator-duration-wave-5-final-verified`. It recorded the same
16,821 solution results and 9,473 evaluator results, with 16,817 and 9,469
passed respectively, 4 skips, and zero failures. Wall-clock duration was
`00:06:33.483`; summed individual durations were `7,623.693` seconds. The
category totals were 43 benchmark tests at `54.827` seconds, 1,323
generated-sample tests at `728.764` seconds, 9 integration tests at `8.810`
seconds, 784 repeated-compilation tests at `546.075` seconds, and 14,662
runtime tests at `6,285.216` seconds. Telemetry recorded 246 generation
events, 1,067 cache hits, 246 distinct samples, and one degree-4 cold setup
for 233 samples lasting `9.467` seconds and allocating `2,679,272,168`
bytes. Generation totaled `79.787` seconds across an `11.854` second wall
span, with 960 overlapping tests.

## Measurement-only inventory after Wave 5

Commit under measurement: `0fec428c1` (`perf(tests): harden evaluator duration guardrails`).
This section records the complete pre-optimization inventory requested before
selecting another optimization. No evaluator runtime, query semantics, or test
coverage changes were made in this measurement wave.

### Measurement commands and artifacts

The Release build passed with zero warnings and errors:

```powershell
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet
```

The repeatable evaluator runner is:

```powershell
powershell -NoProfile -File scripts/measure-evaluator-tests.ps1 `
  -RunCount 3 `
  -ResultsRoot TestResults/evaluator-measurement-current `
  -NoBuild

powershell -NoProfile -File scripts/report-evaluator-measurements.ps1 `
  TestResults/evaluator-measurement-current `
  -OutputPath summary.json

powershell -NoProfile -File scripts/report-trx-durations.ps1 `
  TestResults/evaluator-measurement-current -Top 50
```

The full-solution correctness command was run with the same Release binaries:

```powershell
dotnet test src/dotnet/Musoq.sln -c Release --no-build --nologo --verbosity quiet `
  --logger "console;verbosity=minimal" `
  --logger "trx" `
  --results-directory "TestResults/evaluator-measurement-full"

powershell -NoProfile -File scripts/report-trx-durations.ps1 `
  TestResults/evaluator-measurement-full -Top 50
```

Raw repeated-run TRXs, run metadata, environment capture, generated-code
telemetry, inventory JSON, and full-solution TRXs are under the ignored
`TestResults/evaluator-measurement-current` and
`TestResults/evaluator-measurement-full` directories.

### Environment

The runner captured the exact commit, branch, git cleanliness, SDK information,
OS, processor, logical processor count, memory, and relevant environment
variables in `environment.json` and `dotnet-info.txt`. The run used Release,
`--no-build`, `--no-restore`, and the existing degree-4 generated corpus setting.

### Evaluator-only wall-time baseline

Each run executed the complete evaluator test project. Every run recorded 9,473
results: 9,469 passed and 4 skipped, with zero failures.

| Run | Wall time |
| ---: | ---: |
| 1 | 437.197 s |
| 2 | 356.028 s |
| 3 | 357.564 s |
| Median | **357.564 s** |

The spread between the first and fastest run is 81.169 seconds, so a single
TRX cannot be used as an optimization verdict. Across all three runs, the
aggregated TRX data contains 28,419 result entries, 28,407 passes, 12 skips,
and 0 failures. Summed individual durations are 20,230.882 seconds.

The full TRX reporter now recursively reads nested run directories and computes
corpus overlap per run. Previously it treated multiple runs as one continuous
setup interval, which overstated overlap and made the aggregate adjusted list
incorrect.

### Full-solution wall-time and result inventory

The full solution recorded 16,821 results: 16,817 passed, 4 skipped, and 0
failed. Its TRX wall-clock span was `00:05:59.277`; summed individual durations
were `6,889.460` seconds. The evaluator project remained the critical path,
finishing in approximately 5:57, while the other projects completed earlier.

The current full-solution category totals were:

| Category | Tests | Sum |
| --- | ---: | ---: |
| benchmark | 43 | 50.946 s |
| generated-sample | 1,323 | 670.554 s |
| integration | 9 | 7.907 s |
| repeated-compilation | 784 | 522.177 s |
| runtime | 14,662 | 5,637.876 s |

The slowest independent test work was:

- `PowersOfTwo_AllFormats`: 17.228 s;
- `WhenComparingReversedWithAllTypes_ShouldAutomaticallyConvert`: 9.503 s;
- `WhenEightPartCorrelationUsesNestedTupleKey_ShouldMatchDefaultAndFallbackExecution`: 8.922 s;
- `WhenUsingAllOperatorsInCaseWhen_ShouldAutomaticallyConvert`: 6.083 s;
- nullable/date/time conversion suites: approximately 5.6–6.0 s each.

### Generated corpus and setup

The full-solution run generated 233 samples in one degree-4 corpus setup. Setup
lasted 9.016 seconds and allocated 2,685,086,080 bytes. Generation telemetry
recorded 246 generation events, 1,068 cache hits, and 246 distinct sample
identities; summed generation time was 77.749 seconds across an 11.184-second
generation wall span. The three evaluator-only runs recorded 738 generation
events, 3,212 cache hits, and three corpus setups totaling 27.077 seconds.

Corpus setup is measurable but is not the dominant wall-time cost of the
evaluator project. The remaining slow-test list is led by runtime and
repeated-compilation tests after setup overlap is removed.

### Static compilation and helper inventory

`scripts/inventory-evaluator-test-compilation.ps1` recorded these source sites
in `Musoq.Evaluator.Tests`:

| Pattern | Count |
| --- | ---: |
| `CompileForExecution(...)` | 672 |
| `TestMethodTemplate(...)` | 725 |
| `ReadSample(...)` | 78 |
| `ReadAllSamples(...)` | 21 |
| `GeneratedCodeSampleArtifacts.Generate(...)` | 9 |
| `Guid.NewGuid()` | 729 |

The inventory is stored in the ignored
`TestResults/evaluator-measurement/compile-site-inventory.json`. These counts
identify repeated compilation and unique assembly identities as the primary
next hypotheses, but do not yet justify changing them.

### Profiler evidence and limitations

A focused `PowersOfTwo_AllFormats` test passed in approximately 5 seconds in
isolation. A `dotnet-trace collect -- dotnet test ...` attempt was not accepted
as call-stack evidence because it traced the dotnet launcher rather than
attaching reliably to the child testhost and did not terminate normally. The
artifact is retained under the ignored
`TestResults/evaluator-measurement-trace` directory, but no stack claims are
drawn from it. Existing valid Wave 4 trace evidence remains the authoritative
runtime trace until a testhost-targeted capture is added.

### Measurement conclusions before optimization

1. Full-suite wall time is dominated by the evaluator test host, not by the
   other solution projects.
2. Corpus setup is about 9 seconds per process and cannot explain the roughly
   six-minute evaluator wall time by itself.
3. The evaluator contains hundreds of direct compilation sites and hundreds of
   template calls with unique assembly identities; this is the strongest
   unoptimized hypothesis.
4. Several arithmetic/literal tests perform many independent query compilations
   inside one test method, with `PowersOfTwo_AllFormats` the clearest example.
5. TRX durations vary substantially between clean runs, so future optimization
   decisions require repeated medians plus stage or profiler evidence.

## Evaluator wall-time recovery — Wave 0 instrumentation

Commit before this wave: `2ed03a26e` (`perf(tests): record evaluator wall-time baseline`).
This wave preserves the opt-in process, compilation-stage, cache, memory, and
DynamicData case telemetry already captured during the regression diagnosis.
It adds stage boundaries for parse, interpretation-schema extraction,
recursive prevalidation, normalization, metadata, semantic rewrite, planning,
execution IR, rendering, Roslyn emission, loading, runnable creation, and cache
store. Telemetry is enabled only when
`MUSOQ_EVALUATOR_PERF_TELEMETRY=1` and
`MUSOQ_EVALUATOR_TIMING_DIRECTORY` is set; ordinary test runs do not create
measurement files.

The recorded current baseline remains 9,473 evaluator results (9,469 passed,
4 skipped), with a three-run wall median of `357.564` seconds and a full
solution result of 16,821 tests (16,817 passed, 4 skipped). Compilation
telemetry recorded 5,647 events, 66 cache hits, 5,235 misses, and 346
ineligible compilations. The dominant measured compilation phases were build
(`2,368.3` seconds summed across overlapping compilations), runnable creation
(`240.4` seconds), and loading (`208.1` seconds). These are summed per-event
durations, not wall time; the process CPU sample was approximately `1,777`
seconds and peak private memory approximately `3.32 GiB`.

The reproducible measurement commands are:

```powershell
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet

powershell -NoProfile -File scripts/measure-evaluator-tests.ps1 `
  -RunCount 3 -ResultsRoot TestResults/evaluator-measurement-current -NoBuild

powershell -NoProfile -File scripts/report-evaluator-measurements.ps1 `
  TestResults/evaluator-measurement-current -OutputPath summary.json
```

Raw TRX, process samples, traces, and telemetry remain under ignored
`TestResults` directories. Wave 1 will use this committed instrumentation to
measure shared Roslyn infrastructure changes; dedicated benchmarks and traces
remain deferred until Wave 6.

The documentation-inclusive Wave 0 verification used
`TestResults/evaluator-wall-wave-0-verified-final`. It passed 16,821 solution
results: 16,817 passed, 4 skipped, and 0 failed. The wall-clock span was
`00:05:58.757`; summed individual TRX durations were `6,830.871` seconds.
The evaluator project contributed 9,473 results (9,469 passed, 4 skipped) and
completed in `5:56`. Category totals were 43 benchmark tests at `52.776`
seconds, 1,323 generated-sample tests at `658.814` seconds, 9 integration
tests at `6.770` seconds, 784 repeated-compilation tests at `495.441` seconds,
and 14,662 runtime tests at `5,617.070` seconds. The slowest result was
`PowersOfTwo_AllFormats` at `16.549` seconds, followed by the eight-part
correlation test at `11.788` seconds and the reversed-type conversion test at
`8.753` seconds.

## Evaluator wall-time recovery — Wave 1

Commit: `perf(evaluator): reuse CSharp compilation runtime` (to be created
after the documentation-inclusive verification below). Wave 1 gives the
production C# target descriptor one process-lifetime
`EvaluatorRuntimeEnvironment`, reusing its thread-local Roslyn workspace and
syntax generator across renders. The metadata reference cache now uses a
normalized path as its bounded key, with length and last-write timestamp in
the value. Stable hits avoid reopening and hashing the DLL; changed files are
recreated and replace the stale bounded entry. Cache mutation remains locked,
reads use the immutable published snapshot, and scoped environments remain
isolated and disposable.

Focused Wave 1 tests passed 25/25. The full Release solution gate passed
16,821 results (16,817 passed, 4 skipped) in `00:04:00.544`; summed individual
durations were `4,191.555` seconds. The evaluator project passed 9,469 tests
with 4 skips in `3:58`. Category totals were 43 benchmark tests at `29.102`
seconds, 1,323 generated-sample tests at `396.501` seconds, 9 integration
tests at `4.412` seconds, 784 repeated-compilation tests at `226.166` seconds,
and 14,662 runtime tests at `3,535.373` seconds. The slowest result was
`PowersOfTwo_AllFormats` at `8.271` seconds, followed by the eight-part
correlation test at `6.856` seconds. No benchmarks, traces, or dedicated
performance acceptance workloads were run in this intermediate wave.

## Evaluator wall-time recovery — Wave 2

Wave 2 changes the execution-compilation cache from provider-object identity to
a semantic contract. The lookup key now uses a coarse, stable provider contract
bucket plus compilation options and execution target. A cache candidate is
validated by a stop-after-planning pass and an exact fingerprint covering the
lowered semantic shape, physical plan, interpreter source, provider contract,
and additional reference types. Current providers and runtime bindings are
still rebound for every cache hit; source-runtime-setting declarations and
values remain ineligible for artifact reuse. Concurrent identical entries use
the existing bounded cache rather than introducing another global cache.

The provider bucket deliberately records only stable provider-owned shape and
configuration. Nested schema runtime counters and mutable setting switches are
not part of that coarse key; the exact post-planning contract remains the
safety boundary. This preserved source-planning request cardinality and the
existing stale-settings lifecycle behavior while allowing equivalent providers
to share compiled artifacts.

Focused Wave 2 gates passed 36/36, plus the five-test target-pipeline cache
suite and the six-test bounded-cache concurrency seam suite. The gates include
source-planning, stale source runtime settings,
provider-contract separation, cache-hit pipeline, and stale source safety
tests. The Release build passed with zero warnings and errors.
The full solution passed 16,822 results: 16,818 passed and 4 skipped. Project
counts were Evaluator 9,469 passed plus 4 skipped, Converter 861, Benchmarks
35, Plugins 4,161, Parser 1,747, Schema 457, CSV 43, and Git 45. The
authoritative evaluator TRX record is
`TestResults/evaluator-wall-wave-2-all-single-flight/wave-2-all-single-flight.trx`:
wall-clock `00:03:16.899`, summed individual test duration `3,242.673`
seconds.
Its slowest tests were `PowersOfTwo_AllFormats` at `6.506` seconds, the
eight-part correlation test at `6.255`, and corpus materialization at `3.919`
seconds. The evaluator category totals were 43
benchmark tests at `26.399` seconds, 1,291 generated-sample tests at
`332.700`, 9 integration tests at `3.811`, 193 repeated-compilation tests at
`39.499`, and 7,937 runtime tests at `2,840.264`.

No BenchmarkDotNet runs, allocation comparisons, worker-count experiments, or
profiler traces were run in this intermediate wave, per the wall-time recovery
protocol. The TRX logger used by the parallel solution invocation writes one
shared file name, so the retained TRX contains the evaluator project; the
console result above records every solution project and the aggregate count.

## Evaluator wall-time recovery — Wave 3

Wave 3 separates execution finalization from portable artifact packaging. The
execution path keeps the emitted CLR and PDB streams attached to the executable
artifact and lets the collectible load context consume them directly; byte
arrays are materialized only when callers request them or when packaging needs
portable blobs. Packaging retains the byte-array artifact and generated-code
hash metadata. Strict artifact validation computes the generated-code hash
explicitly, while ordinary render contributions no longer compute that hash.

The finalization purpose is an internal target contract, so custom targets keep
the existing execution default. Collectible CLR contexts now resolve already
loaded default assemblies through immutable name maps and use the default load
path only as a compatibility fallback; the old linear scan of every default
assembly was removed. Existing PDB, unloadability, artifact byte ownership,
strict hash validation, and custom-target boundaries remain covered.

The focused Wave 3 finalization/loading gate passed 53/53, and the complete
Converter test project passed 861/861. The full Release solution gate passed
16,822 tests: 16,818 passed and 4 skipped. The evaluator project passed 9,469
tests with 4 skipped. The retained evaluator TRX was
`TestResults/evaluator-wall-wave-3-final-final/wave-3-final-final.trx`; the
parallel solution invocation wrote one shared TRX filename, so this retained
file is the evaluator project result while the console output recorded the
other solution projects.

The TRX duration report recorded 3:35.366 wall time and 3,682.079 seconds of
summed individual durations. The slowest tests were the eight-part nested-tuple
correlation test (7.987 seconds) and `PowersOfTwo_AllFormats` (7.985 seconds).
Category totals were runtime 3,249.422 seconds across 7,937 tests,
generated-sample 354.469 seconds across 1,291 tests, repeated-compilation
44.882 seconds across 193 tests, benchmark 28.516 seconds across 43 tests, and
integration 4.790 seconds across 9 tests.

No BenchmarkDotNet, profiler, allocation, or worker-count workload was run in
this intermediate wave, as required by the wall-time recovery protocol.

## Evaluator wall-time recovery — Wave 4

Wave 4 makes semantic compilation work purpose-aware. Ordinary execution,
inspection, and artifact validation no longer compute the future-target
readiness report; portable artifact packaging still computes it because the
package contract consumes those diagnostics. A new internal compilation-purpose
field keeps this decision explicit without changing public APIs. The wave also
builds the immutable semantic metadata snapshot directly from the visitor’s
completed state, avoiding the public defensive-copy getters followed by a
second freeze copy.

Recursive CTE raw-syntax validation is now part of the pre-logical-normalizer
boundary. The compiler and QueryAnalyzer no longer perform a separate
prevalidation traversal before normalization; the diagnostic-aware compiler
uses the normalizer’s combined try path. Existing validation diagnostics and
normalization traces remain covered.

Focused Wave 4 semantic, recursive-normalization, readiness-purpose, and
maintainability-budget tests passed. The full Release solution gate passed
16,822 tests: 16,818 passed and 4 skipped. Project counts were Evaluator 9,469
passed plus 4 skipped, Converter 862, Benchmarks 35, Plugins 4,161, Parser
1,747, Schema 457, CSV 43, and Git 45. The retained evaluator TRX is
`TestResults/evaluator-wall-wave-4-final/wave-4-all-final.trx`.

The TRX duration report recorded 3:33.385 wall time and 3,639.364 seconds of
summed individual durations. The slowest tests were `PowersOfTwo_AllFormats`
at 7.503 seconds, the eight-part nested-tuple correlation test at 6.708
seconds, and two DESC schema tests at 5.256 and 5.225 seconds. Category totals
were runtime 3,209.629 seconds across 7,937 tests, generated-sample 353.382
seconds across 1,291 tests, repeated-compilation 42.392 seconds across 193
tests, benchmark 29.019 seconds across 43 tests, and integration 4.942 seconds
across 9 tests.

No BenchmarkDotNet, profiler, allocation, or worker-count workload was run in
this intermediate wave, as required by the wall-time recovery protocol.

## Evaluator wall-time recovery — Wave 5

Wave 5 batches exhaustive test-only CLR emissions. The recursive optimizer
matrix still exposes all 612 DynamicData cases and performs every original
execution and assertion, but its compatible rendered queries are finalized in
shared Roslyn compilations. Each generated query keeps its own namespace,
runnable entry point, provider binding, and `BuildResult`; a failed shared
emission falls back to per-case finalization so one diagnostic cannot hide the
remaining cases. Queries containing interpreter source are excluded from the
shared group. `PowersOfTwo_AllFormats` now projects all 27 literal expressions
as columns of one query and checks every original value and type assertion.

The focused Wave 5 matrix gate passed all 612 recursive optimizer cases, and the
combined matrix plus number-format test passed 613/613. The Release build and
full solution correctness gate passed 16,823 results: 16,819 passed and 4
skipped, with no failures. Project counts were Evaluator 9,469 passed plus 4
skipped, Converter 862, Benchmarks 35, Plugins 4,161, Parser 1,747, Schema
457, CSV 43, and Git 45.

The documentation-inclusive retained evaluator TRX is
`TestResults/evaluator-wall-wave-5-doc-final/wave-5-doc-final.trx`. Its duration
report recorded wall-clock `00:03:20.9173103` and summed individual durations
of `3,726.941` seconds. Category totals were runtime 3,297.161 seconds across
7,937 tests, generated-sample 348.659 seconds across 1,291 tests,
repeated-compilation 44.024 seconds across 193 tests, benchmark 31.675 seconds
across 43 tests, and integration 5.422 seconds across 9 tests. The first
recursive matrix result includes the one-time batch preparation and emission,
so its TRX duration was `103.894` seconds; this is shared setup charged to one
DynamicData result, not 612 independent active intervals. The next slowest
result was the eight-part correlation test at `7.538` seconds.

No BenchmarkDotNet, profiler, allocation, worker-count, or dedicated wall-time
acceptance workload was run in this intermediate wave, as required by the
protocol. The full-solution TRX logger writes one shared file name for parallel
projects; the retained file is the evaluator project result and the console
output records the aggregate solution count.

## Evaluator wall-time recovery — Wave 6

Wave 6 batches the remaining exhaustive test-only compilation cohorts, limits
batch preparation and finalization to four concurrent operations, and selects
eight MSTest method workers. The recursive optimizer matrix still executes all
612 cases and the other DynamicData groups retain their original case counts.
The measurement reporter now uses the monitor's `runs.json` wall-clock values
when available and sums CPU deltas per monitored process lifetime; it no longer
subtracts unrelated testhost counters or treats inflated TRX timestamps as
wall time.

The worker experiment was run before the final three-run measurement, with the
same Release evaluator binary and no telemetry:

| MSTest workers | Wall seconds | Peak private memory |
| ---: | ---: | ---: |
| 4 | 203.849 | 1,701 MB |
| 8 | 178.490 | 1,448 MB |
| 12 | 183.625 | 1,990 MB |
| 16 | 188.600 | 1,503 MB |
| 24 | 188.317 | 1,843 MB |

Eight workers were retained. The final no-telemetry evaluator runs used:

```powershell
powershell -NoProfile -File scripts/measure-evaluator-tests.ps1 `
  -RunCount 3 `
  -ResultsRoot TestResults/evaluator-wall-wave-6-final-three-runs `
  -NoBuild -SampleMilliseconds 1000

powershell -NoProfile -File scripts/report-evaluator-measurements.ps1 `
  TestResults/evaluator-wall-wave-6-final-three-runs
```

The three measured process wall times were `180.721`, `182.490`, and
`182.303` seconds, median `182.303` seconds. All three runs passed 9,469 tests
and skipped 4. Across the three TRXs there were 28,419 results, 28,407 passes,
12 skips, and no failures; summed individual durations were `3,907.888`
seconds. The process monitor reported `1,576.594` aggregate CPU seconds,
`1,775` MB peak private memory, and `39.3%` average system CPU. The summed
test duration is not a wall-time estimate: DynamicData timestamps overlap and
2,154 of 28,419 timestamp intervals were classified as inflated, with a
maximum inflation of `5,292x`.

The final telemetry run is in
`TestResults/evaluator-wall-wave-6-final-telemetry`. It completed in `185.264`
seconds with 9,469 passes and 4 skips. It recorded 771 explicit case scopes
and 4,872 compilation events: 329 cache hits, 4,197 misses, and 346
ineligible compilations. Explicit case activity was `153.893` seconds and
the remaining `31.371` seconds is unobserved by case telemetry, not proven
idle. Compilation phase totals are overlapped CPU work rather than wall time;
the largest totals were parse `320.434s`, interpretation schema `320.071s`,
semantic pipeline `318.786s`, emission `149.774s`, load `88.989s`, and runnable
creation `43.052s`. The dominant measured cohort was the 612-case recursive
optimizer matrix at `153.69s`, followed by special-character cases at
`11.04s`, recursive UNION ALL at `9.21s`, private window benchmark cases at
`8.77s`, and recursive generated samples at `6.31s`.

The final attached trace is
`TestResults/evaluator-wall-wave-6-final-trace/run-01/trace.nettrace` and was
collected successfully for `204.698` seconds. It is `986,334,162` bytes and
is ignored by source control. `dotnet-trace report ... topN` failed with
`System.FormatException: Read past end of stream` while converting the
EventPipe file, so no method ranking is claimed from that trace. The process
and telemetry evidence did show continuous CPU activity in the affected
cohorts: the telemetry run had zero low-CPU samples, `556.906` aggregate CPU
seconds, and `41.3%` average system CPU.

The final Q227–Q230 BenchmarkDotNet commands were run three times in separate
directories with `--job short --memory --exporters json`. Reports are under:

- `TestResults/evaluator-wave-6-bdn-1/results/Musoq.Benchmarks.EvaluatorPerformanceSamplesBenchmark-report-full-compressed.json`
- `TestResults/evaluator-wave-6-bdn-2/results/Musoq.Benchmarks.EvaluatorPerformanceSamplesBenchmark-report-full-compressed.json`
- `TestResults/evaluator-wave-6-bdn-3/results/Musoq.Benchmarks.EvaluatorPerformanceSamplesBenchmark-report-full-compressed.json`

The median of the three report medians was:

| Workload | Time | Allocation |
| --- | ---: | ---: |
| Q227, 1k | 402.134 us | 508.51 KB |
| Q228, 1k | 1.351 ms | 2,546.44 KB |
| Q229, 1k | 579.277 us | 805.27 KB |
| Q230, 1k | 134.880 us | 290.54 KB |
| Q227, 10k | 10.135 ms | 895.29 KB |
| Q228, 10k | 14.457 ms | 17,728.45 KB |
| Q229, 10k | 6.188 ms | 4,164.11 KB |
| Q230, 10k | 242.281 us | 949.20 KB |

The comparator self-check on identical three-report sets passed at `1.0000x`
time and allocation. A cross-wave comparator result cannot be reproduced from
the ignored raw Wave 0–4 JSON files in this checkout; the documented Wave 4
reference is not substituted for a raw three-run baseline. Therefore this
Wave 6 record makes no false 1.03x before/after claim. The short-run results
were noisy enough that Q229's 10k median was about `1.038x` the documented
Wave 4 reference, so it is not treated as a clean acceptance pass.

The wall-time budget remains unmet: the final median is approximately three
times the preferred 60-second target and above the acceptable 70-second limit.
The most useful disproven hypotheses were that additional batch preparation
parallelism would reduce the full tier (degree 8 did not beat degree 4) and
that concurrent batch finalization alone would materially reduce full-tier
wall time (it improved the isolated recursive batch, but not the full run).
The evidence now points to real CPU-heavy execution across the exhaustive
recursive matrix and other broad runtime cohorts, not a long idle gap or a
TRX-only timestamp artifact. The final correctness gate is required before
the Wave 6 commit; dedicated performance acceptance remains recorded as
failed rather than hidden.

The exact-tree correctness gate then passed with zero build warnings or errors:

```powershell
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet
dotnet test src/dotnet/Musoq.sln -c Release --no-build --nologo --verbosity quiet `
  --logger "console;verbosity=minimal" `
  --logger "trx" `
  --results-directory "TestResults/evaluator-wall-wave-6-final-gate"
powershell -File scripts/report-trx-durations.ps1 `
  -Path TestResults/evaluator-wall-wave-6-final-gate -Top 30
```

The solution gate recorded 16,823 results: 16,819 passed and 4 skipped, with
zero failures. Its wall clock was `00:03:32.4097038` and the sum of all
individual TRX durations was `1,806.362` seconds. The evaluator project
recorded 9,469 passed and 4 skipped in approximately `3:30`. Category totals
were runtime `1,340.193s` across 14,662 results, generated-sample `249.961s`
across 1,323 results, repeated-compilation `202.710s` across 786 results,
benchmark `11.270s` across 43 results, and integration `2.228s` across 9
results. The full-solution TRX directory is
`TestResults/evaluator-wall-wave-6-final-gate`.

## Recursive optimizer-matrix recovery — Wave 1

Wave 1 changes only the internal exhaustive-compilation batch activation
boundary. A compatible Roslyn batch is now represented by one assembly artifact
and loaded into one collectible `AssemblyLoadContext`. Every generated runnable
keeps its own provider, settings, parameters, logger, and source-plan binding,
but owns a reference-counted lease on the shared context. Disposing one query
does not unload siblings; the final query disposal unloads the context. A
missing runnable type is reported for that item without hiding successfully
activated siblings, while an assembly-level activation failure retains the
existing per-query compilation fallback. Ordinary single-query activation is
unchanged.

Focused validation passed seven CLR activation tests, including shared-context
identity, independent provider data, partial activation failure, disposal
ordering, and final unloadability. The complete 612-case recursive optimizer
matrix also passed as a correctness check. No dedicated performance workload,
BenchmarkDotNet run, profiler trace, or acceptance comparison was performed in
this intermediate wave.

The pre-documentation Release gate passed after the concrete activation call
was moved behind the existing C# compatibility owner. Its project totals were
16,826 results: 16,822 passed and 4 skipped, with zero failures. Evaluator
recorded 9,469 passed and 4 skipped in approximately 3 minutes 17 seconds;
Converter recorded 865 passed. The retained TRX directory is
`TestResults/evaluator-recursive-wave-1-pre-doc-green`.

The first attempted full gate found one architecture-guardrail violation:
`ExecutionBatchCompilation` directly imported the concrete C# target namespace.
That hypothesis was rejected rather than allowlisted. The concrete dependency
now remains in `BuildItems.Rendering.cs`, the established compatibility owner,
and the target-neutral batch coordinator consumes only converter-local
activation requests and results.

## Recursive optimizer-matrix recovery — Wave 2

Wave 2 makes ownership of test-only batched queries explicit. The new
`CompiledQueryBatchRepository<TKey>` lazily creates a batch, atomically
transfers each query exactly once through `Take`, and disposes only unclaimed
queries during class cleanup. Cleanup does not force an uninitialized batch,
which preserves filtered-test behavior. Duplicate consumption and missing keys
now fail explicitly instead of returning a shared or already-disposed query.

The recursive UNION ALL, recursive optimizer, recursive generated-sample, and
special-character cohorts now use the repository. Every DynamicData case
disposes its materialized `Table` before disposing its `CompiledQuery`, so
assertion failures, cancellation, and materialization exceptions unwind both
the deferred result lease and the shared assembly-context lease. Batch factory
failures also dispose every successfully compiled sibling before propagating.

Focused validation passed five repository ownership/concurrency tests and all
749 affected DynamicData cases. The repository tests cover concurrent and
duplicate takes, missing keys, uninitialized cleanup, unclaimed-entry cleanup,
idempotent cleanup, and the rule that a claimed query remains caller-owned.
The affected real-query tests exercise deferred result materialization and
class cleanup against the shared collectible context introduced in Wave 1.

The pre-documentation Release gate passed 16,831 results: 16,827 passed and 4
skipped, with zero failures. Evaluator recorded 9,474 passed and 4 skipped in
approximately 3 minutes 29 seconds; Converter recorded 865 passed. The retained
TRX directory is `TestResults/evaluator-recursive-wave-2-pre-doc`. No dedicated
performance workload, BenchmarkDotNet run, or profiler trace was performed in
this intermediate wave.

## Recursive optimizer-matrix recovery — Wave 3

Wave 3 changes the recursive optimizer matrix from one eager 612-query batch to
nine lazy profile repositories of 68 queries each. DynamicData is now
profile-major while preserving every original `(case, profile)` value and all
612 test results. A process-local semaphore permits only one profile batch to
prepare and finalize at a time; each batch already uses four-way internal
parallelism. Queries transferred from an earlier profile can be materialized
and disposed while a later profile is being compiled.

Each profile stores compilation success or failure by case key. Requesting a
failed case reports that case's original exception without poisoning successful
siblings or another profile. Repositories for profiles not requested by an
in-process filtered path remain uninitialized and class cleanup does not force
them. MSTest's command-line filter does not expose individual DynamicData
arguments such as `hash-only`, so command-line selection still operates at the
612-case parent-method level; the lazy exclusion behavior is validated directly
at the repository boundary.

Focused validation passed eight ownership, failure-isolation, and matrix-order
tests, then all 612 recursive optimizer cases. The profile-major guardrail
asserts exact `68 x 9` cardinality, stable catalog ordering within each profile,
and the original profile objects. Repository tests prove one factory invocation
under concurrent access, per-entry failure isolation, and that unused profile
repositories are not initialized.

The pre-documentation Release gate passed 16,834 results: 16,830 passed and 4
skipped, with zero failures. Evaluator recorded 9,477 passed and 4 skipped in
approximately 3 minutes 22 seconds; Converter recorded 865 passed. The retained
TRX directory is `TestResults/evaluator-recursive-wave-3-pre-doc`. No dedicated
performance workload, BenchmarkDotNet run, or profiler trace was performed in
this intermediate wave.

## Recursive optimizer-matrix recovery — Wave 4

Wave 4 gives instrumentation-enabled compilations two generated execution
entry paths from the same execution plan. Normal `Run` and contextual `Run`
call an unprofiled body with no `QueryProfileRecorder` parameter, operator
scope, row counter, nullable-recorder branch, or profiling `try/finally`.
`RunWithProfile` calls the `_Profiled` body and retains source/operator timing,
row counts, exception attribution, and cancellation behavior.

Generated row carriers, column metadata, static fields, and identical helper
members are shared. The renderer compares generated member identities and
syntax before reuse; a same-signature member with different bodies is an
internal generation error instead of a silent collision. Parallel execution
requires distinct helper bodies, so only profiled build functions and runner
classes receive the `_Profiled` suffix. This resolved the parallel-CTE
constructor ownership conflict while keeping the normal runner recorder-free.

Eleven profiled snapshots changed; the two snapshots whose queries do not emit
execution instrumentation remained byte-identical. Shape guardrails now inspect
the profiled method explicitly and verify suffixed parallel helpers. A new
syntax-tree test proves all normal `Run` methods avoid profiled calls and
recorder symbols while `RunWithProfile` selects the profiled entrypoint.

Focused validation passed all 866 converter tests, 39 profiled
snapshot/shape/integration tests with one ignored refresh utility, and all 612
recursive optimizer cases plus the matrix cardinality guard. The recursive
focused run completed with 613 passes in 22 seconds; this is a correctness-run
observation, not the dedicated performance acceptance measurement reserved for
Wave 5.

The pre-documentation Release gate passed 16,835 results: 16,831 passed and 4
skipped, with zero failures. Evaluator recorded 9,477 passed and 4 skipped in
approximately 2 minutes 53 seconds; Converter recorded 866 passed. The retained
TRX directory is `TestResults/evaluator-recursive-wave-4-pre-doc`. No dedicated
performance workload, BenchmarkDotNet run, profiler trace, or acceptance
comparison was performed in this intermediate wave.

## Recursive optimizer-matrix recovery — Wave 5

Wave 5 ran the dedicated performance work on Windows 11 using .NET SDK
`10.0.302` and .NET runtime `10.0.10`. The campaign baseline commit was
`44a5da2355a54ee920422a68c4e2efed452cec12`; the final measurements were made
from the Wave 4 tree plus the Wave 5 changes described below. Raw BenchmarkDotNet,
TRX, process, telemetry, and trace artifacts remain ignored under
`TestResults`.

The first degree-4 final measurement established an isolated three-run median
of `23.835` seconds and a full evaluator median of `148.304` seconds. Its
telemetry run attributed `16.472` seconds to batch setup, which missed the
`15.2`-second setup requirement. The profile-streamed batches retain only 68
queries at a time, so the preparation fan-out was increased from four to eight
while Roslyn finalization remained bounded at four. The degree-8 telemetry
experiment reduced setup to `14.714` seconds with `433 MB` peak private memory.

Final evidence identified one additional ordinary-compilation cost introduced
by Wave 4. Structural member identities and Roslyn syntax equivalence were
being computed for every generated member even when instrumentation was
disabled. Uninstrumented rendering now uses the original class-name lookup;
strict structural deduplication and conflicting-body detection remain in the
dual profiled/unprofiled path. This restored the generated-C#-only benchmark
without weakening the profiled renderer's ownership checks.

### Final matrix and full-tier results

The final telemetry-disabled isolated matrix runs are under
`TestResults/evaluator-recursive-wave-5-final-fast-matrix-three-runs`. Every
run contains all 612 DynamicData cases:

| Metric | Confirmed baseline | Final | Ratio |
| --- | ---: | ---: | ---: |
| Isolated wall median | 42.937 s | 22.983 s | 0.535 |
| Batch setup | 21.749 s | 14.803 s | 0.681 |
| Execution | 1.542 s | 0.618 s | 0.401 |
| Materialization | 15.808 s | 3.688 s | 0.233 |
| Peak private memory | 1,541 MB | 433 MB | 0.281 |

The three isolated wall times were `23.045`, `22.836`, and `22.983` seconds.
All 1,836 aggregate results passed. The final telemetry run is retained at
`TestResults/evaluator-recursive-wave-5-final-fast-matrix-telemetry`; all 612
case scopes closed, every materialization completed, and the
`full-instrumentation` profile contributed only `0.417` seconds of
materialization. Setup is below `15.2` seconds, the isolated median is below
`30` seconds, and private memory is below `1.20 GB`.

The final telemetry-disabled evaluator runs are under
`TestResults/evaluator-recursive-wave-5-final-fast-full-three-runs`:

| Run | Wall seconds | Results |
| ---: | ---: | ---: |
| 1 | 145.842 | 9,477 passed, 4 skipped |
| 2 | 145.036 | 9,477 passed, 4 skipped |
| 3 | 146.956 | 9,477 passed, 4 skipped |

The `145.842`-second median is `0.800x` the retained `182.303`-second baseline
and below the preferred `170` seconds. The process monitor recorded `1,369 MB`
peak private memory, `1,401.563` aggregate CPU seconds, `41.8%` average system
CPU, three low-CPU samples, and three testhost starts. Across all three TRXs,
28,431 tests passed, 12 were skipped, and none failed.

The final full telemetry run at
`TestResults/evaluator-recursive-wave-5-final-fast-full-telemetry` completed in
`149.888` seconds with 9,477 passes and 4 skips. It recorded 771 explicit case
scopes and 4,872 compilation events: 329 cache hits, 4,197 misses, and 346
ineligible compilations. Process CPU was `475.281` seconds, peak private memory
was `1,113 MB`, average system CPU was `37.8%`, and only one sample was
low-CPU. The remaining full-tier critical path is therefore active evaluator
work outside this 612-case campaign, not a long process-wide idle gap.

One earlier degree-8 full attempt aborted after 7,562 results with CLR error
`0x80131506`, while `dotnet test` incorrectly returned exit code zero. Three
subsequent pre-fast-path attempts and all three final attempts completed.
`measure-evaluator-tests.ps1` now validates that every emitted TRX has outcome
`Completed`, contains results, and has no failed counters before accepting a
run. The validation was exercised against both the real aborted TRX and a
complete 9,481-result TRX.

### Benchmark acceptance

Benchmark comparisons used detached sibling worktrees for the exact campaign
base and current commits. This avoided the repeatable skew observed when the
IDE-watched primary checkout was compared with an unwatched detached checkout.
The standard median-of-three Q227-Q230 cohort passed seven of eight methods;
the sub-millisecond Q227-1k result was resolved with three focused reports using
ten iterations and five warmups.

| Q workload | Time ratio | Allocation ratio |
| --- | ---: | ---: |
| Q227, 1k | 1.0038 | 1.0000 |
| Q227, 10k | 1.0027 | 1.0000 |
| Q228, 1k | 1.0073 | 1.0000 |
| Q228, 10k | 0.9864 | 1.0000 |
| Q229, 1k | 0.9940 | 1.0000 |
| Q229, 10k | 0.9767 | 1.0000 |
| Q230, 1k | 1.0048 | 1.0000 |
| Q230, 10k | 1.0044 | 1.0000 |

The controlled compilation reports are under
`TestResults/evaluator-recursive-wave-5-controlled-{baseline,current}-compilation-*`.
Cold compilation, cache hit, cache-ineligible compilation, assembly emission,
and typed artifact load all stayed within the `1.03` time/allocation ceiling.
The short generated-C#-only reports were unstable between approximately 5 and
16 milliseconds, so that method was repeated in three focused ten-iteration
reports. The final comparison was `0.9947x` time and `1.0001x` allocation.
Generated Q227 code and all unprofiled generated snapshots remained
byte-identical across the campaign.

### Trace and rejected hypotheses

The final focused trace is
`TestResults/evaluator-recursive-wave-5-final-fast-matrix-trace/run-01/trace.nettrace`.
Its 612 tests passed and the 40,905,592-byte collection log reports
`Trace completed.` However, `dotnet-trace report ... topN -n 30` fails with
`System.FormatException: Read past end of stream`, as did the earlier trace.
No method ranking is claimed from the unreadable trace. Process and explicit
case telemetry provide the accepted CPU, memory, setup, execution, and
materialization evidence.

The hypothesis that Q227 execution had regressed was rejected: generated code
was unchanged, allocations were identical, and controlled sibling-worktree
reports were within `1.0038x`. The hypothesis that the stale compilation
cache-hit report represented this campaign was also rejected because that
baseline predated the campaign; exact-commit controlled reports passed.
Conversely, dormant structural renderer work was confirmed by the
generated-C#-only comparison and removed.

This campaign stops at the intended boundary. It recovered the recursive
matrix and improved the complete evaluator tier, but the full tier still takes
about 146 seconds. Reducing the remaining 8,861-test path toward 60-70 seconds
requires a separate evidence-driven campaign rather than expanding recursive
batch ownership or reducing coverage here.

The first documentation-inclusive full Release gate is retained at
`TestResults/evaluator-recursive-wave-5-pre-commit`. It passed 16,831 of
16,835 solution results with 4 skipped and no failures. The TRX wall span was
`00:02:56.077` and summed individual durations were `1,636.140` seconds.
Category totals were 43 benchmark tests at `10.465` seconds, 1,323
generated-sample tests at `186.249` seconds, 9 integration tests at `2.044`
seconds, 790 repeated-compilation tests at `203.784` seconds, and 14,670
runtime tests at `1,233.598` seconds. Evaluator contributed 9,477 passes and
4 skips; Converter contributed 866 passes.

## Evaluator compilation batching and artifact isolation — Wave 0

Wave 0 establishes the measurement and correctness boundary for the next
campaign. The retained baseline is the final recursive-matrix measurement:
the evaluator median was `145.842` seconds, the telemetry run was `149.888`
seconds, and the run recorded 4,872 compilation events. The provider report
identified 4,113 events with actual CLR emission, including 2,575
`BasicSchemaProvider<BasicEntity>` emissions. These are aggregate compilation
times observed while tests run in parallel and are not wall-clock sums.

The opt-in compilation telemetry now records the consumer family, compilation
mode, batch identity and size, reuse path, artifact and binding fingerprints,
and whether an event performed real emission or loading. Batch finalization is
recorded separately in `execution-batches.jsonl`, so preparation events cannot
be mistaken for independent Roslyn assemblies. Query text and source data are
never written to telemetry.

The measurement reporter now emits actual-emission and actual-load counts,
compilation modes, and batch totals in `measurement-summary.json`. Existing
telemetry-disabled execution remains the no-op path.

Wave 0 also adds concurrent binding-isolation characterization coverage. The
same query is compiled and executed concurrently against distinct providers;
each result must contain only its provider's sentinel value. Existing batch
activation tests continue to cover shared collectible contexts, independent
bindings, partial activation failure, and unload after the final lease.

Focused Converter validation passed 13 tests. The full Release solution gate
at `TestResults/evaluator-compilation-wave-0` passed 16,832 of 16,836
results, with 4 skipped and no failures. Evaluator contributed 9,477 passed
and 4 skipped; Converter contributed 867 passed. The generated measurement
report also confirmed that telemetry-disabled tests produced no compilation
or batch telemetry files. No production compilation behavior is changed by
this wave.

## Evaluator compilation batching and artifact isolation — Wave 3

Wave 3 reuses the same bounded coordinator for stable Generic schema execution
and Binary/Text SQL execution helpers. The Generic helper preserves its current
schema objects, row-source filters, libraries, and logger through a fresh
binding. Binary/Text helpers likewise pass their current provider and options
to each request. The separate interpreter-code compiler remains on its own
path because it does not produce an `ITableRunnable` execution artifact. A
static binary regression helper also remains explicitly single-query because
it has no test-instance ownership boundary for batched leases.

All batched queries are tracked by the owning test instance and disposed during
test cleanup. Stable family requests share only compatible generated execution
artifacts; providers, filters, logger instances, source enumerators, runnable
instances, and tables are never shared. The production compilation source tree
continues to contain no reference to the test coordinator.

Focused Wave 3 validation passed 171 tests across the coordinator, Generic,
Binary, and Text execution families. The Release build passed with zero
warnings and errors. The documentation-inclusive full solution gate at
`TestResults/evaluator-compilation-wave-3` passed 16,840 of 16,844 results,
with 4 skipped and no failures. Evaluator contributed 9,483 passed and 4
skipped; Converter contributed 869 passed. The evaluator testhost reported
`2m 11s`; the TRX duration report measured a maximum `131.938` second span and
`1,292.500` seconds of summed individual durations. Telemetry was disabled,
and no dedicated benchmark or trace acceptance was run in this intermediate
wave.

## Evaluator compilation batching and artifact isolation — Wave 4

Wave 4 adds a production-only canonical executable-artifact index over the
existing bounded execution-cache entries. The cached value is now an
immutable `PreparedExecutableTemplate`; every exact or canonical hit creates
a fresh runnable and a fresh `QueryRuntimeBinding` for the current provider,
source settings, and logger. No provider, parameter, setting, logger,
enumerator, table, result, or runnable instance is retained by the cache.

Canonical identity includes normalized generated syntax, the semantic
execution contract, runtime and execution-semantics versions, target and
result mode, output type, compilation options, ordered references, provider
shape, and interpreter state. Only the generated assembly/type identity is
normalized through the C# compatibility shim. The canonical index is bounded
to 2,048 aliases over the existing 512 owned entries, and canonical misses
use single-flight finalization. Instrumented, debugger, source-runtime-setting,
interpreter, non-CLR, non-table, output-type, and PDB compilations remain
ineligible.

Correctness coverage includes whitespace-equivalent artifact reuse with
current-provider rebinding, concurrent canonical single-flight, literal
separation, instrumentation exclusion, exact-cache binding isolation,
source-settings isolation, shared-context lifecycle, partial activation, and
prepared-template immutability. The Roslyn syntax work remains inside the
C# compatibility boundary and the architecture guardrails remain green.

Focused Wave 4 validation passed 10 TargetPipeline tests and 3 Q228-Q230
sample correctness tests. The Release build passed with zero warnings and
errors. The final documentation-inclusive full solution gate at
`TestResults/evaluator-compilation-wave-4-exact` passed 16,844 of 16,848
results, with 4 skipped and no failures: Evaluator 9,483 passed plus 4
skipped, Converter 873 passed, and all other projects passed. The TRX report
measured `00:02:09.432` wall-clock duration and `1,302.065` seconds summed
individual durations. Category totals were 43 benchmark tests at `3.453`
seconds, 1,323 generated-sample tests at `90.183` seconds, 9 integration
tests at `1.571` seconds, 792 repeated-compilation tests at `220.593`
seconds, and 14,681 runtime tests at `986.264` seconds.

Per the wave protocol, dedicated wall-time measurements, BenchmarkDotNet,
worker experiments, and traces remain deferred to Wave 5.

## Evaluator compilation batching and artifact isolation — Wave 5

Wave 5 completed the dedicated measurements and corrected the compilation
benchmark's cold cases. Wave 4 canonical artifact reuse makes an otherwise
identical query a cache hit, so the benchmark now adds a unique,
semantics-preserving string equality predicate to each cold query. This keeps
the cold, eligible-hit, and cache-ineligible cases distinct without changing
the returned rows.

The three no-telemetry evaluator measurements all passed 9,483 tests and
skipped 4:

| Run | Wall seconds | Peak private memory |
| ---: | ---: | ---: |
| 1 | 138.635 | approximately 1,366 MB |
| 2 | 158.523 | approximately 1,366 MB |
| 3 | 136.941 | approximately 1,366 MB |

The median was `138.635` seconds. This remains above the preferred `60`
seconds and required `70` seconds, so the Wave 5 wall-time acceptance target
was not met. The evaluator test result count and coverage were unchanged.

The worker sweep was run once per setting using the process monitor. The
selected setting was 16 workers: `102.061` seconds and approximately `931`
MB peak private memory. Twenty-four workers measured `101.994` seconds but
used approximately `1,472` MB peak private memory, so the negligible `0.067`
second gain was rejected under the memory constraint. The other measurements
were 4 workers `174.696` seconds/`1,375` MB, 8 workers `134.392`/`1,392` MB,
and 12 workers `115.126`/`1,517` MB. These are worker experiments, not a
change to the repository's default test command.

The telemetry-enabled evaluator run completed in `139.234` seconds. It
recorded 5,765 compilation events, 132 cache hits, 876 misses, 3,608
ineligible compilations, 721 batch events, and 3,258 batched items. After
counting both ordinary and batch finalization events, it recorded 711 actual
assembly emissions and 711 actual loads. The largest measured compilation
stage totals were parse `196.633` seconds, interpretation-schema `195.454`,
semantic pipeline `194.033`, emission `76.392`, runnable loading `41.799`,
and runnable creation `7.546`; these are parallel event totals, not wall
time. The process monitor reported approximately `397.250` aggregate CPU
seconds and `1,270` MB peak private memory for this run. Telemetry remains
opt-in.

The final representative trace run passed 9,483 tests and skipped 4 in
`162.958` seconds. It recorded `1,237.287` seconds of summed TRX durations,
`593.891` aggregate process CPU seconds, approximately `1,399` MB peak
private memory, zero low-CPU samples, and one testhost start. The trace is
`TestResults/evaluator-compilation-wave-5-trace/run-01/trace.nettrace`.
The installed `dotnet-trace report topN` parser failed with an end-of-stream
error on this 838 MB file; no method-level ranking is claimed from it. The
process and TRX measurements remain valid.

Three compilation BenchmarkDotNet reports were run under
`TestResults/evaluator-compilation-wave-5-benchmark-compilation-{1,2,3}`.
The median-of-three results were:

| Workload | Time | Allocation |
| --- | ---: | ---: |
| Simple cold compile | 8.282 ms | 3,476.62 KB |
| Eligible canonical cache hit | 978.6 us | 860.88 KB |
| Complex cold compile | 44.393 ms | 5,633.4 KB |
| Cache-ineligible compile | 71.367 ms | 5,120.77 KB |
| Typed artifact load and run | 15.340 ms | 4,766.66 KB |
| Simple generated C# only | 9.013 ms | 2,476.05 KB |
| Complex generated C# only | 11.589 ms | 4,307.24 KB |
| Simple emitted DLL only | 44.842 ms | 4,904.38 KB |
| Complex emitted DLL only | 69.932 ms | 7,376.14 KB |

Three Q227-Q230 BenchmarkDotNet reports were run under
`TestResults/evaluator-compilation-wave-5-benchmark-q227-q230-{1,2,3}`.
The median-of-three results were:

| Workload | Time | Allocation |
| --- | ---: | ---: |
| Q227, 1k | 388.4 us | 508.51 KB |
| Q228, 1k | 1.277 ms | 2,546.43 KB |
| Q229, 1k | 535.6 us | 805.27 KB |
| Q230, 1k | 132.6 us | 290.54 KB |
| Q227, 10k | 10.037 ms | 895.29 KB |
| Q228, 10k | 14.345 ms | 17,728.05 KB |
| Q229, 10k | 6.241 ms | 4,164.08 KB |
| Q230, 10k | 264.4 us | 949.20 KB |

The raw three-run reports are intentionally ignored. No cross-wave 1.03x
claim is made because the prior three-run JSON artifacts are not available in
this checkout; the identical-report comparator self-check remains the
comparison-infrastructure validation. The Q227-Q230 results are recorded for
the next optimization campaign.

The first documentation-inclusive correctness gate was run under
`TestResults/evaluator-compilation-wave-5-final`; it passed the complete
solution with `16,848` results, `16,844` passed, `4` skipped, and no failures.
After the exact documentation tree was finalized, the build and full gate were
rerun under `TestResults/evaluator-compilation-wave-5-final-tree`. That final
gate had the same counts and no failures; its TRX duration report measured
`00:02:02.174` wall-clock time and `1,204.453` seconds summed individual
durations. Category totals were runtime `938.012` seconds across 14,681
results, generated-sample `67.993` seconds across 1,323 results,
repeated-compilation `193.569` seconds across 792 results, benchmark `3.806`
 seconds across 43 results, and integration `1.074` seconds across 9 results.

## Compilation front-end attribution — Wave 0

Wave 0 changes only measurement boundaries and maintainability structure. The
`parse`, `interpretation-schema`, and `semantic-pipeline` scopes now close
before their successor stages run. Semantic compilation also records exclusive
subphases for normalization, raw-column extraction, metadata binding, CTE
facts, rewrite, artifact freezing, source-contract validation, logical
planning, physical planning, planning text, execution IR, and rendering.

`compilation-stages.jsonl` keeps the existing numeric `phases` values as
exclusive milliseconds and adds `phaseDetails` with inclusive milliseconds,
exclusive milliseconds, invocation count, and maximum inclusive duration. The
measurement reporter now reports both totals and explicitly labels the nested
inclusive overcount; stage comparisons must use exclusive totals.

The previous telemetry totals of parse `196.633s`, interpretation-schema
`195.454s`, and semantic pipeline `194.033s` were inclusive parallel event
totals and must not be interpreted as three independent pipelines. Dedicated
benchmark and trace baselines for the corrected boundaries are deferred to the
final wave; raw measurement artifacts remain ignored.

Wave 0 exact-tree gate: `TestResults/evaluator-frontend-wave-0`. Release build
passed with no warnings or errors. The complete solution passed with `16,844`
tests, `4` skipped, and no failures (`16,848` total). TRX reporting measured
`106.380s` wall-clock and `2,545.058s` summed individual durations. Category
totals were runtime `2,076.392s` across 14,681 results, repeated-compilation
`241.061s` across 792 results, generated-sample `213.533s` across 1,323
results, benchmark `11.524s` across 43 results, and integration `2.547s` across
9 results. The slowest individual result was a recursive optimizer case;
generated-corpus materialization was the slowest non-recursive setup test.

After the documentation-inclusive rerun, `TestResults/evaluator-frontend-wave-0-final-tree`
also passed with `16,844` passed, `4` skipped, and no failures. Its TRX report
measured `108.648s` wall-clock and `2,676.757s` summed individual durations;
category sums were runtime `2,200.673s`, repeated-compilation `237.369s`,
generated-sample `224.514s`, benchmark `12.083s`, and integration `2.118s`.

## Parser hot-path optimization — Wave 1

Wave 1 removes regex matching from normal lexer execution for string literals,
base-prefixed numbers, hash sources, comments, bracketed names, and multi-word
keywords. Schema-method column consumption uses the same direct scanner for the
built-in lexer; the `ILexer.NextOf` regex method remains only as compatibility
fallback for custom lexer implementations. Per-parser arithmetic precedence is
now a static switch, so parsing no longer creates a precedence dictionary per
parser instance.

The Wave 1 rerun under `TestResults/evaluator-frontend-wave-1-rerun` passed
`16,844` tests with `4` skips and no failures. TRX reporting measured
`99.904s` wall-clock and `2,473.921s` summed individual durations. Category
sums were runtime `2,027.067s`, repeated-compilation `237.446s`,
generated-sample `197.193s`, benchmark `10.220s`, and integration `1.994s`.
This is recorded as an observed full-tier result; dedicated parser and frontend
benchmarks remain deferred to Wave 7.

The documentation-inclusive Wave 1 rerun under
`TestResults/evaluator-frontend-wave-1-final-tree` passed with `16,844` passed,
`4` skipped, and no failures. Its TRX report measured `100.056s` wall-clock and
`2,408.182s` summed individual durations. Category sums were runtime
`1,977.046s`, repeated-compilation `217.054s`, generated-sample `200.201s`,
benchmark `10.913s`, and integration `2.967s`.

The final documentation-only verification under
`TestResults/evaluator-frontend-wave-1-final-docs` was also green with
`16,844` passed, `4` skipped, and no failures. It measured `103.043s`
wall-clock and `2,452.344s` summed individual durations; category sums were
runtime `2,004.847s`, repeated-compilation `227.501s`, generated-sample
`206.471s`, benchmark `11.088s`, and integration `2.437s`.

## Parsed query template reuse — Wave 2

Wave 2 adds a bounded process-local parsed-template cache keyed by the exact
script and parser-contract version. A successful cache entry retains only the
unbound AST; every caller receives a deep clone through the existing clone
visitor. Concurrent identical requests use `Lazy<T>` single-flight creation,
failed parses are removed, eviction is FIFO, and both entry count and retained
script text are bounded. The normal parser path remains available through the
same `CreateTree` build boundary, and source text is recreated per compilation.

The first full run exposed a real clone correctness gap: CTE and CTE-inner
spans were not copied, changing a recursive diagnostic span from `counter` to
`Value`. Span/full-span and CTE-column-array preservation were added before the
green rerun; the defect is covered by the recursive diagnostic suite.

The corrected Wave 2 run under `TestResults/evaluator-frontend-wave-2-rerun2`
passed `16,848` existing results plus `4` cache tests, with `4` skips and no
failures. TRX reporting measured `100.620s` wall-clock and `2,447.964s`
summed individual durations. Category sums were runtime `2,015.589s`,
repeated-compilation `217.993s`, generated-sample `200.213s`, benchmark
`12.244s`, and integration `1.926s`.

## Interpretation-schema preprocessing — Wave 3

Wave 3 partitions top-level interpretation-schema declarations and executable
statements in one pass. Declaration registration still uses the existing
`SchemaDefinitionVisitor`, so duplicate definitions, forward references, type
validation, ordering, and diagnostics retain their prior behavior. The
dependency graph now scans a shallow root containing only executable
statements, and declaration removal reuses that same filtered tree. Queries
without declarations return immediately with the original tree and an empty
registry; they no longer construct a schema-definition traversal visitor or
run the dependency graph.

Focused partition, schema-definition, and converter inspection tests passed,
including binary/text ordering, all-declaration handling, source-span
preservation, dependency reachability, and unused-schema source elimination.

The Wave 3 full gate under `TestResults/evaluator-frontend-wave-3` passed
`16,852` tests and skipped `4`, with no failures (`16,856` total). TRX
reporting measured `115.198s` wall-clock and `2,730.825s` summed individual
durations. Category sums were runtime `2,246.191s` across `14,689` results,
repeated-compilation `245.179s` across `792`, generated-sample `224.226s`
across `1,323`, benchmark `12.685s` across `43`, and integration `2.543s`
across `9`. The slowest results remained recursive optimizer cases; no
dedicated performance benchmark or profiler claim is made in this wave.

After the documentation-inclusive rerun, the final-tree gate under
`TestResults/evaluator-frontend-wave-3-final-tree` remained green with
`16,852` passed, `4` skipped, and no failures. Its TRX report measured
`116.340s` wall-clock and `2,716.179s` summed individual durations. Category
sums were runtime `2,232.576s`, repeated-compilation `242.577s`,
generated-sample `224.204s`, benchmark `13.982s`, and integration `2.840s`.

## Provider-neutral semantic artifacts — Wave 4

Wave 4 removes provider-owned schema, table, and column instances from the
semantic handoff. Metadata columns are copied into immutable
`BoundSchemaColumn` contracts, source bindings expose stable source identity
and required member/method signatures, and scope snapshots restore neutral
table contracts instead of retaining the live provider. Runtime providers
remain in the current compilation context and are never stored in semantic
artifacts.

The focused semantic-artifact suite passed `8` tests. The complete evaluator
project passed `9,486` tests with `4` skips and no failures (`9,490` total).
The Wave 4 solution gate under `TestResults/evaluator-frontend-wave-4`
passed `16,855` tests with `4` skips and no failures (`16,859` total).
TRX reporting measured `114.006s` wall-clock and `2,697.621s` summed
individual durations. Category sums were runtime `2,226.623s` across `14,692`
results, repeated-compilation `231.828s` across `792`, generated-sample
`222.837s` across `1,323`, benchmark `13.808s` across `43`, and integration
`2.526s` across `9`. The slowest individual results were recursive optimizer
cases; this wave made no dedicated performance or benchmark claim.

After the documentation-inclusive rerun, the final-tree gate measured
`112.207s` wall-clock and `2,679.436s` summed individual durations with the
same `16,855` passed, `4` skipped, and zero failed results. Category sums were
runtime `2,192.441s`, repeated-compilation `241.431s`, generated-sample
`230.810s`, benchmark `12.904s`, and integration `1.850s`.

## Validated semantic-template reuse — Wave 5

Wave 5 adds a bounded, process-local semantic-template cache for eligible
execution compilations. The key includes the exact script, parser/runtime and
execution-semantics versions, provider type and contract bucket, compilation
options, target/result mode, output type, references, and schema-registry type.
Only provider-neutral semantic artifacts are retained. Each hit clones the
ASTs and rekeys node-bound metadata dictionaries before planning; CTE
parallelization plans are rebuilt for the current clone. Runtime providers,
bindings, parameters, loggers, runnables, and results are not cached.

Templates whose lowered AST contains unsupported clone shapes are rejected at
publication and fall back to the normal pipeline. Failed or incompatible
cache entries are evicted rather than allowed to change query behavior. New
tests cover AST isolation, option separation, and cache eligibility; the
focused semantic/cache suites and the full converter suite passed.

The Wave 5 solution gate under `TestResults/evaluator-frontend-wave-5`
passed `16,857` tests with `4` skips and no failures (`16,861` total).
TRX reporting measured `116.084s` wall-clock and `2,810.704s` summed
individual durations. Category sums were runtime `2,291.733s` across `14,693`
results, repeated-compilation `263.022s` across `793`, generated-sample
`239.686s` across `1,323`, benchmark `13.782s` across `43`, and integration
`2.481s` across `9`. Slowest results remained recursive optimizer cases; Wave
5 dedicated performance runs and benchmarks remain deferred to Wave 7.

## Redundant semantic work — Wave 6

Wave 6 is implemented in the local tree. Normal execution now accumulates
optimizer traces as structural entries without formatting them. Trace text is
formatted once only for inspection builds. Planning text is also produced only
when `EmitExecutionPlanText` is enabled. This removes string-builder,
line-splitting, and plan-printer work from ordinary execution while preserving
the existing inspection output. The pipeline also removed a duplicate
telemetry-scope disposal.

Focused validation passed:

- semantic phase and staged inspection tests: 3 passed;
- rule-based optimizer trace tests: 3 passed;
- converter project: 884 passed, 0 skipped, 0 failed.

The first complete Wave 6 gate used `TestResults/evaluator-frontend-wave-6`:

- 16,858 passed, 4 skipped, 0 failed (`16,862` recorded results);
- wall clock `118.288s`; sum of individual durations `2,900.448s`;
- runtime `2,396.354s` across 14,693 tests; repeated compilation `239.510s`
  across 794; generated samples `246.889s` across 1,323; benchmark `14.105s`
  across 43; integration `3.590s` across 9.

The slowest work remains recursive optimizer DynamicData cases and corpus-wide
architecture checks. Dedicated performance, benchmark, and profiler
measurements remain deferred to Wave 7.

## Final profiling and acceptance — Wave 7

Wave 7 completed the dedicated measurements on the Wave 6 tree. The evaluator
project was run three times with telemetry disabled and no test identities or
cases were filtered:

| Run | Wall time | Result |
| --- | ---: | --- |
| 1 | 95.474s | 9,486 passed, 4 skipped |
| 2 | 97.955s | 9,486 passed, 4 skipped |
| 3 | 97.674s | 9,486 passed, 4 skipped |

The evaluator-only median was `97.674s`, with approximately `1,575.7s` of
testhost-tree CPU time across the three runs, `2.33GB` peak private memory, and
only two low-CPU samples. This confirms that the remaining time is active
parallel test work rather than a long unobserved idle gap.

The telemetry-only complete evaluator run was recorded under
`TestResults/evaluator-frontend-wave-7-telemetry-only`: 9,486 passed and 4
skipped in `98.797s`. It captured 5,767 compilation events, 130 exact cache
hits, 876 misses, 722 real emissions/loads, and `702.465s` exclusive
compilation-stage time. The largest exclusive stages were rendering `331.170s`,
build `122.870s`, emission `112.840s`, and runnable loading `46.010s`.

A full trace run at 24 workers caused a testhost CLR internal error after 8,866
tests had passed; the TRX had zero failed test cases but was correctly marked
aborted. A separate recursive trace succeeded under
`TestResults/evaluator-frontend-wave-7-trace-recursive`: all 612 cases passed,
18 batches emitted and loaded, 47.008s exclusive compilation work, 22.545s
explicit active case time, and a 46.9MB `trace.nettrace`. The trace run showed
no idle low-CPU samples.

Worker scaling remained correct for all 9,490 evaluator results:

| MSTest workers | Wall time | Peak private memory |
| ---: | ---: | ---: |
| 4 | 172.220s | 1.23GB |
| 8 | 132.830s | 0.87GB |
| 12 | 111.186s | 1.34GB |
| 16 | 100.024s | 1.09GB |
| 24 | 100.126s | 4.15GB |

Sixteen workers is the selected setting: it is effectively tied for fastest
while avoiding the 24-worker memory spike.

Three ShortRun JSON benchmark reports were captured for both the Q227–Q230
group and `CompilationPipelineBenchmark`. The Q227–Q230 median-of-three values
at 10,000 rows were:

| Workload | Median time | Median allocation |
| --- | ---: | ---: |
| Q227 join aggregate | 9.958ms | 895.29KB |
| Q228 wide correlation | 14.552ms | 17.728MB |
| Q229 window/CTE/set | 6.372ms | 4.164MB |
| Q230 table projection | 0.244ms | 949.21KB |

`BenchmarkComparison` passed against the three Wave 0 Q227–Q230 reports. The
ratios were Q227 `0.1645x/0.0120x`, Q228 `0.5460x/0.5545x`, Q229
`0.9637x/0.7887x`, and Q230 `0.1697x/0.6329x` for time/allocation. The same
comparison passed for the three compilation reports; the slowest time ratio
was the eligible cache hit at `0.6160x`, and the slowest allocation ratio was
typed artifact load/run at `0.9845x`.

The final acceptance target of `<=70s` evaluator median was not reached. The
measurements show that the remaining critical path is the test suite’s active
parallel compilation/materialization workload, not the frontend text work
changed in Wave 6. No further speculative production optimization was added in
this wave; the residual wall-time campaign should target the remaining test
families with new evidence rather than weaken correctness or cache isolation.

The final documentation-inclusive solution gate used
`TestResults/evaluator-frontend-wave-7-final-final` after the asynchronous
disposal-order repair in `CompiledQuery`: 16,858 passed, 4 skipped, and 0
failed (`16,862` recorded results). The preceding clean corrected run in
`TestResults/evaluator-frontend-wave-7-final-fixed` measured `126.987s` wall
time and `2,900.408s` summed individual TRX durations. Its category sums were
runtime `2,383.932s`, repeated compilation `248.086s`, generated samples
`248.873s`, benchmark `16.168s`, and integration `3.349s`.

The first final-tree gate exposed one intermittent lifecycle race: disposal
could observe the active-run completion signal before the execution semaphore
was released. The `finally` blocks now release that semaphore before signaling
the last active run, so disposal cannot close it while a run is unwinding.

## C# rendering and evaluator batching — Wave 0 attribution

Wave 0 of the rendering/batching campaign is measurement-only. The target
contract now exposes an internal, target-neutral phase sink, so C# rendering
telemetry can distinguish execution-method generation, class assembly,
individual readability passes, syntax cleanup, formatting, reparsing,
references, interpreter parsing, compilation construction, and canonical
identity. Emission, runnable loading, and activation are recorded separately;
cache hits are marked as reuse rather than counted as new emission/load work.

The outer `build` scope is a parent aggregate. Reports use its exclusive value
for stage comparisons and retain inclusive values only to show nesting. Parent
and child totals must not be added together. Batch reports now include origin,
compatibility-group count, queue delay when the request was queued, and a
fallback reason.

The existing Wave 7 baseline remains the pre-optimization reference: `5,073`
rendering calls, `331.170s` aggregate rendering (`226.670s` batched and
`104.500s` single-query), and `97.674s` evaluator median. No execution profile,
batch scheduler, or generated-code behavior changed in Wave 0. The focused
target telemetry tests passed. The first full Release correctness gate under
`TestResults/evaluator-rendering-wave-0` recorded `16,860` passed, `4` skipped,
and no failures (`16,864` results), with `123.623s` wall time and `2,821.573s`
summed TRX durations. Its duration categories were runtime `2,313.473s` across
`14,695` results, repeated compilation `251.792s` across `794`, generated
samples `239.526s` across `1,323`, benchmark `13.700s` across `43`, and
integration `3.082s` across `9`.

The enhanced measurement reporter was validated against the existing telemetry
run: it reports `702.465s` exclusive stage time versus `1,095.972s` inclusive
time, explicitly identifying nested-parent overcount rather than presenting
those values as independent work. The documentation-inclusive gate under
`TestResults/evaluator-rendering-wave-0-final` recorded `16,860` passed,
`4` skipped, and no failures (`16,864` results), with `119.793s` wall time and
`2,742.108s` summed TRX durations. Its category sums were runtime
`2,232.876s`, generated samples `257.144s`, repeated compilation `235.023s`,
benchmark `13.910s`, and integration `3.155s`.

## Execution and stable-artifact render profiles — Wave 1

Wave 1 adds an internal render-purpose contract and two profiles. Ordinary
execution without PDB generation selects `ExecutionFast`; inspection,
portable packaging, strict validation, and PDB-enabled execution select
`StableArtifact`. The purpose/profile travels through the target-neutral render
request and C# backend inputs. Exact execution-cache and canonical-artifact
identities include the profile and its version, preventing a fast execution
artifact from being reused for stable inspection or packaging work.

The stable rendering implementation remains unchanged in this wave; this is a
compatibility and separation boundary for the later fast renderer work. Focused
purpose/profile and cache-isolation tests passed. The corrected Wave 1 full gate
under `TestResults/evaluator-rendering-wave-1-corrected-2` recorded `16,863`
passed, `4` skipped, and no failures (`16,867` results), with `108.076s` wall
time and `2,578.431s` summed TRX durations. Categories were runtime
`2,098.170s`, generated samples `216.713s`, repeated compilation `245.871s`,
benchmark `15.268s`, and integration `2.411s`.

The first Wave 1 gate exposed the existing production-family line-budget
guardrail after the new context fields added one net line. The plumbing was
compacted without changing behavior, and the corrected gate passed.

## Fast execution syntax path — Wave 2

Wave 2 limits the execution profile to the two correctness-preserving codegen
passes: dead-temporary cleanup and approved helper extraction. The stable
artifact profile retains the existing six-pass readability pipeline. Execution
also skips redundant-parenthesis cleanup and the workspace formatter. Generated
syntax is normalized and parsed once to preserve Roslyn contextual-keyword
classification for factory-created `var`/`ref var` nodes; inspection,
packaging, strict validation, snapshots, and stable hashes remain on the old
path.

The focused codegen tests passed. The first direct-tree attempt was rejected by
the full gate because factory-created contextual nodes were not compiler-
equivalent when passed straight to `CSharpSyntaxTree.Create`; the execution
profile therefore keeps the cheaper normalized parse as a correctness boundary
instead of accepting generated-code failures. This is an explicit disproven
hypothesis for the next optimization wave: a fully direct tree requires fixing
the renderer's contextual syntax factories, not bypassing the parser blindly.

The corrected full Release gate under
`TestResults/evaluator-rendering-wave-2-corrected-2` recorded `16,860` passed,
`4` skipped, and no failures (`16,864` results), with `110.566s` wall time and
`2,395.124s` summed TRX durations. Its category sums were runtime
`2,131.605s`, generated samples `221.907s`, repeated compilation `26.768s`,
benchmark `12.730s`, and integration `2.115s`. The result is a correctness-
preserving execution-profile reduction; dedicated renderer benchmark and trace
comparisons remain deferred to Wave 7.

## Structural canonical syntax identity — Wave 4

Wave 4 removes `NormalizeWhitespace().ToFullString()` from canonical artifact
lookup. The identity builder walks each ordered Roslyn syntax tree once and
stores token kind, token text, syntax-tree boundaries, generated identity
normalization, and structured trivia in an immutable descriptor. Ordinary
whitespace and comments do not affect the identity. A SHA-256 fingerprint is
kept for candidate selection, while the complete descriptor and semantic
contract still participate in exact cache-key equality, so a collision cannot
reuse the wrong artifact. Existing entry and alias bounds, single-flight
creation, eviction, clearing, and fresh runtime binding are unchanged.

The canonical-cache focused tests passed. The full Release gate under
`TestResults/evaluator-rendering-wave-4` recorded `16,860` passed, `4` skipped,
and no failures (`16,864` results), with `105.879s` wall time and `2,243.803s`
summed TRX durations. Its category sums were runtime `1,992.742s`, generated
samples `212.398s`, repeated compilation `24.334s`, benchmark `12.085s`, and
integration `2.245s`. Dedicated benchmark and trace comparisons remain
deferred to Wave 7.

## Fused execution codegen cleanup — Wave 3

Wave 3 replaces the two execution-profile syntax passes with one
`ExecutionCodegenOptimizationPass`. It combines safe dead-temporary removal,
reverse block usage accumulation, metadata-backed helper approval, and inline
helper extraction in one traversal. The stable-artifact pass ordering and
readability trace remain unchanged. The first full gate exposed an ownership
guardrail that intentionally listed only the original helper pass files; the
allowlist was extended for the new fused execution pass and the corrected gate
passed.

The corrected full Release gate under
`TestResults/evaluator-rendering-wave-3-corrected` recorded `16,860` passed,
`4` skipped, and no failures (`16,864` results), with `110.715s` wall time and
`2,364.219s` summed TRX durations. Its category sums were runtime
`2,098.701s`, generated samples `223.812s`, repeated compilation `25.680s`,
benchmark `13.232s`, and integration `2.794s`. Dedicated benchmark and trace
comparisons remain deferred to Wave 7.

## Coalesced and bounded test compilation batches — Wave 5

Wave 5 replaces the timer-per-batch test coordinator with one shared queue and
dispatcher per coordinator. A first request has the existing bounded
two-millisecond collection window; queued requests are drained immediately up
to 16 items, isolated requests use the original single-query path, and at most
two batch compilations run concurrently. Shutdown drains pending requests
through the single-query path and waits for scheduled work to release its
assembly/query ownership. Production `CompileForExecutionBatch` preparation
and finalization now use process-wide budgets of 16 and 2 respectively, so
multiple test families cannot multiply Roslyn concurrency by their invocation
count.

The coordinator focused suite passed 7 tests, including 16-request queue
coalescing, bounded batching, fallback isolation, provider sentinel isolation,
and shutdown. The full Release gate under
`TestResults/evaluator-rendering-wave-5` recorded `16,861` passed, `4` skipped,
and no failures (`16,865` results), with `108.282s` wall time and `2,495.620s`
summed TRX durations. Its category sums were runtime `2,324.161s`, generated
samples `136.524s`, repeated compilation `23.612s`, benchmark `8.547s`, and
integration `2.777s`. Dedicated benchmark and trace comparisons remain
deferred to Wave 7.

## Stable interpretation specification batching — Wave 6

Wave 6 labels positive Binary/Text specification requests as
`stable-interpretation-specification` with the explicit
`binary-textual-specification` batch origin while reusing the shared tracked
coordinator and its fresh runtime ownership. A source guardrail rejects new
direct positive-spec compilation calls; the existing mixed inline-schema
regression remains an explicit single-query exception because it exercises a
different interpreter contract. Diagnostic, debugger, mutable-provider, and
deferred-lifetime paths retain their single-query behavior where they bypass
the positive helper.

The focused Binary/Text suite passed 164 tests. The full Release gate under
`TestResults/evaluator-rendering-wave-6` recorded `16,862` passed, `4` skipped,
and no failures (`16,866` results), with `110.277s` wall time and `2,541.090s`
summed TRX durations. Its category sums were runtime `2,342.250s`, generated
samples `161.182s`, repeated compilation `25.196s`, benchmark `10.249s`, and
integration `2.212s`. Dedicated benchmark and trace comparisons remain
deferred to Wave 7.

## Final rendering and batching validation — Wave 7

Wave 7 measurements were run on the exact Wave 6 tree (`1756be959`) after a
successful Release build. The evaluator project was run three times with
telemetry disabled and no filter:

| Run | Wall time | Results |
| --- | ---: | --- |
| 1 | 102.338s | 9,489 passed, 4 skipped |
| 2 | 102.168s | 9,489 passed, 4 skipped |
| 3 | 99.574s | 9,489 passed, 4 skipped |

The no-telemetry evaluator median was `102.168s`, so the `<=70s` required
target and the `<=60s` preferred target were not reached. Across the three
runs, the monitor observed approximately `1,157.4s` process-tree CPU time,
`2.19GB` peak private memory, `39.1%` average system CPU, and two low-CPU
samples. Each run had one testhost start; there was no unexplained idle gap
large enough to explain the wall time.

The telemetry-enabled run is under
`TestResults/evaluator-rendering-wave-7-telemetry` and passed all `9,489`
tests plus four skips in `99.986s`. It recorded `5,784` compilation events:
`127` exact/canonical hits, `876` misses, and `4,000` ineligible paths. There
were `1,981` real emission events and `1,981` real load events. The exclusive
phase totals were:

| Phase | Exclusive time |
| --- | ---: |
| Syntax-tree construction | 124.610s |
| Execution-method generation | 64.063s |
| Roslyn emission | 60.532s |
| Readability optimization | 27.370s |
| Class assembly | 19.409s |
| Semantic pipeline | 19.750s |
| Runnable loading | 35.135s |
| Runnable creation | 10.724s |
| Canonical identity | 2.978s |

This confirms that the dominant remaining cost is the repeated Roslyn syntax
tree/code-generation and emission work, not parsing (`3.323s`) or semantic
planning. The coordinator emitted `610` successful batches containing `3,646`
items; ordinary coordinator batches averaged `4.81` items, while the explicit
recursive matrix batches averaged `34` items.

## Stability-aware loop-invariant code motion — implementation and qualification

The loop-invariant optimization is owned by the Execution IR pipeline. The
shared `ExpressionStabilityAnalyzer` is consulted by all evaluation-changing
rewrites, and `LoopInvariantCodeMotionPass` runs after method-target reuse and
before field/expression CSE. It emits eager `ExecutionLet` locals for stable
scalar expressions repeated by a descendant serial loop. Volatile values remain
at their producer boundary, and no runtime cache or initialization branch is
introduced. See [loop-invariant-code-motion.md](loop-invariant-code-motion.md)
for the provider contract, excluded boundaries, and user-facing switch.

The Wave 6 qualification command ran three complete cohorts in one process:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release --no-build -- `
  gate-loop-invariant `
  --report BenchmarkDotNet.Artifacts/results/loop-invariant-inprocess-cohort-1.json `
  --report BenchmarkDotNet.Artifacts/results/loop-invariant-inprocess-cohort-2.json `
  --report BenchmarkDotNet.Artifacts/results/loop-invariant-inprocess-cohort-3.json
```

The gate covered fan-outs 1, 8, and 64 for stable expensive, stable cheap, and
volatile producers, with identical result/counter oracles and allocation
diagnostics. The accepted high-fan-out expensive ratios were `0.5919x` for the
getter and `0.4025x` for the callable; cheap and volatile cases remained within
`1.03x`, and allocation stayed within the configured diagnostic noise bound.
The raw JSON is intentionally ignored; keep the command and committed report
description here reproducible instead of checking in BenchmarkDotNet artifacts.

The current worker sweep passed all tests at every setting:

| MSTest workers | Wall time | Peak private memory |
| ---: | ---: | ---: |
| 4 | 152.400s | 1.16GB |
| 8 | 128.952s | 0.89GB |
| 12 | 111.571s | 1.61GB |
| 16 | 101.569s | 1.14GB |
| 24 | 99.815s | 1.08GB |

Sixteen workers remains the selected safe setting for tracing and acceptance:
24 workers was only `1.754s` faster in this sweep, but the earlier 24-worker
trace caused a testhost CLR failure and a `4.15GB` private-memory spike.

The selected 16-worker trace is under
`TestResults/evaluator-rendering-wave-7-trace`; it passed all tests, produced
no trace errors, and generated `trace.nettrace` (`696MB`). The trace confirms
active parallel work; no replacement optimization was added after the trace
because the remaining hotspot requires a broader Roslyn/code-generation
campaign than this validation wave.

The isolated recursive optimizer matrix retained all `612` cases and passed
all cases in each of three runs: `24.255s`, `23.977s`, and `24.251s` (median
`24.251s`). Peak private memory was `420MB` and process CPU time was `163.9s`.

Three isolated ShortRun cohorts were captured for both
`CompilationPipelineBenchmark` and `EvaluatorPerformanceSamplesBenchmark`.
The current Q227–Q230 median-of-three values at 10,000 rows were:

| Workload | Median time | Median allocation |
| --- | ---: | ---: |
| Q227 join aggregate | 10.033ms | 895.27KB |
| Q228 wide correlation | 15.650ms | 17.728MB |
| Q229 window/CTE/set | 6.025ms | 4.164MB |
| Q230 table projection | 0.238ms | 949.20KB |

`BenchmarkComparison` passed against the Wave 0 Q227–Q230 reports and the
existing compilation baseline, with all time and allocation ratios below the
1.03 regression ceiling. The current Q227–Q230 ratios were respectively
`0.1650x/0.0120x`, `0.5762x/0.5545x`, `0.9111x/0.7886x`, and
`0.1635x/0.6329x` for time/allocation at 10,000 rows.

Raw TRX, telemetry, trace, and BenchmarkDotNet files remain ignored. The
remaining acceptance failure is explicit: Waves 0–6 materially reduced
rendering and batching costs and preserved all correctness gates, but the
evaluator tier is still above the requested wall-time budget because roughly
two thousand fresh executable emissions/loads remain. No test identity, case,
assertion, or safety boundary was removed.

The first documentation-inclusive full-solution Wave 7 gate passed all
projects: `16,866` passed, `4` skipped, and `0` failed (`16,870` recorded
results). The TRX reporter measured `107.758s` wall time and `2,711.663s` of
summed individual test durations. Category totals were runtime `2,324.916s`,
generated samples `168.511s`, repeated compilation `205.576s`, benchmark
`10.362s`, and integration `2.298s`. The second exact-tree gate after that
documentation update also passed all `16,866` tests plus four skips in
`108.898s` wall time, with `2,739.826s` summed TRX duration; its per-project
TRX files are under `TestResults/evaluator-rendering-wave-7-final-final`.

## Corrective scalar-reuse qualification

The corrective stability-aware scalar-reuse campaign extends LICM across the
other high-reuse scalar boundaries without adding runtime caching. C0 records
the recomputation baseline; C1–C15 cover the stability contract, region-aware
collector, row/operator boundaries, windows, aggregates, PIVOT, guarded APPLY,
specialized joins, correlated probes, UNPIVOT, boundary row width, provider
computed projections, recursive invariants, and exact semantic counters. C17
activates the qualified umbrella option and refreshes the 326-sample corpus.

The qualification matrix has ten workload families:

1. cross-boundary projections;
2. windows;
3. aggregates and PIVOT;
4. guarded APPLY;
5. nested, ASOF, and range joins;
6. correlated probes;
7. UNPIVOT;
8. boundary row width;
9. provider computed projections; and
10. recursive CTEs.

Each family must run cheap stable, no-inlining expensive stable, volatile, and
no-candidate cases at fan-outs 1, 8, and 64. Three isolated JSON cohorts are
required. The machine-readable gate compares equivalent LICM/scalar-reuse
off/on queries and exact counter/result oracles. Acceptance thresholds are:

* expensive no-storage median ratio `<= 0.97x`;
* carried-storage median ratio `<= 0.90x`;
* cheap, volatile, low-reuse, and no-op ratios `<= 1.03x`;
* tiny compilation/cache ratio `<= 1.03x` and feature-heavy compilation
  ratio `<= 1.05x`;
* no-storage allocation increase within `1,024 B/op` noise and storage no
  greater than predicted payload plus `1,024 B/op` or `1.03x`;
* width-128 narrowing time `<= 1.03x` and allocation `<= 0.97x`.

The family qualification command is:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  gate-stability-aware-reuse-families `
  --report BenchmarkDotNet.Artifacts/results/stability-aware-reuse-family-1.json `
  --report BenchmarkDotNet.Artifacts/results/stability-aware-reuse-family-2.json `
  --report BenchmarkDotNet.Artifacts/results/stability-aware-reuse-family-3.json
```

Generated code is the authoritative branch gate: accepted paths contain
ordinary lexical locals or typed storage and no initialization flag, cache
lookup, runtime stability check, or runtime option branch. Disassembly and
hardware branch/misprediction counters are supporting evidence only. Raw JSON
and profiler artifacts remain ignored; the benchmark source and committed
report record the environment, commands, counters, allocation policy, and
thresholds.
