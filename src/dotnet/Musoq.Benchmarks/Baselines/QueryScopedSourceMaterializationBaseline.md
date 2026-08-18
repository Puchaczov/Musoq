# Query-scoped source materialization baseline

This baseline is captured from the unmodified legacy CSV example before query-scoped row support.

Command:

```text
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*QueryScopedSourceMaterializationBenchmark*" --job short --memory --exporters json
```

The first clean run used .NET SDK 10.0.303, .NET 10.0.11, Windows 11, and an Intel Core Ultra 9
285K. The benchmark uses the core CSV example assembly, 2,048 data rows, a header row, and
2/8/32/64 string columns. Its correctness oracle is that full scans return exactly 2,048 rows
and early-take returns 16.

| Fields | Scenario | Mean | Allocated | Notes |
| ---: | --- | ---: | ---: | --- |
| 2 | Legacy CSV rows | 479.2 us | 709.77 KB | baseline |
| 8 | Legacy CSV rows | 968.7 us | 1575.76 KB | baseline |
| 32 | Legacy CSV rows | 2.8273 ms | 5036.71 KB | baseline |
| 64 | Legacy CSV rows | 5.1545 ms | 9653.21 KB | baseline |

The same run reported selective projection at 478.4 us/712.08 KB, 958.3 us/1578.34 KB,
2.8742 ms/5039.17 KB, and 5.1056 ms/9655.71 KB for 2/8/32/64 fields respectively. High
rejection measured 458.5 us, 953.6 us, 2.8223 ms, and 5.1863 ms. Early take measured
214.0 us, 223.6 us, 254.9 us, and 298.5 us with 35.67 KB, 49.2 KB, 103.99 KB, and
184.87 KB allocated.

The generated disassembly and JSON reports remain BenchmarkDotNet artifacts and are not committed.

## Wave 8 qualification run

The same harness was extended with query-scoped readonly-struct and sealed-class carriers,
selective/high-rejection/early-take cases, and a numeric CSV path. The short qualification
command was:

```text
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*Numeric*Materialization*" --job short --warmupCount 2 --iterationCount 5 --invocationCount 32 --memory --exporters json --artifacts "BenchmarkDotNet.Artifacts/wave8-numeric-materialization"
```

It ran on the same .NET SDK/runtime and machine. The correctness oracle executes full scans,
early take, and typed numeric rows before timing. Source-bound numeric results were:

| Fields | Legacy numeric CSV | Query-scoped struct | Legacy allocated | Query-scoped allocated |
| ---: | ---: | ---: | ---: | ---: |
| 2 | 1,569.02 us / 825,859 B | 974.61 us / 317,557 B | 100% | 38.5% |
| 8 | 4,727.54 us / 1,811,369 B | 2,823.24 us / 861,122 B | 100% | 47.5% |
| 32 | 13,037.36 us / 5,748,484 B | 6,970.26 us / 3,031,310 B | 100% | 52.7% |
| 64 | 10,326.69 us / 11,183,057 B | 5,821.30 us / 6,176,272 B | 100% | 55.2% |

The carrier-only matrix measured 0 B per operation for readonly structs and one carrier
allocation per accepted row for sealed classes. It also showed that a string-only object-array
fill is already cheap. The run did not satisfy the original universal 2x materialization gate,
and BenchmarkDotNet did not emit usable per-method disassembly for the generic ref-struct
fixture. Generated-code approval tests are useful structural evidence, but they do not replace
warm JIT evidence. The CSV example therefore remains explicitly opt-in until a corrected,
like-for-like matrix satisfies every activation gate.

## Wave 9 corrected pre-tuning matrix

The Wave 8 comparison was not like-for-like: legacy selective projection materialized every
field, rejection happened at different layers, early-take stopped at different row counts, and
numeric scans did not consume values. Wave 9 replaces those cases with one shared file, exact
metadata, projection, pushdown plan, accepted predicate/take, and checksum per comparison.
Before each timed case the harness compares row count, consumed-value checksum, ordering hash,
and failure behavior for legacy, readonly-struct, and sealed-class modes.

