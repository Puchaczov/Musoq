# Musoq TABLE and COUPLE Statements Specification

**Version:** 1.1.0
**Status:** Specification  
**Author:** Jakub Puchała

> **Compatibility note:** TABLE/COUPLE syntax is defined at language level, but successful execution also depends on target schema plugin implementation and host/runtime version alignment.

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

---

## 1. Introduction

### 1.1 Purpose

This document specifies the TABLE and COUPLE statements in Musoq. TABLE defines explicit row shapes for data sources that return untyped, dynamically-typed, or object-based rows. COUPLE creates query-local aliases for schema methods and can bind those aliases to a table shape, a source runtime settings profile, or both.

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
| **Schema Method** | A method exposed by a schema provider (e.g., `A.Entities`, `csv.file('/path')`) |
| **Dynamic Row Source** | A data source that returns rows with unknown or object-typed columns at query definition time |
| **Settings Profile** | A named source runtime settings profile selected by COUPLE and resolved by the host for one source context |
| **Read Modifier** | A column-local key/value hint, declared in TABLE, that a datasource may use when reading that column |
| **Source Contract Diagnostic** | A datasource-reported info, warning, or error about a TABLE contract, read modifier, or source-vs-table mismatch |

---

## 2. Purpose and Motivation

### 2.1 Problem Statement

Musoq schema providers expose data sources with varying levels of type information:

1. **Strongly-typed sources**: Return entities with well-defined column names and types (e.g., git commits, file metadata)
2. **Dynamically-typed sources**: Return rows where column types are determined at runtime (e.g., CSV files, JSON data, ExpandoObject collections)
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
| `type` | Type keyword or fully-qualified type name |
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
Name: string           -- Non-nullable string
Age: int               -- Nullable int (value types are auto-nullable)
Price: decimal         -- Nullable decimal
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

1. TABLE creates a named schema structure stored in memory
2. The structure is registered and available for COUPLE statements
3. Column order in the definition determines column indices (0-based)
4. Value types are automatically promoted to nullable to handle dynamic data
5. Column read modifiers are stored with the corresponding column metadata

### 3.7 Enum Columns

A column type may name a query-local enum declared earlier in the batch, using
case-insensitive type-name matching, or a native CLR enum using its exact fully
qualified name:

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
always remains separate logical metadata. The initial SeparatedValues profile
accepts no non-empty modifiers for enum columns.

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
couple Schema.Method with table TableName as AliasName;
couple Schema.Method with settings ProfileName as AliasName;
couple Schema.Method with table TableName and settings ProfileName as AliasName;
couple Schema.Method with settings ProfileName and table TableName as AliasName;
```

**Components:**

| Component | Description |
|-----------|-------------|
| `couple` | Keyword initiating the binding |
| `Schema.Method` | Schema method reference |
| `with table` | Optional binding to a table definition |
| `TableName` | Name of a previously defined TABLE |
| `with settings` | Optional binding to a source runtime settings profile |
| `ProfileName` | Host-resolved settings profile name |
| `as` | Keyword introducing the alias |
| `AliasName` | The name to use as a data source in queries |
| `;` | Statement terminator (optional) |

### 4.3 Schema Method Reference

The schema method is specified as `Schema.Method`:

```sql
couple A.Entities with table MyTable as Source;
```

**Note**: The method name is specified WITHOUT parentheses in the COUPLE statement. Arguments are provided when the coupled alias is used in a query.

### 4.4 Semantics

1. COUPLE binds a schema method to a query-local alias
2. A `table` option supplies explicit column metadata for dynamic sources
3. A `settings` option selects a named source runtime settings profile for that source context
4. Settings-only couples infer table metadata from the underlying schema
5. Table-and-settings couples use the declared table shape and selected profile
6. COUPLE does not define or override column read modifiers; it only binds the TABLE contract to the source alias
7. The alias behaves like a method and is invoked with parentheses
8. Arguments can be passed to the aliased source at query time

Arguments at invocation time may use the reflected table-constructor names:

```sql
select * from Data(limit: 2, required: '4')
select * from Data('4', limit: 2)
```

Names are case-insensitive and may be reordered after a positional prefix.
Optional reflected defaults are applied when omitted. The TABLE declaration
controls output shape only; it does not rename constructor parameters. Named
arguments require `GetRawConstructors()` metadata and are lowered to the same
canonical positional vector used by direct schema calls.
9. Setting values are resolved by the host and are not written inline in SQL

Duplicate `table` options, duplicate `settings` options, and `couple ... with` statements without at least one option are invalid.

### 4.5 Alias Usage

After coupling, the alias becomes a callable data source:

```sql
-- Without arguments
select * from AliasName()

