# Parameterized paging and compiler API compatibility

`SKIP` and `TAKE` accept literals and direct `$name` references from `param(...)`, `params(...)`, and `let`. See the [language specification](../specs/musoq-core-language-spec.md#135-skip--take-pagination) for supported types, ranges, and diagnostics.

Compiler integration changes:

- Parser `SkipNode` and `TakeNode` retain their `IntegerNode` constructors and add an operand constructor for direct references. Their `Value` property remains available for literals and throws `InvalidOperationException` for runtime references. Use `Expression` when examining unresolved syntax.
- Logical, physical, and Execution IR paging counts now use `PagingCount`. `Constant` exposes a nullable integer; runtime counts carry `ParameterName` and a portable `ParameterType`. Integer constructor overloads remain available. Consumers of count properties or generated deconstruction must handle the descriptor instead of assuming an integer.
- Runtime counts lower to typed validation locals marked with `ExecutionDeclarationScope.Execution`. Stored-table helper extraction retains these declarations in the enclosing execution and captures their validated values.
- Execution IR contract version advances from 10 to 11 for the changed paging count representation and declaration scope metadata. Host ABI and target package format versions are unchanged. Existing compiled-query parameter APIs and datasource contracts are unchanged.

The test-only portable target retains literal paging support. Runtime paging additionally requires typed parameter binding and validation support from a target.
