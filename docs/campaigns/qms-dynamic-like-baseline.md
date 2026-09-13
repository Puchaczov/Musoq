# Dynamic and Unicode `LIKE` baseline

## Scope and provenance

QMS-R11 measures compiled Musoq SQL before prepared/dynamic Execution IR lowering.
The retained local ref was created from QMS-R10 parent
`847667d2dced72129a8c07e0df5adf65bc084165`; it is not pushed or merged. The
final ref SHA will be recorded in the QMS-R19 qualification receipt. Raw BenchmarkDotNet artifacts are
ignored; this receipt retains commands, environment, medians, semantic oracles,
and artifact locations.

Environment:

- Windows 11 `10.0.26200.9445`, Intel Core Ultra 9 285K, 24 physical/logical cores;
- .NET SDK `10.0.303`, runtime `10.0.11`, x64 RyuJIT;
- AVX2/AVX/SSE through SSE4.2, BMI1/BMI2, FMA, and 256-bit vectors enabled;
- BenchmarkDotNet `0.15.8`, `ShortRun` with three warmups and three measured iterations.

The same command was run three times, replacing `N` with `1`, `2`, and `3`:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- `
  --filter "*DynamicLike*" "*LikePatternSpecializationBenchmark*" `
  --exporters json `
  --artifacts "BenchmarkDotNet.Artifacts/qms-dynamic-like-baseline-valid-N"
```

Each cohort completed 52/52 cases and produced one compressed JSON report per
benchmark class. The table values are the median of the three report means and
the median of the three per-operation allocation values.

## Dynamic pattern cardinality

The compiled query is `p.Email like p.FirstName` over 10,000 rows. Rows sharing
a pattern are contiguous. The warmed process-wide 100-entry baseline cache has
zero steady-state constructions for cardinalities 1, 2, and 64; it constructs
512 or 4,096 matchers per execution once the sequential working set exceeds the
cache.

| Case | Median mean (ns) | Median allocated (B) |
| --- | ---: | ---: |
| ASCII, cardinality 1 | 1,254,301.8 | 1,218,114 |
| Unicode, cardinality 1 | 1,638,984.2 | 1,218,148 |
| Wildcard, cardinality 1 | 1,025,064.6 | 1,218,079 |
| ASCII, cardinality 2 | 1,276,856.5 | 1,218,113 |
| Unicode, cardinality 2 | 1,629,499.7 | 1,218,121 |
| Wildcard, cardinality 2 | 1,023,404.1 | 1,218,079 |
| ASCII, cardinality 64 | 1,256,893.2 | 1,218,119 |
| Unicode, cardinality 64 | 1,670,449.5 | 1,218,033 |
| Wildcard, cardinality 64 | 1,021,481.0 | 1,218,088 |
| ASCII, cardinality 512 | 1,270,236.4 | 1,218,120 |
| Unicode, cardinality 512 | 1,904,161.8 | 1,217,885 |
| Wildcard, cardinality 512 | 1,084,214.1 | 1,218,112 |
| ASCII, cardinality 4,096 | 13,737,528.9 | 78,985,866 |
| Unicode, cardinality 4,096 | 851,105,166.7 | 845,597,752 |
| Wildcard, cardinality 4,096 | 13,636,238.8 | 79,116,554 |

## Input length

These cases use 1,024 rows and 64 dynamic patterns.

| Case | Median mean (ns) | Median allocated (B) |
| --- | ---: | ---: |
| ASCII, 4 code units | 90,838.2 | 324,886 |
| Unicode, 4 code units | 112,452.1 | 324,886 |
| Wildcard, 4 code units | 90,007.9 | 324,886 |
| ASCII, 64 code units | 121,495.7 | 324,886 |
| Unicode, 64 code units | 213,841.8 | 324,886 |
| Wildcard, 64 code units | 90,245.8 | 324,886 |
| ASCII, 4,096 code units | 2,314,950.5 | 324,901 |
| Unicode, 4,096 code units | 2,298,688.1 | 324,901 |
| Wildcard, 4,096 code units | 89,010.3 | 324,886 |

## Correlated APPLY

Both methods execute compiled `CROSS APPLY` SQL over 1,024 outer rows. The inner
orientation evaluates `m1.Input like m2.Pattern`; the outer orientation evaluates
`m2.Value like m1.Pattern`.

