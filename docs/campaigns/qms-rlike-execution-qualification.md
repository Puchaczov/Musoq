# Optimized `RLIKE` execution qualification receipt

Status: **accepted with documented performance exceptions** on 2026-09-14.
Correctness, culture safety, exception timing, bounded memory, generated-code,
and result-parity gates passed. The unchanged `1.03x` performance gate reports
three failures; the campaign author explicitly relaxed performance
qualification before QMS-R20 and accepted close/noisy results. The failures are
retained below and are not relabeled as improvements.

## Outcome and ownership

Core now gives SQL `RLIKE` three explicit execution paths:

- the rigorously limited literal, `\Aliteral`, `literal\z`, and
  `\Aliteral\z` subset lowers to ordinal `ExecutionStringMatch` operations;
- other constant and safe loop-invariant patterns lower to
  `ExecutionPrepareRLikeMatcher` and `ExecutionPreparedRLikeMatch`;
- row-varying patterns lower to `ExecutionDynamicRLikeMatch` and a bounded
  two-entry execution-local cache.

Prepared regexes are lazy, culture-aware, and retain the historical 250 ms
timeout and invalid-pattern behavior. Generated SQL contains no
`new Operators().RLike`; the instance method remains a compatibility wrapper.
Execution IR and Host ABI are v8, `rlike-matcher-v1` is registered, and package
and compiled-artifact formats remain unchanged.

`RLIKE` remains evaluator residual execution. This campaign adds no datasource
transfer, datasource or Search change, parser syntax, custom regex engine, or
traversal-pruning claim.

## Commit receipt

History was preserved and advanced through the requested waves:

| Wave | Commit | Outcome |
| --- | --- | --- |
| QMS-R20 | `587439bcb` | Culture-safe cache key, baseline, and query conformance catalog |
| QMS-R21 | `b90cec34b` | Result coverage across query execution contexts |
| QMS-R22 | `58cd6b21a` | Lazy prepared matchers and bounded dynamic cache slots |
| QMS-R23 | `0a579cc87` | Prepared/dynamic Execution IR, ABI v8, and target contracts |
| QMS-R24 | `517d4ea1a` | Explicit constant, dynamic, and APPLY lowering |
| QMS-R25 | `28a06edd4` | Allocation-free literal classifier and ordinal specialization |
| QMS-R26 | `67192c208` | Gate, benchmark oracles, qualification, and documentation |
| QMS-R26 corrective | this receipt's commit | Serialize the collectible-assembly GC diagnostic after repeated CLR host crashes |

Every product defect discovered during the campaign received a dedicated
regression in its wave. Notable cases include the pattern-only culture cache,
parallel telemetry collection, missing RLIKE final-projection fusion for
correlated APPLY, and generated declaration-pattern collisions. R26 also found
and fixed a benchmark-fixture defect: APPLY rows recorded child/pattern reads
into a private recorder instead of the recorder exposed by the benchmark. The
exact counter test now protects that wiring.

The retained local baseline ref is `qms-rlike-baseline` at
`32d33472d70aeb1bedb20d575425536bb4dbdfea`. Its parent is the requested
starting implementation `a66a25b8259401186c42a35f4ee59b1998631594`; the
extra commit adds only the benchmark harness and was not merged or pushed. The
current cohort measures implementation SHA
`28a06edd4ea6b155e8d386cf9c4427450f40f50e`; R26 changes only qualification
code, benchmark observability, tests, and documentation.

## Conformance and deterministic counters

`RLikeQueryConformanceCatalog` uses hard-coded expected schemas and ordered
rows. It covers regex features and anchors, literal/same-row/parameter/variable/
method/CASE/COALESCE/APPLY pattern sources, ASCII and Unicode, nulls,
negation, invalid patterns, repeated execution, and invariant, `en-US`,
`pl-PL`, and `tr-TR` cultures. Query contexts include projection, filtering,
boolean composition, CASE, joins, CROSS/OUTER APPLY, aggregate FILTER, HAVING,
QUALIFY, windows, ordinary and recursive CTEs, set branches, paging,
quantifiers, and parallel execution.

Representative benchmark result oracles are pinned as SHA-256 values and do
not call production matching code to derive expectations:

| Workload | SHA-256 |
| --- | --- |
| Dynamic literal, cardinality 1 | `97BB7DF94C7C0EF5A23F5618375614542D2B4984996123026A2D2E6EA5806FC5` |
| Dynamic Unicode, cardinality 4,096 | `845B7FCC1EC1FBF761F14C4CFC083053F884A3F0DE3B509731DB5103EA45ABF7` |
| Dynamic complex regex, cardinality 4,096 | `BAA9D8F5308F655395ECB617F6E40318D574CDFB547A03452D903B97F1CF9A54` |
| Constant Unicode | `B2CBF8A05718911676F94DE10A30E4036F749528436A330F70BBF760DEB5DEFB` |
| Complex regex, 4,096-code-unit input | `F15C3B2E89A0E641833D8C2C1A984C056DDE5A7C14609295B07E36B118CD2F43` |
| Correlated APPLY, fan-out 64 | `0E42E91377154960BDC8D5692EF181A60CA63CABDB1AE8A3ECB639EB12756645` |

