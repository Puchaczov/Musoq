# Musoq.Playground instructions

Read the [repository instructions](../../../AGENTS.md) and the complete [module development guide](DEVELOPMENT.md) before editing this module or its tests. These links are required reading; do not assume the tool automatically loads them.

- Use this project for manual query experiments through InstanceCreator and a deliberate schema provider. Keep reusable engine behavior in the owning production module.
- The playground has no automated test suite and is not packaged as a library. A successful manual experiment does not replace the regression checks required for a production change.
- Preserve the typed source/schema examples and keep its target framework aligned with the production projects.
- Keep experimental output under repository-root .artifacts/ and clean task-owned output after success according to the root rules. Preserve user-owned running processes.
