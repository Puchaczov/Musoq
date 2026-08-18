# CSV Datasource Example

The CSV example exposes `#csv.file` for typed CSV reads through TABLE/COUPLE syntax. The TABLE declaration is the source contract: column names, types, and read modifiers tell the datasource how to map and convert fields.

```sql
table People {
  Name: string trim,
  Amount: decimal culture 'pl-PL',
  EventDate: datetime source name 'When' format 'dd.MM.yyyy'
};

couple #csv.file with table People as Rows;

select r.Name, r.Amount
from Rows('people.csv', true) r
where r.Amount > 10
order by r.EventDate desc;
```

## Source Overloads

`#csv.file` supports these forms:

```sql
Rows()
Rows(path)
Rows(path, hasHeader)
Rows(path, hasHeader, skipRows)
Rows(path, hasHeader, skipRows, delimiter)
```

- `path`: CSV file path.
- `hasHeader`: `true` maps columns by header name; `false` maps by ordinal.
- `skipRows`: records to skip before reading the optional header.
- `delimiter`: a single non-quote, non-newline character. The default is `,`.

`Rows()` is kept for contract/scaffold tests and returns no rows.

## Modifiers

Supported TABLE column modifiers:

- `trim`: trims the raw field before conversion.
- `culture 'name'`: uses a .NET culture for numeric/date conversion.
- `format 'pattern'`: uses exact parsing for `datetime`, `datetimeoffset`, and `timespan`.
- `encoding 'name'`: file-wide text encoding. All columns must agree.
- `source index 'n'`: maps a table column to zero-based CSV field index.
- `source name 'Header'`: maps a table column to a CSV header name.

Unsupported modifiers produce source contract diagnostics. Invalid `source index`, missing `source name` headers, conflicting encodings, invalid delimiters, malformed quoted records, and obvious static file-shape mismatches are reported when the query supplies enough static information.

## Planning

The datasource accepts projection, source-local predicates, order, skip, and take when it can preserve query semantics.

Accepted predicates include comparisons, `and`, `or`, `in`/`not in`, and null checks over CSV columns and literals. For `and`, supported conjuncts can be accepted while unsupported conjuncts remain residual. For `or`, the whole predicate is accepted only when both sides are supported.

Order is accepted when all order keys are CSV columns. Skip/take are accepted only when no residual predicate or residual order can change row cardinality or ordering before slicing.

At execution time the datasource parses and converts only the execution columns it needs for projection plus accepted predicate/order work, then applies the accepted `SourceExecutionPlan` before emitting fixed-size chunks.

## Query-scoped rows

The example advertises query-scoped CLR rows by default. With exact `TABLE`/`COUPLE` metadata,
the engine can emit a private typed carrier and the datasource materializes accepted records
through a concrete CSV ref-struct reader. Direct header mode exposes header names as nullable
strings; headerless mode exposes `Column0`, `Column1`, and so on. The legacy `CsvRow` source is
preserved and can be selected explicitly for comparison or compatibility:

```csharp
var optimized = new CsvSchemaProvider();
var legacy = new CsvSchemaProvider(enableQueryScopedRows: false);
```

Both paths use the same source plan, conversion rules, chunking, diagnostics, cancellation, and
error behavior. Accepted predicates, ordering, skip, and take run on raw CSV records before the
query-scoped materializer, so rejected records do not allocate a generated carrier. See
[Query-scoped CLR rows for dynamic sources](../../../../../docs/query-scoped-dynamic-sources.md)
for the format-neutral contract, lifecycle obligations, qualification evidence, and provider
activation checklist.
