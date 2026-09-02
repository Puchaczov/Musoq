# Loop-invariant code-motion qualification

This benchmark is the performance gate for the stability-aware loop-invariant
code-motion (LICM) compilation option. LICM is enabled by default after this
qualification; the benchmark explicitly runs the same generated query with LICM
disabled and enabled, and checks the result row count, checksum, and
producer-read oracle on every invocation. The oracle prevents a faster run from
hiding an evaluation semantics regression.

## Matrix

`LoopInvariantQualificationBenchmark` uses two outer rows and fan-outs of 1, 8,
and 64. `ExecuteOff` and `ExecuteOn` are paired benchmark methods in an
in-process BenchmarkDotNet job (three warmups and three measurements), so both
variants share the same runtime process for each fan-out/scenario pair:

| Scenario | Producer | Contract |
| --- | --- | --- |
| `StableCheapGetter` | stable property | LICM may reduce reads to two outer-row reads |
| `StableExpensiveGetter` | stable, no-inlining property | same, with a deliberately expensive body |
| `VolatileGetter` | `[NonDeterministic]` property | remains one read per output row |
| `StableCheapCallable` | stable function | LICM may reduce the two references to one pair per outer row |
| `StableExpensiveCallable` | stable, no-inlining function | same, with a deliberately expensive body |
| `VolatileCallable` | `[NonDeterministic]` function | remains one read per output row |

The generated query is:

```sql
select <producer> + n.Value + m.Value
from #loopq.items() i
cross apply i.Numbers n
cross apply i.Numbers m
```

The volatile getter and callable rows use the same fan-out loops but project the
volatile producer alone. This keeps their no-op LICM comparison from measuring
the intentionally hoistable inner-column reads used by the stable scenarios.
Stable callable projections reference the same producer twice so that the
fan-out-one row still measures a real repeated scalar candidate.

Each complete report therefore contains 36 rows (six scenarios × three fan-outs
× two option values). Each benchmark invocation repeats the query 64 times
while retaining one counter oracle, which reduces timing noise without changing
the query or its generated code. Three independent reports are required; the
gate compares the median of each row across those reports.

## Qualification thresholds

The gate is intentionally compile-time and report-based. It does not add a
runtime profitability branch to generated code.

* At fan-out 64, each expensive stable scenario must have an enabled/disabled
  median time ratio of at most `0.97`.
* Every cheap stable scenario must have a ratio of at most `1.03`.
* Every volatile scenario must have a ratio of at most `1.03`.
* Enabled allocated bytes per operation must not exceed the disabled value for
  any scenario/fan-out pair beyond the 1 KiB MemoryDiagnoser bookkeeping-noise
  allowance.

The generated-code shape tests remain the authoritative branch check: an
enabled query must contain ordinary lexical locals and no lazy initialization
flag, cache lookup, or stability branch. Disassembly and hardware branch or
misprediction counters are supporting evidence only.

## Collection

Run the following from a Release build. The benchmark class declares its
in-process job and iteration counts; do not add another `--job` option because
that would run a duplicate job cohort.

```powershell
dotnet restore src/dotnet/Musoq.sln --nologo --verbosity quiet
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter "*LoopInvariantQualificationBenchmark.Execute*" --exporters json
```

Repeat the benchmark command three times on the same machine, preserving one
JSON result per run. Raw BenchmarkDotNet artifacts stay ignored and should not
be committed. The current benchmark environment is .NET 10.0.11 on x64
RyuJIT with BenchmarkDotNet 0.15.8; record the exact SDK, runtime, OS, CPU,
and BenchmarkDotNet version alongside any collected result.

Use the repository gate with exactly those three result files:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  gate-loop-invariant `
  --report <cohort-1.json> `
  --report <cohort-2.json> `
  --report <cohort-3.json>
```

The command prints every paired ratio and a final machine-readable JSON result;
exit code zero is required. The paired LICM-off/on rows are the before/after
comparison. `LoopInvariantBaseline.md` remains the separate pre-LICM reference
against a hand-hoisted C# loop.

## Interpretation

If a threshold fails, tune only compile-time candidate selection or placement
and rerun the complete matrix. Do not introduce runtime checks, initialization
flags, cache dictionaries, or branch-based stability decisions to improve a
benchmark result. A failed producer-read oracle is a correctness failure and
must be fixed before timing data is considered.
