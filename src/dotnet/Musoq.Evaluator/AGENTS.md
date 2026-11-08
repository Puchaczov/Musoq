# Musoq.Evaluator instructions

Read the [repository instructions](../../../AGENTS.md) and the complete [module development guide](DEVELOPMENT.md) before editing this module or its tests. These links are required reading; do not assume the tool automatically loads them.

- Own semantic analysis, AST normalization, Expression IR, logical planning, QueryPlanner, physical planning, Execution IR lowering and runtime execution. Read the [architecture rules](../../../.claude/rules/architecture.md) before changing these boundaries.
- Choose query-level strategies in QueryPlanner or planner-owned helpers and carry them explicitly through physical plans and Execution IR. C# rendering belongs to Musoq.Targets.CSharpClr and must faithfully emit those choices.
- Use shared stability, scalar-region, cost and replayability facts before moving or reusing evaluation. Do not cache source or plugin calls without an explicit capability contract.
- Preserve portable typed execution contracts and source accepted/residual partitions. Reuse existing lowering coordinators and source-planning helpers instead of adding parallel implementations.
- Keep query fixtures in Musoq.Evaluator.Tests.Common. Use the seven evaluator test projects according to their query, relational, planning, interpretation, diagnostics, code-generation and runtime ownership.
- Generated C# samples are evidence of planner and IR choices. Refresh them intentionally and investigate changed shapes before accepting snapshots. Follow the root validation policy rather than running runtime qualification for documentation-only edits.
