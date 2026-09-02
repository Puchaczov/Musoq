# Enum support pre-change baseline

This baseline was captured before enum syntax or runtime contracts were
introduced. It freezes representative enum-free generated-code identities and
records primitive execution cohorts that the enum qualification benchmarks
will compare against.

## Environment

- Core commit: `0a8ebaaa117fc12bbef607e22ea45da7942776ae`
- OS: Windows 11 `10.0.26200.9168`
- CPU: Intel Core Ultra 9 285K, 24 physical/logical cores
- SDK: .NET SDK `10.0.303`
- Runtime: .NET `10.0.11`, x64 RyuJIT x86-64-v3
- BenchmarkDotNet: `0.15.8`
- Job: ShortRun, one launch, three warmups, three measured iterations

Raw JSON and disassembly output is machine-local under the ignored
`BenchmarkDotNet.Artifacts/enum-baseline` directory.

## Validation baseline

```powershell
dotnet restore src/dotnet/Musoq.sln --nologo --verbosity quiet
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet
dotnet test src/dotnet/Musoq.sln -c Release --no-build --nologo --verbosity quiet `
  --logger "console;verbosity=minimal" --logger "trx;LogFileName=enum-wave0-baseline.trx"
```

The restore and warning-clean build passed. The full solution reported 19,608
passed, 3 intentionally skipped generated-sample refresh tests, and 0 failed.

## Generated-code identities

The tracked generated-sample manifest recorded these SHA-256 identities:

| Cohort | Sample | SHA-256 |
|---|---|---|
| Comparison/projection | `Q01_SimpleSelectWhere.cs` | `48dfc082266dd75ce041b7bfbefae85e5b1b394c7e524aa10628f2df4c50f770` |
| Join | `Q04_LeftJoin.cs` | `74757cac685e2a7bfc87b5b3282a6cb14b38227fa0e3b54c9fd6fe5fc6eff2a1` |
| Grouping | `Q05_GroupBySingle.cs` | `a6c72246388899e387bcfc2f3104012b188a2020b125f2140e4b6ea9175ab7bf` |
| Small `IN` | `Q23_InClause.cs` | `585e501557d603dcac34d61a347c5dcaa95c0c4c7885099a7d39201a00043f78` |
| Large `IN` | `Q42_InClauseLarge20Values.cs` | `1859a9db7968eed9f7035f0837c3f0fe6ba770b828ac82d4396a1141c02d29d0` |
| Primitive mask | `Q268_SpecCoreOperatorsAndRawLiterals.cs` | `f35d12b5290aa5a71da3d6a09c60e5e4b6ed76f94e61838842034126578f2b74` |
| Query-scoped transfer | `Q237_QueryRowReadonlyStruct.cs` | `0cfe0e8877f8c259d41c356ba5eee6b184d5f1aa83cde0f964cbec7d974fd7c4` |

Enum-free qualification compares these samples after the implementation and
requires byte-for-byte stability unless a contract-version field is the only
documented difference.

## Benchmark commands

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter "*QueryScopedSourceMaterializationBenchmark*Numeric*" --job short `
  --memory --exporters json --artifacts "BenchmarkDotNet.Artifacts/enum-baseline/query-scoped-numeric"

dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter "*InClauseOptimizationBenchmark*" --job short --memory `
  --exporters json --artifacts "BenchmarkDotNet.Artifacts/enum-baseline/in-clause"

dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter "*GroupByAggregationBenchmark*" --job short --memory `
  --exporters json --artifacts "BenchmarkDotNet.Artifacts/enum-baseline/grouping"

dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter "Musoq.Benchmarks.ExecutionBenchmark.*" --job short --memory `
  --exporters json --artifacts "BenchmarkDotNet.Artifacts/enum-baseline/execution-exact"
```

## Representative primitive results

