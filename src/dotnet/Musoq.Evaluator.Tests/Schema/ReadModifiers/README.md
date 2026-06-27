# Read Modifier Evaluator Datasource

This test-only datasource is the canonical maturity proof for TABLE column read modifiers.

The Git example datasource is intentionally not extended for this feature: Git rows do not naturally exercise per-column encoding, culture, format, trim, or source-codec behavior. `#readmods.records()` is small, production-shaped, and uses the same public schema APIs plugin authors use: `GetTableByName`, `DescribeSource`, `TryPlanSource`, and `GetRowSource`.

Keep new read-modifier behavior covered here first. Production datasources can then adopt the same patterns without inheriting artificial Git-specific test cases.
