# Nested APPLY recomputation baseline

This baseline is captured before loop-invariant code motion. It compares the current generated query with a hand-hoisted C# reference over the same rows and fan-out.

## Query shape

```sql
select i.ExpensiveValue + n.Value + m.Value
from #loop.items() i
cross apply i.Numbers n
cross apply i.Numbers m
```

The generated query reads `i.ExpensiveValue` from the innermost loop. The reference reads it once per outer row before enumerating either child loop.

## Reproduction

```powershell
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*LoopInvariantBaselineBenchmark*" --job short --memory --exporters json
```

Record the machine, SDK, BenchmarkDotNet version, `OuterRows`, `Fanout`, mean, error, standard deviation, allocated bytes, and the checksum from both benchmark methods in the qualification report. The generated query and hand-hoisted reference must produce the same checksum for every parameter combination.

The raw BenchmarkDotNet artifacts remain ignored; this file is the committed baseline contract and reproduction recipe.

## Captured baseline

Captured on 2026-08-23 from commit `6453449f2` with:

- Windows 11 25H2 (`10.0.26200.9168`)
- Intel Core Ultra 9 285K, 24 logical/physical cores
- .NET SDK `10.0.303`, runtime `.NET 10.0.11`
- BenchmarkDotNet `0.15.8`, `ShortRun`, 3 warmups and 3 iterations
- Concurrent workstation GC, high-performance power plan

The generated query is dominated by repeated `ExpensiveValue` evaluation and result materialization. The hand-hoisted reference performs the same arithmetic without query-engine allocation or repeated getter calls.

| Outer rows | Fan-out | Generated mean | Hand-hoisted mean | Generated allocation |
|---:|---:|---:|---:|---:|
| 4 | 4 | 125.742 us | 79.637 ns | 243,795 B/op |
| 4 | 8 | 197.329 us | 137.865 ns | 266,671 B/op |
| 16 | 4 | 196.653 us | 317.751 ns | 274,515 B/op |
| 16 | 8 | 228.285 us | 533.973 ns | 365,751 B/op |

These values are a characterization baseline, not a qualification threshold. Later waves must compare equivalent compiled queries with identical result and counter oracles, then report execution time, allocation, generated shape, and compilation cost separately.