For eight outer rows, each APPLY execution invokes the root source once and
reads the child collection exactly eight times. Inner-bound patterns perform
zero outer-pattern reads; prepared outer patterns perform exactly eight, one
per outer row. Runtime cache probes additionally prove:

- null operands: zero entries and zero matcher/cache misses;
- two uses of one literal pattern: one matcher construction/cache miss and zero
  regex constructions;
- 1,024 distinct patterns: exactly 1,024 constructions/misses while retaining
  only two entries;
- an `en-US`, `tr-TR`, `en-US` prepared sequence: three culture-specific regex
  resolutions with historical results preserved.

## Measurement boundary

BenchmarkDotNet recorded:

- Windows 11 `10.0.26200.9445` on Intel Core Ultra 9 285K, 24 physical and 24
  logical cores;
- .NET SDK `10.0.303`, runtime `10.0.11`, x64 RyuJIT x86-64-v3;
- BenchmarkDotNet `0.15.8`, `ShortRun`, one launch, three warmups, and three
  measured iterations;
- AVX2/AVX, BMI1/BMI2, FMA, SSE through SSE4.2, and 256-bit vectors enabled.

One isolated baseline and one isolated current cohort used the identical
command from clean Release builds:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj `
  -c Release --no-build -- `
  --filter "*RLike*" --job short --memory --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/qms-rlike-<baseline-or-current>"