| Case | Median mean (ns) | Median allocated (B) |
| --- | ---: | ---: |
| Inner pattern, fan-out 1 | 3,434,816.4 | 20,035,736 |
| Outer pattern, fan-out 1 | 3,449,946.2 | 20,084,892 |
| Inner pattern, fan-out 8 | 7,986,339.3 | 40,728,952 |
| Outer pattern, fan-out 8 | 4,347,221.6 | 21,289,336 |
| Inner pattern, fan-out 64 | 29,840,281.8 | 50,365,744 |
| Outer pattern, fan-out 64 | 23,555,355.7 | 30,924,994 |

Every execution invokes the root source once. Baseline child-collection reads are
`outerRows * (fanOut + 1)`: 2,048, 9,216, and 66,560 reads at fan-outs 1, 8, and
64. The later hoisting wave must retain one source invocation and reduce only work
that is semantically loop invariant.

## Compilation

| Case | Median mean (ns) | Median allocated (B) |
| --- | ---: | ---: |
| Dynamic column pattern | 10,765,136.5 | 2,621,167 |
| Correlated APPLY pattern | 10,125,077.1 | 5,352,615 |

## Existing constant and fallback guardrail

| Case | 10k mean (ns) | 10k allocated (B) | 100k mean (ns) | 100k allocated (B) |
| --- | ---: | ---: | ---: | ---: |
| Exact | 67,968.2 | 234,987 | 387,698.1 | 238,875 |
| Prefix | 91,564.7 | 299,498 | 1,336,975.2 | 981,593 |
| Suffix | 94,363.1 | 299,498 | 1,309,911.7 | 981,593 |
| Contains | 124,967.6 | 299,499 | 1,540,846.2 | 981,593 |
| Multiple matches | 90,497.8 | 299,498 | 1,322,800.0 | 981,592 |
| `_` wildcard fallback | 421,393.8 | 475,102 | 3,691,259.6 | 2,639,910 |
| Interior `%` fallback | 450,935.1 | 539,608 | 4,992,312.5 | 3,382,666 |
| Non-ASCII pattern fallback | 586,248.4 | 475,006 | 5,420,908.9 | 2,639,830 |
| Dynamic pattern fallback | 394,410.6 | 894,572 | 9,537,219.8 | 6,571,077 |
| Unicode input fallback | 281,372.9 | 606,417 | 6,754,783.9 | 3,689,630 |

## Semantic and generated-code evidence

Dedicated benchmark tests pin deterministic SHA-256 result hashes at cardinality,
length, and APPLY extremes. They also assert exact row counts, one root-source
invocation, and the baseline child-read formula. Representative hashes include:

- dynamic ASCII cardinality 1: `AA3E9ABE832C6CB723EE3C2D2CE038DCF4C6A294E411623909AE33AE93E6ED99`;
- dynamic Unicode cardinality 4,096: `C79EEAFE47E7ABEC003E4A1982A09D238A8D3F6DE52AD36C26929F82D3140464`;
- dynamic wildcard cardinality 4,096: `E87AEE3ECEF437015DF526BE6B048A57D530E3EE6ABF844F304A543D97664381`;
- 4,096-code-unit wildcard probe: `C2D90865D7EB218BB0CA45A924A1A6BD1688E007DDCE8DCAD223F3BFD29C29FF`;
- APPLY fan-out 64: `0E42E91377154960BDC8D5692EF181A60CA63CABDB1AE8A3ECB639EB12756645`.

Inspection tests prove all three baseline query shapes contain
`ExecutionPatternMatch`. The operation registry maps that node to `expr.pattern`,
Execution IR remains v6, and generated C# contains
`new Musoq.Evaluator.Operators().Like(...)`. This is the baseline that R14/R15
must replace explicitly.

## Discarded launches and bug correction

Two launches are excluded:

- `qms-dynamic-like-baseline-1` failed validation because the four new benchmark
  classes were sealed. BenchmarkDotNet requires unsealed fixture types; a dedicated
  test now protects that requirement.
- `qms-dynamic-like-baseline-2` reached the 4,096-code-unit Unicode case and exposed
  a production bug: .NET rejected the non-backtracking regex automaton above its
  10,000-node limit. Separate direct-operator, wildcard, and compiled-query tests
  reproduce the defect. Musoq now retries only rejected construction with the
  timeout-bounded compiled backtracking engine, preserving the existing regex
  matching contract.

The three `baseline-valid-N` cohorts were collected only after both corrections.
No optimization threshold is evaluated in R11; `gate-dynamic-like` is installed
and unit-tested for the baseline/current comparison in QMS-R19.
