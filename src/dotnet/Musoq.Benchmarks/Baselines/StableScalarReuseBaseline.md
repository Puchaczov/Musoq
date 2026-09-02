# Stability-aware scalar reuse gap baseline

This baseline records the recomputation cost before the corrective scalar
reuse waves. `StableScalarReuseBaselineBenchmark` compares the generated-style
use-site evaluation with a handwritten reference that computes the outer and
middle values once per owning loop. The matrix covers cross-boundary
projection, windows, aggregates, PIVOT, guarded APPLY, specialized joins,
correlated probes, UNPIVOT, boundary width, provider projections, and recursive
invariants at fan-outs 1, 8, and 64.

The fixture is an executable correctness baseline: both methods must return the
same checksum. Raw BenchmarkDotNet output belongs under the ignored artifacts
directory; this report intentionally records the command and environment
template without checking in machine-specific raw output.

```powershell
dotnet restore src/dotnet/Musoq.sln --nologo --verbosity quiet
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter "*StableScalarReuseBaselineBenchmark*" --job short --memory --exporters json
```

Record the OS, CPU, SDK/runtime, GC mode, commit, command line, checksum
oracle, getter/call counters, timing, and allocated bytes for each cohort
before comparing optimized query families. This report is the committed
starting point for the corrective campaign; it is not a performance claim for
any particular machine.
