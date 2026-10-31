# Musoq.Benchmarks

The suite measures 32 feature families and ten composed pipelines. Results are individual elapsed times
and allocations, with separate execution, first-compilation, warmed-compilation, artifact-load and typed-output
cases. There is no overall score. All projects are non-packable and independent of existing engine test projects.

## Projects

- **Musoq.Benchmarks.Scenarios** owns all SQL, IDs, specification references, sizes and deterministic fixtures.
- **Musoq.Benchmarks** contains ordinary BenchmarkDotNet classes and the campaign infrastructure.
- **Musoq.Benchmarks.Tests** independently checks complete small resultsets using those exact shared queries.

## Run

Use PowerShell 7 on Windows or Linux with the repository's pinned/compatible .NET 10 SDK:

```powershell
./scripts/benchmarks/Run-Benchmarks.ps1 -Action List -Json
./scripts/benchmarks/Run-Benchmarks.ps1 -Mode Local -Filter '*Join*' -Size 1_000 -Job Short -Json
./scripts/benchmarks/Run-Benchmarks.ps1 -Mode Local -Filter '*Join*' -Size 1_000 -Job Dry -InProcess -Json
./scripts/benchmarks/Run-Benchmarks.ps1 -Mode Official -MachineProfile reference-desktop
./scripts/benchmarks/Run-Benchmarks.ps1 -Mode Official -Resume <run-directory>
dotnet test src/dotnet/Musoq.Benchmarks.Tests -c Release --nologo --verbosity quiet
```

Official mode builds the benchmark tests/dependencies, runs the small tests before timing. It uses a clean checkout, all cases, one parallel engine profile,
the effective processor limit, two process launches and adaptive throughput settings. First compilation uses
eight fresh processes with one compilation each. There is no overall timeout or serial matrix.

Local mode builds only benchmarks/dependencies, allows dirty source, and defaults to Short. Dry is an
execution check; `-InProcess` accelerates local smoke checks only. Neither mode starts the full solution tests.
Filters are case-insensitive globs over case IDs or benchmark class names. `-Output` chooses a new empty
directory. Resumes restore the saved mode, filter, size, machine profile and job. Conflicting overrides are rejected. Commands never commit, push or change branches.
Do not overlap measurements with builds, tests or other heavy work.

## Measurement boundaries

- Execution prepares data and compilation in setup, then consumes every row/column through typed visitors
  and disposes table results. Typed counterparts enumerate their typed results.
- Structured sources forward the records materialized by SQL argument binding; they generate no synthetic
  fixture data during execution. Source construction and argument conversion remain part of query execution.
- First compilation prepares inputs without compiling and measures one compile-and-dispose operation per
  fresh process. Its allocation metric stays unavailable because a memory rerun would observe warmed state.
- Warmed compilation compiles once in setup and then measures compile-and-dispose. A warmed process does
  not guarantee cache hits: interpretation schemas may emit again. No compilation probes run before timing.
- Artifact load compiles an in-memory artifact in setup, then loads/disposes an executable without Run.

Correctness tests use four-row fixtures (plus focused empty/eight-row edge fixtures) and independent expected columns/types/nullability and complete rows.
Ordered results are compared in order; other results use multiset equality including duplicates. There are
no full-size correctness tests, performance thresholds or automatic expectation updates.

## Artifacts and graphs

Official runs write `benchmarks/history/<suite>/<run-id>/`. Each bundle contains `definition.json`,
`manifest.json`, `measurements.json`, `summary.md`, `complete.json`, `attempts/`, `raw/` and `sessions/`.
Raw evidence lives under `raw/<attempt-id>/`; the attempt records identify its scenario and case.
Machine profiles and engine versions are recorded in metadata rather than repeated directory components.
Working binaries/logs stay in ignored `BenchmarkDotNet.Artifacts/`. Review and commit Official bundles manually.
Git attributes preserve history bundle bytes across commits and cross-platform checkouts.

Before collection starts, the runner rejects repository-relative artifact paths longer than 180 characters
and absolute paths longer than 240 characters, including atomic-write temporary suffixes. Custom output
directories obey the same absolute limit. Choose a shorter checkout or output location when rejected.

