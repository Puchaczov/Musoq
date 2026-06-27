# Musoq Core SQL Language Specification

**Version:** 1.4.2
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
| Inline row sources | `VALUES (...)` table constructors in selected contexts | `FROM values { { Field: literal } } alias` creates a strongly typed inline source |
| Script parameters | Vendor-specific variables, prepared statement parameters, or host API bindings | Optional leading `param(...)` block; references use `$name` |
| Script variables | Host-language variables or SQL dialect variables | `let name: type = constantExpression` declarations; references use `$name` |
| Pagination | `OFFSET` / `LIMIT` | `SKIP` / `TAKE` |
| Set operation keys | Implicit (all columns) | Omitted keys and `()` compare all projected values; explicit key lists such as `UNION (col1)` compare a subset |
| Not-equal operator | Both `<>` and `!=` | Both `<>` and `!=` are supported |
| CASE WHEN | ELSE is optional | ELSE is **mandatory** |
| Simple CASE form | `CASE expr WHEN value THEN ...` | Supported (desugared to searched CASE internally) |
| FROM-first syntax | Not standard | Supported: `FROM ... WHERE ... SELECT ...` |
| CROSS APPLY / OUTER APPLY | T-SQL extension | Fully supported with method and property expansion |
| CROSS JOIN | Standard SQL | Supported as an uncorrelated Cartesian product; filtering belongs in `WHERE` |
| FULL OUTER JOIN | Standard SQL | Supported; use `alias IS PRESENT` / `alias IS MISSING` to classify unmatched sides |
| SEMI JOIN / ANTI JOIN | Common relational algebra extension | Supported as left-sided existence and non-existence joins |
| Recursive CTEs | Supported in many dialects | **Not supported** |
| Subqueries in FROM | Supported | Supported as derived tables; plain derived tables are independent and correlation requires `CROSS APPLY` / `OUTER APPLY` |
| `BETWEEN` operator | Supported | Supported — `x BETWEEN a AND b` is equivalent to `x >= a AND x <= b` |
| `ORDER BY` position | `ORDER BY 1` | **Not supported** — use column names or expressions |
| ASOF JOIN | Not standard | Supported — nearest-match join on an ordered column |
| PIVOT | Vendor-specific | Supported as a simplified static-pivot statement with mandatory static `IN (...)` values; see §10.11 |
| UNPIVOT | Vendor-specific | Supported as a Musoq-style static row expansion statement with explicit keep fields; see §10.12 |
| Window functions | `OVER (PARTITION BY ... ORDER BY ...)` with frame specs | Supports `PARTITION BY`, `ORDER BY`, named `WINDOW` clause, `ROWS BETWEEN` and `RANGE BETWEEN` frame specifications, ranking (`ROW_NUMBER`, `RANK`, `DENSE_RANK`, `NTILE`), offset (`LAG`, `LEAD`), aggregate (`SUM`, `COUNT`, `AVG`, `MIN`, `MAX`), and value access (`FIRST_VALUE`, `LAST_VALUE`, `NTH_VALUE`) window functions. Data-source plugins can register custom window functions. `QUALIFY` clause is supported for filtering rows after window function evaluation. `RANGE BETWEEN` requires an `ORDER BY` clause in the window specification. |
| FILTER on aggregates | Not standard (part of SQL:2003) | Supported — `Count(x) FILTER (WHERE condition)` restricts aggregate input; see §10.10 |
| Subqueries | `IN`, `EXISTS`, scalar, quantified, and derived-table subqueries | Supported; correlated predicate and scalar forms are decorrelated where possible; see §7.9-§7.12 |
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
| **Script parameter** | A typed value declared once at the start of a script and supplied by the host before execution |
| **Script variable** | An immutable typed value declared in a script, evaluated at compile time, and referenced as `$name` |
| **Source runtime settings profile** | A named host-resolved settings profile selected by `couple ... with settings ...` for one source context |

### 1.5 Notation Conventions

The key words MUST, MUST NOT, SHOULD, SHOULD NOT, and MAY in this specification are to be interpreted as described in [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119). When these words appear in uppercase, they carry normative weight. When they appear in lowercase, they are used in their ordinary English sense.

### 1.6 Specification Family

This document is the core specification in a family of related documents:

| Document | Scope |
|----------|-------|
| **Musoq Core SQL Language Specification** (this document) | Core SQL dialect: syntax, semantics, type system, built-in functions, error catalog |
| **Musoq Interpretation Schemas: Language Extension Specification** (`musoq-binary-text-spec.md`) | `binary` and `text` schema extensions for declarative parsing of binary and textual data |
| **Musoq AI Interpretation Schemas: Language Extension Specification** (`musoq-ai-spec.md`) | `ai` schema extension for structured extraction from unstructured content via LLMs |
| **Musoq TABLE/COUPLE Statements Specification** (`musoq-table-couple-spec.md`) | `TABLE` and `COUPLE` statements for defining explicit type schemas and selecting source runtime settings profiles for data sources |

Satellite specifications extend the core language with new statement types and schema kinds. They inherit the core specification’s lexical rules, type system, expression semantics, and notation conventions.

### 1.7 Conformance

A conforming implementation MUST support all features defined in this core specification. The extension specifications (§1.6) define optional profiles:

- **Binary/Text Interpretation profile**: `binary` and `text` schema definitions, `Interpret<Schema>()`, `Parse<Schema>()`, and related functions
- **AI Interpretation profile**: `ai` schema definitions, `Infer()`, `TryInfer()`, and related functions
- **TABLE/COUPLE profile**: `TABLE` and `COUPLE` statements for explicit type binding and source runtime settings profile selection

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
| `SEMI` | SEMI JOIN modifier (context-sensitive — only a keyword before `JOIN`) |
| `ANTI` | ANTI JOIN modifier (context-sensitive — only a keyword before `JOIN` or `SEMI JOIN`) |
| `CROSS` | CROSS JOIN / CROSS APPLY modifier (context-sensitive) |
| `FUNCTIONS` | Used in `DESC FUNCTIONS` |
| `QUERY` | Used in `DESC QUERY` |
| `TRUE` | Boolean true literal |
| `FALSE` | Boolean false literal |
| `PARAM` | Optional script parameter declaration block when used at the start of a script |
| `LET` | Contextual script variable declaration keyword when used at statement start |
| `PIVOT` | Begin a pivot query |
| `UNPIVOT` | Begin an unpivot query |
| `KEEP` | UNPIVOT keep field list (context-sensitive) |
| `TABLE` | Define a table structure |
| `COUPLE` | Bind a schema method to a table and/or source runtime settings profile |
| `CASE` | Begin conditional expression |
| `WHEN` | Conditional branch |
| `THEN` | Branch result |
| `ELSE` | Default branch (mandatory in CASE) |
| `END` | End conditional expression |
| `DISTINCT` | Remove duplicate rows / null-safe comparison phrase |
| `ASC` | Ascending sort order (default) |
| `DESC` | Descending sort order / Describe schema |
| `ASOF` | ASOF JOIN modifier (context-sensitive — only a keyword before `JOIN` or `LEFT`) |
| `EXCLUDE` | Star modifier — remove columns (context-sensitive — only a keyword after `*` or `alias.*`) |
| `REPLACE` | Star modifier — substitute column expressions (context-sensitive — only a keyword after `*` or `alias.*`) |
| `RENAME` | Star modifier — rename output columns (context-sensitive — only a keyword after `*` or `alias.*`) |
| `NULLS` | Explicit NULL placement in `ORDER BY` keys |
| `FIRST` | NULL ordering modifier in `NULLS FIRST` |
| `LAST` | NULL ordering modifier in `NULLS LAST` |
| `TIE` | ASOF tie-break modifier (context-sensitive — only in `TIE BREAK BY`) |
| `BREAK` | ASOF tie-break modifier (context-sensitive — only after `TIE`) |
| `ORDINALITY` | APPLY ordinality modifier (context-sensitive — only after `WITH`) |

`ANY`, `SOME`, and `ALL` are contextual SQL keywords. They act as quantified subquery operators only in comparison predicates such as `x > ANY (SELECT ...)` or `x = SOME (FROM ...)`. Lowercase `any(...)` and `all(...)` also remain contextual predicate-quantifier names when they appear as unqualified calls immediately before a supported pattern operator, for example `any(Name, Message) LIKE '%error%'`.

`USING` is a contextual keyword inside `PIVOT` and `UNPIVOT` statements. `KEEP` is a contextual keyword inside an `UNPIVOT` statement. Outside those clause positions they remain ordinary identifiers.

`SETTINGS` is a contextual keyword inside `COUPLE ... WITH SETTINGS ...` and `DESC SETTINGS`. `QUERY` is a contextual keyword inside `DESC QUERY`. `COLUMN` is a contextual keyword inside `DESC ... COLUMN ...`. Outside those clause positions they remain ordinary identifiers.

#### Multi-Word Keywords

| Keyword | Purpose |
|---------|---------|
| `NOT IN` | Negated set membership |
| `NOT EXISTS` | Negated existence predicate |
| `NOT LIKE` | Negated pattern matching |
| `NOT RLIKE` | Negated regex matching |
| `UNION ALL` | Set union preserving duplicates |
| `GROUP BY` | Grouping for aggregation |
| `ORDER BY` | Result ordering |
| `NULLS FIRST` | Explicit NULL ordering |
| `NULLS LAST` | Explicit NULL ordering |
| `INNER JOIN` or `JOIN` | Inner join (equivalent forms) |
| `LEFT OUTER JOIN` or `LEFT JOIN` | Left outer join |
| `RIGHT OUTER JOIN` or `RIGHT JOIN` | Right outer join |
| `FULL OUTER JOIN` or `FULL JOIN` | Full outer join |
| `ASOF JOIN` | ASOF inner join (nearest-match on ordered column) |
| `ASOF LEFT JOIN` or `ASOF LEFT OUTER JOIN` | ASOF left outer join |
| `TIE BREAK BY` | Deterministic ASOF duplicate-candidate tie-break |
| `CROSS APPLY` | Correlated cross join |
| `OUTER APPLY` | Correlated outer join |
| `WITH ORDINALITY` | Expose a zero-based ordinality column for APPLY |
| `IS DISTINCT FROM` | Null-safe inequality comparison |
| `IS NOT DISTINCT FROM` | Null-safe equality comparison |
| `DESC QUERY` | Describe projected query output columns |

`PRESENT` and `MISSING` are contextual words only after `IS` in alias-level row presence predicates: `alias IS PRESENT` and `alias IS MISSING`. Outside that position they remain ordinary identifiers.

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
| 4 | `::` | Strict postfix cast | `Population::Int32` |
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
| `!=` | Not equal (alternative syntax) |
| `>` | Greater than |
| `>=` | Greater than or equal |
| `<` | Less than |
| `<=` | Less than or equal |

> **Note**: Both `<>` and `!=` are supported for not-equal comparison. They are functionally identical.

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
| `any(expr, ...) LIKE pattern` | True when at least one expression matches the pattern |
| `all(expr, ...) LIKE pattern` | True when every expression matches the pattern |
| `IN` | Set membership test |
| `NOT IN` | Negated set membership test |
| `IS NULL` | Tests for NULL |
| `IS NOT NULL` | Tests for non-NULL |
| `CONTAINS` | Tests if a value is in a literal list |

#### Null Fallback Operator

| Operator | Description | Example |
|----------|-------------|---------|
| `??` | Returns the left operand when it is not `null`; otherwise returns the right operand | `Name ?? 'Unknown'` |

The operands MUST have compatible types. A nullable value type MAY fall back to its underlying non-nullable value type, for example `NullableValue ?? 0`. Reference types are treated as potentially nullable even when host-language annotations imply otherwise. If the left operand is statically a non-nullable value type, the fallback operand is ignored and does not participate in generated execution code.

`??` is right-associative: `A ?? B ?? C` is interpreted as `A ?? (B ?? C)`. It is a null fallback operator only. Musoq does not support `?.`, and `??` does not change existing member access or indexer semantics.

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

### 3.7 Strict Postfix Casts

Runtime v2 supports strict postfix casts:

```sql
select Population::Int32 from A.entities()
select (Population + 1)::Decimal from A.entities()
select Name::String::Guid from A.entities()
```

The cast operator has high postfix precedence. `a + b::Int32` is parsed as `a + (b::Int32)`, while `(a + b)::Int32` casts the parenthesized expression.

Supported cast targets are CLR type names only, matched case-insensitively:

`Boolean`, `Byte`, `SByte`, `Int16`, `UInt16`, `Int32`, `UInt32`, `Int64`, `UInt64`, `Single`, `Double`, `Decimal`, `Char`, `String`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`.

SQL type aliases such as `INTEGER`, `VARCHAR`, and `DOUBLE PRECISION` are not supported. A cast target MUST be an identifier-like type name after `::`; a bare cast operator, a missing target, and a numeric target are syntax errors.

Strict cast behavior:
- `null` input returns `null`.
- Invalid text, overflow, and unsupported conversions throw.
- Numeric, date/time, duration, and GUID parsing uses invariant-culture runtime conversion.

The public `ToXxx(...)` helper functions remain available as library functions and keep their established behavior. They are not equivalent to strict postfix casts when a helper has softer conversion semantics, such as returning `null` for invalid text.

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
| **Script variable** | `LET` | Define an immutable compile-time script value |
| **Table definition** | `TABLE` | Define a typed table structure |
| **Couple** | `COUPLE` | Bind a schema method to a table definition and/or source runtime settings profile |
| **Describe** | `DESC` | Introspect schema or query metadata |

### 4.3 Script Parameters

A script MAY begin with one `param(...)` block. The block declares typed values supplied by the host before query execution. The block is optional; a script without `param(...)` is parsed, compiled, and executed as before.

```sql
param(author: string, minStars: int = 10)
select Name, Stars
from github.repositories()
where Author = $author and Stars >= $minStars
```

The parameter block MUST appear before every query or utility statement:

```sql
-- valid
param(country: string)
select City from geo.cities() where Country = $country

-- invalid: parameter block after a query statement
select City from geo.cities();
param(country: string)
```

#### 4.3.1 Declaration Syntax

```sql
param(name: type, optionalName: type = defaultValue)
```

Rules:
- A script MAY contain zero or one parameter block.
- `param()` is valid and declares no parameters.
- Parameter names are case-sensitive and MUST be unique inside the block.
- A declaration without a default value is required.
- A declaration with a default value is optional and uses the default when the host does not provide an override.
- Nullable types use the same `?` suffix used by table definitions, for example `int?` or `datetime?`.
- Parameter references use `$name` in expressions and behave as typed expressions whose type is the declared parameter type.

Examples:

```sql
param(country: string, minPopulation: int = 1000000, since: datetime? = null)

select City
from geo.cities()
where Country = $country
  and Population >= $minPopulation
  and ($since is null or CreatedAt >= $since)

