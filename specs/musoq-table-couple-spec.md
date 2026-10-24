# Musoq TABLE and COUPLE Statements Specification

**Version:** 2.0.0
**Status:** Specification  
**Author:** Jakub Puchała

> Core §3 defines logical types and core §6.1 defines source signatures. Provider capabilities determine which sources can honor a TABLE contract. Examples use the generic source contracts in Appendix D; no concrete provider or installed package is assumed.

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Purpose and Motivation](#2-purpose-and-motivation)
3. [TABLE Statement](#3-table-statement)
4. [COUPLE Statement](#4-couple-statement)
5. [Usage Patterns](#5-usage-patterns)
6. [Type System](#6-type-system)
7. [Error Handling](#7-error-handling)
8. [Grammar Specification](#8-grammar-specification)
9. [Examples](#9-examples)
10. [Integration with Other Constructs](#10-integration-with-other-constructs)
11. [Appendix A: Quick Reference](#appendix-a-quick-reference)
12. [Appendix B: Comparison with Related Constructs](#appendix-b-comparison-with-related-constructs)
13. [Appendix C: Type Mapping Table](#appendix-c-type-mapping-table)
14. [Appendix D: Example Source Contracts](#appendix-d-example-source-contracts)

---

## 1. Introduction

### 1.1 Purpose

This document specifies the TABLE and COUPLE statements in Musoq. TABLE defines the visible column selection and order for coupled sources. Typed sources retain their exact provider types; TABLE-defined sources use the declared types. COUPLE creates query-local aliases for schema methods and can bind those aliases to a table shape, a source runtime settings profile, or both.

### 1.2 Scope

This specification covers:

- TABLE statement syntax and semantics
- COUPLE statement syntax and semantics
- Source runtime settings profile selection through COUPLE
- Column-local read modifiers for datasource interpretation hints
- Datasource-reported source contract diagnostics
- Supported data types
- Type resolution and validation
- Error conditions
- Integration with queries, CTEs, JOINs, and other Musoq constructs

### 1.3 Relationship to Other Specifications

This specification is part of the Musoq specification family (see *Musoq Core SQL Language Specification*, §1.6 for the full list).

- **Core Language Specification** (`musoq-core-language-spec.md`): TABLE and COUPLE are utility statements within the core language. This document provides the detailed specification.
- **Interpretation Schemas** (`musoq-binary-text-spec.md`): Similar in concept but distinct from `binary` and `text` schemas, which define declarative parsing rules rather than explicit type mappings.

The notation conventions defined in the core specification (§1.5) apply to this document. In particular, the key words MUST, MUST NOT, SHOULD, SHOULD NOT, and MAY carry normative weight when capitalized.

### 1.4 Conformance

An implementation that claims conformance to the TABLE/COUPLE profile MUST implement all features defined in this specification. This profile is optional; see the core specification (§1.7) for the conformance model.

### 1.5 Terminology

| Term | Definition |
|------|------------|
| **Table Definition** | A named structure with typed columns created by the TABLE statement |
| **Coupled Alias** | A named reference to a schema method bound via COUPLE that can be used as a data source |
| **Schema Method** | A method exposed by a schema provider (e.g., `schema.rows`, `csv.file('/path')`) |
| **Dynamic Row Source** | A data source that returns rows with unknown or object-typed columns at query definition time |
| **Settings Profile** | A named source runtime settings profile selected by COUPLE and resolved by the host for one source context |
| **Read Modifier** | A column-local key/value hint, declared in TABLE, that a datasource may use when reading that column |
| **Source Contract Diagnostic** | A datasource-reported info, warning, or error about a TABLE contract, read modifier, or source-vs-table mismatch |

---

## 2. Purpose and Motivation

### 2.1 Problem Statement

Musoq schema providers expose data sources with varying levels of type information:

1. **Strongly-typed sources**: Return entities with well-defined column names and types (e.g., git commits, file metadata)
2. **Dynamically-typed sources**: Return rows where column types are determined at runtime (e.g., CSV files, JSON data, collections of named records)
3. **Unknown sources**: Return `object` or `dynamic` typed values that require explicit type declarations

For dynamically-typed and unknown sources, the query engine cannot infer types at compile time, leading to:
- Limited type checking
- Potential runtime type mismatches
- No IntelliSense/completion support in tooling

### 2.2 Solution

TABLE and COUPLE statements allow query authors to:

1. **Define an explicit schema** with named, typed columns using TABLE
2. **Annotate individual columns** with datasource read hints when the source needs extra information
3. **Bind that schema** to a data source method using COUPLE
4. **Select a source runtime settings profile** for one source context when needed
5. **Use the coupled alias** as a data source in queries

This provides:
- Compile-time type checking
- Clear documentation of expected data shape
- Type-safe query expressions
- Reusable schema definitions within a query batch
- Query-local settings profile selection without embedding setting values in SQL

### 2.3 Design Philosophy

- **Explicit over implicit**: Schema MUST be declared before use
- **Fail-fast**: Invalid types or missing definitions produce clear errors
- **SQL-aligned syntax**: Uses familiar SQL-like syntax (`table`, `as`, `with table`, `with settings`)
- **Separation of concerns**: TABLE defines structure; COUPLE binds source aliases to structure and/or settings profile selection
- **Column-local hints**: Read modifiers belong to individual columns; there are no table-level defaults
- **Datasource authority**: The core language preserves modifiers, but datasources decide what each modifier means

---

## 3. TABLE Statement

### 3.1 Syntax

```ebnf
table_definition ::= TABLE table_name '{' column_def_list '}'

table_name ::= identifier

column_def_list ::= column_def { ',' column_def } [',']

column_def ::= column_name ':' type_name ['?'] read_modifier*

column_name ::= identifier

type_name ::= identifier { '.' identifier }

read_modifier ::= 'encoding' string_literal
                | 'culture' string_literal
                | 'format' string_literal
                | 'trim'
                | 'source' identifier string_literal
```

### 3.2 Structure

```sql
table TableName {
    Column1: type1,
    Column2: type2,
    Column3: type3?
};
```

**Components:**

| Component | Description |
|-----------|-------------|
| `table` | Keyword initiating table definition |
| `TableName` | Identifier naming the table structure (case-sensitive) |
| `{` `}` | Braces enclosing column definitions |
| `Column` | Column name identifier |
| `type` | Core keyword or visible query-local enum; qualified names are host extensions (§6.4) |
| `?` | Optional suffix indicating nullable type |
| read modifier | Optional column-local datasource hint such as `encoding`, `culture`, `format`, `trim`, or `source codec` |
| `,` | Column separator (trailing comma is optional) |
| `;` | Statement terminator (optional) |

### 3.3 Column Definitions

Each column definition consists of:

1. **Column name**: A case-sensitive identifier
2. **Type name**: A supported type keyword or qualified type name
3. **Nullable marker** (optional): `?` suffix for explicitly nullable types
4. **Read modifiers** (optional): zero or more column-local datasource hints

**Valid column definitions:**

```sql
Name: string           -- nullable logical string
Age: int               -- int for typed sources; int? for TABLE-defined sources
Price: decimal         -- decimal for typed sources; decimal? for TABLE-defined sources
IsActive: bool?        -- Explicitly nullable boolean
Date: datetimeoffset?  -- Explicitly nullable DateTimeOffset
```

### 3.4 Column Read Modifiers

Column read modifiers provide missing per-column information to the datasource. They are preserved in column metadata and passed through metadata, planning, and execution contexts. The core engine does not interpret modifier semantics beyond parsing and duplicate-key validation.

```sql
table LegacyRecord {
    Id: int,
    Name: string encoding 'windows-1250' trim,
    Amount: decimal culture 'pl-PL' format '#,##0.00',
    Payload: string source codec 'base64'
};
```

Supported modifier syntax:

| Syntax | Stored key | Stored value |
|--------|------------|--------------|
| `encoding 'windows-1250'` | `encoding` | `windows-1250` |
| `culture 'pl-PL'` | `culture` | `pl-PL` |
| `format '#,##0.00'` | `format` | `#,##0.00` |
| `trim` | `trim` | `true` |
| `source codec 'base64'` | `source.codec` | `base64` |

Modifier keys are lowercase. `source` modifiers use the key form `source.<identifier>`. Values preserve the string literal value. `trim` stores the string value `true`.

There are no table-level defaults. If a column does not specify a modifier, its read-modifier map is empty. The datasource SHOULD treat missing modifiers as "use datasource defaults" and MUST NOT infer table-level defaults from other columns.

Duplicate modifier keys on the same column are invalid:

```sql
table Bad {
    Name: string encoding 'utf-8' encoding 'windows-1250'
};
```

The core engine reports duplicate keys as `MQ2012_InvalidSchemaDefinition`. This applies to `source` keys as well, so `source codec 'a' source codec 'b'` is invalid, while `source codec 'a' source mode 'strict'` is valid because the keys are different.

Read modifiers are not CSV-specific. Different datasources may interpret them differently or ignore unsupported modifiers. Datasources SHOULD report source contract diagnostics when a modifier is unsupported, ignored, or conflicts with source capabilities.

### 3.5 Scope and Visibility

- **Scope**: Table definitions are scoped to the query batch in which they are defined
- **Visibility**: Visible only after definition; no forward references
- **Uniqueness**: Table names MUST be unique within a batch
- **Lifetime**: Exists only for the duration of query execution

### 3.6 Semantics

1. TABLE declares a named ordered logical column contract
2. The structure is registered and available for COUPLE statements
3. Column order in the definition determines column indices (0-based)
4. TABLE-defined sources automatically promote scalar types to nullable; provider-defined sources require exact declared types, including scalar nullability
5. Column read modifiers are stored with the corresponding column metadata

### 3.7 Enum Columns

A column type may name a query-local enum declared earlier in the batch, using
case-insensitive type-name matching. Host-native enum type names, including nullable
forms, MUST NOT be named in TABLE declarations. Binding fails with
`MQ2012_InvalidSchemaDefinition` at the offending column, even if the column is
not selected or the datasource supports logical scalar reads. The diagnostic
identifies the table, column, enum type, and underlying primitive type to use
instead. A query-local ENUM is also supported:

```sql
enum JobStatus : int {
    Queued = 10,
    Running = 20
};

table JobRows {
    Id: long,
    Status: JobStatus?
};
```

The TABLE descriptor freezes the enum identity, backing kind, flags marker,
members, aliases, and fingerprint at compilation. Dynamic values are never
examined row by row to infer that metadata. The datasource MUST advertise
logical scalar reads and read the primitive backing type; otherwise planning
fails with a source contract diagnostic. There is no object-valued fallback.

Read modifiers do not encode enum member maps. A datasource may define
representation-specific modifiers in a later profile, but the enum descriptor
always remains separate logical metadata. Provider-specific restrictions belong
to the provider contract, not this profile.

---

## 4. COUPLE Statement

### 4.1 Syntax

```ebnf
couple_statement ::= COUPLE couple_schema_source WITH couple_option { AND couple_option } AS alias_name

couple_option ::= TABLE table_name
                | SETTINGS settings_profile

couple_schema_source ::= schema_name '.' method_name

schema_name ::= identifier

method_name ::= identifier

table_name ::= identifier

settings_profile ::= identifier

alias_name ::= identifier
```

### 4.2 Structure

```sql
couple schema.method with table TableName as AliasName;
couple schema.method with settings ProfileName as AliasName;
couple schema.method with table TableName and settings ProfileName as AliasName;
couple schema.method with settings ProfileName and table TableName as AliasName;
```

**Components:**

| Component | Description |
|-----------|-------------|
| `couple` | Keyword initiating the binding |
| `schema.method` | Schema method reference |
| `with table` | Optional binding to a table definition |
| `TableName` | Name of a previously defined TABLE |
| `with settings` | Optional binding to a source runtime settings profile |
| `ProfileName` | Host-resolved settings profile name |
| `as` | Keyword introducing the alias |
| `AliasName` | The name to use as a data source in queries |
| `;` | Statement terminator (optional) |

### 4.3 Schema Method Reference

The schema method is specified as `schema.method`:

```sql
table MyTable { Name: string };
couple schema.rows with table MyTable as Source;
```

**Note**: The method name is specified WITHOUT parentheses in the COUPLE statement. Arguments are provided when the coupled alias is used in a query.

### 4.4 Semantics

1. COUPLE binds a schema method to a query-local alias
2. A `table` option defines the visible column subset and order; type matching follows the source table's explicit schema origin
3. A `settings` option selects a named source runtime settings profile for that source context
4. Settings-only couples infer table metadata from the underlying schema
5. Table-and-settings couples use the declared table shape and selected profile
6. COUPLE does not define or override column read modifiers; it only binds the TABLE contract to the source alias
7. The alias behaves like a method and is invoked with parentheses
8. Arguments can be passed to the aliased source at query time

Arguments at invocation time may use the published source parameter names:

```sql
table NamedRows { Value: string };
couple schema.named with table NamedRows as Data;
select * from Data(limit: 2, required: '4')
select * from Data('4', limit: 2)
```

Names are case-insensitive and may be reordered after a positional prefix.
Optional declared defaults are applied when omitted. The TABLE declaration
controls output shape only; it does not rename source parameters. Named
arguments require complete source-signature metadata and use the same
ordered argument binding as direct schema calls (core §6.1).
9. Setting values are resolved by the host and are not written inline in SQL

Duplicate `table` options, duplicate `settings` options, and `couple ... with` statements without at least one option are invalid.

### 4.5 Alias Usage

After coupling, the alias becomes a callable data source:

```sql
table Invocation { Column1: string };
couple schema.method with table Invocation as AliasName;

-- Independent invocation forms of the declared alias:
select * from AliasName()
select * from AliasName(true, 'filter', 123)
select a.Column1 from AliasName() a
with Data as (select Value from values { (Value: 'alpha'), (Value: 'beta') } input)
select * from AliasName(items: Data)
```

The coupled alias (`AliasName`) names the callable data source.
A complete visible CTE name in a source argument is a relation value. It is resolved only in that complete-argument position (optionally grouped as `(Data)`), is validated against the selected receiving collection contract, and never falls back to a string or a single row. Primitive collection receivers require one output column; record receivers match output columns by name and preserve duplicates and source order. A scalar column such as `p.Value` remains scalar. There is no `table(...)` wrapper. `DESC ARGUMENTS AliasName` reports the receiving contract without producing or enumerating the CTE. A table alias after the invocation (`a` in `AliasName() a`) is a normal FROM alias scoped to the current query block. When coupled-source columns are projected through a CTE, the core CTE output-name rules apply: explicit SELECT aliases define the exported names, and source qualifiers such as `a.` are not exported.

### 4.6 Names, Declaration Order, and Scope

| Name | Identity and lookup | Scope |
|------|---------------------|-------|
| TABLE name | Ordinal, case-sensitive | Query batch; declared before the referencing COUPLE |
| TABLE column | Ordinal, case-sensitive | Its TABLE; duplicate names are invalid |
| Coupled alias | Ordinal, case-insensitive | Query batch; duplicate aliases report `MQ3021` at the second declaration |
| Schema method | Core case-sensitive method rules | Selected provider schema |
| Named source argument | Ordinal, case-insensitive | Selected source signature; core positional-prefix rules apply |
| Settings profile | Preserved unchanged; comparison belongs to the host resolver | Each source context |
| FROM alias | Core query-block alias rules | Current query block |

TABLE and ENUM declarations MUST precede COUPLE declarations and executable
queries. A TABLE or enum reference MUST refer to an earlier declaration; no
forward declaration is implied by batch scope. COUPLE declarations MUST precede
CTEs and executable queries. Invalid ordering reports `MQ3102`.

TABLE names and coupled aliases occupy separate namespaces and MAY share a
spelling. A coupled alias is selected in a callable source position, while a
CTE is selected as a relation name. A FROM alias does not introduce a new TABLE
or COUPLE declaration. Coupled aliases take precedence over ordinary standalone
source functions with the same name; reserved interpretation callables retain
their core meaning. TABLE and column duplicates report `MQ2012`; coupled-alias
uniqueness uses the case-insensitive rule above even when two aliases select
different providers or profiles.

---

## 5. Usage Patterns

### 5.1 Basic Pattern

Define table, couple to source, query:

```sql
table Items {
    Name: string,
    Price: decimal
};
couple schema.products with table Items as Products;
select Name, Price from Products();
```

### 5.2 Multiple Tables and Sources

Define multiple tables and couple them to different sources:

```sql
table CustomerTable {
    Id: int,
    Name: string
};
table OrderTable {
    OrderId: int,
    CustomerId: int,
    Amount: decimal
};
couple schema.customers with table CustomerTable as Customers;
couple schema.orders with table OrderTable as Orders;

select c.Name, o.Amount 
from Customers() c 
inner join Orders() o on c.Id = o.CustomerId;
```

### 5.3 With Parameters

Pass arguments to the coupled alias:

```sql
table FilteredData {
    Value: string
};
couple schema.filtered with table FilteredData as Data;
select Value from Data(true, 'filter-pattern');
```

### 5.4 With CTEs

Combine with Common Table Expressions:

```sql
table TypedRow {
    Id: int,
    Name: string
};
couple schema.rows with table TypedRow as TypedSource;

with FilteredData as (
    select Id, Name from TypedSource() where Id > 10
)
select * from FilteredData;
```

If the coupled source is table-aliased inside the CTE, that alias is local to the CTE body. The CTE exposes `Id` and `Name`, not `t.Id` and `t.Name`:

```sql
table TypedRow { Id: int, Name: string };
couple schema.rows with table TypedRow as TypedSource;
with FilteredData as (
    select t.Id, t.Name from TypedSource() t where t.Id > 10
)
select Id, Name from FilteredData;
```

### 5.5 With CTE as Argument

Use CTE results as input to a coupled source:

```sql
table OutputSchema {
    Text: string
};
couple schema.transform with table OutputSchema as Transformer;

with InputData as (
    select Value from values { (Value: 'alpha'), (Value: 'beta') } input
)
select Text from Transformer(InputData);
```

### 5.6 With Source Runtime Settings

Select a host-resolved settings profile for a source:

```sql
couple schema.items with settings prod as Items;
select Id, Name from Items();
```

Combine explicit table shape with a settings profile:

```sql
table Item {
    Id: string,
    Name: string,
    Price: decimal?
};
couple schema.items with table Item and settings prod as Items;
select Id, Name, Price from Items();
```

The `table` and `settings` options can appear in either order:

```sql
table Item { Id: string, Name: string, Price: decimal? };
couple schema.items with settings prod and table Item as Items;
```

Repeated uses of the same schema method can select different settings profiles by using different coupled aliases:

```sql
couple schema.items with settings prod as ProdItems;
couple schema.items with settings staging as StagingItems;

select p.Id, s.Id
from ProdItems() p
inner join StagingItems() s on p.Id = s.Id;
```

The selected profile name is passed to the host runtime settings resolver. Values remain outside SQL and are keyed by the source context id, so repeated uses of the same schema method can resolve different settings.

Use the core `DESC SETTINGS` statement to inspect declared requirements and resolution status without exposing values:

```sql
couple schema.items with settings prod as ProdItems;
desc settings schema.items;
desc settings ProdItems;
```

### 5.7 Settings Resolution and Lifetime

The host resolver owns profile-name comparison and unknown-profile policy. The
engine MUST pass the selected name unchanged; it neither case-folds names nor
introduces a fallback profile. Host rejection fails compilation through the
existing diagnostic boundary before any row source opens.

Each source occurrence has its own source-context identity, including repeated
invocations of the same coupled alias. Different aliases and occurrences MUST
NOT share mutable settings state. Setting keys use ordinal, case-sensitive
comparison. Presence of a key satisfies its requirement even when its value is
an empty string; a differently cased key does not satisfy that requirement.

Requirement discovery precedes resolution. Once resolved, the engine MUST copy
and freeze the selected values into a snapshot. The corresponding metadata,
planning, and execution phases consume that established snapshot; later
mutation of resolver results, initial settings, or a provider-retained discovery
context MUST NOT change it. Resolution-status descriptions are immutable and
correspond to the same snapshot. A subsequent compilation may resolve new
values; this guarantee does not turn settings into process-global state.

Required settings MUST be checked before row-source opening. `DESC SETTINGS`
reports requirement names, required/secret flags, applicable phases, resolution
status, and descriptions. It MUST NOT expose resolved values, including secrets;
its inspection of a missing required setting reports status without opening the
source. The resolver API and host policy remain unchanged.

---

## 6. Type System

### 6.1 Supported Type Keywords

The nullable logical types below apply to `TableDefined` sources. For
`ProviderDefined` sources, the declaration's explicit scalar nullability
is preserved and must match the provider.

| Type Keyword | Logical type | Description |
|--------------|-----------|-------------|
| `byte` | `byte?` | Unsigned 8-bit integer |
| `sbyte` | `sbyte?` | Signed 8-bit integer |
| `short` | `short?` | Signed 16-bit integer |
| `int` | `int?` | Signed 32-bit integer |
| `long` | `long?` | Signed 64-bit integer |
| `ushort` | `ushort?` | Unsigned 16-bit integer |
| `uint` | `uint?` | Unsigned 32-bit integer |
| `ulong` | `ulong?` | Unsigned 64-bit integer |
| `float` | `float?` | Single-precision floating-point |
| `double` | `double?` | Double-precision floating-point |
| `decimal` | `decimal?` | High-precision decimal |
| `money` | `decimal?` | Alias for decimal |
| `bool` | `bool?` | Boolean |
| `boolean` | `bool?` | Alias for bool |
| `bit` | `bool?` | Alias for bool |
| `char` | `char?` | One UTF-16 code unit |
| `string` | `string` | Unicode text (nullable by nature) |
| `datetime` | `DateTime?` | Date and time |
| `datetimeoffset` | `DateTimeOffset?` | Date, time, and timezone offset |
| `timespan` | `TimeSpan?` | Time duration |
| `guid` | `Guid?` | Globally unique identifier |
| `object` | `object` | Dynamic logical value |

### 6.2 Nullable Types

The provider explicitly declares one of two schema origins:

- `ProviderDefined` is the default. TABLE types MUST match provider logical column types
  exactly, including nullable scalar types and logical enum identity. `Id: int`
  matches `int`, but not `int?` or `long`; `Age: int?` matches `int?`, but not
  `int`. Type synonyms (`bool`/`boolean`/`bit`, and
  `money`/`decimal`) resolve before comparison. No implicit conversion occurs.
- `TableDefined` is an explicit provider opt-in. TABLE supplies the schema and
  retains automatic nullable lifting (`int` and `int?` both supply `int?`).
  CSV and JSON providers accepting TABLE type metadata use this origin.
- String, dynamic and record values admit null. `string` and `string?` denote
  the same nullable logical string. Host annotation conventions are irrelevant.

Both origins expose only declared columns in declaration order. Each column
binds through the provider's existing name-resolution policy. The source's
storage indexes, source read types, row/entity metadata, stability, and supported
read modifiers MUST be preserved independently of output positions. Declaring
an existing column first MUST NOT change the storage field read for that column.

Every declared column is validated before row sources open, even if unselected.
Missing columns fail with `MQ3001`; ambiguous matches with `MQ3002`; type or
logical-identity mismatches with `MQ3005`. Diagnostics identify the TABLE and
column, point to the declaration, and suggest a matching source declaration.

Direct source calls and settings-only COUPLE preserve the source's original
schema. A provider may return arrays or dictionaries with either origin; row
representation MUST NOT be used to infer schema origin. Existing external
TABLE-aware providers must opt in to `TableDefined` when adopting this contract.

### 6.3 Type Keywords Are Case-Insensitive

Type keywords can be written in any case:

```sql
table Example {
    Col1: STRING,
    Col2: Int,
    Col3: DECIMAL
};
```

### 6.4 Fully-Qualified Type Names

Portable type references name core keywords or visible query-local enums.
Qualified identifiers remain syntactically recognizable for host extensions,
but do not acquire portable meaning from their spelling. A portable query MUST
NOT depend on host-qualified type names or loadable host types. Unknown portable
types fail during binding. No host-type loading mechanism is required.

### 6.5 Enum Type References

Query-local enum names are resolved before primitive type lookup and are
case-insensitive. Host-native enum type references in TABLE are rejected with
`MQ2012_InvalidSchemaDefinition`; use the underlying primitive type or a
query-local ENUM instead. Query-local enum columns carry their primitive backing type and nominal enum
metadata. Provider-defined enum columns supplied directly by a datasource remain
supported. Logical output position, underlying source storage position, read type
and enum identity are separate parts of the source contract.

TABLE's automatic scalar nullability applies to enum carriers. The logical
identity survives nullable lifting, CTEs, derived tables, joins, grouping,
sets, and projection.

---

## 7. Error Handling

### 7.1 Parse-Time Errors

| Error | Cause | Example |
|-------|-------|---------|
| **Unexpected Token** | Invalid syntax in TABLE or COUPLE | `table { }` (missing name) |
| **Missing Identifier** | Column without name | `table T { : string }` |
| **Missing Type** | Column without type | `table T { Name: }` |
| **Unclosed Braces** | Missing closing brace | `table T { Name: string` |
| **Duplicate Read Modifier** | Same read-modifier key repeated on one column | `table T { Name: string trim trim }` |
| **Duplicate COUPLE Option** | `table` or `settings` repeated in one COUPLE statement | `couple schema.labels with settings prod and settings dev as X` |
| **Missing COUPLE Option** | `with` is not followed by `table` or `settings` | `couple schema.labels with as X` |

### 7.2 Semantic Errors

| Error | Cause | Example |
|-------|-------|---------|
| **Unknown Type** | Unrecognized type name | `table T { Name: banana }` |
| **Invalid Schema Definition** | Empty table or structural issues | `table Empty {}` |
| **Duplicate Column Names** | Same column name used twice | `table T { Name: string, Name: int }` |
| **Undefined Table Reference** | COUPLE references non-existent TABLE | `couple schema.labels with table Unknown as Y` |
| **Undefined Alias** | Query references uncoupled alias | `select * from NonExistent()` |
| **Missing Source Runtime Setting** | Required source runtime setting was not resolved | `select * from SecureApi()` |
| **Source Contract Error** | Datasource reports that the TABLE contract or read modifiers cannot be honored | `table T { Name: string encoding 'x-unknown' }` |

### 7.3 Source Contract Diagnostics

Datasources may report contract diagnostics when describing or validating a source contract, before any rows are read.

| Severity | Normal diagnostic behavior |
|----------|----------------------------|
| `Info` | Appears in planning text only |
| `Warning` | Reported as `MQ5013_SourceContractWarning` and appears in planning text |
| `Error` | Reported as `MQ3071_SourceContractError`, stops binding, and appears in source inspection |

When a diagnostic references a TABLE column and modifier, normal diagnostics SHOULD point at the modifier text. If only the column is referenced, they SHOULD point at the column declaration. Diagnostics without available source-location metadata MUST retain an unknown location while keeping the datasource message. Unknown location is distinct from a genuine zero-length insertion point: public envelopes omit unknown offsets, line/column, and length; an authored insertion point retains its known position and zero length.

Typical source contract diagnostics include:

- A modifier is unsupported and ignored.
- A source supports only one encoding but a column requests another.
- A declared TABLE type conflicts with a datasource-known column kind.
- A modifier value is malformed for that datasource.

No diagnostic is required when modifiers are absent. Missing modifiers mean the datasource should use its defaults.

### 7.4 Runtime Adapter Diagnostics [Informative]

Host integration failures do not make a valid TABLE/COUPLE query syntactically invalid. Host adapter troubleshooting is outside the language contract and is documented separately.

<a id="74-diagnostic-codes"></a>

### 7.5 Diagnostic Codes

| Condition | Code | Public phase | Primary query location |
|-----------|------|--------------|------------------------|
| Unexpected token | `MQ2001` | Parse | Offending token |
| Unsupported COUPLE form | `MQ2030` | Parse | Unsupported syntax |
| Invalid TABLE declaration/type, duplicate TABLE/column/modifier | `MQ2012` | Parse | Offending declaration, column, or repeated modifier |
| Duplicate coupled alias | `MQ3021` | Bind | Second COUPLE declaration |
| Undefined TABLE | `MQ3023` | Bind | Referencing COUPLE |
| Invalid declaration order | `MQ3102` | Bind | Misplaced declaration |
| Missing provider column | `MQ3001` | Bind | TABLE column |
| Ambiguous provider column | `MQ3002` | Bind | TABLE column |
| Exact provider type or logical identity mismatch | `MQ3005` | Bind | TABLE column |
| Required runtime setting missing | `MQ3067` | Bind | Source invocation when available |
| Source contract error | `MQ3071` | Bind | Modifier, then column, otherwise unknown |
| Source contract warning | `MQ5013` | Bind | Modifier, then column, otherwise unknown |

Public phases follow the core diagnostic envelope, even when a condition is
found during semantic analysis: `MQ2012` retains its Parse classification.
Contract and required-setting failures precede row-source opening. Provider
codes remain attached as source details; they do not replace the engine code.

---

## 8. Grammar Specification

### 8.1 Table Definition Grammar

```ebnf
table_definition ::= TABLE identifier '{' column_def_list '}'

column_def_list ::= column_def { ',' column_def } [',']

column_def ::= identifier ':' type_name [ '?' ] read_modifier*

read_modifier ::= 'encoding' string_literal
                | 'culture' string_literal
                | 'format' string_literal
                | 'trim'
                | 'source' identifier string_literal

type_name ::= identifier
            | qualified_type_name

qualified_type_name ::= identifier { '.' identifier }
```

An unqualified `type_name` may resolve to a visible query-local enum.
`qualified_type_name` is reserved for host extensions (§6.4). Host-native enum
TABLE declarations, including nullable forms, are rejected during binding with
`MQ2012_InvalidSchemaDefinition`.

### 8.2 Couple Statement Grammar

```ebnf
couple_statement ::= COUPLE couple_schema_source WITH couple_option { AND couple_option } AS identifier

couple_option ::= TABLE identifier
                | SETTINGS identifier

couple_schema_source ::= identifier '.' identifier
```

### 8.3 Coupled Alias Reference Grammar

In FROM clauses:

```ebnf
coupled_source ::= alias_identifier '(' [ arg_list ] ')' [ table_alias ]

arg_list ::= source_arg { ',' source_arg }
source_arg ::= expression | identifier ':' expression

table_alias ::= identifier
```

### 8.4 Concrete Description Calls

```ebnf
description ::= DESC ( couple_schema_source [ invocation ] | alias_identifier invocation )
              | DESC SETTINGS ( couple_schema_source | alias_identifier ) [ invocation ]

invocation ::= '(' [ arg_list ] ')'
```

Concrete calls bind positional and named arguments using the same signature
rules as FROM calls. Omitting the invocation requests metadata for the named
source; it does not supply arguments. Other DESC forms follow the core grammar.
The schema and method names use the unprefixed source notation shown in §8.2.

---

## 9. Examples

### 9.1 Basic String Column

```sql
table DummyTable {
    Name: string
};
couple schema.rows with table DummyTable as SourceOfDummyRows;
select Name from SourceOfDummyRows();
```

### 9.2 Multiple Typed Columns

```sql
table DataTable {
    Country: string,
    Population: decimal
};
couple schema.countries with table DataTable as Countries;
select Country, Population from Countries() where Population > 100;
```

### 9.3 JOIN Between Coupled Sources

```sql
table FirstTable {
    Country: string,
    Population: decimal
};
table SecondTable {
    Name: string
};
couple schema.rows with table FirstTable as Source1;
couple schema.namedCountries with table SecondTable as Source2;

select s1.Country, s2.Name 
from Source1() s1 
inner join Source2() s2 on s1.Country = s2.Name;
```

### 9.4 Passing Parameters

```sql
table Parameters {
    Parameter0: bool,
    Parameter1: string
};
couple schema.parameters with table Parameters as Config;
select Parameter0, Parameter1 from Config(true, 'test');
```

### 9.5 All Supported Types

Declaration fragment: this lists the supported TABLE type keywords. It does not
select an unbounded object-valued source.

```sql
table AllTypes {
    ByteCol: byte,
    SByteCol: sbyte,
    ShortCol: short,
    IntCol: int,
    LongCol: long,
    UShortCol: ushort,
    UIntCol: uint,
    ULongCol: ulong,
    FloatCol: float,
    DoubleCol: double,
    DecimalCol: decimal,
    MoneyCol: money,
    BoolCol: bool,
    CharCol: char,
    StringCol: string,
    DateTimeCol: datetime,
    DateTimeOffsetCol: datetimeoffset,
    TimeSpanCol: timespan,
    GuidCol: guid,
    ObjectCol: object
};

```

### 9.6 Nullable with Trailing Comma

```sql
table NullableExample {
    Id: int?,
    Name: string,
    IsActive: bool?,
};
couple schema.nullableRows with table NullableExample as Data;
select Id, Name, IsActive from Data();
```

### 9.7 Source Runtime Settings Profiles

```sql
table ApiItem {
    Id: string,
    Name: string,
    Price: decimal?
};

couple schema.items with table ApiItem and settings prod as ProdItems;
couple schema.items with settings staging as StagingItems;

select p.Id, p.Price
from ProdItems() p
inner join StagingItems() s on p.Id = s.Id;
```

### 9.8 Column Read Modifiers

```sql
table LegacyInvoiceRow {
    InvoiceNo: string encoding 'windows-1250' trim,
    CustomerName: string encoding 'windows-1250' trim,
    Total: decimal culture 'pl-PL' format '#,##0.00',
    Attachment: string source codec 'base64'
};

couple schema.invoices with table LegacyInvoiceRow as Invoices;

select InvoiceNo, CustomerName, Total, Attachment
from Invoices('legacy-batch');
```

If `Total` omitted `culture` and `format`, the datasource would receive an empty modifier map for those keys and would use its own defaults. The `couple` statement remains unchanged; the annotations belong to TABLE columns.

---

## 10. Integration with Other Constructs

### 10.1 With CTEs (Common Table Expressions)

TABLE/COUPLE definitions MUST appear before CTEs that use them:

```sql
-- Correct order
table TypedRow { Id: int, Name: string };
couple schema.rows with table TypedRow as TypedSource;

with FilteredData as (
    select Id, Name from TypedSource()
)
select * from FilteredData;
```

CTE aliasing follows the core language rules. Source aliases inside the CTE body do not become part of the CTE output names:

```sql
table TypedRow { Id: int, Name: string };
couple schema.rows with table TypedRow as TypedSource;
with FilteredData as (
    select t.Id, t.Name from TypedSource() t
)
select Id, Name from FilteredData;
```

### 10.2 With JOINs

Coupled aliases can be used with all JOIN types:

```sql
table T1 { Key: string, Value1: int };
table T2 { Key: string, Value2: int };
couple schema.leftRows with table T1 as Left;
couple schema.rightRows with table T2 as Right;

-- INNER JOIN
select l.Value1, r.Value2 
from Left() l 
inner join Right() r on l.Key = r.Key;
```

```sql
table T1 { Key: string, Value1: int };
table T2 { Key: string, Value2: int };
couple schema.leftRows with table T1 as Left;
couple schema.rightRows with table T2 as Right;

-- LEFT JOIN
select l.Value1, r.Value2 
from Left() l 
left join Right() r on l.Key = r.Key;
```

```sql
table Events { Time: int, Name: string };
table Snapshots { Time: int, State: string };
couple schema.events with table Events as Events;
couple schema.snapshots with table Snapshots as Snapshots;

select e.Name, s.State
from Events() e
asof join Snapshots() s on e.Time >= s.Time;
```

### 10.3 With APPLY

Coupled aliases can be used with CROSS APPLY and OUTER APPLY:

```sql
table Container { Id: int };
table Item { Name: string, Price: decimal };
couple schema.containers with table Container as Containers;
couple schema.containerItems with table Item as Items;

select c.*, i.Name, i.Price
from Containers() c
cross apply Items(c.Id) i;
```

### 10.4 With Aggregations

Standard aggregation functions work with coupled sources:

```sql
table Sales { Product: string, Amount: decimal };
couple schema.sales with table Sales as SalesData;

select Product, Sum(Amount) as Total
from SalesData()
group by Product
having Sum(Amount) > 1000
order by Total desc;
```

### 10.5 With Set Operations

Coupled aliases can be used in UNION, EXCEPT, and INTERSECT:

```sql
table Record { Id: int, Name: string };
couple schema.firstRows with table Record as SourceA;
couple schema.secondRows with table Record as SourceB;

select Id, Name from SourceA()
union (Id)
select Id, Name from SourceB();
```

### 10.6 Statement Order Requirements

Within a query batch, the following dependency order applies (leading parameters
and earlier enum/let declarations follow core §4):

1. **TABLE definitions** (if any)
2. **COUPLE statements** (after any TABLE definitions they reference)
3. **CTEs** (if any)
4. **Query** (SELECT, FROM-first, etc.)

```sql
-- Correct order
table T1 { Col1: string };           -- 1. TABLE
table T2 { Col2: int };              -- 1. TABLE

couple schema.labels with table T1 as X;     -- 2. COUPLE
couple schema.keys with table T2 as Y;     -- 2. COUPLE

with CTE as (                       -- 3. CTE
    select * from X()
)
select * from CTE                   -- 4. Query
inner join Y() y on CTE.Col1::Int32 = y.Col2;
```

---

## Appendix A: Quick Reference

### TABLE Statement

```sql
table TableName {
    Column1: type1 [read modifiers],
    Column2: type2? [read modifiers],
    ...
};
```

Read modifiers:

```text
encoding 'name'
culture 'name'
format 'pattern'
trim
source identifier 'value'
```

### COUPLE Statement

```text
couple schema.method with table TableName as AliasName;
couple schema.method with settings ProfileName as AliasName;
couple schema.method with table TableName and settings ProfileName as AliasName;
couple schema.method with settings ProfileName and table TableName as AliasName;
```

### Usage in Query

```sql
select columns from AliasName([args]) [alias]
```

### Type Keywords

`byte`, `sbyte`, `short`, `int`, `long`, `ushort`, `uint`, `ulong`, `float`, `double`, `decimal`, `money`, `bool`, `boolean`, `bit`, `char`, `string`, `datetime`, `datetimeoffset`, `timespan`, `guid`, `object`

---

## Appendix B: Comparison with Related Constructs

| Construct | Purpose | Scope |
|-----------|---------|-------|
| **TABLE/COUPLE** | Explicit schema and settings profile binding for source aliases | Query batch |
| **CTE** | Named subquery result | Query batch |
| **binary/text schema** | Parse binary/text data | Query batch |
| **Provider schema** | Source-declared logical column contract | Schema provider |

---

## Appendix C: Type Mapping Table

The keyword aliases and value domains are defined by core §3. TABLE uses those
logical types without host mappings. For `TableDefined`, scalar and enum types
are nullable; for `ProviderDefined`, scalar nullability must match the provider
exactly (§6.2). String, dynamic and record types admit null.

Integer keywords retain their explicit widths: byte/sbyte 8, short/ushort 16,
int/uint 32, and long/ulong 64 bits. `money` is decimal; `boolean` and `bit` are bool.


## Appendix D: Example Source Contracts

Every `schema` method in this document is a generic receiving contract, not an
installed provider. Schema and method spellings have no built-in meaning. A host
running a source example supplies the described metadata and rows. The source
syntax fragments use `TableName`, `ProfileName`, and `AliasName` as placeholders.
Independent queries in one block are run separately with that block's declarations.
Declaration-only blocks illustrate syntax rather than an executable batch.

Unless a row below says **typed**, the source reports **TABLE-defined** origin:
the TABLE declaration supplies the visible columns and their order, and scalar
value columns acquire the nullable storage types specified in §6.2. Read modifiers
are forwarded unchanged. The source accepts those declared logical reads;
undeclared columns do not participate in the contract. A typed source instead
publishes its exact columns/types and storage positions; TABLE selects/reorders
those columns without changing the provider's types or positions.

Arguments are positional unless named labels are shown. Every empty `()` signature
is argumentless; additional arguments are rejected unless an overload is listed.
Samples state input order for representative-selection explanations, not a final
query output-order guarantee. All other samples leave row order unspecified.

| Method | Signature | Columns and sample rows |
|--------|-----------|-------------------------|
| `schema.rows` | `()` | `Id: int`, `Name: string`, `Country: string`, `Population: decimal`; `(12,'Poland','Poland',200)`, `(2,'Germany','Germany',100)`. TABLE selects the columns named by each example. |
| `schema.namedCountries` | `()` | `Name: string`; `'Poland'`, `'Germany'`. |
| `schema.products` | `()` | `Name: string`, `Price: decimal`; `('Book',12.50)`, `('Pen',2.00)`. |
| `schema.customers` | `()` | `Id: int`, `Name: string`; `(1,'Ada')`. |
| `schema.orders` | `()` | `OrderId: int`, `CustomerId: int`, `Amount: decimal`; `(10,1,25)`. |
| `schema.filtered` | `(enabled: bool, pattern: string)` | `Value: string`; returns `'filter-pattern'` when enabled and matching the supplied pattern. |
| `schema.named` | `(required: string, limit: int = 10)` | `Value: string`; repeats `required` up to `limit` rows. Labels are case-insensitive and defaults apply before source opening. |
| `schema.method` | `()`; overload `(enabled: bool, filter: string, count: int)`; overload `(items: (Value: string)[])` | `Column1: string`; default row `'sample'`; filtered overload returns up to `count` matching rows; collection overload maps each input `Value` to `Column1`, preserving duplicates and input order. |
| `schema.transform` | `(items: (Value: string)[])` | `Text: string`; maps each input `Value` to `Text`, preserving duplicates and input order. CTE inputs in §5.5 are inline VALUES rows. |
| `schema.items` | `()` | **Typed**: storage positions 0,1,2 are `Id: string`, `Name: string`, `Price: decimal?`; `('1','Book',12.50)`. Requirements and profiles are defined below. |
| `schema.countries` | `()` | `Country: string`, `Population: decimal`; `('Poland',200)`, `('Germany',100)`. |
| `schema.parameters` | `(enabled: bool, label: string)` | `Parameter0: bool`, `Parameter1: string`; returns the supplied argument values. |
| `schema.nullableRows` | `()` | `Id: int?`, `Name: string`, `IsActive: bool?`; `(null,'Ada',null)`, `(1,'Bob',true)`. |
| `schema.invoices` | `(batch: string)` | `InvoiceNo: string`, `CustomerName: string`, `Total: decimal`, `Attachment: string`. Key `'legacy-batch'` yields `('INV-1','Ada',12.50,'AA==')`. Encoding/culture/format/codec hints describe this fixture's read contract; no CSV reader or filesystem is assumed. |
| `schema.leftRows` | `()` | `Key: string`, `Value1: int`; `('a',1)`, `('b',2)`. |
| `schema.rightRows` | `()` | `Key: string`, `Value2: int`; `('a',10)`. |
| `schema.events` | `()` | `Time: int`, `Name: string`; `(10,'request')`. |
| `schema.snapshots` | `()` | `Time: int`, `State: string`; `(5,'ready')`, `(15,'later')`. |
| `schema.containers` | `()` | `Id: int`; row `(1)` identifies the container to expand. |
| `schema.containerItems` | `(containerId: int)` | `Name: string`, `Price: decimal`; container 1 identifies `[('Book',12.50)]`. |
| `schema.sales` | `()` | `Product: string`, `Amount: decimal`; `('Book',700)`, `('Book',600)`, `('Pen',50)`. |
| `schema.firstRows` | `()` | `Id: int`, `Name: string`; encounter order `(1,'First')`, `(1,'Duplicate')`, `(2,'Second')`. |
| `schema.secondRows` | `()` | `Id: int`, `Name: string`; `(1,'Other')`, `(3,'Third')`. Keyed UNION retains the first encountered left representative of key 1. |
| `schema.labels` | `()` | `Col1: string`; `'1'`. |
| `schema.keys` | `()` | `Col2: int`; `1`. |

For `schema.items`, `TOKEN` is an ordinal, required, secret setting used in all
source phases. The illustrative host resolver recognizes the exact names `prod`
and `staging`, rejects unknown profiles, and supplies separate snapshots for each
source occurrence. Both profiles expose the same published row shape; values are
never included in SQL or DESC SETTINGS output. This example policy does not
require other hosts to compare profile names in the same way (§5.7).

The concrete typed-source validation rules in §6 apply independently of these
TABLE-defined fixtures: a receiving source that publishes exact typed columns
must preserve provider origin, native storage positions, and validation before
row-source opening. A TABLE-defined fixture cannot demonstrate a typed-source
mismatch merely by changing its declaration.
