# Stability-aware scalar reuse qualification

This benchmark qualifies the cross-operator stability-aware scalar reuse path
independently from serial loop-invariant code motion and ordinary CSE. It
compiles equivalent queries with the reuse option disabled and enabled, then
checks result row counts, checksums, and producer-read counters on every
invocation.

## Matrix

`StabilityAwareScalarReuseQualificationBenchmark` uses eight outer rows and
fan-out labels of 1, 8, and 64. The fan-out-one workload is raised to an
effective eight rows so that the low-work case is not dominated by timer noise;
the labels are retained to keep the qualification matrix stable. `ExecuteOff`
and `ExecuteOn` are paired methods in an in-process BenchmarkDotNet job with
five warmups and five measurements.

| Scenario | Producer or boundary | Contract |
| --- | --- | --- |
| `StableCheapFilter` | stable property used by projection and predicate | one getter read per source row when enabled |
| `StableExpensiveFilter` | stable, no-inlining property used by projection and predicate | same read oracle and high-fan-out speed target |
| `StableAggregate` | stable aggregate argument and grouping key | identical rows and checksum across toggles |
| `VolatileFilter` | `[NonDeterministic]` property used by projection and predicate | two getter reads per source row in both modes |

The filter query is (the explicit ordering keeps both options on the same
execution shape):

```sql
select <producer> from #reuse.items() i where <producer> > 0 order by i.Id
```

The aggregate query is:

```sql
select i.ExpensiveValue, Count(i.ExpensiveValue)
from #reuse.items() i
group by i.ExpensiveValue
```

Each complete report contains 24 rows (four scenarios × three fan-outs × two
option values). Three independent reports are required; the gate compares the
median of each paired row across those reports.

## Qualification thresholds

The gate is compile-time and report-based. It does not add a runtime
profitability check or branch to generated code.

* `StableExpensiveFilter` at fan-out 64 must have an enabled/disabled median
  time ratio of at most `0.97`.
* Every other stable or volatile scenario must have a ratio of at most `1.03`.
* Enabled allocated bytes per operation must not exceed the disabled value for
  any pair beyond the 1 KiB MemoryDiagnoser bookkeeping-noise allowance.
* The result and counter oracles must pass before timing data is considered.

The generated-code shape tests remain the authoritative branch check: enabled
code contains ordinary lexical locals and no initialization flag, cache lookup,
or runtime stability branch.

## Collection

Run from a Release build. The benchmark class declares its in-process job and
iteration counts; do not add another `--job` option because that creates a
duplicate cohort.

```powershell
dotnet restore src/dotnet/Musoq.sln --nologo --verbosity quiet
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter "*StabilityAwareScalarReuseQualificationBenchmark.Execute*" --exporters json
```

Repeat the command three times on the same machine and retain one JSON result
per run under the ignored `BenchmarkDotNet.Artifacts` directory. Record the
exact SDK, runtime, OS, CPU, and BenchmarkDotNet version alongside the
qualification result.

Use the machine-readable gate with exactly those reports:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  gate-stability-aware-reuse `
  --report <cohort-1.json> `
  --report <cohort-2.json> `
  --report <cohort-3.json>
```

The command prints every paired ratio and a final JSON result; exit code zero
is required. If a threshold fails, tune only compile-time candidate selection
or placement and rerun the complete matrix. Never add lazy initialization,
cache dictionaries, or runtime stability branches to improve the result.

## Captured correctness baseline

The qualification fixture was validated at fan-out 8 on 2026-08-24 from the
stability-aware reuse implementation. Stable cheap and expensive filters both
produce 64 rows with 128 getter reads disabled and 64 reads enabled. The
volatile filter produces 64 rows with 128 reads in both modes. The aggregate
checksum and row set are identical across toggles. Stable cheap and volatile
producers execute deterministic 32-step arithmetic; the no-inlining stable
expensive producer executes 256 steps.

Three complete cohorts were collected on the qualification machine:

* BenchmarkDotNet 0.15.8, .NET 10.0.11 (`10.0.1126.37416`), Release,
  Windows 11 25H2 (`10.0.26200.9168`), Intel Core Ultra 9 285K, 24 logical
  cores, `dotnet` CLI 10.0.303.
* Stable expensive filter at fan-out 64: median enabled/disabled ratio
  `0.7403` (threshold `<= 0.97`).
* Other stable cases: ratios from `0.8011` to `0.9865` (threshold `<= 1.03`).
* Volatile cases: `0.9954`, `0.9969`, and `1.0068` (threshold `<= 1.03`).
* Allocation ratios were `0.99999`–`1.00002`; no enabled case exceeded the
  disabled allocation allowance.

The machine-readable `gate-stability-aware-reuse` command returned exit code
zero with no failures. Raw JSON reports remain ignored under
`BenchmarkDotNet.Artifacts/qualification`.

## Corrective ten-family cohort

The corrective qualification adds `StabilityAwareScalarReuseFamilyQualificationBenchmark`.
It records the same stable-cheap, no-inlining stable-expensive, and volatile
counter workloads under these ten operator-surface labels: cross-boundary
projection, windows, aggregates/PIVOT, guarded APPLY, specialized joins,
correlated probes, UNPIVOT, boundary row width, provider projections, and
recursive CTEs. `StabilityAwareScalarReuseCompilationQualificationBenchmark`
adds tiny no-op and feature-heavy compilation/cache cohorts.

Run the expanded execution matrix and its separate compilation probe with:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter "*StabilityAwareScalarReuseFamilyQualificationBenchmark*" --exporters json
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter "*StabilityAwareScalarReuseCompilationQualificationBenchmark*" --exporters json
```

Three isolated family JSON cohorts are checked with
`gate-stability-aware-reuse-families`. The gate requires 90 complete paired
rows (10 families × 3 workloads × 3 fan-outs), a 0.97 high-fan-out expensive
limit, a 1.03 general limit, and no allocation increase beyond 1,024 B/op
noise. The broad umbrella option remains default-off during this qualification.
