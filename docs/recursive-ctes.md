# Recursive common table expressions

Musoq supports basic PostgreSQL-shaped recursive common table expressions on the CSharp CLR target. A recursive CTE evaluates an anchor once and then repeatedly evaluates its recursive member against only the rows accepted in the previous generation.

```sql
with recursive reachable (Id, Depth) as (
    select RootId, 0
    from graph.roots()

    union (Id)

    select e.TargetId, r.Depth + 1
    from reachable r
    inner join graph.edges() e on e.SourceId = r.Id
)
select Id, Depth
from reachable
order by Id
```

This is basic PostgreSQL-style syntax, not complete PostgreSQL recursive-query compatibility. Keyed `UNION` is a Musoq extension.

`RECURSIVE` is contextual after `WITH`. It is consumed as the recursion marker only when the following tokens form a CTE definition. An ordinary CTE may therefore still be named `recursive`:

```sql
with recursive as (
    select 1 as Id from system.dual()
)
select Id from recursive
```

## CTE column lists

Ordinary and recursive CTEs may declare exported column names:

```sql
with items (Id, Name) as (
    select SourceId, SourceName
    from inventory.items()
)
select Id, Name from items
```

Names map positionally and override aliases inside the CTE. The number of names must equal the projected column count. CTE names are case-sensitive, while exported column names are case-insensitive for uniqueness and binding, so `(Id, id)` is rejected with `MQ3078`.

Recursive keyed-`UNION` columns bind exclusively to exported anchor names. They do not bind to an underlying anchor expression or a recursive-member alias. Valid keys are canonicalized to their exported spelling during semantic binding; an unknown key reports `MQ3001` before logical planning.

## Recursive shape

A recursive definition consists of one anchor, one top-level union boundary, and one recursive member with exactly one reference to its own CTE name. The anchor cannot reference the CTE. A recursive definition may depend on earlier fully evaluated ordinary or recursive CTEs, but forward, mutual, and nested recursion are rejected.

The recursive member supports:

- projections, scalar expressions, postfix casts, searched `CASE`, and filters;
- `INNER JOIN` and `CROSS JOIN`;
- `CROSS APPLY` and `OUTER APPLY`.

The recursive member does not support:

- aggregation, grouping, `HAVING`, or `DISTINCT`;
- windows, `QUALIFY`, ordering, or pagination;
- nested or chained set operations;
- multiple or nested self-references;
- outer, semi, anti, or as-of joins;
- pivot, unpivot, `SEARCH`, `CYCLE`, multiple recursive members, or mutual recursion.

These restrictions apply only inside the recursive member. The query consuming the completed CTE may join, aggregate, window, order, paginate, or use set operations normally.

## Union and identity modes

The separator determines which generated rows are accepted globally:

| Separator | Identity | Typical use |
| --- | --- | --- |
| `UNION ALL` | None | Counters or recursion terminated by a predicate |
| `UNION` | Complete output row | Cycles where all projected values define identity |
| `UNION (Id)` | Named exported columns | Graph/entity traversal with payload columns |
| `UNION (TenantId, Id)` | Composite named key | Partitioned or composite entity identity |

`UNION ALL (keys)` is not supported for recursive CTEs.

Anchor rows participate in identity tracking. Only newly accepted rows enter the next frontier, so an earlier generation always wins over a later generation. With keyed `UNION`, the first accepted representative for a key is retained. Input order within one generation is not guaranteed; if duplicate keys carry different non-key values, the surviving non-key values are unspecified unless the source order itself is guaranteed. An outer `ORDER BY` orders the completed result but cannot change which representative survived.

Use keyed identity only when non-key columns are functionally determined by the key or when any representative is acceptable.

## Types

The anchor declares the recursive relation's CLR column types. The recursive member must produce the same number of columns with compatible types. Cast the anchor explicitly when widening is required:

```sql
with recursive totals (Id, Total) as (
    select RootId, 0::Decimal
    from graph.roots()

    union (Id)

    select e.TargetId, t.Total + e.DecimalCost
    from totals t
    inner join graph.edges() e on e.SourceId = t.Id
)
select Id, Total from totals
```

## Source snapshot semantics

An uncorrelated relational subplan in the recursive member is opened and completely enumerated at most once, after a non-empty anchor has been produced. Its stable typed snapshot is reused for all generations and disposed once on success, failure, or cancellation. This includes uncorrelated sources nested under `CROSS APPLY` or `OUTER APPLY`, invariant filters/projections, multiple invariant sources, and invariant joins.

```sql
from reachable r
inner join graph.edges() e on e.SourceId = r.Id
```

Although the join predicate refers to `r`, the `graph.edges()` invocation has no recursive argument and is therefore snapshotted once. Changes to the external resource after snapshot completion are not observed by later generations. An empty anchor does not open an otherwise unused recursive source.

Snapshot carriers copy the referenced source-column values while enumerating. Mutating scalar properties on the original source row after enumeration does not affect recursion. Reference-valued column contents are shallow values and are not recursively cloned; mutating an object referenced by a copied column remains observable according to that object's own semantics.

