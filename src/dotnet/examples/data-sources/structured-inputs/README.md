# Structured inputs example datasource

This non-packable .NET 10 datasource is the maintained Core example for named
records, arrays, typed source construction, direct CTE arguments, and DESC
metadata. The source name is `#inputs`.

The source contracts are:

- `match(text, patterns: PatternInput[])`, where `PatternInput` has `Id`,
  `Pattern`, and optional `Mode` (`literal` or `regex`).
- `configure(options: OptionsInput)`, where options contains `Enabled`,
  `Codes`, and a nested `Window` record.
- `numbers(values: int[])`, preserving order and duplicates.
- `matrix(values: int[][])`, flattening nested values with row and column
  coordinates.
- `weighted(items: WeightedInput[])`, where `Enabled` defaults to `true`.
- `overloaded(42)` and `overloaded(patterns: array { ... })`, which expose
  numeric and structural constructor overloads with deterministic selection.
- `defaultconflict(input: ...)`, which demonstrates a declared `Mode` default
  overriding the `PatternInput` receiver default.
- `contextprobe()`, which returns the query and source-context identity received
  by the typed constructor.
- `strict(values: int[])`, registered with deliberately small structural limits.
- `mutable(items: MutableInput[])`, which mutates only its invocation-owned
  input object; `throwing(value: int)` is a constructor-failure fixture and
  `ambiguous(input: ...)` is an equal structural-applicability fixture.
- `empty()`, inherited from `SchemaBase` for metadata-only and empty-source
  examples.

The final VALUES spelling is deliberately used in the query resources:

```sql
select p.Id
from values {
    (Id: 'todo'),
    (Id: 'fixme'),
} p
```

Structured source examples use `array { ... }` and parenthesized records:

```sql
select m.PatternId, m.MatchText
from #inputs.match(
    'TODO FIXME ISSUE-42',
    patterns: array {
        (Id: 'todo', Pattern: 'TODO'),
        (Pattern: 'FIXME', Id: 'fixme'),
        (Id: 'issue', Pattern: 'ISSUE-[0-9]+', Mode: 'regex'),
    }
) m
```

The same `--all` run compiles and executes every SQL resource listed in
`queries/manifest.json`. The manifest is shared by the automated resource
tests, so each example has an authored row count, schema width, and first-row
semantic check. It covers inline records and arrays, nested values, inferred
and annotated `let`, `param`/`params`, host dictionaries, CTE arguments,
correlation, coupled sources, and every `DESC` family.

Run all native and SQL examples with:

```text
dotnet run --project runner/Musoq.Examples.DataSources.StructuredInputs.Runner.csproj -c Release -- --all
```

The positive catalog is paired with `queries/invalid/`, which keeps representative old, mixed, positional, and missing-keyword spellings as diagnostic regression resources.

The VALUES migration is breaking. Parenthesized rows are the only supported
spelling:

```sql
values { (Id: 'todo') } p
```

The former `values { { Id: 'todo' } } p` spelling is intentionally rejected.
