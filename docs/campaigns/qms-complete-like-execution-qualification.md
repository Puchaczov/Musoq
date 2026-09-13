# Complete `LIKE` execution qualification receipt

Status: **accepted with documented performance exceptions** on 2026-09-13.
Correctness qualification passed. The original fixed performance gate did not
fully pass; the author of the QMS-R09-R19 campaign explicitly accepted the
measured performance on 2026-09-13 and authorized completion without further
optimization. The failed comparisons remain failures in this receipt and were
not relabeled or hidden.

## Outcome and ownership

The Core implementation now gives SQL `LIKE` three explicit execution paths:

- constant simple ASCII patterns lower to `ExecutionStringMatch` and direct CLR
  string operations, with the historical regex fallback where Unicode folding
  can differ;
- other constant or loop-invariant patterns lower to
  `ExecutionPrepareLikeMatcher` and `ExecutionPreparedLikeMatch`;
- row-varying patterns lower to `ExecutionDynamicLikeMatch` with a bounded
  two-entry execution-local cache.

The required correlated form, including
`m1.Column like m2.Column` after `CROSS APPLY`, remains a dynamic inner-loop
match. The reverse orientation prepares an outer pattern once per outer row.
Generated SQL `LIKE` code contains no `new Operators().Like`; the instance
method remains as a compatibility wrapper. Execution IR and Host ABI are v7,
the target feature is `like-matcher-v1`, target package format remains v2, and
compiled artifact format remains v3.

Cross-source `LIKE` remains an evaluator residual. This campaign does not add a
datasource contract, datasource pushdown, traversal pruning, parser syntax, or
changes outside this repository.

## Commit receipt

History was preserved and advanced through the planned waves:

| Wave | Commit | Outcome |
| --- | --- | --- |
| QMS-R09 | `c0e8c8cfd` | Executable query conformance catalog and crosswalk |
| QMS-R10 | `847667d2d` | Result coverage across query execution contexts |
| QMS-R11 | `efab6ffbe` | Dynamic, Unicode, APPLY, and compilation baseline |
| QMS-R12 | `621216f52` | Culture-safe Unicode matcher preparation |
| QMS-R13 | `f6219c603` | Bounded contention-free dynamic cache reads |
| QMS-R14 | `bfb22e602` | Prepared and dynamic `LIKE` Execution IR and ABI v7 |
| QMS-R15 | `c4ac36d0a` | Explicit lowering of every `LIKE` strategy |
| QMS-R16 | `125c8b95a` | Dependency-safe APPLY matcher preparation |
| QMS-R17 | `a06440129` | Allocation-free runtime classification and one-pass regex source building |
| QMS-R18 | `c78e72560` | Qualified BCL SIMD-backed ASCII probing |
| QMS-R19 correction | `c93142233` | Runtime qualification corrections found by the first cohort |
| QMS-R19 correction | `32b4d9b0f` | Cache and generated-loop qualification corrections |
| QMS-R19 correction | `6c50f99a9` | Final constant and compilation-path qualification corrections |
| QMS-R19 | this receipt's commit | Final evidence and author-approved decision |
| QMS-R19 final-gate correction | following commit | Isolated ASCII-prefix cache churn from legacy compiled-regex stress |

Each production defect found during implementation received a dedicated
regression in the wave that fixed it. Notable cases include qualified pattern
operands, `HAVING` aggregate finalization, long-pattern regex construction,
query-local cache ownership, preparation placement after parameter binding,
worker-local state, stale culture entries, cache accounting, UTF-16 case-folding
exceptions, and generated final-row ownership.

The retained local baseline ref is `qms-dynamic-like-baseline` at
`efab6ffbe4c119a81ea94c945a4dfea84c00550a`. It was not merged or pushed. The
three current cohorts measure implementation SHA
`6c50f99a9a0b3d078f52a716ce1d060d719271f1`; the final receipt commit changes
documentation only.

## Conformance qualification

