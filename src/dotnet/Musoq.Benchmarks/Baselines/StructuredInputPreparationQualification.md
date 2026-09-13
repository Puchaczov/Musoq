# Structured input preparation qualification

`StructuredInputPreparationBenchmark` measures the generated structural-input
paths in separate workloads. The structural record, numeric value-type,
nested-record/array, nullable-element, and empty-collection cohorts all run a
precompiled SQL query through `CompiledQuery.Run`; they no longer build or
convert `StructuralValue` trees in the benchmark. Independently written typed
loops remain only as correctness and required-storage baselines. The suite also
measures parse/bind and metadata discovery, inline and retained-`let` source
calls, host normalization, CTE production and adaptation, source construction,
result enumeration, and scalar/VALUES cohorts.

The matrix uses sizes 1, 3, 32, 1,024, and `NearLimitSize`, where the generated
pattern input is within one record stride of the one-node-below 100,000-node
default budget. Query
compilation and fixture creation happen in `GlobalSetup`; measured structural
methods therefore charge the actual generated preparation, source lifecycle,
and result work. Host normalization is measured through the generated snapshot
boundary, and the CTE workload uses the same parenthesized VALUES rows as the
maintained example datasource. Nested and nullable cohorts use the benchmark-
only `#benchmarkinputs` provider so their generated contracts are exercised
rather than simulated by a helper.

Run a focused characterization from a Release build.  The environment variable
is needed because BenchmarkDotNet restores its generated project separately and
the repository currently reports the known `Microsoft.Build.Tasks.Git`
`NU1902` advisory as an error:

```powershell
$env:NuGetAudit = 'false'
dotnet restore src/dotnet/Musoq.sln --nologo --verbosity quiet
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release --no-build -- `
  --filter "*StructuredInputPreparationBenchmark*" --exporters json
```

For a shorter representative run, select one phase (for example
`*StructuralRecordConstruction*`) and retain the generated compressed JSON
under the ignored `BenchmarkDotNet.Artifacts/results` directory.  Record the
exact SDK, runtime, OS, CPU, benchmark version, commit, and selected filter.
The report is characterization evidence; it is not a cross-machine timing
claim.

## Wave 13 CTE/set-operation follow-up

The typed `UNION ALL` CTE representation fix was requalified from source
revision `81d2b261d50205ecc5268b81db07fb4eede5e1c9` on Windows 11 Home build
`26200`, Intel Core Ultra 9 285K (24 logical cores), .NET SDK `10.0.303`,
.NET `10.0.11`, and BenchmarkDotNet `0.15.8`. The focused CTE, scalar-source,
and VALUES cohorts used the existing class job plus a three-iteration
ShortRun, with `--memory --exporters json`, and wrote raw reports under
`BenchmarkDotNet.Artifacts/structured-input-wave13-cte`,
`BenchmarkDotNet.Artifacts/structured-input-wave13-scalar`, and
`BenchmarkDotNet.Artifacts/structured-input-wave13-values`.

The CTE cohort measured 82.83 us at size 32 and 4.164 ms at size 19,999, with
337.28 KB and 4,924.76 KB allocated respectively. Scalar-source measured
79.23 us and 79.56 us with 329.54 KB allocated; VALUES measured 1.010 us and
450.2 ns with 7.1 KB and 3.45 KB allocated. These are inclusive required
execution allocations, and remain within the existing characterization
cohorts; scalar and VALUES paths show no regression. The compressed report
hashes are:

```text
cte:    54029B658D5ECE3A35E6D7D5CEE41EF4E83B6219A07E8A5BFD26954E4AB70696
scalar: AAFCF1B4D2EC65E605079EA63D8E77C77700F545FB02D64EF903EFB88DA25601
values: 3CA9B01406E5BC76E246709E83F2FCF6056E9D23C2A8392AB7AFB7B0FA3B48FC
```

The ordinary qualification tests enforce the hard shape gates before timing is
considered:

* generated preparation contains typed constructors and no object argument
  packs, reflection invocation, or dynamic invocation;
* generated methods are selected from constructors present in the Execution IR
  rather than name fragments; their generated callees and approved Musoq
  runtime helpers are traversed until the declared provider boundary;
* generated compute/CTE methods and approved runtime helpers contain no `box`
  IL instruction;
* empty collections use the immutable typed singleton;
* typed and generated fixtures produce the same checksums and source results;
* allocation telemetry records generated execution separately from the
  independent typed/result cohorts. Timing and allocation acceptance comes
  from the reproducible BenchmarkDotNet report below, not a machine-dependent
  unit-test threshold.

Any performance change that adds per-element boxing, per-row dictionaries,
reflection calls, conversion closures, result-loop reconstruction, or an
unproven intermediate copy fails the review.  Compare scalar-source and VALUES
baseline workloads separately before accepting a change.

## Wave 12 reproducibility record

The report is checked in with this wave after the focused cohorts complete. It
must contain the exact commit/tree, SDK and runtime, operating system and CPU,
BenchmarkDotNet version, filters and job settings, raw JSON artifact paths,
required-result versus avoidable-conversion allocation accounting, paired
typed/generated results, scalar/VALUES comparisons, and generated-IL findings.
Raw BenchmarkDotNet output remains ignored under `BenchmarkDotNet.Artifacts/`.

Run from a clean Release build with normal parallelism:

```powershell
$env:NuGetAudit = 'false'
dotnet restore src/dotnet/Musoq.sln --nologo --verbosity quiet
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj -c Release --no-build -- `
  --filter "*StructuredInputPreparationBenchmark*" --job short --memory --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/structured-input-wave12"
```

