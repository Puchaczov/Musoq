# Musoq.Benchmarks

Follow the repository-wide guide and the measurement contract in README.md.

- Benchmark SQL, IDs and deterministic fixtures belong only to Musoq.Benchmarks.Scenarios.
- Admit workloads that represent real query workflows. Generated fixtures may model those workflows; artificial conversion round trips and isolated optimizer stress expressions stay outside the canonical catalog.
- Budget the complete Official campaign, including preflight, compilation, process launches and all standard sizes, for a ten-hour target and twelve-hour overnight ceiling on the declared machine/runtime profile. Record a proposed workload's runtime cost and retain headroom before admitting it.
- Qualify the overnight budget with a complete Official campaign. Local timings or case removal alone do not certify it. Preserve measurement profiles and sizes; the budget does not authorize truncating or marking an incomplete campaign complete.
- Musoq.Benchmarks.Tests independently asserts complete small resultsets.
- Neither shared scenarios nor benchmarks may reference existing engine test projects.
- Compile and generate data outside execution measurement. Consume every result row and dispose owned resources.
- Keep framework behavior in ordinary C# and retain raw BenchmarkDotNet reports.
- Correctness tests must never assert elapsed times. Dry validates execution, not performance.
- Do not overlap measurements with builds or tests.
- Keep all three projects non-packable. Do not change production versions.

- Official campaigns use clean source, the complete catalog and fixed out-of-process parallel measurement.
- Local runs allow filters, dirty source and Short/Dry jobs. Neither starts the full solution test suite.
- All SQL and query generation stay in the shared project; expected resultsets stay independently authored in tests.
- Keep the wrapper thin and use ordinary C#. Do not introduce a query DSL or aggregate scoring.
- First compilation requires a fresh process without preceding compilation. Warmed process does not imply cache hit;
  define these paths through their setup boundaries without compiler probes.
- Preserve branch/commit and CPU/core/RAM/runtime provenance. Separate machine/environment series.
- Persist per-case attempts and raw JSON; failures remain visible. Resume requires identical source revision,
  engine version, suite, exact cases and environment/settings. Successful cases and complete campaigns are immutable.
- Official history is committable. Working binaries/logs remain ignored. Never automatically commit or push results.

- Execution sizes are uniformly 1,000/10,000/100,000, including expensive and recursive workloads.
  Local custom counts replace that matrix; lifecycle fixtures stay fixed. Never silently cap inputs.
- Registrations and job settings have one authority. Reject duplicates instead of deduplicating them.
- Schema-v3 definitions and finalized attempts are authoritative. Derived views must be recoverable;
  completion markers make finished bundles immutable. Preserve interrupted evidence and attempt IDs.
- Status/Verify/Latest are read-only. Historical verification must not rely on the current checkout or hardware.
- Keep the repository running skill thin; structured receipts own compatibility and Official campaign qualification.

- Artifact paths must fit the 180-character repository-relative and 240-character absolute budgets, including temporary writes.
- Keep artifacts free of fingerprints and checksums. Verify checks structure and readable evidence; Latest selects by engine version only.

## Verifying performance changes

Pin the clean baseline SHA before editing. Record the intended case IDs, matching SDK/runtime and machine,
and what size of change would matter for each workload. Run the initial selection against that revision.
Use an isolated checkout of the pinned revision for additional cases identified after regeneration.

```powershell
$baselineRef = (git rev-parse HEAD)
$caseId = 'B01_Scan-execution-Table-Small'
$review = '.artifacts/benchmark-review'
./scripts/benchmarks/Run-Benchmarks.ps1 -Action List -Filter $caseId -Json
./scripts/benchmarks/Run-Benchmarks.ps1 -Mode Local -Job Short -Filter $caseId -Output "$review/base" -Json
```

After editing, regenerate the full collection. The command writes a preview under `.artifacts/` and
`-Apply` updates the tracked collection with a backup of the replaced files. Review the Git diff and
record changed sections. `SnapshotDiff` compares the regenerated working tree to the specified Git commit,
including new files. Reports with `baselineHasSnapshots: false` predate the first tracked collection.
For a bootstrap comparison, select the cases manually.

```powershell
./scripts/benchmarks/Update-BenchmarkSnapshots.ps1 -Apply
./scripts/benchmarks/Run-Benchmarks.ps1 -Action SnapshotDiff -BaselineRef $baselineRef -Json |
    Set-Content -Encoding utf8 "$review/changes.json"
```