The source/carrier matrix contains 96 cases: 24 methods at 2/8/32/64 fields. It covers nullable
numeric and string values, full and selective projection, high rejection, aggregation, early
take, and all three carrier modes. The compiled matrix contains 36 cases across nine semantic
scenarios, with both warm execution and cold compile-plus-first-run. Cold methods use one
invocation per iteration so BenchmarkDotNet cannot batch compiler runs.

Three independent samples of each matrix were captured on the environment above:

```text
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*QueryScopedSourceMaterializationBenchmark*" --job short --memory --exporters json --artifacts BenchmarkDotNet.Artifacts/query-row-wave9/source-N

dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*QueryScopedCompiledExecutionBenchmark*" --job short --memory --exporters json --artifacts BenchmarkDotNet.Artifacts/query-row-wave9/compiled-N
```

`N` was 1, 2, and 3. Every source report contained 96 unique benchmark identities and every
compiled report contained 36. The checked-in `gate-query-rows` command rejects incomplete or
inconsistent cohorts and uses the median of the three report means and allocations.

Carrier-only numeric medians were:

| Fields | Legacy object array | Query struct | Query class | Legacy allocated | Struct allocated | Class allocated | Struct throughput |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 2 | 11,611.7 ns | 775.9 ns | 4,327.3 ns | 180,224 B | 0 B | 49,152 B | 14.9658x |
| 8 | 39,247.2 ns | 3,087.3 ns | 8,897.1 ns | 573,440 B | 0 B | 98,304 B | 12.7124x |
| 32 | 158,348.3 ns | 63,758.0 ns | 78,733.8 ns | 2,146,304 B | 0 B | 294,912 B | 2.4836x |
| 64 | 335,936.7 ns | 127,661.8 ns | 149,227.3 ns | 4,243,456 B | 0 B | 557,056 B | 2.6315x |

The struct path reduced carrier allocation by 100% at every width. Class allocation corresponds
to exactly one carrier per accepted row: 24, 48, 144, and 272 bytes per row respectively.

End-to-end nullable numeric CSV allocation medians were:

| Fields | Legacy allocated | Query struct allocated | Reduction |
| ---: | ---: | ---: | ---: |
| 2 | 1,018,029 B | 464,314 B | 54.39% |
| 8 | 2,749,393 B | 1,430,819 B | 47.96% |
| 32 | 9,669,998 B | 5,294,247 B | 45.25% |
| 64 | 19,134,635 B | 10,680,472 B | 44.18% |

Compiled-query timing medians were:

| Scenario | Legacy warm | Query warm | Warm ratio | Legacy cold | Query cold |
| --- | ---: | ---: | ---: | ---: | ---: |
| Nullable numeric 2 full | 576.49 us | 518.37 us | 0.8992x | 13.67 ms | 33.68 ms |
| Nullable numeric 8 full | 966.91 us | 843.50 us | 0.8724x | 22.61 ms | 42.69 ms |
| Nullable numeric 32 full | 2,345.80 us | 2,090.55 us | 0.8912x | 36.58 ms | 61.41 ms |
| Nullable numeric 64 full | 4,179.61 us | 3,693.57 us | 0.8837x | 66.01 ms | 108.95 ms |
| Nullable string 8 full | 735.82 us | 643.36 us | 0.8743x | 21.35 ms | 42.01 ms |
| Nullable numeric 8 selective | 631.43 us | 612.77 us | 0.9704x | 13.05 ms | 34.31 ms |
| Nullable string 8 high rejection | 597.55 us | 618.59 us | 1.0352x | 12.47 ms | 33.43 ms |
| Nullable numeric 8 aggregation | 583.79 us | 567.17 us | 0.9715x | 19.63 ms | 40.59 ms |
| Nullable numeric 8 early take | 333.58 us | 348.48 us | 1.0447x | 16.70 ms | 38.54 ms |

BenchmarkDotNet again could not emit the generic ref-struct specialization. The harness now has
a non-inlined concrete-reader wrapper, so the fallback was captured with tiering disabled:

```text
COMPlus_TieredCompilation=0
COMPlus_JitDisasm=*MaterializeNumericRows*
COMPlus_JitDisasmAssemblies=Musoq.Benchmarks
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- jit-query-row
```

