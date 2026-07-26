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
operation, so it did not isolate a cache hit. Wave 3 will split that case into
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

Status: pending.

### Wave 2 — typed/table execution paths

Status: pending.

### Wave 3 — compilation cost separation

Status: pending.

### Wave 4 — real-workload validation and hardening

Status: pending.