`LikeQueryConformanceCatalog` provides stable IDs, deterministic source data,
hard-coded expected columns and ordered rows, semantic dimensions, and expected
execution strategy. Expected results do not call the production matcher.

Completeness guards cover:

- exact, prefix, suffix, contains, match-all, repeated `%`, empty, `_`, mixed
  wildcards, interior `%`, regex metacharacters, raw backslashes, and multiline
  inputs;
- literal, same-row column, parameter, variable, concatenation, method result,
  `CASE`, `COALESCE`, and inner-APPLY pattern sources;
- ASCII, ordinary Unicode, culture-folding divergence, composed/decomposed text,
  and surrogate-containing input;
- positive and negated matches, null input, null pattern, and both-null cases;
- projection, `WHERE`, boolean composition, `CASE WHEN`, inner/left joins,
  CROSS/OUTER APPLY, aggregate `FILTER`, `HAVING`, direct `QUALIFY`, window
  filtering, CTE, recursive CTE, set branches, distinct/order/page, predicate
  quantifiers, and parallel execution;
- direct, prepared, and dynamic strategies, including the mandatory
  context/strategy pairs.

Culture-sensitive snapshots run under invariant, `en-US`, `pl-PL`, and `tr-TR`
and include Kelvin sign, long s, dotted/dotless I, sigma forms,
composed/decomposed text, malformed surrogate-containing input, metacharacters,
paths, and line breaks. Cache-sensitive cases execute twice. The detailed audit
is in [the conformance crosswalk](qms-like-query-conformance-crosswalk.md).

Deterministic semantic oracles remained unchanged between the baseline and
current implementation:

| Workload | SHA-256 |
| --- | --- |
| Dynamic ASCII, cardinality 1 | `AA3E9ABE832C6CB723EE3C2D2CE038DCF4C6A294E411623909AE33AE93E6ED99` |
| Dynamic Unicode, cardinality 4,096 | `C79EEAFE47E7ABEC003E4A1982A09D238A8D3F6DE52AD36C26929F82D3140464` |
| Dynamic wildcard, cardinality 4,096 | `E87AEE3ECEF437015DF526BE6B048A57D530E3EE6ABF844F304A543D97664381` |
| Wildcard input length 4,096 | `C2D90865D7EB218BB0CA45A924A1A6BD1688E007DDCE8DCAD223F3BFD29C29FF` |
| Correlated APPLY, fan-out 64 | `0E42E91377154960BDC8D5692EF181A60CA63CABDB1AE8A3ECB639EB12756645` |

Every APPLY execution invokes the root source once. Current execution reads the
child collection once per outer row: eight reads in the eight-row contract
fixture. The inner-pattern orientation performs zero outer-pattern reads; the
prepared outer-pattern orientation performs exactly eight, one per outer row.
The baseline benchmark's observable child-read formula is
`outerRows * (fanOut + 1)`: 2,048, 9,216, and 66,560 at fan-outs 1, 8, and 64.

## Measurement boundary

Environment recorded by BenchmarkDotNet:

- Windows 11 `10.0.26200.9445` on Intel Core Ultra 9 285K, 24 physical/logical
  cores;
- .NET SDK `10.0.303`, runtime `10.0.11`, x64 RyuJIT;
- BenchmarkDotNet `0.15.8`, `ShortRun` with three warmups and three measured
  iterations;
- AVX2/AVX/SSE through SSE4.2, BMI1/BMI2, FMA, and 256-bit vectors enabled.

The same command was run from a clean Release build for cohort numbers 1, 2,
and 3 on each SHA:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter "*DynamicLike*" "*LikePatternSpecializationBenchmark*" `
  --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/qms-dynamic-like-baseline-valid-N"

dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter "*DynamicLike*" "*LikePatternSpecializationBenchmark*" `
  --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/qms-r19-qualified-current-v3-N"