Use each affected case's exact filter and its recorded size in both revisions. Confirm selections with
`List`. Compare matched out-of-process Local Short or Default runs with the same hardware, runtime,
SDK, GC, and job settings. Run sequentially and keep each repeat in a separate output directory.
Manually include relevant cases for shared runtime edits whose generated snapshots stay identical.
Review reported fixture and harness changes before interpreting measurements because code can change
fixture behavior without updating its exported description.

### Working-tree candidates

Capture the complete source patch immediately before and after candidate measurements. Use a temporary
Git index under the review directory so that newly created files are included and your working index
stays available for review. Run these commands from the repository root.

```powershell
function Save-CandidatePatch([string] $Patch) {
    $patchPath = [IO.Path]::GetFullPath($Patch)
    $index = Join-Path (Split-Path -Parent $patchPath) ([Guid]::NewGuid().ToString('N') + '.index')
    $previousIndex = $env:GIT_INDEX_FILE
    try {
        $env:GIT_INDEX_FILE = $index
        git read-tree HEAD
        if ($LASTEXITCODE -ne 0) { throw 'Could not populate the temporary Git index.' }
        git -c core.safecrlf=false add --all -- .
        if ($LASTEXITCODE -ne 0) { throw 'Could not capture the candidate files.' }
        git diff --cached --binary --full-index --no-renames --no-ext-diff "--output=$patchPath" HEAD --
        if ($LASTEXITCODE -ne 0) { throw 'Could not save the candidate patch.' }
    }
    finally {
        $env:GIT_INDEX_FILE = $previousIndex
        [IO.File]::Delete($index)
    }
}

Save-CandidatePatch "$review/candidate.patch"
./scripts/benchmarks/Run-Benchmarks.ps1 -Action SnapshotDiff -BaselineRef $baselineRef `
    -CandidatePatch "$review/candidate.patch" -Json | Set-Content -Encoding utf8 "$review/changes.json"
./scripts/benchmarks/Run-Benchmarks.ps1 -Mode Local -Job Short -Filter $caseId -Output "$review/candidate" -Json
Save-CandidatePatch "$review/candidate-after.patch"
if ((Get-FileHash "$review/candidate.patch").Hash -ne (Get-FileHash "$review/candidate-after.patch").Hash) {
    throw 'Candidate source changed during measurement. Preserve the attempt and measure again.'
}
```

`SnapshotDiff` checks the patch against the candidate and records its digest. The runner records the base
commit and dirty flag. The before/after patch check establishes the working-tree identity for the saved
runs. Save the patch and treat any mismatch as unavailable evidence.

### Compare saved runs

Create the comparison input next to the retained evidence. Paths in the input are relative to the input
file or absolute. List repeats in their acquisition order. Use `additionalCaseIds` for explicit cases
such as a shared runtime helper. For a clean candidate use its snapshot diff and omit `candidatePatch`.

```powershell
@{
    snapshotDiff = 'changes.json'
    runs = @(@{ baseline = 'base'; candidate = 'candidate' })
    additionalCaseIds = @($caseId)
    candidatePatch = 'candidate.patch'
} | ConvertTo-Json -Depth 4 | Set-Content -Encoding utf8 "$review/comparison.json"
./scripts/benchmarks/Run-Benchmarks.ps1 -Action Compare -ComparisonInput "$review/comparison.json" -Json |
    Set-Content -Encoding utf8 "$review/report.json"
```

The report verifies the saved evidence and retains every paired timing and allocation delta alongside
available confidence bounds and sample counts. Positive deltas mean slower execution or more bytes.
Cold compilation's allocation metric is unavailable by design. Missing, added, retired, or incompatible
cases carry a reason. A reported interval relationship is evidence for review.

Repeat suspected changes in a fresh pair with order reversed. Confirm a material timing effect from
consistent paired measurements with uncertainty considered. Check recurring allocation increases
separately. Record an outcome for each affected case in the implementation report or PR.

- No material regression detected.
- Potential regression requiring additional measurement.
- Confirmed regression requiring investigation or correction.
- Regression explicitly accepted with a documented justification.
- Comparison unavailable or invalid, with the reason and disposition recorded.

Keep required comparisons open until resolved. Use a per-case decision so that improvements elsewhere
cannot mask a regression. Retain the run directories, selection, report and any candidate patch under
`.artifacts/` for reproducibility.
