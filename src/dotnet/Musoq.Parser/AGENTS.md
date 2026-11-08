# Musoq.Parser instructions

Read the [repository instructions](../../../AGENTS.md) and the complete [module development guide](DEVELOPMENT.md) before editing this module or its tests. These links are required reading; do not assume the tool automatically loads them.

- Own SQL lexing, parsing, AST nodes, source-preserving syntax and parser diagnostics. Keep provider binding, planning and execution outside this leaf project.
- Preserve direct character-scanning lexer behavior and generic-function token handling. Use the shared script-composition path for ordinary and source-preserving parsing.
- When adding an AST node or visitor method, update every affected visitor implementation, including evaluator visitors. Preserve original tokens, trivia, contextual roles and literal/comment contents in syntax capture.
- Keep unexpected implementation exceptions distinct from typed input diagnostics. Do not weaken parser recovery or source-location contracts.
- Use Musoq.Parser.Tests for parser coverage and evaluator integration tests when behavior changes; choose the required checks by the root validation policy.
