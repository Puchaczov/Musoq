# Musoq.Converter instructions

Read the [repository instructions](../../../AGENTS.md) and the complete [module development guide](DEVELOPMENT.md) before editing this module or its tests. These links are required reading; do not assume the tool automatically loads them.

- Own compilation orchestration and the public InstanceCreator entry point. Keep language semantics and query strategy in their evaluator-owned layers.
- Preserve the build-chain sequence: parsing, interpretation-schema compilation, AST/plan transformations, rendering and finalization. Inspection stops before executable artifact emission.
- Keep execution-target composition internal. The production public API uses CSharpClr; do not introduce a public target selector or treat test-only portable targets as production targets.
- Preserve the phase-based target contracts, portable type/callable/constant descriptors, optional inspection and activation phases, and target-aware artifact loading described in DEVELOPMENT.md.
- Use Musoq.Converter.Compilation.Tests, Musoq.Converter.Inspection.Tests, Musoq.Converter.Targets.Tests, and the relevant architecture guardrails for affected orchestration contracts. Shared fixtures belong in the non-test Musoq.Converter.Tests.Common library. Apply the root validation policy to choose broader checks.
