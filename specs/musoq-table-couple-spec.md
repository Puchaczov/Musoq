# Musoq TABLE and COUPLE Statements Specification

**Version:** 1.0.0-draft  
**Status:** Specification  
**Date:** February 2026

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

---

## 1. Introduction

### 1.1 Purpose

This document specifies the TABLE and COUPLE statements in Musoq — a pair of statements that work together to define explicit type schemas for data sources that return untyped, dynamically-typed, or object-based rows.

### 1.2 Scope

This specification covers:

- TABLE statement syntax and semantics
- COUPLE statement syntax and semantics
- Supported data types
- Type resolution and validation
- Error conditions
- Integration with queries, CTEs, JOINs, and other Musoq constructs

### 1.3 Relationship to Other Specifications

- **Core Language Specification**: TABLE and COUPLE are part of the utility statements defined in `musoq-core-language-spec.md`
- **Interpretation Schemas**: Similar in concept but distinct from `binary` and `text` schemas defined in `musoq-binary-text-spec.md`

### 1.4 Terminology

| Term | Definition |
|------|------------|
| **Table Definition** | A named structure with typed columns created by the TABLE statement |
| **Coupled Alias** | A named reference to a schema method bound via COUPLE that can be used as a data source |
| **Schema Method** | A method exposed by a schema provider (e.g., `#A.Entities`, `#csv.file('/path')`) |
| **Dynamic Row Source** | A data source that returns rows with unknown or object-typed columns at query definition time |

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
2. **Bind that schema** to a data source method using COUPLE
3. **Use the coupled alias** as a strongly-typed data source in queries

This provides:
- Compile-time type checking
- Clear documentation of expected data shape
- Type-safe query expressions
- Reusable schema definitions within a query batch

### 2.3 Design Philosophy

- **Explicit over implicit**: Schema must be declared before use
- **Fail-fast**: Invalid types or missing definitions produce clear errors
- **SQL-aligned syntax**: Uses familiar SQL-like syntax (`table`, `as`, `with table`)
- **Separation of concerns**: TABLE defines structure; COUPLE binds it to a source

---

## 3. TABLE Statement

### 3.1 Syntax

```ebnf
table_definition ::= TABLE table_name '{' column_def_list '}'

table_name ::= identifier

column_def_list ::= column_def { ',' column_def } [',']

column_def ::= column_name type_name ['?']

column_name ::= identifier

type_name ::= identifier
```

### 3.2 Structure

```sql
table TableName {
    Column1 type1,
    Column2 type2,
    Column3 type3?
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
| `,` | Column separator (trailing comma is optional) |
| `;` | Statement terminator (optional) |

### 3.3 Column Definitions

Each column definition consists of:

1. **Column name**: A case-sensitive identifier
2. **Type name**: A supported type keyword or qualified type name
3. **Nullable marker** (optional): `?` suffix for explicitly nullable types

**Valid column definitions:**

```sql
Name string           -- Non-nullable string
Age int               -- Nullable int (value types are auto-nullable)
Price decimal         -- Nullable decimal
IsActive bool?        -- Explicitly nullable boolean
Date datetimeoffset?  -- Explicitly nullable DateTimeOffset
```

### 3.4 Scope and Visibility

- **Scope**: Table definitions are scoped to the query batch in which they are defined
- **Visibility**: Visible only after definition; no forward references
- **Uniqueness**: Table names must be unique within a batch
- **Lifetime**: Exists only for the duration of query execution

### 3.5 Semantics

1. TABLE creates a named schema structure stored in memory
2. The structure is registered and available for COUPLE statements
3. Column order in the definition determines column indices (0-based)
4. Value types are automatically promoted to nullable to handle dynamic data

---

## 4. COUPLE Statement

### 4.1 Syntax

```ebnf
couple_statement ::= COUPLE schema_source WITH TABLE table_name AS alias_name

schema_source ::= ['#'] schema_name '.' method_name

schema_name ::= identifier

method_name ::= identifier

table_name ::= identifier

alias_name ::= identifier
```

### 4.2 Structure

```sql
couple #Schema.Method with table TableName as AliasName;
```

**Components:**

| Component | Description |
|-----------|-------------|
| `couple` | Keyword initiating the binding |
| `#Schema.Method` | Schema method reference (hash prefix optional) |
| `with table` | Keywords linking to the table definition |
| `TableName` | Name of a previously defined TABLE |
| `as` | Keyword introducing the alias |
| `AliasName` | The name to use as a data source in queries |
| `;` | Statement terminator (optional) |

