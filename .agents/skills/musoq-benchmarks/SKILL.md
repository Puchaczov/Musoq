---
name: musoq-benchmarks
description: Run, monitor, resume and verify Musoq engine benchmark campaigns, including overnight official collection and filtered local measurements. Use the repository runner and its receipts; benchmark implementation work is outside this skill.
---

# Musoq benchmarks

The skill lives inside the Musoq repository. Resolve its repository root three directories above this
skill directory, even when invoked through a global skill link. Run commands from that root.
Read `src/dotnet/Musoq.Benchmarks/README.md` when diagnosing compatibility or artifact problems.

Use `scripts/benchmarks/Run-Benchmarks.ps1` with PowerShell 7. Always request `-Json`; the runner owns
case selection, job profiles, checkpoints, provenance validation and website eligibility.
Do not reproduce those decisions in ad hoc scripts or infer success from console timings.

## Choose the requested operation

| Intent | Command arguments |
|---|---|
| Official overnight collection | `-Mode Official -MachineProfile <user-profile> -Json` |
| Fast local execution check | `-Mode Local -Job Dry -InProcess -Size 1_000 -Json` |
| Local performance measurement | `-Mode Local -Job Short -Size 1_000 -Json` |
| Inspect selected cases | `-Action List -Filter '<glob>' -Size 1_000 -Json` |
| Monitor | `-Action Status -RunDirectory '<directory>' -Json` |
| Resume | `-Resume '<directory>' -Json` |
| Verify completed results | `-Action Verify -RunDirectory '<directory>' -Json` |
| Find website results | `-Action Latest -Json` |

Honor user-supplied filters, sizes and jobs. Omit `-Size` to use the standard size matrix.
Local `-Size` affects execution only; compilation and artifact-load fixtures remain fixed.
An unspecified fast-check size may use the example above. Ask for the stable machine-profile name
when starting Official collection without one; do not invent a profile or reuse one for changed hardware.
Official runs always include the full matrix and may take longer than one night, especially recursive
and quadratic workloads. Do not add an overall timeout or shorten their measurement profiles.
The machine must be configured to stay awake; the runner does not manage or detect suspension.
Discard a campaign that survives suspension. Resume after termination or power loss reruns unfinished cases.

## Long-running work and interruption

Retain the process/tool session identifier and the `Results:` directory printed at startup.
Redirect verbose diagnostics to an ignored log if the tool would otherwise truncate them.
A tool timeout is not proof the campaign stopped. Inspect the saved directory before any new launch.

- `active: true`: monitor the existing process. Do not start or resume a second campaign.
- `resumeEligible: true`: use the receipt's `resumeCommand`; saved settings restore automatically.
- Incompatible resume: report its reason. Restore the recorded environment only when that work is
  authorized; otherwise retain the bundle and explain that a new campaign is required.
- Invalid artifacts: report the affected paths; do not edit evidence, mark cases successful or delete
  attempts. An incomplete campaign can retain diagnostic artifacts and still resume valid progress.
- Complete: run Verify and report the receipt and result directory.

Management actions do not build or run tests. If the runner binary is absent, perform the exact build
prerequisite reported by the entrypoint, then retry the management action once.
Do not overlap measurements with builds, tests or other campaigns.

## Interpret receipts

Exit codes: `0` completed operation, `1` unexpected runner/build failure, `2` incomplete campaign,
`3` incompatible resume, `4` invalid artifacts, `5` invalid invocation or missing build prerequisite.
Status can return `2` while a healthy campaign is still running. It reads checkpoint/session metadata
without checking raw reports and always returns `verified: false`; use Verify for structural validation.

Use `verified`, `websiteEligible` and `reasons` from Verify. Complete Local/Dry results are never official graph points.
Latest selects one entire validated Official campaign per engine version across all machines and runtimes.
Report the selected directory, version, commit and machine profile; do not merge cases or choose by directory modification time.
Only schema-v3 bundles are supported. Unsupported formats remain untouched.
Verify detects missing/malformed evidence and inconsistent metadata. No checksums or fingerprints are stored;
plausible edits to metrics cannot be detected automatically. Path-budget errors require a shorter output or checkout location.

The completion report should state mode, completed/expected counts, commit, machine profile (from the
manifest), run directory and website eligibility. Distinguish Dry execution checks from performance evidence.
Never change SQL, expected results, source, manifests or job settings to make a campaign pass. Never
skip Official preflight, automatically commit/push bundles, publish tags or release software.