-- With arguments
select * from AliasName(true, 'filter', 123)

-- With table alias
select a.Column1 from AliasName() a

-- Can be used with CTE results as arguments
with Data as (select * from other.source())
select * from AliasName(Data)
```

The coupled alias (`AliasName`) names the callable data source. A table alias after the invocation (`a` in `AliasName() a`) is a normal FROM alias scoped to the current query block. When coupled-source columns are projected through a CTE, the core CTE output-name rules apply: explicit SELECT aliases define the exported names, and source qualifiers such as `a.` are not exported.

---

## 5. Usage Patterns

### 5.1 Basic Pattern

Define table, couple to source, query:

```sql
table Items {
    Name: string,
    Price: decimal
};
couple store.products with table Items as Products;
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
couple data.customers with table CustomerTable as Customers;
couple data.orders with table OrderTable as Orders;

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
couple source.method with table FilteredData as Data;
select Value from Data(true, 'filter-pattern');
```

### 5.4 With CTEs

Combine with Common Table Expressions:

```sql
table TypedRow {
    Id: int,
    Name: string
};
couple A.Entities with table TypedRow as TypedSource;

with FilteredData as (
    select Id, Name from TypedSource() where Id > 10
)
select * from FilteredData;
```

If the coupled source is table-aliased inside the CTE, that alias is local to the CTE body. The CTE exposes `Id` and `Name`, not `t.Id` and `t.Name`:

```sql
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
couple processor.transform with table OutputSchema as Transformer;

with InputData as (
    select Value from input.source()
)
select Text from Transformer(InputData);
```

### 5.6 With Source Runtime Settings

Select a host-resolved settings profile for a source:

```sql
couple api.items with settings prod as Items;
select Id, Name from Items();
```

Combine explicit table shape with a settings profile:

```sql
table Item {
    Id: string,
    Name: string,
    Price: decimal?
};
couple api.items with table Item and settings prod as Items;
select Id, Name, Price from Items();
```

The `table` and `settings` options can appear in either order:

```sql
couple api.items with settings prod and table Item as Items;
```

Repeated uses of the same schema method can select different settings profiles by using different coupled aliases:

```sql
couple api.items with settings prod as ProdItems;
couple api.items with settings staging as StagingItems;

