# Musoq Core SQL Language Specification

**Version:** 1.1.0
**Status:** Specification  
**Author:** Jakub Puchała

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
11. [Window Functions](#11-window-functions)
12. [Set Operations](#12-set-operations)
13. [ORDER BY, SKIP, TAKE](#13-order-by-skip-take)
14. [Common Table Expressions (CTEs)](#14-common-table-expressions-ctes)
15. [TABLE and COUPLE Statements](#15-table-and-couple-statements)
16. [DESC Statement](#16-desc-statement)
17. [Reordered Query Syntax](#17-reordered-query-syntax)
18. [Built-in Functions](#18-built-in-functions)
19. [NULL Semantics](#19-null-semantics)
20. [String Comparison Semantics](#20-string-comparison-semantics)
21. [Array and Property Access](#21-array-and-property-access)
22. [Automatic Type Coercion](#22-automatic-type-coercion)
23. [Error Catalog](#23-error-catalog)
24. [Formal Grammar](#24-formal-grammar)
25. [Appendices](#25-appendices)

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

Unless stated otherwise, behavior described in this document refers to the **Musoq query engine**. Some host applications (for example, specific CLI preprocessors) may apply additional parsing/validation before a query reaches the engine.

This specification does **not** cover:

- Specific data source schemas (e.g., git, file system, Docker) — each data source defines its own tables and columns
- Internal compilation or code generation details
- Performance characteristics or optimization strategies
- The interpretation schema extension (binary/text parsing) — see the separate `musoq-binary-text-spec.md`

### 1.3 Relationship to Standard SQL

Musoq implements a subset of SQL with several extensions:

| Aspect | Standard SQL | Musoq |
|--------|-------------|-------|
| Data sources | Tables in a database | Schema providers (`schema.method()`) |
| Pagination | `OFFSET` / `LIMIT` | `SKIP` / `TAKE` |
| Set operation keys | Implicit (all columns) | Explicit key columns required: `UNION (col1)`; bare `UNION` / `UNION ALL` is rejected |
| Not-equal operator | Both `<>` and `!=` | Only `<>` is supported — `!=` is rejected with a helpful error suggesting `<>` |
| CASE WHEN | ELSE is optional | ELSE is **mandatory** |
| Simple CASE form | `CASE expr WHEN value THEN ...` | Supported (desugared to searched CASE internally) |
| FROM-first syntax | Not standard | Supported: `FROM ... WHERE ... SELECT ...` |
| CROSS APPLY / OUTER APPLY | T-SQL extension | Fully supported with method and property expansion |
| Recursive CTEs | Supported in many dialects | **Not supported** |
| Subqueries in FROM | Supported | **Not supported** — use CTEs instead |
| `BETWEEN` operator | Supported | Supported — `x BETWEEN a AND b` is equivalent to `x >= a AND x <= b` |
| `ORDER BY` position | `ORDER BY 1` | **Not supported** — use column names or expressions |
| ASOF JOIN | Not standard (DuckDB extension) | Supported — nearest-match join on an ordered column |
| Window functions | `OVER (PARTITION BY ... ORDER BY ...)` with frame specs | Supported without frame specifications (`ROWS BETWEEN`, `RANGE BETWEEN`). Supports `PARTITION BY`, `ORDER BY`, named `WINDOW` clause, ranking (`ROW_NUMBER`, `RANK`, `DENSE_RANK`, `NTILE`), offset (`LAG`, `LEAD`), aggregate (`SUM`, `COUNT`, `AVG`, `MIN`, `MAX`), and value access (`FIRST_VALUE`, `LAST_VALUE`, `NTH_VALUE`) window functions. Data-source plugins can register custom window functions. `QUALIFY` is **not supported** — use a CTE instead. |
| `ORDER BY` with set operations | Applies to the entire combined result | `ORDER BY` after a set operation (e.g., `UNION`) is parsed per-query, not as a clause over the combined result — see §12.10 |

### 1.4 Terminology

| Term | Definition |
|------|------------|
| **Schema** | A named data source provider (e.g., `git`, `os`, `csv`) that exposes one or more methods |
| **Method** | A specific data-producing function on a schema (e.g., `git.log()`, `os.files('/path')`) |
| **Entity** | A single row returned by a data source method |
| **Column** | A named, typed field within an entity |
| **Expression** | Any computation that produces a value (arithmetic, function call, column reference, etc.) |
| **CTE** | Common Table Expression — a named temporary result set defined with `WITH ... AS (...)` |
| **Apply** | A correlated join where the right side can reference columns from the left side |

### 1.5 Notation Conventions

The key words MUST, MUST NOT, SHOULD, SHOULD NOT, and MAY in this specification are to be interpreted as described in [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119). When these words appear in uppercase, they carry normative weight. When they appear in lowercase, they are used in their ordinary English sense.

### 1.6 Specification Family

This document is the core specification in a family of related documents:

| Document | Scope |
|----------|-------|
| **Musoq Core SQL Language Specification** (this document) | Core SQL dialect: syntax, semantics, type system, built-in functions, error catalog |
| **Musoq Interpretation Schemas: Language Extension Specification** (`musoq-binary-text-spec.md`) | `binary` and `text` schema extensions for declarative parsing of binary and textual data |
| **Musoq AI Interpretation Schemas: Language Extension Specification** (`musoq-ai-spec.md`) | `ai` schema extension for structured extraction from unstructured content via LLMs |
| **Musoq TABLE/COUPLE Statements Specification** (`musoq-table-couple-spec.md`) | `TABLE` and `COUPLE` statements for defining explicit type schemas for untyped data sources |

Satellite specifications extend the core language with new statement types and schema kinds. They inherit the core specification’s lexical rules, type system, expression semantics, and notation conventions.

### 1.7 Conformance

A conforming implementation MUST support all features defined in this core specification. The extension specifications (§1.6) define optional profiles:

- **Binary/Text Interpretation profile**: `binary` and `text` schema definitions, `Interpret()`, `Parse()`, and related functions
- **AI Interpretation profile**: `ai` schema definitions, `Infer()`, `TryInfer()`, and related functions
- **TABLE/COUPLE profile**: `TABLE` and `COUPLE` statements for explicit type binding

An implementation MAY support any combination of profiles. An implementation that claims conformance to a profile MUST implement all features defined in the corresponding satellite specification.

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
| `ASOF` | ASOF JOIN modifier (context-sensitive — only a keyword before `JOIN` or `LEFT`) |
| `EXCLUDE` | Star modifier — remove columns (context-sensitive — only a keyword after `*` or `alias.*`) |
| `REPLACE` | Star modifier — substitute column expressions (context-sensitive — only a keyword after `*` or `alias.*`) |

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
| `ASOF JOIN` | ASOF inner join (nearest-match on ordered column) |
| `ASOF LEFT JOIN` or `ASOF LEFT OUTER JOIN` | ASOF left outer join |
| `CROSS APPLY` | Correlated cross join |
| `OUTER APPLY` | Correlated outer join |

### 2.3 Identifiers

**Column names and method names are case-sensitive.** `Name`, `name`, and `NAME` reference different columns.

**Bracket-quoted identifiers** allow reserved words and special characters:

```sql
select [case], [order], [Column With Spaces] from schema.method()
```

Schema data sources are referenced directly as `Schema.Method()`.

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
select 'Hello, World!' from system.dual()
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
select '\\' from system.dual()                    -- result: \
select '\'' from system.dual()                     -- result: '
select '\n' from system.dual()                     -- result: (newline)
select '\u0041' from system.dual()                 -- result: A
select '\x41' from system.dual()                   -- result: A
select 'Hello\nWorld\t\u0394\\test' from system.dual()  -- result: Hello(LF)World(TAB)Δ\test
select '\0\b\f\e' from system.dual()              -- result: (null)(backspace)(formfeed)(ESC)
```

Special characters are valid inside string literals — all punctuation, braces, brackets, etc.:

```sql
select '{', '}', '[', ']', '(', ')' from system.dual()
```

### 2.6 Numeric Literals

#### Integer Literals

Bare integers default to `int` (32-bit signed integer):

```sql
select 42 from system.dual()        -- type: int
select -42 from system.dual()       -- type: int (negative)
```

#### Decimal Literals

Numbers with a decimal point are `decimal`:

```sql
select 3.14 from system.dual()      -- type: decimal
select -1.5 from system.dual()      -- type: decimal
select .5 from system.dual()        -- type: decimal (leading dot)
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
select 1b from system.dual()        -- sbyte
select 255ub from system.dual()     -- byte
select 1000s from system.dual()     -- short
select 65535us from system.dual()   -- ushort
select 42i from system.dual()       -- int (explicit)
select 100ui from system.dual()     -- uint
select 1l from system.dual()        -- long
select 1ul from system.dual()       -- ulong
select 42d from system.dual()       -- decimal (integer forced to decimal)
select 1.0 from system.dual()       -- decimal (implicit from decimal point)
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
select 0xFF from system.dual()                    -- 255 (long)
select 0b1010 from system.dual()                  -- 10 (long)
select 0o77 from system.dual()                    -- 63 (long)
select 0xFF + 0b1010 + 0o77 + 42 from system.dual()  -- 370 (mixed arithmetic)
select 0x0 from system.dual()                     -- 0 (zero values valid)
```

### 2.7 Boolean Literals

```sql
select true from system.dual()
select false from system.dual()
```

### 2.8 NULL Literal

```sql
select null from system.dual()
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
select 256 + 256 / 2 from system.dual()       -- 384 (division first)
select (256 + 256) / 2 from system.dual()      -- 256 (parentheses override)
select 2 * 3 / 2 from system.dual()            -- 3
select 1 + 2 * 3 * (7 * 8) - (45 - 10) from system.dual()  -- 302
```

**Unary minus** is supported:

```sql
select 1 - -1 from system.dual()               -- 2
select 1 - -(1 + 2) from system.dual()         -- 4
select 1 + (-2) from system.dual()             -- -1
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
select 'Hello' + ' ' + 'World' from system.dual()   -- 'Hello World'
select Concat(Name, ' - ', City) from schema.method()  -- alternative
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

Date/time types have no literal syntax — they are produced by data sources or conversion functions. However, when compared against string literals, automatic parsing occurs (see §22).

### 3.3 Nullable Types

Any type can be nullable. When a column is nullable, its values may be `null`. Value types from `OUTER APPLY` and `LEFT JOIN` operations are automatically promoted to nullable when the right side produces no match:

```sql
-- If outer apply produces no match, b.Population becomes decimal? (nullable)
select a.Name, b.Population
from A.entities() a
outer apply B.entities(a.Country) b
```

### 3.4 Complex Object Types

Data sources may expose columns containing complex objects with nested properties. Access nested values with dot notation:

```sql
select Self.Name from A.entities()              -- one level deep
select Self.Self.Name from A.entities()          -- two levels deep
select Self.Dictionary['key'] from A.entities()  -- dictionary access
select Self.Array[2] from A.entities()           -- array indexing
```

### 3.5 Collections and Arrays

Columns may hold arrays (`T[]`) or enumerables (`IEnumerable<T>`). These can be:
- Indexed with `[N]` syntax (see §21)
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
table MyTable { Name: string };
couple A.Entities with table MyTable as Source;
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
select 1 from system.dual()                          -- literal
select Name from A.entities()                         -- column reference
select a.Name from A.entities() a                     -- qualified column
select 1 + 2 * 3 from system.dual()                  -- arithmetic
select Concat(City, ', ', Country) from A.entities()  -- function call
select a.GetPopulation() from A.entities() a          -- method on entity
select Self.Name from A.entities()                    -- property access
select Self.Array[2] from A.entities()                -- indexed access
```

### 5.3 Column Aliasing

Three equivalent forms:

```sql
select Name as FullName from A.entities()       -- explicit AS keyword
select Name FullName from A.entities()           -- implicit (space-separated)
select Name [Full Name] from A.entities()        -- bracketed (allows spaces)
```

When no alias is given, the column name is derived from the expression:
- Column reference: `Name` → column name `Name`
- Function call: `Count(Name)` → column name `Count(Name)`
- Literal: `1` → column name `1`

> **Important**: SELECT aliases MUST NOT be referenced in WHERE or GROUP BY clauses. Doing so produces an `UnknownColumnOrAliasException`.

```sql
-- ERROR: SELECT alias cannot be used in WHERE
select Name as FileName from A.entities() where FileName = 'test'

-- ERROR: SELECT alias cannot be used in GROUP BY
select Length(Name) as NameLen from A.entities() group by NameLen
```

### 5.4 Star Expression (Wildcard)

`*` expands to all columns from the data source:

```sql
select * from A.entities()                              -- all columns
select *, Name as Name2 from A.entities() a             -- star + explicit columns
select *, * from A.entities() a                          -- duplicated columns
```

Qualified star selects columns from a specific table in a join:

```sql
select a.* from A.entities() a inner join B.entities() b on a.Id = b.Id
select a.*, b.* from A.entities() a inner join B.entities() b on a.Id = b.Id
```

Star works through CTEs:

```sql
with p as (select City, Country from A.entities())
select * from p       -- expands to City, Country
```

#### 5.4.1 Star Modifiers

Star expressions support optional modifiers that filter or transform the expanded columns. Modifiers are applied in a fixed order: **LIKE/NOT LIKE → EXCLUDE → REPLACE**.

**EXCLUDE** removes named columns from the expansion:

```sql
select * exclude (City) from A.entities() a
select * exclude (City, Country, Population) from A.entities() a
select a.* exclude (Id) from A.entities() a inner join B.entities() b on a.Id = b.Id
```

**REPLACE** substitutes a column's expression while keeping it in the same position:

```sql
select * replace (Population * 2 as Population) from A.entities() a
select * replace (Upper(Name) as Name, Round(Money, 0) as Money) from A.entities() a
```

**LIKE / NOT LIKE** filters columns by a SQL LIKE pattern on column names (case-insensitive):

```sql
select * like 'C%' from A.entities() a           -- only columns starting with 'C'
select * not like '%Id' from A.entities() a       -- exclude columns ending with 'Id'
select * like '_ame' from A.entities() a          -- match single-char wildcard
```

Modifiers can be composed (all three at once):

```sql
select * like '%o%' exclude (Country) replace (Population * 3 as Population) from A.entities() a
```

**Rules:**

- Modifiers are always applied in the order LIKE → EXCLUDE → REPLACE.
- EXCLUDE and REPLACE column names are matched **case-insensitively**.
- EXCLUDE must not remove all columns (compile-time error MQ3043).
- REPLACE targets must exist in the surviving column set (after LIKE and EXCLUDE).
- A column cannot appear in both EXCLUDE and REPLACE (compile-time error MQ3044).
- Duplicate entries within EXCLUDE or REPLACE are forbidden (MQ3046, MQ3047).
- LIKE/NOT LIKE patterns must match at least one column (MQ3045).
- `EXCLUDE` and `REPLACE` are **context-sensitive keywords** — they are only treated as keywords immediately after `*` or `alias.*`. In all other positions, they are valid identifiers.

### 5.5 DISTINCT

`SELECT DISTINCT` removes duplicate rows from the result:

```sql
select distinct Country from A.entities()
select distinct City, Country from A.entities()   -- unique combinations
```

DISTINCT uses **ordinal (case-sensitive) comparison** — `'POLAND'` and `'poland'` are treated as different values. To achieve case-insensitive deduplication, use `ToLower()`:

```sql
select distinct ToLower(Country) from A.entities()
```

### 5.6 RowNumber

`RowNumber()` returns a 1-based sequential integer for each row in the result:

```sql
select RowNumber(), Name from A.entities()
```

`RowNumber()` is assigned **after** ORDER BY but **before** SKIP/TAKE. When used with ORDER BY, rows are first sorted, then numbered sequentially:

```sql
select Country, RowNumber() from A.entities() order by Country
-- Rows are sorted alphabetically, then numbered 1, 2, 3, ...
-- Germany → 1, Poland → 2 (sorted order determines numbering)
```

With SKIP, row numbers are assigned before SKIP is applied:

```sql
select Country, RowNumber() from A.entities() order by Country skip 1
-- Full result: Germany=1, Poland=2; after SKIP 1: Poland=2 (number preserved)
```

With WHERE filtering, `RowNumber()` counts only the rows that pass the filter:

```sql
select Country, RowNumber() from A.entities() where Country = 'Poland'
-- Returns rows numbered 1, 2, ... (only matching rows counted)
```

---

## 6. FROM Clause

### 6.1 Schema Data Sources

The primary data source syntax uses schema providers:

```sql
select * from schema.method()
select * from schema.method(arg1, arg2)
select * from schema.method() alias
```

Arguments can be literals of any type:

```sql
select * from test.whatever(1, 2d, true, false, 'text')
```

### 6.2 Table Aliasing

Data sources can be given an alias for reference in expressions:

```sql
select a.Name from A.entities() a
select entities.Name from A.entities() entities
```

In **single-table queries**, aliasing is optional — column names can be used directly:

```sql
select Name from A.entities()         -- no alias needed
select a.Name from A.entities() a     -- alias optional
```

In **multi-table queries** (joins, applies), aliases MUST be used to disambiguate columns. For function calls, the engine attempts auto-resolution first — see [§8.7.1](#871-method-auto-resolution-algorithm) for details:

```sql
select a.Name, b.City
from A.entities() a
inner join B.entities() b on a.Id = b.Id
```

### 6.3 CTE References

After a CTE is defined, use its name as a data source:

```sql
with cte as (select City, Country from A.entities())
select * from cte
```

CTE references can be aliased:

```sql
with cte as (select City from A.entities())
select p.City from cte p
```

### 6.4 Coupled Table References

After a `COUPLE` statement, the coupled alias becomes a data source:

```sql
table MyTable { Name: string };
couple A.Entities with table MyTable as Source;
select Name from Source()
select Name from Source(true, 'param')   -- with arguments
```

### 6.5 `system.range(start, end)` Semantics

The `system.range(start, end)` source uses an **end-exclusive** interval.

```sql
select Value from system.range(1, 5)
-- Returns: 1, 2, 3, 4
```

Formally, the returned sequence is equivalent to $[start, end)$.

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

`RLIKE` matches against a regular expression (ECMAScript-compatible subset):

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
select case when Match('\d+', Name) then 'yes' else 'no' end from A.entities()
```

---

## 8. JOIN Clause

### 8.1 INNER JOIN

Returns only rows where the join condition matches in both tables:

```sql
select a.City, b.Population
from A.entities() a
inner join B.entities() b on a.City = b.City
```

`JOIN` without the `INNER` keyword is equivalent to `INNER JOIN`:

```sql
from A.entities() a join B.entities() b on a.City = b.City
```

### 8.2 LEFT OUTER JOIN

Returns all rows from the left table. Unmatched right-side columns are `null`:

```sql
select a.City, b.Population
from A.entities() a
left join B.entities() b on a.City = b.City
```

`LEFT OUTER JOIN` and `LEFT JOIN` are equivalent.

### 8.3 RIGHT OUTER JOIN

Returns all rows from the right table. Unmatched left-side columns are `null`:

```sql
select a.City, b.Population
from A.entities() a
right join B.entities() b on a.City = b.City
```

### 8.4 Cross Join

Produce a Cartesian product using a tautological condition:

```sql
select a.Name, b.Name
from A.entities() a
inner join B.entities() b on 1 = 1
```

### 8.5 Multiple Joins

Chain multiple joins in a single query:

```sql
select a.City, b.Population, c.Area
from A.entities() a
inner join B.entities() b on a.City = b.City
inner join C.entities() c on a.City = c.City
```

### 8.6 Join Condition Expressions

Join conditions can contain expressions, not just simple column equality:

```sql
inner join B.entities() b on a.Id = b.Id + 1
inner join B.entities() b on a.Population > b.Population + 100
```

### 8.7 Function Calls in Multi-Source Queries

In queries with multiple data sources (joins, applies), each schema has its own method registry. When a function call is **unqualified** (no alias prefix), the engine uses the **method auto-resolution algorithm** described in §8.7.1 to determine which schema owns the method. When auto-resolution cannot determine a single owner, the caller must disambiguate by prefixing the function call with a table alias.

#### 8.7.1 Method Auto-Resolution Algorithm

When a function is called without an alias prefix in a multi-source query, the engine attempts to resolve the owning schema automatically. It does so by trying to bind the method against **every** schema in scope and then applying three rules in order:

1. **Common method rule.** If **all** candidate schemas resolve the method to the **same underlying implementation** (same function identity), the engine picks any one of them. This is the typical case for built-in library methods (e.g., `ToDecimal`, `Concat`, `Contains`) that every schema inherits from a shared base.

2. **Unique method rule.** If **exactly one** candidate schema resolves the method successfully, the engine picks that schema. This applies to methods that are unique to a particular schema's library.

3. **Ambiguity error.** If **two or more** candidate schemas resolve the method to **different implementations**, the engine cannot choose and raises diagnostic **MQ3035** (`AmbiguousMethodOwner`). The caller must add an alias prefix to disambiguate.

> **Aggregate methods** (those decorated with `[InjectGroup]`) follow a separate but analogous auto-resolution path that was already present before this algorithm was introduced. The rules above extend the same principle to all non-aggregate methods.

#### 8.7.2 Method Categories

Understanding how methods are classified helps predict when auto-resolution succeeds or fails:

| Category | Characteristic | Auto-Resolution |
|----------|---------------|-----------------|
| **Aggregate** | Decorated with `[InjectGroup]` (e.g., `Sum`, `Count`, `Avg`) | Resolved via dedicated aggregate inference; same common-method logic applies |
| **Pure utility** | No special injection; stateless (e.g., `ToDecimal`, `Concat`, `Abs`) | Almost always shared across schemas → Rule 1 resolves them |
| **Entity-bound** | Decorated with `[InjectSpecificSource]`; schema-specific | Resolves only in the schema that defines them → Rule 2 or Rule 3 applies |

#### 8.7.3 Examples

**Auto-resolved — common method (Rule 1):**

`ToDecimal` is inherited by every schema from the shared plugin library. Both schemas resolve it to the same implementation, so no alias is needed:

```sql
select e.Department, Sum(ToDecimal(t.Amount)) as total_amount
from separatedvalues.comma('employees.csv', true, 0) e
inner join separatedvalues.comma('transactions.csv', true, 0) t
    on e.Id = t.EmployeeId
group by e.Department
```

**Auto-resolved — unique method (Rule 2):**

If `SpecialParse` exists only in schema A's library but not in schema B's, the engine resolves it to A automatically:

```sql
select SpecialParse(a.RawData), b.Name
from A.entities() a
inner join B.entities() b on a.Id = b.Id
```

**Ambiguous — explicit alias required (Rule 3):**

If schemas A and B each define their own `Transform` with different implementations, the engine raises MQ3035. The caller must specify which schema's `Transform` to use:

```sql
-- ERROR MQ3035: Transform resolves to different implementations in A and B
select Transform(a.Value)
from A.entities() a
inner join B.entities() b on a.Id = b.Id

-- FIX: prefix with the desired schema alias
select a.Transform(a.Value)
from A.entities() a
inner join B.entities() b on a.Id = b.Id
```

#### 8.7.4 Explicit Alias Prefix

Even when auto-resolution would succeed, you can always qualify a function call with an alias prefix. The alias selects the **schema library owner** for that method call. It does **not** require that every argument come from the same alias. For example, this is valid because the aggregate implementation is taken from `countries`, while the value being aggregated comes from `population`:

```sql
select cities.Country, countries.Sum(population.Population)
from A.entities() countries
inner join B.entities() cities on countries.Country = cities.Country
inner join C.entities() population on cities.City = population.City
group by cities.Country
```

This applies to all functions, including aggregation:

```sql
-- Explicit alias on aggregate
select a.Count(a.City)
from A.entities() a
inner join B.entities() b on a.Id = b.Id
group by a.City

-- Auto-resolved alias on aggregate (Count is shared across schemas)
select Count(a.City)
from A.entities() a
inner join B.entities() b on a.Id = b.Id
group by a.City
```

#### 8.7.5 Best Practices

When the same aggregate expression is projected with `AS`, prefer the projected alias in `ORDER BY` instead of repeating the aggregate call. This avoids repeating method binding and reads more naturally:

```sql
select
    e.Department as department,
    Sum(ToDecimal(t.Amount)) as total_amount
from separatedvalues.comma('employees.csv', true, 0) e
inner join separatedvalues.comma('transactions.csv', true, 0) t
    on e.Id = t.EmployeeId
group by e.Department
order by total_amount desc
```

For complex analytics that combine JOINs with GROUP BY, use a CTE to flatten the join first, then aggregate on the single-source CTE:

```sql
with joined as (
    select a.City as City, b.Population as Population
    from A.entities() a
    inner join B.entities() b on a.City = b.City
)
select j.City, Sum(j.Population) as TotalPop
from joined j
group by j.City
```

> **Note:** In single-table queries, function calls never need an alias prefix.

### 8.8 ASOF JOIN

ASOF JOIN matches each left-side row to the **single nearest** right-side row based on an ordered (inequality) column. Unlike a regular inequality join — which returns all matching rows — ASOF JOIN returns **at most one right-side row per left-side row**.

#### 8.8.1 Syntax

```sql
select <columns>
from <left_source> <alias>
asof join <right_source> <alias> on <conditions>
```

The ON clause consists of:
- **Zero or more equality conditions** (partition columns) using `=`
- **Exactly one inequality condition** (the ordering/match column) using `>=`, `>`, `<=`, or `<`

#### 8.8.2 Inequality Operators

The inequality operator determines the match direction:

| Operator | Semantics | Description |
|----------|-----------|-------------|
| `>=` | Nearest right-side row where `left.col >= right.col` | Backward lookup ("as of") |
| `>` | Nearest right-side row where `left.col > right.col` | Strict backward (excludes exact match) |
| `<=` | Nearest right-side row where `left.col <= right.col` | Forward lookup ("what happened next") |
| `<` | Nearest right-side row where `left.col < right.col` | Strict forward (excludes exact match) |

#### 8.8.3 Basic Examples

**Backward lookup — find the nearest right-side row at or before:**

```sql
select a.Name, a.Population, b.Name, b.Population
from A.entities() a
asof join B.entities() b on a.Population >= b.Population
```

For each left-side row, returns the single right-side row whose `Population` is the largest value that is less than or equal to the left-side `Population`.

**With equality partition — match within groups:**

```sql
select e.ErrorTime, e.Service, c.Sha
from errors e
asof join commits c on e.Service = c.Project and e.ErrorTime >= c.AuthorDate
```

For each error, finds the most recent commit **within the same project/service**.

**Forward lookup — find what happened next:**

```sql
select c.AuthorDate, c.Sha, e.ErrorTime, e.Message
from commits c
asof join errors e on c.AuthorDate <= e.ErrorTime
```

For each commit, finds the first error that occurred at or after it.

#### 8.8.4 ASOF LEFT JOIN

When no match exists on the right side, `ASOF JOIN` drops the left row (like `INNER JOIN`). To preserve all left rows with NULLs on the right:

```sql
select c.Sha, c.Message, e.ErrorTime
from commits c
asof left join errors e on c.AuthorDate <= e.ErrorTime
```

`ASOF LEFT OUTER JOIN` is an accepted synonym.

#### 8.8.5 ASOF RIGHT JOIN — Not Supported

`ASOF RIGHT JOIN` is not supported. If attempted, the parser raises an error. The same result can be achieved by swapping table positions.

#### 8.8.6 Match Cardinality and Tie-Breaking

ASOF JOIN produces **at most one right-side row per left-side row**.

When multiple right-side rows have the exact same value on the inequality column (after equality partitioning), the result is **implementation-defined**. Users who need deterministic tie-breaking should add additional equality conditions or pre-filter with CTEs.

#### 8.8.7 NULL Handling

- If the left-side inequality column is `NULL`: **no match** (row dropped for `ASOF JOIN`, NULLs on right for `ASOF LEFT JOIN`)
- If the right-side inequality column is `NULL`: **that right row is never a match candidate**
- If an equality column is `NULL` on either side: **no match** (standard SQL `NULL ≠ NULL` semantics)

#### 8.8.8 ON Clause Validation

The ASOF JOIN ON clause has specific requirements beyond normal joins:

1. **At least one inequality condition MUST exist.** All-equality conditions produce error `AsOfJoinRequiresInequality`.
2. **At most one inequality condition.** Multiple inequalities produce error `AsOfJoinSupportsOnlyOneInequality`.
3. **No OR in the ON clause.** OR breaks the partition-then-search semantics and produces error `AsOfJoinDoesNotSupportOr`.
4. **The inequality MUST reference both sides.** A one-sided inequality produces error `AsOfJoinInequalityMustReferenceBothSides`.
5. **The inequality column type MUST be orderable** (implement `IComparable`). Non-orderable types produce error `AsOfJoinInequalityColumnNotOrderable`.

See [§23](#23-error-catalog) for the full error catalog.

#### 8.8.9 Type Requirements

The inequality column MUST be of a comparable, orderable type. Supported types include:

- All numeric types (`int`, `long`, `decimal`, `double`, `float`, etc.)
- `DateTime`, `DateTimeOffset`, `TimeSpan`
- `string` (lexicographic ordering)
- Any type implementing `IComparable`

#### 8.8.10 Interaction with Other Clauses

| Clause | Behavior |
|--------|----------|
| `WHERE` | Applied **after** the ASOF JOIN, filters the joined result |
| `GROUP BY` | Works normally on the ASOF JOIN result |
| `ORDER BY` | Works normally on the ASOF JOIN result |
| `SKIP` / `TAKE` | Works normally on the ASOF JOIN result |
| CTEs | ASOF JOIN can reference CTE results as either side |
| Multiple JOINs | ASOF JOIN can be chained with other join types (including other ASOF JOINs) |
| `CROSS APPLY` | Can appear before or after ASOF JOIN in the query |

#### 8.8.11 Chaining Multiple ASOF JOINs

Multiple ASOF JOINs can be chained in a single query:

```sql
select a.Name, b.Population, c.Area
from A.entities() a
asof join B.entities() b on a.Population >= b.Population
asof join C.entities() c on a.Population >= c.Area
```

Each ASOF JOIN independently materializes and sorts its right side.

### 8.9 Keywords Are Case-Insensitive

All forms are valid:

```sql
INNER JOIN ... ON ...
inner join ... on ...
Inner Join ... On ...
ASOF JOIN ... ON ...
asof join ... on ...
```

---

## 9. APPLY Clause

### 9.1 CROSS APPLY

`CROSS APPLY` is a correlated join where the right side can reference columns from the left side. Only rows with matches are returned:

```sql
select a.Country, b.City
from A.entities() a
cross apply B.entities(a.Country) b
```

The right-side data source receives values from the left side as method arguments.

### 9.2 OUTER APPLY

Like `CROSS APPLY`, but preserves left-side rows without matches. Unmatched right-side columns are `null`:

```sql
select a.Country, b.City
from A.entities() a
outer apply B.entities(a.Country) b
-- If no match for a.Country, b.City is null
```

Value-type columns from the right side are automatically promoted to nullable (e.g., `decimal` → `decimal?`).

### 9.3 Apply with Row Methods

Call a method on a row that produces a table of results:

```sql
-- Split a string into rows
select b.Value
from schema.first() a
cross apply a.Split(a.Text, ' ') as b

-- Chain method results
select c.Value
from schema.first() a
cross apply a.Split(a.Text, ' ') as b
cross apply b.ToCharArray(b.Value) as c
```

Nested method calls are supported:

```sql
-- Skip first element, take next 6
select b.Value
from schema.first() a
cross apply a.Take(a.Skip(a.Split(a.Text, ' '), 1), 6) as b
```

### 9.4 Apply with Collection Properties

Expand a collection property (array, list) into rows:

```sql
-- Expand an array property
select a.City, b.Value
from schema.first() a
cross apply a.Values as b

-- Expand nested collection
select d.Value
from schema.first() a
cross apply a.Values as b
cross apply b.Values as c
cross apply c.Values as d

-- Expand through property chain
select b.Value
from schema.first() a
cross apply a.ComplexType.PrimitiveValues as b
```

When expanding **primitive arrays** (`int[]`, `double[]`, `List<double>`), each element becomes a row with a `Value` column.

When expanding **complex type arrays** (`MyClass[]`), each property of the complex type is exposed as a column.

### 9.5 Chaining Applies

Multiple applies can be chained. Each can reference results from previous ones:

```sql
select b.Value, c.Value
from schema.first() a
cross apply a.Split(a.Text, ' ') as b
cross apply b.ToCharArray(b.Value) as c

-- Apply results can be filtered and grouped
select b.Length(b.Value), b.Count(Length(b.Value))
from schema.first() a
cross apply a.Split(a.Text, ' ') as b
group by b.Length(b.Value)
```

### 9.6 Multiple Independent Applies

When multiple applies reference the same source, they produce a Cartesian product:

```sql
select b.Value, c.Value
from schema.first() a
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

Aggregate functions operate on groups of rows and return a single value per group. The following table lists common aggregate functions available across all schema providers. This list is illustrative, not exhaustive — additional aggregates may be available depending on the data source.

| Function | Description |
|----------|-------------|
| `Count(column)` | Number of non-null values in the group |
| `Sum(column)` | Sum of numeric values in the group |
| `Avg(column)` | Arithmetic mean of numeric values in the group |
| `Min(column)` | Minimum value in the group |
| `Max(column)` | Maximum value in the group |

To discover all available aggregate functions for a given schema, use the `DESC FUNCTIONS` statement (see [§16.5](#165-describe-schema-functions)):

```sql
desc functions A.entities()
```

All numeric aggregation functions accept any numeric type (`byte`, `short`, `int`, `long`, `float`, `double`, `decimal`, etc.) as input. Different data sources may provide additional aggregate functions beyond the common set.

In a query with only one source, aggregates can be written without a source alias:

```sql
select Country, Sum(Population) from A.entities() group by Country
```

In a query with multiple sources, aggregates are still method calls and therefore must be qualified with a source alias:

```sql
-- ERROR: multi-source aggregate without an owning alias
select e.Department, Sum(ToDecimal(t.Amount))
from separatedvalues.comma('/adventure/data/employees.csv', true, 0) e
inner join separatedvalues.comma('/adventure/data/transactions.csv', true, 0) t
    on e.Id = t.EmployeeId
group by e.Department

-- CORRECT: the alias chooses the aggregate implementation owner
select e.Department, t.Sum(ToDecimal(t.Amount))
from separatedvalues.comma('/adventure/data/employees.csv', true, 0) e
inner join separatedvalues.comma('/adventure/data/transactions.csv', true, 0) t
    on e.Id = t.EmployeeId
group by e.Department
```

The aggregate owner alias and the argument source do not have to match. The alias determines which schema library resolves `Sum`, `Count`, and similar methods.

If multiple aliases can resolve the same unqualified aggregate and they resolve to different implementations, the query is ambiguous and Musoq reports the candidate aliases. In that case, qualify the aggregate explicitly:

```sql
-- ERROR: aggregate owner is ambiguous across aliases
select AggregateMethodB()
from #schema.first() first
inner join #schema.second() second on 1 = 1

-- CORRECT:
select first.AggregateMethodB()
from #schema.first() first
inner join #schema.second() second on 1 = 1
```

### 10.3 GROUP BY Examples

```sql
-- Basic grouping with count
select Country, Count(Country) from A.entities() group by Country

-- Grouping with sum
select Country, Sum(Population) from A.entities() group by Country

-- Multi-column grouping
select Country, City, Count(City) from A.entities() group by Country, City

-- Grouping with expression
select Substr(Name, 0, 2), Count(Name)
from A.entities()
group by Substr(Name, 0, 2)

-- CASE WHEN in GROUP BY
select case when Population >= 500 then 'big' else 'small' end, Count(City)
from A.entities()
group by case when Population >= 500 then 'big' else 'small' end

-- Function-of-column expression in SELECT and GROUP BY (supported)
select ToString(CommittedWhen, 'yyyy-MM', '') as MonthBucket, Count(Sha)
from A.commits()
group by ToString(CommittedWhen, 'yyyy-MM', '')
```

### 10.4 Parent-Level Aggregation

Aggregate functions accept an optional second parameter indicating the "parent level" for multi-column grouping:

```sql
-- Group by Month, City but aggregate at Month level
select SumIncome(Money, 1), SumOutcome(Money, 1)
from A.Entities()
group by Month, City
```

With `group by Month, City`:
- `Count(City)` or `Count(City, 0)` — count at the City (innermost) level
- `Count(City, 1)` — count at the Month (parent) level

### 10.5 Aggregation Without GROUP BY

Using aggregate functions without GROUP BY produces a single-row result aggregating all rows:

```sql
select Count(Name), Sum(Population) from A.entities()
-- Returns one row with totals
```

### 10.6 GROUP BY with Constant

Grouping by a constant treats all rows as a single group:

```sql
select Count(Country) from A.entities() group by 'fake'
-- Equivalent to aggregate without GROUP BY
```

### 10.7 HAVING Clause

HAVING filters groups after aggregation:

```sql
select Name, Count(Name) from A.entities()
group by Name
having Count(Name) >= 2
```

HAVING can use any aggregate expression:

```sql
select City, Sum(Money) from A.entities()
group by City
having Sum(Money) >= 400
```

### 10.8 Non-Aggregated Column Restrictions

In a SELECT with GROUP BY, every non-aggregated column MUST appear in the GROUP BY clause:

```sql
-- VALID: Name is in GROUP BY
select Name, Count(Name) from A.entities() group by Name

-- VALID: Only aggregates, no non-aggregated columns
select Count(Country) from A.entities() group by Country

-- ERROR: Name is not in GROUP BY
select Name, City, Count(1) from A.entities() group by City
-- Throws NonAggregatedColumnInSelectException

-- ERROR: SELECT * with GROUP BY expands to non-aggregated columns
select * from A.entities() group by City
-- Throws NonAggregatedColumnInSelectException
```

### 10.9 NULL in GROUP BY

`NULL` values form their own group:

```sql
-- If some Country values are null, null is its own group
select Country, Count(Country) from A.entities() group by Country
-- Group: null, Count(Country): 0 (Count ignores nulls)
```

Multi-column grouping treats `(POLAND, null)` as distinct from `(POLAND, WARSAW)`:

```sql
select Country, City, Count(City) from A.entities() group by Country, City
```

---

## 11. Window Functions

### 11.1 Overview

Window functions perform calculations across a set of rows that are related to the current row, without collapsing the result into a single output row the way aggregate functions with `GROUP BY` do. Every input row produces exactly one output row. Window functions are specified using an aggregate or ranking function followed by an `OVER` clause that defines the window (partitioning, ordering, or both).

Window functions execute **after** `WHERE` filtering and **after** `GROUP BY` / `HAVING`, but **before** `ORDER BY`, `SKIP`, and `TAKE`.

### 11.2 Syntax

```sql
function_name([arguments]) OVER (
    [PARTITION BY expression {, expression}]
    [ORDER BY field [ASC | DESC] {, field [ASC | DESC]}]
)
```

Or with a named window reference:

```sql
function_name([arguments]) OVER window_name
```

Named windows are declared in a `WINDOW` clause that appears after the `FROM` / `WHERE` / `GROUP BY` clauses:

```sql
SELECT col, Sum(col2) OVER w
FROM schema.table()
WINDOW w AS (PARTITION BY col3 ORDER BY col4)
```

**Components:**

| Component | Required | Description |
|-----------|----------|-------------|
| `PARTITION BY` | No | Divides rows into partitions. The window function is computed independently within each partition. When omitted, all rows form a single partition. |
| `ORDER BY` | No | Defines the logical ordering of rows within each partition. Required for ranking functions and running aggregates. When omitted for aggregate functions, the function computes over the entire partition as a whole. |

### 11.3 PARTITION BY

`PARTITION BY` divides the result set into groups (partitions). The window function resets and is computed independently for each partition.

```sql
-- Count employees per department
select Name, City, Count(Name) over (partition by City) from #a.entities()
```

Multiple partition columns are supported — rows are grouped by the composite key:

```sql
select Name, Sum(Population) over (partition by Country, City) from #a.entities()
```

When `PARTITION BY` is omitted, all rows belong to a single partition:

```sql
-- Running sum across all rows
select Name, Sum(Population) over (order by Name) from #a.entities()
```

### 11.4 ORDER BY within Window Specification

`ORDER BY` inside the `OVER` clause determines the logical row ordering within each partition. Both `ASC` (default) and `DESC` are supported.

- **Ranking functions** (`RowNumber`, `Rank`, `DenseRank`) require `ORDER BY` — it determines the assignment of ranks.
- **Aggregate functions** (`Sum`, `Count`, `Avg`, `Min`, `Max`) behave differently depending on whether `ORDER BY` is present:
  - **With `ORDER BY`**: Computes a running (cumulative) result from the first row in the partition up to the current row.
  - **Without `ORDER BY`**: Computes the result over the entire partition — every row in the partition receives the same value.
- **Offset functions** (`Lag`, `Lead`) require `ORDER BY` — it defines which row is "previous" or "next".

```sql
-- Running sum (with ORDER BY)
select Name, Sum(Population) over (order by Name) from #a.entities()

-- Partition total (without ORDER BY)
select Name, City, Sum(Population) over (partition by City) from #a.entities()

-- Descending order
select Name, RowNumber() over (order by Name desc) from #a.entities()
```

### 11.5 Supported Window Functions

#### 11.5.1 Ranking Functions

Ranking functions assign an ordinal position to each row within its partition based on `ORDER BY`.

| Function | Return Type | Description |
|----------|-------------|-------------|
| `RowNumber()` | `long` | Assigns a unique sequential integer to each row within the partition, starting at 1. No ties — every row gets a distinct number. |
| `Rank()` | `long` | Assigns the same rank to rows with equal `ORDER BY` values. Leaves gaps: if two rows share rank 2, the next rank is 4. |
| `DenseRank()` | `long` | Like `Rank()`, but without gaps: if two rows share rank 2, the next rank is 3. |
| `Ntile(n)` | `long` | Distributes rows into `n` roughly equal-sized groups (buckets) within the partition. Assigns a bucket number from 1 to `n`. If the partition size is not evenly divisible, earlier buckets receive one extra row. |

```sql
-- RowNumber: 1, 2, 3, 4, 5
select Name, RowNumber() over (order by Name) from #a.entities()

-- Rank with ties: 1, 2, 2, 4
select Name, Rank() over (order by Population) from #a.entities()

-- DenseRank without gaps: 1, 2, 2, 3
select Name, DenseRank() over (order by Population) from #a.entities()

-- Ntile: distribute 5 rows into 3 buckets → 1, 1, 2, 2, 3
select Name, Ntile(3) over (order by Name) from #a.entities()
```

`RowNumber`, `Rank`, and `DenseRank` take no arguments — the ordering is determined solely by the `ORDER BY` clause. `Ntile` takes a single integer argument specifying the number of buckets.

#### 11.5.2 Offset Functions

Offset functions access values from rows at a fixed distance from the current row within the partition.

| Function | Return Type | Description |
|----------|-------------|-------------|
| `Lag(column [, offset [, default]])` | Nullable type of column | Returns the value of `column` from the row that is `offset` rows **before** the current row. Returns `NULL` (or `default` if specified) when no such row exists. Default `offset` is 1. |
| `Lead(column [, offset [, default]])` | Nullable type of column | Returns the value of `column` from the row that is `offset` rows **after** the current row. Returns `NULL` (or `default` if specified) when no such row exists. Default `offset` is 1. |

```sql
-- Previous row's Population (NULL for the first row)
select Name, Lag(Population) over (order by Name) from #a.entities()

-- Next row's Population (NULL for the last row)
select Name, Lead(Population) over (order by Name) from #a.entities()
```

**NULL semantics for offset functions:**

- When a value-type column (e.g., `decimal`, `int`, `long`) is accessed via `Lag` or `Lead`, the return type is automatically promoted to its nullable equivalent (e.g., `decimal?`, `int?`, `long?`). This allows `NULL` to represent the absence of a previous or next row without runtime errors.
- Reference-type columns (e.g., `string`) are already nullable and need no promotion.

#### 11.5.3 Aggregate Window Functions

Standard aggregate functions can be used as window functions when combined with `OVER`:

| Function | Return Type | Description |
|----------|-------------|-------------|
| `Sum(column)` | `decimal` | Sum of values. Running sum if `ORDER BY` is present; partition total if not. Numeric inputs are promoted to `decimal`. |
| `Count(column)` | `int` | Count of non-null values. Running count with `ORDER BY`; partition count without. |
| `Avg(column)` | `decimal` | Average of values. Running average with `ORDER BY`; partition average without. Numeric inputs are promoted to `decimal`. |
| `Min(column)` | Same as column type | Minimum value. Running minimum with `ORDER BY`; partition minimum without. |
| `Max(column)` | Same as column type | Maximum value. Running maximum with `ORDER BY`; partition maximum without. |

```sql
-- Running sum ordered by Name
select Name, Sum(Population) over (order by Name) from #a.entities()

-- Count per partition
select Name, City, Count(Name) over (partition by City) from #a.entities()

-- Running average
select Name, Avg(Population) over (order by Name) from #a.entities()

-- Partition minimum and maximum
select Name, City, Min(Population) over (partition by City) from #a.entities()
select Name, City, Max(Population) over (partition by City) from #a.entities()
```

#### 11.5.4 Value Access Functions

Value access functions retrieve a specific row's value from within the partition.

| Function | Return Type | Description |
|----------|-------------|-------------|
| `FirstValue(column)` | Same as column type | Returns the first value in the partition (determined by `ORDER BY`). All rows in the partition receive the same result. |
| `LastValue(column)` | Same as column type | Running last with `ORDER BY`: returns the current row's value. Without `ORDER BY`: returns the last value in the partition (all rows same). |
| `NthValue(column, n)` | Same as column type | Returns the value from the `n`-th row in the partition. Returns `NULL` when fewer than `n` rows have been seen. `n` is 1-based. |

```sql
-- First value in partition
select Name, FirstValue(Name) over (order by Name) from #a.entities()

-- Running last value (equals current row's value)
select Name, LastValue(Name) over (order by Name) from #a.entities()

-- Partition-wide last value (all rows same)
select Name, LastValue(Population) over () from #a.entities()

-- Second value in partition (NULL for the first row)
select Name, NthValue(Name, 2) over (order by Name) from #a.entities()

-- NthValue per partition
select Name, City, NthValue(Name, 2) over (partition by City order by Name)
from #a.entities()
```

**NthValue semantics with ORDER BY (running mode):**
- Before the `n`-th row, `NthValue` returns `NULL`.
- From the `n`-th row onward, it returns the value seen at position `n`.

**NthValue semantics without ORDER BY:**
- All rows receive the same value from the `n`-th row in the partition, or `NULL` if the partition has fewer than `n` rows.

### 11.6 Underscore Naming Variants

Function names are case-insensitive and underscore-insensitive. The following pairs are equivalent:

| Camel Case | Underscore Form |
|------------|-----------------|
| `RowNumber()` | `ROW_NUMBER()` |
| `DenseRank()` | `DENSE_RANK()` |
| `Ntile(n)` | `NTILE(n)` |
| `FirstValue(column)` | `FIRST_VALUE(column)` |
| `LastValue(column)` | `LAST_VALUE(column)` |
| `NthValue(column, n)` | `NTH_VALUE(column, n)` |

Both forms produce identical results:

```sql
-- These two queries are equivalent
select Name, RowNumber() over (order by Name) from #a.entities()
select Name, ROW_NUMBER() over (order by Name) from #a.entities()
```

### 11.7 WHERE Clause Interaction

`WHERE` filtering occurs **before** window function computation. Rows excluded by `WHERE` are not visible to the window function:

```sql
-- Only rows with Population > 150 participate in the window
select Name, RowNumber() over (order by Name)
from #a.entities()
where Population > 150
```

If the original table has 5 rows and 3 satisfy the `WHERE` predicate, the window function operates on 3 rows, and `RowNumber()` produces values 1, 2, 3.

### 11.8 Evaluation Order

Window functions occupy a specific position in the query evaluation pipeline:

1. `FROM` — data source enumeration
2. `WHERE` — row-level filtering
3. `GROUP BY` / `HAVING` — aggregation (if present)
4. **Window functions** — computed over the filtered / grouped result
5. `ORDER BY` — final ordering of the output
6. `SKIP` / `TAKE` — pagination

This means window functions cannot appear in `WHERE` or `HAVING` clauses. They can only appear in the `SELECT` list.

### 11.9 Restrictions

The following features are **not** currently supported:

- **Frame specifications** (`ROWS BETWEEN`, `RANGE BETWEEN`) — window functions operate over a fixed implicit frame. See section 11.11 for the exact frame semantics and how they differ from standard SQL.
- **`QUALIFY`** — there is no `QUALIFY` clause for filtering on window function results. Use a CTE as a workaround:
  ```sql
  with ranked as (
      select Name, RowNumber() over (order by Name) as rn from #a.entities()
  )
  select Name, rn from ranked where rn <= 3
  ```
- **Recursive or nested window functions** — a window function cannot reference another window function in its arguments.
- **Window functions in `WHERE` or `HAVING`** — only allowed in the `SELECT` list.

### 11.10 Custom Window Functions

Data-source plugins can register custom window functions beyond the built-in set. Once registered, they are used with the standard `OVER` syntax like any other window function:

```sql
select Name, RunningProduct(Population) over (partition by City order by Name)
from #a.entities()
```

Custom window functions support `PARTITION BY`, `ORDER BY`, and multi-argument signatures — the same capabilities as the built-in functions.

### 11.11 Frame Semantics and Differences from Standard SQL

Musoq window functions use **ROWS semantics**, not the RANGE semantics that PostgreSQL and the SQL standard default to. This section documents the exact behavior and where it diverges from standard SQL.

#### 11.11.1 Implicit Frame

Musoq does not support explicit frame specifications (`ROWS BETWEEN ... AND ...`, `RANGE BETWEEN ... AND ...`). Instead, every window function operates under a fixed implicit frame:

| Condition | Implicit Frame (Musoq) | PostgreSQL Default |
|-----------|------------------------|--------------------|
| `ORDER BY` present | `ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW` | `RANGE BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW` |
| `ORDER BY` absent | Entire partition (all rows receive the same value) | Entire partition (same) |

The difference between ROWS and RANGE only matters **when ORDER BY values contain ties** (duplicate values).

#### 11.11.2 ROWS vs RANGE: Behavior with Tied ORDER BY Values

**ROWS mode** (Musoq): Each row is processed individually in sequence. Even when multiple rows share the same ORDER BY value, each row accumulates independently and may produce a different result.

**RANGE mode** (PostgreSQL default): All rows with the same ORDER BY value are treated as *peers*. Peers receive the **same** aggregate result — the value computed as if all peers were included.

Example with `SUM(val) OVER (ORDER BY category)`:

| Row | category | val | Musoq (ROWS) | PostgreSQL (RANGE) |
|-----|----------|-----|:------------:|:------------------:|
| 1 | A | 10 | 10 | 10 |
| 2 | B | 20 | **30** | **50** |
| 3 | B | 20 | **50** | **50** |
| 4 | C | 30 | 80 | 80 |

- Rows 2 and 3 both have `category = B`. In RANGE mode, they are peers and both receive 50 (sum of all rows up to and including all B's). In ROWS mode, row 2 gets 30 (A + first B) and row 3 gets 50 (A + both B's).
- Rows 1 and 4 have unique ORDER BY values, so both modes produce the same result.

**When ORDER BY values are all distinct, ROWS and RANGE produce identical results.** The divergence only appears with duplicate ORDER BY values.

#### 11.11.3 Which Functions Are Affected

| Function Category | Affected by ROWS vs RANGE? | Notes |
|-------------------|:--------------------------:|-------|
| `RowNumber()` | No | Always assigns distinct sequential numbers — no tie concept |
| `Rank()`, `DenseRank()` | No | Tie-aware by definition — identical behavior in both modes |
| `Ntile(n)` | No | Bucket assignment is position-based in all SQL engines |
| `Lag()`, `Lead()` | No | Offset functions are position-based (ROWS) by definition in standard SQL |
| `Sum()`, `Count()`, `Avg()` | **Yes** | Running aggregates differ when ORDER BY has ties |
| `Min()`, `Max()` | **Yes** | Running min/max differ when ORDER BY has ties |
| `FirstValue()` | No | Returns the first row in the partition — same in both modes |
| `LastValue()` | **Yes** | ROWS: current row is last. RANGE: last peer is last |
| `NthValue()` | **Yes** | ROWS: Nth physical row. RANGE: depends on peer-group boundary |

#### 11.11.4 NULL Ordering

Musoq does not support `NULLS FIRST` / `NULLS LAST` syntax. The NULL ordering is fixed:

| Sort Direction | NULL Position | Matches PostgreSQL Default? |
|----------------|:------------:|:---------------------------:|
| `ASC` | First | Yes |
| `DESC` | Last | No (PostgreSQL puts NULLs first in DESC) |

NULL values in `PARTITION BY` columns are grouped together as their own partition, consistent with standard SQL.

#### 11.11.5 Unsupported Standard SQL Window Features

| Feature | Standard SQL | Musoq |
|---------|-------------|-------|
| `ROWS BETWEEN ... AND ...` | Explicit row-based frame | Not supported |
| `RANGE BETWEEN ... AND ...` | Explicit value-based frame | Not supported |
| `GROUPS BETWEEN ... AND ...` | Explicit peer-group-based frame | Not supported |
| `EXCLUDE CURRENT ROW` | Excludes current row from frame | Not supported |
| `EXCLUDE GROUP` | Excludes current row's peer group | Not supported |
| `EXCLUDE TIES` | Excludes peers of current row (keeps current) | Not supported |
| `NULLS FIRST` / `NULLS LAST` | Explicit NULL ordering | Not supported (fixed behavior) |
| `FILTER (WHERE ...)` | Conditional window aggregation | Not supported |
| `QUALIFY` | Filter on window function results | Not supported (use CTE workaround) |
| `PERCENT_RANK()` | Relative rank as fraction | Not implemented |
| `CUME_DIST()` | Cumulative distribution | Not implemented |

#### 11.11.6 Practical Implications

For most real-world queries, Musoq produces identical results to PostgreSQL because:

1. **Ranking functions** (`RowNumber`, `Rank`, `DenseRank`) are unaffected by frame mode.
2. **Offset functions** (`Lag`, `Lead`) are inherently position-based in all SQL engines.
3. **Aggregates without ORDER BY** compute the whole partition — identical in all modes.
4. **Aggregates with ORDER BY on a unique column** (timestamps, IDs) produce identical results since there are no ties.

The divergence only appears in the specific combination of **running aggregates + non-unique ORDER BY values**. If you need peer-aware behavior, add a tiebreaker column to ORDER BY to make values unique, or use `PARTITION BY` on the grouping column instead.

---

## 12. Set Operations

### 12.1 Syntax

Set operations MUST specify explicit key columns:

```sql
query1 UNION (key_col1, key_col2) query2
query1 UNION ALL (key_col1) query2
query1 EXCEPT (key_col1) query2
query1 INTERSECT (key_col1) query2
```

Standard SQL forms without a key list are not accepted:

```sql
query1 UNION query2
query1 UNION ALL query2
```

If you omit the parenthesized key list, Musoq raises an error telling you to rewrite the operator as `UNION (<key_columns>)`, `UNION ALL (<key_columns>)`, `EXCEPT (<key_columns>)`, or `INTERSECT (<key_columns>)`.

The key columns specify which columns are used to determine row identity for deduplication, difference, or intersection.

### 12.2 UNION

Combines results from two queries, removing duplicates identified by the key columns:

```sql
select Name from A.Entities()
union (Name)
select Name from B.Entities()
```

### 12.3 UNION ALL

Combines results preserving all rows including duplicates:

```sql
select Name from A.Entities()
union all (Name)
select Name from B.Entities()
```

### 12.4 EXCEPT

Returns rows from the first query that do not appear in the second query:

```sql
select Name from A.Entities()
except (Name)
select Name from B.Entities()
```

### 12.5 INTERSECT

Returns only rows that appear in both queries:

```sql
select Name from A.Entities()
intersect (Name)
select Name from B.Entities()
```

### 12.6 Chaining Set Operations

Three or more queries can be chained:

```sql
select Name from A.Entities()
union all (Name)
select Name from A.Entities()
union all (Name)
select Name from A.Entities()
union all (Name)
select Name from A.Entities()
union all (Name)
select Name from A.Entities()
```

### 12.7 Different Source Columns

Source columns can differ if aliases unify them:

```sql
select Name from A.Entities()
union (Name)
select City as Name from B.Entities()   -- City aliased as Name
```

### 12.8 SKIP/TAKE per Subquery

Each subquery in a set operation can have its own SKIP/TAKE:

```sql
select Name from A.Entities() skip 1
union (Name)
select Name from B.Entities() skip 2
```

### 12.9 Set Operations in CTEs

```sql
with p as (
    select 1 as Id, 'First' as Name from A.Entities()
    union all (Name)
    select 2 as Id, 'Second' as Name from A.Entities()
    union all (Name)
    select 3 as Id, 'Third' as Name from A.Entities()
)
select Id, Name from p
```

### 12.10 ORDER BY with Set Operations (Deviation from Standard SQL)

In standard SQL, an `ORDER BY` clause placed after the last query in a set operation applies to the **entire combined result**:

```sql
-- Standard SQL interpretation:
-- ORDER BY sorts the combined UNION result
SELECT City FROM A WHERE Money > 200
UNION
SELECT City FROM A WHERE Money <= 200
ORDER BY City DESC
```

Musoq does **not** follow this convention. Because `ORDER BY` is syntactically part of each individual query (not a clause on the set operator), writing `ORDER BY` after a `UNION` attaches it to the **rightmost query**, not to the combined result.

```sql
-- Musoq interpretation:
-- ORDER BY applies to the second SELECT only, BEFORE the UNION combines rows
select City from #A.Entities() where Money > 200
union (City)
select City from #A.Entities() where Money <= 200
order by City desc    -- sorts only the right-hand query
```

This means the final output order after the UNION is **not guaranteed** to be sorted.

**Workaround** — wrap the set operation in a CTE and apply `ORDER BY` on the outer query:

```sql
with combined as (
    select City from #A.Entities() where Money > 200
    union (City)
    select City from #A.Entities() where Money <= 200
)
select City from combined order by City desc
```

> **Note:** This is a known deviation. Future versions may support `ORDER BY` as a clause over the combined set operation result.

---

## 13. ORDER BY, SKIP, TAKE

### 13.1 ORDER BY

Sorts result rows. Default direction is ascending:

```sql
select Name from A.entities() order by Name            -- ascending (default)
select Name from A.entities() order by Name asc        -- explicit ascending
select Name from A.entities() order by Name desc       -- descending
```

Multi-column ordering with mixed directions:

```sql
select City, Population from A.entities()
order by Population desc, City asc
```

ORDER BY can use expressions:

```sql
select Name, Population from A.entities() order by Population * -1
```

ORDER BY uses **ordinal (case-sensitive) comparison** for strings: uppercase letters sort before lowercase (`'A'` < `'a'`).

> **Set operations:** `ORDER BY` placed after a set operation (e.g., `UNION`) applies to the rightmost query only, not the combined result. This is a deviation from standard SQL — see §12.10 for details and the CTE workaround.

### 13.2 ORDER BY with SELECT Aliases

SELECT aliases defined with `AS` can be referenced directly in ORDER BY:

```sql
select City, Money as Amount from A.entities() order by Amount desc
```

This also works with computed expressions:

```sql
select City, Money * 2 as DoubledMoney from A.entities() order by DoubledMoney desc
```

And with aggregate functions after GROUP BY:

```sql
select City, Sum(Money) as TotalRevenue
from A.entities()
group by City
order by TotalRevenue desc
```

This is the recommended form in multi-source grouped queries as well:

```sql
select
    e.Department as department,
    t.Sum(ToDecimal(t.Amount)) as total_amount
from separatedvalues.comma('/adventure/data/employees.csv', true, 0) e
inner join separatedvalues.comma('/adventure/data/transactions.csv', true, 0) t
    on e.Id = t.EmployeeId
group by e.Department
order by total_amount desc
```

Alias lookup is **case-insensitive**:

```sql
select City as CITYNAME, Money as amount from A.entities() order by Amount desc
-- "Amount" matches alias "amount"
```

> **Note:** Only explicit aliases (those declared with `AS`) can be referenced in ORDER BY. Auto-generated column names (where no `AS` is used) must be referenced by their expression directly.

When an explicit alias is used in `ORDER BY`, ordering applies to the aliased **SELECT expression result**, not to an unrelated source column with the same name.

### 13.3 SKIP

Skip the first N rows of the result:

```sql
select Name from A.entities() skip 2
```

If SKIP exceeds the number of rows, zero rows are returned (no error).

### 13.4 TAKE

Take the first N rows of the result:

```sql
select Name from A.entities() take 3
```

If TAKE exceeds the number of available rows, all available rows are returned (no error).

### 13.5 SKIP + TAKE (Pagination)

Combine for pagination:

```sql
select Name from A.entities() order by Name skip 10 take 5
-- Skip first 10, return next 5
```

### 13.6 Interaction with GROUP BY and HAVING

ORDER BY, SKIP, and TAKE are applied after GROUP BY and HAVING:

```sql
select City, Sum(Money) from A.entities()
group by City
having Sum(Money) >= 400
order by City
skip 1
take 2
```

---

## 14. Common Table Expressions (CTEs)

### 14.1 Basic Syntax

```sql
WITH cte_name AS (
    query
)
SELECT ... FROM cte_name
```

### 14.2 Simple CTE

```sql
with p as (
    select City, Country from A.entities()
)
select Country, City from p
```

### 14.3 Star Expansion from CTE

```sql
with p as (
    select City, Country from A.entities()
)
select * from p    -- expands to City, Country
```

### 14.4 CTE with Aggregation

```sql
with summary as (
    select Country, Sum(Population) from A.entities() group by Country
)
select * from summary
```

Aggregation can also occur on a CTE reference:

```sql
with raw as (
    select Population, Country from A.entities()
)
select Country, Sum(Population) from raw group by Country
```

### 14.5 Multiple CTEs

Define multiple CTEs separated by commas:

```sql
with
    cities as (select City, Country from A.entities()),
    countries as (select distinct Country from A.entities())
select * from cities
```

### 14.6 CTE with Set Operations

```sql
with combined as (
    select Name from A.Entities()
    union (Name)
    select Name from B.Entities()
)
select * from combined
```

### 14.7 CTE with JOIN

```sql
with p as (select City, Country from A.entities())
select p.City, b.Population
from p
inner join B.entities() b on p.City = b.City
```

### 14.8 Limitations

- **No recursive CTEs**: Musoq does not support `WITH RECURSIVE` or self-referencing CTEs.
- **Duplicate aliases**: Using the same alias for two tables within a CTE inner expression throws `AliasAlreadyUsedException`.

```sql
-- ERROR: Duplicate alias 'a'
with p as (
    select 1 from A.entities() a inner join A.entities() a on 1 = 1
)
select * from p
```

---

## 15. TABLE and COUPLE Statements

The TABLE and COUPLE statements are summarized here. For the complete specification including all supported types, error handling, and integration patterns, see *Musoq TABLE/COUPLE Statements Specification* (`musoq-table-couple-spec.md`).

### 15.1 TABLE Definition

Defines a named table structure with typed columns:

```sql
table TableName {
    Column1: type1,
    Column2: type2,
    Column3: type3?       -- ? suffix for nullable
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
    ProductName: string,
    Price: decimal,
    Date: datetimeoffset?
};
```

### 15.2 COUPLE Statement

Binds a schema method to a table structure, creating a new data source alias:

```sql
couple schema.Method with table TableName as AliasName;
```

**Complete Example:**

```sql
table DummyTable {
    Name: string
};
couple A.Entities with table DummyTable as SourceOfDummyRows;
select Name from SourceOfDummyRows();
```

With parameters:

```sql
select Name from SourceOfDummyRows(true, 'filter');
```

### 15.3 Purpose

TABLE and COUPLE are used when:
- The data source returns untyped or dynamically-typed rows
- You want to project a subset of columns with explicit types
- You need to create a named alias for a complex data source expression

---

## 16. DESC Statement


### 16.1 Describe a Schema

List available methods exposed by a schema:

```sql
desc A
```

Returns a single-column table named `Name`. Each row contains one available schema method (for example `empty`, `entities`).

### 16.2 Describe a Method (Overloads)

```sql
desc A.entities
```

Returns one row per available overload of the selected method.

The result shape is:

- `Name`
- `Param 0`, `Param 1`, ... as needed to fit the widest overload

Each parameter cell contains `ParameterName: Full.Type.Name`. Overloads with fewer parameters leave the remaining parameter columns empty.

### 16.3 Describe a Specific Constructor Result

Describe the columns produced by a concrete constructor call:

```sql
desc A.entities()
```

Returns a table with columns: `Name`, `Index`, `Type`.

Arguments may be provided when the schema method is overloaded and the engine needs to identify a specific constructor:

```sql
desc dynamic.method(0, 'test', 10.5d)
```

The argument values are matched against the selected constructor signature. The returned table describes the row shape produced by that constructor.

### 16.4 Describe a Specific Column or Nested Property

Inspect the structure behind a complex column, private table, or nested property path:

```sql
desc A.entities() column Array             -- describe an array column
desc A.entities() column Self              -- describe a complex object column
desc A.entities() column Children          -- describe a complex type column
desc A.entities() column Self.Children     -- nested property path
desc A.entities() column Self.Other.Children  -- deep nested path
desc A.entities() column Self.Dictionary   -- IEnumerable<T> path
```

Returns a table with columns: `Name`, `Index`, `Type`.

Rules:

- Root column names and nested property names are matched case-insensitively.
- Property paths may be arbitrarily deep: `A.B.C.D` is valid if each step resolves.
- The final target may be a complex object, an array, or any `IEnumerable<T>`.
- If the final target is an array or `IEnumerable<T>`, the output describes the element type.
- If the final target is a complex object, the output describes that object's properties so you can continue exploratory navigation.
- If the final target is a primitive, `string`, or `object`, the statement fails.
- If any path segment does not exist, the statement fails.

For nested descriptions, the `Index` column refers to the original top-level column index from the described table.

### 16.5 Describe Schema Functions

```sql
desc functions A
desc functions A.entities
desc functions A.entities()
desc functions A.entities('filter')    -- with arguments
```

Returns a table with columns: `Method`, `Description`, `Category`, `Source`.

This statement lists the query functions available for the schema context. A `.method` or `.method(...)` suffix is accepted by the parser, but it does not narrow the function list. These forms behave the same as `desc functions A`.

Only user-visible query functions are returned. Internal helpers and aggregation-set helpers are excluded.

### 16.6 General DESC Rules

- `DESC`, `FUNCTIONS`, and `COLUMN` are case-insensitive.
- Optional trailing semicolons are accepted.
- Normal statement whitespace, comments, and multiline formatting are accepted.

---

## 17. Reordered Query Syntax

### 17.1 FROM-First Syntax

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

### 17.2 Standard vs. Reordered Clause Order

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

### 17.3 Examples

```sql
-- Simple
from A.Entities() select City, Country

-- With WHERE
from A.Entities() where Country = 'POLAND' select City, Country

-- With GROUP BY
from A.Entities() group by Country select Country, Sum(Population)

-- Full combination
from A.Entities() a
inner join B.Entities() b on a.City = b.City
where a.Country = 'POLAND'
group by a.Country
select a.Country, Sum(b.Population)
order by a.Country
skip 1
take 5

-- Inside a CTE
with cte as (
    from A.Entities() where Country = 'POLAND' select City, Country
)
select * from cte
```

---

## 18. Built-in Functions

### 18.1 Conventions

- Most functions return `null` when any required parameter is `null` (NULL propagation).
- Function names are **case-sensitive**.
- Functions can be called standalone or with table alias prefix: `Length(Name)` or `a.Length(a.Name)`.

### 18.2 Discovering Available Functions

Musoq provides a rich library of built-in functions covering string manipulation, math, date/time, type conversion, validation, JSON/XML processing, cryptography, compression, bitwise operations, networking utilities, and collections. Additionally, each data source may define its own functions.

Because the set of available functions depends on which data sources are in use, the authoritative way to discover them is the `DESC FUNCTIONS` statement (see [§16.5](#165-describe-schema-functions)):

```sql
-- List all functions available for a schema
desc functions A

-- List all functions available in the context of a specific method
desc functions A.entities()
```

The result includes the method name, description, category, and source for each function.

### 18.3 Function Categories

Built-in functions are organized into the following categories:

- **String** — text manipulation, searching, formatting, and encoding operations
- **Math** — arithmetic, rounding, trigonometry, and numeric utility operations
- **Date and Time** — component extraction, arithmetic, formatting, and parsing of temporal values
- **Type Conversion** — converting between numeric types, strings, and encoded representations
- **Validation** — verifying that values conform to expected formats
- **JSON/XML** — serialization, deserialization, extraction, and formatting of structured text
- **Cryptography and Hashing** — hash computation, checksums, and message authentication
- **Compression** — compressing and decompressing byte data
- **Binary and Bitwise** — byte-level conversion and bitwise logical operations
- **Network and Utility** — IP address operations, identifier generation, and encoding utilities
- **Generic and Collection** — row numbering, null handling, and collection transformation operations

Use `desc functions` to see the full list with signatures and descriptions for any given schema context.

---

## 19. NULL Semantics

### 19.1 NULL Propagation

Most expressions involving `null` produce `null`:

```sql
select null + 1 from system.dual()        -- null
select null = null from system.dual()      -- null (not true)
```

### 19.2 NULL in Comparisons

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

### 19.2.1 NULL Comparisons Inside `CASE WHEN`

`CASE WHEN` MUST use three-valued logic: `null = null` evaluates to `null`, not `true`. Query authors MUST use explicit null predicates (`IS NULL` / `IS NOT NULL`) to test for null values.

### 19.3 NULL in LIKE, RLIKE, CONTAINS

| Expression | Result |
|------------|--------|
| `null LIKE '%test%'` | `false` |
| `null RLIKE 'pattern'` | `false` |
| `null NOT LIKE '%test%'` | `true` |
| `CONTAINS(null, 'a', 'b')` | `false` |
| `CONTAINS(null, null, 'a')` | `true` (null found in list) |

### 19.4 NULL in GROUP BY

`NULL` values form their own distinct group:

```sql
-- Data: (POLAND, WARSAW), (POLAND, null), (GERMANY, BERLIN)
select Country, City, Count(1) from A.entities() group by Country, City
-- Groups: (POLAND, WARSAW), (POLAND, null), (GERMANY, BERLIN)
```

### 19.5 NULL from OUTER Joins

When `LEFT JOIN`, `OUTER APPLY`, or `ASOF LEFT JOIN` produces no match for a left-side row, right-side columns are `null`. Value types are automatically promoted to nullable:

```sql
-- If no match, b.Population becomes decimal? with value null
select a.City, b.Population
from A.entities() a
left join B.entities() b on a.City = b.City
```

### 19.6 NULL in ASOF JOIN

ASOF JOIN has specific NULL behavior for inequality and equality columns:

- If the **left-side inequality column** is `NULL`: no match (row dropped for `ASOF JOIN`, NULLs on right for `ASOF LEFT JOIN`)
- If the **right-side inequality column** is `NULL`: that right row is never a match candidate
- If an **equality (partition) column** is `NULL` on either side: no match (standard SQL `NULL ≠ NULL`)

```sql
-- Left row with NULL inequality key produces no match
select a.Name, b.Name
from A.entities() a
asof left join B.entities() b on a.Population >= b.Population
-- If a.Population is NULL, b columns are NULL
```

### 19.7 NULL-Related Functions

Musoq provides functions for working with `NULL` values, such as coalescing, null-checking, and replacing nulls with defaults. To discover all available NULL-handling functions and their signatures, use `desc functions` (see [§16.5](#165-describe-schema-functions)).

### 19.8 NULL in Functions

Most built-in functions return `null` when any required parameter is `null`:

```sql
select Trim(null) from system.dual()        -- null
select ToUpper(null) from system.dual()      -- null
select Abs(null) from system.dual()          -- null
select Concat(null, 'text') from system.dual()  -- null
```

---

## 20. String Comparison Semantics

Musoq uses different comparison strategies depending on context:

### 20.1 Case-Insensitive Contexts

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

### 20.2 Case-Sensitive (Ordinal) Contexts

These operations use **ordinal (case-sensitive)** comparison:

| Operation | Example |
|-----------|---------|
| `=` / `<>` | `'Hello' = 'hello'` → false |
| `>` / `>=` / `<` / `<=` | `'b' > 'a'` → true (ordinal) |
| `ORDER BY` | `'A'` sorts before `'a'` (ASCII order) |
| `GROUP BY` | `'Hello'` and `'hello'` are different groups |
| `DISTINCT` | `'Hello'` and `'hello'` are different values |

### 20.3 Achieving Case-Insensitive Grouping

To group or deduplicate case-insensitively, normalize with `ToLower()` or `ToUpper()`:

```sql
select ToLower(Name), Count(Name) from A.entities() group by ToLower(Name)
select distinct ToLower(Name) from A.entities()
```

### 20.4 Unicode Support

Full Unicode support across all operations including LIKE, GROUP BY, ORDER BY, and all string functions. Tested with: Polish, Russian, French, Japanese (Hiragana/Katakana/Kanji), Chinese (Simplified/Traditional), Korean, Arabic, German, Thai, Hebrew, Hindi, Turkish, Greek, Ukrainian, Vietnamese, and emoji.

---

## 21. Array and Property Access

### 21.1 Array Indexing

Array elements are accessed with bracket notation (0-based):

```sql
select Array[0] from A.entities()     -- first element
select Array[2] from A.entities()     -- third element
```

#### Negative Indexing

Negative indices count from the end (Python-style wrapping):

```sql
select Array[-1] from A.entities()    -- last element
select Array[-2] from A.entities()    -- second to last
```

#### Out-of-Bounds Access

Out-of-bounds access **never throws an exception**. It returns the default value for the element type:

| Scenario | Result |
|----------|--------|
| `int_array[100]` (out of bounds) | `0` (default int) |
| `string_array[100]` | `null` |
| `Array[-100]` (excessive negative) | Wraps modularly: `effectiveIndex = ((index % length) + length) % length` |

### 21.2 String Character Access

Strings support bracket indexing to access individual characters:

```sql
select Name[0] from A.entities()      -- first character
select Name[-1] from A.entities()     -- last character
```

Out-of-bounds on strings returns `'\0'` (null character). Null strings return `'\0'`.

### 21.3 Dictionary Key Access

Access dictionary values by key:

```sql
select Dict['key_name'] from A.entities()
```

Missing keys return `null` (no exception).

### 21.4 Property Navigation

Access nested object properties with dot notation:

```sql
select Self.Name from A.entities()              -- single level
select Self.Self.Name from A.entities()          -- two levels
select Self.Self.Array from A.entities()         -- deep property
select Self.Array[2] from A.entities()           -- property + index
select Inc(Self.Array[2]) from A.entities()      -- function on indexed property
```

Accessing a non-existing property throws `UnknownPropertyException` at compile time.

### 21.5 Method Calls on Entities

Entity methods can be called with dot notation:

```sql
select a.GetPopulation() from A.entities() a
select a.ToUpperInvariant(a.City) from A.entities() a
```

---

## 22. Automatic Type Coercion

### 22.1 String-to-Numeric Coercion

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

### 22.2 String-to-DateTime Coercion

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

**Edge cases:**
- Unparseable strings (e.g., `"not-a-date"`) simply don't match — no exception thrown
- `null` strings don't match any date/time comparison
- Supported formats follow the invariant culture parsing rules (ISO 8601, common date patterns)
- Supports all comparison operators: `=`, `<>`, `>`, `<`, `>=`, `<=`

### 22.3 Numeric Type Promotion

In arithmetic and bitwise operations involving different numeric types, values are promoted to the wider type:

| Operation | Result Type |
|-----------|-------------|
| `byte + int` | `int` |
| `int + long` | `long` |
| `int + decimal` | `decimal` |
| `sbyte AND byte` | `int?` |
| `byte AND ulong` | `ulong?` |

### 22.4 Object Column Coercion

When an `object`-typed column is compared to a numeric literal, runtime conversion is attempted. Same graceful failure as string coercion — no exception on failure.

---

## 23. Error Catalog

### 23.1 Compile-Time Errors

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
| Ambiguous method owner (MQ3035) | Unqualified function call resolves to different implementations across schemas — see [§8.7.1](#871-method-auto-resolution-algorithm) | `AmbiguousMethodOwnerException` |
| ASOF JOIN: no inequality | ASOF JOIN ON clause contains only equality conditions | `AsOfJoinRequiresInequality` |
| ASOF JOIN: multiple inequalities | ASOF JOIN ON clause contains more than one inequality condition | `AsOfJoinSupportsOnlyOneInequality` |
| ASOF JOIN: OR in ON clause | ASOF JOIN ON clause contains OR | `AsOfJoinDoesNotSupportOr` |
| ASOF JOIN: one-sided inequality | ASOF JOIN inequality condition does not reference columns from both sides | `AsOfJoinInequalityMustReferenceBothSides` |
| ASOF JOIN: non-orderable type | ASOF JOIN inequality column type does not implement `IComparable` | `AsOfJoinInequalityColumnNotOrderable` |
| Star EXCLUDE non-existent column (MQ3041) | EXCLUDE references a column not in the star expansion | `StarModifierValidationException` |
| Star REPLACE non-existent column (MQ3042) | REPLACE targets a column not in the table | `StarModifierValidationException` |
| Star EXCLUDE removes all columns (MQ3043) | EXCLUDE would leave zero columns | `StarModifierValidationException` |
| Star column in both EXCLUDE and REPLACE (MQ3044) | Same column appears in both EXCLUDE and REPLACE lists | `StarModifierValidationException` |
| Star LIKE matches no columns (MQ3045) | LIKE/NOT LIKE pattern matched zero columns | `StarModifierValidationException` |
| Star EXCLUDE duplicate column (MQ3046) | Same column listed twice in EXCLUDE | `StarModifierValidationException` |
| Star REPLACE duplicate column (MQ3047) | Same column listed twice in REPLACE | `StarModifierValidationException` |
| Star REPLACE targets removed column (MQ3048) | REPLACE targets a column already removed by LIKE or EXCLUDE | `StarModifierValidationException` |

### 23.2 Runtime Errors

| Error | Cause | Behavior |
|-------|-------|----------|
| Invalid type conversion | `ToInt32('abc')` | Returns `null` (no exception) |
| Invalid regex in RLIKE | `Name RLIKE '[invalid('` | Exception thrown |
| Non-numeric string comparison | String `"abc"` compared to number | No match, no exception |

### 23.3 Graceful Failures

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

## 24. Formal Grammar

### 24.1 Notation

- `KEYWORD` — literal keyword (case-insensitive)
- `name` — production rule
- `[x]` — optional
- `{x}` — zero or more repetitions
- `x+` — one or more repetitions
- `x | y` — alternatives
- `'symbol'` — literal symbol

Nested brackets (`[ [AS] alias_name ]`) mean the entire group is optional, with `AS` independently optional within it.

### 24.2 Statement-Level Grammar

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

### 24.3 Query Grammar

```ebnf
set_operators  ::= query { set_operator query }

set_operator   ::= UNION '(' key_list ')'
                 | UNION ALL '(' key_list ')'
                 | EXCEPT '(' key_list ')'
                 | INTERSECT '(' key_list ')'

key_list       ::= [identifier {',' identifier}]

query          ::= regular_query | reordered_query

regular_query  ::= SELECT [DISTINCT] select_list
                   FROM from_clause
                   {join_or_apply}
                   [WHERE expression]
                   [GROUP BY expression_list [HAVING expression]]
                   [WINDOW window_def {',' window_def}]
                   [ORDER BY order_list]
                   [SKIP integer]
                   [TAKE integer]

reordered_query ::= FROM from_clause
                    {join_or_apply}
                    [WHERE expression]
                    [GROUP BY expression_list [HAVING expression]]
                    [WINDOW window_def {',' window_def}]
                    SELECT [DISTINCT] select_list
                    [ORDER BY order_list]
                    [SKIP integer]
                    [TAKE integer]
```

### 24.4 FROM Clause Grammar

```ebnf
from_clause    ::= schema_source [alias]
                 | identifier [alias]

schema_source  ::= identifier '.' identifier '(' [arg_list] ')'

alias          ::= identifier | AS identifier

join_or_apply  ::= join_clause | apply_clause

join_clause    ::= [INNER] JOIN from_clause ON expression
                 | LEFT [OUTER] JOIN from_clause ON expression
                 | RIGHT [OUTER] JOIN from_clause ON expression
                 | ASOF JOIN from_clause ON asof_condition
                 | ASOF LEFT [OUTER] JOIN from_clause ON asof_condition

asof_condition ::= asof_expr { AND asof_expr }

asof_expr      ::= expression '=' expression
                 | expression inequality_op expression

inequality_op  ::= '>=' | '>' | '<=' | '<'

apply_clause   ::= CROSS APPLY apply_source AS identifier
                 | OUTER APPLY apply_source AS identifier

apply_source   ::= schema_source
                 | identifier '.' method_call
                 | identifier '.' property_path
```

### 24.5 SELECT List Grammar

```ebnf
select_list    ::= select_item {',' select_item}

select_item    ::= star_expr
                 | expression [[AS] alias_name]

star_expr      ::= ('*' | identifier '.' '*') [star_modifiers]

star_modifiers ::= [like_modifier] [exclude_modifier] [replace_modifier]

like_modifier  ::= LIKE string_literal
                 | NOT LIKE string_literal

exclude_modifier ::= EXCLUDE '(' identifier {',' identifier} ')'

replace_modifier ::= REPLACE '(' replace_item {',' replace_item} ')'

replace_item   ::= expression AS identifier

alias_name     ::= identifier
                 | string_literal
                 | '[' any_text ']'
```

### 24.6 Expression Grammar (by precedence, lowest to highest)

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
                 | add_expr [NOT] BETWEEN add_expr AND add_expr
                 | add_expr CONTAINS '(' expression_list ')'

comp_op        ::= '=' | '<>' | '>' | '>=' | '<' | '<='

add_expr       ::= bitwise_expr {('+'|'-') bitwise_expr}
bitwise_expr   ::= shift_expr {('&'|'|'|'^') shift_expr}
shift_expr     ::= mul_expr {('<<'|'>>') mul_expr}
mul_expr       ::= unary_expr {('*'|'/'|'%') unary_expr}
unary_expr     ::= ['-'] primary

primary        ::= literal
                 | identifier {'.' identifier} ['(' [arg_list] ')'] [window_over]
                 | identifier '[' expression ']'
                 | '(' expression ')'
                 | case_expression
                 | '::' integer

case_expression ::= searched_case_expression
                  | simple_case_expression

searched_case_expression ::= CASE when_clause+ ELSE expression END

simple_case_expression ::= CASE expression simple_when_clause+ ELSE expression END

when_clause ::= WHEN expression THEN expression

simple_when_clause ::= WHEN expression THEN expression
```

### 24.7 Literal Grammar

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

### 24.8 Utility Statement Grammar

```ebnf
table_definition ::= TABLE identifier '{' column_def_list '}'

column_def_list ::= column_def { ',' column_def } [',']

column_def     ::= identifier ':' type_name ['?']

type_name      ::= identifier
                  | qualified_type_name

qualified_type_name ::= identifier { '.' identifier }

couple_statement ::= COUPLE schema_source WITH TABLE identifier AS identifier

desc_statement ::= DESC desc_target [column_clause]
                 | DESC FUNCTIONS desc_function_target

desc_target ::= identifier
              | identifier '.' identifier
              | identifier '.' identifier '(' [arg_list] ')'

desc_function_target ::= identifier
                       | identifier '.' identifier
                       | identifier '.' identifier '(' [arg_list] ')'

column_clause ::= COLUMN column_path

column_path ::= identifier { '.' identifier }
```

### 24.9 Window Function Grammar

```ebnf
window_over    ::= OVER '(' window_spec ')'
                 | OVER identifier

window_spec    ::= [partition_clause] [order_clause]

partition_clause ::= PARTITION BY expression {',' expression}

order_clause   ::= ORDER BY ordered_field {',' ordered_field}

ordered_field  ::= expression [ASC | DESC]

window_clause  ::= WINDOW window_def {',' window_def}

window_def     ::= identifier AS '(' window_spec ')'
```

Window functions are recognized at the expression level — when a function call is followed by the `OVER` keyword, the parser wraps it as a `WindowFunctionNode`.

**Supported function names** (case-insensitive, underscores ignored):

| Ranking | Offset | Aggregate |
|---------|--------|-----------|
| `RowNumber()` / `ROW_NUMBER()` | `Lag(expr [, offset [, default]])` | `Sum(expr)` |
| `Rank()` / `RANK()` | `Lead(expr [, offset [, default]])` | `Count(expr)` |
| `DenseRank()` / `DENSE_RANK()` | | `Avg(expr)` |
| | | `Min(expr)` |
| | | `Max(expr)` |

---

## 25. Appendices

### Appendix A: Complete Keyword List

```
AND, AS, ASC, ASOF, ASOF JOIN, ASOF LEFT JOIN, ASOF LEFT OUTER JOIN,
CASE, CONTAINS, COUPLE, CROSS APPLY, DESC, DISTINCT,
ELSE, END, EXCEPT, FALSE, FROM, FUNCTIONS, GROUP BY, HAVING, IN,
INNER JOIN, INTERSECT, IS, JOIN, LEFT JOIN, LEFT OUTER JOIN, LIKE,
NOT, NOT IN, NOT LIKE, NOT RLIKE, NULL, ON, OR, ORDER BY, OVER,
OUTER APPLY, PARTITION BY, RIGHT JOIN, RIGHT OUTER JOIN, RLIKE,
SELECT, SKIP, TABLE, TAKE, THEN, TRUE, UNION, UNION ALL,
WHEN, WHERE, WINDOW, WITH
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
| Data sources | `FROM table_name` | `FROM schema.method()` |
| Pagination | `OFFSET n LIMIT m` | `SKIP n TAKE m` |
| Not-equal | `<>` and `!=` | Only `<>` supported — `!=` is rejected with a suggestion to use `<>` |
| CASE WHEN ELSE | ELSE optional | ELSE **mandatory** |
| Simple CASE | `CASE expr WHEN value THEN ...` | Supported |
| Set operations | `UNION` / `UNION ALL` without extra syntax | `UNION (key_columns)` / `UNION ALL (key_columns)`; bare standard SQL form is rejected |
| Recursive CTEs | Supported | Not supported |
| Subqueries in FROM | Supported | Not supported (use CTEs) |
| `BETWEEN` | `x BETWEEN a AND b` | Supported — `x BETWEEN a AND b` is equivalent to `x >= a AND x <= b` |
| Window functions | `ROW_NUMBER() OVER (...)` | `RowNumber()` (no OVER clause, sequential) |
| String comparison | Implementation-defined | LIKE is case-insensitive; `=` is ordinal |
| Cross/Outer Apply | T-SQL only | Fully supported with method/property expansion |
| ASOF JOIN | Not standard (DuckDB extension) | Supported — nearest-match join on an ordered column |
| Array indexing | Not standard | `column[n]`, negative indexing, safe OOB |
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
from A.entities()
```

Simple CASE is also supported:

```sql
select
    case Population
        when 100 then 'small'
        when 1000 then 'large'
        else 'other'
    end
from A.entities()
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
from A.entities()
group by case when Population >= 500 then 'big' else 'small' end

-- In arithmetic
select 1 + (case when 2 > 1 then 1 else 0 end) - 1 from system.dual()

-- Short-circuit evaluation: only the matching branch is evaluated
select case when City <> 'X' then 'safe' else ThrowException() end from ...
```

### Appendix G: Field Links (GROUP BY References)

Field links use `::N` syntax (where N is a positive integer starting from 1) to reference GROUP BY columns by position:

```sql
-- ::1 refers to the first GROUP BY expression (Country)
select ::1, Count(Name) from A.entities() group by Country

-- Equivalent to:
select Country, Count(Name) from A.entities() group by Country
```

**Rules:**
- `::N` is 1-based — `::1` references the first GROUP BY column.
- `N` MUST be a positive integer (no `::0`).
- If `N` exceeds the number of GROUP BY columns, a `FieldLinkIndexOutOfRangeException` is thrown.
- Field links are resolved during query compilation — they are syntactic shorthand, not a runtime feature.
