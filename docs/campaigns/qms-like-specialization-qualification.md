# Constant LIKE specialization qualification receipt

Status: **passed** on 2026-09-13. This is local change evidence, not a
cross-machine throughput claim.

## Scope and ownership

The qualified Core change recognizes constant ASCII `LIKE` patterns as exact,
prefix, suffix, or contains matches; represents them as Execution IR
`expr.string-match`; emits direct CLR string operations with the historical
regular-expression behavior as the non-ASCII-input fallback; and negotiates
direct top-level source predicates for candidate-metadata or row-filtering
execution. `CandidateMetadata` means filtering before payload open, decode, and
row materialization. It does not claim filesystem traversal pruning.

No `Musoq.DataSources` files were changed. The Search integration remains the
separate handoff in
[`musoq-datasources-search-string-match-v1.md`](../handoffs/musoq-datasources-search-string-match-v1.md).

## Commit and contract receipt

The four existing implementation commits were preserved without rewriting:

| Original scope | Commit | Recorded state |
| --- | --- | --- |
| W00 | `ab04dcf3a` | Initial four query benchmarks |
| W01 | `e28fce540` | Classifier and direct helpers |
| W03 | `dfde92afd` | Metadata specialization on `ExecutionPatternMatch` |
| W04-W07 | `1accd9d52` | Initial source contract, negotiation, fixture, and short documentation |

The corrective campaign then advanced through these Core commits:

| Wave | Commit | Outcome |
| --- | --- | --- |
| R00 | `8d078bfa4` | Complete benchmark matrix and fixed-threshold gate |
| R01 | `de7b9be1a` | Legacy-result matching semantics and differential corpus |
| R02 | `6003132cc` | Execution IR v6 `expr.string-match` and target feature negotiation |
| R03 | `ac8673955` | Constant `LIKE` lowering to the explicit operation |
| R04 | `c44c01fe7` | Hardened source capability contract |
| R05 | `7124b4afd` | Top-level-conjunct negotiation and exact residual validation |
| R06 | `3ab568e32` | Candidate-metadata execution and independently counted pruning source |
| R07 | `6c67f6d59` | Provider guide and Search handoff |
| R08 correction | `0d144b24e` | Direct-first Unicode fallback guard |
| R08 correction | `473ca0bf1` | Short-span fallback guard qualification fix |
| R08 correction | `9346f8bd2` | Runnable unsealed candidate benchmark fixture |
| R08 correction | `84ff8977b` | Equivalent observable payload workload and allocation-safe candidate matching |
| R08 | this document's commit | Final evidence and threshold decision |

The benchmark-only historical branch is `qms-like-baseline-completion`. It
starts at `ab04dcf3a` and advances forward through `cb4d781c3`, `297e34089`,
`e980b09c9`, `8d5bdd043`, and `ff94e49d6`; it was neither merged nor pushed.

The pre-correction tree at `1accd9d52` demonstrably had Execution IR v5,
`ExecutionPatternMatch` registered as `expr.pattern`, and generated Q269 calls
to `Operators.LikePrefix` and `Operators.LikeContains`. The qualified tree has
Execution IR v6, registers `ExecutionStringMatch` as `expr.string-match`, and
Q269 prints `STRING_MATCH` with pattern, needle, kind, and comparison before
rendering direct `StartsWith`/`Contains` operations plus
`Operators.LikeLegacyRegex` fallback. Host ABI remains v6 and package format
remains v2.

## Measurement boundary

| Cohort | Historical SHA | Current SHA | Reports |
| --- | --- | --- | --- |
| Local query execution | `e980b09c9` | `473ca0bf1` | `qms-r08b-local-baseline-{1,2,3}` / `qms-r08c-local-current-{1,2,3}` |
| Candidate payload execution | `ff94e49d6` | `84ff8977b` | `qms-r08f-candidate-baseline-{1,2,3}` / `qms-r08f-candidate-current-{1,2,3}` |

The local baseline SHA is the `ab04dcf3a` runtime with benchmark-only workload
alignment. The candidate baseline additionally makes the simulated payload hash
observable so the JIT cannot erase it; both nominal baseline arms reject typed
pushdown. The current candidate benchmark uses exactly the same observable
payload loop and changes only the negotiated execution phase.