```

All six launches completed 52/52 benchmark cases. Baseline cohorts took about
nine minutes each; current cohorts took 8:22, 8:20, and 8:23. Raw
BenchmarkDotNet artifacts remain ignored. Each artifacts directory contains one
compressed JSON report for each of the five benchmark classes below.

The comparison tools were run with three `--baseline` and three `--current`
arguments per report. `gate-dynamic-like` received the same three reports for
each `dynamic`, `length`, `apply`, `constant`, and `compilation` baseline/current
pair:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  compare-reports `
  --baseline <baseline-1.json> --baseline <baseline-2.json> --baseline <baseline-3.json> `
  --current <current-1.json> --current <current-2.json> --current <current-3.json>

dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  gate-dynamic-like `
  --dynamic-baseline <dynamic-baseline-1.json> `
  --dynamic-baseline <dynamic-baseline-2.json> `
  --dynamic-baseline <dynamic-baseline-3.json> `
  --dynamic-current <dynamic-current-1.json> `
  --dynamic-current <dynamic-current-2.json> `
  --dynamic-current <dynamic-current-3.json> `
  --length-baseline <length-baseline-1.json> `
  --length-baseline <length-baseline-2.json> `
  --length-baseline <length-baseline-3.json> `
  --length-current <length-current-1.json> `
  --length-current <length-current-2.json> `
  --length-current <length-current-3.json> `
  --apply-baseline <apply-baseline-1.json> `
  --apply-baseline <apply-baseline-2.json> `
  --apply-baseline <apply-baseline-3.json> `
  --apply-current <apply-current-1.json> `
  --apply-current <apply-current-2.json> `
  --apply-current <apply-current-3.json> `
  --constant-baseline <constant-baseline-1.json> `
  --constant-baseline <constant-baseline-2.json> `
  --constant-baseline <constant-baseline-3.json> `
  --constant-current <constant-current-1.json> `
  --constant-current <constant-current-2.json> `
  --constant-current <constant-current-3.json> `
  --compilation-baseline <compilation-baseline-1.json> `
  --compilation-baseline <compilation-baseline-2.json> `
  --compilation-baseline <compilation-baseline-3.json> `
  --compilation-current <compilation-current-1.json> `
  --compilation-current <compilation-current-2.json> `
  --compilation-current <compilation-current-3.json>
```

## Three-cohort execution medians

Times are microseconds per compiled query execution. Allocations are bytes per
operation. Ratios are current divided by the QMS-R11 baseline; lower is better.

### Dynamic cardinality

| Case | Baseline us | Current us | Time | Baseline B | Current B | Allocation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ASCII, cardinality 1 | 1,254.302 | 658.042 | 0.5246x | 1,218,114 | 738,125 | 0.6060x |
| Unicode, cardinality 1 | 1,638.984 | 668.853 | 0.4081x | 1,218,148 | 737,892 | 0.6057x |
| Wildcard, cardinality 1 | 1,025.065 | 629.461 | 0.6141x | 1,218,079 | 738,117 | 0.6060x |
| ASCII, cardinality 2 | 1,276.856 | 680.016 | 0.5326x | 1,218,113 | 738,390 | 0.6062x |
| Unicode, cardinality 2 | 1,629.500 | 655.878 | 0.4025x | 1,218,121 | 737,944 | 0.6058x |
| Wildcard, cardinality 2 | 1,023.404 | 632.403 | 0.6179x | 1,218,079 | 738,415 | 0.6062x |
| ASCII, cardinality 64 | 1,256.893 | 706.802 | 0.5623x | 1,218,119 | 756,754 | 0.6212x |
| Unicode, cardinality 64 | 1,670.450 | 643.645 | 0.3853x | 1,218,033 | 740,887 | 0.6083x |
| Wildcard, cardinality 64 | 1,021.481 | 668.512 | 0.6545x | 1,218,088 | 756,733 | 0.6212x |
| ASCII, cardinality 512 | 1,270.236 | 793.792 | 0.6249x | 1,218,120 | 889,419 | 0.7302x |
| Unicode, cardinality 512 | 1,904.162 | 794.688 | 0.4173x | 1,217,885 | 762,362 | 0.6260x |
| Wildcard, cardinality 512 | 1,084.214 | 846.305 | 0.7806x | 1,218,112 | 889,447 | 0.7302x |
| ASCII, cardinality 4,096 | 13,737.529 | 1,508.171 | 0.1098x | 78,985,866 | 1,950,847 | 0.0247x |
| Unicode, cardinality 4,096 | 851,105.167 | 53,821.873 | 0.0632x | 845,597,752 | 49,009,088 | 0.0580x |
| Wildcard, cardinality 4,096 | 13,636.239 | 1,424.484 | 0.1045x | 79,116,554 | 1,950,835 | 0.0247x |

