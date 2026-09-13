# Structured inputs remediation qualification ledger

This ledger records the audited gaps in the structured-input implementation and the wave that closes each one. The syntax is migrated to parenthesized VALUES rows. All remediation requirements are implemented and closed by the evidence below.

| Finding / requirement | Primary owner | Evidence required | Wave | Status |
|---|---|---|---:|---|
| Explicit declaration defaults survive normalization and beat receiver defaults | Evaluator | Differing defaults, explicit null, missing versus empty | 3, 6, 8 | implemented |
| Authored field expressions retain source order | Evaluator/Targets | Side-effect trace and generated typed temporaries | 4 | implemented |
| Typed sources expose deterministic overload sets | Schema/Evaluator | Multiple constructors, exact cost, ambiguity | 2, 3 | implemented |
| Scalar-column and complete-CTE interpretations report ambiguity | Planner/Evaluator | Scope collision and incompatible CTE cases | 3 | implemented |
| Retained lets, parameters, and CTEs use typed carriers | Evaluator/Targets | Carrier shape, direct field reads, no boxed hot path | 5, 6, 7, 9 | implemented |
| Inline, host, let, and CTE paths enforce global and receiver limits | Evaluator | Boundary, overflow, defaults, cancellation cases | 5, 6, 8, 9 | implemented |
| Receiving collections use the exact supported whitelist | Schema/Evaluator | Arrays, IReadOnlyList, IEnumerable; reject concrete/unstable types | 7, 10 | implemented |
| Source construction and lifecycle share one context and boundary | Schema/Targets | Constructor timing, context identity, cleanup | 4, 10 | implemented |
| Parser and VALUES use one token-based named-field composer | Parser | Trivia, identifiers, spans, recovery, old spelling rejection | existing | implemented |
| DESC ARGUMENTS reports corrected explicit-null text and all overloads | Evaluator | Eleven columns, deterministic paths, metadata-only behavior | existing | implemented |
| Example datasource exercises every accepted syntax and failure family | Examples | Runner, shared resources, counters, descriptions | 11 | implemented |
| Generated samples prove typed lowering and ownership | Converter/Targets | Q330–Q368 plus remediation corpus and manifest | 11 | implemented |
| IR/ABI and fingerprints include structural contracts | Evaluator/Converter | Version rejection, cache reuse/invalidation, unloadability | 5, 13 | implemented |
| Allocation and IL gates detect all avoidable structural work | Benchmarks | Paired typed baselines, decoded callees, zero avoidable bytes | 12 | implemented |
| Independent parser/binder/execution populations close gaps | All | Fixed-seed unique cases, shrinker, metamorphic checks | 13 | implemented |

## Canonical syntax

```sql
from values {
    (Id: 'todo'),
    (Id: 'fixme'),
} p
select p.Id
```

The former `values { { Id: 'todo' } } p` spelling is deliberately invalid. `array { ... }` contains expressions and `(Name: expression)` is the shared named-record notation; ordinary parenthesized arithmetic remains grouping.

## Wave gate

Every wave must add focused tests and realistic cases discovered from the changed code, run `git diff --check`, reread applicable guides, and finish with the full Release restore/build/test gate plus the structured-input example runner. The commit is created only from the unchanged tested tree. Record the exact revision, SDK/runtime/OS, commands, test counts, TRX paths, generated manifests, and performance artifacts in wave evidence.

## Wave 13 closure evidence

The final population gate uses fixed seeds `0x5A17_20`, `0xB17D_20`, and `0xC7E_20`. It covers 2,000 parser cases (1,600 valid and 400 invalid), 512 binding cases, and 128 separately compiled execution cases. Each population records a normalized SHA-256 query or value hash and coverage cells; malformed parser cases carry authored diagnostic expectations and write shrink candidates only on failure. Same-artifact concurrency, host-representation equivalence, mutable receiver isolation, fingerprint stability/invalidation, collectible loading, and previous IR rejection are covered by focused regressions.

The final unchanged-tree Release test gate completed with 0 build warnings and 0 errors. Test results were Schema 556, Parser 2,694, Plugins 4,743, Git 45, CSV 49, Structured Inputs 51, Benchmarks 106, Converter 1,163, and Evaluator 11,885, with 3 intentional generated-sample refresh skips: 21,292 passed, 3 skipped, 21,295 total. The structured-input runner `--all` passed all authored positive resources and native probes. The gate ran from the Windows 11 Home 26200 workstation with Intel Core Ultra 9 285K (24 logical processors), .NET SDK 10.0.303, and .NET 10.0.11. Exact TRX files are `src/dotnet/Musoq.Schema.Tests/TestResults/Jakub_DESKTOP-I52018A_2026-09-12_15_00_52_net10.0.trx`, `src/dotnet/Musoq.Parser.Tests/TestResults/Jakub_DESKTOP-I52018A_2026-09-12_15_00_52_net10.0.trx`, `src/dotnet/Musoq.Plugins.Tests/TestResults/Jakub_DESKTOP-I52018A_2026-09-12_15_00_52_net10.0.trx`, `src/dotnet/examples/data-sources/git/tests/TestResults/Jakub_DESKTOP-I52018A_2026-09-12_15_00_54_net10.0.trx`, `src/dotnet/examples/data-sources/csv/tests/TestResults/Jakub_DESKTOP-I52018A_2026-09-12_15_00_54_net10.0.trx`, `src/dotnet/examples/data-sources/structured-inputs/tests/TestResults/Jakub_DESKTOP-I52018A_2026-09-12_15_01_10_net10.0.trx`, `src/dotnet/Musoq.Benchmarks.Tests/TestResults/Jakub_DESKTOP-I52018A_2026-09-12_15_00_53_net10.0.trx`, `src/dotnet/Musoq.Converter.Tests/TestResults/Jakub_DESKTOP-I52018A_2026-09-12_15_00_52_net10.0.trx`, and `src/dotnet/Musoq.Evaluator.Tests/TestResults/Jakub_DESKTOP-I52018A_2026-09-12_15_00_55_net10.0.trx`.

The final typed `UNION ALL` CTE regression is covered by `Sql_UnionAllCteUsesOneTypedStoredRowRepresentationForStructuralArguments`; ordinary relational set-operation tests remain on their legacy `Row` representation. The independent populations use fixed seeds `0x5A17_20` (2,000 parser/recovery cases), `0xB17D_20` (512 binder cases), and `0xC7E_20` (128 separately compiled execution cases), with normalized-hash duplicate protection, coverage cells, authored diagnostics, shrinking, and metamorphic checks.

Wave 13 repeated the measured CTE, scalar-source, and VALUES cohorts under BenchmarkDotNet 0.15.8, ShortRun plus the class job, with MemoryDiagnoser. Raw reports are ignored under `BenchmarkDotNet.Artifacts/structured-input-wave13-cte`, `structured-input-wave13-scalar`, and `structured-input-wave13-values`; compressed-report SHA-256 values are `54029B658D5ECE3A35E6D7D5CEE41EF4E83B6219A07E8A5BFD26954E4AB70696`, `AAFCF1B4D2EC65E605079EA63D8E77C77700F545FB02D64EF903EFB88DA25601`, and `3CA9B01406E5BC76E246709E83F2FCF6056E9D23C2A8392AB7AFB7B0FA3B48FC`. The CTE size-32/19,999 means were 82.83 μs / 4.164 ms; scalar-source means were 79.23 μs / 79.56 μs; VALUES means were 1.010 μs / 450.2 ns. Allocation and IL gates remained green, with no newly detected boxing, reflection, delegate, iterator, dictionary, or unplanned-copy path.