The optimized eight-field numeric struct wrapper calls the closed static materializer directly.
Its warm loop contains no boxing helper, `callvirt`, interface dispatch, or virtual-function-pointer
marker. The raw BenchmarkDotNet reports and JIT dump remain ignored artifacts.

The pre-tuning gate passed 29 checks and failed one: nullable numeric early-take warm execution
was 4.47% slower than legacy, exceeding the ordinary 3% regression ceiling by 1.47 percentage
points. All original carrier throughput/allocation, numeric CSV allocation, other warm regression,
and disassembly gates passed. CSV query-scoped rows therefore remain explicit opt-in after Wave 9.

## Wave 10 qualification and activation

Profiling isolated the remaining warm early-take cost to rebuilding immutable `QueryRowField`
metadata and recomputing the shape SHA-256 fingerprint on every query execution. The renderer now
emits one static readonly `QueryRowShape` per fingerprint into the collectible generated query
type. This is query-owned immutable metadata, not a process-wide generated-type or closed-generic
cache, and it leaves with the collectible query assembly.

The complete Wave 9 matrix was rerun unchanged in three independent samples for both benchmark
classes. Reports and the JIT dump were captured beneath the ignored
`BenchmarkDotNet.Artifacts/query-row-wave10` directory:

```text
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*QueryScopedSourceMaterializationBenchmark*" --job short --memory --exporters json --artifacts BenchmarkDotNet.Artifacts/query-row-wave10/source-N

dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*QueryScopedCompiledExecutionBenchmark*" --job short --memory --exporters json --artifacts BenchmarkDotNet.Artifacts/query-row-wave10/compiled-N

$env:COMPlus_TieredCompilation='0'; $env:COMPlus_JitDisasm='*MaterializeNumericRows*'; $env:COMPlus_JitDisasmAssemblies='Musoq.Benchmarks'; dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- jit-query-row

dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- gate-query-rows --source-report <source-1.json> --source-report <source-2.json> --source-report <source-3.json> --compiled-report <compiled-1.json> --compiled-report <compiled-2.json> --compiled-report <compiled-3.json> --disassembly BenchmarkDotNet.Artifacts/query-row-wave10/query-row-jit-disasm.txt
```

The environment remained Windows 11, .NET SDK 10.0.303, .NET 10.0.11, and an Intel Core Ultra 9
285K. Each source report contained all 96 identities, each compiled report contained all 36, and
all correctness oracles passed before timing. The qualification command passed all 30 checks.

Carrier-only numeric medians were:

| Fields | Struct throughput versus legacy | Struct allocation reduction | Struct allocated | Class allocated | One-carrier ceiling |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 2 | 15.3825x | 100% | 0 B | 49,152 B | 65,536 B |
| 8 | 12.7635x | 100% | 0 B | 98,304 B | 114,688 B |
| 32 | 2.4194x | 100% | 0 B | 294,912 B | 311,296 B |
| 64 | 2.6422x | 100% | 0 B | 557,056 B | 573,440 B |

End-to-end nullable numeric CSV allocated 54.39%, 47.96%, 45.25%, and 44.18% less than legacy
at 2, 8, 32, and 64 fields respectively. Warm query-scoped/legacy median time ratios were:

| Scenario | Warm ratio | Allowed maximum |
| --- | ---: | ---: |
| Nullable numeric 2 full | 0.8775x | 1.03x |
| Nullable numeric 8 full | 0.8240x | 1.03x |
| Nullable numeric 32 full | 0.8324x | 1.03x |
| Nullable numeric 64 full | 0.8562x | 1.03x |
| Nullable string 8 full | 0.9711x | 1.05x |
| Nullable numeric 8 selective | 0.9292x | 1.03x |
| Nullable string 8 high rejection | 1.0484x | 1.05x |
| Nullable numeric 8 aggregation | 0.9830x | 1.03x |
| Nullable numeric 8 early take | 1.0113x | 1.03x |

The pinned fallback disassembly again targeted the non-inlined warmed eight-field concrete-reader
specialization. It contained no boxing helper, `callvirt`, interface-dispatch marker, or virtual
function-pointer marker. With every original gate satisfied, parameterless CSV provider/schema
construction now advertises query-scoped rows by default; passing `enableQueryScopedRows: false`
retains an explicit legacy comparison and compatibility path.