### Dynamic input length

| Case | Baseline us | Current us | Time | Baseline B | Current B | Allocation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ASCII, 4 code units | 90.838 | 78.115 | 0.8599x | 324,886 | 294,631 | 0.9069x |
| Unicode, 4 code units | 112.452 | 75.499 | 0.6714x | 324,886 | 278,744 | 0.8580x |
| Wildcard, 4 code units | 90.008 | 72.914 | 0.8101x | 324,886 | 294,631 | 0.9069x |
| ASCII, 64 code units | 121.496 | 86.408 | 0.7112x | 324,886 | 294,631 | 0.9069x |
| Unicode, 64 code units | 213.842 | 78.510 | 0.3671x | 324,886 | 278,744 | 0.8580x |
| Wildcard, 64 code units | 90.246 | 72.658 | 0.8051x | 324,886 | 294,631 | 0.9069x |
| ASCII, 4,096 code units | 2,314.951 | 704.025 | 0.3041x | 324,901 | 294,639 | 0.9069x |
| Unicode, 4,096 code units | 2,298.688 | 479.833 | 0.2087x | 324,901 | 278,750 | 0.8580x |
| Wildcard, 4,096 code units | 89.010 | 73.644 | 0.8274x | 324,886 | 294,631 | 0.9069x |

### Correlated APPLY

| Case | Baseline us | Current us | Time | Baseline B | Current B | Allocation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Inner pattern, fan-out 1 | 3,434.816 | 222.510 | 0.0648x | 20,035,736 | 766,652 | 0.0383x |
| Inner pattern, fan-out 8 | 7,986.339 | 456.515 | 0.0572x | 40,728,952 | 1,356,600 | 0.0333x |
| Inner pattern, fan-out 64 | 29,840.282 | 4,430.664 | 0.1485x | 50,365,744 | 3,650,870 | 0.0725x |
| Outer pattern, fan-out 1 | 3,449.946 | 208.359 | 0.0604x | 20,084,892 | 758,459 | 0.0378x |
| Outer pattern, fan-out 8 | 4,347.222 | 434.955 | 0.1001x | 21,289,336 | 1,332,082 | 0.0626x |
| Outer pattern, fan-out 64 | 23,555.356 | 6,760.197 | 0.2870x | 30,924,994 | 5,920,647 | 0.1915x |

### Constant, wildcard, and Unicode guardrails

