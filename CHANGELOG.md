# Changelog

All notable Musoq package releases are documented here. Release entries are grouped by NuGet package because minor and patch releases may publish a single package.

## Unreleased

## 17.0.2-alpha.2

See [release-notes/v17.0.2-alpha.2.md](release-notes/v17.0.2-alpha.2.md) for the curated GitHub Release text.

### Release Infrastructure

- Added datasource ABI package compatibility validation for `Musoq.Plugins` and `Musoq.Schema`.
- Added release-script tests and packaging guardrails that prevent test-only projects from being packed.
- Hardened full-train package validation and consumer smoke coverage for the release artifact set.

## 17.0.2-alpha.1

See [release-notes/v17.0.2-alpha.1.md](release-notes/v17.0.2-alpha.1.md) for the curated GitHub Release text.

### Musoq.Evaluator

- Added a portable execution core with stable type, callable, constant, operation, semantics, and host ABI contracts.
- Added deterministic target capability validation, portable subset conformance coverage, and target-aware execution semantics/cache fingerprints.
- Intentionally changed the public-looking Execution IR surface to use portable descriptors instead of CLR reflection and arbitrary object payloads.

### Musoq.Converter

- Added the internal execution-target composition pipeline while preserving the existing CSharpClr public compilation and execution APIs.
- Bundled the internal target implementation assemblies in `Musoq.Converter` instead of publishing them as separate NuGet packages.
- Added release package validation that verifies the bundled assemblies, symbols, dependency graph, and clean consumer restore path.

## 17.0.1-alpha.2

See [release-notes/evaluator/v17.0.1-alpha.2.md](release-notes/evaluator/v17.0.1-alpha.2.md) for the curated GitHub Release text.

### Musoq.Evaluator

- Optimized runtime-v2 chunked parallel aggregate rendering with query-specific chunk workers, local shard loops, and fused aggregate result emission.
- Added weather-measurement generated-code and benchmark coverage for runtime-v2 single aggregate fast paths.
- Tracked the generated-code sample corpus so code generator changes produce reviewable Git diffs.
- Tightened generated-code snapshot validation so missing tracked samples fail instead of being skipped.

## 17.0.0-alpha.4

See [release-notes/v17.0.0-alpha.4.md](release-notes/v17.0.0-alpha.4.md) for the curated GitHub Release text.

### Musoq.Parser

- Added canonical runtime-v2 script parameter type cataloging and `MQ7006_UnknownScriptParameter`.

### Musoq.Schema

- Added public runtime-v2 contract signature constants for artifact and cache compatibility.

### Musoq.Evaluator

- Added strict unknown runtime parameter validation while preserving missing/null/type-mismatch precedence.
- Added canonical `ScriptParameterContract` metadata for declared type names, canonical engine type names, CLR types, nullability, collection elements, and defaults.
- Fixed CTE sidecar join-chain lowering for final projection filters over aliases introduced by sidecar probes.

### Musoq.Converter

- Exposed canonical parameter contracts through compiled query, typed/profile query, generated runnable, and typed artifact APIs.
- Included the runtime-v2 contract signature and CTE sidecar option in artifact validation, semantic hashes, and execution compilation cache keys.

## 17.0.0-alpha.3

See [release-notes/v17.0.0-alpha.3.md](release-notes/v17.0.0-alpha.3.md) for the curated GitHub Release text.

### Musoq.Evaluator

- `CompiledQuery` now supports disposal so artifact-loaded queries can release loader-owned lifetimes.

### Musoq.Converter

- Updated runtime-v2 compiled artifacts to format version `2` with planning-shape validation for fast artifact loads.
- Added strict generated-code hash validation as an opt-in artifact load mode.
- Added a lifecycle-aware artifact loader result API and collectible default artifact byte loader.
- Strengthened persisted artifact engine signatures with informational versions and module IDs.

## 17.0.0-alpha.2

See [release-notes/v17.0.0-alpha.2.md](release-notes/v17.0.0-alpha.2.md) for the curated GitHub Release text.

### Musoq.Parser

- Added `MQ8002_CompiledArtifactIncompatible` diagnostics for runtime-v2 compiled artifact validation and loading failures.

### Musoq.Converter

- Added the runtime-v2 compiled artifact API for host-managed cross-process compiled query persistence.
- Added artifact loading from bytes with an optional per-call custom runnable type loader for host-owned `AssemblyLoadContext` strategies.
- Artifact loading now revalidates current script, schema, compilation options, and generated code shape before creating a fresh executable query.

## 17.0.0-alpha.1

See [release-notes/v17.0.0-alpha.1.md](release-notes/v17.0.0-alpha.1.md) for the curated GitHub Release text.

### Release Infrastructure

- Added secure tag-driven release planning for full-train and package-specific NuGet publishing.
- First alpha release train using tag-driven NuGet publishing.

### Musoq.Parser

- Added Runtime V2 grammar coverage for strict postfix casts, script parameters, script variables, SELECT alias visibility, star modifiers, derived tables, inline `VALUES`, richer subqueries, set-operation keys, full/semi/anti/cross joins, APPLY ordinality, aggregate `FILTER`, `PIVOT`, `UNPIVOT`, window frames, `NULLS FIRST` / `NULLS LAST`, and `QUALIFY`.
- Updated interpretation schema calls to generic syntax such as `Interpret<Header>(bytes)` and `Parse<LogEntry>(text)`.
- Added grammar coverage for TABLE column read modifiers and COUPLE source runtime settings profiles.

### Musoq.Plugins

- Added source-contract and source-runtime-settings specification coverage for datasource authors.
- Documented datasource read modifiers for TABLE columns, including `encoding`, `culture`, `format`, `trim`, and `source <key> '<value>'`.
- Documented source contract diagnostics reported from `DescribeSource` and `TryPlanSource`, including warning `MQ5013` and blocking error `MQ3071`.

### Musoq.Schema

- Added TABLE/COUPLE metadata contract updates for source runtime settings profiles, read modifiers, and source contract diagnostics.
- Added per-column read-modifier metadata semantics and duplicate-modifier validation rules.

### Musoq.Evaluator

- Added Runtime V2 query behavior coverage for script values, projection shaping, grouping, joins, APPLY, VALUES sources, subqueries, set operations, aggregate filtering, pivoting, unpivoting, windows, `QUALIFY`, null handling, row-presence predicates, and metadata description statements.
- Added binary/text interpretation schema behavior for value validations, `repeat until eof`, switch payloads, and bounded substreams.

### Musoq.Converter

- Added full-train Runtime V2 conversion surface notes covering the new parser/evaluator query features, TABLE/COUPLE source contracts, and interpretation schema syntax.