Use focused filters for confirmatory alternating-order cohorts, for example
`*StructuralRecordConstruction*`, `*StructuralNumericStructConstruction*`,
`*StructuralNestedConstruction*`, `*StructuralNullableConstruction*`,
`*HostNormalizationAndSourceInvocation*`, and
`*CteProductionAndAdaptation*`. A positive paired timing result is repeated in
two opposite-order cohorts; a reproducible regression is fixed before the
wave is committed. The final report records artifact hashes and exact commands
rather than embedding machine-specific thresholds in unit tests.

### Wave 12 evidence

The focused runs were executed from source revision
`e52cb2ef53cba18f1fb2d4801db90b05d6ffc767` (the Wave 12 working tree; the
commit created after the final gate contains the same tested files). The
environment was Windows 11 Home build `26200`, Intel Core Ultra 9 285K (24
logical cores), .NET SDK `10.0.303`, .NET `10.0.11`, and BenchmarkDotNet
`0.15.8`, with concurrent workstation GC. Every run used
`--job short --memory --exporters json` and a separate ignored artifact
directory under `BenchmarkDotNet.Artifacts/structured-input-wave12-*`.

ShortRun results below show the two representative points (`Size=32` and the
one-node-below-limit stride (`Size=19,999`). `Allocated` is inclusive of the required
compiled query, provider, and result storage; it is not presented as
conversion garbage. The typed rows are independently written construction
baselines. Full five-size CSV/JSON reports are retained in the artifact
directories.

| Cohort | Size 32 mean / allocated | Size 19,999 mean / allocated |
|---|---:|---:|
| Typed record construction | 73.852 ns / 792 B | 99,948.792 ns / 480,020 B |
| Generated record conversion | 82.60 us / 334.82 KB | 8,754.04 us / 11,593.93 KB |
| Generated numeric struct conversion | 80.80 us / 335.28 KB | 7,492.91 us / 11,152.45 KB |
| Generated nested conversion | 56.66 us / 245.39 KB | 6,999.46 us / 8,556.51 KB |
| Generated nullable conversion | 55.80 us / 244.23 KB | 2,086.72 us / 2,317.29 KB |
| Inline source invocation | 81.18 us / 334.82 KB | 8,876.01 us / 11,593.93 KB |
| Retained `let` invocation | 80.89 us / 335.19 KB | 79.14 us / 329.62 KB* |
| Host capture and invocation | 88.38 us / 346.11 KB | 8,815.20 us / 11,593.93 KB |
| CTE production and adaptation | 82.00 us / 337.28 KB | 4,338.59 us / 4,924.76 KB |
| Result enumeration after preparation | 269.67 ns / 2,360 B | 469,819.05 ns / 2,053,327 B |
| Scalar source cohort | 79.91 us / 329.54 KB | 78.46 us / 329.54 KB |
| VALUES source cohort | 1,040.4 ns / 7.1 KB | 461.5 ns / 3.45 KB* |
| Parse, bind, metadata | 15.681 ms / 10.82 MB | 11.677 ms / 4.18 MB* |

`*` The boundary `let`, VALUES, and parse rows deliberately use a small
representative query because compiling a 20,000-element constant SQL literal
would measure multi-megabyte source generation rather than the preparation
path. The full-size boundary capture is covered by the host, inline, numeric,
nested, nullable, and CTE cohorts.

The focused filters and artifact directories were:

```text
*StructuralRecordConstruction*        -> structured-input-wave12-record-short
*TypedRecordConstruction*              -> structured-input-wave12-typed-record-short
*StructuralNumericStructConstruction* -> structured-input-wave12-numeric-short
*StructuralNestedConstruction*         -> structured-input-wave12-nested-short
*StructuralNullableConstruction*       -> structured-input-wave12-nullable-short
*InlineSourceInvocation*                -> structured-input-wave12-inline-short
*RetainedLetSourceInvocation*           -> structured-input-wave12-let-short
*HostNormalizationAndSourceInvocation* -> structured-input-wave12-host-short
*CteProductionAndAdaptation*            -> structured-input-wave12-cte-short
*ResultEnumerationAfterPreparation*     -> structured-input-wave12-result-short
*ScalarSourceInvocation*                -> structured-input-wave12-scalar-short
*ValuesSourceInvocation*                -> structured-input-wave12-values-short
*ParseBindAndMetadata*                  -> structured-input-wave12-parse-short
```

The SHA-256 hashes of the compressed JSON reports are:

```text
record-short:       B82F836F493C3FFCDCD9F92144B1B09BB639C409B0B04BF6AFED22B17629C97A
typed-record-short:  82535AE367CE8DDA2EB76D1BD8FAA9BB8E291E50837A88AC55D3564AC46ABA8F
numeric-short:      B0DF8F2A375171F2D66EFC1ED9E69F9E53E7F238791584865EBC33642C9A33A7
nested-short:       3BBF0B819481AA4825A941C5349101C864F5F8D0502AD57F6FCE61613B13B437
nullable-short:     04E1FF983048854E2CE985635055E5504F63F0A27BB96FD8D8E2FBABC37295D7
inline-short:       0874CD8323CE7ABC72568D96EE90CC3086C46364C10C2A3390C7E44927B7B662
let-short:          75AF2B0CE5EEF37EE451CB6109889DD9B5990978DD843052BEDF3676C3B09085
host-short:         A46D43EA8129A2B12FD5D67D0A0F2EC98C54725317446B0051BC9D327FD110F3
cte-short:          D4B6FA8F23F63F923A77E2BB434F2494F3FC61CD6C188A1283FB4AFA201E9659
result-short:       F0635E7F8177A93C54FDF8703380F907A5287DBAEAAB4325DAB1A5D688EE5606
scalar-short:       9FBB4022C2BDA3C66A9AEAC88AC620B9E2199BE4B7BD689D791C3659047BC69F
values-short:       2D2E7E6383730408961C6DBFB1F7C0FBF8C921E15062E0BF5EC0847760C89A8C
parse-short:        3E8E23BF7DCD0E8CA3447D5746D765B44DFAEED4A93F9B729477F11A5966C251
```

Generated-shape tests passed 8/8. They resolved hot roots from Execution IR
callable IDs, traversed generated callees and approved evaluator helpers, and
found no `box`, reflective array access, reflection invocation, delegate
invocation, iterator access, per-record dictionary, or object-packed
structural path. The generated benchmark source contains no `StructuralValue`
tree creation or manual conversion helper. Allocation checks compare
checksums and required result allocation separately from the typed baseline;
they do not turn a whole-query allocation number into a false zero-allocation
claim.