```

Both cohorts completed 39/39 cases. Baseline took 6:53 and current took 6:09.
Raw artifacts remain ignored. With one report per side, each table entry is the
single-cohort median of report means; every report mean contains three measured
iterations.

The six report pairs and SHA-256 hashes are:

| Report | Baseline SHA-256 | Current SHA-256 |
| --- | --- | --- |
| Legacy regex optimization | `8B93EDB21DF3AA9137B4251FB14297033A87B22BC7F1C095FDDC7AFA8C7E669C` | `4C4593B4E58F111C1AA77CEC441B6A65181CFAD003550B1C63793181AC45200F` |
| APPLY | `AE8A19AC512A18CE7122ED867684F8E6072F24DB52CC72F7569C9E8CE1279685` | `8C35CD2DF0FE91251C3815F2BC0C936DC5582FEB63666B0C072C9C3E4917BEE3` |
| Compilation | `B752E40278FA499DC0A45CD5AA0E6F2A766628727D06D7FAB729BEEFD47F43F6` | `7AEEA48AB5A5D84FEF36ACA2F0CD844EBFD4182D1A05390558128F5E79F95219` |
| Constant | `8508F941DFEE9D3CBC935B59824B709D0E35025EFF77DCE420031178AED0C7D6` | `B4BBC02AE5F5524453E3BE5F7761C9483840FD4BE735A7B41B97580C4D3D20CC` |
| Dynamic | `8F58D3CDE3742B94964E94A3F8BDB7060E64161848DD1A2F30E0164A462A474F` | `EF1B3DA097E9EB074B9B4E83A0B3C59045F73E76A54E629AE287EB717DDD3271` |
| Input length | `86855621A4CA0A6829370096BD303D346A2A4803DF12F41A7661F217B183355E` | `FC3013D954C48C6FF9E7FFB34C42D6FB2F49F94278DF5F72BE2362585F1E8371` |

## Benchmark results

Times are microseconds per compiled query execution. Allocations are bytes per
operation. Ratios are current divided by baseline; lower is better.

### Constant patterns and legacy guard

| Case | Baseline us | Current us | Time | Baseline B | Current B | Allocation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Legacy 1,000-row RLIKE | 90.15 | 87.07 | 0.9658x | 261,236 | 237,345 | 0.9085x |
| Literal | 11,167.04 | 9,082.88 | 0.8134x | 9,541,866 | 7,140,538 | 0.7483x |
| Anchored literal | 11,721.95 | 8,770.39 | 0.7482x | 9,541,699 | 7,140,741 | 0.7484x |
| Complex regex | 12,590.16 | 10,472.31 | 0.8318x | 9,541,700 | 7,140,689 | 0.7484x |
| Unicode literal | 11,475.88 | 8,806.66 | 0.7674x | 9,541,737 | 7,140,537 | 0.7483x |

### Dynamic pattern cardinality

| Cardinality | Scenario | Baseline us | Current us | Time | Baseline B | Current B | Allocation |
| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | Literal | 1,208.87 | 633.40 | 0.5240x | 1,218,103 | 737,907 | 0.6058x |
| 1 | Anchored literal | 1,164.23 | 604.69 | 0.5194x | 1,218,100 | 737,905 | 0.6058x |
| 1 | Complex | 1,319.09 | 820.68 | 0.6222x | 1,218,071 | 737,882 | 0.6058x |
| 1 | Unicode | 1,210.31 | 629.81 | 0.5204x | 1,218,081 | 737,919 | 0.6058x |
| 2 | Literal | 1,236.67 | 634.16 | 0.5128x | 1,218,101 | 738,013 | 0.6059x |
| 2 | Anchored literal | 1,165.80 | 593.04 | 0.5087x | 1,218,103 | 738,001 | 0.6059x |
| 2 | Complex | 1,280.50 | 833.75 | 0.6511x | 1,218,061 | 738,046 | 0.6059x |
| 2 | Unicode | 1,155.17 | 666.04 | 0.5766x | 1,218,098 | 738,017 | 0.6059x |
| 64 | Literal | 1,192.55 | 643.26 | 0.5394x | 1,218,091 | 743,949 | 0.6107x |
| 64 | Anchored literal | 1,196.52 | 654.64 | 0.5471x | 1,218,066 | 743,920 | 0.6107x |
| 64 | Complex | 1,326.03 | 782.64 | 0.5902x | 1,218,093 | 745,941 | 0.6124x |
| 64 | Unicode | 1,201.84 | 621.52 | 0.5171x | 1,218,081 | 743,955 | 0.6108x |
| 512 | Literal | 1,212.65 | 735.09 | 0.6062x | 1,218,082 | 786,987 | 0.6461x |
| 512 | Anchored literal | 1,299.32 | 677.69 | 0.5216x | 1,218,053 | 786,989 | 0.6461x |
| 512 | Complex | 1,364.30 | 997.06 | 0.7308x | 1,218,046 | 803,312 | 0.6595x |
| 512 | Unicode | 1,320.42 | 691.81 | 0.5239x | 1,218,053 | 786,977 | 0.6461x |
| 4,096 | Literal | 1,304,492.80 | 1,018.18 | 0.0008x | 33,425,712 | 1,131,255 | 0.0338x |
| 4,096 | Anchored literal | 1,556,298.90 | 1,003.08 | 0.0006x | 36,665,128 | 1,131,254 | 0.0309x |
| 4,096 | Complex | 3,936,584.27 | 3,980,802.73 | 1.0112x | 46,681,456 | 46,758,048 | 1.0016x |
| 4,096 | Unicode | 1,559,086.00 | 1,010.37 | 0.0006x | 36,665,128 | 1,131,214 | 0.0309x |

The very large 4,096-cardinality gains apply only to patterns admitted by the
literal classifier. The complex-regex case still constructs and runs .NET
Regex and is correctly reported as noise, not an improvement.

### Input length

| Length | Scenario | Baseline us | Current us | Time | Baseline B | Current B | Allocation |
| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 4 | Literal | 1,124.41 | 652.45 | 0.5803x | 1,218,160 | 738,045 | 0.6059x |
| 4 | Complex | 1,225.98 | 769.79 | 0.6279x | 1,218,129 | 738,055 | 0.6059x |
| 64 | Literal | 1,223.57 | 627.60 | 0.5129x | 1,218,129 | 738,004 | 0.6059x |
| 64 | Complex | 1,450.06 | 926.47 | 0.6389x | 1,218,108 | 738,009 | 0.6059x |
| 4,096 | Literal | 12,284.25 | 6,329.88 | 0.5153x | 1,217,873 | 737,800 | 0.6058x |
| 4,096 | Complex | 17,449.53 | 12,395.40 | 0.7104x | 1,217,873 | 737,967 | 0.6059x |

### Correlated APPLY

| Case | Baseline us | Current us | Time | Baseline B | Current B | Allocation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Inner pattern, fan-out 1 | 368,170.13 | 206.26 | 0.0006x | 9,394,328 | 561,844 | 0.0598x |
| Inner pattern, fan-out 8 | 924,529.43 | 532,536.47 | 0.5760x | 20,147,464 | 10,434,480 | 0.5179x |
| Inner pattern, fan-out 64 | 947,223.27 | 529,596.87 | 0.5591x | 29,744,536 | 12,728,312 | 0.4279x |
| Outer pattern, fan-out 1 | 397,685.57 | 399,775.13 | 1.0053x | 10,098,840 | 10,108,680 | 1.0010x |
| Outer pattern, fan-out 8 | 399,505.23 | 412,946.43 | **1.0336x** | 11,303,280 | 10,688,496 | 0.9456x |
| Outer pattern, fan-out 64 | 403,799.70 | 403,993.17 | 1.0005x | 20,937,288 | 15,276,160 | 0.7296x |

The fan-out-1 inner pattern is always an admitted anchored literal; the
baseline constructs thousands of Regex instances while the current path uses
ordinal matching. Higher fan-outs mix literal and complex regex patterns. The
outer-pattern cases are source-work dominated: time is neutral while
allocation falls at fan-outs 8 and 64.

### Compilation

| Case | Baseline us | Current us | Time | Baseline B | Current B | Allocation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Dynamic column | 8,718.74 | 12,355.05 | **1.4171x** | 2,639,827 | 2,647,415 | 1.0029x |
| Correlated APPLY | 10,644.32 | 16,807.39 | **1.5790x** | 5,362,026 | 4,529,582 | 0.8448x |

Compilation was investigated with a second isolated diagnostic pair. Its
dynamic result reversed to 0.8933x (14,038.01 us to 12,539.95 us), proving high
ShortRun variance; APPLY remained slower at 1.3038x (12,254.53 us to 15,977.43
us), while allocation improved to 0.8431x. Explicit IR operations, cache-slot
ownership, and matcher placement add one-time planning/rendering work. The
repository benchmark policy permits a compilation/execution trade-off when
execution gains are significant. The formal pair above remains the recorded
gate input and is not replaced by the diagnostic.

## Gate and acceptance decision

`gate-rlike` requires exactly one complete report for each of the six benchmark
families and compares all 39 methods with the unchanged `1.03x` time and
allocation ceilings. It labels ratios below `0.97x` as improved, ratios within
the gate as noise, and exceeded ratios as failed. It rejects missing, extra,
misassigned, or partial reports.

The formal gate reports 33 improvements, three noise results, and exactly
three failures:

| Case | Ratio | Decision |
| --- | ---: | --- |
| Outer-pattern APPLY, fan-out 8 | 1.0336x time, 0.9456x allocation | Noisy 0.36 percentage-point miss; author accepted |
| Dynamic-column compilation | 1.4171x time, 1.0029x allocation | One-time trade-off; author accepted after diagnostic |
| APPLY compilation | 1.5790x time, 0.8448x allocation | One-time trade-off; author accepted after diagnostic |

The command intentionally exits 1 and prints `RLIKE qualification failed.`.
The author's performance relaxation changes the campaign completion decision,
not the fixed gate, raw metrics, or labels. All execution workloads except the
single noisy APPLY cell are faster or within noise; no allocation comparison
exceeds `1.03x`.

## Generated code, Execution IR, and SIMD

Q372-Q375 provide tracked constant, dynamic-column, correlated-APPLY, direct,
and fallback RLIKE generated samples. Inspection and manifest tests prove:

- safe constants use ordinal `STRING_MATCH` and BCL `string` operations;
- fallback constants prepare once and retain lazy .NET Regex;
- dynamic patterns call `Operators.RLikeDynamic` with a two-entry cache slot;
- outer-stable APPLY patterns call `Operators.PrepareRLike` once per outer row
  and `Operators.RLikePrepared` after the inner input is bound;
- inner-bound APPLY patterns remain dynamic;
- generated code contains no `new Musoq.Evaluator.Operators().RLike`;
- unsupported portable targets reject the v8 operations before rendering.

Ordinal `string.Equals`, `StartsWith`, `EndsWith`, and `Contains` delegate
hardware selection and vectorization to the .NET BCL/runtime. No handwritten
`Vector128`, `Vector256`, or `Vector512` code was introduced. Arbitrary regex
syntax remains on .NET Regex and is not claimed to use the direct BCL path.

## Validation

QMS-R20 through QMS-R25 each passed focused checks, `git diff --check`, Release
restore, warning-clean Release build, and the complete Release solution suite.
The last implementation tree passed 21,874 tests with three intentional
generated-sample refresh skips and zero failures.

The QMS-R26 pre-commit tree passed `git diff --check`, Release restore, a
warning-clean Release build, and 21,891 tests with the same three intentional
refresh skips and zero failures. Two other full-suite attempts reached 10,113
and 12,212 passing Evaluator tests before the test host terminated with an
internal CLR error. The first crash identified `GC.Collect()` in the existing
collectible-assembly diagnostic; the exact test passed immediately in
isolation. Marking the test `DoNotParallelize` produced one complete green run,
but a third crash at 12,402 tests proved that serial scheduling inside the
long-lived test host was not sufficient. The forced-GC probe now executes in a
fresh child test host with a 30-second parent timeout and captured diagnostics;
the parent test remains serialized, with a dedicated reflection regression
protecting that scheduling contract.

The process-isolation corrective tree passed the focused diagnostic class
10/10, `git diff --check`, Release restore, a warning-clean Release build, and
the complete Release suite: 21,892 passed, three intentional refresh skips,
and zero failures.

The exact committed QMS-R26 tree receives the same final gate before campaign
completion is reported.