| Case | Baseline us | Current us | Time | Baseline B | Current B | Allocation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Contains, 10k | 124.968 | 116.204 | 0.9299x | 299,499 | 299,498 | 1.0000x |
| Contains, 100k | 1,540.846 | 1,575.576 | 1.0225x | 981,593 | 981,592 | 1.0000x |
| Exact, 10k | 67.968 | 68.459 | 1.0072x | 234,987 | 234,987 | 1.0000x |
| Exact, 100k | 387.698 | 380.450 | 0.9813x | 238,875 | 238,875 | 1.0000x |
| Multiple matches, 10k | 90.498 | 86.420 | 0.9549x | 299,498 | 299,498 | 1.0000x |
| Multiple matches, 100k | 1,322.800 | 1,286.304 | 0.9724x | 981,592 | 981,593 | 1.0000x |
| Prefix, 10k | 91.565 | 90.539 | 0.9888x | 299,498 | 299,498 | 1.0000x |
| Prefix, 100k | 1,336.975 | 1,338.895 | 1.0014x | 981,593 | 981,593 | 1.0000x |
| Suffix, 10k | 94.363 | 90.244 | 0.9563x | 299,498 | 299,498 | 1.0000x |
| Suffix, 100k | 1,309.912 | 1,305.248 | 0.9964x | 981,593 | 981,593 | 1.0000x |
| Dynamic fallback, 10k | 394.411 | 249.100 | 0.6316x | 894,572 | 510,931 | 0.5711x |
| Dynamic fallback, 100k | 9,537.220 | 3,981.268 | 0.4174x | 6,571,077 | 2,729,562 | 0.4154x |
| Interior `%`, 10k | 450.935 | 269.133 | 0.5968x | 539,608 | 299,507 | 0.5550x |
| Interior `%`, 100k | 4,992.312 | 2,992.818 | 0.5995x | 3,382,666 | 981,580 | 0.2902x |
| Non-ASCII pattern, 10k | 586.248 | 296.756 | 0.5062x | 475,006 | 234,901 | 0.4945x |
| Non-ASCII pattern, 100k | 5,420.909 | 2,496.244 | 0.4605x | 2,639,830 | 238,820 | 0.0905x |
| `_` wildcard, 10k | 421.394 | 238.259 | 0.5654x | 475,102 | 234,996 | 0.4946x |
| `_` wildcard, 100k | 3,691.260 | 1,881.476 | 0.5097x | 2,639,910 | 238,899 | 0.0905x |
| Unicode input, 10k | 281.373 | 152.033 | 0.5403x | 606,417 | 606,416 | 1.0000x |
| Unicode input, 100k | 6,754.784 | 5,889.860 | **0.8720x** | 3,689,630 | 3,689,567 | 1.0000x |

### Compilation

| Case | Baseline us | Current us | Time | Baseline B | Current B | Allocation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Dynamic column query | 10,765.136 | 18,089.513 | **1.6804x** | 2,621,167 | 2,646,804 | 1.0098x |
| Correlated APPLY query | 10,125.077 | 20,507.880 | **2.0255x** | 5,352,615 | 4,531,254 | 0.8465x |

## Comparator and acceptance decision

`compare-reports` passed the dynamic-cardinality, input-length, APPLY, and
constant/fallback report families under its 1.03 time/allocation guardrail. It
failed only the compilation report family. `gate-dynamic-like` failed exactly
three comparisons:

| Original requirement | Observed | Original decision | Final campaign decision |
| --- | ---: | --- | --- |
| Constant Unicode/wildcard time no greater than 0.85x | Unicode input 100k: 0.8720x | Fail; still 12.8% faster | Accepted by plan author |
| Compilation time no greater than 1.05x | Dynamic query: 1.6804x | Fail | Accepted by plan author |
| Compilation time no greater than 1.05x | APPLY query: 2.0255x | Fail | Accepted by plan author |

All execution acceptance groups otherwise passed. In particular:

- cardinality 1-2 runs at 0.4025x-0.6179x baseline time;
- cardinality 64-512 runs at 0.3853x-0.7806x;
- cardinality 4,096 runs at 0.0632x-0.1098x and 0.0247x-0.0580x allocation;
- inner-pattern APPLY runs at 0.0572x-0.1485x;
- outer-pattern APPLY runs at 0.0604x-0.2870x;
- dynamic/generic and wildcard fallback paths run at 0.4174x-0.6316x and
  0.0905x-0.5711x allocation;
- constant ASCII remains within the 1.03x guardrail.

The fixed gate still returns exit code 1 and prints
`Dynamic LIKE qualification failed.` This is intentionally retained as exact
evidence. The author-approved relaxation changes the campaign completion
decision, not the recorded comparator result or its thresholds.