Plot each case and input size using mean nanoseconds (divide by 1,000,000 for milliseconds), optionally
with its confidence interval. Allocations belong on a separate graph. Lower time is better; JSON null
means unavailable. Point metadata includes engine version, commit, branch, timestamp, CPU/cores/RAM,
runtime and measurement settings. Each measured process records its source revision, exact SQL, input
count, runtime, effective processors and actual server-GC state during setup, outside timing.
Dry and in-process smoke timings must not be presented as performance baselines.

Latest chooses one complete, validated Official campaign per engine version. Machine, runtime and suite
metadata remain visible, but do not partition selection. Changes in those values can affect comparisons;
there is no hardware normalization, fingerprint grouping or aggregate score.

## Sizes and expensive cases

Every execution scenario, including typed output, uses Small **1,000**, Medium **10,000** and Large
**100,000**. There are no smaller caps for joins, interpretation or recursion. `InputUnit` distinguishes
rows, traversal depth and binary chunks. Counts describe fixture inputs, not necessarily output rows;
the non-equality join compares each main row against a fixed three-row lookup and emits at most two
matches per main input row. The main input still uses the full selected size; the dimension has no hidden cap.
Large recursive chunk decoding can take substantial time.
Recursive scenarios declare their termination steps in the shared catalog. Their iteration limit is the
actual input count plus those steps: linear traversal uses N; chunk decoding uses N + 2 for its malformed
tail and empty terminating frontier. Empty fixtures allow at least one iteration. Effective recursion limits
are recorded in each case's arguments; row and snapshot limits retain the engine defaults.
The structured source allows at least `1 + 3 * N` nodes: one collection root and one record plus two
scalar fields per input row. Its node budget keeps the engine default as a minimum, and depth/string
budgets retain their defaults. Effective structural limits are included in case arguments.
The complete Official campaign has no overall duration limit and is not guaranteed to finish overnight.

Local `-Size 1_000` replaces all selected execution sizes with one exact count. Underscores are accepted.
Use `-Filter` for a subset or omit it for all workloads. Compilation and artifact-load cases retain the
small contract fixture; custom execution size does not multiply them. Use List to inspect exact cases/counts.

## Suite identity and expensive workloads

The current suite is `engine-v3`. Its non-equality workload uses N main rows and a fixed three-row lookup
with `r.Key < d.Key`, bounding both candidate comparisons and output per main row. Packet analytics
widens binary values to `long` before SUM; its nullable total has type `long?`. The optional fixture payload
value offset defaults to zero and exists for deterministic small overflow regression tests, without adding
a benchmark dimension.

Recursive chunk decoding deliberately retains repeated remainder copying. A prior native Local Dry probe
at 100,000 chunks took about 88 seconds for one measured execution and allocated about 136.7 GiB
cumulatively. Those single-sample observations are not Official baselines or peak-memory measurements.
Repeated Official measurements can take substantially longer.

Artifact schema is v3. The suite label describes workloads; Latest selection uses the engine version.
Recorded SQL and parameters let consumers review which work each point represents.

## Recovery and durable artifacts

Schema v3 stores immutable `definition.json` (including the saved invocation), `sessions/`, unique
`attempts/<id>/start.json` and `result.json`, and a final `complete.json` completion marker. The marker
records schema version, run ID, completion UTC and expected/completed case counts. Once it exists,
collection refuses to resume or rewrite the campaign. Manifest, measurements and Markdown summary
are derived views. Each case includes its full SQL script, external execution options, actual input count
and workload unit. SQL includes all CTE/schema declarations and script parameter defaults.
No external query parameter overrides are currently exposed.

BenchmarkDotNet's successful report and statistics supply metrics. Raw native JSON is retained;
verification checks readable report structure without comparing statistics copied into measurements.

Attempt starts are flushed before measurement. Raw JSON and process metadata are persisted before a
successful result is finalized. Aggregate JSON uses invariant, ordered serialization, flushed writes and
atomic replacement. The completion marker is written last. A storage device can lose its last write during
a power cut: uncertain cases rerun rather than count as successful. Temporary files are ignored on recovery.

Resume reconstructs structurally valid completed work from the definition and finalized attempts. It
retains orphaned or damaged evidence, reports it, and reruns unfinished/failed/damaged cases with fresh
attempt IDs. There is no mid-measurement resume. Damaged authoritative definitions require a new campaign.
Preflight and session history are retained. Resume directly compares source commit, engine revision,
hardware, runtime/SDK, GC/workers, exact cases/SQL/arguments and job settings. Conflicting saved-invocation
options are rejected. Source metadata is checked before and after each case; the parent validates the
measured process's source, SQL, count and runtime. Generated builds are cleaned up normally.

