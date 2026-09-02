# Stability-aware loop-invariant code motion

Musoq can hoist a stable scalar that is evaluated repeatedly by a descendant
serial loop into an eager local owned by the earliest loop that supplies its
row dependencies. For example, a projection over nested `CROSS APPLY` loops:

```sql
select a.Value, b.Value, c.Value
from a cross apply b cross apply c
```

is rendered as the equivalent of:

```csharp
foreach (var a in aValues)
{
    var aValue = a.Value;
    foreach (var b in bValues)
    {
        var bValue = b.Value;
        foreach (var c in cValues)
            result.Add(aValue, bValue, c.Value);
    }
}
```

The generated hot path has no lazy-initialization flag, cache lookup, or
stability branch. The renderer is deliberately passive: it emits the
`ExecutionLet` already present in Execution IR.

Musoq applies the same contract in two independent Execution IR passes:

* loop-invariant code motion (LICM) moves a scalar from a descendant serial
  loop to the earliest loop that owns its row dependencies; and
* stability-aware scalar reuse shares a stable scalar across compatible
  evaluation regions such as projections, predicates, aggregate arguments,
  window inputs, hash/keyset/range keys, and CTE or recursive payloads.

Both passes create ordinary lexical locals. They do not install a runtime
cache, an initialization sentinel, or a branch that checks stability. Ordinary
expression CSE remains a separate switch and may still be disabled while
either stability-aware pass is enabled.

## Provider contract

`ISchemaColumn.Stability` describes whether a column can be read once for the
lifetime of its bound row. Existing implementations remain source-compatible
because the interface default is `ColumnStability.Stable`; `SchemaColumn`
constructors without a stability argument also remain stable. Providers must
choose `Volatile` for any getter whose value or observable behavior can depend
on access count, timing, exception timing, mutable external state, or side
effects. Stability is a semantic contract, not a performance hint.

Entity properties are stable by default. Add `[NonDeterministic]` to a property
that is not repeatable (for example, a random or clock-backed getter); reflected
entity metadata then publishes a volatile schema column. The same attribute on
a bindable function makes that callable volatile. Functions that are unmarked
are assumed stable only when they have no injected query statistics, source row,
or runtime type context. Dynamic/reflection-based member reads and unknown calls
are conservative and volatile.

Manually authored schemas should set the metadata explicitly:

```csharp
new SchemaColumn(
    "Value",
    0,
    typeof(decimal),
    ColumnStability.Volatile);
```

If dynamic schema reconstruction sees conflicting stability declarations for
the same logical column, it merges them to `Volatile`.

The contract applies to every datasource that publishes columns, including
columns reconstructed from nullable, dynamic, logical, or physical metadata.
Do not infer stability from a fast getter: a getter that observes access count,
time, mutable state, exception timing, or side effects is volatile even when
its common-case value looks constant.

## Evaluation semantics

Stable means repeatable and side-effect-free for the lifetime of the bound row.
Hoisting therefore makes evaluation eager: a stable value is read when its
owner loop iteration begins, even if a descendant source is empty and produces
no result rows. This can expose an exception earlier, so providers must not
claim stability when access timing is observable. Volatile producers stay at
their original evaluation boundary.

Materialization is an explicit boundary. A volatile producer is evaluated once
per produced row; the materialized field is then stable for downstream consumers.
An optimization must not remove that boundary, duplicate the producer, or move
it across a conditional, source, aggregate, window, helper, or recursive
boundary.

Stable children inside a volatile parent may still be hoisted independently when
doing so does not move the volatile producer. Conversely, a scalar composition
is stable only when its callable and every input are stable. Literals,
parameters, variables, and stable fields are stable; aggregate/window state,
row streams, injected values, unknown calls, and dynamic reads are not.

An explicit materialization is stronger than an optimizer opportunity: it
evaluates a volatile producer once for each produced row and stores that value
for downstream consumers. Reuse may happen after that boundary, but a rewrite
must never duplicate the producer, move it before the boundary, or silently
freeze it in a query-row transfer. When transfer cannot preserve volatility,
the planner retains the declared-row fallback and reports the deterministic
reason in the optimizer trace.

## Compilation and boundaries

The two stability-aware passes are controlled independently from ordinary CSE
and from each other:

```csharp
var options = new CompilationOptions()
    .WithLoopInvariantCodeMotion(false)       // opt out of loop LICM
    .WithStabilityAwareScalarReuse(false);   // opt out of cross-region reuse
```