Environment recorded by BenchmarkDotNet:

- BenchmarkDotNet 0.15.8.
- Windows 11 `10.0.26200.9445` (25H2).
- Intel Core Ultra 9 285K, 24 physical and 24 logical cores.
- .NET SDK 10.0.303.
- .NET runtime 10.0.11, x64 RyuJIT x86-64-v3, concurrent workstation GC.
- `ShortRun`: one launch, three warmups, and three measured iterations.

Each command was run from the applicable clean worktree after a warning-clean
Release build. The cohort number was changed from 1 through 3:

```powershell
dotnet build src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj `
  -c Release --no-restore --nologo --verbosity quiet

dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter '*LikePatternSpecializationBenchmark*' --exporters json `
  --artifacts 'BenchmarkDotNet.Artifacts/qms-r08b-local-baseline-1'

dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter '*LikePatternSpecializationBenchmark*' --exporters json `
  --artifacts 'BenchmarkDotNet.Artifacts/qms-r08c-local-current-1'

dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter '*LikeCandidatePayloadBenchmark*' --exporters json `
  --artifacts 'BenchmarkDotNet.Artifacts/qms-r08f-candidate-baseline-1'

dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter '*LikeCandidatePayloadBenchmark*' --exporters json `
  --artifacts 'BenchmarkDotNet.Artifacts/qms-r08f-candidate-current-1'