param(ids: int[])
select Name
from geo.cities()
where Id in $ids
```

#### 4.3.2 Supported Parameter Types

Script parameters support scalar Musoq primitive expression types and one-dimensional collection parameters whose element type is one of those scalar types:

| Musoq Type | CLR Type |
|------------|----------|
| `bool`, `boolean`, `bit` | `System.Boolean` |
| `byte` | `System.Byte` |
| `sbyte` | `System.SByte` |
| `short` | `System.Int16` |
| `ushort` | `System.UInt16` |
| `int` | `System.Int32` |
| `uint` | `System.UInt32` |
| `long` | `System.Int64` |
| `ulong` | `System.UInt64` |
| `float` | `System.Single` |
| `double` | `System.Double` |
| `decimal`, `money` | `System.Decimal` |
| `char` | `System.Char` |
| `string` | `System.String` |
| `datetime` | `System.DateTime` |
| `datetimeoffset` | `System.DateTimeOffset` |
| `timespan` | `System.TimeSpan` |
| `guid` | `System.Guid` |

The nullable form `type?` is supported for value types and accepts `null`.

Collection parameters use array declaration syntax, for example `int[]` or `string[]`. Collection parameter declarations:

- MUST be one-dimensional arrays.
- MUST use a supported scalar parameter type as the element type.
- MUST NOT use a nullable collection suffix such as `int[]?`.
- MUST NOT declare a default value.
- Are supplied by the host as `T[]`, `List<T>`, or `IReadOnlyList<T>` values and are bound as typed read-only lists.
- Are intended for `expr IN $name` and `expr NOT IN $name` membership predicates.

#### 4.3.3 Default Values

Default values MUST be primitive constants or `null`.

```sql
param(
    flag: bool = true,
    code: char = 'x',
    limit: int? = null,
    id: guid = '2ffcf6fa-3369-4300-946a-bb131a037985',
    created: datetime = '2024-01-02T03:04:05Z'
)
select $flag, $code, $limit, $id, $created from system.dual()
```

Default binding rules:
- Numeric defaults use the literal type and suffix rules from §3.6 and Appendix D.
- `guid`, `datetime`, `datetimeoffset`, and `timespan` defaults are supplied as string literals and converted during binding.
- `null` is valid for reference types and nullable value types.
- A supplied parameter value overrides its default.
- Supplied parameter values must match the declared type; Musoq does not parse strings or perform numeric conversion for supplied values.
- Missing required parameters, type mismatches, and illegal null values fail before any data source is opened.

#### 4.3.4 Expression Type Compatibility

A parameter reference has the type declared in the `param(...)` block. Comparisons, boolean clauses, function calls, `IN`, `CONTAINS`, `BETWEEN`, `CASE`, grouping, ordering, joins, windows, CTEs, and set operations use that declared type when checking expression compatibility.

Valid typed comparison:

```sql
param(minStars: int)
select Name
from github.repositories()
where Stars >= $minStars
```

Invalid comparison:

```sql
param(minStars: string)
select Name
from github.repositories()
where Stars >= $minStars
```

The second query is invalid because `$minStars` is declared as `string`, while `Stars` is numeric. Musoq does not infer that the supplied string might contain a number.

Use a strict postfix cast when a textual parameter should be interpreted as another type:

```sql
param(minStarsText: string)
select Name
from github.repositories()
where Stars >= $minStarsText::Int32
```

The library helper form, for example `ToInt32($minStarsText)`, remains available where its established soft conversion behavior is desired.

Normal expression compatibility still applies: same-type comparisons are valid, nullable and non-nullable forms of the same value type are compatible where Musoq normally permits them, and supported numeric widening follows the same rules as other typed expressions.

#### 4.3.5 Source Arguments

A script parameter MAY be passed into a schema source argument only as a direct `$name` argument, and only when the parameter declares a default value:

```sql
-- valid
param(path: string = 'repo')
select Name from files.list($path)

-- invalid: source argument parameter is required
param(path: string)
select Name from files.list($path)

-- invalid: source argument parameter is nested in an expression
param(path: string = 'repo')
select Name from files.list($path + '/src')
```

This restriction keeps source discovery predictable. Use the parameter directly as the source argument and perform additional filtering in `WHERE` when possible.

Script variables are compile-time values and may be used directly or inside supported constant source-argument expressions. See [§4.4.4](#444-source-arguments).

#### 4.3.6 Unsupported Declaration Styles

Musoq deliberately uses one compact syntax instead of borrowing declaration forms from other languages. These forms are rejected with script-parameter diagnostics:

```sql
param(string author)             -- C#-style order is invalid
param([string]$author)           -- PowerShell-style declarations are invalid
def query(author: str = 'x')     -- Python-style function declarations are invalid
declare @author string           -- SQL variable declarations are invalid
```

Use:

```sql
param(author: string = 'x')
select ...
```

#### 4.3.7 Diagnostics

Script-parameter mistakes are reported through the standard Musoq error envelope format. Compile-time problems use parse or bind phases and include source snippets when source text is available. Runtime argument problems use the runtime phase and occur before any data source is opened.

| Code | Phase | Typical Cause |
|------|-------|---------------|
| MQ2031 | parse | Malformed `param(...)` declaration |
| MQ2032 | parse | Unsupported borrowed declaration syntax |
| MQ3056 | bind | Duplicate parameter block |
| MQ3057 | bind | Parameter block after another statement |
| MQ3058 | bind | Duplicate parameter name |
| MQ3059 | bind | Reference to undeclared `$name` |
| MQ3060 | bind | Unsupported parameter type |
| MQ3061 | bind | Invalid default literal for the declared type or default on collection parameter |
| MQ3062 | bind | Invalid parameter usage in a source argument |
| MQ3005 | bind | Parameter declared type is incompatible with the expression context |
| MQ7003 | runtime | Required parameter missing |
| MQ7004 | runtime | Supplied value has the wrong CLR type |
| MQ7005 | runtime | Supplied `null` for a non-nullable value type or collection parameter |

Every envelope includes a stable code, severity, phase, message, explanation, suggested fixes, and a documentation reference.

### 4.4 Script Variables

A script MAY declare immutable script variables with `let`. A script variable is evaluated at compile time, emitted into generated code as a constant or initialized local value, and referenced later with the same `$name` syntax used by script parameters.

```sql
let topic: string = 'important'
let minimumStars: int = 10 + 5

select Name, Stars
from github.repositories()
where Description like '%' + $topic + '%'
    and Stars >= $minimumStars
```

`let` is contextual: it begins a script variable declaration only when it appears at statement start. Existing identifiers or columns named `let` remain valid in expression contexts.

#### 4.4.1 Declaration Syntax

```sql
let name: type = initializer
```

Rules:
- Script variable names are case-sensitive.
- Script variables and script parameters share the `$name` namespace. A name declared by `param(...)` MUST NOT be redeclared by `let`, and a `let` name MUST NOT be declared twice.
- A script variable declaration MAY appear anywhere in the script before its first use.
- A script variable is immutable and has no runtime override mechanism.
- Nullable types use the same `?` suffix used by table definitions and script parameters, for example `int?` or `datetime?`.
- References use `$name` in expressions and behave as typed expressions whose type is the declared script variable type.

#### 4.4.2 Supported Types

Script variables support the same primitive expression types as script parameters. See [§4.3.2](#432-supported-parameter-types) for the complete type table.

#### 4.4.3 Initializers

Initializers MUST be compile-time constants. They MAY use literals, `null`, supported unary/binary constant operators, comparisons, boolean operators, `IS NULL`, `BETWEEN`, and references to earlier script variables.

```sql
let root: string = 'repo'
let sourcePath: string = $root + '/src'
let maxRows: int = 100 * 2
let hasLimit: bool = $maxRows > 0
```

Initializers MUST NOT use data-source columns, runtime script parameters, function calls, subqueries, or script variables declared later in the script.

```sql
param(root: string = 'repo')

-- invalid: runtime parameter used in a compile-time variable initializer
let sourcePath: string = $root + '/src'

-- invalid: forward reference
let laterCopy: int = $later
let later: int = 1
```

#### 4.4.4 Source Arguments

Because script variables are compile-time values, they MAY be used as schema source arguments either directly or inside a supported constant expression:

```sql
let root: string = 'repo'
select Name from files.list($root + '/src')
```

This differs from script parameters: parameters used in source arguments must still be direct `$name` references with defaults, because source metadata is discovered before runtime parameter values are supplied.

#### 4.4.5 Diagnostics

Script-variable mistakes are reported through the standard Musoq error envelope format.

| Code | Phase | Typical Cause |
|------|-------|---------------|
| MQ2033 | parse | Malformed `let name: type = value` declaration |
| MQ3063 | bind | Duplicate name across `param(...)` and `let` declarations |
| MQ3064 | bind | Unsupported script variable type |
| MQ3065 | bind | Invalid initializer for the declared type |
| MQ3066 | bind | Reference to a script variable before it is declared |

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
- Source-qualified column reference: `a.Name` → column name `Name`
- Function call: `Count(Name)` → column name `Count(Name)`
- Literal: `1` → column name `1`

Explicit aliases always define the output column name. This is especially important for CTEs and set operations, where later query blocks see the output names of the preceding query, not the source aliases used to compute them:

```sql
with p as (
    select a.City from A.entities() a
)
select City from p          -- valid; exposed name is City

-- INVALID unless explicitly aliased as [a.City]
select [a.City] from p
```

If a dotted output name is required, it MUST be declared explicitly and referenced as a bracket-quoted identifier:

```sql
with p as (
    select a.City as [a.City] from A.entities() a
)
select [a.City] from p
```

Runtime v2 also makes explicit SELECT aliases visible in selected clauses of the same query block:

```sql
-- Non-aggregate alias in WHERE
select Name as FileName from A.entities() where FileName = 'test'

-- Non-aggregate alias in GROUP BY
select Length(Name) as NameLen, Count(*)
from A.entities()
group by NameLen

-- Aggregate alias in HAVING
select Count(*) as cnt
from A.entities()
group by City
having cnt > 1
```

Alias lookup rules:
- A real source column or table binding wins over a SELECT alias with the same name.
- Non-aggregate SELECT aliases are visible in `WHERE` and `GROUP BY` when no source column with that name exists.
- Aggregate SELECT aliases are visible in `HAVING`; grouped non-aggregate aliases may also be referenced there.
- Aggregate SELECT aliases are rejected in `GROUP BY`.
- Duplicate aliases keep the first matching alias in the SELECT list.
- Alias visibility is query-local; nested query blocks, CTE consumers, and derived-table consumers see only the output column names exposed by their source query.

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

Qualified star inside a CTE exports the source column names, not source-qualified names:

```sql
with p as (select a.* from A.entities() a)
select City, Country from p
```

#### 5.4.1 Star Modifiers

Star expressions support optional modifiers that filter or transform the expanded columns. Modifiers are applied in a fixed order: **LIKE/NOT LIKE → EXCLUDE → REPLACE → RENAME**.

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

**RENAME** changes the output column name after filtering and replacement without changing the underlying expression:

```sql
select * rename (Name as EntityName) from A.entities() a
select * replace (Population * 2 as Population) rename (Population as Pop2) from A.entities() a
select a.* rename (a.Name as EntityName) from A.entities() a
```

**LIKE / NOT LIKE** filters columns by a SQL LIKE pattern on column names (case-insensitive):

```sql
select * like 'C%' from A.entities() a           -- only columns starting with 'C'
select * not like '%Id' from A.entities() a       -- exclude columns ending with 'Id'
select * like '_ame' from A.entities() a          -- match single-char wildcard
```

Modifiers can be composed:

```sql
select * like '%o%' exclude (Country) replace (Population * 3 as Population) rename (Population as Population3x) from A.entities() a
```

**Rules:**

- Modifiers are always applied in the order LIKE → EXCLUDE → REPLACE → RENAME.
- EXCLUDE, REPLACE, and RENAME column names are matched **case-insensitively**.
- EXCLUDE must not remove all columns (compile-time error MQ3043).
- REPLACE targets must exist in the surviving column set (after LIKE and EXCLUDE).
- RENAME targets the post-LIKE/post-EXCLUDE/post-REPLACE output name.
- A column cannot appear in both EXCLUDE and REPLACE (compile-time error MQ3044).
- Duplicate entries within EXCLUDE, REPLACE, or RENAME are forbidden (MQ3046, MQ3047, MQ3068).
- RENAME target names must not duplicate another output column (MQ3069).
- LIKE/NOT LIKE patterns must match at least one column (MQ3045).
- `EXCLUDE`, `REPLACE`, and `RENAME` are **context-sensitive keywords** — they are only treated as keywords immediately after `*` or `alias.*`. In all other positions, they are valid identifiers.

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

In **multi-table queries** (joins, applies), aliases MUST be used to disambiguate columns. For function calls, the engine attempts auto-resolution first — see [§8.10.1](#8101-method-auto-resolution-algorithm) for details:

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

When a CTE reference has a table alias, that alias hides the original CTE name in the current query block:

```sql
with cte as (select City from A.entities())
select p.City from cte p      -- valid

-- INVALID in the same query block
select cte.City from cte p
```

CTE names do not reserve table/source aliases in unrelated query blocks. A source alias used inside a CTE body is local to that CTE body, and the outer query may reuse the same text for its own aliases.

### 6.4 Derived Tables

A parenthesized query may be used as a `FROM` or `JOIN` source when it has an alias:

```sql
select d.City
from (
    select a.City
    from A.entities() a
    where a.Population > 250
) d

select a.City, d.City
from A.entities() a
inner join (
    select b.Country, b.City
    from B.entities() b
) d on a.Country = d.Country
```

Rules:

- A derived table MUST have an alias after the closing parenthesis.
- The derived-table body may be a normal `SELECT` query, a `FROM`-first query, a query with set operators, or a `WITH` expression.
- Query blocks outside the derived table see only the derived table output column names. Source aliases declared inside the derived-table body are not exported.
- `*` and `alias.*` expand the derived table output columns under the derived table alias.
- Plain derived tables are not lateral. A derived table in `FROM` or `JOIN` MUST NOT reference source aliases from the containing query block.
- To write a correlated derived table, use `CROSS APPLY` or `OUTER APPLY`; see §9.7.

```sql
-- INVALID: plain JOIN derived tables are independent
select a.City, d.City
from A.entities() a
inner join (
    select b.City
    from B.entities() b
    where b.Country = a.Country
) d on 1 = 1
```

### 6.5 Inline VALUES Row Sources

`values` can be used in the `FROM` clause to define a small inline table:

```sql
from values {
    { Name: 'Newtonsoft.Json', Approved: true, Score: 10ui },
    { Name: 'Legacy.Package', Approved: false, Score: 20ui }
} packages
select packages.Name, packages.Score
```

Scalar script parameters and scalar `let` variables may be used in row expressions:

```sql
param(defaultScore: int)
let boost: int = 5

