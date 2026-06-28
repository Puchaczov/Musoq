# Changelog

All notable Musoq package releases are documented here. Release entries are grouped by NuGet package because minor and patch releases may publish a single package.

## Unreleased

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