```

Raw reports remain ignored. Baseline reports are under the auxiliary worktree's
`BenchmarkDotNet.Artifacts`; current reports are under the main worktree's
`BenchmarkDotNet.Artifacts`.

## Three-cohort medians

Times are microseconds per compiled query execution. Allocations are bytes per
operation. Ratios are current divided by historical baseline.

| Local workload and rows | Baseline us | Current us | Time | Baseline B | Current B | Allocation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Contains, 10k | 245.902 | 124.548 | 0.5065x | 539605 | 299499 | 0.5550x |
| Contains, 100k | 3143.960 | 1527.960 | 0.4860x | 3382708 | 981593 | 0.2902x |
| Exact, 10k | 203.703 | 67.698 | 0.3323x | 475101 | 234987 | 0.4946x |
| Exact, 100k | 1593.978 | 375.549 | 0.2356x | 2639895 | 238875 | 0.0905x |
| Prefix, 10k | 219.597 | 90.997 | 0.4144x | 539607 | 299497 | 0.5550x |
| Prefix, 100k | 2921.675 | 1326.694 | 0.4541x | 3382701 | 981593 | 0.2902x |
| Suffix, 10k | 289.011 | 91.824 | 0.3177x | 539608 | 299498 | 0.5550x |
| Suffix, 100k | 3581.914 | 1333.956 | 0.3724x | 3382701 | 981592 | 0.2902x |
| Multiple matches, 10k | 247.029 | 91.502 | 0.3704x | 563620 | 299498 | 0.5314x |
| Multiple matches, 100k | 3140.595 | 1302.710 | 0.4148x | 3622799 | 981593 | 0.2709x |
| `_` fallback, 10k | 375.726 | 366.166 | 0.9746x | 475102 | 475102 | 1.0000x |
| `_` fallback, 100k | 3203.946 | 3203.390 | 0.9998x | 2639910 | 2639910 | 1.0000x |
| Interior `%` fallback, 10k | 392.461 | 391.867 | 0.9985x | 539607 | 539606 | 1.0000x |
| Interior `%` fallback, 100k | 4479.709 | 4558.308 | 1.0175x | 3382666 | 3382666 | 1.0000x |
| Non-ASCII pattern fallback, 10k | 543.034 | 544.310 | 1.0024x | 475006 | 475006 | 1.0000x |
| Non-ASCII pattern fallback, 100k | 5034.608 | 5048.042 | 1.0027x | 2639830 | 2639830 | 1.0000x |
| Dynamic pattern fallback, 10k | 335.999 | 337.849 | 1.0055x | 894574 | 894572 | 1.0000x |
| Dynamic pattern fallback, 100k | 8869.253 | 9085.893 | 1.0244x | 6571077 | 6571077 | 1.0000x |
| Unicode input, 10k | 270.116 | 268.248 | 0.9931x | 846550 | 606416 | 0.7163x |
| Unicode input, 100k | 7594.103 | 6872.266 | 0.9049x | 6090926 | 3689630 | 0.6058x |

| Candidate workload and rows | Baseline us | Current us | Time | Baseline B | Current B | Allocation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Source planning off, 10k | 1357.875 | 1186.451 | 0.8738x | 662570 | 423039 | 0.6385x |
| Source planning off, 100k | 12781.018 | 11412.377 | 0.8929x | 3543470 | 1143796 | 0.3228x |
| Candidate metadata on, 10k | 1351.765 | 427.109 | 0.3160x | 662570 | 671335 | 1.0132x |
| Candidate metadata on, 100k | 12844.041 | 2488.754 | 0.1938x | 3543470 | 3551527 | 1.0023x |

## Result parity and payload counters

An unmeasured qualification probe invoked every compiled benchmark query once
on both exact local-measurement SHAs. It serialized each row as invariant
length-prefixed values and computed SHA-256. Every baseline/current count and
hash pair was identical:

| Rows | Workloads | Count | SHA-256 |
| ---: | --- | ---: | --- |
| 10k | Exact; `_` fallback | 1 | `9427F8A0D7E8E68B7830373735E9A0C4DF3F66959C36114771F2C5CDE06329E5` |
| 10k | Prefix; suffix; contains; multiple; interior `%` | 1000 | `B0C924FC330CFBC3D57EAFAAEB5E4C4E346778737D89236C27C401442CAD0B48` |
| 10k | Non-ASCII pattern | 0 | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| 10k | Dynamic pattern | 6000 | `318006B91F80B247DF87117CF71B30FD51B2FCFC5C8E0ED2E4578D7AC663689A` |
| 10k | Unicode input | 5000 | `DD897FFBE98184D8AA2CECF39F38E4750D9CF3A7A5D5F8A2209921863E3E6942` |
| 100k | Exact; `_` fallback | 1 | `9427F8A0D7E8E68B7830373735E9A0C4DF3F66959C36114771F2C5CDE06329E5` |
| 100k | Prefix; suffix; contains; multiple; interior `%` | 10000 | `1722C31047D5A1526BB34424DB30C6E5F1BF4327E7CB09C33DAC6F17DC41325A` |
| 100k | Non-ASCII pattern | 0 | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| 100k | Dynamic pattern | 60000 | `98D2FF20F08BE2FEB4FD44A6BDA84FC1F08175348F526E77F547E1BFBAD4B887` |
| 100k | Unicode input | 50000 | `B420AEB097A26A6EE3E1B88F34B26B196B8CBBEEBACA913B920B3F36544A3819` |

The candidate benchmark returns the same first 1,000 ordered IDs and payloads
for both sizes and both arms. Its canonical result SHA-256 is
`E01E398494E8A0FB7094309FBBF66574308CF0AC034748B46B4091C857440178`.

| Candidates | Historical payload opens | Candidate-metadata opens | Reduction | Matching count |
| ---: | ---: | ---: | ---: | ---: |
| 10000 | 10000 | 1000 | 90% | 1000 |
| 100000 | 100000 | 1000 | 99% | 1000 |

The dedicated Core source additionally counts each lifecycle stage
independently. Its zero-match case records zero opens, zero decodes, and zero
row materializations. Its seven-candidate partial case records seven of each
without candidate negotiation and exactly two opens, two decodes, and two row
materializations with candidate metadata; row count, order, and hash remain
identical. Malformed provider claims fail before all three counters and disposal
are touched.

## Comparator and acceptance decision

`compare-reports` passed all 20 local methods and all four candidate methods.
The candidate comparator printed these final median ratios:

```text
SourcePlanningOff(10000):  time 0.8738x, allocation 0.6385x
SourcePlanningOff(100000): time 0.8929x, allocation 0.3228x
SourcePlanningOn(10000):   time 0.3160x, allocation 1.0132x
SourcePlanningOn(100000):  time 0.1938x, allocation 1.0023x
Benchmark comparison passed.
```

`gate-like-specialization` also passed. The acceptance boundaries resolve as
follows:

| Requirement | Observed result | Decision |
| --- | --- | --- |
| Enabled local time/allocation no worse than 1.03x | Slowest enabled time ratio 0.5065x; highest enabled allocation ratio 0.5550x | Pass |
| 100k contains time no greater than 0.95x | 0.4860x | Pass |
| Generic/fallback time/allocation no worse than 1.03x | Worst time 1.0244x; worst allocation 1.0000x | Pass |
| Candidate opens reduced by at least 90% | 90% at 10k; 99% at 100k; exact matching count | Pass |
| Candidate median time no greater than 0.80x | 0.3160x at 10k; 0.1938x at 100k | Pass |

No specialization kind was removed. No runtime profitability toggle was added,
and no threshold was weakened.

The comparison commands consume exactly three occurrences of each report
option. This abbreviated invocation shows the reproducible layout:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  compare-reports `
  --baseline <baseline-1.json> --baseline <baseline-2.json> --baseline <baseline-3.json> `
  --current <current-1.json> --current <current-2.json> --current <current-3.json>

dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  gate-like-specialization `
  --local-baseline <local-baseline-1.json> `
  --local-baseline <local-baseline-2.json> `
  --local-baseline <local-baseline-3.json> `
  --local-current <local-current-1.json> `
  --local-current <local-current-2.json> `
  --local-current <local-current-3.json> `
  --candidate-baseline <candidate-baseline-1.json> `
  --candidate-baseline <candidate-baseline-2.json> `
  --candidate-baseline <candidate-baseline-3.json> `
  --candidate-current <candidate-current-1.json> `
  --candidate-current <candidate-current-2.json> `
  --candidate-current <candidate-current-3.json>
```

