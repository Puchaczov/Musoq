# Changelog

All notable Musoq package releases are documented here. Release entries are grouped by NuGet package because minor and patch releases may publish a single package.

## Unreleased

## 17.0.3-alpha.2

See [release-notes/v17.0.3-alpha.2.md](release-notes/v17.0.3-alpha.2.md) for the curated GitHub Release text.

### Musoq.Evaluator

- Fixed root `DESC #schema` and `DESC FUNCTIONS #schema` metadata binding so they do not dispatch the filtered raw-constructor overload with an empty method name.
- Preserved method-specific binding for constructor, table, column, settings, query, and ordinary datasource paths.

### Tooling and verification

- Added an end-to-end raw-constructor dispatch matrix that fails on empty-name filtered lookups and covers the complete `DESC` surface.
- Release validation passed with 16,779 total tests: 16,775 passed, 4 skipped, and 0 failed. The five registered packages are validated through Release packaging and package smoke tests.

## 17.0.3-alpha.1

See [release-notes/v17.0.3-alpha.1.md](release-notes/v17.0.3-alpha.1.md) for the curated GitHub Release text.

### Musoq.Parser

- Added contextual `WITH RECURSIVE`, CTE column lists, recursive shape diagnostics, and keyed recursive `UNION` syntax.
- Added named datasource argument syntax with source-only parser metadata and stable invalid-form diagnostics.

### Musoq.Evaluator

- Added iterative breadth-first semi-naive recursive CTE execution with typed reusable frontiers, full-row/keyed identity, stable typed invariant snapshots, direct reusable indexes, cancellation, and independently configurable row, iteration, and snapshot-row limits.
- Added ordinary and recursive CTE composition, strict anchor-derived output typing, optimizer containment, and fixed-point column liveness.
- Added compile-time binding of case-insensitive named datasource arguments, reflected optional defaults, deterministic overload diagnostics, and canonical positional lowering across direct, coupled, APPLY, and DESC source surfaces. Existing positional datasource calls and public `Musoq.Schema` signatures remain compatible.
- Hardened named datasource maintenance paths: canonical vectors are reused by metadata/planning and property-source re-resolution, hidden or mismatched reflection parameters cannot become public names, unsupported function-shaped sources report `MQ2034`, and positional APPLY overloads remain compatible.

### Musoq.Converter

- Added dedicated recursive logical, physical, and execution plans plus CSharp CLR generation with context-free value-type frontier rows and cache/artifact signatures for effective recursive limits.

### Tooling and verification

- Expanded the corpus to 229 current generated-code samples and 13 profiled samples, with 68 supported recursive result contracts, 51 unsupported diagnostic contracts, computed pair coverage, Roslyn shape checks, and scoped mutation checks.
- Added six-tier performance gates over three cohorts for eight handwritten-equivalent scenarios, full-mode and overhead regression, recursive compilation, and ordinary CTE regression.
- Added CLI/server recursive limit transport and hard ceilings through run, inspect, scalar execution, watch, queues, and compiled-query caching.

## 17.0.2-alpha.3

See [release-notes/v17.0.2-alpha.3.md](release-notes/v17.0.2-alpha.3.md) for the curated GitHub Release text.

### Musoq.Parser

- Extended correlated subquery support across predicate, quantified, scalar, and `CROSS APPLY` forms, including null-aware semantics.

### Musoq.Plugins

- Added typed correlated scalar-subquery results and aggregate kernels used by indexed execution.

### Musoq.Evaluator

- Added a phase-aware correlated-subquery pipeline covering equality and bounded range correlation, composite keys, hash/single/mark/semi/anti join strategies, per-key scalar shaping, and CTE sidecar indexes.
- Added deterministic empty-result, scalar-cardinality, null-key, set-operator, window, `QUALIFY`, `PIVOT`, `UNPIVOT`, and `ASOF` composition handling.

### Musoq.Converter

- Updated generated C# execution support for correlated subquery joins, typed scalar carriers, range frames, and bundled target assemblies.

### Tooling and verification

- Added correlated-subquery generated samples, integration coverage, performance gates, and benchmark report comparison tooling.
- Unsupported correlated shapes are rejected with `MQ2024_InvalidSubquery` instead of falling back to hidden per-row execution.

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

### Runtime architecture

- Completed the runtime-v2 execution ownership split across semantic phases, portable execution IR, physical lowering, target composition, generated-code rendering, and execution-plan dispatch.
- Added immutable handoffs for execution artifacts and runtime metadata, per-run query state isolation, true asynchronous row execution, deferred-result lifetime leasing, generated assembly ownership, and bounded runtime caches.
- Preserved diagnostic exception taxonomy and explicit compatibility boundaries while removing legacy lowering-kernel and interpreter lifetime coupling.

- Added contextual-keyword collision catalogs, parser recovery coverage, named-source documentation ratchets, architecture budgets, and release guardrails for the completed runtime-v2 boundaries.
