# Source pattern predicate pushdown

Core can offer constant `LIKE` and `RLIKE` predicates to a datasource through
`ISchema.TryPlanSource`. Support is opt-in through
`SourceDescriptor.PredicateCapabilities`. This document describes predicate
capability contract version 2.

## Supported expressions

`LIKE` uses the existing exact, prefix, suffix, and contains classification.
Its comparison remains `LikeIgnoreCase` and its existing evaluation phases
remain available.

`RLIKE` accepts any non-null constant string pattern applied directly to a
stable source column. A string literal already produced by folding qualifies
at the predicate conversion boundary. `NOT RLIKE` additionally requires
negation support. The original pattern, including anchors and inline options,
is carried unchanged in `SourcePredicateRegexMatch`.

Regex transfer is limited to filters directly over source scans. It does not
make regex generally safe to move across joins or conditional evaluation
boundaries. Column-valued patterns, computed inputs, null patterns, volatile
columns, and unsupported evaluation contexts remain Core filters. Only direct
top-level AND conjuncts are negotiated; a regex nested under OR remains
entirely residual.

## Advertising and accepting support

Declare `SourceRegexMatchCapability` for each supported column in
`SourcePredicateCapabilities.RegexMatches`. Set `supportsNegation: true` only
when the source implements the negated behavior. A column may advertise both
LIKE and RLIKE capabilities. Duplicate declarations within either collection
are invalid. Collections are copied and frozen when assigned.

Regex applications support `RowFiltering` only. `CandidateMetadata` is not
supported for regex in this contract version.

Core removes unsupported typed predicates before calling the provider. A
provider may reject any offered pattern, or accept only some AND conjuncts.
Return an exact accepted/residual partition, preserving duplicate occurrence
counts. Accepted predicates must also appear in the execution plan, with one
`SourcePredicateApplication` for each accepted pattern occurrence. Use
`SourcePredicateApplication.ForRowFiltering` to construct those application
records; `SourcePlanResult.AcceptAll` uses it automatically.

Core validates capabilities, phases, the accepted/residual partition, and the
exact application records before execution. Missing, duplicate, altered, or
unadvertised applications are source contract failures. Core removes its own
filter only for validated acceptance without a corresponding residual.

## Required RLIKE semantics

`SourceRegexSemantics.MusoqRLike` means compatibility with the existing Core
matcher on the executing runtime:

- .NET regex syntax and inline options; matching is case-sensitive unless
  changed by the pattern, and searches within the input unless anchored.
- Culture-sensitive matching uses the execution-time current culture. Do not
  capture compilation-time culture or substitute invariant matching.
- Preserve Core's adaptive engine behavior: non-backtracking where supported,
  with bounded compiled backtracking fallback and existing literal fast paths.
- Null input produces false before negation. Consequently, `NOT RLIKE` includes
  null input. Empty patterns match every non-null string.
- Honor `SourcePredicateRegexMatch.MatchTimeout`. Core obtains this finite
  budget from its existing timeout policy; providers must not substitute an
  infinite or independently chosen budget.

A capability is a promise of these semantics, not a declaration that another
regex dialect is approximately equivalent. Return unsupported patterns as
residuals when the source cannot preserve the contract.

A .NET provider that already references Evaluator can reuse
`Operators.PrepareRLike(pattern, matchTimeout)` and
`Operators.RLikePrepared(input, matcher)`, applying the predicate's negation
after matching. Prepare once per execution rather than once per row. No new
provider dependency is required by the schema DTO contract itself.

Invalid constant patterns continue to fail binding with `MQ3094`. Core's typed
regex timeout exceptions retain `MQ7017` through datasource lifecycle wrapping;
the lifecycle exception, source identity, and original cause remain available
in the exception chain. Arbitrary provider failures retain their ordinary
datasource diagnostic. Raw exceptions from another regex implementation do not
automatically acquire Core's typed timeout identity.

Planning inspection reports the regex column, escaped pattern, semantics,
timeout, negation, accepted phase, and fallback reason. Residual expressions
continue through the existing Core RLIKE execution path.

## Alpha API migration

Version 2 changes `SourcePredicateApplication.Predicate` and its constructor
parameter from `SourcePredicateStringMatch` to the common
`SourcePredicatePatternMatch` base. Existing LIKE predicates now derive from
that base; their constructor and match-specific fields remain unchanged.
`SourcePredicateCapabilities.Supports` also accepts the shared base type.

Rebuild consumers against the updated Schema package and pattern-match the
application predicate before reading operator-specific properties such as
LIKE's `Kind` or regex's `Pattern`. Update capability declarations to version
2; declarations using the default obtain the current version on rebuild.
Explicit version-1 or otherwise unknown declarations leave typed predicates
residual. This version negotiation is not binary compatibility for plugins
compiled against the old public member signatures.

No production datasource gains regex support automatically. Core qualification
uses a test datasource and establishes negotiation, validation, filtering,
fallback, and diagnostics; each external provider needs separate conformance
testing when it implements this capability.