## Disproven hypotheses and excluded evidence

- Metadata on `ExecutionPatternMatch` was not an adequate target contract; an
  explicit operation and Execution IR v6 negotiation were required.
- `OrdinalIgnoreCase` was not result-equivalent to the historical regex matcher
  for all Unicode input. Kelvin sign and Turkish dotted-I differential tests
  required the non-ASCII input fallback.
- Recursive capability matching was not safe for typed matches nested in `OR`;
  only direct flattened top-level `AND` conjuncts can be offered.
- Projection-before-predicate fixture behavior did not, by itself, prove
  pre-materialization pruning. Independent open/decode/materialization counters
  and an actual candidate-phase boundary were required.
- Direct-first fallback guards alone did not keep the dynamic-pattern cohort
  under 1.03x. The short ASCII-span guard correction was required.
- The original Kelvin-sign Unicode benchmark did not have historical/current
  result parity and was excluded. The replacement uses non-ASCII `Ł` input and
  preserves identical results while still exercising the fallback guard.
- A sealed BenchmarkDotNet fixture was not runnable and produced no qualifying
  report.
- The first candidate reports were invalid because current-only
  `[IterationSetup]` changed invocation behavior and a discarded payload hash
  allowed the JIT to remove nominal payload work. Those directories are
  excluded; only the `qms-r08f-*` candidate reports are qualifying evidence.

## Verification

Every corrective wave ran focused tests before its commit and then the required
restore, warning-clean Release build, and complete Release solution test suite.
The final candidate-execution correction passed 21,378 tests with three
intentional generated-sample refresh skips and no failures. The focused
candidate suites passed 26 scenarios, including all four match kinds against
legacy Unicode behavior and exact 10k/100k payload counters.

The repository-wide, Evaluator, Schema, Benchmark, and architecture guides were
re-read after implementation. The compliance audit confirmed that strategy is
represented in Execution IR, source contracts remain in Schema, source
execution mechanics remain in `Musoq.Tests.Common.SourcePlanning`, renderers
only emit the explicit operation, benchmark arms differ only at the advertised
source capability/phase boundary, generated Q269 is current, and public API/XML
documentation and target-version tests are present.
