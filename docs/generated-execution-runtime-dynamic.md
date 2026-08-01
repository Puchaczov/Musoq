# Public `DynamicObject` rows in generated execution

Generated execution accepts a public CLR type derived from `System.Dynamic.DynamicObject` as a datasource row. The concrete row type is retained through `GetRowSource<T>`, chunk storage, and generated loops; the engine does not create a per-row adapter or change the datasource contract.

## Schema is the contract

The schema columns are authoritative for runtime member names and result types. Names are matched case-insensitively during SQL binding, then emitted with the canonical spelling from the schema. A public CLR property or field wins over a runtime member with the same name and is emitted as ordinary static CLR access. Runtime members must resolve to referenceable CLR types. Existing `DynamicObjectPropertyDefaultTypeHint` and `DynamicObjectPropertyTypeHint` attributes provide the type of nested runtime values when the schema does not provide a more specific hint.

Only `DynamicObject` subclasses are admitted at this boundary. Private or otherwise non-referenceable roots, arbitrary custom `IDynamicMetaObjectProvider` implementations, and advertised non-referenceable result types remain `MQ3084` policy failures.

## Null and contract failures

A runtime member read is lowered to one DLR `GetMember` operation on the immediate receiver and is immediately cast to its schema type. The value is then handled by the normal statically typed operators, joins, and library calls. A runtime member returning `true` with `null` is SQL null; nested members are evaluated only after the normal null guard. `TryGetMember` returning `false` for an advertised member is a datasource contract error and surfaces as the runtime binder failure rather than being silently converted to null.

Runtime paths are cached once per row when they are shared by a predicate, join key, or projection. Predicate values are read before filtering, while projection-only values are read only for surviving rows. Nested paths preserve the same ordering and null-guard behavior.

The tracked samples `Q231` through `Q234` under `generated-code-samples/current` show the supported shapes and are intentionally reviewed as generated C# artifacts. They contain concrete source generics and only immediate-receiver dynamic member reads; downstream code remains statically bound.