Both switches are enabled by default after qualification. Each setting
participates in the compilation and artifact fingerprint, so plans compiled
with different semantics cannot share an artifact. LICM covers serial
`ExecutionForEach`, ordinality, and indexed loops. Cross-region reuse covers
the supported scalar fields of append/projection, aggregate, window, hash,
keyset, range-probe, and recursive-CTE nodes.

Neither pass moves source creation or enumeration, row streams, aggregate or
window operations themselves, explicit materialization, helper or recursive
boundaries, conditional-only `CASE`/short-circuit/`COALESCE` arms, ASOF index
setup/probes, or specialized parallel loops. The passes do not hoist a literal
or variable-only expression merely to create a local; local names are
deterministic and collision-safe. A stable value may be evaluated before an
empty descendant source, so this eager timing is part of the provider contract.

The optimizer trace records both `LoopInvariantCodeMotion` and
`StabilityAwareScalarReuse` entries with inserted-local counts, placement
scopes, and deterministic skip reasons. Query inspection surfaces this text in
`OptimizerTraceText`. A no-change entry for a volatile or unsupported shape is
expected; it is not permission to assume that the value was cached.

## Qualification

The semantic suite uses two outer rows, a three-row fan-out, and a four-row leaf
fan-out. Stable getters and callables have exact count oracles of 2, 6, and 24
with LICM enabled; the disabled baseline evaluates each selected getter 24
times. Volatile getters remain at 24, and two volatile references remain 48
calls rather than being CSE-collapsed. Empty `CROSS APPLY`, matched and
unmatched `OUTER APPLY`, ordinality, filters, grouping, aggregate arguments,
CTEs, transfer fallback, and null/order parity are covered. Generated-code
shape is the authoritative branch check: accepted output contains direct
locals, no initialization flag, no cache lookup, and no runtime stability
branch.

The scalar-reuse qualification uses eight outer rows, fan-out labels 1, 8, and
64 (the label-one workload is raised to an effective eight rows to avoid timer
noise), stable cheap/expensive/aggregate cases, and a volatile filter. Three
complete in-process BenchmarkDotNet cohorts were collected on Windows 11
25H2, .NET 10.0.11, BenchmarkDotNet 0.15.8, and an Intel Core Ultra 9 285K.
The machine-readable gate passed with the following median ratios:

* stable expensive at fan-out 64: `0.7403x` (required `<= 0.97x`);
* all other stable cases: `0.8011x`–`0.9865x` (required `<= 1.03x`);
* volatile cases: `0.9954x`, `0.9969x`, and `1.0068x` (required `<= 1.03x`);
* allocation ratios: `0.99999x`–`1.00002x`, with no enabled allocation growth.

Raw BenchmarkDotNet artifacts remain ignored. The checked-in benchmark
baseline and gate source record the exact commands, counter oracles, machine,
and thresholds.

## Provider and source-author responsibilities

Providers publish stability, not a best-effort hint. A column is stable only
when reading it once for a bound row is indistinguishable from reading it at
every original use: value, side effects, access count, exception timing, and
observable timing must all be preserved. Mark clock-, random-, cursor-,
mutation-, network-, and access-count-backed properties `Volatile` with
`[NonDeterministic]` or explicit `SchemaColumn` metadata. Unmarked functions
are treated as stable only when they are pure for the bound row and do not
consume injected statistics, runtime type information, or mutable settings.

`RowStreamReplayability` is a separate contract. A stable column does not make
an enumeration replayable. `Unknown` replayability blocks reuse that would
require a second enumeration; `Replayable` describes a source that can be
enumerated again, and `Materialized` describes a stored row set. Providers
must not infer replayability from a cheap or repeatable column getter.

Source computed projections use the portable intrinsic subset only. Providers
advertise capabilities, return an accepted/residual partition, and expose the
accepted value as materialized once per produced source row. A malformed
acceptance, missing accepted runtime field, or incompatible shape is a source
contract error with a deterministic diagnostic; it is never silently
recomputed in the engine.

## Explicitly unsupported behavior

The contract intentionally does not provide arbitrary function pushdown,
global memoization, inferred stream replayability, generic branchless SQL
evaluation, or renderer-owned optimization strategy. Strategy decisions belong
to planning and Execution IR; the C# renderer only emits existing locals,
carriers, and boundaries. Conditional-only regions, source creation and
enumeration, streams, aggregate/window state, materialization, helper and
recursive boundaries, and specialized parallel loops remain excluded unless a
future contract makes their evaluation region explicit.
