# Correlated subqueries

Musoq supports correlated predicate, scalar, quantified, and `CROSS APPLY` subqueries by decorrelating bounded shapes into set-based joins. Supported predicate and scalar shapes do not execute the inner query once per outer row.

## Supported forms

| SQL form | Correlation shape | Physical strategy |
| --- | --- | --- |
| `IN`, `EXISTS` in a filter | Equality keys | Hash semi join |
| `NOT IN`, `NOT EXISTS` in a filter | Equality keys | Hash anti-semi join |
| `IN` or `EXISTS` used as a value, including inside `CASE`, join conditions, or `QUALIFY` | Equality keys | Hash mark join |
| `ANY`/`SOME`/`ALL` | Bounded equality or range comparison, optionally with equality correlation inside the subquery | Semi, anti-semi, or mark join according to quantifier and expression context |
| Predicate filter or value | Zero or more equality keys plus one `<`, `<=`, `>`, or `>=` key | Partitioned range semi, anti-semi, or mark join |
| Scalar value or aggregate | Equality keys | Hash-single join; aggregate work is grouped by the correlation key |
| Scalar `ORDER BY` with `SKIP` or `TAKE` | Equality keys | Per-key partitioned top/offset followed by hash-single |
| Scalar `DISTINCT`, `GROUP BY`/`HAVING`, window/`QUALIFY`, or supported set operators | Equality keys | Per-key shaping followed by hash-single |
| Non-aggregate scalar value | Zero or more equality keys plus one range key | Partitioned range single join |
| Correlated derived table under `CROSS APPLY` | Decorrelatable keys projected by every required branch | Set-based inner join |

Scalar subqueries must return one column. Zero rows produce `NULL`; more than one row for an outer key raises a scalar-cardinality error. Correlation key comparisons preserve SQL null behavior: a null equality or range key does not match another null key.

`UNION`, `EXCEPT`, and `INTERSECT` are supported for correlated scalar subqueries when every branch exposes the same equality correlation key. Correlated derived-table set branches must also expose the correlation columns needed by the outer join.

## Bounded correlation rules

Outer references must be in the subquery `WHERE` clause and must be expressible as an `AND`-conjunction of supported comparisons. The indexed range path accepts exactly one comparable range predicate, optionally partitioned by equality predicates. Local-only predicates stay on the inner source and are evaluated before index construction.

The following shapes are intentionally rejected with `MQ2024_InvalidSubquery` instead of falling back to hidden per-row execution:

- outer references outside the subquery `WHERE` clause;
- `OR`, `!=`, arbitrary residual correlation, or more than one range correlation predicate;
- range-correlated scalar aggregates;
- scalar `SKIP` or `TAKE` combined with `DISTINCT`, `GROUP BY`, window functions, or `QUALIFY`;
- correlated scalar set-operation branches that do not use the same equality key;
- a CTE definition that consumes an alias from outside its own definition;
- a correlated derived table used where explicit `CROSS APPLY` semantics are required, or one that hides a required correlation column;
- a `HAVING` subquery that correlates to a non-grouped outer row value.

These restrictions keep execution costs predictable. Musoq does not silently select nested-loop or per-row `APPLY` execution for an unsupported predicate or scalar correlation.

## Performance model

Equality correlation builds a hash index once and performs one lookup per outer row. Range correlation builds sorted inner ranges once, partitioned by any equality keys, and uses a binary-search probe per outer row. Composite equality and range-partition keys use generated `ValueTuple` keys for up to seven key parts; wider or non-renderable keys use the compatibility object-key path.

Generated code avoids row wrappers and object-array composite keys on the typed hot paths. Nullable composite range partitions are represented by nullable tuples so SQL null keys are rejected without allocating a sentinel object.

Use `CompileForInspection` when embedding Musoq to verify the selected path. Its planning text reports strategies such as `PredicateSemiJoin`, `PredicateHashMark`, `PredicateRangeMark`, `ScalarHashSingle`, and `ScalarRangeSingle`; the physical plan should contain the corresponding hash or sort-merge join rather than a nested-loop join.

The benchmark procedure and regression threshold are documented in [`src/dotnet/Musoq.Benchmarks/README.md`](../src/dotnet/Musoq.Benchmarks/README.md).
