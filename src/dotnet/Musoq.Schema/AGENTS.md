# Musoq.Schema instructions

Read the [repository instructions](../../../AGENTS.md) and the complete [module development guide](DEVELOPMENT.md) before editing this module or its tests. These links are required reading; do not assume the tool automatically loads them.

- Own provider-facing schema, table, column, typed row-source, source-planning and datasource diagnostic contracts. Treat public signature changes as provider compatibility changes.
- Use native typed rows and constrained source arguments. Keep authoritative entity/column types in metadata; do not reintroduce erased object-valued execution fallbacks or reflective row construction.
- Keep source execution plans immutable and preserve exact accepted/residual partitions. Stability and row-stream replayability are independent capabilities; unknown replayability must block unsafe reuse.
- Advertise predicate support explicitly and validate acceptance before removing Core filters. Read the [pattern-pushdown contract](DEVELOPMENT.md#source-pattern-predicate-pushdown) before changing LIKE/RLIKE negotiation.
- Use public datasource diagnostics deliberately and preserve the separation between source input locations and SQL source positions.
- Run affected Musoq.Schema.Tests and evaluator integration checks when contracts change; select the full validation gate by the root impact policy.