select p.Id, s.Id
from ProdItems() p
inner join StagingItems() s on p.Id = s.Id;
```

The selected profile name is passed to the host runtime settings resolver. Values remain outside SQL and are keyed by the source context id, so repeated uses of the same schema method can resolve different settings.

Use the core `DESC SETTINGS` statement to inspect declared requirements and resolution status without exposing values:

```sql
desc settings api.items;
desc settings ProdItems;
```

---

## 6. Type System

### 6.1 Supported Type Keywords

| Type Keyword | .NET Type | Description |
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
| `char` | `char?` | Single Unicode character |
| `string` | `string` | Unicode text (nullable by nature) |
| `datetime` | `DateTime?` | Date and time |
| `datetimeoffset` | `DateTimeOffset?` | Date, time, and timezone offset |
| `timespan` | `TimeSpan?` | Time duration |
| `guid` | `Guid?` | Globally unique identifier |
| `object` | `object` | Any object type |

### 6.2 Nullable Types

**Automatic Nullability:**
- All value types are automatically promoted to nullable (`int` → `int?`)
- This allows handling of dynamic sources where values may be null
- Reference types (`string`, `object`) are inherently nullable

**Explicit Nullability:**
- The `?` suffix can be used for documentation purposes: `int?`, `decimal?`
- Semantically identical to the base type for value types in TABLE context

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

Types not in the keyword list can be specified using their fully-qualified .NET type name:

```sql
table Example {
    CustomData: System.SomeCustomType
};
```

**Note**: The type must be loadable at runtime. If the type cannot be resolved, a `TypeNotFoundException` is raised.

### 6.5 Enum Type References

Query-local enum names are resolved before primitive or CLR type lookup and are
case-insensitive. Native enum references MUST be exact fully qualified CLR
names. Both forms use a primitive integral `ColumnType`, an exact
source-facing `SourceReadType`, and portable `EnumType` metadata. Ordinary
columns use the same `ColumnType` and `SourceReadType` and have no enum
metadata.

TABLE's automatic value-type nullability applies to enum carriers. The logical
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
| **Duplicate COUPLE Option** | `table` or `settings` repeated in one COUPLE statement | `couple A.X with settings prod and settings dev as X` |
| **Missing COUPLE Option** | `with` is not followed by `table` or `settings` | `couple A.X with as X` |

### 7.2 Semantic Errors

| Error | Cause | Example |
|-------|-------|---------|
| **TypeNotFoundException** | Unrecognized type name | `table T { Name: banana }` |
| **Invalid Schema Definition** | Empty table or structural issues | `table Empty {}` |
| **Duplicate Column Names** | Same column name used twice | `table T { Name: string, Name: int }` |
| **Undefined Table Reference** | COUPLE references non-existent TABLE | `couple A.X with table Unknown as Y` |
| **Undefined Alias** | Query references uncoupled alias | `select * from NonExistent()` |
| **Missing Source Runtime Setting** | Required source runtime setting was not resolved | `select * from SecureApi()` |
| **Source Contract Error** | Datasource reports that the TABLE contract or read modifiers cannot be honored | `table T { Name: string encoding 'x-unknown' }` |
| **Constructor Not Found** | Internal adapter type generated for a schema source does not expose the expected constructor | `couple separatedvalues.comma with table CsvRow as Csv` |

### 7.3 Source Contract Diagnostics

Datasources may report contract diagnostics from `DescribeSource` or `TryPlanSource`.

| Severity | Normal diagnostic behavior |
|----------|----------------------------|
| `Info` | Appears in planning text only |
| `Warning` | Reported as `MQ5013_SourceContractWarning` and appears in planning text |
| `Error` | Reported as `MQ3071_SourceContractError`, stops compilation, and appears in planning text |

When a diagnostic references a TABLE column and modifier, normal diagnostics SHOULD point at the modifier text. If only the column is referenced, they SHOULD point at the column declaration. Diagnostics without table-origin metadata use the normal empty-span fallback while keeping the datasource message.

Typical source contract diagnostics include:

- A modifier is unsupported and ignored.
- A source supports only one encoding but a column requests another.
- A declared TABLE type conflicts with a datasource-known column kind.
- A modifier value is malformed for that datasource.

No diagnostic is required when modifiers are absent. Missing modifiers mean the datasource should use its defaults.

### 7.4 Runtime Adapter Diagnostics [Informative]

When COUPLE is used with dynamic file-based sources (for example separated values), host/runtime internals may route through generated adapter/helper types. In stack traces, names such as `memoryMapped` can appear.

If execution fails with an error similar to:

```text
Constructor on type '...memoryMapped' not found
```

treat this as a **runtime/plugin integration issue**, not as invalid TABLE/COUPLE query syntax.

Recommended checks:

1. Verify schema plugin package version matches the engine version.
2. Validate source method signature and arguments independently (without COUPLE).
3. Retry with non-memory-mapped source mode, if the plugin exposes one.
4. Reproduce in another host (API/runtime) to distinguish host-preprocessor issues from engine/plugin issues.

### 7.4 Diagnostic Codes

| Code | Description |
|------|-------------|
| `MQ2001` | Unexpected Token |
| `MQ2008` | Duplicate Alias |
| `MQ2012` | Invalid Schema Definition |
| `MQ3071` | Source Contract Error |
| `MQ4008` | Duplicate Schema Field |
| `MQ2030` | Unsupported Syntax |
| `MQ5013` | Source Contract Warning |

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

An unqualified `type_name` may resolve to a visible query-local enum. A
`qualified_type_name` may resolve to a reachable native CLR enum. Resolution
does not make other CLR enum types globally visible.

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

arg_list ::= expression { ',' expression }

table_alias ::= identifier
```

---

## 9. Examples

### 9.1 Basic String Column

```sql
table DummyTable {
    Name: string
};
couple A.Entities with table DummyTable as SourceOfDummyRows;
select Name from SourceOfDummyRows();
```

### 9.2 Multiple Typed Columns

```sql
table DataTable {
    Country: string,
    Population: decimal
};
couple data.countries with table DataTable as Countries;
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
couple A.Entities with table FirstTable as Source1;
couple B.Entities with table SecondTable as Source2;

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
couple config.reader with table Parameters as Config;
select Parameter0, Parameter1 from Config(true, 'test');
```

### 9.5 All Supported Types

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
couple data.source with table AllTypes as TypedData;
select * from TypedData();
```

### 9.6 Nullable with Trailing Comma

```sql
table NullableExample {
    Id: int?,
    Name: string,
    IsActive: bool?,
};
couple dynamic.source with table NullableExample as Data;
select Id, Name, IsActive from Data();
```

### 9.7 Source Runtime Settings Profiles

```sql
table ApiItem {
    Id: string,
    Name: string,
    Price: decimal?
};

couple api.items with table ApiItem and settings prod as ProdItems;
couple api.items with settings staging as StagingItems;

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

