# feat(search): consume Core string-match v1 for pre-open path pruning

Owner: Musoq.DataSources Search

Core dependency: a Musoq Core build containing source predicate contract v1,
`SourceStringComparison.LikeIgnoreCase`, and `CandidateMetadata` application
validation.

## Scope

Implement an opt-in Search provider integration in the Musoq.DataSources
repository. This document is a handoff only: the Core campaign must not edit,
merge, release, or alter the existing Search W21 campaign.

Search should advertise `SourceStringMatchCapability` only for columns whose
value is available from cheap candidate metadata before content open or
decompression. A likely first column is the normalized candidate path; confirm
that the exact value exposed to matching is identical to the eventual row
column value.

## Required behavior

- Use source predicate contract v1 and advertise only the match kinds,
  negation support, comparison, and phases that Search implements.
- Preserve Core `LikeIgnoreCase` results for null and Unicode input. Do not use
  unconditional ordinal-ignore-case matching as a semantic substitute.
- Echo every accepted direct top-level match exactly once in
  `SourceExecutionPlan.PredicateApplications` at `CandidateMetadata`.
- Evaluate accepted path applications before content open, decompression,
  decode, parsing, or row materialization.
- Leave unsupported columns, `_`, interior/repeated `%`, non-ASCII patterns,
  dynamic patterns, nested `OR`, and unknown contract versions as Core runtime
  residuals.
- Retain independent counters for candidates inspected, content opens,
  decompressions/decodes, materialized rows, and resource disposal.
- Preserve cancellation and contextual lifecycle exceptions on content-open,
  decode, enumeration, and cleanup failures.

This scope makes no traversal-pruning claim. Candidate filtering may reduce
content I/O after enumeration without reducing directory walking, archive entry
enumeration, API pagination, or any other discovery work.

## Acceptance evidence

Add deterministic Search repository tests using real provider boundaries:

1. Compare capability-off and capability-on query results by count, order, and
   stable hash.
2. Prove zero path matches open zero content payloads.
3. Prove partial path matches open exactly the matching candidate count.
4. Cover positive and negated matches, null and Unicode paths, unsupported
   columns/shapes/versions, partial `AND`, and supported-looking `OR`.
5. Prove invalid applications fail before all content counters and that
   cancellation/failure paths dispose resources.
6. Add compiled Musoq SQL benchmarks with identical fixtures and result hashes;
   report candidate and payload counters beside time and allocation.

The Core reference behavior and terminology are documented in
`docs/source-string-match-specialization.md`. Any Search limitation discovered
during implementation should narrow the advertised capability rather than
change Core matching semantics or residual ownership.