from values {
    { Name: 'Pinned', Score: $defaultScore + $boost },
    { Name: 'Fallback', Score: 0 }
} packages
select packages.Name, packages.Score
```

Rules:

- A VALUES source MUST have an alias after the closing brace.
- The source MUST contain at least one row.
- Every row MUST contain at least one field.
- Field names are case-insensitive for shape validation and MUST be unique within each row.
- Every row MUST contain the same field names as the first row.
- Field expressions MAY use literals, `NULL`, scalar script parameter references, scalar `let` references, and arithmetic composed from those values.
- Field expressions MUST NOT reference source columns, aggregate functions, window functions, subqueries, or any row-dependent expression.
- Each field becomes a strongly typed column. Numeric suffixes control the generated CLR type; for example, `10ui` generates a `uint` column. Bare `10u` is not a valid suffix.
- VALUES row literals use the same numeric literal syntax as the rest of the language. Supported integer type suffixes are `b`, `ub`, `s`, `us`, `i`, `ui`, `l`, and `ul`; `d`/`D` forces a decimal literal.
- Base-prefixed numeric literals are supported: `0x`/`0X` for hexadecimal, `0b`/`0B` for binary, and `0o`/`0O` for octal. These prefixes produce `long` values and do not accept type suffixes; for example, `0x10` is valid and `0x10ui` is not.
- `NULL` values are allowed. If a column contains both `NULL` and value-type expressions, the generated column type is nullable.
- Numeric column inference includes parameter and `let` expression types. It preserves the exact type when all non-NULL expressions have the same type. Mixed numeric columns are promoted conservatively: `decimal` wins over integer types, `long` wins over non-`ulong` integer mixes that contain a `long`, `uint` wins over smaller non-`long` integer mixes, and `ulong` may only mix with unsigned integer values unless the column also contains `decimal`.

VALUES sources participate in joins, applies, grouping, ordering, set operators, subqueries, CTEs, and built-in ranking/offset window functions like any other source:

```sql
with policy as (
    from values {
        { Name: 'Newtonsoft.Json', Approved: true },
        { Name: 'Legacy.Package', Approved: false }
    } p
    select p.Name, p.Approved
)
select leftPolicy.Name
from policy leftPolicy
inner join policy rightPolicy on leftPolicy.Name = rightPolicy.Name
where rightPolicy.Approved = false
```

When a VALUES source is used inside a CTE, normal CTE materialization rules apply. Referencing the CTE multiple times reuses the CTE result rather than reparsing the row literal syntax at each reference.

### 6.6 Coupled Source References

After a `COUPLE` statement, the coupled alias becomes a data source. The alias can be bound to a table definition, a source runtime settings profile, or both:

```sql
table MyTable { Name: string };
couple A.Entities with table MyTable as Source;
select Name from Source()
select Name from Source(true, 'param')   -- with arguments
```

### 6.7 `system.range(start, end)` Semantics

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

### 7.5 IS DISTINCT FROM / IS NOT DISTINCT FROM

`IS DISTINCT FROM` and `IS NOT DISTINCT FROM` are null-safe comparison operators:

```sql
where PreviousValue is distinct from CurrentValue
where PreviousValue is not distinct from CurrentValue
```

Semantics:

- Two `NULL` values are not distinct.
- One `NULL` and one non-`NULL` value are distinct.
- Two non-`NULL` values use the normal typed equality and coercion rules.

`IS NOT DISTINCT FROM` is useful in join predicates when nullable keys should match only when both sides are `NULL` or both sides have the same value:

```sql
select a.Name, b.Name
from A.entities() a
inner join B.entities() b on a.OptionalCode is not distinct from b.OptionalCode
```

### 7.6 LIKE Pattern Matching

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

### 7.7 RLIKE (Regular Expression Matching)

`RLIKE` matches against a regular expression (ECMAScript-compatible subset):

```sql
where Name rlike '^\d+'              -- starts with digits
where Email rlike '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'
where Name not rlike '^test.*$'      -- does not match pattern
```

> **Note**: Invalid regex patterns cause a runtime error when the query executes.

### 7.8 Multi-Field Pattern Predicate Quantifiers

The contextual calls `any(...)` and `all(...)` can be placed immediately before the pattern operators `LIKE`, `NOT LIKE`, `RLIKE`, and `NOT RLIKE` to apply the same pattern to several expressions.

```sql
where any(Name, Message, Details) like '%error%'
where all(Source, Category) not like '%deprecated%'
where any(FileName, FullPath) rlike '\.(cs|fs)$'
where all(Name, City) not rlike '^test.*$'
```

These forms are parser-level shorthand. The parser MUST lower them before semantic analysis:

| Source syntax | Lowered form |
|---------------|--------------|
| `any(a, b) LIKE p` | `a LIKE p OR b LIKE p` |
| `all(a, b) LIKE p` | `a LIKE p AND b LIKE p` |
| `any(a, b) NOT LIKE p` | `a NOT LIKE p OR b NOT LIKE p` |
| `all(a, b) NOT RLIKE p` | `a NOT RLIKE p AND b NOT RLIKE p` |

Each argument is a normal expression. It may be a column reference, literal, property access, or method call. Null handling is the same as the underlying per-expression pattern predicate: `null LIKE pattern` and `null RLIKE pattern` evaluate to `false`, so their negated forms evaluate to `true`.

The quantifier names are contextual. A qualified or otherwise normal method call such as `source.any(Name)` is not rewritten. In v1, predicate quantifiers are supported only with the `LIKE`/`RLIKE` operator family. `any(*)`, `all(*)`, star-qualified forms such as `any(source.*)`, and a regex literal prefix such as `rx'...'` are not supported.

The lowered `OR`/`AND` expression participates in normal precedence rules. Use parentheses when combining it with additional `AND` or `OR` predicates:

```sql
where (any(Name, Message) like '%error%' and Severity = 'high')
    or Id = 0
```

### 7.9 IN / NOT IN

Tests membership in a set of values.

#### 7.9.1 Literal Lists

The simplest form tests against a list of literal values or column references:

```sql
where Population in (100, 200, 300, 400)
where City in ('WARSAW', 'BERLIN', 'MUNICH')
where City in (Country, 'Warsaw')          -- can mix column references and literals
where Population not in (100, 400)
```

#### 7.9.2 Collection Parameters

When a script parameter is declared as a one-dimensional collection, `IN $name` and `NOT IN $name` test membership in the supplied typed collection:

```sql
param(ids: int[])
select Name
from A.entities()
where Id in $ids
```

The left expression type must match the parameter element type after normal nullable normalization. The runtime membership check uses a typed indexed loop over the read-only collection. A missing or `NULL` collection parameter fails during parameter binding before source execution.

#### 7.9.3 Subqueries

The `IN` and `NOT IN` predicates also accept a single-column subquery:

```sql
-- Basic IN subquery: keep rows whose City appears in the subquery result
select a.City from A.entities() a
where a.City in (select b.City from B.entities() b)

-- NOT IN subquery: exclude rows whose City appears in the subquery result
select a.City from A.entities() a
where a.City not in (select b.City from B.entities() b)
```

The subquery MUST return exactly one column. If it returns more than one column, a compile-time error is raised (MQ3049):

```sql
-- ERROR: MQ3049 — subquery returns two columns
select a.City from A.entities() a
where a.City in (select b.City, b.Country from B.entities() b)
```

#### 7.9.4 Set Operators in IN Subqueries

Subqueries within `IN` / `NOT IN` support set operators (`UNION`, `EXCEPT`, `INTERSECT`):

```sql
-- UNION: cities from either source
select a.City from A.entities() a
where a.City in (
    select b.City from B.entities() b
    union (City) select c.City from C.entities() c
)

-- EXCEPT: cities in B but not in C
select a.City from A.entities() a
where a.City in (
    select b.City from B.entities() b
    except (City) select c.City from C.entities() c
)

-- INTERSECT: cities in both B and C
select a.City from A.entities() a
where a.City in (
    select b.City from B.entities() b
    intersect (City) select c.City from C.entities() c
)
```

Set operators within IN subqueries follow the same key-column syntax as top-level set operations (§12).

#### 7.9.5 IN Subqueries with Other Conditions

IN subqueries can be combined with other conditions using `AND` and `OR`:

```sql
-- Combined with OR
select a.City from A.entities() a
where a.City in (select b.City from B.entities() b)
   or a.Population > 400

-- Combined with AND
select a.City from A.entities() a
where a.City in (select b.City from B.entities() b)
  and a.Country = 'Poland'
```

#### 7.9.6 Correlated IN Subqueries

An `IN` or `NOT IN` subquery may reference aliases from the containing query block:

```sql
select a.City
from A.entities() a
where a.City in (
    select b.City
    from B.entities() b
    where b.Country = a.Country
)
```

Equality correlations are decorrelated to semi-join or anti-semi-join plans where possible, preserving one output row per matching left row. These internal plans share the same left-sided row and column semantics as direct `SEMI JOIN` and `ANTI JOIN` syntax. When an `IN` or `NOT IN` subquery appears inside an expression that must produce a Boolean value, Musoq may use a left-join/null-check fallback. That fallback is supported only for uncorrelated subqueries and equality-only correlations; non-equality correlated fallback shapes raise MQ2024 until APPLY fallback lowering is available.

### 7.10 EXISTS / NOT EXISTS

`EXISTS` tests whether a subquery returns at least one row. `NOT EXISTS` is the negated form:

```sql
select a.City
from A.entities() a
where exists (
    select b.City
    from B.entities() b
    where b.Country = a.Country
)

select a.City
from A.entities() a
where not exists (
    select b.City
    from B.entities() b
    where b.Country = a.Country
)
```

The selected expressions inside an `EXISTS` subquery are ignored for the truth value. They are parsed and bound only as far as needed for the query shape; correlation must normally be expressed in the subquery `WHERE`, join, grouping, or other filtering clauses. Equality-correlated `EXISTS` and `NOT EXISTS` predicates are lowered to semi-join or anti-semi-join plans where possible.

Predicate subqueries (`IN`, `NOT IN`, `EXISTS`, `NOT EXISTS`, and equality `ANY`/`SOME` forms) may also appear in Boolean expression contexts such as `SELECT`/`CASE`, `JOIN ON`, `WHERE`, `HAVING`, `QUALIFY`, and `ORDER BY`. Top-level `WHERE` predicates use semi/anti-semi joins when possible. Expression-producing contexts use a left-join/null-check fallback when the fallback is cardinality-safe. In `HAVING`, predicate subqueries can be moved before grouping only when every outer reference used by the predicate is a grouping key; predicates depending on non-grouped row values raise MQ2024.

### 7.11 Scalar Subqueries

A parenthesized `SELECT` or `FROM`-first query can appear as a scalar expression:

```sql
select a.City,
       (
           select b.City
           from B.entities() b
           where b.Country = a.Country
       ) as MatchCity
from A.entities() a
```

Scalar subqueries are valid in expression contexts including `SELECT`, `WHERE`, `JOIN ON`, `GROUP BY`, `HAVING`, `WINDOW`, `QUALIFY`, `ORDER BY`, function arguments, and `CASE` expressions.

Rules:

- A scalar subquery MUST return exactly one column. Otherwise Musoq raises MQ2024.
- A scalar subquery over a set operator is not supported.
- If the subquery returns zero rows, the scalar value is `NULL`.
- If the subquery returns more than one row, query execution raises a runtime error.
- Uncorrelated scalar subqueries may use result-shaping clauses such as `DISTINCT`, `GROUP BY`, `ORDER BY`, `SKIP`, `TAKE`, `WINDOW`, and `QUALIFY`. Musoq materializes the shaped subquery result and then applies scalar cardinality rules to the materialized rows.
- Correlated scalar aggregates are optimized as grouped aggregate plus left join where possible. Correlated scalar subqueries with result-shaping clauses that cannot be decorrelated accurately raise MQ2024.

### 7.12 Quantified Subqueries: ANY, SOME, ALL

Comparison predicates may use `ANY`, `SOME`, or `ALL` with a subquery:

```sql
where a.Population > ANY (select b.Population from B.entities() b)
where a.Population > ALL (select b.Population from B.entities() b)
where a.Country = SOME (select b.Country from B.entities() b)
```

`SOME` is a synonym for `ANY`. `= ANY` and `= SOME` are equivalent to `IN` and use the same semi-join lowering path.

Rules:

- Quantified subqueries MUST return exactly one column.
- Except for `= ANY` / `= SOME`, which lower to `IN`, quantified subqueries over set operators are not supported.
- `ANY` / `SOME` is true when at least one non-null comparison is true. If the subquery is empty, it is false.
- `ALL` is true when no row makes the comparison false or unknown. If the subquery is empty, it is true.
- Null operands make the comparison unknown for quantified predicates; unknown does not pass `WHERE` filtering.
- Correlated quantified subqueries are lowered through `EXISTS`, `NOT EXISTS`, `IN`, semi-join, or anti-semi-join plans when the rewritten predicate is decorrelatable. Non-equality quantified predicates used in expression fallback contexts raise MQ2024 until APPLY fallback lowering is available.

### 7.13 CONTAINS

Tests if a value matches any item in a literal list:

```sql
where Name contains ('ABC', 'CDA', 'EFG')
```

**NULL handling in CONTAINS:**
- `CONTAINS(null, 'a', 'b', 'c')` → `false` (null not in list)
- `CONTAINS(null, 'a', null, 'c')` → `true` (null explicitly in list)
- When the list itself cannot be constructed (null array), returns `false`.

### 7.14 Implicit Boolean Conversion

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

### 8.4 FULL OUTER JOIN

Returns all rows from both sides. Matched rows contain columns from both sources; left-only rows contain `null` for right-side columns; right-only rows contain `null` for left-side columns:

```sql
select a.City, b.Population
from A.entities() a
full outer join B.entities() b on a.City = b.City
```

`FULL OUTER JOIN` and `FULL JOIN` are equivalent.

Use alias row presence predicates when classifying full-join rows. They test whether a source alias that may be absent in the current scope contributed a row, independent of whether any projected column is `null`:

```sql
select
    case
        when b is missing then 'Added'
        when a is missing then 'Removed'
        when b.Hash <> a.Hash then 'Changed'
        else 'Unchanged'
    end as State
