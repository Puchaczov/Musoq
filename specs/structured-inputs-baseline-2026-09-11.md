# Structured-input remediation baseline

Captured 2026-09-11 from tree `39ea88e61416c6e969cfc9aeae171d800532a1cc` before remediation changes. The repository was clean at capture. The current NuGet audit rejects SourceLink 10.0.300 because Microsoft.Build.Tasks.Git 10.0.300 is affected by GHSA-23fw-v26w-5fgq; the baseline tree was first made buildable by the independent central-package update to SourceLink 10.0.303.

Environment:

- Windows 11 Home, build `10.0.26200`, win-x64
- Intel Core Ultra 9 285K, 24 logical processors
- .NET SDK selected by `global.json` 10.0.300 with `latestFeature`; installed SDK/runtime used: 10.0.303 / host 10.0.11
- MSBuild 18.6.14

Commands:

```text
dotnet build src/dotnet/Musoq.sln -c Release --nologo --verbosity quiet
dotnet test src/dotnet/Musoq.sln -c Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal" --logger "trx"
dotnet run --project src/dotnet/examples/data-sources/structured-inputs/runner/Musoq.Examples.DataSources.StructuredInputs.Runner.csproj -c Release --no-build -- --all
```

Release build: succeeded with 0 warnings and 0 errors after the SourceLink patch.

Release test totals: 21,218 tests, 21,215 passed, 0 failed, 3 intentional refresh skips. Project totals were Schema 546, Parser 2,692, Plugins 4,743, Git examples 45, CSV examples 49, structured-input examples 36, Benchmarks 104, Converter 1,160, and Evaluator 11,843 (11,840 passed plus 3 refresh skips).

TRX artifacts are emitted under each `*.Tests/TestResults/` directory. The captured console log is retained in the ignored `artifacts/structured-baseline-test.txt` file. The structured-input runner completed all 29 resources, including 6 native checks and 23 SQL/description checks; every resource reported the expected column and row counts.

This is a characterization baseline only. It does not make any of the audited behavior an oracle: declaration-default precedence, authored expression order, overload identity, typed retained storage, CTE representation, limit scopes, lifecycle boundaries, and performance gates are requalified by the remediation waves.