## Generated code, IR, and SIMD evidence

Inspection tests prove:

- dynamic operands are evaluated once, left-to-right, before
  `Operators.LikeDynamic` receives a query-local cache slot;
- loop-invariant patterns render `Operators.PrepareLike` before dependent inner
  setup and `Operators.LikePrepared` after the inner input is bound;
- safe constant ASCII patterns render direct ordinal BCL operations, while
  I/K/S-sensitive patterns retain the conservative historical-regex fallback;
- no generated SQL `LIKE` path contains `new Musoq.Evaluator.Operators().Like`;
- unsupported portable targets reject v7 matcher operations before rendering.

Q269 and Q369-Q371 are tracked generated samples for constant, dynamic-column,
correlated-APPLY, and Unicode clause contexts. Their manifest was refreshed
through the repository utility.

The R18 receipt, [BCL SIMD `LIKE` kernels](qms-bcl-simd-like-kernels.md), records
three cohorts, disassembly, and enabled/disabled hardware-intrinsic runs.
`Ascii.IsValid` measured 0.3532x the alternative long-input geometric mean with
zero allocation and no regression; production keeps the scalar probe through
eight code units and uses the BCL primitive for longer spans. No handwritten
`Vector128`, `Vector256`, or `Vector512` kernel was added.

## Disproven hypotheses and excluded evidence

- A process-wide pattern cache alone was not suitable for high-cardinality
  workloads; bounded execution-local slots and selective process-cache reuse
  were both required.
- Inlining a large dynamic matching expression in generated code did not improve
  the full workload and harmed compilation; the compact static call remains.
- Extending direct final-row emission to constant/prepared matching made the
  selective 100k exact case about 1.821 ms versus the 0.388 ms historical
  median; that experiment was reverted. The policy remains dynamic-only.
- A late inline Kelvin/long-s experiment measured about 6.044 ms for the Unicode
  workload and did not improve the committed implementation; it was reverted
  and is excluded.
- A focused compilation diagnostic measured 8.328 ms for the plain query and
  9.409 ms for APPLY, but the formal three-cohort medians were 18.090 ms and
  20.508 ms. The diagnostic is not used to override the formal failure.
- Same-time interleaved diagnostics suggested machine drift and improved 9/12
  constant cases, but only the separated three-cohort reports above are treated
  as formal evidence.
- BCL ordinal string operations already use runtime-selected vectorized kernels;
  a custom SIMD implementation was not justified by the measured boundary.
- The original process-cache churn fixture embedded `_` in every generated
  pattern. Those characters are SQL single-character wildcards, so the test
  constructed 2,048 compiled legacy regex matchers and could crash the .NET test
  host after the full suite. Crash-blame identified that exact in-flight test.
  The bounded-cache test now uses the intended simple ASCII-prefix shape, and a
  separate regression proves that shape leaves legacy-regex construction lazy.
  This is a test-harness correction; production code and benchmarked behavior
  are unchanged.

## Verification

Before the formal cohorts, the exact `6c50f99a9` tree passed:

- 1,310 focused evaluator `LIKE`, renderer, and generated-artifact tests, with
  two intentional generated refresh skips;
- 27 benchmark contract/gate tests;
- 224 architecture-named tests;
- `git diff --check`, Release restore, and warning-clean Release solution build;
- the complete Release solution suite: **21,674 passed, 3 intentional generated
  refresh skips, 0 failed**.

The exact committed QMS-R19 documentation tree receives the same final
`git diff --check`, restore, warning-clean Release build, and complete Release
solution test gate before campaign completion is reported.

The final-gate fixture correction then passed its focused class 7/7 and the
complete pre-commit Release solution suite: **21,675 passed, 3 intentional
generated refresh skips, 0 failed**. The one-test increase is the dedicated
legacy-regex-laziness regression described above.