There are no file checksums, binary hashes or suite/query/fixture fingerprints. Verify detects missing
files, malformed JSON and inconsistent structures, not plausible edits to metrics or evidence. A completion
marker prevents accidental runner writes; it does not authenticate the files. Historical inspection does
not require the original checkout, binaries or hardware.

Configure the machine to stay awake before running a campaign. The runner neither inhibits nor detects
suspension. Discard a run that survives suspension because its timings may be affected. After a power failure
or process termination, resume unfinished cases from their beginning in the same environment. This version
assumes one writer and sequential measurement; do not launch overlapping campaigns.

## Inspect, verify and select results

```powershell
./scripts/benchmarks/Run-Benchmarks.ps1 -Action Status -RunDirectory '<directory>' -Json
./scripts/benchmarks/Run-Benchmarks.ps1 -Resume '<directory>' -Json
./scripts/benchmarks/Run-Benchmarks.ps1 -Action Verify -RunDirectory '<directory>' -Json
./scripts/benchmarks/Run-Benchmarks.ps1 -Action Latest -HistoryRoot benchmarks/history -Json
```

Status reads checkpoint/session metadata and reports the active process/attempt, last progress, counts,
resume eligibility and saved resume command. It does not read raw reports and always returns
`verified: false` and `websiteEligible: false`. Resume checks compatibility and checkpoint evidence.
Verify checks completion-marker consistency, finalized attempts, finite metrics, referenced JSON,
measured-process metadata and Official preflight. Inspection commands never build, run tests or rewrite
bundles. They require an existing Release runner binary and print its build prerequisite when missing.
Only schema-v3 bundles are supported; unsupported formats receive an error and remain untouched.

JSON receipts are emitted on stdout; measurement diagnostics go to stderr with `-Json`. Retain the startup
`Results:` directory when launching long-running work. A tool/session timeout does not mean the benchmark stopped:
use Status rather than launching another campaign. Exit codes are 0 success, 1 unexpected/build failure,
2 incomplete (including running Status), 3 incompatible resume, 4 invalid artifacts and 5 invalid invocation/prerequisite.

Keep every campaign. Latest selects the latest **validated complete Official campaign by completion UTC**
for each engine version, across all machines, runtimes and suite labels. Equal completion times use
ascending ordinal run ID. Selection is whole-campaign, never per-case. Incomplete or invalid runs do not
displace valid results. Branch, full commit and hardware remain visible even when versions repeat.
Only validated complete Official campaigns are website-eligible; Local/Dry campaigns remain ineligible.
Latest never writes a mutable pointer. Website consumers use the engine version, selected run directory,
case identity and `measurements.json`.

The repository skill is `.agents/skills/musoq-benchmarks/SKILL.md`; it delegates state and verification to
these commands. It never commits or pushes collected results.

## Add a workload

1. Add SQL, description, input unit, specification reference and fixtures in the shared scenario project.
2. Add independently authored small expected columns/rows in the test project. The normal catalog automatically
   participates in mirrored tests and table benchmarks. Register specialized benchmark types in `BenchmarkRegistrations`.
3. Run its small test and a filtered Dry check, then proper measurements when needed.

Inventory tests check unique IDs, complete expectations and benchmark registrations. Preserve historical
bundles and use a new suite label for intentionally incompatible methodology changes. Exact sizes,
distributions and consumption contracts
are exported in case definitions. AI inference is outside this suite.

## Current engine boundaries

X09 uses the supported stream-frontier recursive shape and disables primitive-only validation for its
byte-array frontier. Other scenarios retain primitive validation. Reconciliation uses a nullable fixture
column and explicit presence classification; coalescing non-nullable decimal properties introduced by
a FULL JOIN currently produces a generated-code compilation error. These cases measure supported paths
without modifying engine behavior. Structured-source queries use distinct aliases across CTE scopes because
reusing an alias with different source row types can produce an incompatible generated typed-source call.
Benchmark classes remain unsealed for BenchmarkDotNet generated hosts.