Snapshot accounting is per recursive CTE and shared by all of its invariant lists and indexes. Every row retained in an invariant representation counts once. When a hash index is the only consumer, generated execution builds it directly and does not retain a redundant source list.

A source invocation with a recursive argument remains correlated and runs per input row:

```sql
from reachable r
cross apply graph.neighbors(r.Id) e
```

Inspection can reveal whether an invariant source was materialized and whether an eligible hash lookup was built outside the fixed-point loop.

## Execution model and order

Generated execution is iterative and breadth-first:

1. Evaluate and identity-filter the anchor (generation zero).
2. Put accepted anchor rows in the result and current frontier.
3. Snapshot recursive-row-independent inputs when the frontier is non-empty.
4. Evaluate the recursive member against the current frontier.
5. Emit accepted rows directly to the result and next frontier.
6. Swap two reusable typed frontier buffers and continue until the frontier is empty.

No CLR recursion, candidate table, per-generation collection allocation, LINQ, reflection, closure, or delegate is required in the fixed-point hot path. The loop is sequential; ordinary source work and independent sibling CTEs retain their normal parallelization behavior.

Breadth-first evaluation does not promise result ordering. Add an outer `ORDER BY` whenever deterministic presentation is required.

## Limits and cancellation

The defaults are 1,000 recursive-member iterations, 10,000,000 accepted rows, and 10,000,000 retained invariant snapshot rows. Anchor evaluation is generation zero. Rejected duplicates do not consume the row limit. Snapshot rows are counted immediately before retention, across all invariant representations owned by one recursive CTE.

Engine consumers can override the limits without changing the existing `CompilationOptions` constructor:

```csharp
var options = new CompilationOptions()
    .WithRecursiveCteLimits(
        new RecursiveCteExecutionLimits(
            maxIterations: 10_000,
            maxRows: 50_000_000,
            maxSnapshotRows: 5_000_000));
```

The existing two-argument constructor remains supported and uses the default snapshot limit.

All existing `With...` option methods preserve recursive limits. Effective limits participate in compilation-cache and artifact signatures.

The CLI exposes the same settings for `run` and `inspect`:

```powershell
musoq run query.sql `
  --recursive-max-iterations 10000 `
  --recursive-max-rows 50000000 `
  --recursive-max-snapshot-rows 5000000

musoq inspect query.sql --stage generated `
  --recursive-max-iterations 10000 `
  --recursive-max-rows 50000000 `
  --recursive-max-snapshot-rows 5000000
```

All three values must be positive. Watch execution forwards the configured limits. The local server accepts the same nested request object on `/local/execute`, `/local/execute-scalar`, and `/local/inspect`:

```json
{
  "script": "with recursive ...",
  "recursiveCteLimits": {
    "maxIterations": 10000,
    "maxRows": 50000000,
    "maxSnapshotRows": 5000000
  }
}
```

The server-configured triple is both the default and a hard ceiling. Requests may lower each property independently; a zero, negative, or above-ceiling property returns HTTP 400 rather than being clamped. The effective triple separates compiled-query cache entries and is used consistently by inspection and execution.

## Diagnostics

| Code | Meaning |
| --- | --- |
| `MQ3072` | A self-referencing CTE was used without `WITH RECURSIVE` |
| `MQ3073` | The recursive definition does not have the supported anchor/union/member shape |
| `MQ3074` | The self-reference, dependency order, or recursion graph is invalid |
| `MQ3075` | The recursive member contains an unsupported operator |
| `MQ3076` | Anchor and recursive-member output columns or types do not match |
| `MQ3077` | The CTE column-list count does not match its projection |
| `MQ3078` | A CTE column list contains a duplicate name |
| `MQ7007` | The configured recursive iteration limit was exceeded |
| `MQ7008` | The configured accepted-row limit was exceeded |
| `MQ7009` | The configured invariant snapshot-row limit was exceeded |

For a non-terminating `UNION ALL` query, add a terminating predicate or use full-row/keyed identity. Raise a host limit only when the intended traversal legitimately exceeds it.

## Compatibility

| Feature | Musoq | PostgreSQL/SQLite expectation | SQL Server expectation |
| --- | --- | --- | --- |
| `WITH RECURSIVE` keyword | Required for recursion | Familiar | SQL Server omits `RECURSIVE` |
| Ordinary CTE named `recursive` | Supported; `RECURSIVE` is contextual | Identifier rules are dialect-dependent | Familiar identifier behavior |
| Anchor plus recursive member | Supported | Familiar | Familiar |
| `UNION ALL` | Supported | Familiar | Familiar |
| Full-row `UNION` | Supported | Familiar | Recursive boundary commonly uses `UNION ALL` |
| Keyed `UNION (columns)` | Musoq extension | Not portable | Not portable |
| CTE column list | Supported | Familiar | Familiar |
| Exported column uniqueness | Case-insensitive | Dialect-dependent | Typically case-insensitive |
| `SEARCH` / `CYCLE` | Not supported | Advanced dialect feature | Not supported in this form |
| Multiple/mutual recursive members | Not supported | Dialect-dependent | Not supported by this v1 contract |

When porting a query, assign or declare exported column names, add explicit anchor casts where needed, and replace path-based cycle logic with keyed `UNION` only when its representative semantics are acceptable.