from AfterFiles.entities() a
full outer join BeforeFiles.entities() b on a.RelativePath = b.RelativePath
```

Do not use `b.Id is null` to decide that `b` is absent: `b.Id` can be `null` on a real row. Use `b is missing` instead. Row presence predicates are rejected for aliases that are always present, such as single-source aliases, inner-join aliases, cross-join aliases, cross-apply aliases, or the preserved side of an outer join. They cannot classify the unmatched side inside the same join's `ON` predicate, because unmatched rows are produced after the join condition is evaluated.

Musoq v1 intentionally does not define a `COMPARE` statement, `DIFF` statement, `ChangeKind`, or `DiffKind` helper. Rowset comparison is expressed with `FULL OUTER JOIN`, alias presence predicates, and `CASE`.

### 8.5 SEMI JOIN

Returns each left-side row for which at least one right-side row satisfies the join condition. The result exposes only left-side columns, and duplicate matching rows on the right side do not duplicate the left row:

```sql
select a.City
from A.entities() a
semi join B.entities() b on a.City = b.City
```

`LEFT SEMI JOIN` is equivalent to `SEMI JOIN`:

```sql
select a.City
from A.entities() a
left semi join B.entities() b on a.City = b.City
```

`SEMI JOIN` requires an `ON` condition. Right-side columns are available inside the `ON` condition but are not part of the join output.

### 8.6 ANTI JOIN

Returns each left-side row for which no right-side row satisfies the join condition. The result exposes only left-side columns:

```sql
select a.City
from A.entities() a
anti join B.entities() b on a.City = b.City
```

`ANTI SEMI JOIN` and `LEFT ANTI SEMI JOIN` are equivalent to `ANTI JOIN`:

```sql
select a.City
from A.entities() a
left anti semi join B.entities() b on a.City = b.City
```

`ANTI JOIN` requires an `ON` condition. Right-side columns are available inside the `ON` condition but are not part of the join output.

### 8.7 CROSS JOIN

Produces the Cartesian product of the left and right sources:

```sql
select a.Name, b.Name
from A.entities() a
cross join B.entities() b
```

`CROSS JOIN` does not accept an `ON` condition. Use `WHERE` to filter the Cartesian product:

```sql
select a.Name, b.Name
from A.entities() a
cross join B.entities() b
where a.Country = b.Country
```

Use `CROSS APPLY` or `OUTER APPLY` for lateral expansion where the right side must reference columns from the left side.

### 8.8 Multiple Joins

Chain multiple joins in a single query:

```sql
select a.City, b.Population, c.Area
from A.entities() a
inner join B.entities() b on a.City = b.City
inner join C.entities() c on a.City = c.City
```

### 8.9 Join Condition Expressions

Join conditions can contain expressions, not just simple column equality:

```sql
inner join B.entities() b on a.Id = b.Id + 1
inner join B.entities() b on a.Population > b.Population + 100
```

### 8.10 Function Calls in Multi-Source Queries

In queries with multiple data sources (joins, applies), each schema has its own method registry. When a function call is **unqualified** (no alias prefix), the engine uses the **method auto-resolution algorithm** described in §8.10.1 to determine which schema owns the method. When auto-resolution cannot determine a single owner, the caller must disambiguate by prefixing the function call with a table alias.

#### 8.10.1 Method Auto-Resolution Algorithm

When a function is called without an alias prefix in a multi-source query, the engine attempts to resolve the owning schema automatically. It does so by trying to bind the method against **every** schema in scope and then applying three rules in order:

1. **Common method rule.** If **all** candidate schemas resolve the method to the **same underlying implementation** (same function identity), the engine picks any one of them. This is the typical case for built-in library methods (e.g., `ToDecimal`, `Concat`, `Contains`) that every schema inherits from a shared base.

2. **Unique method rule.** If **exactly one** candidate schema resolves the method successfully, the engine picks that schema. This applies to methods that are unique to a particular schema's library.

3. **Ambiguity error.** If **two or more** candidate schemas resolve the method to **different implementations**, the engine cannot choose and raises diagnostic **MQ3035** (`AmbiguousMethodOwner`). The caller must add an alias prefix to disambiguate.

> **Aggregate methods** (those decorated with `[InjectGroup]`) follow a separate but analogous auto-resolution path that was already present before this algorithm was introduced. The rules above extend the same principle to all non-aggregate methods.

#### 8.10.2 Method Categories

Understanding how methods are classified helps predict when auto-resolution succeeds or fails:

| Category | Characteristic | Auto-Resolution |
|----------|---------------|-----------------|
| **Aggregate** | Decorated with `[InjectGroup]` (e.g., `Sum`, `Count`, `Avg`) | Resolved via dedicated aggregate inference; same common-method logic applies |
| **Pure utility** | No special injection; stateless (e.g., `ToDecimal`, `Concat`, `Abs`) | Almost always shared across schemas → Rule 1 resolves them |
| **Entity-bound** | Decorated with `[InjectSpecificSource]`; schema-specific | Resolves only in the schema that defines them → Rule 2 or Rule 3 applies |

#### 8.10.3 Examples

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

#### 8.10.4 Explicit Alias Prefix

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

#### 8.10.5 Best Practices

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

### 8.11 ASOF JOIN

ASOF JOIN matches each left-side row to the **single nearest** right-side row based on an ordered (inequality) column. Unlike a regular inequality join — which returns all matching rows — ASOF JOIN returns **at most one right-side row per left-side row**.

#### 8.11.1 Syntax

```sql
select <columns>
from <left_source> <alias>
asof join <right_source> <alias> on <conditions>
[tie break by <right_expression> [asc|desc] [nulls first|nulls last]]
```

The ON clause consists of:
- **Zero or more equality conditions** (partition columns) using `=`
- **Exactly one inequality condition** (the ordering/match column) using `>=`, `>`, `<=`, or `<`

#### 8.11.2 Inequality Operators

The inequality operator determines the match direction:

| Operator | Semantics | Description |
|----------|-----------|-------------|
| `>=` | Nearest right-side row where `left.col >= right.col` | Backward lookup ("as of") |
| `>` | Nearest right-side row where `left.col > right.col` | Strict backward (excludes exact match) |
| `<=` | Nearest right-side row where `left.col <= right.col` | Forward lookup ("what happened next") |
| `<` | Nearest right-side row where `left.col < right.col` | Strict forward (excludes exact match) |

#### 8.11.3 Basic Examples

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

#### 8.11.4 ASOF LEFT JOIN

When no match exists on the right side, `ASOF JOIN` drops the left row (like `INNER JOIN`). To preserve all left rows with NULLs on the right:

```sql
select c.Sha, c.Message, e.ErrorTime
from commits c
asof left join errors e on c.AuthorDate <= e.ErrorTime
```

`ASOF LEFT OUTER JOIN` is an accepted synonym.

#### 8.11.5 ASOF RIGHT JOIN — Not Supported

`ASOF RIGHT JOIN` is not supported. If attempted, the parser raises an error. The same result can be achieved by swapping table positions.

#### 8.11.6 Match Cardinality and Tie-Breaking

ASOF JOIN produces **at most one right-side row per left-side row**.

When multiple right-side rows have the exact same value on the inequality column (after equality partitioning), a `TIE BREAK BY` clause can choose deterministically among those equal-distance candidates:

```sql
select a.Name, b.Name
from A.entities() a
asof join B.entities() b on a.Population >= b.Population
tie break by b.UpdatedAt desc nulls last
```

Rules:

- The tie-break expression MUST reference the right-side alias only.
- `ASC` is the default direction; `DESC` reverses the tie-break ordering.
- `NULLS FIRST` and `NULLS LAST` are supported. If omitted, ascending tie-break keys place `NULL` first and descending tie-break keys place `NULL` last.
- Tie-break ordering applies only among candidates with the same ASOF inequality key and the same equality partition. It does not change which inequality key is nearest.
- If `TIE BREAK BY` is omitted, existing duplicate-candidate behavior is preserved.

#### 8.11.7 NULL Handling

- If the left-side inequality column is `NULL`: **no match** (row dropped for `ASOF JOIN`, NULLs on right for `ASOF LEFT JOIN`)
- If the right-side inequality column is `NULL`: **that right row is never a match candidate**
- If an equality column is `NULL` on either side: **no match** (standard SQL `NULL ≠ NULL` semantics)

#### 8.11.8 ON Clause Validation

The ASOF JOIN ON clause has specific requirements beyond normal joins:

1. **At least one inequality condition MUST exist.** All-equality conditions produce error `AsOfJoinRequiresInequality`.
2. **At most one inequality condition.** Multiple inequalities produce error `AsOfJoinSupportsOnlyOneInequality`.
3. **No OR in the ON clause.** OR breaks the partition-then-search semantics and produces error `AsOfJoinDoesNotSupportOr`.
4. **The inequality MUST reference both sides.** A one-sided inequality produces error `AsOfJoinInequalityMustReferenceBothSides`.
5. **The inequality column type MUST be orderable** (implement `IComparable`). Non-orderable types produce error `AsOfJoinInequalityColumnNotOrderable`.
6. **The tie-break expression MUST reference the right side only** and MUST have an orderable type.

See [§23](#23-error-catalog) for the full error catalog.

#### 8.11.9 Type Requirements

The inequality column MUST be of a comparable, orderable type. Supported types include:

- All numeric types (`int`, `long`, `decimal`, `double`, `float`, etc.)
- `DateTime`, `DateTimeOffset`, `TimeSpan`
- `string` (lexicographic ordering)
- Any type implementing `IComparable`

#### 8.11.10 Interaction with Other Clauses

| Clause | Behavior |
|--------|----------|
| `WHERE` | Applied **after** the ASOF JOIN, filters the joined result |
| `GROUP BY` | Works normally on the ASOF JOIN result |
| `ORDER BY` | Works normally on the ASOF JOIN result |
| `SKIP` / `TAKE` | Works normally on the ASOF JOIN result |
| CTEs | ASOF JOIN can reference CTE results as either side |
| Multiple JOINs | ASOF JOIN can be chained with other join types (including other ASOF JOINs) |
| `CROSS APPLY` | Can appear before or after ASOF JOIN in the query |

#### 8.11.11 Chaining Multiple ASOF JOINs

Multiple ASOF JOINs can be chained in a single query:

```sql
select a.Name, b.Population, c.Area
from A.entities() a
asof join B.entities() b on a.Population >= b.Population
asof join C.entities() c on a.Population >= c.Area
```

Each ASOF JOIN independently materializes and sorts its right side.

### 8.12 Keywords Are Case-Insensitive

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

### 9.5 APPLY WITH ORDINALITY

`WITH ORDINALITY` exposes a zero-based `Ordinal` column on the right-side APPLY alias:

```sql
select a.City, b.Value, b.Ordinal
from schema.first() a
cross apply a.Values b with ordinality
order by a.City, b.Ordinal
```

Rules:

- `WITH ORDINALITY` is supported for `CROSS APPLY` and `OUTER APPLY`.
- The `Ordinal` column is `int` for `CROSS APPLY`.
- The `Ordinal` column is `int?` for `OUTER APPLY`, because an unmatched right side is null-extended.
- Ordinality starts at `0` for each left-side row and each apply source.
- If the right-side alias already exposes an `Ordinal` column, the query is rejected.

### 9.6 Chaining Applies

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

### 9.7 APPLY with Derived Tables

`CROSS APPLY` and `OUTER APPLY` may use a parenthesized query as the right side. Unlike plain `FROM` or `JOIN` derived tables, an APPLY derived table may reference aliases from the left side:

```sql
select a.City, d.City
from A.entities() a
cross apply (
    select b.City, b.Country
    from B.entities() b
    where b.Country = a.Country
) d

select a.City, d.City
from A.entities() a
outer apply (
    select b.City, b.Country
    from B.entities() b
    where b.Country = a.Country
) d
```

`CROSS APPLY` returns only left rows with at least one right-side match. `OUTER APPLY` preserves unmatched left rows and fills right-side columns with `NULL`.

For correlated APPLY derived tables, Musoq requires the local columns used to decorrelate the right side to be visible in the derived-table output. If the query hides a required local correlation column, Musoq rejects the query with MQ2024 instead of exporting hidden columns.

An APPLY derived-table body may contain set operators when every set branch exposes the same projected correlation key. Musoq decorrelates each branch independently and joins the combined set result back to the left side by that shared key:

```sql
select a.City, d.City
from A.entities() a
cross apply (
    select b.City, b.Country
    from B.entities() b
    where b.Country = a.Country
    union (City, Country)
    select c.City, c.Country
    from C.entities() c
    where c.Country = a.Country
) d
```

If any branch hides the required correlation key, or branches expose incompatible correlation keys, Musoq raises MQ2024.

### 9.8 Multiple Independent Applies

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
SELECT group_column, aggregate_function(column) FROM ... GROUP BY 1
SELECT group_column, aggregate_function(column) FROM ... GROUP BY ALL
```

### 10.2 Aggregate Functions

Aggregate functions operate on groups of rows and return a single value per group. The following table lists common aggregate functions available across all schema providers. This list is illustrative, not exhaustive — additional aggregates may be available depending on the data source.

| Function | Description |
|----------|-------------|
| `Count(column)` | Number of non-null values in the group |
| `Count(*)` | Number of rows in the group |
| `Sum(column)` | Sum of numeric values in the group |
| `Avg(column)` | Arithmetic mean of numeric values in the group |
| `Min(column)` | Minimum value in the group |
| `Max(column)` | Maximum value in the group |
| `AggregateValues(column)` | Concatenates values into a comma-separated string (nulls become empty strings) |
| `AggregateValues(column, delimiter)` | Concatenates non-null values into a single string, separated by `delimiter` |

