# Musoq Core SQL Language Specification

**Version:** 1.0.0-draft  
**Status:** Specification  
**Date:** February 2026

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Lexical Elements](#2-lexical-elements)
3. [Data Types](#3-data-types)
4. [Statement Structure](#4-statement-structure)
5. [SELECT Clause](#5-select-clause)
6. [FROM Clause](#6-from-clause)
7. [WHERE Clause](#7-where-clause)
8. [JOIN Clause](#8-join-clause)
9. [APPLY Clause](#9-apply-clause)
10. [GROUP BY and Aggregation](#10-group-by-and-aggregation)
11. [Set Operations](#11-set-operations)
12. [ORDER BY, SKIP, TAKE](#12-order-by-skip-take)
13. [Common Table Expressions (CTEs)](#13-common-table-expressions-ctes)
14. [TABLE and COUPLE Statements](#14-table-and-couple-statements)
15. [DESC Statement](#15-desc-statement)
16. [Reordered Query Syntax](#16-reordered-query-syntax)
17. [Built-in Functions Reference](#17-built-in-functions-reference)
18. [NULL Semantics](#18-null-semantics)
19. [String Comparison Semantics](#19-string-comparison-semantics)
20. [Array and Property Access](#20-array-and-property-access)
21. [Automatic Type Coercion](#21-automatic-type-coercion)
22. [Error Catalog](#22-error-catalog)
23. [Formal Grammar](#23-formal-grammar)
24. [Appendices](#24-appendices)

---

## 1. Introduction

### 1.1 Purpose

This document specifies the Musoq SQL dialect — a query language for querying diverse data sources (files, APIs, git repositories, operating system resources, and more) using SQL syntax. The specification is intended for query authors who need to write correct Musoq queries without knowledge of the engine's internal implementation.

### 1.2 Scope

This specification covers:

- Complete SQL syntax supported by the query engine
- All built-in operators, expressions, and functions
- Data types and type system behavior
- Query execution semantics (clause evaluation order, NULL handling, type coercion)
- Error conditions and their causes

This specification does **not** cover:

- Specific data source schemas (e.g., git, file system, Docker) — each data source defines its own tables and columns
- Internal compilation or code generation details
- Performance characteristics or optimization strategies
- The interpretation schema extension (binary/text parsing) — see the separate `musoq-binary-text-spec.md`

### 1.3 Relationship to Standard SQL

Musoq implements a subset of SQL with several extensions:

| Aspect | Standard SQL | Musoq |
|--------|-------------|-------|
| Data sources | Tables in a database | Schema providers (`#schema.method()`) |
| Pagination | `OFFSET` / `LIMIT` | `SKIP` / `TAKE` |
| Set operation keys | Implicit (all columns) | Explicit key columns required: `UNION (col1)` |
| Not-equal operator | Both `<>` and `!=` | Only `<>` is supported — `!=` is rejected with a helpful error suggesting `<>` |
| CASE WHEN | ELSE is optional | ELSE is **mandatory** |
| FROM-first syntax | Not standard | Supported: `FROM ... WHERE ... SELECT ...` |
| CROSS APPLY / OUTER APPLY | T-SQL extension | Fully supported with method and property expansion |
| Recursive CTEs | Supported in many dialects | **Not supported** |
| Subqueries in FROM | Supported | **Not supported** — use CTEs instead |
| `BETWEEN` operator | Supported | Supported — `x BETWEEN a AND b` is equivalent to `x >= a AND x <= b` |
| `ORDER BY` position | `ORDER BY 1` | **Not supported** — use column names or expressions |

### 1.4 Terminology

| Term | Definition |
|------|------------|
| **Schema** | A named data source provider (e.g., `#git`, `#os`, `#csv`) that exposes one or more methods |
| **Method** | A specific data-producing function on a schema (e.g., `#git.log()`, `#os.files('/path')`) |
| **Entity** | A single row returned by a data source method |
| **Column** | A named, typed field within an entity |
| **Expression** | Any computation that produces a value (arithmetic, function call, column reference, etc.) |
| **CTE** | Common Table Expression — a named temporary result set defined with `WITH ... AS (...)` |
| **Apply** | A correlated join where the right side can reference columns from the left side |

---

## 2. Lexical Elements

### 2.1 Character Set

Musoq supports full Unicode text in string literals, column values, and identifiers. Keywords are ASCII-only.

### 2.2 Keywords

All keywords are **case-insensitive**. `SELECT`, `select`, and `SeLeCt` are all equivalent.

#### Single-Word Keywords

| Keyword | Purpose |
|---------|---------|
| `SELECT` | Begin column selection |
| `FROM` | Specify data source |
| `WHERE` | Row filter condition |
| `AND` | Logical conjunction |
| `OR` | Logical disjunction |
| `NOT` | Logical negation |
| `AS` | Alias assignment |
| `IS` | NULL check (`IS NULL`, `IS NOT NULL`) |
| `NULL` | Null literal |
| `IN` | Set membership test |
| `LIKE` | Pattern matching (SQL wildcards) |
| `RLIKE` | Regular expression matching |
| `HAVING` | Post-aggregation filter |
| `CONTAINS` | Value-in-list check |
| `UNION` | Set union |
| `EXCEPT` | Set difference |
| `INTERSECT` | Set intersection |
| `SKIP` | Skip N rows (offset) |
| `TAKE` | Take N rows (limit) |
| `WITH` | Begin CTE definition |
| `ON` | Join condition |
| `FUNCTIONS` | Used in `DESC FUNCTIONS` |
| `TRUE` | Boolean true literal |
| `FALSE` | Boolean false literal |
| `TABLE` | Define a table structure |
| `COUPLE` | Bind a schema method to a table |
| `CASE` | Begin conditional expression |
| `WHEN` | Conditional branch |
| `THEN` | Branch result |
| `ELSE` | Default branch (mandatory in CASE) |
| `END` | End conditional expression |
| `DISTINCT` | Remove duplicate rows |
| `ASC` | Ascending sort order (default) |
| `DESC` | Descending sort order / Describe schema |

#### Multi-Word Keywords

| Keyword | Purpose |
|---------|---------|
| `NOT IN` | Negated set membership |
| `NOT LIKE` | Negated pattern matching |
| `NOT RLIKE` | Negated regex matching |
| `UNION ALL` | Set union preserving duplicates |
| `GROUP BY` | Grouping for aggregation |
| `ORDER BY` | Result ordering |
| `INNER JOIN` or `JOIN` | Inner join (equivalent forms) |
| `LEFT OUTER JOIN` or `LEFT JOIN` | Left outer join |
| `RIGHT OUTER JOIN` or `RIGHT JOIN` | Right outer join |
| `CROSS APPLY` | Correlated cross join |
| `OUTER APPLY` | Correlated outer join |

### 2.3 Identifiers

**Column names and method names are case-sensitive.** `Name`, `name`, and `NAME` reference different columns.

**Bracket-quoted identifiers** allow reserved words and special characters:

```sql
select [case], [order], [Column With Spaces] from #schema.method()
```

The `#` prefix identifies a schema data source. It is **optional** — `#A.Entities()` and `A.Entities()` are equivalent.

### 2.4 Comments

```sql
-- This is a line comment (everything after -- to end of line)

/* This is a
   block comment
   spanning multiple lines */
```

### 2.5 String Literals

String literals are enclosed in single quotes:

```sql
select 'Hello, World!' from #system.dual()
```

#### Escape Sequences

| Sequence | Character | Description |
|----------|-----------|-------------|
| `\\` | `\` | Backslash |
| `\'` | `'` | Single quote |
| `\"` | `"` | Double quote |
| `\n` | U+000A | Newline (LF) |
| `\r` | U+000D | Carriage return (CR) |
| `\t` | U+0009 | Horizontal tab |
| `\b` | U+0008 | Backspace |
| `\f` | U+000C | Form feed |
| `\e` | U+001B | Escape (ESC) |
| `\0` | U+0000 | Null character |
| `\uXXXX` | U+XXXX | Unicode code point (exactly 4 hex digits) |
| `\xXX` | — | Hex byte value (exactly 2 hex digits) |

**Rules:**
- `\uXXXX` requires exactly 4 hex digits. If fewer are available, the sequence is preserved literally: `'\u123'` → `\u123`
- `\xXX` requires exactly 2 hex digits. If fewer are available, preserved literally.
- Unknown escape sequences are preserved literally: `'\z'` → `\z`
- Double quotes can appear unescaped inside single-quoted strings: `select '"' from ...` is valid.

**Examples:**

```sql
select '\\' from #system.dual()                    -- result: \
select '\'' from #system.dual()                     -- result: '
select '\n' from #system.dual()                     -- result: (newline)
select '\u0041' from #system.dual()                 -- result: A
select '\x41' from #system.dual()                   -- result: A
select 'Hello\nWorld\t\u0394\\test' from #system.dual()  -- result: Hello(LF)World(TAB)Δ\test
select '\0\b\f\e' from #system.dual()              -- result: (null)(backspace)(formfeed)(ESC)
```

Special characters are valid inside string literals — all punctuation, braces, brackets, etc.:

```sql
select '{', '}', '[', ']', '(', ')' from #system.dual()
```

### 2.6 Numeric Literals

#### Integer Literals

Bare integers default to `int` (32-bit signed integer):

```sql
select 42 from #system.dual()        -- type: int
select -42 from #system.dual()       -- type: int (negative)
```

#### Decimal Literals

Numbers with a decimal point are `decimal`:

```sql
select 3.14 from #system.dual()      -- type: decimal
select -1.5 from #system.dual()      -- type: decimal
select .5 from #system.dual()        -- type: decimal (leading dot)
```

#### Numeric Type Suffixes

Append a suffix to force a specific numeric type:

| Suffix | Type | .NET Type | Range |
|--------|------|-----------|-------|
| `b` | signed byte | `sbyte` | -128 to 127 |
| `ub` | unsigned byte | `byte` | 0 to 255 |
| `s` | short | `short` | -32,768 to 32,767 |
| `us` | unsigned short | `ushort` | 0 to 65,535 |
| `i` | int | `int` | -2,147,483,648 to 2,147,483,647 |
| `ui` | unsigned int | `uint` | 0 to 4,294,967,295 |
| `l` | long | `long` | -9,223,372,036,854,775,808 to 9,223,372,036,854,775,807 |
| `ul` | unsigned long | `ulong` | 0 to 18,446,744,073,709,551,615 |
| `d` or `D` | decimal | `decimal` | ±1.0 × 10⁻²⁸ to ±7.9228 × 10²⁸ |

**Examples:**

```sql
select 1b from #system.dual()        -- sbyte
select 255ub from #system.dual()     -- byte
select 1000s from #system.dual()     -- short
select 65535us from #system.dual()   -- ushort
select 42i from #system.dual()       -- int (explicit)
select 100ui from #system.dual()     -- uint
select 1l from #system.dual()        -- long
select 1ul from #system.dual()       -- ulong
select 42d from #system.dual()       -- decimal (integer forced to decimal)
select 1.0 from #system.dual()       -- decimal (implicit from decimal point)
```

#### Alternative Number Bases

| Prefix | Base | Example | Result Type |
|--------|------|---------|-------------|
| `0x` or `0X` | Hexadecimal | `0xFF` → 255 | `long` |
| `0b` or `0B` | Binary | `0b1010` → 10 | `long` |
| `0o` or `0O` | Octal | `0o77` → 63 | `long` |

Prefixes are case-insensitive: `0xFF` and `0XFF` are equivalent.

**Examples:**

```sql
select 0xFF from #system.dual()                    -- 255 (long)
select 0b1010 from #system.dual()                  -- 10 (long)
select 0o77 from #system.dual()                    -- 63 (long)
select 0xFF + 0b1010 + 0o77 + 42 from #system.dual()  -- 370 (mixed arithmetic)
select 0x0 from #system.dual()                     -- 0 (zero values valid)
```

### 2.7 Boolean Literals

```sql
select true from #system.dual()
select false from #system.dual()
```

### 2.8 NULL Literal

```sql
select null from #system.dual()
```

### 2.9 Operators

#### Arithmetic Operators (by precedence, highest first)

| Precedence | Operator | Description | Example |
|------------|----------|-------------|---------|
| 4 | `.` | Member access | `a.Name` |
| 3 | `*` | Multiplication | `2 * 3` → 6 |
| 3 | `/` | Division | `10 / 3` → 3 |
| 3 | `%` | Modulo | `10 % 3` → 1 |
| 2 | `+` | Addition / String concatenation | `1 + 2` → 3, `'a' + 'b'` → `'ab'` |
| 2 | `-` | Subtraction | `5 - 3` → 2 |
| 1 | `<<` | Left bit shift | `1 << 3` → 8 |
| 1 | `>>` | Right bit shift | `8 >> 2` → 2 |
| 0 | `&` | Bitwise AND | `0xFF & 0x0F` → 15 |
| 0 | `\|` | Bitwise OR | `0x0F \| 0xF0` → 255 |
| 0 | `^` | Bitwise XOR | `0xFF ^ 0x0F` → 240 |

All arithmetic operators are **left-associative**. Use parentheses to override precedence:

```sql
select 256 + 256 / 2 from #system.dual()       -- 384 (division first)
select (256 + 256) / 2 from #system.dual()      -- 256 (parentheses override)
select 2 * 3 / 2 from #system.dual()            -- 3
select 1 + 2 * 3 * (7 * 8) - (45 - 10) from #system.dual()  -- 302
```

**Unary minus** is supported:

```sql
select 1 - -1 from #system.dual()               -- 2
select 1 - -(1 + 2) from #system.dual()         -- 4
select 1 + (-2) from #system.dual()             -- -1
```

#### Comparison Operators

| Operator | Description |
|----------|-------------|
| `=` | Equal |
| `<>` | Not equal |
| `>` | Greater than |
| `>=` | Greater than or equal |
| `<` | Less than |
| `<=` | Less than or equal |

> **Note**: Only `<>` is supported for not-equal comparison. The `!=` operator is **not** supported and will produce a clear error directing the user to use `<>` instead.

#### Logical Operators

| Operator | Description |
|----------|-------------|
| `AND` | Logical conjunction (both conditions must be true) |
| `OR` | Logical disjunction (at least one condition must be true) |
| `NOT` | Logical negation |

#### Pattern and Set Operators

| Operator | Description |
|----------|-------------|
| `LIKE` | SQL wildcard pattern matching (case-insensitive) |
| `NOT LIKE` | Negated SQL wildcard pattern matching |
| `RLIKE` | Regular expression matching |
| `NOT RLIKE` | Negated regular expression matching |
| `IN` | Set membership test |
| `NOT IN` | Negated set membership test |
| `IS NULL` | Tests for NULL |
| `IS NOT NULL` | Tests for non-NULL |
| `CONTAINS` | Tests if a value is in a literal list |

#### String Concatenation

The `+` operator concatenates strings when both operands are strings:

```sql
select 'Hello' + ' ' + 'World' from #system.dual()   -- 'Hello World'
select Concat(Name, ' - ', City) from #schema.method()  -- alternative
```

---

## 3. Data Types

### 3.1 Primitive Types

| Type | Description | Default Value | Example Literal |
|------|-------------|---------------|-----------------|
| `bool` | Boolean | `false` | `true`, `false` |
| `byte` | Unsigned 8-bit integer | `0` | `255ub` |
| `sbyte` | Signed 8-bit integer | `0` | `127b` |
| `short` | Signed 16-bit integer | `0` | `1000s` |
| `ushort` | Unsigned 16-bit integer | `0` | `65535us` |
| `int` | Signed 32-bit integer | `0` | `42`, `42i` |
| `uint` | Unsigned 32-bit integer | `0` | `100ui` |
| `long` | Signed 64-bit integer | `0` | `1l`, `0xFF` |
| `ulong` | Unsigned 64-bit integer | `0` | `1ul` |
| `float` | 32-bit floating point | `0.0` | — |
| `double` | 64-bit floating point | `0.0` | — |
| `decimal` | 128-bit precise decimal | `0.0` | `3.14`, `42d` |
| `char` | Single Unicode character | `'\0'` | — |
| `string` | Unicode text | `null` | `'Hello'` |

### 3.2 Date and Time Types

| Type | Description | Example String Format |
|------|-------------|----------------------|
| `DateTime` | Date and time (no timezone) | `'2023-03-15'` |
| `DateTimeOffset` | Date and time with timezone | `'2023-03-15T12:00:00+00:00'` |
| `TimeSpan` | Duration / time interval | `'02:30:00'` |

Date/time types have no literal syntax — they are produced by data sources or conversion functions. However, when compared against string literals, automatic parsing occurs (see §21).

### 3.3 Nullable Types

Any type can be nullable. When a column is nullable, its values may be `null`. Value types from `OUTER APPLY` and `LEFT JOIN` operations are automatically promoted to nullable when the right side produces no match:

```sql
-- If outer apply produces no match, b.Population becomes decimal? (nullable)
select a.Name, b.Population
from #A.entities() a
outer apply #B.entities(a.Country) b
```

### 3.4 Complex Object Types

Data sources may expose columns containing complex objects with nested properties. Access nested values with dot notation:

```sql
select Self.Name from #A.entities()              -- one level deep
select Self.Self.Name from #A.entities()          -- two levels deep
select Self.Dictionary['key'] from #A.entities()  -- dictionary access
select Self.Array[2] from #A.entities()           -- array indexing
```

### 3.5 Collections and Arrays

Columns may hold arrays (`T[]`) or enumerables (`IEnumerable<T>`). These can be:
- Indexed with `[N]` syntax (see §20)
- Expanded into rows via `CROSS APPLY` (see §9)
- Processed with collection functions (`Length`, `Skip`, `Take`, `FirstOrDefault`, etc.)

### 3.6 Type Inference for Literals

| Literal Form | Inferred Type |
|-------------|---------------|
| `42` | `int` |
| `42d` | `decimal` |
| `3.14` | `decimal` |
| `.5` | `decimal` |
| `0xFF` | `long` |
| `0b1010` | `long` |
| `0o77` | `long` |
| `true` / `false` | `bool` |
| `null` | `object` (contextual) |
| `'text'` | `string` |

---

## 4. Statement Structure

### 4.1 Statement Termination

Statements are optionally terminated with `;`. Multiple statements in a batch are separated by `;`:

```sql
table MyTable { Name string };
couple #A.Entities with table MyTable as Source;
select Name from Source();
```

### 4.2 Statement Types

| Statement | Starting Keyword | Purpose |
|-----------|-----------------|---------|
| **SELECT query** | `SELECT` | Query data from sources |
| **Reordered query** | `FROM` | Query with FROM-first syntax |
| **CTE expression** | `WITH` | Define named temporary result sets |
| **Table definition** | `TABLE` | Define a typed table structure |
| **Couple** | `COUPLE` | Bind a schema method to a table definition |
| **Describe** | `DESC` | Introspect schema metadata |

---

## 5. SELECT Clause

### 5.1 Basic Syntax

```sql
SELECT [DISTINCT] expression [[AS] alias], ...
```

### 5.2 Column Expressions

Any expression can appear in SELECT:

```sql
select 1 from #system.dual()                          -- literal
select Name from #A.entities()                         -- column reference
select a.Name from #A.entities() a                     -- qualified column
select 1 + 2 * 3 from #system.dual()                  -- arithmetic
select Concat(City, ', ', Country) from #A.entities()  -- function call
select a.GetPopulation() from #A.entities() a          -- method on entity
select Self.Name from #A.entities()                    -- property access
select Self.Array[2] from #A.entities()                -- indexed access
```

### 5.3 Column Aliasing

Three equivalent forms:

```sql
select Name as FullName from #A.entities()       -- explicit AS keyword
select Name FullName from #A.entities()           -- implicit (space-separated)
select Name [Full Name] from #A.entities()        -- bracketed (allows spaces)
```

When no alias is given, the column name is derived from the expression:
- Column reference: `Name` → column name `Name`
- Function call: `Count(Name)` → column name `Count(Name)`
- Literal: `1` → column name `1`

> **Important**: SELECT aliases **cannot** be referenced in WHERE or GROUP BY clauses. Doing so produces an `UnknownColumnOrAliasException`.

```sql
-- ERROR: SELECT alias cannot be used in WHERE
select Name as FileName from #A.entities() where FileName = 'test'

-- ERROR: SELECT alias cannot be used in GROUP BY
select Length(Name) as NameLen from #A.entities() group by NameLen
```

### 5.4 Star Expression (Wildcard)

`*` expands to all columns from the data source:

```sql
select * from #A.entities()                              -- all columns
select *, Name as Name2 from #A.entities() a             -- star + explicit columns
select *, * from #A.entities() a                          -- duplicated columns
```

Qualified star selects columns from a specific table in a join:

```sql
select a.* from #A.entities() a inner join #B.entities() b on a.Id = b.Id
select a.*, b.* from #A.entities() a inner join #B.entities() b on a.Id = b.Id
```

Star works through CTEs:

```sql
with p as (select City, Country from #A.entities())
select * from p       -- expands to City, Country
```

### 5.5 DISTINCT

`SELECT DISTINCT` removes duplicate rows from the result:

```sql
select distinct Country from #A.entities()
select distinct City, Country from #A.entities()   -- unique combinations
```

DISTINCT uses **ordinal (case-sensitive) comparison** — `'POLAND'` and `'poland'` are treated as different values. To achieve case-insensitive deduplication, use `ToLower()`:

```sql
select distinct ToLower(Country) from #A.entities()
```

### 5.6 RowNumber

`RowNumber()` returns a 1-based sequential integer for each row in the result:

```sql
select RowNumber(), Name from #A.entities()
```

`RowNumber()` is assigned **after** ORDER BY but **before** SKIP/TAKE. When used with ORDER BY, rows are first sorted, then numbered sequentially:

```sql
select Country, RowNumber() from #A.entities() order by Country
-- Rows are sorted alphabetically, then numbered 1, 2, 3, ...
-- Germany → 1, Poland → 2 (sorted order determines numbering)
```

With SKIP, row numbers are assigned before SKIP is applied:

```sql
select Country, RowNumber() from #A.entities() order by Country skip 1
-- Full result: Germany=1, Poland=2; after SKIP 1: Poland=2 (number preserved)
```

With WHERE filtering, `RowNumber()` counts only the rows that pass the filter:

```sql
select Country, RowNumber() from #A.entities() where Country = 'Poland'
-- Returns rows numbered 1, 2, ... (only matching rows counted)
```

---

## 6. FROM Clause

### 6.1 Schema Data Sources

The primary data source syntax uses schema providers:

```sql
select * from #schema.method()
select * from #schema.method(arg1, arg2)
select * from #schema.method() alias
```

The `#` prefix is **optional** — both forms are equivalent:

```sql
select * from #A.Entities()    -- with hash
select * from A.Entities()     -- without hash (equivalent)
```

Arguments can be literals of any type:

```sql
select * from #test.whatever(1, 2d, true, false, 'text')
```

### 6.2 Table Aliasing

Data sources can be given an alias for reference in expressions:

```sql
select a.Name from #A.entities() a
select entities.Name from #A.entities() entities
```

In **single-table queries**, aliasing is optional — column names can be used directly:

```sql
select Name from #A.entities()         -- no alias needed
select a.Name from #A.entities() a     -- alias optional
```

In **multi-table queries** (joins, applies), aliases are required to disambiguate columns:

```sql
select a.Name, b.City
from #A.entities() a
inner join #B.entities() b on a.Id = b.Id
```

### 6.3 CTE References

After a CTE is defined, use its name as a data source:

```sql
with cte as (select City, Country from #A.entities())
select * from cte
```

CTE references can be aliased:

```sql
with cte as (select City from #A.entities())
select p.City from cte p
```

### 6.4 Coupled Table References

After a `COUPLE` statement, the coupled alias becomes a data source:

```sql
table MyTable { Name string };
couple #A.Entities with table MyTable as Source;
select Name from Source()
select Name from Source(true, 'param')   -- with arguments
```

---

## 7. WHERE Clause

### 7.1 Basic Syntax

```sql
SELECT ... FROM ... WHERE condition
```

The WHERE clause filters rows before any grouping or aggregation.

### 7.2 Comparison Expressions

```sql
where Population > 500
where City = 'WARSAW'
where Population >= 100 and Population <= 500
where Name <> 'Unknown'
```

### 7.3 Logical Operators

```sql
where Country = 'POLAND' and Population > 300
where City = 'WARSAW' or City = 'BERLIN'
where not (Country = 'GERMANY')
```

### 7.4 IS NULL / IS NOT NULL

```sql
where NullableValue is null
where Country is not null
where NullableValue is not null and NullableValue <> 5
```

### 7.5 LIKE Pattern Matching

`LIKE` performs **case-insensitive** pattern matching with SQL wildcards:

| Wildcard | Meaning |
|----------|---------|
| `%` | Matches zero or more characters |
| `_` | Matches exactly one character |

```sql
where Name like '%test%'        -- contains 'test' (case-insensitive)
where Name like 'ABC%'          -- starts with 'ABC'
where Name like '%XYZ'          -- ends with 'XYZ'
where Name like 'tes_'          -- 'test', 'tess', etc.
where Name not like '%test%'    -- does not contain 'test'
```

LIKE supports full Unicode including Polish, Russian, Japanese, Arabic, and other scripts:

```sql
where Name like '%żółć%'        -- Polish characters
where Name like '%привет%'      -- Cyrillic
```

### 7.6 RLIKE (Regular Expression Matching)

`RLIKE` matches against a .NET regular expression:

```sql
where Name rlike '^\d+'              -- starts with digits
where Email rlike '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'
where Name not rlike '^test.*$'      -- does not match pattern
```

> **Note**: Invalid regex patterns cause a runtime error when the query executes.

### 7.7 IN / NOT IN

Tests membership in a set of values:

```sql
where Population in (100, 200, 300, 400)
where City in ('WARSAW', 'BERLIN', 'MUNICH')
where City in (Country, 'Warsaw')          -- can mix column references and literals
where Population not in (100, 400)
```

### 7.8 CONTAINS

Tests if a value matches any item in a literal list:

```sql
where Name contains ('ABC', 'CDA', 'EFG')
```

**NULL handling in CONTAINS:**
- `CONTAINS(null, 'a', 'b', 'c')` → `false` (null not in list)
- `CONTAINS(null, 'a', null, 'c')` → `true` (null explicitly in list)
- When the list itself cannot be constructed (null array), returns `false`.

### 7.9 Implicit Boolean Conversion

Functions returning `bool` or `bool?` can be used directly in WHERE without `= true`:

```sql
-- These are equivalent:
where Match('\d+', Name) = true
where Match('\d+', Name)

-- Also works in CASE WHEN:
select case when Match('\d+', Name) then 'yes' else 'no' end from #A.entities()
```

---

## 8. JOIN Clause

### 8.1 INNER JOIN

Returns only rows where the join condition matches in both tables:

```sql
select a.City, b.Population
from #A.entities() a
inner join #B.entities() b on a.City = b.City
```

`JOIN` without the `INNER` keyword is equivalent to `INNER JOIN`:

```sql
from #A.entities() a join #B.entities() b on a.City = b.City
```

### 8.2 LEFT OUTER JOIN

Returns all rows from the left table. Unmatched right-side columns are `null`:

```sql
select a.City, b.Population
from #A.entities() a
left join #B.entities() b on a.City = b.City
```

`LEFT OUTER JOIN` and `LEFT JOIN` are equivalent.

### 8.3 RIGHT OUTER JOIN

Returns all rows from the right table. Unmatched left-side columns are `null`:

```sql
select a.City, b.Population
from #A.entities() a
right join #B.entities() b on a.City = b.City
```

### 8.4 Cross Join

Produce a Cartesian product using a tautological condition:

```sql
select a.Name, b.Name
from #A.entities() a
inner join #B.entities() b on 1 = 1
```

### 8.5 Multiple Joins

Chain multiple joins in a single query:

```sql
select a.City, b.Population, c.Area
from #A.entities() a
inner join #B.entities() b on a.City = b.City
inner join #C.entities() c on a.City = c.City
```

### 8.6 Join Condition Expressions

Join conditions can contain expressions, not just simple column equality:

```sql
inner join #B.entities() b on a.Id = b.Id + 1
inner join #B.entities() b on a.Population > b.Population + 100
```

### 8.7 Keywords Are Case-Insensitive

All forms are valid:

```sql
INNER JOIN ... ON ...
inner join ... on ...
Inner Join ... On ...
```

---

## 9. APPLY Clause

### 9.1 CROSS APPLY

`CROSS APPLY` is a correlated join where the right side can reference columns from the left side. Only rows with matches are returned:

```sql
select a.Country, b.City
from #A.entities() a
cross apply #B.entities(a.Country) b
```

The right-side data source receives values from the left side as method arguments.

### 9.2 OUTER APPLY

Like `CROSS APPLY`, but preserves left-side rows without matches. Unmatched right-side columns are `null`:

```sql
select a.Country, b.City
from #A.entities() a
outer apply #B.entities(a.Country) b
-- If no match for a.Country, b.City is null
```

Value-type columns from the right side are automatically promoted to nullable (e.g., `decimal` → `decimal?`).

### 9.3 Apply with Row Methods

Call a method on a row that produces a table of results:

```sql
-- Split a string into rows
select b.Value
from #schema.first() a
cross apply a.Split(a.Text, ' ') as b

-- Chain method results
select c.Value
from #schema.first() a
cross apply a.Split(a.Text, ' ') as b
cross apply b.ToCharArray(b.Value) as c
```

Nested method calls are supported:

```sql
-- Skip first element, take next 6
select b.Value
from #schema.first() a
cross apply a.Take(a.Skip(a.Split(a.Text, ' '), 1), 6) as b
```

### 9.4 Apply with Collection Properties

Expand a collection property (array, list) into rows:

```sql
-- Expand an array property
select a.City, b.Value
from #schema.first() a
cross apply a.Values as b

-- Expand nested collection
select d.Value
from #schema.first() a
cross apply a.Values as b
cross apply b.Values as c
cross apply c.Values as d

-- Expand through property chain
select b.Value
from #schema.first() a
cross apply a.ComplexType.PrimitiveValues as b
```

When expanding **primitive arrays** (`int[]`, `double[]`, `List<double>`), each element becomes a row with a `Value` column.

When expanding **complex type arrays** (`MyClass[]`), each property of the complex type is exposed as a column.

### 9.5 Chaining Applies

Multiple applies can be chained. Each can reference results from previous ones:

```sql
select b.Value, c.Value
from #schema.first() a
cross apply a.Split(a.Text, ' ') as b
cross apply b.ToCharArray(b.Value) as c

-- Apply results can be filtered and grouped
select b.Length(b.Value), b.Count(Length(b.Value))
from #schema.first() a
cross apply a.Split(a.Text, ' ') as b
group by b.Length(b.Value)
```

### 9.6 Multiple Independent Applies

When multiple applies reference the same source, they produce a Cartesian product:

```sql
select b.Value, c.Value
from #schema.first() a
cross apply a.Split(a.Numbers, ',') as b
cross apply a.Split(a.Words, ' ') as c
-- Every combination of b and c values
```

---

## 10. GROUP BY and Aggregation

### 10.1 Basic Syntax

```sql
SELECT group_column, aggregate_function(column) FROM ... GROUP BY group_column
```

### 10.2 Aggregate Functions

| Function | Return Type | Description |
|----------|-------------|-------------|
| `Count(column)` | `int` | Count non-null values in group |
| `Sum(column)` | `decimal` | Sum of values |
| `Avg(column)` | `decimal` | Average value (Sum / Count) |
| `Min(column)` | `decimal` | Minimum value |
| `Max(column)` | `decimal` | Maximum value |
| `StDev(column)` | `decimal` | Population standard deviation |
| `SumIncome(column)` | `decimal` | Sum of positive values only |
| `SumOutcome(column)` | `decimal` | Sum of negative values only |
| `AggregateValues(column)` | `string` | Comma-separated concatenation |
| `Window(column)` | `IEnumerable<T>` | All values in group as a collection |
| `MinDateTimeOffset(column)` | `DateTimeOffset?` | Minimum DateTimeOffset |
| `MaxDateTimeOffset(column)` | `DateTimeOffset?` | Maximum DateTimeOffset |
| `SumTimeSpan(column)` | `TimeSpan?` | Sum of TimeSpan values |
| `MinTimeSpan(column)` | `TimeSpan?` | Minimum TimeSpan |
| `MaxTimeSpan(column)` | `TimeSpan?` | Maximum TimeSpan |

All numeric aggregation functions (`Count`, `Sum`, `Avg`, `Min`, `Max`, `StDev`) accept any numeric type (`byte`, `short`, `int`, `long`, `float`, `double`, `decimal`, etc.) as input.

### 10.3 GROUP BY Examples

```sql
-- Basic grouping with count
select Country, Count(Country) from #A.entities() group by Country

-- Grouping with sum
select Country, Sum(Population) from #A.entities() group by Country

-- Multi-column grouping
select Country, City, Count(City) from #A.entities() group by Country, City

-- Grouping with expression
select Substr(Name, 0, 2), Count(Name)
from #A.entities()
group by Substr(Name, 0, 2)

-- CASE WHEN in GROUP BY
select case when Population >= 500 then 'big' else 'small' end, Count(City)
from #A.entities()
group by case when Population >= 500 then 'big' else 'small' end
```

### 10.4 Parent-Level Aggregation

Aggregate functions accept an optional second parameter indicating the "parent level" for multi-column grouping:

```sql
-- Group by Month, City but aggregate at Month level
select SumIncome(Money, 1), SumOutcome(Money, 1)
from #A.Entities()
group by Month, City
```

With `group by Month, City`:
- `Count(City)` or `Count(City, 0)` — count at the City (innermost) level
- `Count(City, 1)` — count at the Month (parent) level

### 10.5 Aggregation Without GROUP BY

Using aggregate functions without GROUP BY produces a single-row result aggregating all rows:

```sql
select Count(Name), Sum(Population) from #A.entities()
-- Returns one row with totals
```

### 10.6 GROUP BY with Constant

Grouping by a constant treats all rows as a single group:

```sql
select Count(Country) from #A.entities() group by 'fake'
-- Equivalent to aggregate without GROUP BY
```

### 10.7 HAVING Clause

HAVING filters groups after aggregation:

```sql
select Name, Count(Name) from #A.entities()
group by Name
having Count(Name) >= 2
```

HAVING can use any aggregate expression:

```sql
select City, Sum(Money) from #A.entities()
group by City
having Sum(Money) >= 400
```

### 10.8 Non-Aggregated Column Restrictions

In a SELECT with GROUP BY, every non-aggregated column must appear in the GROUP BY clause:

```sql
-- VALID: Name is in GROUP BY
select Name, Count(Name) from #A.entities() group by Name

-- VALID: Only aggregates, no non-aggregated columns
select Count(Country) from #A.entities() group by Country

-- ERROR: Name is not in GROUP BY
select Name, City, Count(1) from #A.entities() group by City
-- Throws NonAggregatedColumnInSelectException

-- ERROR: SELECT * with GROUP BY expands to non-aggregated columns
select * from #A.entities() group by City
-- Throws NonAggregatedColumnInSelectException
```

### 10.9 NULL in GROUP BY

`NULL` values form their own group:

```sql
-- If some Country values are null, null is its own group
select Country, Count(Country) from #A.entities() group by Country
-- Group: null, Count(Country): 0 (Count ignores nulls)
```

Multi-column grouping treats `(POLAND, null)` as distinct from `(POLAND, WARSAW)`:

```sql
select Country, City, Count(City) from #A.entities() group by Country, City
```

---

## 11. Set Operations

### 11.1 Syntax

Musoq requires explicit key columns for set operations:

```sql
query1 UNION (key_col1, key_col2) query2
query1 UNION ALL (key_col1) query2
query1 EXCEPT (key_col1) query2
query1 INTERSECT (key_col1) query2
```

The key columns specify which columns are used to determine row identity for deduplication, difference, or intersection.

### 11.2 UNION

Combines results from two queries, removing duplicates identified by the key columns:

```sql
select Name from #A.Entities()
union (Name)
select Name from #B.Entities()
```

### 11.3 UNION ALL

Combines results preserving all rows including duplicates:

```sql
select Name from #A.Entities()
union all (Name)
select Name from #B.Entities()
```

### 11.4 EXCEPT

Returns rows from the first query that do not appear in the second query:

```sql
select Name from #A.Entities()
except (Name)
select Name from #B.Entities()
```

### 11.5 INTERSECT

Returns only rows that appear in both queries:

```sql
select Name from #A.Entities()
intersect (Name)
select Name from #B.Entities()
```

### 11.6 Chaining Set Operations

Three or more queries can be chained:

```sql
select Name from #A.Entities()
union all (Name)
select Name from #A.Entities()
union all (Name)
select Name from #A.Entities()
union all (Name)
select Name from #A.Entities()
union all (Name)
select Name from #A.Entities()
```

### 11.7 Different Source Columns

Source columns can differ if aliases unify them:

```sql
select Name from #A.Entities()
union (Name)
select City as Name from #B.Entities()   -- City aliased as Name
```

### 11.8 SKIP/TAKE per Subquery

Each subquery in a set operation can have its own SKIP/TAKE:

```sql
select Name from #A.Entities() skip 1
union (Name)
select Name from #B.Entities() skip 2
```

### 11.9 Set Operations in CTEs

```sql
with p as (
    select 1 as Id, 'First' as Name from #A.Entities()
    union all (Name)
    select 2 as Id, 'Second' as Name from #A.Entities()
    union all (Name)
    select 3 as Id, 'Third' as Name from #A.Entities()
)
select Id, Name from p
```

---

## 12. ORDER BY, SKIP, TAKE

### 12.1 ORDER BY

Sorts result rows. Default direction is ascending:

```sql
select Name from #A.entities() order by Name            -- ascending (default)
select Name from #A.entities() order by Name asc        -- explicit ascending
select Name from #A.entities() order by Name desc       -- descending
```

Multi-column ordering with mixed directions:

```sql
select City, Population from #A.entities()
order by Population desc, City asc
```

ORDER BY can use expressions:

```sql
select Name, Population from #A.entities() order by Population * -1
```

ORDER BY uses **ordinal (case-sensitive) comparison** for strings: uppercase letters sort before lowercase (`'A'` < `'a'`).

### 12.2 SKIP

Skip the first N rows of the result:

```sql
select Name from #A.entities() skip 2
```

If SKIP exceeds the number of rows, zero rows are returned (no error).

### 12.3 TAKE

Take the first N rows of the result:

```sql
select Name from #A.entities() take 3
```

If TAKE exceeds the number of available rows, all available rows are returned (no error).

### 12.4 SKIP + TAKE (Pagination)

Combine for pagination:

```sql
select Name from #A.entities() order by Name skip 10 take 5
-- Skip first 10, return next 5
```

### 12.5 Interaction with GROUP BY and HAVING

ORDER BY, SKIP, and TAKE are applied after GROUP BY and HAVING:

```sql
select City, Sum(Money) from #A.entities()
group by City
having Sum(Money) >= 400
order by City
skip 1
take 2
```

---

## 13. Common Table Expressions (CTEs)

### 13.1 Basic Syntax

```sql
WITH cte_name AS (
    query
)
SELECT ... FROM cte_name
```

### 13.2 Simple CTE

```sql
with p as (
    select City, Country from #A.entities()
)
select Country, City from p
```

### 13.3 Star Expansion from CTE

```sql
with p as (
    select City, Country from #A.entities()
)
select * from p    -- expands to City, Country
```

### 13.4 CTE with Aggregation

```sql
with summary as (
    select Country, Sum(Population) from #A.entities() group by Country
)
select * from summary
```

Aggregation can also occur on a CTE reference:

```sql
with raw as (
    select Population, Country from #A.entities()
)
select Country, Sum(Population) from raw group by Country
```

### 13.5 Multiple CTEs

Define multiple CTEs separated by commas:

```sql
with
    cities as (select City, Country from #A.entities()),
    countries as (select distinct Country from #A.entities())
select * from cities
```

### 13.6 CTE with Set Operations

```sql
with combined as (
    select Name from #A.Entities()
    union (Name)
    select Name from #B.Entities()
)
select * from combined
```

### 13.7 CTE with JOIN

```sql
with p as (select City, Country from #A.entities())
select p.City, b.Population
from p
inner join #B.entities() b on p.City = b.City
```

### 13.8 Limitations

- **No recursive CTEs**: Musoq does not support `WITH RECURSIVE` or self-referencing CTEs.
- **Duplicate aliases**: Using the same alias for two tables within a CTE inner expression throws `AliasAlreadyUsedException`.

```sql
-- ERROR: Duplicate alias 'a'
with p as (
    select 1 from #A.entities() a inner join #A.entities() a on 1 = 1
)
select * from p
```

---

## 14. TABLE and COUPLE Statements

### 14.1 TABLE Definition

Defines a named table structure with typed columns:

```sql
table TableName {
    Column1 type1,
    Column2 type2,
    Column3 type3?       -- ? suffix for nullable
};
```

Supported type keywords:

| Type Keyword | Maps To |
|-------------|---------|
| `byte` | `byte?` |
| `sbyte` | `sbyte?` |
| `short` | `short?` |
| `int` | `int?` |
| `long` | `long?` |
| `ushort` | `ushort?` |
| `uint` | `uint?` |
| `ulong` | `ulong?` |
| `float` | `float?` |
| `double` | `double?` |
| `decimal` | `decimal?` |
| `money` | `decimal?` |
| `bool` | `bool?` |
| `boolean` | `bool?` |
| `bit` | `bool?` |
| `char` | `char?` |
| `string` | `string` |
| `datetime` | `DateTime?` |
| `datetimeoffset` | `DateTimeOffset?` |
| `timespan` | `TimeSpan?` |
| `guid` | `Guid?` |
| `object` | `object` |

**Example:**

```sql
table Invoice {
    ProductName string,
    Price decimal,
    Date datetimeoffset?
};
```

### 14.2 COUPLE Statement

Binds a schema method to a table structure, creating a new data source alias:

```sql
couple #schema.Method with table TableName as AliasName;
```

**Complete Example:**

```sql
table DummyTable {
    Name string
};
couple #A.Entities with table DummyTable as SourceOfDummyRows;
select Name from SourceOfDummyRows();
```

With parameters:

```sql
select Name from SourceOfDummyRows(true, 'filter');
```

### 14.3 Purpose

TABLE and COUPLE are used when:
- The data source returns untyped or dynamically-typed rows
- You want to project a subset of columns with explicit types
- You need to create a named alias for a complex data source expression

---

## 15. DESC Statement

### 15.1 Describe a Schema

List available methods on a schema:

```sql
desc #A
```

Returns a single-column table with method names (e.g., `empty`, `entities`).

### 15.2 Describe a Method (Constructors)

```sql
desc #A.entities
```

Returns the method name.

### 15.3 Describe Table Columns

List columns exposed by a method, including name, index, and type:

```sql
desc #A.entities()
```

Returns a table with columns: `Name`, `Index`, `Type`.

Arguments can be passed to describe methods with specific parameters:

```sql
desc #dynamic.method(0, 'test', 10.5d)
```

### 15.4 Describe a Specific Column

Inspect a specific column's type structure, useful for complex or nested types:

```sql
desc #A.entities() column Array             -- describe an array column
desc #A.entities() column Children           -- describe a complex type column
desc #A.entities() column Self.Children      -- nested property path
desc #A.entities() column Self.Other.Children  -- deep nested path
```

Column name matching for DESC is **case-insensitive**.

### 15.5 Describe Schema Functions

```sql
desc functions #A
desc functions #A.entities
desc functions #A.entities()
desc functions #A.entities('filter')    -- with arguments
```

Lists all functions available when querying the schema. Accepts optional method and argument context.

---

## 16. Reordered Query Syntax

### 16.1 FROM-First Syntax

Musoq supports an alternative query ordering where FROM appears first:

```sql
FROM source
[JOIN/APPLY ...]
[WHERE condition]
[GROUP BY columns]
SELECT columns
[ORDER BY columns]
[SKIP n]
[TAKE m]
```

### 16.2 Standard vs. Reordered Clause Order

| Position | Standard Query | Reordered Query |
|----------|---------------|-----------------|
| 1 | `SELECT` | `FROM` |
| 2 | `FROM` | `JOIN/APPLY` |
| 3 | `JOIN/APPLY` | `WHERE` |
| 4 | `WHERE` | `GROUP BY` |
| 5 | `GROUP BY` | `SELECT` |
| 6 | `ORDER BY` | `ORDER BY` |
| 7 | `SKIP` | `SKIP` |
| 8 | `TAKE` | `TAKE` |

### 16.3 Examples

```sql
-- Simple
from #A.Entities() select City, Country

-- With WHERE
from #A.Entities() where Country = 'POLAND' select City, Country

-- With GROUP BY
from #A.Entities() group by Country select Country, Sum(Population)

-- Full combination
from #A.Entities() a
inner join #B.Entities() b on a.City = b.City
where a.Country = 'POLAND'
group by a.Country
select a.Country, Sum(b.Population)
order by a.Country
skip 1
take 5

-- Inside a CTE
with cte as (
    from #A.Entities() where Country = 'POLAND' select City, Country
)
select * from cte
```

---

## 17. Built-in Functions Reference

### 17.1 Conventions

- Most functions return `null` when any required parameter is `null` (NULL propagation).
- Function names are **case-sensitive**.
- Functions can be called standalone or with table alias prefix: `Length(Name)` or `a.Length(a.Name)`.

### 17.2 String Functions

| Function | Signature | Description |
|----------|-----------|-------------|
| `Trim` | `(string?) → string?` | Remove leading/trailing whitespace |
| `TrimStart` | `(string?) → string?` | Remove leading whitespace |
| `TrimEnd` | `(string?) → string?` | Remove trailing whitespace |
| `Substring` | `(string?, int, int?) → string?` | Extract substring (index, length) |
| `Substr` | `(string?, int, int?) → string?` | Alias for `Substring` |
| `Concat` | `(params string?[]) → string?` | Concatenate multiple strings |
| `Contains` | `(string?, string?) → bool?` | Case-insensitive substring check |
| `IndexOf` | `(string?, string?) → int?` | First occurrence index (case-insensitive) |
| `NthIndexOf` | `(string?, string?, int) → int?` | Nth occurrence index |
| `LastIndexOf` | `(string?, string?) → int?` | Last occurrence index |
| `ToUpper` | `(string?) → string?` | Uppercase (invariant culture) |
| `ToUpperInvariant` | `(string?) → string?` | Uppercase (invariant) |
| `ToLower` | `(string?) → string?` | Lowercase (invariant culture) |
| `ToLowerInvariant` | `(string?) → string?` | Lowercase (invariant) |
| `Replace` | `(string?, string, string?) → string?` | Replace all occurrences (case-insensitive) |
| `Reverse` | `(string?) → string?` | Reverse string |
| `PadLeft` | `(string?, int, char?) → string?` | Left-pad to width |
| `PadRight` | `(string?, int, char?) → string?` | Right-pad to width |
| `Head` | `(string?, int?) → string?` | First N characters (default 10) |
| `Tail` | `(string?, int?) → string?` | Last N characters (default 10) |
| `Split` | `(string?, params string[]) → string[]` | Split by separators |
| `StartsWith` | `(string?, string?) → bool?` | Case-insensitive prefix check |
| `EndsWith` | `(string?, string?) → bool?` | Case-insensitive suffix check |
| `IsNullOrEmpty` | `(string?) → bool` | Null or empty check |
| `IsNullOrWhiteSpace` | `(string?) → bool` | Null or whitespace check |
| `IsNumeric` | `(string?) → bool?` | Only digits |
| `IsAlpha` | `(string?) → bool?` | Only letters |
| `IsAlphaNumeric` | `(string?) → bool?` | Letters and digits |
| `Replicate` | `(string, int) → string` | Repeat string N times |
| `Repeat` | `(string?, int, string?) → string?` | Repeat with separator |
| `Translate` | `(string?, string?, string?) → string?` | Character-by-character translation |
| `ToTitleCase` | `(string?) → string?` | Title case formatting |
| `Capitalize` | `(string?) → string?` | Capitalize first character |
| `GetNthWord` | `(string?, int, string?) → string?` | Nth word in string |
| `GetFirstWord` | `(string, string) → string?` | First word |
| `GetLastWord` | `(string?, string?) → string?` | Last word |
| `WordCount` | `(string?) → int?` | Count words |
| `LineCount` | `(string?) → int?` | Count lines |
| `SentenceCount` | `(string?) → int?` | Count sentences |
| `CountOccurrences` | `(string?, string?) → int?` | Count substring occurrences |
| `RemoveWhitespace` | `(string?) → string?` | Strip all whitespace |
| `Truncate` | `(string?, int, string?) → string?` | Truncate with ellipsis |
| `Wrap` | `(string?, string?, string?) → string?` | Wrap with prefix/suffix |
| `RemovePrefix` | `(string?, string?) → string?` | Remove leading prefix |
| `RemoveSuffix` | `(string?, string?) → string?` | Remove trailing suffix |
| `ToSnakeCase` | `(string?) → string?` | Convert to snake_case |
| `ToKebabCase` | `(string?) → string?` | Convert to kebab-case |
| `ToCamelCase` | `(string?) → string?` | Convert to camelCase |
| `ToPascalCase` | `(string?) → string?` | Convert to PascalCase |
| `LevenshteinDistance` | `(string?, string?) → int?` | Edit distance |
| `LongestCommonSubstring` | `(string, string) → string?` | Longest common substring |
| `Soundex` | `(string?) → string?` | Phonetic code |
| `HasFuzzyMatchedWord` | `(string, string, string?) → bool` | Fuzzy word match |
| `HasWordThatSoundLike` | `(string, string, string?) → bool` | Sound-alike word match |
| `RemoveDiacritics` | `(string?) → string?` | Remove accents/diacritics |
| `RegexReplace` | `(string?, string?, string?) → string?` | Regex-based replace |
| `RegexMatches` | `(string?, string?) → string[]?` | All regex matches |
| `RegexExtract` | `(string?, string?, int?) → string?` | Extract regex capture group |
| `RegexExtractAll` | `(string?, string?, int?) → string[]` | All matches of capture group |
| `IsMatch` | `(string?, string?) → bool?` | Regex match check |
| `Match` | `(string?, string?) → bool?` | Regex match check |
| `UrlEncode` | `(string?) → string?` | URL-encode string |
| `UrlDecode` | `(string?) → string?` | URL-decode string |
| `HtmlEncode` | `(string?) → string?` | HTML-encode string |
| `HtmlDecode` | `(string?) → string?` | HTML-decode string |
| `ExtractBetween` | `(string?, string?, string?) → string?` | Text between delimiters |
| `ExtractBetweenAll` | `(string?, string?, string?) → string[]` | All texts between delimiters |
| `ExtractAfter` | `(string?, string?, bool?) → string?` | Text after delimiter |
| `ExtractBefore` | `(string?, string?, bool?) → string?` | Text before delimiter |
| `SplitAndTake` | `(string?, string?, int) → string?` | Split and get element by index |
| `StringsJoin` | `(string?, params string?[]) → string?` | Join with separator |
| `NewId` | `() → string` | Generate new GUID string |
| `Diff` | `(string?, string?, string?) → string?` | Character-level text diff |
| `Rot13` | `(string?) → string?` | ROT13 cipher |
| `Rot47` | `(string?) → string?` | ROT47 cipher |
| `ToMorse` | `(string?) → string?` | Convert to Morse code |
| `FromMorse` | `(string?) → string?` | Convert from Morse code |
| `ToBinaryString` | `(string?) → string?` | Binary representation |
| `FromBinaryString` | `(string?) → string?` | Parse binary representation |
| `ToUnicodeEscape` | `(string?) → string?` | Unicode escape sequences |
| `FromUnicodeEscape` | `(string?) → string?` | Parse Unicode escapes |

### 17.3 Math Functions

| Function | Signature | Description |
|----------|-----------|-------------|
| `Abs` | `(numeric?) → numeric?` | Absolute value |
| `Ceil` | `(decimal?) → decimal?` | Ceiling |
| `Floor` | `(decimal?) → decimal?` | Floor |
| `Round` | `(decimal?, int) → decimal?` | Round with precision |
| `Sign` | `(numeric?) → numeric?` | Sign (+1, 0, -1) |
| `Pow` | `(numeric?, numeric?) → double?` | Power |
| `Sqrt` | `(numeric?) → double?` | Square root |
| `Log` | `(numeric?, numeric?) → double?` | Logarithm with base |
| `Log10` | `(double?) → double?` | Base-10 logarithm |
| `Log2` | `(double?) → double?` | Base-2 logarithm |
| `Ln` | `(numeric?) → numeric?` | Natural logarithm |
| `Sin` | `(numeric?) → numeric?` | Sine |
| `Cos` | `(numeric?) → numeric?` | Cosine |
| `Tan` | `(numeric?) → numeric?` | Tangent |
| `Exp` | `(numeric?) → numeric?` | e^x |
| `PercentOf` | `(decimal?, decimal?) → decimal?` | Percentage calculation |
| `PercentRank` | `(IEnumerable<T>?, T?) → double?` | Percent rank in window |
| `Rand` | `() → int` | Random number |
| `Rand` | `(int?, int?) → int?` | Random in range |
| `Clamp` | `(T?, T?, T?) → T?` | Clamp to min/max range |
| `IsBetween` | `(T?, T?, T?) → bool?` | Inclusive range check |
| `IsBetweenExclusive` | `(T?, T?, T?) → bool?` | Exclusive range check |

Where `numeric?` means any of: `int?`, `long?`, `decimal?`, `double?`, `float?`.

### 17.4 Date and Time Functions

| Function | Signature | Description |
|----------|-----------|-------------|
| `GetDate` | `() → DateTimeOffset?` | Current local date/time |
| `UtcGetDate` | `() → DateTimeOffset?` | Current UTC date/time |
| `Year` | `(DateTimeOffset?) → int?` | Extract year |
| `Month` | `(DateTimeOffset?) → int?` | Extract month |
| `Day` | `(DateTimeOffset?) → int?` | Extract day |
| `Hour` | `(DateTimeOffset?) → int?` | Extract hour |
| `Minute` | `(DateTimeOffset?) → int?` | Extract minute |
| `Second` | `(DateTimeOffset?) → int?` | Extract second |
| `Milliseconds` | `(DateTimeOffset?) → int?` | Extract millisecond |
| `DayOfWeek` | `(DateTimeOffset?) → int?` | Day of week (0=Sunday) |
| `DayOfYear` | `(DateTimeOffset?) → int?` | Day of year (1-366) |
| `WeekOfYear` | `(DateTimeOffset?) → int?` | ISO week number |
| `Quarter` | `(DateTimeOffset?) → int?` | Quarter (1-4) |
| `IsLeapYear` | `(DateTimeOffset?) → bool?` | Leap year check |
| `IsWeekend` | `(DateTimeOffset?) → bool?` | Saturday or Sunday |
| `IsWeekday` | `(DateTimeOffset?) → bool?` | Monday through Friday |
| `AddDays` | `(date?, int) → date?` | Add days |
| `AddMonths` | `(date?, int) → date?` | Add months |
| `AddYears` | `(date?, int) → date?` | Add years |
| `AddHours` | `(date?, int) → date?` | Add hours |
| `AddMinutes` | `(date?, int) → date?` | Add minutes |
| `AddSeconds` | `(date?, int) → date?` | Add seconds |
| `StartOfDay` | `(date?) → date?` | Midnight of given day |
| `EndOfDay` | `(date?) → date?` | 23:59:59.9999999 of given day |
| `DateDiffInDays` | `(date?, date?) → int?` | Day difference |
| `DateDiffInHours` | `(date?, date?) → int?` | Hour difference |
| `DateDiffInMinutes` | `(date?, date?) → int?` | Minute difference |
| `DateDiffInSeconds` | `(date?, date?) → long?` | Second difference |
| `SubtractDateTimeOffsets` | `(DateTimeOffset?, DateTimeOffset?) → TimeSpan?` | Difference |
| `ToDateTimeOffset` | `(string, string?) → DateTimeOffset?` | Parse from string |
| `ToDateTime` | `(string, string?) → DateTime?` | Parse from string |
| `ExtractFromDate` | `(string?, string) → int` | Extract component from date string |

Where `date?` means `DateTime?` or `DateTimeOffset?`.

### 17.5 TimeSpan Functions

| Function | Signature | Description |
|----------|-----------|-------------|
| `AddTimeSpans` | `(params TimeSpan?[]) → TimeSpan?` | Sum timespan values |
| `SubtractTimeSpans` | `(params TimeSpan?[]) → TimeSpan?` | Subtract timespans |
| `ToTimeSpan` | `(string, string?) → TimeSpan?` | Parse from string |

### 17.6 Type Conversion Functions

| Function | Signature | Description |
|----------|-----------|-------------|
| `ToDecimal` | `(value, culture?) → decimal?` | Convert to decimal |
| `ToInt32` | `(value) → int?` | Convert to int |
| `ToInt64` | `(value) → long?` | Convert to long |
| `ToDouble` | `(value) → double?` | Convert to double |
| `ToFloat` | `(value) → float?` | Convert to float |
| `ToString` | `(value, format?, culture?) → string?` | Convert to string |
| `ToChar` | `(value) → char?` | Convert to char |
| `ToHex` | `(value) → string?` | To hexadecimal string |
| `ToBin` | `(value) → string` | To binary string |
| `ToOcta` | `(value) → string` | To octal string |
| `ToDec` | `(value) → string` | To decimal string |
| `ToBase64` | `(value) → string?` | Base64 encode |
| `FromBase64` | `(string?) → byte[]?` | Base64 decode |
| `FromBase64ToString` | `(string?) → string?` | Base64 decode to string |
| `FromHex` | `(string?) → long?` | Hex string to long |
| `FromBin` | `(string?) → long?` | Binary string to long |
| `FromOct` | `(string?) → long?` | Octal string to long |
| `ToHexFromString` | `(string?, encoding?) → string?` | String bytes to hex |

All conversion functions accept any numeric type as input. When conversion fails (e.g., `ToInt32('not_a_number')`), the function returns `null` — no exception is thrown.

### 17.7 Validation Functions

| Function | Signature | Description |
|----------|-----------|-------------|
| `IsValidEmail` | `(string?) → bool?` | RFC 5322 email validation |
| `IsValidUrl` | `(string?) → bool?` | URL validation (http/https/ftp) |
| `IsValidUri` | `(string?) → bool?` | Any absolute URI |
| `IsValidJson` | `(string?) → bool?` | JSON validation |
| `IsValidXml` | `(string?) → bool?` | XML validation |
| `IsValidGuid` | `(string?) → bool?` | GUID/UUID validation |
| `IsValidInteger` | `(string?) → bool?` | Integer string validation |
| `IsValidDecimal` | `(string?) → bool?` | Decimal string validation |
| `IsValidDateTime` | `(string?) → bool?` | DateTime string validation |

### 17.8 JSON Functions

| Function | Signature | Description |
|----------|-----------|-------------|
| `ToJson` | `(T?) → string?` | Serialize to JSON |
| `ExtractFromJson` | `(string?, string?) → string?` | Extract value by JSON path |
| `ExtractFromJsonToArray` | `(string?, string?) → string[]` | Extract values as array |
| `FormatJson` | `(string?) → string?` | Pretty-print JSON |
| `MinifyJson` | `(string?) → string?` | Minify JSON |

### 17.9 Cryptography and Hashing Functions

| Function | Signature | Description |
|----------|-----------|-------------|
| `Md5` | `(string/byte[]?) → string?` | MD5 hash |
| `Sha1` | `(string/byte[]?) → string?` | SHA-1 hash |
| `Sha256` | `(string/byte[]?) → string?` | SHA-256 hash |
| `Sha384` | `(string/byte[]?) → string?` | SHA-384 hash |
| `Sha512` | `(string/byte[]?) → string?` | SHA-512 hash |
| `Crc32` | `(string/byte[]?) → string?` | CRC32 checksum |
| `HmacSha256` | `(string?, string?) → string?` | HMAC-SHA256 |
| `HmacSha512` | `(string?, string?) → string?` | HMAC-SHA512 |

### 17.10 Compression Functions

| Function | Signature | Description |
|----------|-----------|-------------|
| `CompressZLib` | `(string/byte[]?) → byte[]?` | ZLib compress |
| `DecompressZLib` | `(byte[]?, encoding?) → string?` | ZLib decompress |
| `DecompressZLibToBytes` | `(byte[]?) → byte[]?` | ZLib decompress to bytes |
| `CompressZLibToBase64` | `(string?) → string?` | ZLib compress to Base64 |
| `DecompressZLibFromBase64` | `(string?) → string?` | Base64 → ZLib decompress |
| `CompressGZip` | `(string/byte[]?) → byte[]?` | GZip compress |
| `DecompressGZip` | `(byte[]?, encoding?) → string?` | GZip decompress |
| `DecompressGZipToBytes` | `(byte[]?) → byte[]?` | GZip decompress to bytes |
| `CompressGZipToBase64` | `(string?) → string?` | GZip compress to Base64 |
| `DecompressGZipFromBase64` | `(string?) → string?` | Base64 → GZip decompress |
| `CompressDeflate` | `(string/byte[]?) → byte[]?` | Deflate compress |
| `DecompressDeflate` | `(byte[]?, encoding?) → string?` | Deflate decompress |

### 17.11 Binary and Byte Functions

| Function | Signature | Description |
|----------|-----------|-------------|
| `GetBytes` | `(string/char/bool/int/long?) → byte[]?` | Convert to bytes |
| `FromBytesToInt16` | `(byte[]?) → short?` | Bytes to short |
| `FromBytesToInt32` | `(byte[]?) → int?` | Bytes to int |
| `FromBytesToInt64` | `(byte[]?) → long?` | Bytes to long |
| `FromBytesToFloat` | `(byte[]?) → float?` | Bytes to float |
| `FromBytesToDouble` | `(byte[]?) → double?` | Bytes to double |
| `FromBytesToString` | `(byte[]?) → string?` | Bytes to string |
| `FromBytesToBool` | `(byte[]?) → bool?` | Bytes to bool |

### 17.12 Bitwise Functions

| Function | Types | Description |
|----------|-------|-------------|
| `BitwiseAnd` | All integer type pairs | Bitwise AND |
| `BitwiseOr` | All integer type pairs | Bitwise OR |
| `BitwiseXor` | All integer type pairs | Bitwise XOR |
| `BitwiseNot` | All integer types | Bitwise NOT |
| `ShiftLeft` | All integer types | Left bit shift |
| `ShiftRight` | All integer types | Right bit shift |

Cross-type operations promote to the wider type (e.g., `byte AND long` → `long?`).

### 17.13 Network and Utility Functions

| Function | Signature | Description |
|----------|-----------|-------------|
| `IsPrivateIP` | `(string?) → bool?` | RFC 1918 private IP |
| `IpToLong` | `(string?) → long?` | IPv4 to numeric |
| `LongToIp` | `(long?) → string?` | Numeric to IPv4 |
| `IsInSubnet` | `(string?, string?) → bool?` | CIDR subnet check |
| `FormatMac` | `(string?, string?) → string?` | Format MAC address |
| `NewGuid` | `() → string` | New GUID |
| `NewGuidCompact` | `() → string` | GUID without dashes |
| `ConvertBase` | `(string?, int, int) → string?` | Number base conversion (2-36) |
| `UnixToDateTime` | `(long?) → DateTime?` | Unix timestamp to DateTime |
| `UnixMillisToDateTime` | `(long?) → DateTime?` | Unix millis to DateTime |
| `DateTimeToUnix` | `(DateTime?) → long?` | DateTime to Unix timestamp |
| `DateTimeToUnixMillis` | `(DateTime?) → long?` | DateTime to Unix millis |
| `ToSlug` | `(string?) → string?` | URL-friendly slug |
| `EscapeRegex` | `(string?) → string?` | Escape regex chars |
| `EscapeSql` | `(string?) → string?` | Escape single quotes |
| `ExtractUrls` | `(string?) → string?` | URLs from text |
| `ExtractEmails` | `(string?) → string?` | Emails from text |
| `ExtractIPs` | `(string?) → string?` | IPs from text |
| `CalculateEntropy` | `(string?) → double?` | Shannon entropy |
| `ToHumanReadableSize` | `(long?) → string?` | Bytes to "1.5 MB" |
| `ToHumanReadableDuration` | `(long?) → string?` | Seconds to "1h 30m" |
| `IsBase64` | `(string?) → bool?` | Base64 string check |
| `IsHex` | `(string?) → bool?` | Hex string check |
| `IsJwt` | `(string?) → bool?` | JWT token check |
| `JwtDecode` | `(string?) → string?` | Decode JWT payload |
| `JwtGetHeader` | `(string?) → string?` | Decode JWT header |
| `JwtGetClaim` | `(string?, string?) → string?` | Get JWT claim |
| `GetQueryParam` | `(string?, string?) → string?` | URL query parameter |
| `ParseKeyValue` | `(string?, string?, string?, string?) → string?` | Parse key-value |
| `FormatXml` | `(string?) → string?` | Pretty-print XML |
| `MinifyXml` | `(string?) → string?` | Minify XML |

### 17.14 Generic and Collection Functions

| Function | Signature | Description |
|----------|-----------|-------------|
| `RowNumber` | `() → int` | 1-based row counter |
| `GetTypeName` | `(object?) → string?` | .NET type name |
| `Coalesce` | `(params T[]) → T?` | First non-null value |
| `Choose` | `(int, params T[]) → T?` | Value at index from list |
| `If` | `(bool, T, T) → T` | Ternary conditional |
| `NullIf` | `(T?, T?) → T?` | Returns null if equal |
| `IfNull` | `(T?, T?) → T?` | Replace null with default |
| `DefaultIfNull` | `(T?) → T?` | Type default if null |
| `IsNull` | `(T?) → bool` | Null check |
| `IsNotNull` | `(T?) → bool` | Not-null check |
| `Length` | `(IEnumerable<T>/T[]?) → int?` | Collection length |
| `Skip` | `(IEnumerable<T>?, int) → IEnumerable<T>?` | Skip N elements |
| `Take` | `(IEnumerable<T>?, int) → IEnumerable<T>?` | Take N elements |
| `Distinct` | `(IEnumerable<T>?) → IEnumerable<T>?` | Remove duplicates |
| `FirstOrDefault` | `(IEnumerable<T>?) → T?` | First element |
| `LastOrDefault` | `(IEnumerable<T>?) → T?` | Last element |
| `NthOrDefault` | `(IEnumerable<T>?, int) → T?` | Element at index |
| `GetElementAtOrDefault` | `(IEnumerable<T>?, int?) → T?` | Element by index |
| `EnumerableToArray` | `(IEnumerable<T>?) → T[]?` | Convert to array |
| `MergeArrays` | `(params T[][]) → T[]?` | Merge arrays |
| `LongestCommonSequence` | `(IEnumerable<T>?, IEnumerable<T>?) → IEnumerable<T>?` | LCS |

---

## 18. NULL Semantics

### 18.1 NULL Propagation

Most expressions involving `null` produce `null`:

```sql
select null + 1 from #system.dual()        -- null
select null = null from #system.dual()      -- null (not true)
```

### 18.2 NULL in Comparisons

| Expression | Result |
|------------|--------|
| `null = null` | `null` (not `true`) |
| `null <> null` | `null` (not `true`) |
| `null > 1` | `null` |
| `1 = null` | `null` |

Use `IS NULL` or `IS NOT NULL` to test for null:

```sql
where Value is null
where Value is not null
```

### 18.3 NULL in LIKE, RLIKE, CONTAINS

| Expression | Result |
|------------|--------|
| `null LIKE '%test%'` | `false` |
| `null RLIKE 'pattern'` | `false` |
| `null NOT LIKE '%test%'` | `true` |
| `CONTAINS(null, 'a', 'b')` | `false` |
| `CONTAINS(null, null, 'a')` | `true` (null found in list) |

### 18.4 NULL in GROUP BY

`NULL` values form their own distinct group:

```sql
-- Data: (POLAND, WARSAW), (POLAND, null), (GERMANY, BERLIN)
select Country, City, Count(1) from #A.entities() group by Country, City
-- Groups: (POLAND, WARSAW), (POLAND, null), (GERMANY, BERLIN)
```

### 18.5 NULL from OUTER Joins

When `LEFT JOIN` or `OUTER APPLY` produces no match for a left-side row, right-side columns are `null`. Value types are automatically promoted to nullable:

```sql
-- If no match, b.Population becomes decimal? with value null
select a.City, b.Population
from #A.entities() a
left join #B.entities() b on a.City = b.City
```

### 18.6 NULL-Related Functions

| Function | Behavior |
|----------|----------|
| `Coalesce(a, b, c)` | Returns first non-null value |
| `IfNull(value, default)` | Returns default when value is null |
| `NullIf(a, b)` | Returns null when a equals b |
| `IsNull(value)` | Returns `true` if value is null |
| `IsNotNull(value)` | Returns `true` if value is not null |
| `DefaultIfNull(value)` | Returns type's default value if null |

### 18.7 NULL in Functions

Most built-in functions return `null` when any required parameter is `null`:

```sql
select Trim(null) from #system.dual()        -- null
select ToUpper(null) from #system.dual()      -- null
select Abs(null) from #system.dual()          -- null
select Concat(null, 'text') from #system.dual()  -- null
```

---

## 19. String Comparison Semantics

Musoq uses different comparison strategies depending on context:

### 19.1 Case-Insensitive Contexts

These operations are **case-insensitive**:

| Operation | Example |
|-----------|---------|
| `LIKE` | `'Hello' LIKE '%hello%'` → true |
| `NOT LIKE` | `'Hello' NOT LIKE '%hello%'` → false |
| `Contains()` | `Contains('Hello', 'hello')` → true |
| `StartsWith()` | `StartsWith('Hello', 'hello')` → true |
| `EndsWith()` | `EndsWith('Hello World', 'world')` → true |
| `Replace()` | `Replace('Hello', 'hello', 'Hi')` → 'Hi' |
| `IndexOf()` | `IndexOf('Hello', 'HELLO')` → 0 |

### 19.2 Case-Sensitive (Ordinal) Contexts

These operations use **ordinal (case-sensitive)** comparison:

| Operation | Example |
|-----------|---------|
| `=` / `<>` | `'Hello' = 'hello'` → false |
| `ORDER BY` | `'A'` sorts before `'a'` (ASCII order) |
| `GROUP BY` | `'Hello'` and `'hello'` are different groups |
| `DISTINCT` | `'Hello'` and `'hello'` are different values |

### 19.3 Achieving Case-Insensitive Grouping

To group or deduplicate case-insensitively, normalize with `ToLower()` or `ToUpper()`:

```sql
select ToLower(Name), Count(Name) from #A.entities() group by ToLower(Name)
select distinct ToLower(Name) from #A.entities()
```

### 19.4 Unicode Support

Full Unicode support across all operations including LIKE, GROUP BY, ORDER BY, and all string functions. Tested with: Polish, Russian, French, Japanese (Hiragana/Katakana/Kanji), Chinese (Simplified/Traditional), Korean, Arabic, German, Thai, Hebrew, Hindi, Turkish, Greek, Ukrainian, Vietnamese, and emoji.

---

## 20. Array and Property Access

### 20.1 Array Indexing

Array elements are accessed with bracket notation (0-based):

```sql
select Array[0] from #A.entities()     -- first element
select Array[2] from #A.entities()     -- third element
```

#### Negative Indexing

Negative indices count from the end (Python-style wrapping):

```sql
select Array[-1] from #A.entities()    -- last element
select Array[-2] from #A.entities()    -- second to last
```

#### Out-of-Bounds Access

Out-of-bounds access **never throws an exception**. It returns the default value for the element type:

| Scenario | Result |
|----------|--------|
| `int_array[100]` (out of bounds) | `0` (default int) |
| `string_array[100]` | `null` |
| `Array[-100]` (excessive negative) | Wraps modularly |

### 20.2 String Character Access

Strings support bracket indexing to access individual characters:

```sql
select Name[0] from #A.entities()      -- first character
select Name[-1] from #A.entities()     -- last character
```

Out-of-bounds on strings returns `'\0'` (null character). Null strings return `'\0'`.

### 20.3 Dictionary Key Access

Access dictionary values by key:

```sql
select Dict['key_name'] from #A.entities()
```

Missing keys return `null` (no exception).

### 20.4 Property Navigation

Access nested object properties with dot notation:

```sql
select Self.Name from #A.entities()              -- single level
select Self.Self.Name from #A.entities()          -- two levels
select Self.Self.Array from #A.entities()         -- deep property
select Self.Array[2] from #A.entities()           -- property + index
select Inc(Self.Array[2]) from #A.entities()      -- function on indexed property
```

Accessing a non-existing property throws `UnknownPropertyException` at compile time.

### 20.5 Method Calls on Entities

Entity methods can be called with dot notation:

```sql
select a.GetPopulation() from #A.entities() a
select a.ToUpperInvariant(a.City) from #A.entities() a
```

---

## 21. Automatic Type Coercion

### 21.1 String-to-Numeric Coercion

When a `string` column is compared to a numeric literal, the engine automatically attempts to parse the string as a number at runtime:

```sql
-- Size is a string column containing "1500"
select Name from Items() where Size > 1000       -- matches: "1500" parsed as 1500
select Name from Items() where Size = 1500       -- matches exact value
select Name from Items() where 1000 < Size       -- bidirectional: literal on left
```

**Edge cases:**
- Non-numeric strings (e.g., `"abc"`) simply don't match — no exception thrown
- `null` strings don't match any numeric comparison
- Supports all comparison operators: `=`, `<>`, `>`, `<`, `>=`, `<=`
- Works with hex (`0xFF`), binary (`0b1010`), and long (`9223372036854775807l`) literals

### 21.2 String-to-DateTime Coercion

When a `DateTime`, `DateTimeOffset`, or `TimeSpan` column is compared to a string literal, automatic parsing occurs:

```sql
-- EventDate is DateTime column
select Name from Events() where EventDate > '2023-01-01'
select Name from Events() where EventDate = '2023-03-15'

-- EventDate is DateTimeOffset column
select Name from Events() where EventDate = '2023-03-15T12:00:00+00:00'

-- Duration is TimeSpan column
select Name from Events() where Duration >= '02:00:00'
```

Bidirectional comparisons work:

```sql
select Name from Events() where '2023-03-15' < EventDate
```

Works in CASE WHEN expressions:

```sql
select Name,
    case when EventDate > '2023-03-15' then 'Future'
         when EventDate = '2023-03-15' then 'Present'
         else 'Past'
    end as TimeCategory
from Events()
```

Nullable date/time types (`DateTime?`, `DateTimeOffset?`, `TimeSpan?`) behave identically.

### 21.3 Numeric Type Promotion

In arithmetic and bitwise operations involving different numeric types, values are promoted to the wider type:

| Operation | Result Type |
|-----------|-------------|
| `byte + int` | `int` |
| `int + long` | `long` |
| `int + decimal` | `decimal` |
| `sbyte AND byte` | `int?` |
| `byte AND ulong` | `ulong?` |

### 21.4 Object Column Coercion

When an `object`-typed column is compared to a numeric literal, runtime conversion is attempted. Same graceful failure as string coercion — no exception on failure.

---

## 22. Error Catalog

### 22.1 Compile-Time Errors

| Error | Cause | Message/Exception |
|-------|-------|-------------------|
| Non-aggregated column in SELECT | Column not in GROUP BY and not aggregated | `NonAggregatedColumnInSelectException` |
| Unknown column or alias | Referencing a SELECT alias in WHERE or GROUP BY | `UnknownColumnOrAliasException` |
| Duplicate alias in join | Using the same alias for two tables | `AliasAlreadyUsedException` |
| Division by zero (literal) | `10 / 0` with literal zero | `CompilationException` |
| Modulo by zero (literal) | `10 % 0` with literal zero | `CompilationException` |
| `ILIKE` operator used | Using `ILIKE` (PostgreSQL syntax) | Error: *"Consider using LIKE instead."* |
| Non-existing property | `Self.NonExistingProperty` | `UnknownPropertyException` |
| Indexer not supported | `Self['key']` on non-indexable type | `ObjectDoesNotImplementIndexerException` |
| Non-array indexed | `Self[0]` on non-array type | `ObjectIsNotAnArrayException` |
| SELECT * with GROUP BY | Star expands to non-aggregated columns | `NonAggregatedColumnInSelectException` |
| Missing alias in multi-table | Column without table qualifier in join | `AliasMissingException` |

### 22.2 Runtime Errors

| Error | Cause | Behavior |
|-------|-------|----------|
| Invalid type conversion | `ToInt32('abc')` | Returns `null` (no exception) |
| Invalid regex in RLIKE | `Name RLIKE '[invalid('` | Exception thrown |
| Non-numeric string comparison | String `"abc"` compared to number | No match, no exception |

### 22.3 Graceful Failures

These situations are handled gracefully without exceptions:

| Situation | Behavior |
|-----------|----------|
| Array out-of-bounds access | Returns default value |
| Dictionary missing key | Returns `null` |
| String character out-of-bounds | Returns `'\0'` |
| SKIP exceeds row count | Returns 0 rows |
| TAKE exceeds row count | Returns all available rows |
| Null string in numeric comparison | No match |

---

## 23. Formal Grammar

### 23.1 Notation

- `KEYWORD` — literal keyword (case-insensitive)
- `name` — production rule
- `[x]` — optional
- `{x}` — zero or more repetitions
- `x | y` — alternatives
- `'symbol'` — literal symbol

### 23.2 Statement-Level Grammar

```ebnf
root           ::= statement { ';' statement } [';']

statement      ::= select_query
                 | cte_expression
                 | table_definition
                 | couple_statement
                 | desc_statement

cte_expression ::= WITH cte_def {',' cte_def} set_operators

cte_def        ::= identifier AS '(' set_operators ')'
```

### 23.3 Query Grammar

```ebnf
set_operators  ::= query { set_operator query }

set_operator   ::= UNION ['(' key_list ')']
                 | UNION ALL ['(' key_list ')']
                 | EXCEPT ['(' key_list ')']
                 | INTERSECT ['(' key_list ')']

key_list       ::= [identifier {',' identifier}]

query          ::= regular_query | reordered_query

regular_query  ::= SELECT [DISTINCT] select_list
                   FROM from_clause
                   {join_or_apply}
                   [WHERE expression]
                   [GROUP BY expression_list [HAVING expression]]
                   [ORDER BY order_list]
                   [SKIP integer]
                   [TAKE integer]

reordered_query ::= FROM from_clause
                    {join_or_apply}
                    [WHERE expression]
                    [GROUP BY expression_list [HAVING expression]]
                    SELECT [DISTINCT] select_list
                    [ORDER BY order_list]
                    [SKIP integer]
                    [TAKE integer]
```

### 23.4 FROM Clause Grammar

```ebnf
from_clause    ::= schema_source [alias]
                 | identifier [alias]

schema_source  ::= ['#'] identifier '.' identifier '(' [arg_list] ')'

alias          ::= identifier | AS identifier

join_or_apply  ::= join_clause | apply_clause

join_clause    ::= [INNER] JOIN from_clause ON expression
                 | LEFT [OUTER] JOIN from_clause ON expression
                 | RIGHT [OUTER] JOIN from_clause ON expression

apply_clause   ::= CROSS APPLY apply_source AS identifier
                 | OUTER APPLY apply_source AS identifier

apply_source   ::= schema_source
                 | identifier '.' method_call
                 | identifier '.' property_path
```

### 23.5 SELECT List Grammar

```ebnf
select_list    ::= select_item {',' select_item}

select_item    ::= '*'
                 | identifier '.' '*'
                 | expression [[AS] alias_name]

alias_name     ::= identifier
                 | string_literal
                 | '[' any_text ']'
```

### 23.6 Expression Grammar (by precedence, lowest to highest)

```ebnf
expression     ::= or_expr

or_expr        ::= and_expr {OR and_expr}
and_expr       ::= not_expr {AND not_expr}
not_expr       ::= [NOT] comparison
comparison     ::= add_expr [comp_op add_expr]
                 | add_expr IS [NOT] NULL
                 | add_expr [NOT] IN '(' expression_list ')'
                 | add_expr [NOT] LIKE expression
                 | add_expr [NOT] RLIKE expression
                 | add_expr CONTAINS '(' expression_list ')'

comp_op        ::= '=' | '<>' | '>' | '>=' | '<' | '<='

add_expr       ::= bitwise_expr {('+'|'-') bitwise_expr}
bitwise_expr   ::= shift_expr {('&'|'|'|'^') shift_expr}
shift_expr     ::= mul_expr {('<<'|'>>') mul_expr}
mul_expr       ::= unary_expr {('*'|'/'|'%') unary_expr}
unary_expr     ::= ['-'] primary

primary        ::= literal
                 | identifier ['.' identifier]* ['(' [arg_list] ')']
                 | identifier '[' expression ']'
                 | '(' expression ')'
                 | case_expression
                 | '::' integer

case_expression ::= CASE {WHEN expression THEN expression}+ ELSE expression END
```

### 23.7 Literal Grammar

```ebnf
literal        ::= string_literal
                 | integer_literal [type_suffix]
                 | decimal_literal
                 | hex_literal
                 | binary_literal
                 | octal_literal
                 | TRUE | FALSE
                 | NULL

string_literal ::= "'" {char | escape_seq} "'"

escape_seq     ::= '\' ('\\' | "'" | '"' | 'n' | 'r' | 't' | 'b' | 'f' | 'e' | '0')
                 | '\u' hex_digit hex_digit hex_digit hex_digit
                 | '\x' hex_digit hex_digit

integer_literal ::= digit {digit}
decimal_literal ::= digit {digit} '.' digit {digit} ['d'|'D']
                  | '.' digit {digit} ['d'|'D']
hex_literal     ::= '0' ('x'|'X') hex_digit {hex_digit}
binary_literal  ::= '0' ('b'|'B') ('0'|'1') {('0'|'1')}
octal_literal   ::= '0' ('o'|'O') octal_digit {octal_digit}

type_suffix    ::= 'b' | 'ub' | 's' | 'us' | 'i' | 'ui' | 'l' | 'ul' | 'd' | 'D'
```

### 23.8 Utility Statement Grammar

```ebnf
table_definition ::= TABLE identifier '{' column_def_list '}'

column_def_list ::= column_def { ',' column_def } [',']

column_def     ::= identifier type_name ['?']

type_name      ::= identifier
                  | qualified_type_name

qualified_type_name ::= identifier { '.' identifier }

couple_statement ::= COUPLE schema_source WITH TABLE identifier AS identifier

desc_statement ::= DESC ['#'] identifier
                 | DESC ['#'] identifier '.' identifier
                 | DESC ['#'] identifier '.' identifier '(' [arg_list] ')'
                 | DESC FUNCTIONS ['#'] identifier
```

---

## 24. Appendices

### Appendix A: Complete Keyword List

```
AND, AS, ASC, CASE, CONTAINS, COUPLE, CROSS APPLY, DESC, DISTINCT,
ELSE, END, EXCEPT, FALSE, FROM, FUNCTIONS, GROUP BY, HAVING, IN,
INNER JOIN, INTERSECT, IS, JOIN, LEFT JOIN, LEFT OUTER JOIN, LIKE,
NOT, NOT IN, NOT LIKE, NOT RLIKE, NULL, ON, OR, ORDER BY,
OUTER APPLY, RIGHT JOIN, RIGHT OUTER JOIN, RLIKE, SELECT, SKIP,
TABLE, TAKE, THEN, TRUE, UNION, UNION ALL, WHEN, WHERE, WITH
```

### Appendix B: Operator Precedence Table

From **lowest** to **highest** precedence:

| Level | Operators | Category |
|-------|-----------|----------|
| — | `OR` | Logical |
| — | `AND` | Logical |
| — | `NOT` | Logical |
| — | `=`, `<>`, `>`, `>=`, `<`, `<=`, `IS`, `IN`, `LIKE`, `RLIKE`, `CONTAINS` | Comparison |
| 0 | `&`, `\|`, `^` | Bitwise |
| 1 | `<<`, `>>` | Shift |
| 2 | `+`, `-` | Additive |
| 3 | `*`, `/`, `%` | Multiplicative |
| 4 | `.` | Member access |

### Appendix C: Escape Sequence Reference

| Sequence | Character | Code Point |
|----------|-----------|------------|
| `\\` | Backslash | U+005C |
| `\'` | Single quote | U+0027 |
| `\"` | Double quote | U+0022 |
| `\n` | Newline | U+000A |
| `\r` | Carriage return | U+000D |
| `\t` | Tab | U+0009 |
| `\b` | Backspace | U+0008 |
| `\f` | Form feed | U+000C |
| `\e` | Escape | U+001B |
| `\0` | Null | U+0000 |
| `\uXXXX` | Unicode | U+XXXX |
| `\xXX` | Hex byte | — |

### Appendix D: Numeric Literal Format Summary

| Format | Prefix/Suffix | Type | Example |
|--------|---------------|------|---------|
| Integer | (none) | `int` | `42` |
| Explicit int | `i` | `int` | `42i` |
| Signed byte | `b` | `sbyte` | `127b` |
| Unsigned byte | `ub` | `byte` | `255ub` |
| Short | `s` | `short` | `1000s` |
| Unsigned short | `us` | `ushort` | `65535us` |
| Unsigned int | `ui` | `uint` | `100ui` |
| Long | `l` | `long` | `1l` |
| Unsigned long | `ul` | `ulong` | `1ul` |
| Decimal (suffix) | `d` / `D` | `decimal` | `42d` |
| Decimal (dot) | `.` | `decimal` | `3.14` |
| Hexadecimal | `0x` / `0X` | `long` | `0xFF` |
| Binary | `0b` / `0B` | `long` | `0b1010` |
| Octal | `0o` / `0O` | `long` | `0o77` |

### Appendix E: Key Differences from Standard SQL

| Feature | Standard SQL | Musoq |
|---------|-------------|-------|
| Data sources | `FROM table_name` | `FROM #schema.method()` |
| Pagination | `OFFSET n LIMIT m` | `SKIP n TAKE m` |
| Not-equal | `<>` and `!=` | Only `<>` supported — `!=` is rejected with a suggestion to use `<>` |
| CASE WHEN ELSE | ELSE optional | ELSE **mandatory** |
| Set operations | `UNION` (implicit key) | `UNION (key_columns)` (explicit keys) |
| Recursive CTEs | Supported | Not supported |
| Subqueries in FROM | Supported | Not supported (use CTEs) |
| `BETWEEN` | `x BETWEEN a AND b` | Supported — `x BETWEEN a AND b` is equivalent to `x >= a AND x <= b` |
| Window functions | `ROW_NUMBER() OVER (...)` | `RowNumber()` (no OVER clause, sequential) |
| String comparison | Implementation-defined | LIKE is case-insensitive; `=` is ordinal |
| `#` prefix | Not applicable | Optional schema identifier prefix |
| Cross/Outer Apply | T-SQL only | Fully supported with method/property expansion |
| Array indexing | Not standard | `column[n]`, negative indexing, safe OOB |
| Hash-optional | N/A | `#A.Entities()` = `A.Entities()` |
| Property navigation | Not standard | `column.property.subproperty` |
| Type suffixes | Not standard | `42l`, `255ub`, `3.14d` |
| Hex/bin/oct literals | Varies | `0xFF`, `0b1010`, `0o77` |

### Appendix F: CASE WHEN Requirements

The `ELSE` clause is **mandatory** in all CASE expressions:

```sql
-- VALID
select case when x > 0 then 'positive' else 'non-positive' end from ...

-- ERROR: Missing ELSE clause
select case when x > 0 then 'positive' end from ...
```

Multiple WHEN branches are supported:

```sql
select
    case
        when Population >= 1000 then 'large'
        when Population >= 500 then 'medium'
        when Population >= 100 then 'small'
        else 'tiny'
    end
from #A.entities()
```

CASE expressions can be nested:

```sql
select
    case when x > 0 then
        case when x > 100 then 'very large' else 'moderate' end
    else 'non-positive'
    end
from ...
```

CASE can appear in SELECT, WHERE, GROUP BY, ORDER BY, and HAVING:

```sql
-- In GROUP BY
select case when Population >= 500 then 'big' else 'small' end, Count(1)
from #A.entities()
group by case when Population >= 500 then 'big' else 'small' end

-- In arithmetic
select 1 + (case when 2 > 1 then 1 else 0 end) - 1 from #system.dual()

-- Short-circuit evaluation: only the matching branch is evaluated
select case when City <> 'X' then 'safe' else ThrowException() end from ...
```

### Appendix G: Field Links (GROUP BY References)

Field links use `::N` syntax (where N is a positive integer starting from 1) to reference GROUP BY columns by position:

```sql
-- ::1 refers to the first GROUP BY expression (Country)
select ::1, Count(Name) from #A.entities() group by Country

-- Equivalent to:
select Country, Count(Name) from #A.entities() group by Country
```

**Rules:**
- `::N` is 1-based — `::1` references the first GROUP BY column.
- `N` must be a positive integer (no `::0`).
- If `N` exceeds the number of GROUP BY columns, a `FieldLinkIndexOutOfRangeException` is thrown.
- Field links are resolved during query compilation — they are syntactic shorthand, not a runtime feature.
