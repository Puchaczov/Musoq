# Musoq.Benchmarks instructions

Read the [repository instructions](../../../AGENTS.md) and the complete [module development guide](DEVELOPMENT.md) before editing this module or its tests. These links are required reading; do not assume the tool automatically loads them.

- This guide also covers work on Musoq.Benchmarks.Scenarios and Musoq.Benchmarks.Tests. Read the [measurement contract](README.md) and DEVELOPMENT.md before changing campaigns or benchmark definitions.
- Keep SQL, stable IDs and deterministic data in Musoq.Benchmarks.Scenarios; independently authored expected resultsets belong in Musoq.Benchmarks.Tests. Do not depend on engine test projects.
- Admit representative query workflows with a measured runtime cost. The complete Official suite targets ten hours and must qualify within a twelve-hour overnight ceiling on its declared machine/runtime profile; follow DEVELOPMENT.md and README.md.
- Keep compilation and data generation outside execution measurements, consume every row and dispose owned resources. Do not overlap measurements with builds or tests.
- Official campaigns use clean source and the complete fixed catalog. Local measurements allow narrow filters and Short/Dry jobs; Dry checks execution rather than performance.
- Preserve raw attempts, provenance, immutable completed campaigns and the existing resume contract. Never automatically commit or publish results, silently cap inputs, or assert timing in correctness tests.
- Use the repository runner and keep transient output under .artifacts/. Preserve existing retained evidence and apply the root cleanup policy to task-owned temporary files.
