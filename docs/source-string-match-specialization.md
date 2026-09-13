# Source string-match specialization provider contract

This guide is the normative provider contract for Core source string-match v1.
It applies to providers that advertise `SourcePredicateCapabilities` and accept
`SourcePredicateStringMatch` instances during source planning. Providers that do
not advertise the capability continue to receive the existing residual-filter
behavior.

## Ownership and compatibility

Core owns SQL `LIKE` classification, capability negotiation, accepted/residual
partition validation, and runtime residual execution. A provider owns only the
application it explicitly accepts. `ISchema` signatures are unchanged; support
is opt-in through `DescribeSource`, `TryPlanSource`, and the existing
`SourceExecutionPlan` passed to `GetRowSource`.

The current predicate contract version is `1`. A non-positive version, malformed
flags or phases, a null declaration, or duplicate column declarations under
case-insensitive identity are contract errors. A positive future version is not
an error, but Core does not offer typed predicates to it: Core emits a warning
and retains the runtime residual.

Execution IR `expr.string-match` is a separate Core/target contract at Execution
IR version 7. Providers do not create or interpret Execution IR nodes.

## Matching semantics

`SourceStringComparison.LikeIgnoreCase` means result equivalence with Musoq's
legacy SQL `LIKE` matcher, not unconditional
`StringComparison.OrdinalIgnoreCase`. In particular:

- a null input produces false before `NOT LIKE` negates the result;
- matching is case-insensitive under the execution culture captured when the
  matcher is prepared;
- regex metacharacters in SQL pattern literals remain literal characters;
- `_` is one-character wildcard and `%` is zero-or-more wildcard in the generic
  matcher;
- Unicode input must preserve the legacy regex `IgnoreCase` result, including
  compatibility cases such as Kelvin sign `K` versus ASCII `K`;
- a provider must use `OriginalPattern` when it needs the legacy matcher.

Core offers only constant ASCII patterns classified as one of `Exact`,
`Prefix`, `Suffix`, or `Contains`. `Needle` is the wildcard-free classified
value. Patterns containing `_`, interior or repeated `%`, non-ASCII pattern
characters, or runtime-dependent values remain generic residuals. Providers
must not reinterpret a rejected pattern as a typed match.

An implementation may use ordinal operations after proving the relevant input
comparison span is ASCII. It must use a result-equivalent legacy matcher when
the relevant span contains non-ASCII input. The optimization must never change
query results.

## Negotiation and residual ownership

Core flattens only top-level `AND` conjuncts. Each direct
`SourcePredicateStringMatch` is offered independently when its column, kind,
comparison, negation, and at least one evaluation phase are advertised. A typed
match nested in `OR` or any other expression is not offered; the complete
containing conjunct remains a runtime residual.

For each accepted direct match, the provider must return exactly one
`SourcePredicateApplication` with the exact predicate value and an advertised
phase. Multiplicity matters: two equal top-level conjuncts require two
applications. Missing, duplicate, altered, nested, future-version, or
unadvertised applications fail validation before source execution.

Core merges deferred predicates with provider residuals and validates that the
accepted and residual multisets form an exact partition of the original
predicate. A provider must not broaden, narrow, synthesize, or silently discard
a conjunct.

## Evaluation phases

`CandidateMetadata` means the provider evaluates the accepted match against
cheap metadata before payload open, decompression, decode, deserialization, or
row materialization. It does not mean filesystem traversal pruning. If a
provider cannot meet every pre-materialization obligation for a column, it must
advertise `RowFiltering` instead or decline the match.

`RowFiltering` means the provider evaluates the match only after a complete row
has been materialized. It can reduce rows crossing the source boundary, but it
cannot claim reduced payload I/O.

For either phase, cancellation must remain cooperative, enumerators and payload
resources must be disposed on completion, early termination, cancellation, and
failure, and source failures must cross the existing contextual lifecycle
boundary. Provider counters should distinguish candidate inspection, payload
open, decode, and row materialization so phase behavior can be verified.

## Minimal provider example

The following outline advertises prefix matching for a metadata-backed `Path`
column and accepts the offered direct match at the candidate phase:

```csharp
public override SourceDescriptor DescribeSource(
    string name,
    SourceDescribeContext context,
    params object?[] parameters)
{
    return base.DescribeSource(name, context, parameters) with
    {
        PredicateCapabilities = new SourcePredicateCapabilities
        {
            StringMatches =
            [
                new SourceStringMatchCapability(
                    new SourceColumnRef("Path"),
                    SourceStringMatchOperations.Prefix,
                    SourceStringComparison.LikeIgnoreCase,
                    supportsNegation: true,
                    SourcePredicateEvaluationPhases.CandidateMetadata)
            ]
        }
    };
}

public override SourcePlanResult TryPlanSource(
    string name,
    SourcePlanRequest request,
    params object?[] parameters)
{
    if (request.Predicate is not SourcePredicateStringMatch match)
        return SourcePlanResult.RejectAll(request);

    return new SourcePlanResult
    {
        AcceptedColumns = request.RequiredColumns,
        AcceptedPredicate = match,
        ExecutionPlan = new SourceExecutionPlan
        {
            Identity = request.Identity,
            AcceptedColumns = request.RequiredColumns,
            AcceptedPredicate = match,
            PredicateApplications =
            [
                new SourcePredicateApplication(
                    match,
                    SourcePredicateEvaluationPhase.CandidateMetadata)
            ]
        }
    };
}
```

The row source must then read only `Path`, apply the exact application with
`LikeIgnoreCase` semantics, and open the payload only for matching candidates.
The provider should retain a runtime row-filtering implementation for any match
it advertises at `RowFiltering`; unsupported requests must remain residual.

## Qualification checklist

Before enabling `CandidateMetadata` for a column, verify:

1. Optimized and rejected plans return identical row count, order, and stable
   result hash for positive, negated, null, and Unicode inputs.
2. Zero matches cause zero payload opens, and partial matches open exactly the
   matching candidate count.
3. Unsupported columns, shapes, versions, and supported-looking `OR`
   expressions remain runtime residuals.
4. Invalid applications fail before any payload counter changes.
5. Cancellation, matching-payload failures, early termination, and normal
   completion dispose resources and preserve contextual diagnostics.
6. Documentation and performance reports say whether the optimization reduces
   candidate payload work, row transfer, or traversal. These are distinct
   claims.

The Core executable reference is
`CandidateMetadataPruningTests`; the query-level performance workload is
`LikeCandidatePayloadBenchmark`.
