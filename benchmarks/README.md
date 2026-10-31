# Recorded engine benchmarks

Official campaign bundles are written to `history/<suite>/<machine>/<engine-version>/<run-id>/`.
Review and commit the generated bundle after running the campaign. The runner never commits or pushes.

Read `manifest.json` for provenance, complete case definitions and campaign status. Read
`measurements.json` for the latest attempt per case; `attempts/` preserves all attempts.
`raw/` retains BenchmarkDotNet JSON, small-test preflight output and compilation-cache probes.

For graphs, use mean nanoseconds (divide by 1,000,000 for milliseconds), optionally with the
confidence interval. Lower elapsed time is better. Allocations use a separate bytes-per-operation series.
Unavailable metrics are JSON null. Dry runs validate execution and are not performance measurements.

A comparison series is the same suite fingerprint, case ID, input parameters, fixture fingerprint,
result API, phase, job settings, machine profile and environment. Engine release, branch, commit and
measurement timestamp identify points on that series. Engine/benchmark binary hashes identify the exact
measured builds; they are provenance, not a reason to split a version-comparison series.

Use separate series after changing hardware or runtime. Rerun historical versions on the new machine
when an overlapping history is useful. There is no hardware normalization or aggregate score.

Only complete Official runs that passed preflight belong in published release comparisons.
Preserve failed/incomplete bundles when useful for diagnosis. Generated binaries and working logs
remain in ignored `BenchmarkDotNet.Artifacts/`. Completed campaign bundles cannot be resumed or overwritten.