### 4.3 Schema Method Reference

The schema method can be specified with or without the `#` prefix:

```sql
-- Both are equivalent
couple #A.Entities with table MyTable as Source;
couple A.Entities with table MyTable as Source;
```

**Note**: The method name is specified WITHOUT parentheses in the COUPLE statement. Arguments are provided when the coupled alias is used in a query.

### 4.4 Semantics

1. COUPLE binds a previously defined TABLE to a schema method
2. Creates an alias that can be used as a data source
3. The alias behaves like a method and is invoked with parentheses
4. Arguments can be passed to the aliased source at query time

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
with Data as (select * from #other.source())
select * from AliasName(Data)
```

---

## 5. Usage Patterns

### 5.1 Basic Pattern

Define table, couple to source, query:

```sql
table Items {
    Name string,
    Price decimal
};
couple #store.products with table Items as Products;
select Name, Price from Products();
```

### 5.2 Multiple Tables and Sources

Define multiple tables and couple them to different sources:

```sql
table CustomerTable {
    Id int,
    Name string
};
table OrderTable {
    OrderId int,
    CustomerId int,
    Amount decimal
};
couple #data.customers with table CustomerTable as Customers;
couple #data.orders with table OrderTable as Orders;

select c.Name, o.Amount 
from Customers() c 
inner join Orders() o on c.Id = o.CustomerId;
```

### 5.3 With Parameters

Pass arguments to the coupled alias:

```sql
table FilteredData {
    Value string
};
couple #source.method with table FilteredData as Data;
select Value from Data(true, 'filter-pattern');
```

### 5.4 With CTEs

Combine with Common Table Expressions:

```sql
table TypedRow {
    Id int,
    Name string
};
couple #A.Entities with table TypedRow as TypedSource;

with FilteredData as (
    select Id, Name from TypedSource() where Id > 10
)
select * from FilteredData;
```

### 5.5 With CTE as Argument

Use CTE results as input to a coupled source:

```sql
table OutputSchema {
    Text string
};
couple #processor.transform with table OutputSchema as Transformer;

with InputData as (
    select Value from #input.source()
)
select Text from Transformer(InputData);
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
    Col1 STRING,
    Col2 Int,
    Col3 DECIMAL
};
```

### 6.4 Fully-Qualified Type Names

Types not in the keyword list can be specified using their fully-qualified .NET type name:

```sql
table Example {
    CustomData System.SomeCustomType
};
```

**Note**: The type must be loadable at runtime. If the type cannot be resolved, a `TypeNotFoundException` is raised.

---

## 7. Error Handling

### 7.1 Parse-Time Errors

| Error | Cause | Example |
|-------|-------|---------|
| **Unexpected Token** | Invalid syntax in TABLE or COUPLE | `table { }` (missing name) |
| **Missing Identifier** | Column without name | `table T { string }` |
| **Missing Type** | Column without type | `table T { Name }` |
| **Unclosed Braces** | Missing closing brace | `table T { Name string` |

### 7.2 Semantic Errors

| Error | Cause | Example |
|-------|-------|---------|
| **TypeNotFoundException** | Unrecognized type name | `table T { Name banana }` |
| **Invalid Schema Definition** | Empty table or structural issues | `table Empty {}` |
| **Duplicate Column Names** | Same column name used twice | `table T { Name string, Name int }` |
| **Undefined Table Reference** | COUPLE references non-existent TABLE | `couple #A.X with table Unknown as Y` |
| **Undefined Alias** | Query references uncoupled alias | `select * from NonExistent()` |

### 7.3 Diagnostic Codes

| Code | Description |
|------|-------------|
| `MQ2001` | Unexpected Token |
| `MQ2008` | Duplicate Alias |
| `MQ2012` | Invalid Schema Definition |
| `MQ4008` | Duplicate Schema Field |
| `MQ2030` | Unsupported Syntax |

---

## 8. Grammar Specification

### 8.1 Table Definition Grammar

```ebnf
table_definition ::= TABLE identifier '{' column_def_list '}'

column_def_list ::= column_def { ',' column_def } [',']

column_def ::= identifier type_name [ '?' ]

type_name ::= identifier
            | qualified_type_name

qualified_type_name ::= identifier { '.' identifier }
```

### 8.2 Couple Statement Grammar

```ebnf
couple_statement ::= COUPLE schema_source WITH TABLE identifier AS identifier

schema_source ::= [ '#' ] identifier '.' identifier
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
    Name string
};
couple #A.Entities with table DummyTable as SourceOfDummyRows;
select Name from SourceOfDummyRows();
```

### 9.2 Multiple Typed Columns

```sql
table DataTable {
    Country string,
    Population decimal
};
couple #data.countries with table DataTable as Countries;
select Country, Population from Countries() where Population > 100;
```

### 9.3 JOIN Between Coupled Sources

```sql
table FirstTable {
    Country string,
    Population decimal
};
table SecondTable {
    Name string
};
couple #A.Entities with table FirstTable as Source1;
couple #B.Entities with table SecondTable as Source2;

select s1.Country, s2.Name 
from Source1() s1 
inner join Source2() s2 on s1.Country = s2.Name;
```

### 9.4 Passing Parameters

```sql
table Parameters {
    Parameter0 bool,
    Parameter1 string
};
couple #config.reader with table Parameters as Config;
select Parameter0, Parameter1 from Config(true, 'test');
```

### 9.5 All Supported Types

```sql
table AllTypes {
    ByteCol byte,
    SByteCol sbyte,
    ShortCol short,
    IntCol int,
    LongCol long,
    UShortCol ushort,
    UIntCol uint,
    ULongCol ulong,
    FloatCol float,
    DoubleCol double,
    DecimalCol decimal,
    MoneyCol money,
    BoolCol bool,
    CharCol char,
    StringCol string,
    DateTimeCol datetime,
    DateTimeOffsetCol datetimeoffset,
    TimeSpanCol timespan,
    GuidCol guid,
    ObjectCol object
};
couple #data.source with table AllTypes as TypedData;
select * from TypedData();
```

### 9.6 Nullable with Trailing Comma

```sql
table NullableExample {
    Id int?,
    Name string,
    IsActive bool?,
};
couple #dynamic.source with table NullableExample as Data;
select Id, Name, IsActive from Data();
```

---

## 10. Integration with Other Constructs

### 10.1 With CTEs (Common Table Expressions)

TABLE/COUPLE definitions MUST appear before CTEs:

```sql
-- Correct order
table TypedRow { Id int, Name string };
couple #A.Entities with table TypedRow as TypedSource;

with FilteredData as (
    select Id, Name from TypedSource()
)
select * from FilteredData;
```

### 10.2 With JOINs

Coupled aliases can be used with all JOIN types:

```sql
table T1 { Key string, Value1 int };
table T2 { Key string, Value2 int };
couple #data.left with table T1 as Left;
couple #data.right with table T2 as Right;

-- INNER JOIN
select l.Value1, r.Value2 
from Left() l 
inner join Right() r on l.Key = r.Key;

-- LEFT JOIN
select l.Value1, r.Value2 
from Left() l 
left join Right() r on l.Key = r.Key;
```

### 10.3 With APPLY

Coupled aliases can be used with CROSS APPLY and OUTER APPLY:

```sql
table Container { Items object };
table Item { Name string, Price decimal };
couple #data.containers with table Container as Containers;
couple #data.items with table Item as Items;

select c.*, i.Name, i.Price
from Containers() c
cross apply Items(c.Items) i;
```

### 10.4 With Aggregations

Standard aggregation functions work with coupled sources:

```sql
table Sales { Product string, Amount decimal };
couple #data.sales with table Sales as SalesData;

select Product, Sum(Amount) as Total
from SalesData()
group by Product
having Sum(Amount) > 1000
order by Total desc;
```

### 10.5 With Set Operations

Coupled aliases can be used in UNION, EXCEPT, and INTERSECT:

```sql
table Record { Id int, Name string };
couple #source.a with table Record as SourceA;
couple #source.b with table Record as SourceB;

select Id, Name from SourceA()
union (Id)
select Id, Name from SourceB();
```

### 10.6 Statement Order Requirements

Within a query batch, statements must follow this order:

1. **TABLE definitions** (one or more)
2. **COUPLE statements** (referencing previously defined tables)
3. **CTEs** (if any)
4. **Query** (SELECT, FROM-first, etc.)

```sql
-- Correct order
table T1 { Col1 string };           -- 1. TABLE
table T2 { Col2 int };              -- 1. TABLE

couple #A.X with table T1 as X;     -- 2. COUPLE
couple #B.Y with table T2 as Y;     -- 2. COUPLE

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
    Column1 type1,
    Column2 type2?,
    ...
};
```

### COUPLE Statement

```sql
couple [#]Schema.Method with table TableName as AliasName;
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
| **TABLE/COUPLE** | Explicit schema for dynamic sources | Query batch |
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