couple separatedvalues.comma with table LegacyInvoiceRow as Invoices;

select InvoiceNo, CustomerName, Total, Attachment
from Invoices('legacy-invoices.csv');
```

If `Total` omitted `culture` and `format`, the datasource would receive an empty modifier map for those keys and would use its own defaults. The `couple` statement remains unchanged; the annotations belong to TABLE columns.

---

## 10. Integration with Other Constructs

### 10.1 With CTEs (Common Table Expressions)

TABLE/COUPLE definitions MUST appear before CTEs:

```sql
-- Correct order
table TypedRow { Id: int, Name: string };
couple A.Entities with table TypedRow as TypedSource;

with FilteredData as (
    select Id, Name from TypedSource()
)
select * from FilteredData;
```

CTE aliasing follows the core language rules. Source aliases inside the CTE body do not become part of the CTE output names:

```sql
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
couple data.left with table T1 as Left;
couple data.right with table T2 as Right;

-- INNER JOIN
select l.Value1, r.Value2 
from Left() l 
inner join Right() r on l.Key = r.Key;

-- LEFT JOIN
select l.Value1, r.Value2 
from Left() l 
left join Right() r on l.Key = r.Key;

-- ASOF JOIN (nearest-match on ordered column)
table Events { Time: int, Name: string };
table Snapshots { Time: int, State: string };
couple data.events with table Events as Events;
couple data.snapshots with table Snapshots as Snapshots;

select e.Name, s.State
from Events() e
asof join Snapshots() s on e.Time >= s.Time;
```

### 10.3 With APPLY

Coupled aliases can be used with CROSS APPLY and OUTER APPLY:

```sql
table Container { Items: object };
table Item { Name: string, Price: decimal };
couple data.containers with table Container as Containers;
couple data.items with table Item as Items;

select c.*, i.Name, i.Price
from Containers() c
cross apply Items(c.Items) i;
```

### 10.4 With Aggregations

Standard aggregation functions work with coupled sources:

```sql
table Sales { Product: string, Amount: decimal };
couple data.sales with table Sales as SalesData;

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
couple source.a with table Record as SourceA;
couple source.b with table Record as SourceB;

select Id, Name from SourceA()
union (Id)
select Id, Name from SourceB();
```

### 10.6 Statement Order Requirements

Within a query batch, statements MUST follow this order:

1. **TABLE definitions** (if any)
2. **COUPLE statements** (after any TABLE definitions they reference)
3. **CTEs** (if any)
4. **Query** (SELECT, FROM-first, etc.)

```sql
-- Correct order
table T1 { Col1: string };           -- 1. TABLE
table T2 { Col2: int };              -- 1. TABLE

couple A.X with table T1 as X;     -- 2. COUPLE
couple B.Y with table T2 as Y;     -- 2. COUPLE

with CTE as (                       -- 3. CTE
    select * from X()
)
select * from CTE                   -- 4. Query
inner join Y() on CTE.Col1 = Y.Col2;
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
couple Schema.Method with table TableName as AliasName;
couple Schema.Method with settings ProfileName as AliasName;
couple Schema.Method with table TableName and settings ProfileName as AliasName;
couple Schema.Method with settings ProfileName and table TableName as AliasName;
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
| **ISchemaTable** | Built-in schema definition | Schema provider |

---

## Appendix C: Type Mapping Table

| Musoq Keyword | .NET Type (Nullable) | SQL Server Equivalent |
|---------------|---------------------|----------------------|
| `byte` | `byte?` | `TINYINT` |
| `sbyte` | `sbyte?` | `SMALLINT` |
| `short` | `short?` | `SMALLINT` |
| `int` | `int?` | `INT` |
| `long` | `long?` | `BIGINT` |
| `ushort` | `ushort?` | `INT` |
| `uint` | `uint?` | `BIGINT` |
| `ulong` | `ulong?` | `DECIMAL(20,0)` |
| `float` | `float?` | `REAL` |
| `double` | `double?` | `FLOAT` |
| `decimal` | `decimal?` | `DECIMAL` |
| `money` | `decimal?` | `MONEY` |
| `bool` | `bool?` | `BIT` |
| `char` | `char?` | `NCHAR(1)` |
| `string` | `string` | `NVARCHAR` |
| `datetime` | `DateTime?` | `DATETIME` |
| `datetimeoffset` | `DateTimeOffset?` | `DATETIMEOFFSET` |
| `timespan` | `TimeSpan?` | `TIME` |
| `guid` | `Guid?` | `UNIQUEIDENTIFIER` |
| `object` | `object` | N/A |