To discover all available aggregate functions for a given schema, use the `DESC FUNCTIONS` statement (see [§16.5](#165-describe-schema-functions)):

```sql
desc functions A.entities()
```

All numeric aggregation functions accept any numeric type (`byte`, `short`, `int`, `long`, `float`, `double`, `decimal`, etc.) as input. Different data sources may provide additional aggregate functions beyond the common set.

In a query with only one source, aggregates can be written without a source alias:

```sql
select Country, Sum(Population) from A.entities() group by Country
```

In a query with multiple sources, unqualified aggregate calls are resolved with the same method-owner auto-resolution rules described in §8.10.1. If exactly one alias can provide the aggregate implementation, the unqualified call is accepted:

```sql
select e.Department, Sum(ToDecimal(t.Amount))
from separatedvalues.comma('/adventure/data/employees.csv', true, 0) e
inner join separatedvalues.comma('/adventure/data/transactions.csv', true, 0) t
    on e.Id = t.EmployeeId
group by e.Department
```

You may still qualify an aggregate explicitly to choose the owner:

```sql
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
from schema.first() first
inner join schema.second() second on 1 = 1

-- CORRECT:
select first.AggregateMethodB()
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

-- Positional grouping by the first final SELECT expression
select City, Count(*) from A.entities() group by 1

-- Grouping by a non-aggregate SELECT alias
select City as c, Count(*) from A.entities() group by c

-- HAVING can reference aggregate aliases
select Count(*) as cnt from A.entities() group by City having cnt > 1
```

### 10.3.1 GROUP BY Positional References

A direct positive integer literal in `GROUP BY` is a SELECT-list ordinal, counted from 1 after final SELECT projection and star expansion:

```sql
select City, Count(*)
from A.entities()
group by 1
```

This groups by the first final SELECT expression (`City`). Multiple ordinals may be used:

```sql
select City, Country, Count(*)
from A.entities()
group by 1, 2
```

Rules:
- Ordinals are 1-based.
- `GROUP BY 0` and out-of-range ordinals fail with diagnostic `MQ3024`.
- Only direct positive integer literals are ordinals. Other constant expressions, such as `'all'` or `1 + 0`, are normal grouping expressions.
- The referenced SELECT expression MUST be valid as a grouping key. An ordinal that points at an aggregate-derived SELECT expression is rejected.
- Positional grouping does not affect `ORDER BY`; `ORDER BY 1` and `ORDER BY ALL` are not defined by this section.

### 10.3.2 GROUP BY ALL

`GROUP BY ALL` is shorthand for grouping by every final `SELECT` expression that is not aggregate-derived or window-derived.

```sql
select City, Country, Count(Name)
from A.entities()
group by all
```

This is equivalent to:

```sql
select City, Country, Count(Name)
from A.entities()
group by City, Country
```

The expansion runs after projection shaping. Star projections, `alias.*`, `LIKE`, `EXCLUDE`, `REPLACE`, and `RENAME` contribute their final selected scalar expressions as grouping keys:

```sql
select * like 'C%', Count(Name) as Rows
from A.entities()
group by all
```

Computed scalar expressions are also grouped automatically:

```sql
select ToLower(City) as CityKey, Sum(Population) as TotalPopulation
from A.entities()
group by all
having Sum(Population) > 1000
```

Expansion rules:
- Aggregate and window expressions are excluded from inferred grouping keys.
- Constants are valid inferred grouping keys.
- Duplicate inferred grouping expressions are deduplicated case-insensitively by expression text.
- If no grouping keys remain, Musoq uses a single constant grouping key, producing one aggregate group.
- `GROUP BY ALL` cannot be combined with explicit grouping expressions.
- Explicit grouping expressions, including positional ordinals, cannot be mixed with `GROUP BY ALL`.

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

Grouping by a non-ordinal constant treats all rows as a single group:

```sql
select Count(Country) from A.entities() group by 'fake'
-- Equivalent to aggregate without GROUP BY
```

Direct positive integer literals are positional references, not constants. Use a non-integer constant or a non-direct expression when a single constant group is intended.

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

HAVING can also use aggregate SELECT aliases and grouped non-aggregate aliases:

```sql
select City as c, Count(*) as cnt
from A.entities()
group by c
having cnt > 1 and c is not null
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

-- ERROR: SELECT * with explicit GROUP BY expands to non-aggregated columns
select * from A.entities() group by City
-- Throws NonAggregatedColumnInSelectException

-- VALID: GROUP BY ALL infers every expanded SELECT * column as a grouping key
select * from A.entities() group by all
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

### 10.10 FILTER Clause on Aggregates

The `FILTER` clause restricts which rows an aggregate function considers, providing a cleaner alternative to `CASE WHEN` inside the aggregate. `FILTER` is a **context-sensitive keyword** — it is only treated as a keyword immediately after a function call's closing parenthesis.

#### 10.10.1 Syntax

```sql
aggregate_function(expression) FILTER (WHERE condition)
```

The `FILTER` clause applies a row-level predicate *before* the aggregate processes the row. Rows for which the condition evaluates to `false` or `NULL` are excluded from that particular aggregate computation.

#### 10.10.2 Equivalence to CASE WHEN

Internally, `FILTER` is rewritten as a `CASE WHEN` expression. The following two queries are semantically equivalent:

```sql
-- FILTER syntax (preferred)
select Count(City) filter (where Population > 200) from A.entities()

-- Equivalent CASE WHEN
select Count(case when Population > 200 then City else null end) from A.entities()
```

#### 10.10.3 Supported Functions

The `FILTER` clause can be applied to **any aggregate function**: `Count`, `Sum`, `Min`, `Max`, `Avg`, `AggregateValues`, and any plugin-provided aggregate.

Applying `FILTER` to a non-aggregate function is a compile-time error (MQ3051).

#### 10.10.4 Examples

**Basic filtered count:**

```sql
select Count(City) filter (where Population > 200) from A.entities()
```

**Multiple aggregates with different filters:**

```sql
select
    Count(City) filter (where Country = 'Poland') as PolishCities,
    Count(City) filter (where Country = 'Germany') as GermanCities
from A.entities()
```

**FILTER with GROUP BY:**

```sql
select Country,
       Count(City) filter (where Population > 200) as LargeCities
from A.entities()
group by Country
```

**FILTER in HAVING:**

```sql
select Country,
       Count(City) filter (where Population > 200) as LargeCities
from A.entities()
group by Country
having Count(City) filter (where Population > 200) >= 2
```

#### 10.10.5 Rules

- `FILTER` is case-insensitive.
- `FILTER` MUST be followed by `(WHERE condition)`.
- The condition inside `FILTER` follows the same rules as a `WHERE` clause expression.
- `FILTER` MUST only be applied to aggregate functions. Applying it to a non-aggregate function raises compile-time error MQ3051.
- `FILTER` can be used together with `DISTINCT`: `Count(distinct City) filter (where Population > 200)`.

### 10.11 PIVOT Statement

`PIVOT` reshapes selected row values into aggregate output columns. Musoq supports a simplified static-pivot form with mandatory static pivot values:

```sql
pivot source
on key_expression in (constant_value as output_alias, ...)
using aggregate_function(...) as MeasureAlias
[group by row_expression, ...]
[order by order_expression [asc|desc], ...]
[skip integer]
[take integer]
```

The `IN (...)` list is required so the output schema is known at compile time. Dynamic pivot column discovery is not supported.

#### 10.11.1 Single-Key Example

```sql
pivot sales.orders()
on Quarter in ('Q1' as Q1, 'Q2' as Q2)
using Sum(Amount) as Sales, Count(*) as Orders
group by Region
order by Region
take 100
```

This query groups rows by `Region`. For each listed `Quarter` value, Musoq emits one filtered aggregate per measure.

#### 10.11.2 Output Column Names

`GROUP BY` expressions appear first in the output. Pivot aggregate columns are emitted in `IN` list order, and then in `USING` measure order.

If there is exactly one measure, the pivot value alias is the output column name:

```sql
pivot sales.orders()
on Quarter in ('Q1' as Q1)
using Sum(Amount) as Sales
group by Region
-- Output: Region, Q1
```

If there is more than one measure, Musoq uses `{pivot_alias}_{measure_alias}`:

```sql
pivot sales.orders()
on Quarter in ('Q1' as Q1)
using Sum(Amount) as Sales, Count(*) as Orders
group by Region
-- Output: Region, Q1_Sales, Q1_Orders
```

Generated output names MUST be unique. If aliases would produce duplicate names, compilation fails with MQ2008.

#### 10.11.3 Measures

`USING` accepts aggregate function calls only, including `DISTINCT` aggregate calls:

```sql
pivot sales.orders()
on Quarter in ('Q1' as Q1)
using Count(distinct CustomerId) as Customers
group by Region
```

Non-aggregate scalar functions in `USING` are rejected.

#### 10.11.4 No GROUP BY

If `GROUP BY` is omitted, all input rows form a single aggregate group:

```sql
pivot sales.orders()
on Quarter in ('Q1' as Q1, 'Q2' as Q2)
using Count(*) as Orders
```

The result has one row.

#### 10.11.5 Multi-Column Pivot Keys

Multiple `ON` expressions are matched with tuple values in the `IN (...)` list:

```sql
pivot cities
on Year, Country in ((2000, 'NL') as y2000_nl, (2020, 'PL') as y2020_pl)
using Sum(Population) as Total
group by Name
```

Every tuple MUST contain the same number of values as the `ON` key list. A mismatch is a compile-time error.

#### 10.11.6 NULL Values

`NULL` pivot values match source rows where the corresponding pivot key is `NULL`:

```sql
pivot sales.orders()
on Quarter in (null as Missing, 'Q1' as Q1)
using Count(*) as Orders
```

#### 10.11.7 Composition

A pivot query is select-like. It can appear at the top level, inside a CTE, or as a derived table:

```sql
with p as (
    pivot sales.orders()
    on Quarter in ('Q1' as Q1)
    using Sum(Amount) as Sales
    group by Region
)
select Region, Q1 from p
```

```sql
select p.Region, p.Q1
from (
    pivot sales.orders()
    on Quarter in ('Q1' as Q1)
    using Sum(Amount) as Sales
    group by Region
) p
```

#### 10.11.8 Validation Rules

- `PIVOT` MUST specify `ON`.
- `ON` MUST be followed by a static `IN (...)` list.
- `IN` values MUST be constants: string, numeric, boolean, date/time strings coerced by normal comparison rules, or `NULL`.
- `USING` MUST contain at least one aggregate function call.
- Generated output column names MUST be unique.
- Multi-column pivot value tuples MUST match the number of `ON` expressions.

### 10.12 UNPIVOT Statement

`UNPIVOT` reshapes columns or expressions from each source row into multiple output rows. Musoq supports a static, explicit form:

```sql
unpivot source
on name_column in (value_expression [[as] name_value], ...)
using value_column
[keep expression [[as] output_alias], ...]
[order by order_expression [asc|desc], ...]
[skip integer]
[take integer]
```

The source is scanned once. For each source row, Musoq emits one output row per `IN (...)` entry, preserving the entry order. `ORDER BY`, `SKIP`, and `TAKE` are evaluated after row expansion. Implementations SHOULD stream this expansion internally rather than materializing an intermediate expanded row list, but final query results are still materialized according to the normal Musoq result-table behavior.

#### 10.12.1 Example

```sql
unpivot #sales.wide() s
on Quarter in (s.Q1 as Q1, s.Q2 as Q2, s.Q3 as Q3)
using Sales
keep s.Region as Region
order by Region, Quarter
```

For each input row, this query emits one row for `Q1`, then one for `Q2`, then one for `Q3`. The output columns are `Region`, `Quarter`, and `Sales`.

#### 10.12.2 Output Columns

Output columns are emitted in this order:

1. `KEEP` fields, in the order written.
2. The generated name column declared after `ON`.
3. The generated value column declared after `USING`.

The generated name column has type `string`. The generated value column uses the common type of the `IN (...)` value expressions.

`KEEP` is explicit. Source columns that are not listed in `KEEP` are not carried forward automatically:

```sql
unpivot #sales.wide() s
on Metric in (s.Population as Population, s.Money as Money)
using Amount
keep s.Country as Country, s.Name as City
```

`KEEP` fields may be expressions and may use aliases. If an alias is omitted, Musoq can derive one only for simple identifiers or property references. Complex `KEEP` expressions MUST use an explicit alias.

#### 10.12.3 Name Values

Each `IN (...)` entry pairs a value expression with the string value emitted in the generated name column:

```sql
on Metric in (s.Population as Population, s.Money as Money)
```

Aliases may be omitted only for simple identifiers or property references where Musoq can derive a stable name:

```sql
on Metric in (s.Population, s.Money)
```

Complex expressions require an explicit alias:

```sql
on Metric in ((s.Population + s.Money) as Total)
```

#### 10.12.4 Type and NULL Rules

The generated value column uses the common type of all `IN (...)` value expressions, using the same compatibility rules as inline `VALUES` fields. Numeric values can widen when the existing type system allows it. If any value can be `NULL`, including an explicit `NULL` literal or a nullable value-type expression, the value column is nullable. Incompatible mixes, such as numeric and string expressions, are rejected.

`NULL` values are preserved. UNPIVOT does not filter null value rows implicitly.

#### 10.12.5 Composition

An unpivot query is select-like. It can appear at the top level, inside a CTE, as a derived table, in joins, and in set-operator branches:

```sql
with u as (
    unpivot #sales.wide() s
    on Quarter in (s.Q1 as Q1, s.Q2 as Q2)
    using Sales
    keep s.Region as Region
)
select Region, Quarter, Sales from u
```

```sql
select u.Region, u.Quarter, u.Sales
from (
    unpivot #sales.wide() s
    on Quarter in (s.Q1 as Q1)
    using Sales
    keep s.Region as Region
) u
```

#### 10.12.6 Validation Rules

- `UNPIVOT` MUST specify `ON`.
- `ON` MUST be followed by the generated name column and a static `IN (...)` list.
- `IN (...)` MUST contain at least one value expression.
- `USING` MUST specify the generated value column name.
- `IN (...)` aliases become string values in the generated name column.
- `IN (...)` aliases may be omitted only for simple identifiers or property references.
- `KEEP` is optional and explicit; unmentioned source columns are not automatically included.
- `KEEP` aliases may be omitted only for simple identifiers or property references; complex `KEEP` expressions MUST have explicit aliases.
- Generated output column names from `KEEP`, `ON`, and `USING` MUST be unique.
- `IN (...)` name values MUST be unique.
- Rows with `NULL` value expressions MUST be emitted.
- Dynamic source-column discovery is not supported.

---

## 11. Window Functions

### 11.1 Overview

Window functions perform calculations across a set of rows that are related to the current row, without collapsing the result into a single output row the way aggregate functions with `GROUP BY` do. Every input row produces exactly one output row. Window functions are specified using an aggregate or ranking function followed by an `OVER` clause that defines the window (partitioning, ordering, or both).

Window functions execute **after** `WHERE` filtering and **after** `GROUP BY` / `HAVING`, but **before** `ORDER BY`, `SKIP`, and `TAKE`.

### 11.2 Syntax

```sql
function_name([arguments]) OVER (
    [PARTITION BY expression {, expression}]
    [ORDER BY field [ASC | DESC] [NULLS FIRST | NULLS LAST] {, field [ASC | DESC] [NULLS FIRST | NULLS LAST]}]
    [frame_specification]
)
```

Where `frame_specification` is:

```sql
(ROWS | RANGE) BETWEEN frame_bound AND frame_bound
```

And `frame_bound` is one of:

```
UNBOUNDED PRECEDING
N PRECEDING
CURRENT ROW
N FOLLOWING
UNBOUNDED FOLLOWING
```

See section 11.12 for full details on frame specifications.

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
select Name, City, Count(Name) over (partition by City) from a.entities()
```

Multiple partition columns are supported — rows are grouped by the composite key:

```sql
select Name, Sum(Population) over (partition by Country, City) from a.entities()
```

When `PARTITION BY` is omitted, all rows belong to a single partition:

```sql
-- Running sum across all rows
select Name, Sum(Population) over (order by Name) from a.entities()
```

### 11.4 ORDER BY within Window Specification

`ORDER BY` inside the `OVER` clause determines the logical row ordering within each partition. Both `ASC` (default) and `DESC` are supported, and each key may use `NULLS FIRST` or `NULLS LAST`.

- **Ranking functions** (`RowNumber`, `Rank`, `DenseRank`) require `ORDER BY` — it determines the assignment of ranks.
- **Aggregate functions** (`Sum`, `Count`, `Avg`, `Min`, `Max`) behave differently depending on whether `ORDER BY` is present:
  - **With `ORDER BY`**: Computes a running (cumulative) result from the first row in the partition up to the current row.
  - **Without `ORDER BY`**: Computes the result over the entire partition — every row in the partition receives the same value.
- **Offset functions** (`Lag`, `Lead`) require `ORDER BY` — it defines which row is "previous" or "next".

```sql
-- Running sum (with ORDER BY)
select Name, Sum(Population) over (order by Name) from a.entities()

-- Partition total (without ORDER BY)
select Name, City, Sum(Population) over (partition by City) from a.entities()

-- Descending order
select Name, RowNumber() over (order by Name desc) from a.entities()
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
select Name, RowNumber() over (order by Name) from a.entities()

-- Rank with ties: 1, 2, 2, 4
select Name, Rank() over (order by Population) from a.entities()

-- DenseRank without gaps: 1, 2, 2, 3
select Name, DenseRank() over (order by Population) from a.entities()

-- Ntile: distribute 5 rows into 3 buckets → 1, 1, 2, 2, 3
select Name, Ntile(3) over (order by Name) from a.entities()
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
select Name, Lag(Population) over (order by Name) from a.entities()

-- Next row's Population (NULL for the last row)
select Name, Lead(Population) over (order by Name) from a.entities()
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
select Name, Sum(Population) over (order by Name) from a.entities()

-- Count per partition
select Name, City, Count(Name) over (partition by City) from a.entities()

-- Running average
select Name, Avg(Population) over (order by Name) from a.entities()

-- Partition minimum and maximum
select Name, City, Min(Population) over (partition by City) from a.entities()
select Name, City, Max(Population) over (partition by City) from a.entities()
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
select Name, FirstValue(Name) over (order by Name) from a.entities()

-- Running last value (equals current row's value)
select Name, LastValue(Name) over (order by Name) from a.entities()

-- Partition-wide last value (all rows same)
select Name, LastValue(Population) over () from a.entities()

-- Second value in partition (NULL for the first row)
select Name, NthValue(Name, 2) over (order by Name) from a.entities()

-- NthValue per partition
select Name, City, NthValue(Name, 2) over (partition by City order by Name)
from a.entities()
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
select Name, RowNumber() over (order by Name) from a.entities()
select Name, ROW_NUMBER() over (order by Name) from a.entities()
```

### 11.7 WHERE Clause Interaction

`WHERE` filtering occurs **before** window function computation. Rows excluded by `WHERE` are not visible to the window function:

```sql
-- Only rows with Population > 150 participate in the window
select Name, RowNumber() over (order by Name)
from a.entities()
where Population > 150
```

If the original table has 5 rows and 3 satisfy the `WHERE` predicate, the window function operates on 3 rows, and `RowNumber()` produces values 1, 2, 3.

### 11.8 Evaluation Order

Window functions occupy a specific position in the query evaluation pipeline:

1. `FROM` — data source enumeration
2. `WHERE` — row-level filtering
3. `GROUP BY` / `HAVING` — aggregation (if present)
4. **Window functions** — computed over the filtered / grouped result
5. `QUALIFY` — filters rows based on window function results (see section 11.13)
6. `ORDER BY` — final ordering of the output
7. `SKIP` / `TAKE` — pagination

This means window functions cannot appear in `WHERE` or `HAVING` clauses. They can only appear in the `SELECT` list. To filter rows based on window function results, use the `QUALIFY` clause.

### 11.9 Restrictions

The following features are **not** currently supported:

- **`GROUPS BETWEEN` frame specifications** — `ROWS BETWEEN` is fully supported and `RANGE BETWEEN` is accepted with the restrictions documented in section 11.12.
- **Recursive or nested window functions** — a window function cannot reference another window function in its arguments.
- **Window functions in `WHERE` or `HAVING`** — only allowed in the `SELECT` list. Use `QUALIFY` (section 11.13) to filter on window function results.

### 11.10 Custom Window Functions

Data-source plugins can register custom window functions beyond the built-in set. Once registered, they are used with the standard `OVER` syntax like any other window function:

```sql
select Name, RunningProduct(Population) over (partition by City order by Name)
from a.entities()
```

Custom window functions support `PARTITION BY`, `ORDER BY`, and multi-argument signatures — the same capabilities as the built-in functions.

### 11.11 Frame Semantics and Differences from Standard SQL

Musoq's implicit ordered window frames use **ROWS semantics**, not the peer-aware RANGE semantics that PostgreSQL and the SQL standard default to. Explicit `RANGE BETWEEN` syntax is accepted, but it currently uses the engine's existing ordered-frame execution semantics rather than SQL peer-group expansion. This section documents the exact behavior and where it diverges from standard SQL.

#### 11.11.1 Implicit Frame

When no explicit frame specification is provided, every window function operates under a fixed implicit frame:

| Condition | Implicit Frame (Musoq) | PostgreSQL Default |
|-----------|------------------------|--------------------|
| `ORDER BY` present | `ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW` | `RANGE BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW` |
| `ORDER BY` absent | Entire partition (all rows receive the same value) | Entire partition (same) |

The difference between ROWS and RANGE only matters **when ORDER BY values contain ties** (duplicate values).

When an explicit `ROWS BETWEEN` or `RANGE BETWEEN` frame is specified, it overrides the implicit frame. See section 11.12 for the explicit frame specification syntax.

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

Musoq supports `NULLS FIRST` / `NULLS LAST` in top-level `ORDER BY`, window `OVER (ORDER BY ...)`, and ASOF `TIE BREAK BY` order keys. If the syntax is omitted, the default NULL ordering is:

| Sort Direction | NULL Position | Matches PostgreSQL Default? |
|----------------|:------------:|:---------------------------:|
| `ASC` | First | Yes |
| `DESC` | Last | No (PostgreSQL puts NULLs first in DESC) |

Explicit `NULLS FIRST` or `NULLS LAST` overrides the default for that individual key.

NULL values in `PARTITION BY` columns are grouped together as their own partition, consistent with standard SQL.

#### 11.11.5 Unsupported Standard SQL Window Features

| Feature | Standard SQL | Musoq |
|---------|-------------|-------|
| `ROWS BETWEEN ... AND ...` | Explicit row-based frame | **Supported** (see section 11.12) |
| `RANGE BETWEEN ... AND ...` | Explicit value-based frame | Partial — accepted, requires window `ORDER BY`, uses existing ordered-frame semantics |
| `GROUPS BETWEEN ... AND ...` | Explicit peer-group-based frame | Not supported |
| `EXCLUDE CURRENT ROW` | Excludes current row from frame | Not supported |
| `EXCLUDE GROUP` | Excludes current row's peer group | Not supported |
| `EXCLUDE TIES` | Excludes peers of current row (keeps current) | Not supported |
| `NULLS FIRST` / `NULLS LAST` | Explicit NULL ordering | **Supported** |
| `FILTER (WHERE ...)` | Conditional window aggregation | **Supported** for aggregate window functions |
| `QUALIFY` | Filter on window function results | **Supported** (see section 11.13) |
| `PERCENT_RANK()` | Relative rank as fraction | Not implemented |
| `CUME_DIST()` | Cumulative distribution | Not implemented |

#### 11.11.6 Practical Implications

For most real-world queries, Musoq produces identical results to PostgreSQL because:

1. **Ranking functions** (`RowNumber`, `Rank`, `DenseRank`) are unaffected by frame mode.
2. **Offset functions** (`Lag`, `Lead`) are inherently position-based in all SQL engines.
3. **Aggregates without ORDER BY** compute the whole partition — identical in all modes.
4. **Aggregates with ORDER BY on a unique column** (timestamps, IDs) produce identical results since there are no ties.

The divergence only appears in the specific combination of **running aggregates + non-unique ORDER BY values**. If you need peer-aware behavior, add a tiebreaker column to ORDER BY to make values unique, or use `PARTITION BY` on the grouping column instead.

### 11.12 Window Frame Specifications

Frame specifications control which rows within a partition are included in the window function computation for each row. When an explicit frame is provided, it overrides the default implicit frame (section 11.11.1).

#### 11.12.1 Syntax

```sql
ROWS BETWEEN frame_bound AND frame_bound
```

Where `frame_bound` is one of:

| Bound | Meaning |
|-------|---------|
| `UNBOUNDED PRECEDING` | The first row in the partition |
| `N PRECEDING` | `N` rows before the current row |
| `CURRENT ROW` | The current row |
| `N FOLLOWING` | `N` rows after the current row |
| `UNBOUNDED FOLLOWING` | The last row in the partition |

The first bound specifies the start of the frame, and the second specifies the end. The start must not be later than the end:

```sql
-- Valid: 1 row before to 1 row after (sliding window of 3 rows)
Sum(Population) OVER (ORDER BY Name ROWS BETWEEN 1 PRECEDING AND 1 FOLLOWING)

-- Valid: everything up to the current row
Sum(Population) OVER (ORDER BY Name ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)

-- Valid: current row to end of partition
Sum(Population) OVER (ORDER BY Name ROWS BETWEEN CURRENT ROW AND UNBOUNDED FOLLOWING)

-- Valid: entire partition (same as no frame with no ORDER BY)
Sum(Population) OVER (ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
```

#### 11.12.2 Frame Bound Clamping

Frame bounds are automatically clamped to the partition boundaries:

- If the start bound resolves to before the first row of the partition, it is clamped to the first row.
- If the end bound resolves to after the last row of the partition, it is clamped to the last row.

This means `2 PRECEDING` on the first row of a partition includes only the current row (no error is raised).

#### 11.12.3 Supported Frame Types

| Frame Type | Supported | Notes |
|------------|:---------:|-------|
| `ROWS BETWEEN` | Yes | Row-based frame — counts physical rows |
| `RANGE BETWEEN` | Partial | Parsed and accepted, but requires `ORDER BY` in the window specification (error MQ3052 if missing) |
| `GROUPS BETWEEN` | No | Peer-group-based frame — not supported |

`ROWS BETWEEN` is fully supported. `RANGE BETWEEN` is parsed and requires an `ORDER BY` clause in the window specification; omitting `ORDER BY` with a `RANGE` frame raises compile-time error MQ3052. `RANGE BETWEEN` currently uses the same existing ordered-frame execution model as `ROWS` rather than peer-aware SQL RANGE semantics. `GROUPS BETWEEN` is not supported.

#### 11.12.4 Examples

**Sliding window sum (3-row window):**

```sql
select Name, Population,
       Sum(Population) over (order by Name rows between 1 preceding and 1 following)
from a.entities()
```

For rows ordered A, B, C, D, E with populations 100, 200, 300, 400, 500:
- Row A: Sum(100, 200) = 300 (no preceding row)
- Row B: Sum(100, 200, 300) = 600
- Row C: Sum(200, 300, 400) = 900
- Row D: Sum(300, 400, 500) = 1200
- Row E: Sum(400, 500) = 900 (no following row)

**Moving average with fixed lookback:**

```sql
select Name, Population,
       Avg(Population) over (order by Name rows between 2 preceding and current row)
from a.entities()
```

**Full partition aggregate with explicit frame:**

```sql
select Name, Population,
       Sum(Population) over (rows between unbounded preceding and unbounded following) as Total
from a.entities()
```

This produces the same result as `Sum(Population) over ()` — every row receives the total of all rows.

**Partitioned frame:**

```sql
select Name, City, Population,
       Sum(Population) over (
           partition by City
           order by Name
           rows between 1 preceding and 1 following
       )
from a.entities()
```

The frame operates independently within each partition defined by `City`.

### 11.13 QUALIFY Clause

The `QUALIFY` clause filters rows based on the results of window functions, similar to how `HAVING` filters rows after `GROUP BY`. It executes **after** window function computation and **before** `ORDER BY`, `SKIP`, and `TAKE`.

#### 11.13.1 Syntax

```sql
SELECT columns, window_function() OVER (...) as alias
FROM source
[WHERE condition]
[GROUP BY columns]
[HAVING condition]
WINDOW window_name AS (window_specification)
QUALIFY boolean_expression
[ORDER BY columns]
[SKIP n] [TAKE n]
```

The `QUALIFY` expression must evaluate to a boolean. It can reference any column or alias from the `SELECT` list, including window function results.

#### 11.13.2 Execution Order

`QUALIFY` is evaluated after window functions are computed but before `ORDER BY` and `SKIP`/`TAKE`. This means:

1. All window function results are available in the `QUALIFY` expression.
2. Rows filtered by `QUALIFY` do not count toward `SKIP`/`TAKE` limits.
3. `ORDER BY` operates only on rows that pass the `QUALIFY` filter.

#### 11.13.3 Examples

**Top N per partition (most common use case):**

```sql
select Name, City, RowNumber() over (partition by City order by Name) as rn
from a.entities()
qualify rn <= 2
```

Returns at most 2 rows per `City`, ordered by `Name` within each city.

**Filter by ranking:**

```sql
select Name, Population, DenseRank() over (order by Population desc) as dr
from a.entities()
qualify dr <= 3
```

Returns rows with the top 3 distinct population values.

**QUALIFY with WHERE:**

```sql
select Name, City, RowNumber() over (order by Name) as rn
from a.entities()
where Population > 100
qualify rn <= 3
```

`WHERE` filters rows before window computation. `QUALIFY` filters after. In this example, only rows with `Population > 100` are numbered, and then only the first 3 are returned.

**QUALIFY with SKIP/TAKE:**

```sql
select Name, RowNumber() over (order by Name) as rn
from a.entities()
qualify rn <= 5
skip 1 take 2
```

First, `QUALIFY` keeps only rows where `rn <= 5` (at most 5 rows). Then `SKIP 1 TAKE 2` paginates within those 5 rows, returning rows 2 and 3.

#### 11.13.4 Comparison with CTE Workaround

Before `QUALIFY` was available, the equivalent could be achieved with a CTE:

```sql
-- Without QUALIFY (CTE workaround)
with ranked as (
    select Name, City, RowNumber() over (partition by City order by Name) as rn
    from a.entities()
)
select Name, City, rn from ranked where rn <= 2

-- With QUALIFY (more concise)
select Name, City, RowNumber() over (partition by City order by Name) as rn
from a.entities()
qualify rn <= 2
```

Both produce identical results. `QUALIFY` is more concise and avoids the overhead of materializing intermediate CTE results.

---

## 12. Set Operations

### 12.1 Syntax

Set operation key lists are optional:

```sql
query1 UNION query2
query1 UNION ALL query2
query1 EXCEPT query2
query1 INTERSECT query2
query1 UNION () query2
query1 UNION (key_col1, key_col2) query2
query1 UNION ALL (key_col1) query2
query1 EXCEPT (key_col1) query2
query1 INTERSECT (key_col1) query2
```

If the key list is omitted, or if the key list is written as empty parentheses `()`, Musoq uses all projected values from the left query in ordinal order as the effective comparison key.

The key columns specify which projected values are used to determine row identity for deduplication, difference, or intersection. Explicit key names are resolved against the left query projection. The resulting ordinal positions are then compared on both sides.

Both sides of a set operation MUST return the same number of projected values. Corresponding projected values MUST have compatible types.

`UNION ALL` accepts omitted, empty, and explicit key syntax for consistency, but it preserves every row from both inputs and does not use the key list to remove duplicates.

### 12.2 UNION

Combines results from two queries, removing duplicates identified by the effective key. With omitted keys, every projected value participates:

```sql
select Name, City from A.Entities()
union
select Name, City from B.Entities()
```

With an explicit subset key, only the named projected values participate:

```sql
select Name, City from A.Entities()
union (Name)
select Name, City from B.Entities()
```

### 12.3 UNION ALL

Combines results preserving all rows including duplicates. The key list may be omitted:

```sql
select Name from A.Entities()
union all
select Name from B.Entities()
```

An explicit key list is accepted for syntactic consistency but does not change `UNION ALL` behavior:

```sql
select Name from A.Entities()
union all (Name)
select Name from B.Entities()
```

### 12.4 EXCEPT

Returns rows from the first query whose effective key does not appear in the second query:

```sql
select Name, City from A.Entities()
except
select Name, City from B.Entities()
```

### 12.5 INTERSECT

Returns only rows whose effective key appears in both queries:

```sql
select Name, City from A.Entities()
intersect
select Name, City from B.Entities()
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
select City from A.Entities() where Money > 200
union (City)
select City from A.Entities() where Money <= 200
order by City desc    -- sorts only the right-hand query
```

This means the final output order after the UNION is **not guaranteed** to be sorted.

**Workaround** — wrap the set operation in a CTE and apply `ORDER BY` on the outer query:

```sql
with combined as (
    select City from A.Entities() where Money > 200
    union (City)
    select City from A.Entities() where Money <= 200
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
select Name from A.entities() order by Name nulls last
select Name from A.entities() order by Name desc nulls first
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

`NULLS FIRST` and `NULLS LAST` may be added after each order key. If omitted, ascending keys place `NULL` first and descending keys place `NULL` last.

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

### 14.3 Output Column Names and Aliases

A CTE is a named result set. Query blocks outside the CTE body see only the CTE output column names.

The output name for each CTE column is determined as follows:

1. An explicit SELECT alias is the output name.
2. An unaliased simple column projection exposes the column name itself.
3. A source qualifier used inside the CTE body is not exported.
4. Complex expressions SHOULD be explicitly aliased when the CTE result is consumed by name.

```sql
with p as (
    select a.City from A.entities() a
)
select City from p          -- valid

-- INVALID because a is local to the CTE body
select [a.City] from p
```

To expose a dotted name, declare that name explicitly:

```sql
with p as (
    select a.City as [a.City] from A.entities() a
)
select [a.City] from p
```

Musoq MUST NOT automatically prefix duplicate implicit output names with the source alias. If a CTE projects same-named columns from multiple sources, query authors SHOULD provide explicit output aliases:

```sql
with p as (
    select
        a.Country as LeftCountry,
        b.Country as RightCountry
    from A.entities() a
    inner join B.entities() b on a.Id = b.Id
)
select LeftCountry, RightCountry from p
```

Source aliases are scoped to the query block where they are declared. Aliases declared inside a CTE body are not visible outside that body.

CTE definitions are independent named query expressions. A CTE definition MUST NOT reference aliases from a query block that later consumes the CTE. Subqueries nested inside a CTE body may correlate only to aliases visible in that CTE body's own query block:

```sql
with matched as (
    select a.City
    from A.entities() a
    where exists (
        select b.City
        from B.entities() b
        where b.Country = a.Country
    )
)
select City from matched
```

### 14.4 Star Expansion from CTE

```sql
with p as (
    select City, Country from A.entities()
)
select * from p    -- expands to City, Country
```

Qualified star follows the same output-name rules:

```sql
with p as (
    select a.* from A.entities() a
)
select * from p    -- expands to the source column names, for example City, Country
```

### 14.5 CTE with Aggregation

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

### 14.6 Multiple CTEs

Define multiple CTEs separated by commas:

```sql
with
    cities as (select City, Country from A.entities()),
    countries as (select distinct Country from A.entities())
select * from cities
```

### 14.7 CTE with Set Operations

```sql
with combined as (
    select Name from A.Entities()
    union (Name)
    select Name from B.Entities()
)
select * from combined
```

### 14.8 CTE with JOIN

```sql
with p as (select City, Country from A.entities())
select p.City, b.Population
from p
inner join B.entities() b on p.City = b.City
```

If a CTE reference is given a table alias in `FROM`, that alias is the only qualifier visible for that CTE in the current query block:

```sql
with p as (select City, Country from A.entities())
select c.City from p c       -- valid

-- INVALID in the same query block
select p.City from p c
```

Unqualified identifiers in expression contexts resolve to columns before table or CTE names. This allows a CTE to expose a column with the same name as the CTE itself:

```sql
with cte as (
    select City as cte from A.entities()
)
select cte from cte          -- reads the column named cte
```

### 14.9 Limitations

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

The TABLE and COUPLE statements are summarized here. For the complete specification including all supported types, source runtime settings profile binding, error handling, and integration patterns, see *Musoq TABLE/COUPLE Statements Specification* (`musoq-table-couple-spec.md`).

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

Binds a schema method to a table structure, a source runtime settings profile, or both, creating a new data source alias:

```sql
couple schema.Method with table TableName as AliasName;
couple schema.Method with settings ProfileName as AliasName;
couple schema.Method with table TableName and settings ProfileName as AliasName;
couple schema.Method with settings ProfileName and table TableName as AliasName;
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

The `settings` option selects a host-resolved source runtime settings profile. Settings-only couples infer table metadata from the underlying schema; table-and-settings couples use the declared table shape and selected profile. Explicit SQL-level profile selection is available only through `couple`.

**Settings example:**

```sql
couple api.items with settings prod as ProdItems;
select Id, Name from ProdItems();
```

For table-and-settings examples, repeated source profile selection, and full option-order rules, see *Musoq TABLE/COUPLE Statements Specification* (`musoq-table-couple-spec.md`).

### 15.3 Purpose

TABLE and COUPLE are used when:
- The data source returns untyped or dynamically-typed rows
- You want to project a subset of columns with explicit types
- You need to select a named source runtime settings profile for one source context
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

### 16.6 Describe Source Runtime Settings

Inspect source runtime setting requirements and resolution status:

```sql
desc settings A.entities;
desc settings CoupledAlias;
```

For a coupled alias, the selected settings profile is the one declared by the `couple` statement:

```sql
couple api.items with settings prod as ProdItems;
desc settings ProdItems;
```

Returns a table with columns: `Name`, `Required`, `Secret`, `Phases`, `Status`, `Description`.

`Status` is `Provided`, `Missing`, or `Default`. Setting values are never returned, even for non-secret settings. A coupled alias form uses the settings profile selected by the `couple ... with settings ...` statement.

### 16.7 Describe Query Output

Describe the projected output columns of a query without executing the query against source rows:

```sql
desc query (
    select Name as PersonName, Population + Money as Total
    from A.entities()
)
```

The inner query is parsed and bound normally so aliases, star expansion, CTEs, parameters, script variables, set operators, and validation rules match normal query compilation. Execution returns only metadata, not the query's data rows.

The result shape is:

- `Name`
- `Index`
- `Type`

One row is returned for each projected output column in final projection order.

### 16.8 General DESC Rules

- `DESC`, `FUNCTIONS`, `SETTINGS`, and `COLUMN` are case-insensitive.
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

Use `IS DISTINCT FROM` or `IS NOT DISTINCT FROM` when equality itself should be null-safe:

```sql
where LeftValue is distinct from RightValue
where LeftValue is not distinct from RightValue
```

`NULL IS NOT DISTINCT FROM NULL` is `true`; `NULL IS DISTINCT FROM 1` is `true`.

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

When `LEFT JOIN`, `RIGHT JOIN`, `FULL OUTER JOIN`, `OUTER APPLY`, or `ASOF LEFT JOIN` produces an unmatched side, columns from the missing side are `null`. Value types are automatically promoted to nullable:

```sql
-- If no match, b.Population becomes decimal? with value null
select a.City, b.Population
from A.entities() a
left join B.entities() b on a.City = b.City
```

Use alias row presence predicates to distinguish an absent row from a present row whose columns contain `null`:

```sql
where b is missing
where b is present
```

`b is missing` is true only when alias `b` did not contribute a row. It is not equivalent to `b.Id is null`. The alias must be one that can be absent in the current scope because of `LEFT JOIN`, `RIGHT JOIN`, `FULL OUTER JOIN`, `ASOF LEFT JOIN`, or `OUTER APPLY`; always-present aliases are rejected rather than treated as constant true or false.

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

### 19.7 NULL Fallback Operator

The `??` operator returns the first operand when that operand is not `null`; otherwise, it evaluates to the fallback operand:

```sql
select Name ?? 'Unknown' from system.dual()
select NullableValue ?? 0 from A.entities()
select null ?? Name from A.entities()
```

The result type is the compatible operand type selected by semantic analysis. For `Nullable<T> ?? T`, the result type is `T`. If the left operand is a statically non-nullable value type, Musoq treats the fallback as unreachable and uses the left operand directly. `??` is not a null-navigation operator; `?.` is not part of the language.

### 19.8 NULL-Related Functions

Musoq provides functions for working with `NULL` values, such as coalescing, null-checking, and replacing nulls with defaults. To discover all available NULL-handling functions and their signatures, use `desc functions` (see [§16.5](#165-describe-schema-functions)).

### 19.9 NULL in Functions

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
| Unknown column or alias | A referenced source column or eligible SELECT alias cannot be resolved | `UnknownColumnOrAliasException` |
| GROUP BY ordinal out of range (MQ3024) | `GROUP BY 0` or an ordinal greater than the final SELECT-list width | `GroupByIndexOutOfRangeException` |
| Unsupported strict cast target (MQ2030) | `expr::TypeName` uses a type name outside the supported CLR-name set | `UnsupportedSyntaxException` |
| Duplicate alias in join | Using the same alias for two tables | `AliasAlreadyUsedException` |
| Division by zero (literal) | `10 / 0` with literal zero | `CompilationException` |
| Modulo by zero (literal) | `10 % 0` with literal zero | `CompilationException` |
| `ILIKE` operator used | Using `ILIKE` (PostgreSQL syntax) | Error: *"Consider using LIKE instead."* |
| Non-existing property | `Self.NonExistingProperty` | `UnknownPropertyException` |
| Indexer not supported | `Self['key']` on non-indexable type | `ObjectDoesNotImplementIndexerException` |
| Non-array indexed | `Self[0]` on non-array type | `ObjectIsNotAnArrayException` |
| SELECT * with GROUP BY | Star expands to non-aggregated columns | `NonAggregatedColumnInSelectException` |
| Missing alias in multi-table | Column without table qualifier in join | `AliasMissingException` |
| Ambiguous method owner (MQ3035) | Unqualified function call resolves to different implementations across schemas — see [§8.10.1](#8101-method-auto-resolution-algorithm) | `AmbiguousMethodOwnerException` |
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
| Star RENAME duplicate source (MQ3068) | Same source output column listed twice in RENAME | `StarModifierValidationException` |
| Star RENAME duplicate target (MQ3069) | RENAME targets collide with each other or with an existing output column | `StarModifierValidationException` |
| Star RENAME unknown column (MQ3070) | RENAME references a column not present after LIKE/EXCLUDE/REPLACE | `StarModifierValidationException` |
| Invalid subquery shape or scope (MQ2024) | Malformed subquery, scalar subquery with multiple columns, quantified subquery with multiple columns, unsupported quantified/scalar set-operator form, plain derived table with outer references, or correlated APPLY derived table hiding required local correlation columns | Parser diagnostic envelope or `VisitorException` |
| IN subquery returns multiple columns (MQ3049) | `IN (SELECT col1, col2 FROM ...)` — subquery must return exactly one column | `VisitorException` |
| QUALIFY without window function (MQ3050) | `QUALIFY` expression does not reference any window function | `VisitorException` |
| FILTER on non-aggregate (MQ3051) | `FILTER (WHERE ...)` applied to a non-aggregate function | `VisitorException` |
| Invalid PIVOT form (MQ2002/MQ2003/MQ2008) | Missing `ON`, missing static `IN (...)`, missing `USING`, non-constant pivot values, non-aggregate `USING` expression, duplicate generated output name, or multi-key tuple length mismatch | Parser diagnostic envelope |
| Invalid UNPIVOT form (MQ2002/MQ2003/MQ2008/MQ2022/MQ3055) | Missing `ON`, empty or missing `IN (...)`, missing `USING`, entry expression without a stable alias, duplicate output name, duplicate name value, or incompatible value expression types | Parser diagnostic envelope or `ValuesSourceException` |
| RANGE frame without ORDER BY (MQ3052) | `RANGE BETWEEN` used without `ORDER BY` in the window specification | `VisitorException` |
| Invalid VALUES source (MQ3055) | Inline VALUES rows have inconsistent fields, duplicate field names, unsupported row-dependent expressions, invalid parameter/let references, or incompatible inferred field types | `ValuesSourceException` |
| Invalid script parameter declaration (MQ2031) | A `param(...)` declaration is malformed, has the wrong `name: type` order, or uses an invalid default expression | Parser diagnostic envelope |
| Unsupported script parameter syntax (MQ2032) | A script uses PowerShell, Python, SQL-variable, or another unsupported parameter declaration style | Parser diagnostic envelope |
| Invalid script variable declaration (MQ2033) | A `let` declaration is malformed or does not use `let name: type = value` syntax | Parser diagnostic envelope |
| Duplicate script parameter block (MQ3056) | More than one `param(...)` block appears in the script | Bind diagnostic envelope |
| Script parameter block after statement (MQ3057) | `param(...)` appears after a query or utility statement | Bind diagnostic envelope |
| Duplicate script parameter name (MQ3058) | The same parameter name appears more than once in the block | Bind diagnostic envelope |
| Undeclared script parameter reference (MQ3059) | `$name` is referenced but no parameter named `name` is declared | Bind diagnostic envelope |
| Unsupported script parameter type (MQ3060) | A parameter type is not one of the supported scalar expression types or one-dimensional collection parameter types | Bind diagnostic envelope |
| Invalid script parameter default (MQ3061) | A default literal cannot be converted to the declared parameter type, or a collection parameter declares a default | Bind diagnostic envelope |
| Invalid script parameter source argument (MQ3062) | A parameter used in a source argument is required or is not passed directly as `$name` | Bind diagnostic envelope |
| Duplicate script symbol name (MQ3063) | A `let` declaration duplicates another `let` name or a `param(...)` name | Bind diagnostic envelope |
| Unsupported script variable type (MQ3064) | A script variable type is not one of the supported primitive expression types | Bind diagnostic envelope |
| Invalid script variable initializer (MQ3065) | A `let` initializer is not a compile-time constant or cannot be converted to the declared type | Bind diagnostic envelope |
| Script variable used before declaration (MQ3066) | A `let` initializer references a variable declared later or a mistyped variable name | Bind diagnostic envelope |
| Script parameter type mismatch (MQ3005) | A parameter declared type is incompatible with a comparison, boolean context, function argument, or other expression context | Bind diagnostic envelope |

### 23.2 Runtime Errors

| Error | Cause | Behavior |
|-------|-------|----------|
| Invalid soft type conversion | `ToInt32('abc')` | Returns `null` where that helper's established behavior is soft |
| Invalid strict cast | `'abc'::Int32` | Runtime conversion exception |
| Invalid regex in RLIKE | `Name RLIKE '[invalid('` | Exception thrown |
| Non-numeric string comparison | String `"abc"` compared to number | No match, no exception |
| Required script parameter missing (MQ7003) | A required parameter value was not supplied by the host | Runtime diagnostic envelope before source open |
| Script parameter type mismatch (MQ7004) | A supplied value cannot be cast to the declared CLR type | Runtime diagnostic envelope before source open |
| Script parameter null not allowed (MQ7005) | A supplied `null` is used for a non-nullable value-type parameter | Runtime diagnostic envelope before source open |
| Scalar subquery returns multiple rows | A scalar subquery produces more than one row for the current outer row | Runtime exception |

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
root           ::= [parameter_block [';']] script_item { ';' script_item } [';']

script_item    ::= script_variable_decl
                 | statement

parameter_block ::= PARAM '(' [parameter_decl {',' parameter_decl}] ')'

parameter_decl ::= identifier ':' parameter_type ['=' parameter_default]

parameter_type ::= type_name ['[]'] ['?']

parameter_default ::= literal | NULL

script_variable_decl ::= LET identifier ':' type_name ['?'] '=' constant_expression

constant_expression ::= literal
                      | NULL
                      | script_reference
                      | '(' constant_expression ')'
                      | unary_constant_expression
                      | constant_expression binary_constant_op constant_expression
                      | constant_expression comp_op constant_expression
                      | constant_expression IS [NOT] NULL
                      | constant_expression BETWEEN constant_expression AND constant_expression

unary_constant_expression ::= ('-' | NOT) constant_expression

binary_constant_op ::= '+' | '-' | '*' | '/' | '%' | '&' | '|' | '^' | '<<' | '>>' | AND | OR

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

set_operator   ::= UNION [set_operator_key_list]
                 | UNION ALL [set_operator_key_list]
                 | EXCEPT [set_operator_key_list]
                 | INTERSECT [set_operator_key_list]

set_operator_key_list ::= '(' key_list ')'

key_list       ::= [identifier {',' identifier}]

query          ::= regular_query | reordered_query | pivot_query | unpivot_query

regular_query  ::= SELECT [DISTINCT] select_list
                   FROM from_clause
                   {join_or_apply}
                   [WHERE expression]
                   [group_by_clause]
                   [WINDOW window_def {',' window_def}]
                   [QUALIFY expression]
                   [ORDER BY order_list]
                   [SKIP integer]
                   [TAKE integer]

reordered_query ::= FROM from_clause
                    {join_or_apply}
                    [WHERE expression]
                    [group_by_clause]
                    [WINDOW window_def {',' window_def}]
                    [QUALIFY expression]
                    SELECT [DISTINCT] select_list
                    [ORDER BY order_list]
                    [SKIP integer]
                    [TAKE integer]

pivot_query    ::= PIVOT from_clause
                   {join_or_apply}
                   ON pivot_key_list IN '(' pivot_value {',' pivot_value} ')'
                   USING pivot_measure {',' pivot_measure}
                   [GROUP BY (expression_list | ALL)]
                   [ORDER BY order_list]
                   [SKIP integer]
                   [TAKE integer]

group_by_clause ::= GROUP BY (expression_list | ALL) [HAVING expression]

pivot_key_list ::= expression {',' expression}

pivot_value    ::= pivot_scalar_value [[AS] alias_name]
                 | '(' pivot_scalar_value {',' pivot_scalar_value} ')' [[AS] alias_name]

pivot_scalar_value ::= literal

pivot_measure  ::= identifier {'.' identifier} '(' [DISTINCT] [arg_list | '*'] ')' [[AS] alias_name]

unpivot_query  ::= UNPIVOT from_clause
                   {join_or_apply}
                   ON identifier IN '(' unpivot_entry {',' unpivot_entry} ')'
                   USING identifier
                   [KEEP unpivot_keep {',' unpivot_keep}]
                   [ORDER BY order_list]
                   [SKIP integer]
                   [TAKE integer]

simple_reference ::= identifier {'.' identifier}

unpivot_entry  ::= simple_reference [[AS] alias_name]
                 | expression [AS] alias_name

unpivot_keep   ::= simple_reference [[AS] alias_name]
                 | expression [AS] alias_name
```

### 24.4 FROM Clause Grammar

```ebnf
from_clause    ::= schema_source [alias]
                 | values_source
                 | identifier [alias]
                 | derived_table

schema_source  ::= identifier '.' identifier '(' [arg_list] ')'

values_source  ::= VALUES '{' values_row {',' values_row} [','] '}' alias

values_row     ::= '{' values_field {',' values_field} [','] '}'

values_field   ::= identifier ':' values_expression

values_expression ::= literal
                    | script_reference
                    | values_expression arithmetic_op values_expression
                    | '(' values_expression ')'

arithmetic_op  ::= '+' | '-' | '*' | '/' | '%'

alias          ::= identifier | AS identifier

derived_table  ::= '(' (set_operators | cte_expression) ')' alias

join_or_apply  ::= join_clause | apply_clause

join_clause    ::= [INNER] JOIN from_clause ON expression
                 | LEFT [OUTER] JOIN from_clause ON expression
                 | RIGHT [OUTER] JOIN from_clause ON expression
                 | FULL [OUTER] JOIN from_clause ON expression
                 | ASOF JOIN from_clause ON asof_condition [tie_break_clause]
                 | ASOF LEFT [OUTER] JOIN from_clause ON asof_condition [tie_break_clause]

asof_condition ::= asof_expr { AND asof_expr }

asof_expr      ::= expression '=' expression
                 | expression inequality_op expression

inequality_op  ::= '>=' | '>' | '<=' | '<'

tie_break_clause ::= TIE BREAK BY ordered_field

apply_clause   ::= CROSS APPLY apply_source [WITH ORDINALITY]
                 | OUTER APPLY apply_source [WITH ORDINALITY]

apply_source   ::= from_clause
                 | schema_source
                 | identifier '.' method_call
                 | identifier '.' property_path
```

### 24.5 SELECT List Grammar

```ebnf
select_list    ::= select_item {',' select_item}

select_item    ::= star_expr
                 | expression [[AS] alias_name]

star_expr      ::= ('*' | identifier '.' '*') [star_modifiers]

star_modifiers ::= [like_modifier] [exclude_modifier] [replace_modifier] [rename_modifier]

like_modifier  ::= LIKE string_literal
                 | NOT LIKE string_literal

exclude_modifier ::= EXCLUDE '(' identifier {',' identifier} ')'

replace_modifier ::= REPLACE '(' replace_item {',' replace_item} ')'

replace_item   ::= expression AS identifier

rename_modifier ::= RENAME '(' rename_item {',' rename_item} ')'

rename_item   ::= identifier {'.' identifier} AS identifier

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
comparison     ::= coalesce_expr [comp_op coalesce_expr]
                 | coalesce_expr IS [NOT] NULL
                 | coalesce_expr IS [NOT] DISTINCT FROM coalesce_expr
                 | identifier IS PRESENT
                 | identifier IS MISSING
                 | coalesce_expr [NOT] IN '(' expression_list ')'
                 | coalesce_expr [NOT] IN '(' set_operators ')'
                 | coalesce_expr [NOT] IN script_reference
                 | [NOT] EXISTS subquery
                 | coalesce_expr comp_op quantified_subquery
                 | coalesce_expr [NOT] LIKE expression
                 | coalesce_expr [NOT] RLIKE expression
                 | predicate_quantifier [NOT] LIKE expression
                 | predicate_quantifier [NOT] RLIKE expression
                 | coalesce_expr [NOT] BETWEEN coalesce_expr AND coalesce_expr
                 | coalesce_expr CONTAINS '(' expression_list ')'

subquery       ::= '(' set_operators ')'

quantified_subquery ::= (ANY | SOME | ALL) '(' set_operators ')'

predicate_quantifier ::= quantifier_name '(' nonempty_expression_list ')'

quantifier_name ::= 'any' | 'all'

nonempty_expression_list ::= expression {',' expression}

comp_op        ::= '=' | '<>' | '!=' | '>' | '>=' | '<' | '<='

coalesce_expr  ::= add_expr ['??' coalesce_expr]

add_expr       ::= bitwise_expr {('+'|'-') bitwise_expr}
bitwise_expr   ::= shift_expr {('&'|'|'|'^') shift_expr}
shift_expr     ::= mul_expr {('<<'|'>>') mul_expr}
mul_expr       ::= unary_expr {('*'|'/'|'%') unary_expr}
unary_expr     ::= ['-'] cast_expr

cast_expr      ::= primary {'::' cast_type_name}

cast_type_name ::= identifier

primary        ::= literal
                 | script_reference
                 | identifier {'.' identifier} ['(' [arg_list] ')'] [filter_clause] [window_over]
                 | identifier '[' expression ']'
                 | '(' expression ')'
                 | subquery
                 | case_expression

script_reference ::= '$' identifier

filter_clause  ::= FILTER '(' WHERE expression ')'

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

couple_statement ::= COUPLE couple_schema_source WITH couple_option { AND couple_option } AS identifier

couple_schema_source ::= identifier '.' identifier

couple_option ::= TABLE identifier
                | SETTINGS identifier

desc_statement ::= DESC desc_target [column_clause]
                 | DESC FUNCTIONS desc_function_target
                 | DESC SETTINGS desc_settings_target
                 | DESC QUERY '(' (set_operators | cte_expression) ')'

desc_target ::= identifier
              | identifier '.' identifier
              | identifier '.' identifier '(' [arg_list] ')'

desc_function_target ::= identifier
                       | identifier '.' identifier
                       | identifier '.' identifier '(' [arg_list] ')'

desc_settings_target ::= identifier
                       | identifier '.' identifier
                       | identifier '.' identifier '(' [arg_list] ')'

column_clause ::= COLUMN column_path

column_path ::= identifier { '.' identifier }
```

### 24.9 Window Function Grammar

```ebnf
window_over    ::= OVER '(' window_spec ')'
                 | OVER identifier

window_spec    ::= [partition_clause] [order_clause] [frame_clause]

partition_clause ::= PARTITION BY expression {',' expression}

order_clause   ::= ORDER BY ordered_field {',' ordered_field}

ordered_field  ::= expression [ASC | DESC] [NULLS FIRST | NULLS LAST]

frame_clause   ::= frame_type BETWEEN frame_bound AND frame_bound

frame_type     ::= ROWS | RANGE

frame_bound    ::= UNBOUNDED PRECEDING
                 | UNBOUNDED FOLLOWING
                 | CURRENT ROW
                 | integer PRECEDING
                 | integer FOLLOWING

window_clause  ::= WINDOW window_def {',' window_def}

window_def     ::= identifier AS '(' window_spec ')'
```

Window functions are recognized at the expression level — when a function call is followed by the `OVER` keyword, the parser wraps it as a `WindowFunctionNode`.

**Supported function names** (case-insensitive, underscores ignored):

| Ranking | Offset | Value Access | Aggregate |
|---------|--------|--------------|-----------|
| `RowNumber()` / `ROW_NUMBER()` | `Lag(expr [, offset [, default]])` | `FirstValue(expr)` / `FIRST_VALUE(expr)` | `Sum(expr)` |
| `Rank()` / `RANK()` | `Lead(expr [, offset [, default]])` | `LastValue(expr)` / `LAST_VALUE(expr)` | `Count(expr)` |
| `DenseRank()` / `DENSE_RANK()` | | `NthValue(expr, n)` / `NTH_VALUE(expr, n)` | `Avg(expr)` |
| `Ntile(n)` / `NTILE(n)` | | | `Min(expr)` |
| | | | `Max(expr)` |

---

## 25. Appendices

### Appendix A: Complete Keyword List

```
ALL, AND, ANY, AS, ASC, ASOF, ASOF JOIN, ASOF LEFT JOIN, ASOF LEFT OUTER JOIN,
BREAK, CASE, COLUMN, CONTAINS, COUPLE, CROSS APPLY, DESC, DESC QUERY, DISTINCT,
ELSE, END, EXCEPT, EXISTS, FALSE, FILTER, FIRST, FROM, FUNCTIONS, GROUP BY, HAVING, IN,
FULL JOIN, FULL OUTER JOIN, INNER JOIN, INTERSECT, IS, IS DISTINCT FROM,
IS NOT DISTINCT FROM, JOIN, KEEP, LAST, LEFT JOIN, LEFT OUTER JOIN, LIKE, MISSING,
NOT, NOT IN, NOT LIKE, NOT RLIKE, NULL, NULLS FIRST, NULLS LAST, ON, OR,
ORDER BY, ORDINALITY, OVER, PIVOT, OUTER APPLY, PARTITION BY, PRESENT, QUALIFY,
QUERY, RANGE, RENAME, RIGHT JOIN, RIGHT OUTER JOIN, RLIKE, ROWS, SELECT, SETTINGS,
SKIP, SOME, TABLE, TAKE, THEN, TIE, TIE BREAK BY, TRUE, UNION, UNION ALL,
UNPIVOT, USING, WHEN, WHERE, WINDOW, WITH, WITH ORDINALITY
```

### Appendix B: Operator Precedence Table

From **lowest** to **highest** precedence:

| Level | Operators | Category |
|-------|-----------|----------|
| — | `OR` | Logical |
| — | `AND` | Logical |
| — | `NOT` | Logical |
| — | `=`, `<>`, `!=`, `>`, `>=`, `<`, `<=`, `IS NULL`, `IS DISTINCT FROM`, `IS NOT DISTINCT FROM`, `IS PRESENT`, `IS MISSING`, `IN`, `IN $param`, `EXISTS`, `ANY`, `SOME`, `ALL`, `LIKE`, `RLIKE`, `CONTAINS`, `any(...) LIKE`, `all(...) RLIKE` | Comparison |
| 0 | `&`, `\|`, `^`, `??` | Bitwise and null fallback |
| 1 | `<<`, `>>` | Shift |
| 2 | `+`, `-` | Additive |
| 3 | `*`, `/`, `%` | Multiplicative |
| 4 | `.` | Member access |

At level 0, `??` is right-associative; the bitwise operators are left-associative.

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
| Inline row sources | `VALUES (...)` | `FROM values { { Field: literalOrStaticExpression } } alias`; scalar params/lets are allowed |
| Script parameters | Vendor-specific | Optional leading `param(name: type = default)` block; scalar refs use `$name`, collection params use `type[]` and `IN $name` |
| Script variables | Host-language or dialect-specific variables | `let name: type = constantExpression`; references use `$name` |
| Pagination | `OFFSET n LIMIT m` | `SKIP n TAKE m` |
| Not-equal | `<>` and `!=` | Both `<>` and `!=` are supported |
| CASE WHEN ELSE | ELSE optional | ELSE **mandatory** |
| Simple CASE | `CASE expr WHEN value THEN ...` | Supported |
| Set operations | Omitted keys compare all projected values | Omitted keys and `()` compare all projected values; explicit key lists such as `UNION (key_columns)` compare a subset |
| Recursive CTEs | Supported | Not supported |
| Subqueries in FROM | Supported | Supported as independent derived tables; correlated derived tables require `CROSS APPLY` / `OUTER APPLY` |
| `BETWEEN` | `x BETWEEN a AND b` | Supported — `x BETWEEN a AND b` is equivalent to `x >= a AND x <= b` |
| `IS DISTINCT FROM` | Null-safe equality family | Supported — `IS DISTINCT FROM` and `IS NOT DISTINCT FROM` use null-safe semantics |
| Window functions | `ROW_NUMBER() OVER (...)` | Supported with both `RowNumber()` and `ROW_NUMBER()` naming; `OVER` clause with `PARTITION BY`, `ORDER BY`, and frame specs |
| NULL ordering | `ORDER BY x NULLS FIRST/LAST` | Supported in top-level ordering, window ordering, and ASOF tie-break ordering |
| QUALIFY clause | Not standard (Snowflake/BigQuery extension) | Supported — filters on window function results; see §11.13 |
| FILTER on aggregates | Part of SQL:2003 | Supported — `Count(x) FILTER (WHERE condition)`; see §10.10 |
| Subqueries | `IN`, `EXISTS`, scalar, quantified, and derived-table subqueries | Supported with Musoq scoping and set-operator limitations; see §7.9-§7.12 and §6.4 |
| String comparison | Implementation-defined | LIKE is case-insensitive; `=` is ordinal |
| Cross/Outer Apply | T-SQL only | Fully supported with method/property expansion and optional `WITH ORDINALITY` |
| Full outer row classification | Often written with nullable key checks | Use `alias IS PRESENT` / `alias IS MISSING` on aliases that may be absent; there is no `COMPARE` / `DIFF` statement or `ChangeKind` helper in v1 |
| ASOF JOIN | Not standard | Supported — nearest-match join on an ordered column with optional `TIE BREAK BY` |
| Query description | Vendor-specific | `DESC QUERY (<query>)` returns projected output metadata |
| PIVOT | Vendor-specific | Simplified static-pivot statement with mandatory static `IN (...)` values |
| UNPIVOT | Vendor-specific | Musoq-style explicit row expansion with a static `IN (...)` list, explicit `KEEP` fields, and null-preserving rows |
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

### Appendix G: Runtime V2 Cast and Grouping Quick Reference

Runtime v2 removes the old bare double-colon numeric grouping shorthand. The `::` token is postfix cast syntax only:

```sql
select Population::Int32 from A.entities()
select (Population + 1)::Decimal from A.entities()
```

Use `GROUP BY` ordinals or aliases for projection-based grouping references:

```sql
select City, Count(*) from A.entities() group by 1
select City as c, Count(*) from A.entities() group by c
```

Quick rules:
- `expr::TypeName` performs a strict cast to a supported CLR type name.
- Bare double-colon grouping references and bare cast targets are invalid syntax.
- Direct positive integer literals in `GROUP BY` are SELECT-list ordinals.
- `GROUP BY ALL` infers every non-aggregate, non-window SELECT expression after projection expansion.
- Non-aggregate SELECT aliases are visible in `WHERE` and `GROUP BY`; aggregate SELECT aliases are visible in `HAVING`.
