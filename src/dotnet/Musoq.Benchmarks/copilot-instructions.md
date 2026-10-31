# Musoq.Benchmarks

Follow the repository-wide guide and the measurement contract in README.md.

- Benchmark SQL, IDs and deterministic fixtures belong only to Musoq.Benchmarks.Scenarios.
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
- Keep the repository running skill thin; structured receipts own compatibility and website eligibility.

- Artifact paths must fit the 180-character repository-relative and 240-character absolute budgets, including temporary writes.
- Keep artifacts free of fingerprints and checksums. Verify checks structure and readable evidence; Latest selects by engine version only.