| Cohort | Mean | Allocated per operation |
|---|---:|---:|
| Query-scoped numeric struct source, 8 fields x 2,048 rows | 1.399 ms | 1,430,648 B |
| Query-scoped numeric struct materialization, 8 fields x 2,048 rows | 3.070 us | 0 B |
| Equality filter | 349.8 us | 728.80 KB |
| `IN` with 3 values | 156.4 us | 229.07 KB |
| `IN` with 10 values | 219.9 us | 229.07 KB |
| Single-key grouping | 200.5 us | 244.23 KB |
| Multi-key grouping | 1.060 ms | 1,031.14 KB |
| Hash join, 1,000 rows | 239.6 us | 719.58 KB |
| Non-hash join, 1,000 rows | 5.591 ms | 673.92 KB |

These whole-query allocation figures include source and final-table costs.
The enum gates use paired carrier/enum benchmarks and generated-loop probes to
measure the incremental hot-path cost. Existing final-table row allocation
and `Row.this[int] : object` boxing are explicitly measured but excluded from
the `0 B/row` enum hot-loop requirement.

## Post-implementation qualification

Qualification was captured on 2026-09-02 on the same machine, SDK, runtime,
and BenchmarkDotNet version. The committed enum benchmark uses MediumRun: two
launches, ten warmups, and fifteen measured iterations for each of sixteen
carrier/enum cases over 8,192 rows. Three independent complete reports are
required. The gate takes the median of the three within-report enum/carrier
ratios so machine-speed changes cannot pair values from different cohorts.

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter "*FirstClassEnumQualificationBenchmark*" --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/enum-qualification/medium-1"

# Repeat with medium-2 and medium-3, then:
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  gate-enums `
  --report "BenchmarkDotNet.Artifacts/enum-qualification/medium-1/results/Musoq.Benchmarks.FirstClassEnumQualificationBenchmark-report-full-compressed.json" `
  --report "BenchmarkDotNet.Artifacts/enum-qualification/medium-2/results/Musoq.Benchmarks.FirstClassEnumQualificationBenchmark-report-full-compressed.json" `
  --report "BenchmarkDotNet.Artifacts/enum-qualification/medium-3/results/Musoq.Benchmarks.FirstClassEnumQualificationBenchmark-report-full-compressed.json"
```

The three cohorts contained 48 benchmark cases and 1,440 measured case
iterations before BenchmarkDotNet outlier handling. The gate passed:

| Scenario | Paired-median time ratio | Limit | Allocation delta per operation | Incremental B/row |
|---|---:|---:|---:|---:|
| Equality | 0.9964x | 1.02x | -43 B | -0.005249 |
| `IN` | 1.0064x | 1.03x | +16 B | +0.001953 |
| Flags | 0.9695x | 1.02x | -8 B | -0.000977 |
| Join | 0.9989x | 1.03x | -2 B | -0.000244 |
| Grouping | 0.9992x | 1.03x | -19 B | -0.002319 |
| Distinct | 0.9686x | 1.03x | -8 B | -0.000977 |
| Helpers | 0.9752x | informational | +7 B | +0.000854 |
| Projection | 1.0012x | informational | -13 B | -0.001587 |

The largest positive difference was sixteen bytes for an entire 8,192-row
operation. This fixed-operation fluctuation, together with generated-loop and
IL checks, qualifies the enum implementation as adding `0 B/row`. Final-table
materialization remains symmetric and is not treated as an enum hot-path cost.

### Enum-free generated-code audit

The normalized manifest hashes for `Q01`, `Q04`, `Q05`, `Q23`, `Q42`, and
`Q268` remain byte-for-byte equal to the pre-change hashes above. The only
expected enum-free exception is `Q237_QueryRowReadonlyStruct.cs`: its normalized
hash changed from
`0cfe0e8877f8c259d41c356ba5eee6b184d5f1aa83cde0f964cbec7d974fd7c4`
to
`3cdaf0905cea8a018b358351f5fa7660f496199c00feff6806c0be7acfabbf13`.
The query-row fingerprint now includes the required source-read/logical-type
metadata. The diff changes only the fingerprint-derived generated type names;
typed reads, projection, and loop instructions are unchanged.

`Q324_EnumQueryScopedRows.cs` is the direct enum sample. Its structural tests
require typed primitive reads, direct mask operations, compiled name lookup,
descriptor construction outside loops, and absence of runtime enum parsing,
reflection, conversion, and hot-loop boxing.
