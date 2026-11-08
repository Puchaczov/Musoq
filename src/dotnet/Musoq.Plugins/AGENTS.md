# Musoq.Plugins instructions

Read the [repository instructions](../../../AGENTS.md) and the complete [module development guide](DEVELOPMENT.md) before editing this module or its tests. These links are required reading; do not assume the tool automatically loads them.

- Own built-in SQL functions, declaration attributes and typed aggregate/window kernels. Keep each LibraryBase category in its existing partial file and preserve public SQL names and signatures.
- Use the established BindableMethod and injection contracts for scalar declarations. Implement aggregate/window execution with the typed static kernel contracts described in DEVELOPMENT.md.
- Do not reintroduce the deleted object-backed plugin Group state or erased aggregate/window execution adapters. Preserve concrete argument/result types in generated hot paths.
- Keep non-determinism and injected-context behavior explicit so planning can make correct evaluation-stability decisions.
- Add behavior-based coverage in Musoq.Plugins.Tests and SQL integration coverage when binding or callable signatures change. Apply the root validation policy for broader qualification.
