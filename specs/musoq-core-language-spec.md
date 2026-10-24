# Musoq Core SQL Language Specification

**Version:** 2.0.0
**Status:** Language specification
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
23. [Diagnostic Contract and Catalog](#23-diagnostic-contract-and-catalog)
24. [Formal Grammar](#24-formal-grammar)
25. [Appendices](#25-appendices)

---

## 1. Introduction

### 1.1 Purpose

This document specifies the Musoq SQL dialect — a query language for querying diverse data sources (files, APIs, git repositories, operating system resources, and more) using SQL syntax. The specification is intended for query authors and implementers in any programming language. It defines observable behavior independently of execution architecture.

### 1.2 Scope

This specification covers:

- Complete SQL syntax supported by the query engine
- All language operators and expressions, and the required function surface (§18)
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
| Inline row sources | `VALUES (...)` table constructors in selected contexts | `FROM values { (Field: literal) } alias` creates a strongly typed inline source |
| Script parameters | Vendor-specific variables, prepared statement parameters, or host API bindings | Optional leading `param(...)` or `params(...)` block; references use `$name` |
| Script variables | Host-language variables or SQL dialect variables | `let name: type = constantExpression` declarations; references use `$name` |
| Pagination | `OFFSET` / `LIMIT` | `SKIP` / `TAKE` |
| Set operation keys | Implicit (all columns) | Omitted keys and `()` compare all projected values; explicit key lists such as `UNION (col1)` compare a subset |
| Not-equal operator | Both `<>` and `!=` | Both `<>` and `!=` are supported |
| CASE WHEN | ELSE is optional | ELSE is optional; omitted ELSE returns NULL |
| Simple CASE form | `CASE expr WHEN value THEN ...` | Supported (desugared to searched CASE internally) |
| FROM-first syntax | Not standard | Supported: `FROM ... WHERE ... SELECT ...` |
| CROSS APPLY / OUTER APPLY | T-SQL extension | Fully supported with method and property expansion |
| CROSS JOIN | Standard SQL | Supported as an uncorrelated Cartesian product; filtering belongs in `WHERE` |
| FULL OUTER JOIN | Standard SQL | Supported; use `alias IS PRESENT` / `alias IS MISSING` to classify unmatched sides |
| SEMI JOIN / ANTI JOIN | Common relational algebra extension | Supported as left-sided existence and non-existence joins |
| Recursive CTEs | Supported in many dialects | Supported with `WITH RECURSIVE`; recursive members have documented operator restrictions (see §14.9) |
| Subqueries in FROM | Supported | Supported as derived tables; plain derived tables are independent and correlation requires `CROSS APPLY` / `OUTER APPLY` |
| `BETWEEN` operator | Supported | Supported — `x BETWEEN a AND b` is equivalent to `x >= a AND x <= b` |
| `ORDER BY` position | `ORDER BY 1` | **Not supported** — use column names or expressions |
| ASOF JOIN | Not standard | Supported — nearest-match join on an ordered column |
| PIVOT | Vendor-specific | Supported as a simplified static-pivot statement with mandatory static `IN (...)` values; see §10.11 |
| UNPIVOT | Vendor-specific | Supported as a Musoq-style static row expansion statement with explicit keep fields; see §10.12 |
| Window functions | `OVER (PARTITION BY ... ORDER BY ...)` with frame specs | Supports `PARTITION BY`, `ORDER BY`, named `WINDOW` clause, `ROWS BETWEEN` and `RANGE BETWEEN` frame specifications, ranking (`ROW_NUMBER`, `RANK`, `DENSE_RANK`, `PERCENT_RANK`, `CUME_DIST`, `NTILE`), offset (`LAG`, `LEAD`), aggregate (`SUM`, `COUNT`, `AVG`, `MIN`, `MAX`), and value access (`FIRST_VALUE`, `LAST_VALUE`, `NTH_VALUE`) window functions. Data-source plugins can register custom window functions. `QUALIFY` clause is supported for filtering rows after window function evaluation. `RANGE BETWEEN` requires an `ORDER BY` clause in the window specification. |
| FILTER on aggregates | Not standard (part of SQL:2003) | Supported — `Count(x) FILTER (WHERE condition)` restricts aggregate input; see §10.10 |
| Subqueries | `IN`, `EXISTS`, scalar, quantified, and derived-table subqueries | Supported; correlated predicate and scalar forms are decorrelated where possible; see §7.9-§7.12 |
| Result modifiers with set operations | Trailing `ORDER BY` / `SKIP` / `TAKE` apply to the combined result | Supported with standard result-level semantics; wrap an operand in a derived table or CTE for branch-local modifiers — see §12.8 and §12.10 |

### 1.4 Terminology

| Term | Definition |
|------|------------|
| **Schema** | A named data source provider (e.g., `git`, `os`, `csv`) that exposes one or more methods |
| **Method** | A specific data-producing function on a schema (e.g., `schema.records()`, `schema.method('key')`) |
| **Entity** | A single row returned by a data source method |
| **Column** | A named, typed field within an entity |
| **Expression** | Any computation that produces a value (arithmetic, function call, column reference, etc.) |
| **CTE** | Common Table Expression — a named temporary result set defined with `WITH ... AS (...)` |
| **Apply** | A correlated join where the right side can reference columns from the left side |
| **Script parameter** | A typed value declared once at the start of a script and supplied by the host before execution |
| **Parameter block** | One leading block introduced by `param(...)` or `params(...)`; `param(...)` is canonical |
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

### 1.8 Authority and semantic stages

Core owns lexical rules, logical values and types, expression semantics, and the
diagnostic envelope. Satellite specifications own only their extension syntax and
behavior; duplicated summaries defer to the owning section. Grammar and prose
describe the same contract. A contradiction is a specification defect, not license
for implementation-specific behavior.

Parse recognizes syntax. Bind resolves names, logical types, callable signatures,
and constants without opening row sources. Execute evaluates demanded expressions
and reads sources. "Compile-time" elsewhere means before execution (parse or bind),
not a requirement to generate executable code. Implementations MAY interpret,
compile, or otherwise evaluate a query, provided observable behavior is preserved.

Provider contracts describe available sources, fields, callable signatures,
function identities, optional settings, and declared capabilities. Providers are
not required to expose native objects or any particular host interface.
Core conformance does not require installing the providers used in examples.
Ordinary examples use inline VALUES rows and require no installed datasource.
Examples about source binding, metadata, settings, or supplied member shapes use
the generic contracts in [Appendix H](#appendix-h-generic-source-contracts-used-in-examples). SQL syntax fragments and intentional failures
are identified by their surrounding text. The specifications
define the required behavior independently of any implementation test corpus.

Host-specific type lookup, metadata rendering, integration APIs, execution
strategies, and migration advice are outside this contract.
Conformance counts MUST distinguish passed mandatory cases from known deviations.

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
| `PARAM` / `PARAMS` | Contextual script parameter declaration block when used at the start of a script; neither word is globally reserved |
| `LET` | Contextual script variable declaration keyword when used at statement start |
| `PIVOT` | Begin a pivot query |
| `UNPIVOT` | Begin an unpivot query |
| `KEEP` | UNPIVOT keep field list (context-sensitive) |
| `TABLE` | Define a table structure |
| `COUPLE` | Bind a schema method to a table and/or source runtime settings profile |
| `ENUM` | Contextual enum declaration keyword when used at statement start |
| `FLAGS` | Contextual modifier only in `FLAGS ENUM` at statement start |
| `CASE` | Begin conditional expression |
| `WHEN` | Conditional branch |
| `THEN` | Branch result |
| `ELSE` | Optional default branch in CASE; omitted ELSE means NULL |
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
| ARRAY | Contextual collection literal keyword before {; ordinary identifier elsewhere |
| VALUES | Contextual inline source keyword before {; ordinary identifier elsewhere |

`ANY`, `SOME`, and `ALL` are contextual SQL keywords. They act as quantified subquery operators only in comparison predicates such as `x > ANY (SELECT ...)` or `x = SOME (FROM ...)`. Lowercase `any(...)` and `all(...)` also remain contextual predicate-quantifier names when they appear as unqualified calls immediately before a supported pattern operator, for example `any(Name, Message) LIKE '%error%'`.

`EXISTS` is also contextual in expression positions. A bare `EXISTS` is an ordinary, case-sensitive column identifier unless it is followed by a parenthesized supported subquery; `EXISTS (SELECT ...)` and `exists(SELECT ...)` are equivalent existence predicates. A parenthesized non-subquery expression after `EXISTS` is invalid. `NOT EXISTS` follows the same rule.

`USING` is a contextual keyword inside `PIVOT` and `UNPIVOT` statements. `KEEP` is a contextual keyword inside an `UNPIVOT` statement. Outside those clause positions they remain ordinary identifiers.

`SETTINGS` is a contextual keyword inside `COUPLE ... WITH SETTINGS ...` and `DESC SETTINGS`. `QUERY` is a contextual keyword inside `DESC QUERY`. `COLUMN` is a contextual keyword inside `DESC ... COLUMN ...`. Outside those clause positions they remain ordinary identifiers. `ARGUMENTS` is contextual inside `DESC ARGUMENTS`; outside that clause it remains an ordinary identifier.

`ENUM` and `FLAGS` are contextual only at statement boundaries. `ENUM name :
type { ... }` and `FLAGS ENUM name : type { ... }` begin enum declarations;
outside those forms both words remain ordinary identifiers.

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

Contextual keywords used as identifiers preserve their source casing. For example, `Exists`, `ANY`, and `Some` can be selected as ordinary columns when they are not occupying their predicate-operator positions:

```sql
select FullPath, Exists from values { (FullPath: 'notes.txt', [Exists]: true) } files take 5
select Any, Some, All from values { ([Any]: 1, [Some]: 2, [All]: 3) } words
```

**Bracket-quoted identifiers** allow reserved words and special characters:

```sql
select [case], [order], [Column With Spaces] from values { ([case]: 'item', [order]: 1, [Column With Spaces]: 'value') } names
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

Musoq supports ordinary and raw string literals. Both forms use a single quote
as the delimiter and are accepted anywhere a string literal is accepted.

#### Ordinary String Literals

Ordinary string literals decode the escape sequences listed below:

```sql
select 'Hello, World!' from values { (Seed: 0) } seed
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
- `\uXXXX` requires exactly 4 hex digits. If fewer are available, the malformed
  sequence is reported as `MQ1004` and remains literal.
- `\xXX` requires exactly 2 hex digits. If fewer are available, the malformed
  sequence is reported as `MQ1004` and remains literal.
- Unknown escape sequences are preserved literally: `'\z'` → `\z`
- Double quotes can appear unescaped inside single-quoted strings: `select '"' from ...` is valid.

#### Raw String Literals

Raw string literals use an adjacent lowercase or uppercase `r` prefix:

```sql
select r'C:\Some\Path\To\Directory' from values { (Seed: 0) } seed
select r'\\server\share' from values { (Seed: 0) } seed
select r'C:\Temp\' from values { (Seed: 0) } seed
select r'a''b' from values { (Seed: 0) } seed
```

Raw literal rules:

- The prefix and opening quote must be adjacent: `r'...'` or `R'...'`.
- Backslashes and all other content are preserved exactly; raw literals do not
  decode ordinary escape sequences.
- A pair of single quotes `''` represents one embedded single quote.
- Backslashes do not escape the closing quote. For example,
  `r'path\'` contains one trailing backslash and then closes.
- Raw literals are generic and may be used in expressions, function arguments,
  schema-method arguments, script values, and every other string-literal
  position.
- A separated prefix such as `r 'value'` is not raw syntax; `r` remains an
  ordinary identifier.

The distinction between the two forms is observable:

```sql
select '\n' from values { (Seed: 0) } seed   -- one newline character
select r'\n' from values { (Seed: 0) } seed  -- two literal characters: backslash and n
```

Use raw literals for Windows paths, or double each backslash in an ordinary
literal. Unknown ordinary escapes remain literal, while malformed fixed-length
`\u` and `\x` escapes produce `MQ1004`.

#### Suspicious Ordinary-String Path Escapes

Ordinary strings retain their existing escape semantics. When a recognized
escape changes the value of text that looks like a path, Musoq reports the
advisory warning `MQ5014_SuspiciousOrdinaryStringEscape`. The warning does not
rewrite the literal, change its runtime value, or prevent compilation.

```sql
select 'C:\new\test' as FullPath from values { (Seed: 0) } seed
-- MQ5014: \n and \t alter this path-like ordinary string

select r'C:\new\test' as FullPath from values { (Seed: 0) } seed
select 'C:\\new\\test' as FullPath from values { (Seed: 0) } seed
```

Rooted path risks such as drive-rooted, UNC, device, and explicit relative-root
forms can be identified during parsing. A relative value such as
`'some\text'` is warned about only when semantic binding identifies a
path-sensitive string destination, for example `schema.files('some\text')`;
a bare projection such as
`select 'some\text' from values { (Seed: 0) } seed` remains quiet because its intent cannot
be inferred. The detector reports at most one warning per literal, at the first
recognized value-changing escape.

Intentional standalone escapes such as `select '\n' from values { (Seed: 0) } seed` and
unknown escapes such as `\q` remain warning-free. Malformed fixed-length
`\u` and `\x` sequences remain `MQ1004` errors. Consumers that need advisory
diagnostics must use a diagnostic-aware host operation. An execution-only operation need not expose warnings.

`ValidateSyntax()` reports only parse-level warnings, such as rooted
`MQ5014`. Relative path warnings and all other binding-dependent advisories
require `QueryAnalyzer.Analyze()` or diagnostic-aware compilation, where
schema and method metadata are available.

**Examples:**

```sql
select '\\' from values { (Seed: 0) } seed                    -- result: \
select '\'' from values { (Seed: 0) } seed                     -- result: '
select '\n' from values { (Seed: 0) } seed                     -- result: (newline)
select '\u0041' from values { (Seed: 0) } seed                 -- result: A
select '\x41' from values { (Seed: 0) } seed                   -- result: A
select 'Hello\nWorld\t\u0394\\test' from values { (Seed: 0) } seed  -- result: Hello(LF)World(TAB)Δ\test
select '\0\b\f\e' from values { (Seed: 0) } seed              -- result: (null)(backspace)(formfeed)(ESC)
```

Special characters are valid inside string literals — all punctuation, braces, brackets, etc.:

```sql
select '{', '}', '[', ']', '(', ')' from values { (Seed: 0) } seed
```

### Advisory warning contract

Advisory warnings are default-on diagnostics for diagnostic-aware APIs. They
never change parsed values, execution behavior, query identity, datasource
arguments, runtime behavior, or query results. A warning-only parse or build
still succeeds; `Errors` and `ToEnvelopes()` contain no entries, and the
compiled query remains runnable.

The following warnings are active in the core language:

| Code | Phase | Trigger | Quiet cases and limits | Correction |
|------|-------|---------|------------------------|------------|
| `MQ5003_ImplicitTypeConversion` | Bind | Ambiguous numeric date text is implicitly converted from `string` to `DateTime` or `DateTimeOffset`, such as `01/02/2026`. | ISO year-first text, month names, a component greater than 12, `TimeSpan`, numeric widening, and explicit format conversions remain quiet. | Use ISO year-first text or `ToDateTimeWithFormat` / `ToDateTimeOffsetWithFormat`. |
| `MQ5008_UnreachableCode` | Bind | A searched/simple `CASE` branch or coalesce fallback is proven unreachable. | Nullable, dynamic, literal-NULL, and uncertain expressions are not guessed; one warning is emitted per unreachable region. | Remove the branch or correct the condition. |
| `MQ5010_TautologicalCondition` | Bind | A predicate is proven always true, including safe complement proofs. | Only deterministic direct columns and compatible constants are proven; floating-point, method-call, dynamic, and null-sensitive identities are excluded. | Remove the redundant condition. |
| `MQ5011_ContradictoryCondition` | Bind | A predicate is proven impossible, such as `x = 1 AND x = 2` or non-overlapping safe ranges. | The same conservative proof limits as `MQ5010` apply. | Remove or correct the contradictory predicate. |
| `MQ5013_SourceContractWarning` | Bind | A datasource reports a non-fatal table-contract or read-modifier issue. | It is emitted only when the source supplies the warning. | Follow the source-provided correction. |
| `MQ5014_SuspiciousOrdinaryStringEscape` | Parse or Bind | A recognized ordinary-string escape changes high-confidence rooted path text during parsing, or relative path-like text bound to a path-sensitive string destination. | Raw literals, doubled backslashes, unknown escapes, deliberate standalone escapes, and bare relative projections remain quiet. One warning is emitted per literal at its first hazard. | Use `r'...'` or double each intended backslash. |
| `MQ5015_SuspiciousRegexEscape` | Bind | Ordinary source `\b` becomes a backspace before regex evaluation in `RLIKE` or a bound regex-pattern parameter. | Raw `r'\b'`, ordinary `'\\b'`, `[\b]`, and preserved `\d`, `\s`, and `\w` remain quiet. | Use a raw regex literal or double the backslash. |
| `MQ5016_GlobWildcardInLike` | Bind | A constant `LIKE` pattern uses glob `*`, or path-like `?`, without SQL `%` or `_`. | Dynamic patterns, ordinary prose, SQL wildcards, and patterns with whitespace remain quiet. | Use `%` / `_` for `LIKE`, or `RLIKE` for regex patterns. |
| `MQ5017_NullComparison` | Bind | `=`, `<>`, `!=`, `<`, `<=`, `>`, or `>=` compares an operand with literal or proven-NULL value in a predicate context. | Projection/order expressions, `IS NULL`, `IS NOT NULL`, and `IS [NOT] DISTINCT FROM` remain quiet. | Use an explicit NULL predicate or null-safe distinctness operator. |
| `MQ5018_AmbiguousOuterJoinNullCheck` | Bind | `optional_alias.column IS NULL` can mean either a missing outer-join row or a present row whose original nullable column is NULL. | Original non-nullable columns and unknown derived/CTE nullability remain quiet; explicit `alias IS PRESENT` / `IS MISSING` suppresses the warning. | Test row presence or choose a non-nullable presence column. |
| `MQ5019_NullRejectingOuterJoinFilter` | Bind | A `WHERE` predicate is provably false or UNKNOWN for the NULL-extended row of an outer join/apply. | `ON` restrictions, inner joins, preserving `OR` branches, and uncertain method semantics remain quiet. | Move an intended match restriction into `ON`, or state row-presence intent explicitly. |
| `MQ5020_SetOperationOrderByScope` | Bind | Compatibility identifier for the completed set-ordering migration. | Never emitted by the current language version. | No action is required; trailing ordering is result-level. |
| `MQ5021_UnorderedSkip` | Bind | A positive `SKIP` has no `ORDER BY` in the same query block. | `SKIP 0`, `TAKE` without `SKIP`, and ordered slicing remain quiet. | Add a deterministic `ORDER BY` or remove `SKIP`. |
| `MQ5022_UnusedCte` | Bind | A user-authored CTE is not transitively reachable from its outer query. | Live transitive dependencies, nested live scopes, and synthesized/internal CTEs are not reported. | Remove the CTE or reference it from a live query. |
| `MQ5023_UnusedScriptVariable` | Bind | A `let` declaration is not used by an executable statement or live transitive initializer chain. | Script parameters are not `let` declarations; variables used only by dead declarations remain unused. | Remove the declaration or reference it from live work. |
| `MQ5024_NullSensitiveNotIn` | Bind | A `NOT IN` list or proven collection contains `NULL`, so non-matching values can become `UNKNOWN`. | Positive `IN`, null-free collections, and values proven to exclude `NULL` remain quiet. | Filter `NULL` values or use a null-aware predicate. |
| `MQ5025_ImpossibleImplicitConversion` | Bind | An engine-selected implicit conversion of a constant is proven unable to succeed, so the comparison cannot match. | Dynamic values, explicit soft conversions, and uncertain plugin conversions remain quiet. | Use a compatible literal or an explicit conversion. |
| `MQ5026_SetOperationSliceScope` | Bind | Compatibility identifier for the completed set-slicing migration. | Never emitted by the current language version. | No action is required; trailing slicing is result-level. |

Specific advisories take precedence over generic tautology, contradiction,
and unreachable-branch messages for the same source region. The v17 identifiers
`MQ5001`, `MQ5002`, `MQ5004`, `MQ5005`, `MQ5006`, `MQ5007`, `MQ5009`, and
`MQ5012` are not active v18 enum members and are never emitted. `MQ5009` is
retained as a compatibility documentation identifier so consumers can explain
older reports; current tested `ORDER BY` alias behavior does not produce it.

Diagnostic-aware operations expose warnings separately from errors. The host operation used to execute a query does not determine the query's semantics.

Common examples:

```sql
param(eventTime: datetime = '2024-01-02T03:04:05')
select Name from values {
    (Id: 1, Name: 'Ada', Time: $eventTime),
    (Id: 2, Name: 'Bob', Time: $eventTime),
    (Id: 3, Name: 'Cara', Time: $eventTime),
    (Id: 4, Name: 'Dan', Time: $eventTime)
} rows where Name rlike '\bword\b'
-- MQ5015: the ordinary \b escape becomes backspace before regex matching

select Name from values {
    (Id: 1, Name: 'Ada', Time: $eventTime),
    (Id: 2, Name: 'Bob', Time: $eventTime),
    (Id: 3, Name: 'Cara', Time: $eventTime),
    (Id: 4, Name: 'Dan', Time: $eventTime)
} rows where Name like '*.log'
-- MQ5016: SQL LIKE uses '%' and '_', not filesystem glob '*'

select Name from values {
    (Id: 1, Name: 'Ada', Time: $eventTime),
    (Id: 2, Name: 'Bob', Time: $eventTime),
    (Id: 3, Name: 'Cara', Time: $eventTime),
    (Id: 4, Name: 'Dan', Time: $eventTime)
} rows where Name = NULL
-- MQ5017: NULL comparison is UNKNOWN; use IS NULL

select a.Name, b.Name
from values {
    (Id: 1, Name: 'Ada', Time: $eventTime),
    (Id: 2, Name: 'Bob', Time: $eventTime),
    (Id: 3, Name: 'Cara', Time: $eventTime),
    (Id: 4, Name: 'Dan', Time: $eventTime)
} a
left join values {
    (Id: 1, Name: 'Eve', Time: $eventTime),
    (Id: 2, Name: 'Finn', Time: $eventTime),
    (Id: 3, Name: 'Gia', Time: $eventTime),
    (Id: 4, Name: 'Hugo', Time: $eventTime)
} b on a.Id = b.Id
where b.Name = 'match'
-- MQ5019: the WHERE predicate removes NULL-extended unmatched rows

select Name from values {
    (Id: 1, Name: 'Ada', Time: $eventTime),
    (Id: 2, Name: 'Bob', Time: $eventTime),
    (Id: 3, Name: 'Cara', Time: $eventTime),
    (Id: 4, Name: 'Dan', Time: $eventTime)
} rows skip 10
-- MQ5021: SKIP needs ORDER BY for a deterministic result

select Name from values {
    (Id: 1, Name: 'Ada', Time: $eventTime),
    (Id: 2, Name: 'Bob', Time: $eventTime),
    (Id: 3, Name: 'Cara', Time: $eventTime),
    (Id: 4, Name: 'Dan', Time: $eventTime)
} rows where Name not in ('Alice', NULL)
-- MQ5024: NULL makes NOT IN non-matching results UNKNOWN

select 1 from values {
    (Id: 1, Name: 'Ada', Time: $eventTime),
    (Id: 2, Name: 'Bob', Time: $eventTime),
    (Id: 3, Name: 'Cara', Time: $eventTime),
    (Id: 4, Name: 'Dan', Time: $eventTime)
} rows where Time = '02/31/2026'
-- MQ5025: the constant cannot be implicitly converted to the temporal type
```

### 2.6 Numeric Literals

#### Integer Literals

Bare integers default to `int` (32-bit signed integer):

```sql
select 42 from values { (Seed: 0) } seed        -- type: int
select -42 from values { (Seed: 0) } seed       -- type: int (negative)
```

#### Decimal Literals

Numbers with a decimal point are `decimal`:

```sql
select 3.14 from values { (Seed: 0) } seed      -- type: decimal
select -1.5 from values { (Seed: 0) } seed      -- type: decimal
select .5 from values { (Seed: 0) } seed        -- type: decimal (leading dot)
```

#### Numeric Type Suffixes

Append a suffix to force a specific numeric type:

| Suffix | Type | Canonical keyword | Range |
|--------|------|-----------|-------|
| `b` | signed byte | `sbyte` | -128 to 127 |
| `ub` | unsigned byte | `byte` | 0 to 255 |
| `s` | short | `short` | -32,768 to 32,767 |
| `us` | unsigned short | `ushort` | 0 to 65,535 |
| `i` | int | `int` | -2,147,483,648 to 2,147,483,647 |
| `ui` | unsigned int | `uint` | 0 to 4,294,967,295 |
| `l` | long | `long` | -9,223,372,036,854,775,808 to 9,223,372,036,854,775,807 |
| `ul` | unsigned long | `ulong` | 0 to 18,446,744,073,709,551,615 |
| `d` or `D` | decimal | `decimal` | ±79228162514264337593543950335, scale 0–28 (§3.9) |

**Examples:**

```sql
select 1b from values { (Seed: 0) } seed        -- sbyte
select 255ub from values { (Seed: 0) } seed     -- byte
select 1000s from values { (Seed: 0) } seed     -- short
select 65535us from values { (Seed: 0) } seed   -- ushort
select 42i from values { (Seed: 0) } seed       -- int (explicit)
select 100ui from values { (Seed: 0) } seed     -- uint
select 1l from values { (Seed: 0) } seed        -- long
select 1ul from values { (Seed: 0) } seed       -- ulong
select 42d from values { (Seed: 0) } seed       -- decimal (integer forced to decimal)
select 1.0 from values { (Seed: 0) } seed       -- decimal (implicit from decimal point)
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
select 0xFF from values { (Seed: 0) } seed                    -- 255 (long)
select 0b1010 from values { (Seed: 0) } seed                  -- 10 (long)
select 0o77 from values { (Seed: 0) } seed                    -- 63 (long)
select 0xFF + 0b1010 + 0o77 + 42 from values { (Seed: 0) } seed  -- 370 (mixed arithmetic)
select 0x0 from values { (Seed: 0) } seed                     -- 0 (zero values valid)
```

### 2.7 Boolean Literals

```sql
select true from values { (Seed: 0) } seed
select false from values { (Seed: 0) } seed
```

### 2.8 NULL Literal

```sql
select null from values { (Seed: 0) } seed
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
| 2 | `+`, `\|\|` | Addition / String concatenation (left-associative) | `1 + 2` → 3, `'a' + 'b'` → `'ab'` |
| 2 | `-` | Subtraction | `5 - 3` → 2 |
| 1 | `<<` | Left bit shift | `1 << 3` → 8 |
| 1 | `>>` | Right bit shift | `8 >> 2` → 2 |
| 0 | `&` | Bitwise AND | `0xFF & 0x0F` → 15 |
| 0 | `\|` | Bitwise OR | `0x0F \| 0xF0` → 255 |
| 0 | `^` | Bitwise XOR | `0xFF ^ 0x0F` → 240 |

All arithmetic operators are **left-associative**. Use parentheses to override precedence:

```sql
select 256 + 256 / 2 from values { (Seed: 0) } seed       -- 384 (division first)
select (256 + 256) / 2 from values { (Seed: 0) } seed      -- 256 (parentheses override)
select 2 * 3 / 2 from values { (Seed: 0) } seed            -- 3
select 1 + 2 * 3 * (7 * 8) - (45 - 10) from values { (Seed: 0) } seed  -- 302
```

**Unary minus** is supported:

```sql
select 1 - -1 from values { (Seed: 0) } seed               -- 2
select 1 - -(1 + 2) from values { (Seed: 0) } seed         -- 4
select 1 + (-2) from values { (Seed: 0) } seed             -- -1
```

Arithmetic with a nullable numeric operand has a nullable result and preserves
NULL. Callable binding uses that nullable type; it does not implicitly unwrap a
nullable value for a non-nullable parameter. The byte-array slice overload of
`ToBase64(value, offset, length)` accepts nullable integer offsets and lengths,
returning NULL when any argument is NULL. The existing non-nullable integer
overload retains its range validation.

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

The operands MUST have compatible types. A nullable scalar type MAY fall back to its underlying non-nullable scalar type, for example `NullableValue ?? 0`. String, dynamic and record values admit null independently of host annotations. If the left operand is statically a non-nullable scalar type, the fallback operand is ignored and is not evaluated.

`??` is right-associative: `A ?? B ?? C` is interpreted as `A ?? (B ?? C)`. It is a null fallback operator only. Musoq does not support `?.`, and `??` does not change existing member access or indexer semantics.

#### String Concatenation

The SQL `||` operator accepts strings and contextual `NULL` only. Its result is `NULL` when either operand is `NULL`. Convert numeric, character, or other values explicitly before concatenation; implicit formatting is an `MQ3007` type error. `||` has the same precedence as `+` and `-` and associates left to right. Every operand is evaluated once, in order, including operands after a NULL. Homogeneous chains preserve these semantics without allocating intermediate concatenated strings or temporary argument arrays.

```sql
select 'Hello' || ' ' || 'World' from values { (Seed: 0) } seed -- 'Hello World'
select 'Hello' || null from values { (Seed: 0) } seed          -- NULL
select ToString(42) || ' items' from values { (Seed: 0) } seed -- '42 items'
```

A single `|` remains bitwise OR. Legacy string `+` and the `Concat` function retain their existing behavior. Use `||` when NULL propagation is required. The `+` operator concatenates strings when both operands are strings:

```sql
select 'Hello' + ' ' + 'World' from values { (Seed: 0) } seed -- 'Hello World'
select Concat(Name, ' - ', City) from values { (Name: 'Ada', City: 'WARSAW', Country: 'Poland', Population: 100d), (Name: 'Bob', City: 'BERLIN', Country: 'Germany', Population: 200d) } people
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
| `decimal` | Decimal coefficient and scale (§3.9) | `0.0` | `3.14`, `42d` |
| `char` | One UTF-16 code unit | `'\0'` | — |
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
from schema.records() a
outer apply schema.byCountry(a.Country) b
```

### 3.4 Complex Object Types

Data sources may expose columns containing complex objects with nested properties. Access nested values with dot notation:

```sql
select Self.Name from schema.records()              -- one level deep
select Self.Self.Name from schema.records()          -- two levels deep
select Self.Dictionary['key'] from schema.records()  -- dictionary access
select Self.Array[2] from schema.records()           -- array indexing
```

### 3.5 Collections and Arrays

Columns may hold ordered collections with an element type (`T[]`). These can be:
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

Musoq supports strict postfix casts:

```sql
select Population::Int32 from values { (Population: 100d) } people
select (Population + 1)::Decimal from values { (Population: 100) } people
select Name::String::Guid from values { (Name: '2ffcf6fa-3369-4300-946a-bb131a037985') } identifiers
```

The cast operator has high postfix precedence. `a + b::Int32` is parsed as `a + (b::Int32)`, while `(a + b)::Int32` casts the parenthesized expression.

Supported cast targets include the following Musoq aliases, matched case-insensitively:

`Boolean`, `Byte`, `SByte`, `Int16`, `UInt16`, `Int32`, `UInt32`, `Int64`, `UInt64`, `Single`, `Double`, `Decimal`, `Char`, `String`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`.

Primitive keywords are also supported and map to the corresponding logical target:

`bool` → `Boolean`, `byte` → `Byte`, `sbyte` → `SByte`, `short` → `Int16`, `ushort` → `UInt16`, `int` → `Int32`, `uint` → `UInt32`, `long` → `Int64`, `ulong` → `UInt64`, `float` → `Single`, `double` → `Double`, `decimal` → `Decimal`, `char` → `Char`, `string` → `String`.

SQL type aliases such as `INTEGER`, `VARCHAR`, and `DOUBLE PRECISION` are not supported. `object`, `nint`, `nuint`, nullable suffixes, and other type names are not supported. A cast target MUST be an identifier-like type name after `::`; a bare cast operator, a missing target, and a numeric target are syntax errors.

Strict cast behavior:
- `null` input returns `null`.
- Invalid text, overflow, and unsupported conversions fail; constants fail during binding and demanded nonconstant values fail during execution.
- Conversion and formatting follow §3.9; the host's default locale MUST NOT select the meaning.
- `::String` and `::string` preserve null and format non-null logical values according to §3.9.

The public `ToXxx(...)` helper functions remain available as library functions and keep their established behavior. They are not equivalent to strict postfix casts when a helper has softer conversion semantics, such as returning `null` for invalid text.

### 3.8 Enum Types

Enums are nominal compile-time types backed by primitive integral execution
carriers. Query-local declarations use statement syntax:

```sql
enum JobStatus : int {
    Queued = 10,
    Running = 20,
    Finished = 30
};

flags enum FileAccess : uint {
    None = 0ui,
    Read = 1ui,
    Write = 2ui,
    ReadWrite = 3ui
};
```

The backing type MUST be `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`,
`long`, or `ulong`. Every member MUST have an explicit, representable integral
literal. Empty declarations, auto-numbering, and forward references are not
supported; a trailing comma is accepted. Duplicate values are aliases and the
first declared name is canonical. Member names use exact case-sensitive
matching and case-only duplicates are invalid. Query-local enum type names are
case-insensitive.

Provider-defined enums are discovered through source columns, reachable record
fields, or callable results. Host-native enum type names MUST NOT appear in TABLE
declarations (§15.1). Query-local enums have their own logical identity. Enum
values carry their declared integral value and nominal identity after source reads.

Quoted literals bind contextually to members:

```sql
where Status = 'Running'
where Status in ('Queued', 'Running')
```

Bare names, `Type.Member`, implicit numeric or string conversion, enum casts,
and cross-enum operations are invalid. Unknown representable numeric values
are preserved. The complete operator, output, source-planning, and performance
contract follows below. Equality and IN require the same enum identity; grouping,
DISTINCT and sets preserve it. Direct enum ordering and relational comparisons
are rejected; order by EnumValue when integral ordering is intended. Projection,
CASE, null fallback, offset/value-access windows and Count preserve the logical
identity where their result remains an enum. Numeric aggregates do not accept enums.
EnumName returns the first declared name for an exact value, or null for an unknown
value; it does not synthesize names for flag combinations. EnumValue returns the
backing scalar. IsDefined tests exact declared values. HasAnyFlags and HasAllFlags
apply only to flags enums: combine the requested declared member masks with OR,
then test whether any or all requested bits are set. With no requested members the
results are false and true respectively for a non-null value. Null inputs return
null for EnumName/EnumValue and false for the three predicates. All other operators
and cross-identity conversions are rejected during binding.

---

### 3.9 Logical value domains and conversion rules

These rules define language values, not storage representations. Canonical type
names in metadata are lowercase keywords. Aliases resolve before type comparison.
`T?` means the domain of T plus NULL; it does not change non-null values. String,
dynamic (`object`) and source record values admit NULL with or without `?`.
Collection and structural parameter nullability is explicit at each node (§4.3).

#### 3.9.1 Numeric domains

Signed integers use two's-complement value domains of their declared width;
unsigned integers range from zero to 2^width - 1. The widths are in §3.1.
Floating-point types are IEEE 754 binary32 and binary64, including signed zero,
infinities and NaN. Arithmetic rounds to nearest, ties to even. Comparisons with
NaN are false except `<>`, which is true; positive and negative zero compare
equal. Grouping and deduplication treat NaNs as equal, and numeric sorting places
NaN below other non-null numeric values.

Decimal is sign × coefficient × 10^-scale, where coefficient is an integer from
0 through 79228162514264337593543950335 and scale is 0 through 28. Arithmetic
retains exact results when representable; otherwise scale is reduced with
round-to-nearest, ties-to-even. A result outside the domain fails. Decimal is not
IEEE decimal128. Decimal values compare numerically regardless of stored scale;
formatting preserves fractional scale, including trailing zeros.

Binary arithmetic first unwraps nullable scalar types, selects a common operand
type, and propagates null if an operand is null. Apply these rules in order:

| Operand combination | Common arithmetic type |
|---------------------|------------------------|
| Decimal and integral | decimal |
| Decimal and float/double | Invalid without an explicit conversion |
| Any double, no decimal | double |
| Any float, no decimal/double | float |
| ulong and unsigned integral | ulong |
| ulong and signed integral | Invalid without an explicit conversion |
| uint with long | ulong |
| Any long, no preceding rule | long |
| uint with any smaller integral | uint |
| Other integral combinations | int |

Nullable operands give a nullable result. Bitwise AND/OR/XOR follow the integral rules; non-nullable operands retain
non-nullable result metadata. Nullable input and conversion paths lift it. A shift preserves the promoted left
operand type (small integers promote to int); the shift count uses its low five
bits for 32-bit operands or low six bits for 64-bit operands. Signed right shifts
extend the sign; unsigned right shifts insert zero bits.

Integral addition, subtraction and multiplication wrap modulo 2^width. Integral
division truncates toward zero; remainder has the dividend's sign and magnitude
less than the divisor's magnitude. Integral or decimal division by zero fails.
Minimum signed value divided by -1 fails because its quotient is unrepresentable.
Floating-point division follows IEEE 754, including infinities and NaN.
Literal binding and constant arithmetic reject out-of-range values instead of
wrapping (MQ1009 for literals, MQ3032 for constant arithmetic). Strict numeric
casts reject overflow and round fractional numeric inputs to the nearest integer,
ties to even; this differs from integer division. Explicit floating narrowing
rounds to the destination floating format. These arithmetic rules do not override
the separate common-column inference rules for VALUES (§6.5).

#### 3.9.2 Text domains and comparisons

A string is a sequence of UTF-16 code units. A char is one unsigned 16-bit code
unit, including an isolated surrogate. Supplementary Unicode characters occupy
two units. Length, indexing, Substring, text parser positions, query diagnostic
offsets and columns count units, not Unicode scalar values or grapheme clusters.
Strings may contain unpaired surrogates; decoding byte encodings is stricter.
`\uXXXX` inserts exactly one unit. No implicit Unicode normalization is performed.

Ordinal comparison is lexicographic comparison of unsigned code units, with a
proper prefix ordered before the longer string. Ordinal case-insensitive name
matching uses one-to-one invariant uppercase mappings without normalization or
multi-character expansions. Keyword matching uses ASCII casing. Locale-sensitive
library functions are provider extensions; their contracts must name the locale.
The default language text policy uses Unicode 15.0 character categories and simple
case mappings. Later Unicode mappings require an explicitly identified profile.

LIKE matches the entire input, `%` matches zero or more units and `_` exactly one
unit, including a surrogate or newline. Case comparison is invariant. Regex
classes and regex options use §7.7; they do not change ordinal equality.

#### 3.9.3 Temporal and identifier domains

datetime is a proleptic Gregorian calendar date/time from 0001-01-01 through
9999-12-31 with 100-nanosecond precision, without a portable local timezone kind.
datetimeoffset adds an explicit whole-minute offset from -14:00 through +14:00;
the represented UTC instant must also be within that calendar range. Equality
and ordering compare instants, not displayed offsets. datetime equality/order
compare calendar ticks; no host timezone conversion is implied.
timespan is a signed 64-bit count of 100-nanosecond ticks. A guid is a 128-bit
identifier; its canonical textual form is lowercase `8-4-4-4-12` hexadecimal.
Temporal addition/subtraction operate in ticks and fail outside the result domain.
Subtracting two matching temporal values returns a timespan; datetimeoffset
subtraction compares UTC instants.

Portable temporal parsing accepts complete `yyyy-MM-dd` dates, optionally followed
by `T` or a space and `HH:mm:ss` with one through seven fractional second digits.
datetimeoffset requires a complete date/time and explicit `Z` or `±HH:mm` suffix.
Missing time means midnight for datetime. A portable datetime has no offset suffix;
host-local interpretations are extensions. timespan accepts `[-][d.]hh:mm:ss[.fffffff]`
with hours 00–23, minutes/seconds 00–59, and one through seven fractional digits.
Month/day names, omitted calendar components, slash dates and implicit local
offsets are host parsing extensions and MUST NOT define portable conversion.

#### 3.9.4 Strict conversion and formatting

Numeric text accepts optional surrounding ASCII whitespace and a leading sign.
Integer targets require decimal digits without a decimal point or exponent.
Decimal targets accept decimal digits, an optional decimal point and comma group
separators; floating targets additionally accept decimal exponents, NaN, Infinity
and -Infinity. Hex/binary/octal prefixes are SQL literal syntax, not integer text
conversion syntax. Numeric-to-bool maps zero to false and nonzero to true; bool-to-
numeric maps false/true to zero/one. Boolean text accepts True/False ignoring case
and surrounding whitespace. Char text requires exactly one UTF-16 code unit;
integer-to-char accepts 0–65535. Other logical conversions must be explicitly
defined by this section or rejected; custom dynamic-value conversions are extensions.

String casts format integers in base ten without grouping, bool as True/False,
char as its unit, decimal with its retained scale and a dot, and float/double as
the shortest decimal round-trip spelling (NaN, Infinity, -Infinity for special
values). datetime string casts use `MM/dd/yyyy HH:mm:ss`; datetimeoffset uses
`MM/dd/yyyy HH:mm:ss ±HH:mm`. These display casts omit fractional seconds.
timespan uses `[-][d.]hh:mm:ss[.fffffff]`, with seven fractional digits when nonzero.
Guid casts use the canonical form above. Fixture serialization instead uses
complete ISO temporal forms so expectations do not lose precision.

After validating every branch's names and types, binding may eliminate
statically unreachable CASE results; such results do not
contribute to the chosen result metadata. Invalid constant casts are binding errors. Invalid demanded execution casts fail
execution; unselected CASE branches, short-circuited predicates and unreachable
fallbacks MUST NOT execute conversions. Binding still validates constant casts
and types in these branches; execution laziness does not waive static validity. Null input stays null and scalar cast
metadata is nullable, even for a successful non-null constant.

Record identity and provider extension types are declared through metadata rather
than guessed from values. A dynamic value is a tagged logical value; it is not a
requirement for a universal host object type. Core defines only the conversions
of its recognized logical values. Provider extension conversions require a
separate published receiving contract.

---

## 4. Statement Structure

### 4.1 Statement Termination

Statements are optionally terminated with `;`. Multiple statements in a batch are separated by `;`:

```sql
table MyTable { Name: string };
couple schema.records with table MyTable as Source;
select Name from Source();
```

### 4.2 Statement Types

| Statement | Starting Keyword | Purpose |
|-----------|-----------------|---------|
| **SELECT query** | `SELECT` | Query data from sources |
| **Reordered query** | `FROM` | Query with FROM-first syntax |
| **CTE expression** | `WITH` | Define named temporary result sets |
| **Script variable** | `LET` | Define an immutable compile-time script value |
| **Enum declaration** | `ENUM` / `FLAGS ENUM` | Define a query-local nominal integral enum |
| **Table definition** | `TABLE` | Define a typed table structure |
| **Couple** | `COUPLE` | Bind a schema method to a table definition and/or source runtime settings profile |
| **Describe** | `DESC` | Introspect schema or query metadata |

### 4.3 Script Parameters

A script MAY begin with one parameter block. Either `param(...)` or `params(...)` may introduce it. Both spellings declare typed values supplied by the host before query execution.
`param(...)` remains the canonical spelling in this specification and in user-visible diagnostics, canonical query rendering.
`params(...)` is accepted only at that leading boundary.
The block is optional; a script without either spelling is parsed, compiled, and executed as before.

```sql
param(author: string, minStars: int = 10)
select Name, Stars
from values { (Name: 'Compiler', Author: 'Ada', Stars: 20, Description: 'important package'), (Name: 'Parser', Author: 'Bob', Stars: 5, Description: 'small package') } projects
where Author = $author and Stars >= $minStars
```

The parameter block MUST appear before every query or utility statement:

```sql
-- valid
param(createdAt: datetime = '2024-01-02T03:04:05', country: string)
select City from values { (Id: 1, Name: 'WARSAW', City: 'WARSAW', Country: 'Poland', Population: 1800000, CreatedAt: $createdAt), (Id: 2, Name: 'BERLIN', City: 'BERLIN', Country: 'Germany', Population: 3700000, CreatedAt: $createdAt) } cities where Country = $country

-- invalid: parameter block after a query statement
select City from values { (Id: 1, Name: 'WARSAW', City: 'WARSAW', Country: 'Poland', Population: 1800000, CreatedAt: $createdAt), (Id: 2, Name: 'BERLIN', City: 'BERLIN', Country: 'Germany', Population: 3700000, CreatedAt: $createdAt) } cities;
param(country: string)
```

#### 4.3.1 Declaration Syntax

Syntax fragment (not a complete executable batch):

```sql
param(name: type, optionalName: type = defaultValue)
```

Rules:
- A script MAY contain zero or one parameter block. The block may begin with either `param` or `params`.
- `param()` and `params()` are valid and declare no parameters.
- Parameter names are case-sensitive and MUST be unique inside the block.
- A declaration without a default value is required.
- A declaration with a default value is optional and uses the default when the host does not provide an override.
- Nullable types use the same `?` suffix used by table definitions, for example `int?` or `datetime?`.
- Parameter references use `$name` in expressions and behave as typed expressions whose type is the declared parameter type.

Examples:

```sql
param(createdAt: datetime = '2024-01-02T03:04:05', country: string, minPopulation: int = 1000000, since: datetime? = null)

select City
from values { (Id: 1, Name: 'WARSAW', City: 'WARSAW', Country: 'Poland', Population: 1800000, CreatedAt: $createdAt), (Id: 2, Name: 'BERLIN', City: 'BERLIN', Country: 'Germany', Population: 3700000, CreatedAt: $createdAt) } cities
where Country = $country
  and Population >= $minPopulation
  and ($since is null or CreatedAt >= $since)
```

The alias spelling is equivalent:

```sql
params(author: string)
select Name, Stars
from values { (Name: 'Compiler', Author: 'Ada', Stars: 20, Description: 'important package'), (Name: 'Parser', Author: 'Bob', Stars: 5, Description: 'small package') } projects
where Author = $author
```

```sql
param(createdAt: datetime = '2024-01-02T03:04:05', ids: int[])
select Name
from values { (Id: 1, Name: 'WARSAW', City: 'WARSAW', Country: 'Poland', Population: 1800000, CreatedAt: $createdAt), (Id: 2, Name: 'BERLIN', City: 'BERLIN', Country: 'Germany', Population: 3700000, CreatedAt: $createdAt) } cities
where Id in $ids
```

#### 4.3.2 Supported Parameter Types

Script parameters support scalar Musoq primitive expression types and structural records and collections. Collection element types may be primitive, nullable, record, or recursively nested collection types. The recursive `?` and `[]` suffixes are part of the declared type and are evaluated from right to left:

Scalar keywords and aliases use the logical types in §3.1–§3.2. `boolean`
and `bit` mean `bool`; `money` means `decimal`; `datetime`, `datetimeoffset`,
`timespan`, and `guid` name the corresponding types in §3.9.

The nullable form `type?` is supported for scalar types and accepts `null`. For a structural declaration, `?` applies recursively to the immediately preceding scalar, record, or collection node; `int?[]` and `int[]?` therefore have different meanings.

Examples of recursive declarations are `int?[]` (array of nullable integers), `int[]?` (nullable array of integers), `(Id: string)?[]?` (nullable array of nullable records), and `int[][]` (array of integer arrays). Structural declarations may use a closed named-record shape:

Syntax fragment (not a complete executable batch):

```sql
param(
    patterns: (
        Id: string,
        Pattern: string,
        Mode: string = 'literal'
    )[],
    emptyNumbers: int[] = array {},
    nullableNumbers: int?[] = array { 1, null },
    optionalRecords: (Id: string)?[]? = null,
    matrix: int?[][] = array { array { 1, null }, array {} }
)
```

Collection parameters are supplied as finite ordered values matching their declared element shape. Values are captured at the execution boundary; later host mutation cannot change the run. Host adapters MUST validate the declared logical shape. Text containing serialized data is not automatically a record or collection, and record keys must be strings. `IN $name` retains its scalar-element contract; structured collections are source arguments rather than implicit row sources.

#### 4.3.3 Default Values

Default values may be scalar constants, `null`, record literals, or array literals whose values are valid for the declared shape. Defaults are normalized once during parameter preflight.

```sql
param(
    flag: bool = true,
    code: char = 'x',
    limit: int? = null,
    id: guid = '2ffcf6fa-3369-4300-946a-bb131a037985',
    created: datetime = '2024-01-02T03:04:05'
)
select $flag, $code, $limit, $id, $created from values { (Seed: 0) } seed
```

Default binding rules:
- Numeric defaults use the literal type and suffix rules from §3.6 and Appendix D.
- `guid`, `datetime`, `datetimeoffset`, and `timespan` defaults are supplied as string literals and converted during binding.
- `null` is valid for string, dynamic and record types and nullable scalar types.
- A supplied parameter value overrides its default.
- Supplied parameter values must match the declared type; Musoq does not parse strings or perform numeric conversion for supplied values.
- Missing required parameters, type mismatches, and illegal null values fail before any data source is opened.

#### 4.3.4 Expression Type Compatibility

A parameter reference has the type declared in the parameter block. Comparisons, boolean clauses, function calls, `IN`, `CONTAINS`, `BETWEEN`, `CASE`, grouping, ordering, joins, windows, CTEs, and set operations use that declared type when checking expression compatibility.

Valid typed comparison:

```sql
param(minStars: int)
select Name
from values { (Name: 'Compiler', Author: 'Ada', Stars: 20, Description: 'important package'), (Name: 'Parser', Author: 'Bob', Stars: 5, Description: 'small package') } projects
where Stars >= $minStars
```

Invalid comparison:

```sql
-- INVALID: string parameters do not implicitly become numeric operands.
param(minStars: string)
select Name
from values { (Name: 'Compiler', Author: 'Ada', Stars: 20, Description: 'important package'), (Name: 'Parser', Author: 'Bob', Stars: 5, Description: 'small package') } projects
where Stars >= $minStars
```

The second query is invalid because `$minStars` is declared as `string`, while `Stars` is numeric. Musoq does not infer that the supplied string might contain a number.

Use a strict postfix cast when a textual parameter should be interpreted as another type:

```sql
param(minStarsText: string)
select Name
from values { (Name: 'Compiler', Author: 'Ada', Stars: 20, Description: 'important package'), (Name: 'Parser', Author: 'Bob', Stars: 5, Description: 'small package') } projects
where Stars >= $minStarsText::Int32
```

The library helper form, for example `ToInt32($minStarsText)`, remains available where its established soft conversion behavior is desired.

Normal expression compatibility still applies: same-type comparisons are valid, nullable and non-nullable forms of the same scalar type are compatible where Musoq normally permits them, and supported numeric widening follows the same rules as other typed expressions.

#### 4.3.5 Source Arguments

Script parameters MAY occur in ordinary supported scalar expressions in positional or named datasource arguments in `FROM` and correlated `APPLY`. Concatenation, arithmetic, CASE, explicit casts, indexing and scalar calls retain their normal type and evaluation rules. Each bound argument is evaluated once per source opening in the existing argument order; ordinary stability rules govern reuse and movement.

```sql
param(root: string = 'repo')
select Name from schema.files($root + '/src')

-- Also valid without a default when metadata does not depend on the path.
param(root: string)
select Name from schema.files($root + '/src')
```

Compilation evaluates metadata arguments only within the existing safe constant-expression subset, using literals, LET values and declared parameter defaults. It MUST NOT execute arbitrary scalar/plugin calls, read correlated columns, or fabricate values for required parameters. Computed scalar metadata values must fold completely using the bound numeric types and runtime string semantics. Runtime-supported expressions outside that subset remain valid when provider metadata is independent of their values. A provider requiring an unavailable metadata argument receives MQ3062; supply appropriate defaults and a safe static expression, or make its metadata independent of runtime arguments.

Metadata APIs receive only the known canonical positional prefix. Binding stops at the first unavailable slot, including named invocations; later constants/defaults MUST NOT shift into that slot. The original expressions and typed argument carriers remain available at execution. Runtime overrides do not trigger metadata discovery or recompilation and MUST preserve the compiled source shape and existing provider contracts. Missing required parameters fail before any source is opened.

Script variables are compile-time values and may be used directly or inside supported constant source-argument expressions. See [§4.4.4](#444-source-arguments).

#### 4.3.6 Unsupported Declaration Styles

Musoq deliberately uses one compact syntax instead of borrowing declaration forms from other languages. These forms are rejected with script-parameter diagnostics:

Syntax fragment (not a complete executable batch):

```sql
param(string author)             -- type-first order is invalid
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
| MQ2031 | parse | Malformed `param(...)` or `params(...)` declaration |
| MQ2032 | parse | Unsupported borrowed declaration syntax |
| MQ3056 | bind | Duplicate parameter block |
| MQ3057 | bind | Parameter block after another statement |
| MQ3058 | bind | Duplicate parameter name |
| MQ3059 | bind | Reference to undeclared `$name` |
| MQ3060 | bind | Unsupported parameter type |
| MQ3061 | bind | Invalid default value for the declared scalar or structural type, including null defaults on nonnullable collections |
| MQ3062 | bind | Invalid parameter usage in a source argument |
| MQ3005 | bind | Parameter declared type is incompatible with the expression context |
| MQ7003 | runtime | Required parameter missing |
| MQ7004 | runtime | Supplied value has the wrong logical type |
| MQ7005 | runtime | Supplied `null` for a non-nullable scalar type or collection parameter used by `IN`/`NOT IN` |

Every envelope includes a stable code, severity, phase, message, explanation, suggested fixes, and a documentation reference.

### 4.4 Script Variables

A script MAY declare immutable script variables with `let`. A script variable is evaluated during binding, and referenced later with the same `$name` syntax used by script parameters.

```sql
let topic: string = 'important'
let minimumStars: int = 10 + 5

select Name, Stars
from values { (Name: 'Compiler', Author: 'Ada', Stars: 20, Description: 'important package'), (Name: 'Parser', Author: 'Bob', Stars: 5, Description: 'small package') } projects
where Description like '%' + $topic + '%'
    and Stars >= $minimumStars
```

`let` is contextual: it begins a script variable declaration only when it appears at statement start. Existing identifiers or columns named `let` remain valid in expression contexts.

#### 4.4.1 Declaration Syntax

Syntax fragment (not a complete executable batch):

```sql
let name: type = initializer
```

Rules:
- Script variable names are case-sensitive.
- Script variables and script parameters share the `$name` namespace. A parameter name MUST NOT be redeclared by `let`, and a `let` name MUST NOT be declared twice.
- A script variable declaration MAY appear anywhere in the script before its first use.
- A script variable is immutable and has no runtime override mechanism.
- Nullable types use the same `?` suffix used by table definitions and script parameters, for example `int?` or `datetime?`.
- References use `$name` in expressions and behave as typed expressions whose type is the declared script variable type.

#### 4.4.2 Supported Types

Script variables support the same primitive and recursive structural types as script parameters. Inferred record arrays preserve field presence: an omitted field remains omitted and is not rewritten as an explicit `null`.

#### 4.4.3 Initializers

Initializers MUST be compile-time constants. They MAY use literals, `null`, `array { ... }`, named records `(Field: expression, ...)`, supported unary/binary constant operators, comparisons, boolean operators, `IS NULL`, `BETWEEN`, and references to earlier script variables. Runtime parameters, datasource access, subqueries, forward references, and unsupported function evaluation remain invalid.

Syntax fragment (not a complete executable batch):

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
select Name from schema.files($root + '/src')
```

Script parameters may also occur in source-argument expressions. Their defaults can participate in safe static metadata expressions, while current runtime values determine actual source arguments; see [§4.3.5](#435-source-arguments). LET initializers still cannot depend on runtime parameters.

#### 4.4.5 Diagnostics

Script-variable mistakes are reported through the standard Musoq error envelope format.

| Code | Phase | Typical Cause |
|------|-------|---------------|
| MQ2033 | parse | Malformed `let name: type = value` declaration |
| MQ3063 | bind | Duplicate name across a parameter block and `let` declarations |
| MQ3064 | bind | Unsupported script variable type |
| MQ3065 | bind | Invalid initializer for the declared type |
| MQ3066 | bind | Reference to a script variable before it is declared |

---

### 4.5 Structured values and source inputs

Musoq uses one named-record delimiter for expression values and inline table rows. Parentheses are disambiguated by lookahead: `(Name: expression)` is a record literal, while any other parenthesized expression keeps ordinary grouping and scalar-subquery behavior. `array` and `values` are contextual words; comments and supported trivia may occur between the word and its opening brace, and ordinary identifiers with either spelling remain valid outside the construct.

```text
record_literal ::= '(' named_field { ',' named_field } [ ',' ] ')'
named_field    ::= field_name ':' expression
array_literal  ::= ARRAY '{' [ expression { ',' expression } [ ',' ] ] '}'
values_source  ::= VALUES '{' values_row { ',' values_row } [ ',' ] '}' alias
values_row     ::= '(' values_field { ',' values_field } [ ',' ] ')'
values_field   ::= field_name ':' values_expression
```

The final VALUES spelling is a breaking migration:

```sql
select p.Id
from values { (Id: 'todo'), (Id: 'fixme'), } p
```

The former `values { { Id: 'todo' } } p` spelling is rejected; there is no compatibility parser or feature flag. VALUES still requires an alias, one or more rows, one or more fields per row, the same case-insensitive field set in every row (field order may differ), and the existing scalar constant-expression restrictions. Its numeric suffix, type inference, null, join, apply, grouping, ordering, set, window, paging, CTE, diagnostics, and typed-lowering behavior are unchanged.

An array is an ordered, multiplicity-preserving collection value. It may contain primitive values, records, or nested arrays, including empty and trailing-comma forms:

```sql
let patterns = array {
    (Id: 'todo', Pattern: 'TODO'),
    (Pattern: 'FIXME', Id: 'fixme'),
};

select m.PatternId
from schema.match('TODO FIXME', patterns: $patterns) m
```

Record fields are matched using ordinal case-insensitive names (§3.9); single-field records are valid, and arbitrary record equality, member access, and collection-to-table conversion are outside this feature. Empty or all-null arrays require an expected element type. Core never falls back to a collection of dynamic values to hide missing or incompatible type information.

Structural source arguments use the same binder for literals, `let`, parameters, and CTE results. A receiver publishes one unambiguous closed record shape with named, typed fields and optional defaults. Defaults are applied in this order: supplied value, declared field default, receiving field default, then a missing-required-field error. Explicit `null` never activates a default. Missing or ambiguous receiving shapes, cyclic input values, automatic serialized-text conversion, and guessing a shape from host values are rejected.

An explicit receiving contract is required for structural source calls. Normalization, materialization, reuse and invocation MUST preserve authored evaluation order and receiver validation. Construction occurs once per source invocation, retains execution context and cancellation, and never moves into a provider's result loop. Host values and CTE results are captured per execution. Core-owned snapshots and provider-facing collections are not shared between runs or invocations.

The default structural input limits are maximum depth 32 (the root is depth 1), 100,000 logical value nodes, and 67,108,864 UTF-16 payload bytes. Record, collection, scalar, null, and applied-default occurrences count as nodes; checked `length * 2` counts string payload. Limits are checked before storage grows during parameter preflight and whenever a receiver demands an inline, let, parameter, or CTE value. A receiver with stricter limits cannot reject an input that it does not demand.

Complete CTE results may be passed as a source argument by a bare visible CTE name, optionally grouped with parentheses. `$name` remains a parameter or `let`, string literals remain strings, qualified columns remain scalar values, and a row alias never becomes a collection. Primitive collection receivers require exactly one CTE output column; record receivers match output columns by name, reject unexpected columns before pruning, preserve order and duplicates, and use receiving-field defaults for missing optional fields. Ambiguous scalar/CTE interpretations and newly introduced dependency cycles are errors. There is no `table(...)` syntax and no implicit singleton wrapping or unwrapping.

The structural contract is also the source for metadata descriptions. `DESC ARGUMENTS` reports the selected receiving contract without evaluating values, opening a source, or enumerating a CTE. It returns `Overload`, `Path`, `Kind`, `Type`, `Required`, `Nullable`, `HasDefault`, `Default`, `MaxDepth`, `MaxNodes`, and `MaxStringBytes` in deterministic signature and nested-field order. An explicit null default has `HasDefault = true` and the four-character text `null` in its `Default` cell; an absent default has `HasDefault = false` and a database-null cell. This distinction is visible in metadata and does not change the fact that an explicitly supplied null never activates a default.

The former brace-row spelling remains rejected. Implementations MUST preserve authored evaluation order, deterministic overload selection, per-execution capture, receiver validation and ownership independently of evaluation strategy.

---
## 5. SELECT Clause

### 5.1 Basic Syntax

Syntax fragment (not a complete executable batch):

```sql
SELECT [DISTINCT] expression [[AS] alias], ...
```

### 5.2 Column Expressions

Any expression can appear in SELECT:

```sql
select 1 from values { (Seed: 0) } seed                -- literal
select Name from values { (Name: 'Ada') } people       -- column reference
select p.Name from values { (Name: 'Ada') } p           -- qualified column
select 1 + 2 * 3 from values { (Seed: 0) } seed          -- arithmetic
select Concat(City, ', ', Country)
from values { (City: 'WARSAW', Country: 'Poland') } places -- function call

-- Supplied member/callable shapes follow Appendix H.
select a.GetPopulation() from schema.records() a
select Self.Name from schema.records()
select Self.Array[2] from schema.records()
```

### 5.3 Column Aliasing

Three equivalent forms:

```sql
select Name as FullName from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows       -- explicit AS keyword
select Name FullName from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows           -- implicit (space-separated)
select Name [Full Name] from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows        -- bracketed (allows spaces)
```

When no alias is given, the column name is derived from the expression:
- Column reference: `Name` → column name `Name`
- Source-qualified column reference: `a.Name` → column name `Name`
- Function call: `Count(Name)` → column name `Count(Name)`
- Literal: `1` → column name `1`

Simple column projections and wildcard expansions use the bound source column name without adding the query's source alias. This rule applies to top-level results, CTE and derived-table projections, joins, APPLY, and query descriptions. Explicit aliases and star RENAME targets are preserved exactly, including names containing a dot. Non-column expressions retain their expression-derived names.

Result labels need not be unique. `select l.Id, r.Id` exposes two columns named `Id`, with independent positions and types. Wildcard expansion preserves both columns; a reference to an ambiguous name must be disambiguated by distinct output aliases. Duplicate labels do not change source-column identity, row values, or column ordering. Existing uniqueness requirements for explicit CTE column lists and star RENAME targets still apply.

**Compatibility:** implicit result labels previously included source qualifiers in some projections. Consumers selecting a result column named `p.Name` must now use `Name`, or request the old label explicitly with `select p.Name as [p.Name]`. Exported JSON/CSV headers and query-description labels follow the new output names. Use distinct explicit aliases when a consumer requires unique field names.

Explicit aliases always define the output column name. This is especially important for CTEs and set operations, where later query blocks see the output names of the preceding query, not the source aliases used to compute them:

```sql
with p as (
    select a.City from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'BERLIN'),
    (Name: 'Cara', City: 'WARSAW'),
    (Name: 'Dan', City: 'MUNICH')
} a
)
select City from p          -- valid; exposed name is City

-- INVALID unless explicitly aliased as [a.City]
select [a.City] from p
```

If a dotted output name is required, it MUST be declared explicitly and referenced as a bracket-quoted identifier:

```sql
with p as (
    select a.City as [a.City] from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} a
)
select [a.City] from p
```

Musoq also makes explicit SELECT aliases visible in selected clauses of the same query block:

```sql
-- Non-aggregate alias in WHERE
select Name as FileName from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'BERLIN'),
    (Name: 'Cara', City: 'WARSAW'),
    (Name: 'Dan', City: 'MUNICH')
} rows where FileName = 'test'

-- Non-aggregate alias in GROUP BY
select Length(Name) as NameLen, Count(*)
from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'BERLIN'),
    (Name: 'Cara', City: 'WARSAW'),
    (Name: 'Dan', City: 'MUNICH')
} rows
group by NameLen

-- Aggregate alias in HAVING
select Count(*) as cnt
from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'BERLIN'),
    (Name: 'Cara', City: 'WARSAW'),
    (Name: 'Dan', City: 'MUNICH')
} rows
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
select * from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows                              -- all columns
select *, Name as Name2 from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} a             -- star + explicit columns
select *, * from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} a                          -- duplicated columns
```

Qualified star selects columns from a specific table in a join:

```sql
select a.* from values {
    (Id: 1),
    (Id: 2),
    (Id: 3),
    (Id: 4)
} a inner join values {
    (Id: 1),
    (Id: 2),
    (Id: 3),
    (Id: 4)
} b on a.Id = b.Id
select a.*, b.* from values {
    (Id: 1),
    (Id: 2),
    (Id: 3),
    (Id: 4)
} a inner join values {
    (Id: 1),
    (Id: 2),
    (Id: 3),
    (Id: 4)
} b on a.Id = b.Id
```

Star works through CTEs:

```sql
with p as (select City, Country from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} rows)
select * from p       -- expands to City, Country
```

Qualified star inside a CTE exports the source column names, not source-qualified names:

```sql
with p as (select a.* from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} a)
select City, Country from p
```

#### 5.4.1 Star Modifiers

Star expressions support optional modifiers that filter or transform the expanded columns. Modifiers are applied in a fixed order: **LIKE/NOT LIKE → EXCLUDE → REPLACE → RENAME**.

**EXCLUDE** removes named columns from the expansion:

```sql
select * exclude (City) from values {
    (Id: 1, City: 'WARSAW', Country: 'Poland', Population: 100d),
    (Id: 2, City: 'BERLIN', Country: 'Germany', Population: 200d),
    (Id: 3, City: 'WARSAW', Country: 'Poland', Population: 200d),
    (Id: 4, City: 'MUNICH', Country: 'Germany', Population: 400d)
} a
select * exclude (City, Country, Population) from values {
    (Id: 1, City: 'WARSAW', Country: 'Poland', Population: 100d),
    (Id: 2, City: 'BERLIN', Country: 'Germany', Population: 200d),
    (Id: 3, City: 'WARSAW', Country: 'Poland', Population: 200d),
    (Id: 4, City: 'MUNICH', Country: 'Germany', Population: 400d)
} a
select a.* exclude (Id) from values {
    (Id: 1, City: 'WARSAW', Country: 'Poland', Population: 100d),
    (Id: 2, City: 'BERLIN', Country: 'Germany', Population: 200d),
    (Id: 3, City: 'WARSAW', Country: 'Poland', Population: 200d),
    (Id: 4, City: 'MUNICH', Country: 'Germany', Population: 400d)
} a inner join values {
    (Id: 1, City: 'WARSAW', Country: 'Poland', Population: 100d),
    (Id: 2, City: 'BERLIN', Country: 'Germany', Population: 200d),
    (Id: 3, City: 'HAMBURG', Country: 'Germany', Population: 200d),
    (Id: 4, City: 'ROME', Country: 'Italy', Population: 400d)
} b on a.Id = b.Id
```

**REPLACE** substitutes a column's expression while keeping it in the same position:

```sql
select * replace (Population * 2 as Population) from values {
    (Name: 'Ada', Population: 100d, Money: 100d),
    (Name: 'Bob', Population: 200d, Money: 200d),
    (Name: 'Cara', Population: 200d, Money: 300d),
    (Name: 'Dan', Population: 400d, Money: 400d)
} a
select * replace (ToUpper(Name) as Name, Round(Money, 0) as Money) from values {
    (Name: 'Ada', Population: 100d, Money: 100d),
    (Name: 'Bob', Population: 200d, Money: 200d),
    (Name: 'Cara', Population: 200d, Money: 300d),
    (Name: 'Dan', Population: 400d, Money: 400d)
} a
```

**RENAME** changes the output column name after filtering and replacement without changing the underlying expression:

```sql
select * rename (Name as EntityName) from values {
    (Name: 'Ada', Population: 100d),
    (Name: 'Bob', Population: 200d),
    (Name: 'Cara', Population: 200d),
    (Name: 'Dan', Population: 400d)
} a
select * replace (Population * 2 as Population) rename (Population as Pop2) from values {
    (Name: 'Ada', Population: 100d),
    (Name: 'Bob', Population: 200d),
    (Name: 'Cara', Population: 200d),
    (Name: 'Dan', Population: 400d)
} a
select a.* rename (a.Name as EntityName) from values {
    (Name: 'Ada', Population: 100d),
    (Name: 'Bob', Population: 200d),
    (Name: 'Cara', Population: 200d),
    (Name: 'Dan', Population: 400d)
} a
```

**LIKE / NOT LIKE** filters columns by a SQL LIKE pattern on column names (case-insensitive):

```sql
select * like 'C%' from values { (Id: 1, Name: 'Ada', City: 'WARSAW', Country: 'Poland') } a           -- only columns starting with 'C'
select * not like '%Id' from values { (Id: 1, Name: 'Ada', City: 'WARSAW', Country: 'Poland') } a       -- exclude columns ending with 'Id'
select * like '_ame' from values { (Id: 1, Name: 'Ada', City: 'WARSAW', Country: 'Poland') } a          -- match single-char wildcard
```

Modifiers can be composed:

```sql
select * like '%o%' exclude (Country) replace (Population * 3 as Population) rename (Population as Population3x) from values {
    (Country: 'Poland', Population: 100d),
    (Country: 'Germany', Population: 200d),
    (Country: 'Poland', Population: 200d),
    (Country: 'Germany', Population: 400d)
} a
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
select distinct Country from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} rows
select distinct City, Country from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} rows   -- unique combinations
```

DISTINCT uses **ordinal (case-sensitive) comparison** — `'POLAND'` and `'poland'` are treated as different values. To achieve case-insensitive deduplication, use `ToLower()`:

```sql
select distinct ToLower(Country) from values {
    (Country: 'Poland'),
    (Country: 'Germany'),
    (Country: 'Poland'),
    (Country: 'Germany')
} rows
```

### 5.6 RowNumber

`RowNumber()` returns a 1-based sequential integer for each row in the result:

```sql
select RowNumber(), Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows
```

`RowNumber()` is assigned **after** ORDER BY but **before** SKIP/TAKE. When used with ORDER BY, rows are first sorted, then numbered sequentially:

```sql
select Country, RowNumber() from values { (Country: 'Poland'), (Country: 'Germany') } countries order by Country
-- Rows are sorted alphabetically, then numbered 1, 2, 3, ...
-- Germany → 1, Poland → 2 (sorted order determines numbering)
```

With SKIP, row numbers are assigned before SKIP is applied:

```sql
select Country, RowNumber() from values { (Country: 'Poland'), (Country: 'Germany') } countries order by Country skip 1
-- Full result: Germany=1, Poland=2; after SKIP 1: Poland=2 (number preserved)
```

With WHERE filtering, `RowNumber()` counts only the rows that pass the filter:

```sql
select Country, RowNumber() from values { (Country: 'Poland'), (Country: 'Germany') } countries where Country = 'Poland'
-- Returns rows numbered 1, 2, ... (only matching rows counted)
```

---

## 6. FROM Clause

### 6.1 Schema Data Sources

The primary data source syntax uses schema providers:

```sql
select * from schema.method()
select * from schema.method('4', 2)
select * from schema.method() sourceAlias
```

Arguments can be literals of any type:

```sql
select * from schema.arguments(1, 2d, true, false, 'text')
```

#### 6.1.1 Named source arguments

Datasource calls may bind arguments by the published source parameter
name. Names are case-insensitive and are part of the datasource's public SQL
surface:

```sql
select * from schema.method(required: '4', limit: 2)
select * from schema.method('4', limit: 2) -- positional prefix, then named suffix
```

The receiver publishes an ordered signature with parameter names, logical types,
and usable optional defaults. Arguments before the first named argument are
positional; positional arguments after a named argument are invalid. Named
arguments may be reordered. Unknown names, duplicate assignments, missing required
parameters, incompatible types, and ambiguous overloads fail during binding.
Omitted optional arguments use the published default; explicit null does not.

These rules apply to FROM, JOIN, APPLY, coupled aliases, concrete DESC, DESC SETTINGS,
and source calls in DESC QUERY. Scalar/aggregate functions, row-access functions,
Interpret/Parse and DESC FUNCTIONS remain positional-only. A source without
complete named-signature metadata remains positional-only. Hidden context and
cancellation inputs are not SQL-bindable parameters. Inconsistent signature
metadata MUST NOT enable named binding.

Exact logical type matches outrank compatible matches. Scalar applicability and
overload preference follow §22.6; equally applicable or incomparable overloads
are ambiguous. Registration order MUST NOT select an overload.
Before execution, metadata requests may inspect only the known ordered argument
prefix. A later constant or default MUST NOT shift into an earlier unknown slot.
At execution the selected callable receives all argument values in signature order.

### 6.2 Table and Source Aliasing

Data sources can be given an alias for reference in expressions:

```sql
select a.Name from schema.records() a
select entities.Name from schema.records() entities
```

In **single-table queries**, aliasing is optional — column names can be used directly:

```sql
select Name from schema.records()         -- no alias needed
select a.Name from schema.records() a     -- alias optional
```

Every source in a query block containing a `JOIN` or `APPLY` MUST have a stable,
addressable name. A CTE/reference name and a named in-memory table are natural
aliases. Schema calls, function sources, row methods, row properties, derived
tables, and `VALUES` sources require an explicit alias in such a block. A single
schema or function source remains backward-compatible when it is unaliased.
Derived tables and `VALUES` sources always require an explicit alias, including
when they are the only source in the query block. Generated evaluator aliases
are internal implementation details and do not satisfy this syntax rule.

The rule is applied independently inside every nested query block. For function
calls in a multi-source query, the engine still attempts method-owner
auto-resolution after the source aliases have been parsed; see
[§8.10.1](#8101-method-auto-resolution-algorithm) for details.

```sql
select a.Name, b.City
from schema.records() a
inner join schema.byCountry() b on a.Id = b.Id
```

The first source must already have an alias before the first join or apply
operator, and each right operand must have one before its following boundary:

```sql
select a1.Name, item.Value
from schema.collections() a1
cross apply a1.Column item
take 10
```

If the right-side alias is omitted, the boundary remains part of the query and
the parser reports `MQ2035_MissingRequiredAlias` at the insertion point:

```sql
select a1.Name
from schema.collections() a1
cross apply a1.Column
take 10
-- MQ2035 at the position immediately after Column:
-- The CROSS APPLY source requires an alias before TAKE.
```

Clause and operator words are not consumed as implicit aliases at an active
source boundary. Use brackets when a reserved word is intentional:

```sql
select [take].Name, [where].Name
from schema.records() [take]
cross apply [take].Children [where]
take 10
```

After an explicit `AS`, a boundary is still a missing-alias error, while a
non-boundary invalid token retains `MQ2022_InvalidAlias`:

```sql
-- MQ2035: the alias identifier is missing after AS; TAKE remains unconsumed
cross apply a1.Column AS TAKE

-- MQ2022: 123 is not an alias identifier
cross apply a1.Column AS 123
```

### 6.3 CTE References

After a CTE is defined, use its name as a data source:

```sql
with cte as (select City, Country from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} rows)
select * from cte
```

CTE references can be aliased:

```sql
with cte as (select City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} rows)
select p.City from cte p
```

When a CTE reference has a table alias, that alias hides the original CTE name in the current query block:

```sql
with cte as (select City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} rows)
select p.City from cte p      -- valid

-- INVALID in the same query block
select cte.City from cte p
```

CTE names do not reserve table/source aliases in unrelated query blocks. A source alias used inside a CTE body is local to that CTE body, and the outer query may reuse the same text for its own aliases.

In a multi-source block, the CTE name itself is a natural stable alias and may
be used without an explicit override:

```sql
with source as (select City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} a)
select source.City, rightSide.City
from source
cross join values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'HAMBURG'),
    (City: 'ROME')
} rightSide
```

### 6.4 Derived Tables

A parenthesized query may be used as a `FROM` or `JOIN` source when it has an alias:

```sql
select d.City
from (
    select a.City
    from values {
    (City: 'WARSAW', Country: 'Poland', Population: 100d),
    (City: 'BERLIN', Country: 'Germany', Population: 200d),
    (City: 'WARSAW', Country: 'Poland', Population: 200d),
    (City: 'MUNICH', Country: 'Germany', Population: 400d)
} a
    where a.Population > 250
) d

select a.City, d.City
from values {
    (City: 'WARSAW', Country: 'Poland', Population: 100d),
    (City: 'BERLIN', Country: 'Germany', Population: 200d),
    (City: 'WARSAW', Country: 'Poland', Population: 200d),
    (City: 'MUNICH', Country: 'Germany', Population: 400d)
} a
inner join (
    select b.Country, b.City
    from values {
    (City: 'WARSAW', Country: 'Poland', Population: 100d),
    (City: 'BERLIN', Country: 'Germany', Population: 200d),
    (City: 'HAMBURG', Country: 'Germany', Population: 200d),
    (City: 'ROME', Country: 'Italy', Population: 400d)
} b
) d on a.Country = d.Country
```

Rules:

- A derived table MUST have an alias after the closing parenthesis.
- The alias is required even when the derived table is the only source in its
  query block, and it is required again when the derived table is a JOIN/APPLY
  operand.
- The derived-table body may be a normal `SELECT` query, a `FROM`-first query, a query with set operators, or a `WITH` expression.
- Query blocks outside the derived table see only the derived table output column names. Source aliases declared inside the derived-table body are not exported.
- `*` and `alias.*` expand the derived table output columns under the derived table alias.
- Plain derived tables are not lateral. A derived table in `FROM` or `JOIN` MUST NOT reference source aliases from the containing query block.
- To write a correlated derived table, use `CROSS APPLY` or `OUTER APPLY`; see §9.7.

```sql
-- INVALID: plain JOIN derived tables are independent
select a.City, d.City
from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} a
inner join (
    select b.City
    from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'HAMBURG', Country: 'Germany'),
    (City: 'ROME', Country: 'Italy')
} b
    where b.Country = a.Country
) d on 1 = 1
```

### 6.5 Inline VALUES Row Sources

`values` can be used in the `FROM` clause to define a small inline table:

```sql
from values {
    (Name: 'Newtonsoft.Json', Approved: true, Score: 10ui),
    (Name: 'Legacy.Package', Approved: false, Score: 20ui)
} packages
select packages.Name, packages.Score
```

Scalar script parameters and scalar `let` variables may be used in row expressions:

```sql
param(defaultScore: int)
let boost: int = 5

from values {
    (Name: 'Pinned', Score: $defaultScore + $boost),
    (Name: 'Fallback', Score: 0)
} packages
select packages.Name, packages.Score
```

Rules:

- A VALUES source MUST have an alias after the closing brace.
- The alias is required whether the VALUES source is alone or participates in
  a JOIN/APPLY block.
- The source MUST contain at least one row.
- Every row MUST contain at least one field.
- Field names are case-insensitive for shape validation and MUST be unique within each row.
- Every row MUST contain the same field names as the first row.
- Field expressions MAY use literals, `NULL`, scalar script parameter references, scalar `let` references, and arithmetic composed from those values.
- Field expressions MUST NOT reference source columns, aggregate functions, window functions, subqueries, or any row-dependent expression.
- Each field becomes a strongly typed column. Numeric suffixes control the logical column type; for example, `10ui` produces a `uint` column. Bare `10u` is not a valid suffix.
- VALUES row literals use the same numeric literal syntax as the rest of the language. Supported integer type suffixes are `b`, `ub`, `s`, `us`, `i`, `ui`, `l`, and `ul`; `d`/`D` forces a decimal literal.
- Base-prefixed numeric literals are supported: `0x`/`0X` for hexadecimal, `0b`/`0B` for binary, and `0o`/`0O` for octal. These prefixes produce `long` values and do not accept type suffixes; for example, `0x10` is valid and `0x10ui` is not.
- `NULL` values are allowed. If a column contains both `NULL` and scalar expressions, the logical column type is nullable.
- Numeric column inference includes parameter and `let` expression types. It preserves the exact type when all non-NULL expressions have the same type. Mixed numeric columns are promoted conservatively: `decimal` wins over integer types, `long` wins over non-`ulong` integer mixes that contain a `long`, `uint` wins over smaller non-`long` integer mixes, and `ulong` may only mix with unsigned integer values unless the column also contains `decimal`.

VALUES sources participate in joins, applies, grouping, ordering, set operators, subqueries, CTEs, and built-in ranking/offset window functions like any other source:

```sql
with policy as (
    from values {
        (Name: 'Newtonsoft.Json', Approved: true),
        (Name: 'Legacy.Package', Approved: false)
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
couple schema.records with table MyTable as Source;
select Name from Source()
select Name from Source(true, 'param')   -- with arguments
```

Coupled aliases use the underlying source's published names and defaults as their
signature. TABLE controls row shape, not source parameters. Named and positional
binding use core §6.1 consistently in metadata and execution.


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

Logical values are `true`, `false`, and `NULL` (unknown). `AND`, `OR`, and `NOT`
use the following three-valued rules:

| Left | Right | AND | OR |
|------|-------|-----|----|
| false | false | false | false |
| false | true | false | true |
| false | NULL | false | NULL |
| true | false | false | true |
| true | true | true | true |
| true | NULL | NULL | true |
| NULL | false | false | NULL |
| NULL | true | NULL | true |
| NULL | NULL | NULL | NULL |

`NOT true` is false, `NOT false` is true, and `NOT NULL` is NULL. Logical result
metadata is nullable when its operand types can produce an unknown result.
Filtering clauses admit only true; false and unknown both exclude the row.

Logical operands are evaluated from left to right, at most once per demanded
expression. For `AND`, a false left operand MUST leave the right operand
unevaluated. For `OR`, a true left operand MUST leave the right operand
unevaluated. A null left operand requires the right operand to distinguish a
decisive result from unknown. `NOT` evaluates its operand once.

Runtime laziness does not waive static name and type validity. An unselected
operand does not execute casts, functions, or subqueries and cannot raise their
runtime failures. Optimizers may transform expressions only when they preserve
these guards, results, and observable failures. Stability alone does not permit
moving a potentially failing expression outside its guard. Provider contracts
must identify volatile functions and fields; declared stable callables may be
reused only within the same valid evaluation region. Ordinary scalar function
arguments remain eager unless their own contract specifies otherwise.

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
from values {
    (Name: 'Ada', OptionalCode: 7),
    (Name: 'Bob', OptionalCode: null),
    (Name: 'Cara', OptionalCode: 7),
    (Name: 'Dan', OptionalCode: 9)
} a
inner join values {
    (Name: 'Eve', OptionalCode: 7),
    (Name: 'Finn', OptionalCode: null),
    (Name: 'Gia', OptionalCode: 7),
    (Name: 'Hugo', OptionalCode: 9)
} b on a.OptionalCode is not distinct from b.OptionalCode
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

`LIKE` uses SQL wildcards, not filesystem glob syntax. A constant pattern
such as `LIKE '*.log'` or `LIKE 'file?.txt'` produces the advisory warning
`MQ5016_GlobWildcardInLike` when it has no `%` or `_` wildcard and looks like
a path or filename. Use `%` / `_`, or use `RLIKE` when regex semantics are
intended.

### 7.7 RLIKE (Regular Expression Matching)

`RLIKE` searches for a case-sensitive regular-expression match; anchors constrain
where a match may occur. Required constructs are literals, escapes, dot, classes,
quantifiers (including lazy forms), alternation, groups, named groups, anchors,
lookahead/lookbehind and backreferences. Unlike text schema patterns, RLIKE permits
lookaround and backreferences. Inline flags `i`, `m`, `s`, `x` are supported.
Dot excludes LF unless `s` is active; `^`/`$` denote input boundaries (`m` adds
line boundaries; `$` may precede a final LF). `\A`/`\z` denote absolute boundaries.
`\d` uses Unicode Nd; `\w` uses Ll, Lu, Lt, Lo, Lm, Mn, Nd and Pc;
`\s` uses Unicode separators plus tab, LF, VT, FF, CR and NEL. Negated forms
complement these classes. Matching consumes UTF-16 units, uses no normalization,
and chooses the first leftmost match with alternatives in authored order.
Case-insensitive matching uses the invariant text policy (§3.9); host-locale regex
options are compatibility extensions.

```sql
where Name rlike '^\d+'              -- starts with digits
where Email rlike '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'
where Name not rlike '^test.*$'      -- does not match pattern
```

> **Note**: An invalid constant regex pattern that can be validated during binding
> is rejected with `MQ3094_InvalidConstantRegex`. Patterns supplied by parameters,
> variables, columns, or other expressions that are unavailable during binding
> are validated when evaluated during execution. Invalid runtime patterns raise
> `MQ7015_InvalidRuntimeRegex`. Preparing an unevaluated pattern does not require
> regex validation; null and short-circuit behavior is preserved.

Exhausting an implementation's finite regex match budget is a runtime failure
reported as `MQ7017_RegexMatchTimeout`. It does not make a valid pattern invalid
and must not be converted into a `false` predicate result. Budget values and
execution-engine choices are implementation configuration, not portable syntax.

An ordinary source `\b` is decoded to a backspace before the regex engine sees
it. In a regex context this produces `MQ5015_SuspiciousRegexEscape`:

```sql
where Name rlike '\bword\b'       -- warning: use r'\bword\b'
where Name rlike r'\bword\b'      -- warning-free word-boundary pattern
where Name rlike '\\bword\\b'     -- warning-free ordinary spelling
```

The warning is limited to a `\b` outside a regex character class. Other
preserved escapes and intentional character-class backspaces are not guessed.

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
from values {
    (Id: 1, Name: 'Ada'),
    (Id: 2, Name: 'Bob'),
    (Id: 3, Name: 'Cara'),
    (Id: 4, Name: 'Dan')
} rows
where Id in $ids
```

The left expression type must match the parameter element type after normal nullable normalization. Membership checks the captured collection values in order using the declared element type. A missing or `NULL` collection parameter fails during parameter binding before source execution.

#### 7.9.3 Subqueries

The `IN` and `NOT IN` predicates also accept a single-column subquery:

```sql
-- Basic IN subquery: keep rows whose City appears in the subquery result
select a.City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} a
where a.City in (select b.City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'HAMBURG'),
    (City: 'ROME')
} b)

-- NOT IN subquery: exclude rows whose City appears in the subquery result
select a.City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} a
where a.City not in (select b.City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'HAMBURG'),
    (City: 'ROME')
} b)
```

The subquery MUST return exactly one column. If it returns more than one column, a compile-time error is raised (MQ3049):

```sql
-- ERROR: MQ3049 — subquery returns two columns
select a.City from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} a
where a.City in (select b.City, b.Country from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'HAMBURG', Country: 'Germany'),
    (City: 'ROME', Country: 'Italy')
} b)
```

#### 7.9.4 Set Operators in IN Subqueries

Subqueries within `IN` / `NOT IN` support set operators (`UNION`, `EXCEPT`, `INTERSECT`):

```sql
-- UNION: cities from either source
select a.City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} a
where a.City in (
    select b.City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'HAMBURG'),
    (City: 'ROME')
} b
    union (City) select c.City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} c
)

-- EXCEPT: cities in B but not in C
select a.City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} a
where a.City in (
    select b.City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'HAMBURG'),
    (City: 'ROME')
} b
    except (City) select c.City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} c
)

-- INTERSECT: cities in both B and C
select a.City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} a
where a.City in (
    select b.City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'HAMBURG'),
    (City: 'ROME')
} b
    intersect (City) select c.City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} c
)
```

Set operators within IN subqueries follow the same key-column syntax as top-level set operations (§12).

#### 7.9.5 IN Subqueries with Other Conditions

IN subqueries can be combined with other conditions using `AND` and `OR`:

```sql
-- Combined with OR
select a.City from values {
    (City: 'WARSAW', Country: 'Poland', Population: 100d),
    (City: 'BERLIN', Country: 'Germany', Population: 200d),
    (City: 'WARSAW', Country: 'Poland', Population: 200d),
    (City: 'MUNICH', Country: 'Germany', Population: 400d)
} a
where a.City in (select b.City from values {
    (City: 'WARSAW', Country: 'Poland', Population: 100d),
    (City: 'BERLIN', Country: 'Germany', Population: 200d),
    (City: 'HAMBURG', Country: 'Germany', Population: 200d),
    (City: 'ROME', Country: 'Italy', Population: 400d)
} b)
   or a.Population > 400

-- Combined with AND
select a.City from values {
    (City: 'WARSAW', Country: 'Poland', Population: 100d),
    (City: 'BERLIN', Country: 'Germany', Population: 200d),
    (City: 'WARSAW', Country: 'Poland', Population: 200d),
    (City: 'MUNICH', Country: 'Germany', Population: 400d)
} a
where a.City in (select b.City from values {
    (City: 'WARSAW', Country: 'Poland', Population: 100d),
    (City: 'BERLIN', Country: 'Germany', Population: 200d),
    (City: 'HAMBURG', Country: 'Germany', Population: 200d),
    (City: 'ROME', Country: 'Italy', Population: 400d)
} b)
  and a.Country = 'Poland'
```

#### 7.9.6 Correlated IN Subqueries

An `IN` or `NOT IN` subquery may reference aliases from the containing query block:

```sql
select a.City
from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} a
where a.City in (
    select b.City
    from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'HAMBURG', Country: 'Germany'),
    (City: 'ROME', Country: 'Italy')
} b
    where b.Country = a.Country
)
```

Equality correlations are decorrelated to semi-join or anti-semi-join plans where possible, preserving one output row per matching left row. These internal plans share the same left-sided row and column semantics as direct `SEMI JOIN` and `ANTI JOIN` syntax. When an `IN` or `NOT IN` subquery appears inside an expression that produces a Boolean value, its result MUST preserve three-valued membership semantics, including null values and empty inputs. Otherwise valid correlations MUST be accepted even when no specialized join or range strategy preserves their meaning; the planner selects the general per-binding fallback in that case.

### 7.10 EXISTS / NOT EXISTS

`EXISTS` tests whether a subquery returns at least one row. `NOT EXISTS` is the negated form:

```sql
select a.City
from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} a
where exists (
    select b.City
    from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'HAMBURG', Country: 'Germany'),
    (City: 'ROME', Country: 'Italy')
} b
    where b.Country = a.Country
)

select a.City
from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} a
where not exists (
    select b.City
    from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'HAMBURG', Country: 'Germany'),
    (City: 'ROME', Country: 'Italy')
} b
    where b.Country = a.Country
)
```

The selected expressions inside an `EXISTS` subquery are ignored for the truth value. They MUST be parsed and bound, but MUST NOT be evaluated when their values are unnecessary for row presence. Values needed by `DISTINCT`, grouping, set operations, or other result shaping retain their normal semantics. Equality-correlated `EXISTS` and `NOT EXISTS` predicates are lowered to semi-join or anti-semi-join plans where possible.

Predicate subqueries (`IN`, `NOT IN`, `EXISTS`, `NOT EXISTS`, and equality `ANY`/`SOME` forms) may also appear in Boolean expression contexts such as `SELECT`/`CASE`, `JOIN ON`, `WHERE`, `HAVING`, `QUALIFY`, and `ORDER BY`. Top-level `WHERE` predicates use semi/anti-semi joins when possible. Expression-producing contexts use a specialized strategy when it preserves the required result, or the general per-binding fallback otherwise. In `HAVING`, predicate subqueries can be moved before grouping only when every outer reference used by the predicate is a grouping key; predicates depending on non-grouped row values raise MQ2024.

### 7.11 Scalar Subqueries

A parenthesized `SELECT` or `FROM`-first query can appear as a scalar expression:

```sql
select a.City,
       (select b.City
        from values { (City: 'WARSAW', Country: 'Poland'), (City: 'BERLIN', Country: 'Germany') } b
        where b.Country = a.Country) as MatchCity
from values { (City: 'KRAKOW', Country: 'Poland'), (City: 'MUNICH', Country: 'Germany') } a
```

Scalar subqueries are valid in expression contexts including `SELECT`, `WHERE`, `JOIN ON`, `GROUP BY`, `HAVING`, `WINDOW`, `QUALIFY`, `ORDER BY`, function arguments, and `CASE` expressions.

Rules:

- A scalar subquery MUST return exactly one column. Otherwise Musoq raises MQ2024.
- Scalar subqueries over `UNION`, `UNION ALL`, `EXCEPT`, and `INTERSECT` are supported. Correlated branches MAY use different outer references or correlation keys; this can require the general per-binding fallback.
- If the subquery returns zero rows, the scalar value is `NULL`.
- If the subquery returns more than one row, query execution raises
  `MQ7016_ScalarSubqueryCardinalityViolation`. This includes identical duplicate
  values; cardinality counts rows, not distinct values.
- Row cardinality is checked during execution after the subquery's filtering,
  DISTINCT, grouping, and paging. A statically known multirow VALUES source is
  not rejected during binding. `MQ3095_ScalarSubqueryCardinality` is retired;
  its numeric identifier remains reserved for compatibility.
- Uncorrelated scalar subqueries may use result-shaping clauses such as `DISTINCT`, `GROUP BY`, `ORDER BY`, `SKIP`, `TAKE`, `WINDOW`, and `QUALIFY`. Musoq materializes the shaped subquery result and then applies scalar cardinality rules to the materialized rows.
- Correlated scalar aggregates are optimized as grouped aggregate plus left join where possible. Correlated scalar subqueries with result-shaping clauses MUST use the general per-binding fallback when a specialized strategy cannot preserve their meaning. A scalar result with value type `T` has nullable type `T?`, including when the selected inner expression itself is non-nullable.

#### 7.11.1 General per-binding execution

Acceptance of a well-formed scalar, `EXISTS`, `IN`, or negated subquery MUST NOT depend on whether an optimizer can decorrelate it. The planner MUST retain a valid specialized strategy or select a typed per-binding fallback. Existing query inspection output identifies this strategy as `Apply` and includes the inner plan.

Outer references bind lexically to the nearest visible query scope. An unqualified column first resolves against the inner query; only a missing local column can resolve to an outer scope. Local ambiguity remains an error. Nested subqueries capture the current typed outer values, and references to an enclosing CTE consume that execution's stored rows rather than rebuilding the CTE for each outer row. Plain derived tables remain independent; explicit `CROSS APPLY` or `OUTER APPLY` is required for a correlated FROM source. Restrictions on recursive members remain in force.

The fallback evaluates the inner query for the current outer binding only when the enclosing expression is demanded. It MUST preserve the surrounding Boolean, `CASE`, and `??` guards, the clause's evaluation phase, cancellation, and disposal of inner enumerators. Filtering and result shaping run within that binding before scalar cardinality or membership is determined. Planning and binding still validate an inner query whose execution is skipped by a guard.

### 7.12 Quantified Subqueries: ANY, SOME, ALL

Comparison predicates may use `ANY`, `SOME`, or `ALL` with a subquery:

```sql
where a.Population > ANY (select b.Population from values {
    (Country: 'Poland', Population: 100d),
    (Country: 'Germany', Population: 200d),
    (Country: 'Germany', Population: 200d),
    (Country: 'Italy', Population: 400d)
} b)
where a.Population > ALL (select b.Population from values {
    (Country: 'Poland', Population: 100d),
    (Country: 'Germany', Population: 200d),
    (Country: 'Germany', Population: 200d),
    (Country: 'Italy', Population: 400d)
} b)
where a.Country = SOME (select b.Country from values {
    (Country: 'Poland', Population: 100d),
    (Country: 'Germany', Population: 200d),
    (Country: 'Germany', Population: 200d),
    (Country: 'Italy', Population: 400d)
} b)
```

`SOME` is a synonym for `ANY`. `= ANY` and `= SOME` have the same three-valued result as `IN`.

Rules:

- Quantified subqueries MUST return exactly one column. All comparison operators (`=`, `<>`, `<`, `<=`, `>`, `>=`) are supported.
- The left operand is evaluated once when the expression is demanded. The comparison is applied to each row of the completed inner result, including its filtering, grouping, `DISTINCT`, windows, ordering, `SKIP`, `TAKE`, and set operations.
- `ANY` / `SOME` returns `true` if any comparison is true; otherwise it returns `null` if any comparison is unknown; otherwise it returns `false`.
- `ALL` returns `false` if any comparison is false; otherwise it returns `null` if any comparison is unknown; otherwise it returns `true`.
- Empty inputs yield `false` for `ANY` / `SOME` and `true` for `ALL`, even when the left operand is null. A comparison with a null operand is unknown. Duplicates do not change these reductions.
- The expression has nullable Boolean type. Negation preserves unknown; only true passes a filtering clause. The same reduction applies in projections, `CASE`, joins, filters, `HAVING`, `QUALIFY`, and nested expressions.
- Valid bodies, including `UNION`, `UNION ALL`, `EXCEPT`, and `INTERSECT`, are accepted independently of optimizer capabilities. The planner retains specialized semi/anti-semi plans when they preserve the demanded result and evaluation boundaries. It uses the typed per-binding fallback otherwise, including expression contexts that must retain unknown results. The scope, guards, cancellation, and disposal rules in §7.11.1 also apply.

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
select case when Match('\d+', Name) then 'yes' else 'no' end from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows
```

---

## 8. JOIN Clause

Every JOIN family uses the stable-source rule from §6.2. The initial source
must have a natural or explicit alias before the first JOIN, and every right
operand must have a stable name before `ON` or the next source/clause boundary.
This applies to `INNER`, `LEFT`, `RIGHT`, `FULL`, `CROSS`, `SEMI`, `ANTI`, and
`ASOF` joins. A missing alias is a parse error (`MQ2035`), not a method-owner
binding error (`MQ3022`).

### 8.1 INNER JOIN

Returns only rows where the join condition matches in both tables:

```sql
select a.City, b.Population
from values {
    (City: 'WARSAW', Population: 100d),
    (City: 'BERLIN', Population: 200d),
    (City: 'WARSAW', Population: 200d),
    (City: 'MUNICH', Population: 400d)
} a
inner join values {
    (City: 'WARSAW', Population: 100d),
    (City: 'BERLIN', Population: 200d),
    (City: 'HAMBURG', Population: 200d),
    (City: 'ROME', Population: 400d)
} b on a.City = b.City
```

`JOIN` without the `INNER` keyword is equivalent to `INNER JOIN`:

Syntax fragment (not a complete executable batch):

```sql
from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} a join values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'HAMBURG'),
    (City: 'ROME')
} b on a.City = b.City
```

### 8.2 LEFT OUTER JOIN

Returns all rows from the left table. Unmatched right-side columns are `null`:

```sql
select a.City, b.Population
from values {
    (City: 'WARSAW', Population: 100d),
    (City: 'BERLIN', Population: 200d),
    (City: 'WARSAW', Population: 200d),
    (City: 'MUNICH', Population: 400d)
} a
left join values {
    (City: 'WARSAW', Population: 100d),
    (City: 'BERLIN', Population: 200d),
    (City: 'HAMBURG', Population: 200d),
    (City: 'ROME', Population: 400d)
} b on a.City = b.City
```

`LEFT OUTER JOIN` and `LEFT JOIN` are equivalent.

When a nullable column from the optional side is tested with `IS NULL`,
`MQ5018_AmbiguousOuterJoinNullCheck` explains that NULL can mean either a
missing row or a present row containing NULL. A `WHERE` predicate that is
provably false or UNKNOWN for the NULL-extended row produces
`MQ5019_NullRejectingOuterJoinFilter`. Put intended match restrictions in
`ON`, or use `alias IS PRESENT` / `alias IS MISSING` when row existence is
the actual condition.

### 8.3 RIGHT OUTER JOIN

Returns all rows from the right table. Unmatched left-side columns are `null`:

```sql
select a.City, b.Population
from values {
    (City: 'WARSAW', Population: 100d),
    (City: 'BERLIN', Population: 200d),
    (City: 'WARSAW', Population: 200d),
    (City: 'MUNICH', Population: 400d)
} a
right join values {
    (City: 'WARSAW', Population: 100d),
    (City: 'BERLIN', Population: 200d),
    (City: 'HAMBURG', Population: 200d),
    (City: 'ROME', Population: 400d)
} b on a.City = b.City
```

### 8.4 FULL OUTER JOIN

Returns all rows from both sides. Matched rows contain columns from both sources; left-only rows contain `null` for right-side columns; right-only rows contain `null` for left-side columns:

```sql
select a.City, b.Population
from values {
    (City: 'WARSAW', Population: 100d),
    (City: 'BERLIN', Population: 200d),
    (City: 'WARSAW', Population: 200d),
    (City: 'MUNICH', Population: 400d)
} a
full outer join values {
    (City: 'WARSAW', Population: 100d),
    (City: 'BERLIN', Population: 200d),
    (City: 'HAMBURG', Population: 200d),
    (City: 'ROME', Population: 400d)
} b on a.City = b.City
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
from values {
    (RelativePath: 'a.txt', Hash: 'new'),
    (RelativePath: 'b.txt', Hash: 'same'),
    (RelativePath: 'c.txt', Hash: 'third'),
    (RelativePath: 'd.txt', Hash: 'fourth')
} a
full outer join values {
    (RelativePath: 'a.txt', Hash: 'old'),
    (RelativePath: 'b.txt', Hash: 'same'),
    (RelativePath: 'c.txt', Hash: 'third'),
    (RelativePath: 'd.txt', Hash: 'fourth')
} b on a.RelativePath = b.RelativePath
```

Do not use `b.Id is null` to decide that `b` is absent: `b.Id` can be `null` on a real row. Use `b is missing` instead. Row presence predicates are rejected for aliases that are always present, such as single-source aliases, inner-join aliases, cross-join aliases, cross-apply aliases, or the preserved side of an outer join. They cannot classify the unmatched side inside the same join's `ON` predicate, because unmatched rows are produced after the join condition is evaluated.

Musoq v1 intentionally does not define a `COMPARE` statement, `DIFF` statement, `ChangeKind`, or `DiffKind` helper. Rowset comparison is expressed with `FULL OUTER JOIN`, alias presence predicates, and `CASE`.

### 8.5 SEMI JOIN

Returns each left-side row for which at least one right-side row satisfies the join condition. The result exposes only left-side columns, and duplicate matching rows on the right side do not duplicate the left row:

```sql
select a.City
from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} a
semi join values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'HAMBURG'),
    (City: 'ROME')
} b on a.City = b.City
```

`LEFT SEMI JOIN` is equivalent to `SEMI JOIN`:

```sql
select a.City
from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} a
left semi join values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'HAMBURG'),
    (City: 'ROME')
} b on a.City = b.City
```

`SEMI JOIN` requires an `ON` condition. Right-side columns are available inside the `ON` condition but are not part of the join output.

### 8.6 ANTI JOIN

Returns each left-side row for which no right-side row satisfies the join condition. The result exposes only left-side columns:

```sql
select a.City
from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} a
anti join values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'HAMBURG'),
    (City: 'ROME')
} b on a.City = b.City
```

`ANTI SEMI JOIN` and `LEFT ANTI SEMI JOIN` are equivalent to `ANTI JOIN`:

```sql
select a.City
from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} a
left anti semi join values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'HAMBURG'),
    (City: 'ROME')
} b on a.City = b.City
```

`ANTI JOIN` requires an `ON` condition. Right-side columns are available inside the `ON` condition but are not part of the join output.

### 8.7 CROSS JOIN

Produces the Cartesian product of the left and right sources:

```sql
select a.Name, b.Name
from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} a
cross join values {
    (Name: 'Eve'),
    (Name: 'Finn'),
    (Name: 'Gia'),
    (Name: 'Hugo')
} b
```

`CROSS JOIN` does not accept an `ON` condition. Use `WHERE` to filter the Cartesian product:

```sql
select a.Name, b.Name
from values {
    (Name: 'Ada', Country: 'Poland'),
    (Name: 'Bob', Country: 'Germany'),
    (Name: 'Cara', Country: 'Poland'),
    (Name: 'Dan', Country: 'Germany')
} a
cross join values {
    (Name: 'Eve', Country: 'Poland'),
    (Name: 'Finn', Country: 'Germany'),
    (Name: 'Gia', Country: 'Germany'),
    (Name: 'Hugo', Country: 'Italy')
} b
where a.Country = b.Country
```

Use `CROSS APPLY` or `OUTER APPLY` for lateral expansion where the right side must reference columns from the left side.

### 8.8 Multiple Joins

Chain multiple joins in a single query:

```sql
select a.City, b.Population, c.Area
from values {
    (City: 'WARSAW', Population: 100d, Area: 50d),
    (City: 'BERLIN', Population: 200d, Area: 150d),
    (City: 'WARSAW', Population: 200d, Area: 250d),
    (City: 'MUNICH', Population: 400d, Area: 350d)
} a
inner join values {
    (City: 'WARSAW', Population: 100d, Area: 50d),
    (City: 'BERLIN', Population: 200d, Area: 150d),
    (City: 'HAMBURG', Population: 200d, Area: 250d),
    (City: 'ROME', Population: 400d, Area: 350d)
} b on a.City = b.City
inner join values {
    (City: 'WARSAW', Population: 100d, Area: 50d),
    (City: 'BERLIN', Population: 200d, Area: 150d),
    (City: 'WARSAW', Population: 200d, Area: 250d),
    (City: 'MUNICH', Population: 400d, Area: 350d)
} c on a.City = c.City
```

### 8.9 Join Condition Expressions

Join conditions can contain expressions, not just simple column equality:

Syntax fragment (not a complete executable batch):

```sql
inner join values {
    (Id: 1, Population: 100d),
    (Id: 2, Population: 200d),
    (Id: 3, Population: 200d),
    (Id: 4, Population: 400d)
} b on a.Id = b.Id + 1
inner join values {
    (Id: 1, Population: 100d),
    (Id: 2, Population: 200d),
    (Id: 3, Population: 200d),
    (Id: 4, Population: 400d)
} b on a.Population > b.Population + 100
```

### 8.10 Function Calls in Multi-Source Queries

In queries with multiple data sources (joins, applies), each schema has its own method registry. When a function call is **unqualified** (no alias prefix), the engine uses the **method auto-resolution algorithm** described in §8.10.1 to determine which schema owns the method. When auto-resolution cannot determine a single owner, the caller must disambiguate by prefixing the function call with a table alias.

#### 8.10.1 Method Auto-Resolution Algorithm

When a function is called without an alias prefix in a multi-source query, the engine attempts to resolve the owning schema automatically. It does so by trying to bind the method against **every** schema in scope and then applying three rules in order:

1. **Common method rule.** If **all** candidate schemas resolve the method to the **same underlying implementation** (same function identity) **and the invocation is alias-independent**, the engine picks any one of them. Alias-independent methods do not receive a source row through an injected parameter. This is the typical case for shared utility methods (e.g., `ToDecimal`, `Concat`, `Contains`) that every schema inherits from a common library. A method that receives an injected source row is alias-dependent even when every candidate exposes the same implementation, because the selected alias changes the observable input and nullability of the call; such a call MUST be qualified when more than one alias can provide it.

2. **Unique method rule.** If **exactly one** candidate schema resolves the method successfully, the engine picks that schema. This applies to methods that are unique to a particular schema's library.

3. **Ambiguity error.** If **two or more** candidate schemas resolve the method to different implementations, or if the candidates resolve an alias-dependent (source-injected) method, the engine cannot choose and raises diagnostic **MQ3035** (`AmbiguousMethodOwner`). The caller must add an alias prefix to disambiguate.

> **Aggregate methods** (those receiving group context) follow a separate but analogous auto-resolution path that was already present before this algorithm was introduced. The rules above extend the same principle to all non-aggregate methods.

#### 8.10.2 Method Categories

Understanding how methods are classified helps predict when auto-resolution succeeds or fails:

| Category | Characteristic | Auto-Resolution |
|----------|---------------|-----------------|
| **Aggregate** | Receives group context (e.g., `Sum`, `Count`, `Avg`) | Resolved via dedicated aggregate inference; same common-method logic applies |
| **Pure utility** | No source or group context; stateless (e.g., `ToDecimal`, `Concat`, `Abs`) | Almost always shared across schemas → Rule 1 resolves them |
| **Entity-bound** | Source-bound; receives the selected source row | A method available through exactly one alias uses Rule 2; multiple eligible aliases require qualification and otherwise report MQ3035, even when they expose the same implementation |

#### 8.10.3 Examples

**Auto-resolved — common method (Rule 1):**

`ToDecimal` is inherited by every schema from the shared plugin library. Both schemas resolve it to the same implementation, so no alias is needed:

```sql
select e.Department, Sum(t.Amount) as total_amount
from values { (Id: 1, Department: 'Engineering'), (Id: 2, Department: 'Support') } e
inner join values { (EmployeeId: 1, Amount: 120d), (EmployeeId: 1, Amount: 80d), (EmployeeId: 2, Amount: 50d) } t
    on e.Id = t.EmployeeId
group by e.Department
```

**Auto-resolved — unique method (Rule 2):**

If `SpecialParse` exists only in schema A's library but not in schema B's, the engine resolves it to A automatically:

```sql
select SpecialParse(a.RawData), b.Name
from schema.records() a
inner join schema.byCountry() b on a.Id = b.Id
```

**Ambiguous — explicit alias required (Rule 3):**

If schemas A and B each define their own `Transform` with different implementations, the engine raises MQ3035. The same diagnostic applies when both aliases expose one shared source-injected implementation: the injected row still makes the owner observable. The caller must specify which schema's `Transform` to use:

```sql
-- ERROR MQ3035: Transform resolves to different implementations in A and B
select Transform(a.Value)
from schema.records() a
inner join schema.byCountry() b on a.Id = b.Id

-- FIX: prefix with the desired schema alias
select a.Transform(a.Value)
from schema.records() a
inner join schema.byCountry() b on a.Id = b.Id
```

#### 8.10.4 Explicit Alias Prefix

Even when auto-resolution would succeed, you can always qualify a function call with an alias prefix. The alias selects the **schema library owner** for that method call. It does **not** require that every argument come from the same alias. For example, this is valid because the aggregate implementation is taken from `countries`, while the value being aggregated comes from `population`:

```sql
select cities.Country, countries.Sum(population.Population)
from values {
    (City: 'WARSAW', Country: 'Poland', Population: 100d),
    (City: 'BERLIN', Country: 'Germany', Population: 200d),
    (City: 'WARSAW', Country: 'Poland', Population: 200d),
    (City: 'MUNICH', Country: 'Germany', Population: 400d)
} countries
inner join values {
    (City: 'WARSAW', Country: 'Poland', Population: 100d),
    (City: 'BERLIN', Country: 'Germany', Population: 200d),
    (City: 'HAMBURG', Country: 'Germany', Population: 200d),
    (City: 'ROME', Country: 'Italy', Population: 400d)
} cities on countries.Country = cities.Country
inner join values {
    (City: 'WARSAW', Country: 'Poland', Population: 100d),
    (City: 'BERLIN', Country: 'Germany', Population: 200d),
    (City: 'WARSAW', Country: 'Poland', Population: 200d),
    (City: 'MUNICH', Country: 'Germany', Population: 400d)
} population on cities.City = population.City
group by cities.Country
```

This applies to all functions, including aggregation:

```sql
-- Explicit alias on aggregate
select a.Count(a.City)
from values {
    (Id: 1, City: 'WARSAW'),
    (Id: 2, City: 'BERLIN'),
    (Id: 3, City: 'WARSAW'),
    (Id: 4, City: 'MUNICH')
} a
inner join values {
    (Id: 1, City: 'WARSAW'),
    (Id: 2, City: 'BERLIN'),
    (Id: 3, City: 'HAMBURG'),
    (Id: 4, City: 'ROME')
} b on a.Id = b.Id
group by a.City

-- Auto-resolved alias on aggregate (Count is shared across schemas)
select Count(a.City)
from values {
    (Id: 1, City: 'WARSAW'),
    (Id: 2, City: 'BERLIN'),
    (Id: 3, City: 'WARSAW'),
    (Id: 4, City: 'MUNICH')
} a
inner join values {
    (Id: 1, City: 'WARSAW'),
    (Id: 2, City: 'BERLIN'),
    (Id: 3, City: 'HAMBURG'),
    (Id: 4, City: 'ROME')
} b on a.Id = b.Id
group by a.City
```

#### 8.10.5 Best Practices

When the same aggregate expression is projected with `AS`, prefer the projected alias in `ORDER BY` instead of repeating the aggregate call. This avoids repeating method binding and reads more naturally:

```sql
select
    e.Department as department,
    Sum(t.Amount) as total_amount
from values { (Id: 1, Department: 'Engineering'), (Id: 2, Department: 'Support') } e
inner join values { (EmployeeId: 1, Amount: 120d), (EmployeeId: 1, Amount: 80d), (EmployeeId: 2, Amount: 50d) } t
    on e.Id = t.EmployeeId
group by e.Department
order by total_amount desc
```

For complex analytics that combine JOINs with GROUP BY, use a CTE to flatten the join first, then aggregate on the single-source CTE:

```sql
with joined as (
    select a.City as City, b.Population as Population
    from values {
    (City: 'WARSAW', Population: 100d),
    (City: 'BERLIN', Population: 200d),
    (City: 'WARSAW', Population: 200d),
    (City: 'MUNICH', Population: 400d)
} a
    inner join values {
    (City: 'WARSAW', Population: 100d),
    (City: 'BERLIN', Population: 200d),
    (City: 'HAMBURG', Population: 200d),
    (City: 'ROME', Population: 400d)
} b on a.City = b.City
)
select j.City, Sum(j.Population) as TotalPop
from joined j
group by j.City
```

> **Note:** In single-table queries, function calls never need an alias prefix.

### 8.11 ASOF JOIN

ASOF JOIN matches each left-side row to the **single nearest** right-side row based on an ordered (inequality) column. Unlike a regular inequality join — which returns all matching rows — ASOF JOIN returns **at most one right-side row per left-side row**.

#### 8.11.1 Syntax

Syntax fragment (not a complete executable batch):

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
from values {
    (Name: 'Ada', Population: 100d),
    (Name: 'Bob', Population: 200d),
    (Name: 'Cara', Population: 200d),
    (Name: 'Dan', Population: 400d)
} a
asof join values {
    (Name: 'Eve', Population: 100d),
    (Name: 'Finn', Population: 200d),
    (Name: 'Gia', Population: 200d),
    (Name: 'Hugo', Population: 400d)
} b on a.Population >= b.Population
```

For each left-side row, returns the single right-side row whose `Population` is the largest value that is less than or equal to the left-side `Population`.

**With equality partition — match within groups:**

```sql
param(errorTime: datetime = '2024-01-02T12:00:00', commitTime: datetime = '2024-01-02T11:00:00')
with errors as (
    select ErrorTime, Service, Message
    from values { (ErrorTime: $errorTime, Service: 'api', Message: 'timeout') } inputErrors
), commits as (
    select AuthorDate, Project, Sha, Message
    from values { (AuthorDate: $commitTime, Project: 'api', Sha: 'abc123', Message: 'fix') } inputCommits
)
select e.ErrorTime, e.Service, c.Sha
from errors e
asof join commits c on e.Service = c.Project and e.ErrorTime >= c.AuthorDate
```

For each error, finds the most recent commit **within the same project/service**.

**Forward lookup — find what happened next:**

```sql
param(errorTime: datetime = '2024-01-02T12:00:00', commitTime: datetime = '2024-01-02T11:00:00')
with errors as (
    select ErrorTime, Service, Message
    from values { (ErrorTime: $errorTime, Service: 'api', Message: 'timeout') } inputErrors
), commits as (
    select AuthorDate, Project, Sha, Message
    from values { (AuthorDate: $commitTime, Project: 'api', Sha: 'abc123', Message: 'fix') } inputCommits
)
select c.AuthorDate, c.Sha, e.ErrorTime, e.Message
from commits c
asof join errors e on c.AuthorDate <= e.ErrorTime
```

For each commit, finds the first error that occurred at or after it.

#### 8.11.4 ASOF LEFT JOIN

When no match exists on the right side, `ASOF JOIN` drops the left row (like `INNER JOIN`). To preserve all left rows with NULLs on the right:

```sql
param(errorTime: datetime = '2024-01-02T12:00:00', commitTime: datetime = '2024-01-02T11:00:00')
with errors as (
    select ErrorTime, Service, Message
    from values { (ErrorTime: $errorTime, Service: 'api', Message: 'timeout') } inputErrors
), commits as (
    select AuthorDate, Project, Sha, Message
    from values { (AuthorDate: $commitTime, Project: 'api', Sha: 'abc123', Message: 'fix') } inputCommits
)
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
from values {
    (Name: 'Ada', Population: 100d, UpdatedAt: 1),
    (Name: 'Bob', Population: 200d, UpdatedAt: 2),
    (Name: 'Cara', Population: 200d, UpdatedAt: 3),
    (Name: 'Dan', Population: 400d, UpdatedAt: 4)
} a
asof join values {
    (Name: 'Eve', Population: 100d, UpdatedAt: 1),
    (Name: 'Finn', Population: 200d, UpdatedAt: 2),
    (Name: 'Gia', Population: 200d, UpdatedAt: 3),
    (Name: 'Hugo', Population: 400d, UpdatedAt: 4)
} b on a.Population >= b.Population
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
5. **The inequality column type MUST be orderable** (have a declared total-order contract). Non-orderable types produce error `AsOfJoinInequalityColumnNotOrderable`.
6. **The tie-break expression MUST reference the right side only** and MUST have an orderable type.

See [§23](#23-diagnostic-contract-and-catalog) for the full diagnostic catalog.

#### 8.11.9 Type Requirements

The inequality column MUST be of a comparable, orderable type. Supported types include:

- All numeric types (`int`, `long`, `decimal`, `double`, `float`, etc.)
- `DateTime`, `DateTimeOffset`, `TimeSpan`
- `string` (lexicographic ordering)
- Provider extension types with a declared total-order contract

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
from values {
    (Name: 'Ada', Population: 100d, Area: 50d),
    (Name: 'Bob', Population: 200d, Area: 150d),
    (Name: 'Cara', Population: 200d, Area: 250d),
    (Name: 'Dan', Population: 400d, Area: 350d)
} a
asof join values {
    (Name: 'Eve', Population: 100d, Area: 50d),
    (Name: 'Finn', Population: 200d, Area: 150d),
    (Name: 'Gia', Population: 200d, Area: 250d),
    (Name: 'Hugo', Population: 400d, Area: 350d)
} b on a.Population >= b.Population
asof join values {
    (Name: 'Ada', Population: 100d, Area: 50d),
    (Name: 'Bob', Population: 200d, Area: 150d),
    (Name: 'Cara', Population: 200d, Area: 250d),
    (Name: 'Dan', Population: 400d, Area: 350d)
} c on a.Population >= c.Area
```

Each ASOF JOIN independently selects the nearest candidate under its own keys. With no tie-break, equal candidates retain source encounter order; reversing a lookup direction does not select a different duplicate representative.

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

`CROSS APPLY` and `OUTER APPLY` also require a stable name for both the left
source and the APPLY result. This requirement is independent for each nested
query block and covers schema calls, functions, row methods, properties,
derived tables, and `VALUES` sources. `WITH ORDINALITY` follows the alias, so a
missing alias is diagnosed before `WITH ORDINALITY` is consumed.

### 9.1 CROSS APPLY

When the right side is a schema datasource, its named arguments are bound
before APPLY planning and are evaluated once per left row when they reference
left-side columns. The canonical parameter order is preserved for every row,
including `NULL` values and declared defaults. A row access method such as
`alias.Split(...)` is a different context and remains positional-only.

`CROSS APPLY` is a correlated join where the right side can reference columns from the left side. Only rows with matches are returned:

```sql
select a.Country, b.City
from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} a
cross apply schema.byCountry(a.Country) b
```

The alias is mandatory when the APPLY source is a property or method result:

```sql
select a1.Name, item.Value
from schema.collections() a1
cross apply a1.Column item
take 10
```

The following is invalid because `TAKE` is a query boundary, not an alias:

```sql
select a1.Name
from schema.collections() a1
cross apply a1.Column
take 10
-- MQ2035_MissingRequiredAlias: The CROSS APPLY source requires an alias before TAKE.
```

The right-side data source receives values from the left side as method arguments.

### 9.2 OUTER APPLY

Like `CROSS APPLY`, but preserves left-side rows without matches. Unmatched right-side columns are `null`:

```sql
select a.Country, b.City
from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} a
outer apply schema.byCountry(a.Country) b
-- If no match for a.Country, b.City is null
```

Value-type columns from the right side are automatically promoted to nullable (e.g., `decimal` → `decimal?`).

### 9.3 Apply with Row Methods

Call a method on a row that produces a table of results:

```sql
-- Split a string into rows
select b.Value
from schema.texts() a
cross apply a.Split(a.Text, ' ') as b

-- Chain method results
select c.Value
from schema.texts() a
cross apply a.Split(a.Text, ' ') as b
cross apply b.ToCharArray(b.Value) as c
```

Nested method calls are supported:

```sql
-- Skip first element, take next 6
select b.Value
from schema.texts() a
cross apply a.Take(a.Skip(a.Split(a.Text, ' '), 1), 6) as b
```

### 9.4 Apply with Collection Properties

Expand a collection property (array, list) into rows:

```sql
-- Expand an array property
select a.City, b.Value
from schema.values() a
cross apply a.Values as b

-- Expand nested collection
select d.Value
from schema.nestedValues() a
cross apply a.Values as b
cross apply b.Values as c
cross apply c.Values as d

-- Expand through property chain
select b.Value
from schema.values() a
cross apply a.ComplexType.PrimitiveValues as b
```

When expanding **primitive arrays** (`int[]`, `double[]`), each element becomes a row with a `Value` column.

When expanding **complex type arrays** (collections of records), each property of the complex type is exposed as a column.

### 9.5 APPLY WITH ORDINALITY

`WITH ORDINALITY` exposes a zero-based `Ordinal` column on the right-side APPLY alias:

```sql
select a.City, b.Value, b.Ordinal
from schema.values() a
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
from schema.texts() a
cross apply a.Split(a.Text, ' ') as b
cross apply b.ToCharArray(b.Value) as c

-- Apply results can be filtered and grouped
select b.Length(b.Value), b.Count(Length(b.Value))
from schema.texts() a
cross apply a.Split(a.Text, ' ') as b
group by b.Length(b.Value)
```

### 9.7 APPLY with Derived Tables

`CROSS APPLY` and `OUTER APPLY` may use a parenthesized query as the right side. Unlike plain `FROM` or `JOIN` derived tables, an APPLY derived table may reference aliases from the left side:

```sql
select a.City, d.City
from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} a
cross apply (
    select b.City, b.Country
    from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'HAMBURG', Country: 'Germany'),
    (City: 'ROME', Country: 'Italy')
} b
    where b.Country = a.Country
) d

select a.City, d.City
from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} a
outer apply (
    select b.City, b.Country
    from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'HAMBURG', Country: 'Germany'),
    (City: 'ROME', Country: 'Italy')
} b
    where b.Country = a.Country
) d
```

`CROSS APPLY` returns only left rows with at least one right-side match. `OUTER APPLY` preserves unmatched left rows and fills right-side columns with `NULL`.

For correlated APPLY derived tables, Musoq requires the local columns used to decorrelate the right side to be visible in the derived-table output. If the query hides a required local correlation column, Musoq rejects the query with MQ2024 instead of exporting hidden columns.

An APPLY derived-table body may contain set operators when every set branch exposes the same projected correlation key. Musoq decorrelates each branch independently and joins the combined set result back to the left side by that shared key:

```sql
select a.City, d.City
from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} a
cross apply (
    select b.City, b.Country
    from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'HAMBURG', Country: 'Germany'),
    (City: 'ROME', Country: 'Italy')
} b
    where b.Country = a.Country
    union (City, Country)
    select c.City, c.Country
    from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} c
    where c.Country = a.Country
) d
```

If any branch hides the required correlation key, or branches expose incompatible correlation keys, Musoq raises MQ2024.

### 9.8 Multiple Independent Applies

When multiple applies reference the same source, they produce a Cartesian product:

```sql
select b.Value, c.Value
from schema.texts() a
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
desc functions schema.records()
```

All numeric aggregation functions accept any numeric type (`byte`, `short`, `int`, `long`, `float`, `double`, `decimal`, etc.) as input. Different data sources may provide additional aggregate functions beyond the common set.

Sum, Min and Max return the nullable input numeric type. Avg returns decimal? for integral inputs, otherwise the nullable decimal/float/double input type. Counts return long. Numeric nulls are ignored; empty/all-null input returns null for non-count functions and zero for counts. Integral and decimal arithmetic is checked. These rules also apply to existing distinct variants and aggregate windows. See §18.5 for the required function surface.

The built-in `Min(column)` and `Max(column)` also accept strings. String extrema use case-sensitive ordinal comparison of UTF-16 code units, independent of the current culture. They ignore null inputs and return null for empty or all-null input; the empty string is a valid non-null value. Their result type is string. This comparison is not a configurable SQL collation.

Temporal extrema use separate names: `MinDateTime`, `MaxDateTime`, `MinDateTimeOffset`, `MaxDateTimeOffset`, `MinTimeSpan`, and `MaxTimeSpan`. Provider-specific libraries may expose additional overloads; use `DESC FUNCTIONS` to inspect the available signatures.

In a query with only one source, aggregates can be written without a source alias:

```sql
select Country, Sum(Population) from values {
    (Country: 'Poland', Population: 100d),
    (Country: 'Germany', Population: 200d),
    (Country: 'Poland', Population: 200d),
    (Country: 'Germany', Population: 400d)
} rows group by Country
```

In a query with multiple sources, unqualified aggregate calls are resolved with the same method-owner auto-resolution rules described in §8.10.1. If exactly one alias can provide the aggregate implementation, the unqualified call is accepted:

```sql
select e.Department, Sum(t.Amount)
from values { (Id: 1, Department: 'Engineering'), (Id: 2, Department: 'Support') } e
inner join values { (EmployeeId: 1, Amount: 120d), (EmployeeId: 1, Amount: 80d), (EmployeeId: 2, Amount: 50d) } t
    on e.Id = t.EmployeeId
group by e.Department
```

You may still qualify an aggregate explicitly to choose the owner:

```sql
select e.Department, t.Sum(t.Amount)
from values { (Id: 1, Department: 'Engineering'), (Id: 2, Department: 'Support') } e
inner join values { (EmployeeId: 1, Amount: 120d), (EmployeeId: 1, Amount: 80d), (EmployeeId: 2, Amount: 50d) } t
    on e.Id = t.EmployeeId
group by e.Department
```

The aggregate owner alias and the argument source do not have to match. The alias determines which schema library resolves `Sum`, `Count`, and similar methods.

If multiple aliases can resolve the same unqualified aggregate and they resolve to different implementations, the query is ambiguous and Musoq reports the candidate aliases. In that case, qualify the aggregate explicitly:

```sql
-- ERROR: aggregate owner is ambiguous across aliases
select AggregateMethodB()
from schema.records() first
inner join schema.otherRecords() second on 1 = 1

-- CORRECT:
select first.AggregateMethodB()
```

### 10.3 GROUP BY Examples

```sql
param(committedWhen: datetimeoffset = '2024-01-02T03:04:05+00:00')
-- Basic grouping with count
select Country, Count(Country) from values {
    (Name: 'Ada', City: 'WARSAW', Country: 'Poland', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Country: 'Germany', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Country: 'Poland', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Country: 'Germany', Population: 400d)
} rows group by Country

-- Grouping with sum
select Country, Sum(Population) from values {
    (Name: 'Ada', City: 'WARSAW', Country: 'Poland', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Country: 'Germany', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Country: 'Poland', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Country: 'Germany', Population: 400d)
} rows group by Country

-- Multi-column grouping
select Country, City, Count(City) from values {
    (Name: 'Ada', City: 'WARSAW', Country: 'Poland', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Country: 'Germany', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Country: 'Poland', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Country: 'Germany', Population: 400d)
} rows group by Country, City

-- Grouping with expression
select Substring(Name, 0, 2), Count(Name)
from values {
    (Name: 'Ada', City: 'WARSAW', Country: 'Poland', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Country: 'Germany', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Country: 'Poland', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Country: 'Germany', Population: 400d)
} rows
group by Substring(Name, 0, 2)

-- CASE WHEN in GROUP BY
select case when Population >= 500 then 'big' else 'small' end, Count(City)
from values {
    (Name: 'Ada', City: 'WARSAW', Country: 'Poland', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Country: 'Germany', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Country: 'Poland', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Country: 'Germany', Population: 400d)
} rows
group by case when Population >= 500 then 'big' else 'small' end

-- Function-of-column expression in SELECT and GROUP BY (supported)
select ToString(CommittedWhen, 'yyyy-MM', '') as MonthBucket, Count(Sha)
from values { (Sha: 'abc123', CommittedWhen: $committedWhen) } commits
group by ToString(CommittedWhen, 'yyyy-MM', '')

-- Positional grouping by the first final SELECT expression
select City, Count(*) from values {
    (Name: 'Ada', City: 'WARSAW', Country: 'Poland', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Country: 'Germany', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Country: 'Poland', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Country: 'Germany', Population: 400d)
} rows group by 1

-- Grouping by a non-aggregate SELECT alias
select City as c, Count(*) from values {
    (Name: 'Ada', City: 'WARSAW', Country: 'Poland', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Country: 'Germany', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Country: 'Poland', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Country: 'Germany', Population: 400d)
} rows group by c

-- HAVING can reference aggregate aliases
select Count(*) as cnt from values {
    (Name: 'Ada', City: 'WARSAW', Country: 'Poland', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Country: 'Germany', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Country: 'Poland', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Country: 'Germany', Population: 400d)
} rows group by City having cnt > 1
```

### 10.3.1 GROUP BY Positional References

A direct positive integer literal in `GROUP BY` is a SELECT-list ordinal, counted from 1 after final SELECT projection and star expansion:

```sql
select City, Count(*)
from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} rows
group by 1
```

This groups by the first final SELECT expression (`City`). Multiple ordinals may be used:

```sql
select City, Country, Count(*)
from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} rows
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
from values {
    (Name: 'Ada', City: 'WARSAW', Country: 'Poland'),
    (Name: 'Bob', City: 'BERLIN', Country: 'Germany'),
    (Name: 'Cara', City: 'WARSAW', Country: 'Poland'),
    (Name: 'Dan', City: 'MUNICH', Country: 'Germany')
} rows
group by all
```

This is equivalent to:

```sql
select City, Country, Count(Name)
from values {
    (Name: 'Ada', City: 'WARSAW', Country: 'Poland'),
    (Name: 'Bob', City: 'BERLIN', Country: 'Germany'),
    (Name: 'Cara', City: 'WARSAW', Country: 'Poland'),
    (Name: 'Dan', City: 'MUNICH', Country: 'Germany')
} rows
group by City, Country
```

The expansion runs after projection shaping. Star projections, `alias.*`, `LIKE`, `EXCLUDE`, `REPLACE`, and `RENAME` contribute their final selected scalar expressions as grouping keys:

```sql
select * like 'C%', Count(Name) as Rows
from values { (Id: 1, Name: 'Ada', City: 'WARSAW', Country: 'Poland') } rows
group by all
```

Computed scalar expressions are also grouped automatically:

```sql
select ToLower(City) as CityKey, Sum(Population) as TotalPopulation
from values {
    (City: 'WARSAW', Population: 100d),
    (City: 'BERLIN', Population: 200d),
    (City: 'WARSAW', Population: 200d),
    (City: 'MUNICH', Population: 400d)
} rows
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
from values {
    (City: 'WARSAW', Money: 100d, Month: 'January'),
    (City: 'BERLIN', Money: 200d, Month: 'January'),
    (City: 'WARSAW', Money: 300d, Month: 'February'),
    (City: 'MUNICH', Money: 400d, Month: 'February')
} rows
group by Month, City
```

With `group by Month, City`:
- `Count(City)` or `Count(City, 0)` — count at the City (innermost) level
- `Count(City, 1)` — count at the Month (parent) level

### 10.5 Aggregation Without GROUP BY

Using aggregate functions without GROUP BY produces a single-row result aggregating all rows:

```sql
select Count(Name), Sum(Population) from values {
    (Name: 'Ada', Population: 100d),
    (Name: 'Bob', Population: 200d),
    (Name: 'Cara', Population: 200d),
    (Name: 'Dan', Population: 400d)
} rows
-- Returns one row with totals
```

### 10.6 GROUP BY with Constant

Grouping by a non-ordinal constant treats all rows as a single group:

```sql
select Count(Country) from values {
    (Country: 'Poland'),
    (Country: 'Germany'),
    (Country: 'Poland'),
    (Country: 'Germany')
} rows group by 'fake'
-- Equivalent to aggregate without GROUP BY
```

Direct positive integer literals are positional references, not constants. Use a non-integer constant or a non-direct expression when a single constant group is intended.

### 10.7 HAVING Clause

HAVING filters groups after aggregation:

```sql
select Name, Count(Name) from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Ada'),
    (Name: 'Dan')
} rows
group by Name
having Count(Name) >= 2
```

HAVING can use any aggregate expression:

```sql
select City, Sum(Money) from values {
    (City: 'WARSAW', Money: 100d),
    (City: 'BERLIN', Money: 200d),
    (City: 'WARSAW', Money: 300d),
    (City: 'MUNICH', Money: 400d)
} rows
group by City
having Sum(Money) >= 400
```

HAVING can also use aggregate SELECT aliases and grouped non-aggregate aliases:

```sql
select City as c, Count(*) as cnt
from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} rows
group by c
having cnt > 1 and c is not null
```

### 10.8 Non-Aggregated Column Restrictions

In a SELECT with GROUP BY, every non-aggregated column MUST appear in the GROUP BY clause:

```sql
-- VALID: Name is in GROUP BY
select Name, Count(Name) from values {
    (Name: 'Ada', City: 'WARSAW', Country: 'Poland'),
    (Name: 'Bob', City: 'BERLIN', Country: 'Germany'),
    (Name: 'Cara', City: 'WARSAW', Country: 'Poland'),
    (Name: 'Dan', City: 'MUNICH', Country: 'Germany')
} rows group by Name

-- VALID: Only aggregates, no non-aggregated columns
select Count(Country) from values {
    (Name: 'Ada', City: 'WARSAW', Country: 'Poland'),
    (Name: 'Bob', City: 'BERLIN', Country: 'Germany'),
    (Name: 'Cara', City: 'WARSAW', Country: 'Poland'),
    (Name: 'Dan', City: 'MUNICH', Country: 'Germany')
} rows group by Country

-- ERROR: Name is not in GROUP BY
select Name, City, Count(1) from values {
    (Name: 'Ada', City: 'WARSAW', Country: 'Poland'),
    (Name: 'Bob', City: 'BERLIN', Country: 'Germany'),
    (Name: 'Cara', City: 'WARSAW', Country: 'Poland'),
    (Name: 'Dan', City: 'MUNICH', Country: 'Germany')
} rows group by City
-- Reports a nonaggregated-column diagnostic

-- ERROR: SELECT * with explicit GROUP BY expands to non-aggregated columns
select * from values {
    (Name: 'Ada', City: 'WARSAW', Country: 'Poland'),
    (Name: 'Bob', City: 'BERLIN', Country: 'Germany'),
    (Name: 'Cara', City: 'WARSAW', Country: 'Poland'),
    (Name: 'Dan', City: 'MUNICH', Country: 'Germany')
} rows group by City
-- Reports a nonaggregated-column diagnostic

-- VALID: GROUP BY ALL infers every expanded SELECT * column as a grouping key
select * from values {
    (Name: 'Ada', City: 'WARSAW', Country: 'Poland'),
    (Name: 'Bob', City: 'BERLIN', Country: 'Germany'),
    (Name: 'Cara', City: 'WARSAW', Country: 'Poland'),
    (Name: 'Dan', City: 'MUNICH', Country: 'Germany')
} rows group by all
```

### 10.9 NULL in GROUP BY

`NULL` values form their own group:

```sql
select Country, Count(Country) from values { (Country: 'Poland'), (Country: null), (Country: null) } countries group by Country
-- The NULL group has Count(Country) = 0.
```

Multi-column grouping treats `(POLAND, null)` as distinct from `(POLAND, WARSAW)`:

```sql
select Country, City, Count(City) from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} rows group by Country, City
```

### 10.10 FILTER Clause on Aggregates

The `FILTER` clause restricts which rows an aggregate function considers, providing a cleaner alternative to `CASE WHEN` inside the aggregate. `FILTER` is a **context-sensitive keyword** — it is only treated as a keyword immediately after a function call's closing parenthesis.

#### 10.10.1 Syntax

```sql
aggregate_function(expression) FILTER (WHERE condition)
```

The `FILTER` clause applies a row-level predicate *before* the aggregate processes the row. Rows for which the condition evaluates to `false` or `NULL` are excluded from that particular aggregate computation.

#### 10.10.2 Equivalence to CASE WHEN

Row exclusion is the normative definition of `FILTER`, independently of any
implementation strategy. For a null-ignoring aggregate such as `Count`, the
following two queries are semantically equivalent:

```sql
-- FILTER syntax (preferred)
select Count(City) filter (where Population > 200) from values {
    (City: 'WARSAW', Population: 100d),
    (City: 'BERLIN', Population: 200d),
    (City: 'WARSAW', Population: 200d),
    (City: 'MUNICH', Population: 400d)
} rows

-- Equivalent CASE WHEN
select Count(case when Population > 200 then City else null end) from values {
    (City: 'WARSAW', Population: 100d),
    (City: 'BERLIN', Population: 200d),
    (City: 'WARSAW', Population: 200d),
    (City: 'MUNICH', Population: 400d)
} rows
```

This equivalence does not hold for aggregates that observe null inputs or count
updates. For example, `AggregateValues` converts null inputs to empty strings;
substituting null would add an unwanted value. An excluded row MUST NOT evaluate
the aggregate's arguments or invoke its update operation. The filter predicate
is evaluated before those arguments, and only `true` admits the row.

#### 10.10.3 Supported Functions

The `FILTER` clause can be applied to **any aggregate function**: `Count`, `Sum`, `Min`, `Max`, `Avg`, `AggregateValues`, and any plugin-provided aggregate.

Applying `FILTER` to a non-aggregate function is a compile-time error (MQ3051).

#### 10.10.4 Examples

**Basic filtered count:**

```sql
select Count(City) filter (where Population > 200) from values {
    (City: 'WARSAW', Population: 100d),
    (City: 'BERLIN', Population: 200d),
    (City: 'WARSAW', Population: 200d),
    (City: 'MUNICH', Population: 400d)
} rows
```

**Multiple aggregates with different filters:**

```sql
select
    Count(City) filter (where Country = 'Poland') as PolishCities,
    Count(City) filter (where Country = 'Germany') as GermanCities
from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} rows
```

**FILTER with GROUP BY:**

```sql
select Country,
       Count(City) filter (where Population > 200) as LargeCities
from values {
    (City: 'WARSAW', Country: 'Poland', Population: 100d),
    (City: 'BERLIN', Country: 'Germany', Population: 200d),
    (City: 'WARSAW', Country: 'Poland', Population: 200d),
    (City: 'MUNICH', Country: 'Germany', Population: 400d)
} rows
group by Country
```

**FILTER in HAVING:**

```sql
select Country,
       Count(City) filter (where Population > 200) as LargeCities
from values {
    (City: 'WARSAW', Country: 'Poland', Population: 300d),
    (City: 'BERLIN', Country: 'Germany', Population: 200d),
    (City: 'WARSAW', Country: 'Poland', Population: 600d),
    (City: 'MUNICH', Country: 'Germany', Population: 400d)
} rows
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
pivot values { (Quarter: 'Q1', Amount: 10d, CustomerId: 1, Region: 'North'), (Quarter: 'Q2', Amount: 20d, CustomerId: 2, Region: 'North'), (Quarter: null, Amount: 5d, CustomerId: 1, Region: 'South') } sales
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
pivot values { (Quarter: 'Q1', Amount: 10d, CustomerId: 1, Region: 'North'), (Quarter: 'Q2', Amount: 20d, CustomerId: 2, Region: 'North'), (Quarter: null, Amount: 5d, CustomerId: 1, Region: 'South') } sales
on Quarter in ('Q1' as Q1)
using Sum(Amount) as Sales
group by Region
-- Output: Region, Q1
```

If there is more than one measure, Musoq uses `{pivot_alias}_{measure_alias}`:

```sql
pivot values { (Quarter: 'Q1', Amount: 10d, CustomerId: 1, Region: 'North'), (Quarter: 'Q2', Amount: 20d, CustomerId: 2, Region: 'North'), (Quarter: null, Amount: 5d, CustomerId: 1, Region: 'South') } sales
on Quarter in ('Q1' as Q1)
using Sum(Amount) as Sales, Count(*) as Orders
group by Region
-- Output: Region, Q1_Sales, Q1_Orders
```

Generated output names MUST be unique. If aliases would produce duplicate names, compilation fails with MQ2008.

#### 10.11.3 Measures

`USING` accepts aggregate function calls only, including `DISTINCT` aggregate calls:

```sql
pivot values { (Quarter: 'Q1', Amount: 10d, CustomerId: 1, Region: 'North'), (Quarter: 'Q2', Amount: 20d, CustomerId: 2, Region: 'North'), (Quarter: null, Amount: 5d, CustomerId: 1, Region: 'South') } sales
on Quarter in ('Q1' as Q1)
using Count(distinct CustomerId) as Customers
group by Region
```

Non-aggregate scalar functions in `USING` are rejected.

#### 10.11.4 No GROUP BY

If `GROUP BY` is omitted, all input rows form a single aggregate group:

```sql
pivot values { (Quarter: 'Q1', Amount: 10d, CustomerId: 1, Region: 'North'), (Quarter: 'Q2', Amount: 20d, CustomerId: 2, Region: 'North'), (Quarter: null, Amount: 5d, CustomerId: 1, Region: 'South') } sales
on Quarter in ('Q1' as Q1, 'Q2' as Q2)
using Count(*) as Orders
```

The result has one row.

#### 10.11.5 Multi-Column Pivot Keys

Multiple `ON` expressions are matched with tuple values in the `IN (...)` list:

```sql
pivot values { (Year: 2000, Country: 'NL', Population: 100, Name: 'City'), (Year: 2020, Country: 'PL', Population: 200, Name: 'City') } cities
on Year, Country in ((2000, 'NL') as y2000_nl, (2020, 'PL') as y2020_pl)
using Sum(Population) as Total
group by Name
```

Every tuple MUST contain the same number of values as the `ON` key list. A mismatch is a compile-time error.

#### 10.11.6 NULL Values

`NULL` pivot values match source rows where the corresponding pivot key is `NULL`:

```sql
pivot values { (Quarter: 'Q1', Amount: 10d, CustomerId: 1, Region: 'North'), (Quarter: 'Q2', Amount: 20d, CustomerId: 2, Region: 'North'), (Quarter: null, Amount: 5d, CustomerId: 1, Region: 'South') } sales
on Quarter in (null as Missing, 'Q1' as Q1)
using Count(*) as Orders
```

#### 10.11.7 Composition

A pivot query is select-like. It can appear at the top level, inside a CTE, or as a derived table:

```sql
with p as (
    pivot values { (Quarter: 'Q1', Amount: 10d, CustomerId: 1, Region: 'North'), (Quarter: 'Q2', Amount: 20d, CustomerId: 2, Region: 'North'), (Quarter: null, Amount: 5d, CustomerId: 1, Region: 'South') } sales
    on Quarter in ('Q1' as Q1)
    using Sum(Amount) as Sales
    group by Region
)
select Region, Q1 from p
```

```sql
select p.Region, p.Q1
from (
    pivot values { (Quarter: 'Q1', Amount: 10d, CustomerId: 1, Region: 'North'), (Quarter: 'Q2', Amount: 20d, CustomerId: 2, Region: 'North'), (Quarter: null, Amount: 5d, CustomerId: 1, Region: 'South') } sales
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
- The PIVOT source follows the source-alias rules in §6.2. Pivot values and
  measures use the same alias grammar; bracket a reserved word when it is an
  intentional output alias.

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

The source is scanned once. For each source row, Musoq emits one output row per `IN (...)` entry, preserving the entry order. `ORDER BY`, `SKIP`, and `TAKE` are evaluated after row expansion. Evaluation strategy MUST preserve these rows and their paging semantics; streaming or materialization is not prescribed.

#### 10.12.1 Example

```sql
unpivot values { (Region: 'North', Country: 'Poland', Name: 'WARSAW', Q1: 10d, Q2: 20d, Q3: 30d, Population: 100d, Money: 50d) } s
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
unpivot values { (Region: 'North', Country: 'Poland', Name: 'WARSAW', Q1: 10d, Q2: 20d, Q3: 30d, Population: 100d, Money: 50d) } s
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

The generated value column uses the common type of all `IN (...)` value expressions, using the same compatibility rules as inline `VALUES` fields. Numeric values can widen when the existing type system allows it. If any value can be `NULL`, including an explicit `NULL` literal or a nullable scalar expression, the value column is nullable. Incompatible mixes, such as numeric and string expressions, are rejected.

`NULL` values are preserved. UNPIVOT does not filter null value rows implicitly.

#### 10.12.5 Composition

An unpivot query is select-like. It can appear at the top level, inside a CTE, as a derived table, in joins, and in set-operator branches:

```sql
with u as (
    unpivot values { (Region: 'North', Country: 'Poland', Name: 'WARSAW', Q1: 10d, Q2: 20d, Q3: 30d, Population: 100d, Money: 50d) } s
    on Quarter in (s.Q1 as Q1, s.Q2 as Q2)
    using Sales
    keep s.Region as Region
)
select Region, Quarter, Sales from u
```

```sql
select u.Region, u.Quarter, u.Sales
from (
    unpivot values { (Region: 'North', Country: 'Poland', Name: 'WARSAW', Q1: 10d, Q2: 20d, Q3: 30d, Population: 100d, Money: 50d) } s
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
- The UNPIVOT source follows the source-alias rules in §6.2. `IN` entry and
  `KEEP` aliases use the same boundary-aware grammar; complex expressions
  still require an explicit alias.

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
FROM values { (col: 1, col2: 10, col3: 'x', col4: 1), (col: 2, col2: 20, col3: 'x', col4: 2) } data
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
select Name, City, Count(Name) over (partition by City) from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'BERLIN'),
    (Name: 'Cara', City: 'WARSAW'),
    (Name: 'Dan', City: 'MUNICH')
} rows
```

Multiple partition columns are supported — rows are grouped by the composite key:

```sql
select Name, Sum(Population) over (partition by Country, City) from values {
    (Name: 'Ada', City: 'WARSAW', Country: 'Poland', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Country: 'Germany', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Country: 'Poland', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Country: 'Germany', Population: 400d)
} rows
```

When `PARTITION BY` is omitted, all rows belong to a single partition:

```sql
-- Running sum across all rows
select Name, Sum(Population) over (order by Name) from values {
    (Name: 'Ada', Population: 100d),
    (Name: 'Bob', Population: 200d),
    (Name: 'Cara', Population: 200d),
    (Name: 'Dan', Population: 400d)
} rows
```

### 11.4 ORDER BY within Window Specification

`ORDER BY` inside the `OVER` clause determines the logical row ordering within each partition. Both `ASC` (default) and `DESC` are supported, and each key may use `NULLS FIRST` or `NULLS LAST`.

- **Ranking functions** (`RowNumber`, `Rank`, `DenseRank`, `PercentRank`, `CumeDist`) require `ORDER BY` — it determines rank and peer-group boundaries.
- **Aggregate functions** (`Sum`, `Count`, `Avg`, `Min`, `Max`) behave differently depending on whether `ORDER BY` is present:
  - **With `ORDER BY`**: Computes a running (cumulative) result from the first row in the partition up to the current row.
  - **Without `ORDER BY`**: Computes the result over the entire partition — every row in the partition receives the same value.
- **Offset functions** (`Lag`, `Lead`) require `ORDER BY` — it defines which row is "previous" or "next".

```sql
-- Running sum (with ORDER BY)
select Name, Sum(Population) over (order by Name) from values {
    (Name: 'Ada', City: 'WARSAW', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Population: 400d)
} rows

-- Partition total (without ORDER BY)
select Name, City, Sum(Population) over (partition by City) from values {
    (Name: 'Ada', City: 'WARSAW', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Population: 400d)
} rows

-- Descending order
select Name, RowNumber() over (order by Name desc) from values {
    (Name: 'Ada', City: 'WARSAW', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Population: 400d)
} rows
```

### 11.5 Supported Window Functions

#### 11.5.1 Ranking Functions

Ranking functions assign an ordinal position to each row within its partition based on `ORDER BY`.

| Function | Return Type | Description |
|----------|-------------|-------------|
| `RowNumber()` | `long` | Assigns a unique sequential integer to each row within the partition, starting at 1. No ties — every row gets a distinct number. |
| `Rank()` | `long` | Assigns the same rank to rows with equal `ORDER BY` values. Leaves gaps: if two rows share rank 2, the next rank is 4. |
| `DenseRank()` | `long` | Like `Rank()`, but without gaps: if two rows share rank 2, the next rank is 3. |
| `PercentRank()` | `double` | Returns `(Rank() - 1) / (partition row count - 1)`. A one-row partition returns `0`. |
| `CumeDist()` | `double` | Returns the ordinal of the peer group's final row divided by the partition row count. |
| `Ntile(n)` | `long` | Distributes rows into `n` roughly equal-sized groups (buckets) within the partition. Assigns a bucket number from 1 to `n`. If the partition size is not evenly divisible, earlier buckets receive one extra row. |

```sql
-- RowNumber: 1, 2, 3, 4, 5
select Name, RowNumber() over (order by Name) from values { (Name: 'Ada'), (Name: 'Bob'), (Name: 'Cara'), (Name: 'Dan'), (Name: 'Eve') } people

-- Rank: 1, 2, 2, 4
select Name, Rank() over (order by Population) from values { (Name: 'Ada', Population: 100), (Name: 'Bob', Population: 200), (Name: 'Cara', Population: 200), (Name: 'Dan', Population: 400) } people

-- DenseRank: 1, 2, 2, 3
select Name, DenseRank() over (order by Population) from values { (Name: 'Ada', Population: 100), (Name: 'Bob', Population: 200), (Name: 'Cara', Population: 200), (Name: 'Dan', Population: 400) } people

-- PercentRank: 0, 0.333..., 0.333..., 1
select Name, PercentRank() over (order by Population) from values { (Name: 'Ada', Population: 100), (Name: 'Bob', Population: 200), (Name: 'Cara', Population: 200), (Name: 'Dan', Population: 400) } people

-- CumeDist: 0.25, 0.75, 0.75, 1
select Name, CumeDist() over (order by Population) from values { (Name: 'Ada', Population: 100), (Name: 'Bob', Population: 200), (Name: 'Cara', Population: 200), (Name: 'Dan', Population: 400) } people

-- Ntile: 1, 1, 2, 2, 3
select Name, Ntile(3) over (order by Name) from values { (Name: 'Ada'), (Name: 'Bob'), (Name: 'Cara'), (Name: 'Dan'), (Name: 'Eve') } people
```

`RowNumber`, `Rank`, `DenseRank`, `PercentRank`, and `CumeDist` take no arguments — the ordering is determined solely by the `ORDER BY` clause. `Ntile` takes a single integer argument specifying the number of buckets.

#### 11.5.2 Offset Functions

Offset functions access values from rows at a fixed distance from the current row within the partition.

| Function | Return Type | Description |
|----------|-------------|-------------|
| `Lag(column [, offset [, default]])` | Nullable type of column | Returns the value of `column` from the row that is `offset` rows **before** the current row. Returns `NULL` (or `default` if specified) when no such row exists. Default `offset` is 1. |
| `Lead(column [, offset [, default]])` | Nullable type of column | Returns the value of `column` from the row that is `offset` rows **after** the current row. Returns `NULL` (or `default` if specified) when no such row exists. Default `offset` is 1. |

```sql
-- Previous row's Population (NULL for the first row)
select Name, Lag(Population) over (order by Name) from values {
    (Name: 'Ada', Population: 100d),
    (Name: 'Bob', Population: 200d),
    (Name: 'Cara', Population: 200d),
    (Name: 'Dan', Population: 400d)
} rows

-- Next row's Population (NULL for the last row)
select Name, Lead(Population) over (order by Name) from values {
    (Name: 'Ada', Population: 100d),
    (Name: 'Bob', Population: 200d),
    (Name: 'Cara', Population: 200d),
    (Name: 'Dan', Population: 400d)
} rows
```

**NULL semantics for offset functions:**

- When a scalar column (e.g., `decimal`, `int`, `long`) is accessed via `Lag` or `Lead`, the return type is automatically promoted to its nullable equivalent (e.g., `decimal?`, `int?`, `long?`). This allows `NULL` to represent the absence of a previous or next row without runtime errors.
- String and record columns are already nullable and need no promotion.

#### 11.5.3 Aggregate Window Functions

Standard aggregate functions can be used as window functions when combined with `OVER`:

| Function | Return Type | Description |
|----------|-------------|-------------|
| `Sum(column)` | Nullable input numeric type | Sum of non-null values. Running sum with `ORDER BY`; partition total without. Checked integral/decimal arithmetic. |
| `Count(column)` | `long` | Count of non-null values. Running count with `ORDER BY`; partition count without. Empty frames return zero. |
| `Avg(column)` | Integral inputs: `decimal?`; otherwise nullable input numeric type | Average of non-null values. Running average with `ORDER BY`; partition average without. Empty/all-null frames return null. |
| `Min(column)` | Input scalar type, nullable | Minimum value. Running minimum with `ORDER BY`; partition minimum without. |
| `Max(column)` | Input scalar type, nullable | Maximum value. Running maximum with `ORDER BY`; partition maximum without. |

```sql
-- Running sum ordered by Name
select Name, Sum(Population) over (order by Name) from values {
    (Name: 'Ada', City: 'WARSAW', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Population: 400d)
} rows

-- Count per partition
select Name, City, Count(Name) over (partition by City) from values {
    (Name: 'Ada', City: 'WARSAW', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Population: 400d)
} rows

-- Running average
select Name, Avg(Population) over (order by Name) from values {
    (Name: 'Ada', City: 'WARSAW', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Population: 400d)
} rows

-- Partition minimum and maximum
select Name, City, Min(Population) over (partition by City) from values {
    (Name: 'Ada', City: 'WARSAW', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Population: 400d)
} rows
select Name, City, Max(Population) over (partition by City) from values {
    (Name: 'Ada', City: 'WARSAW', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Population: 400d)
} rows
```

#### 11.5.4 Value Access Functions

Value access functions retrieve a specific row's value from within the partition. Their result retains the input scalar type, with nullable scalar metadata (`T?`) because a frame or requested position may have no value. This nullable metadata also applies when the particular input is nonnullable and its evaluated frames are nonempty. String and record results retain their logical type and admit `NULL`.

| Function | Return Type | Description |
|----------|-------------|-------------|
| `FirstValue(column)` | Input scalar type, nullable | Returns the first value in the effective frame, or `NULL` for an empty frame. |
| `LastValue(column)` | Input scalar type, nullable | Returns the last value in the effective frame, or `NULL` for an empty frame. |
| `NthValue(column, n)` | Input scalar type, nullable | Returns the value at 1-based position `n` in the effective frame, or `NULL` when that position is outside the frame. |

```sql
-- First value in partition
select Name, FirstValue(Name) over (order by Name) from values {
    (Name: 'Ada', City: 'WARSAW', Population: 100d, Value: 1),
    (Name: 'Bob', City: 'BERLIN', Population: 200d, Value: 2),
    (Name: 'Cara', City: 'WARSAW', Population: 200d, Value: 3),
    (Name: 'Dan', City: 'MUNICH', Population: 400d, Value: 4)
} rows

-- Running last value (last included peer; current row when keys are unique)
select Name, LastValue(Name) over (order by Name) from values {
    (Name: 'Ada', City: 'WARSAW', Population: 100d, Value: 1),
    (Name: 'Bob', City: 'BERLIN', Population: 200d, Value: 2),
    (Name: 'Cara', City: 'WARSAW', Population: 200d, Value: 3),
    (Name: 'Dan', City: 'MUNICH', Population: 400d, Value: 4)
} rows

-- Partition-wide last value (all rows same)
select Name, LastValue(Population) over () from values {
    (Name: 'Ada', City: 'WARSAW', Population: 100d, Value: 1),
    (Name: 'Bob', City: 'BERLIN', Population: 200d, Value: 2),
    (Name: 'Cara', City: 'WARSAW', Population: 200d, Value: 3),
    (Name: 'Dan', City: 'MUNICH', Population: 400d, Value: 4)
} rows

-- Second value in the running frame (NULL until the frame contains two rows)
select Name, NthValue(Name, 2) over (order by Name) from values {
    (Name: 'Ada', City: 'WARSAW', Population: 100d, Value: 1),
    (Name: 'Bob', City: 'BERLIN', Population: 200d, Value: 2),
    (Name: 'Cara', City: 'WARSAW', Population: 200d, Value: 3),
    (Name: 'Dan', City: 'MUNICH', Population: 400d, Value: 4)
} rows

-- NthValue per partition
select Name, City, NthValue(Name, 2) over (partition by City order by Name)
from values {
    (Name: 'Ada', City: 'WARSAW', Population: 100d, Value: 1),
    (Name: 'Bob', City: 'BERLIN', Population: 200d, Value: 2),
    (Name: 'Cara', City: 'WARSAW', Population: 200d, Value: 3),
    (Name: 'Dan', City: 'MUNICH', Population: 400d, Value: 4)
} rows
```

**NthValue with the implicit ordered frame:**
- The frame includes all peers of the current row, so position `n` can be
  available before the current physical row reaches position `n`.
- With unique ordering keys, this reduces to a running prefix: positions before
  `n` return `NULL`, and subsequent rows return the value at position `n`.

**NthValue semantics without ORDER BY:**
- All rows receive the same value from the `n`-th row in the partition, or `NULL` if the partition has fewer than `n` rows.

These partition-wide and running descriptions assume no explicit frame. An
explicit frame overrides them. Without a defined order, the choice of rows at
frame positions is unspecified.

### 11.6 Underscore Naming Variants

Function names are case-insensitive and underscore-insensitive. The following pairs are equivalent:

| Camel Case | Underscore Form |
|------------|-----------------|
| `RowNumber()` | `ROW_NUMBER()` |
| `DenseRank()` | `DENSE_RANK()` |
| `PercentRank()` | `PERCENT_RANK()` |
| `CumeDist()` | `CUME_DIST()` |
| `Ntile(n)` | `NTILE(n)` |
| `FirstValue(column)` | `FIRST_VALUE(column)` |
| `LastValue(column)` | `LAST_VALUE(column)` |
| `NthValue(column, n)` | `NTH_VALUE(column, n)` |

Both forms produce identical results:

```sql
-- These two queries are equivalent
select Name, RowNumber() over (order by Name) from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows
select Name, ROW_NUMBER() over (order by Name) from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows
```

### 11.7 WHERE Clause Interaction

`WHERE` filtering occurs **before** window function computation. Rows excluded by `WHERE` are not visible to the window function:

```sql
-- Only rows with Population > 150 participate in the window
select Name, RowNumber() over (order by Name)
from values {
    (Name: 'Ada', Population: 100d),
    (Name: 'Bob', Population: 200d),
    (Name: 'Cara', Population: 200d),
    (Name: 'Dan', Population: 400d)
} rows
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
from values { (Name: 'Ada', City: 'WARSAW', Country: 'Poland', Population: 100d), (Name: 'Bob', City: 'BERLIN', Country: 'Germany', Population: 200d) } people
```

Custom window functions support `PARTITION BY`, `ORDER BY`, and multi-argument signatures — the same capabilities as the built-in functions.

### 11.11 Frame Semantics

Musoq's implicit ordered window frame uses peer-aware `RANGE` semantics. Explicit `ROWS BETWEEN` remains position-based, while explicit `RANGE BETWEEN` uses the complete peer group for `CURRENT ROW` boundaries.

#### 11.11.1 Implicit Frame

When no explicit frame specification is provided, frame-sensitive aggregate and
value-access window functions use the following implicit frame:

| Condition | Implicit Frame (Musoq) | PostgreSQL Default |
|-----------|------------------------|--------------------|
| `ORDER BY` present | `RANGE BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW` | `RANGE BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW` |
| `ORDER BY` absent | Entire partition (all rows receive the same value) | Entire partition (same) |

For the unbounded-preceding-to-current-row frames compared here, ROWS and RANGE
differ only when ordering values contain ties. Bounded positional and
value-distance frames can differ even when all ordering keys are distinct.

When an explicit `ROWS BETWEEN` or `RANGE BETWEEN` frame is specified, it overrides the implicit frame. See section 11.12 for the explicit frame specification syntax.

#### 11.11.2 ROWS vs RANGE: Behavior with Tied ORDER BY Values

**ROWS mode**: Each row is processed individually in sequence. Even when multiple rows share the same ORDER BY value, each row accumulates independently and may produce a different result.

**RANGE mode**: Rows for which every ORDER BY key compares equal are treated as *peers*. Peers receive the **same** aggregate result — the value computed as if all peers were included. Direction and `NULLS FIRST` / `NULLS LAST` are honored independently for every key.

Example with `SUM(val) OVER (ORDER BY category)`:

| Row | category | val | Explicit ROWS | Implicit/explicit RANGE |
|-----|----------|-----|:-------------:|:-----------------------:|
| 1 | A | 10 | 10 | 10 |
| 2 | B | 20 | **30** | **50** |
| 3 | B | 20 | **50** | **50** |
| 4 | C | 30 | 80 | 80 |

- Rows 2 and 3 both have `category = B`. In RANGE mode, they are peers and both receive 50 (sum of all rows up to and including all B's). In ROWS mode, row 2 gets 30 (A + first B) and row 3 gets 50 (A + both B's).
- Rows 1 and 4 have unique ORDER BY values, so both modes produce the same result.

For this running-prefix example, distinct ordering values give identical results.

#### 11.11.3 Which Functions Are Affected

| Function Category | Affected by ROWS vs RANGE? | Notes |
|-------------------|:--------------------------:|-------|
| `RowNumber()` | No | Always assigns distinct sequential numbers — no tie concept |
| `Rank()`, `DenseRank()` | No | Tie-aware by definition — identical behavior in both modes |
| `Ntile(n)` | No | Bucket assignment uses positions in the partition |
| `Lag()`, `Lead()` | No | Offsets use positions in the partition, independently of the frame |
| `Sum()`, `Count()`, `Avg()` | **Yes** | Running aggregates differ when ORDER BY has ties |
| `Min()`, `Max()` | **Yes** | Running min/max differ when ORDER BY has ties |
| `FirstValue()` | **Yes** | Returns the first row in the effective frame; identical for the running-prefix comparison above |
| `LastValue()` | **Yes** | ROWS: current row is last. RANGE: last peer is last |
| `NthValue()` | **Yes** | ROWS: Nth physical row. RANGE: depends on peer-group boundary |

Ranking functions (including `PercentRank` and `CumeDist`) and offset functions
ignore frame boundaries. The running descriptions in this table compare
unbounded-preceding-to-current-row frames; explicit bounded frames govern
aggregate and value-access functions through their own included rows.

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
| `RANGE BETWEEN ... AND ...` | Explicit value-based frame | **Supported with bounded-key restrictions** — requires window `ORDER BY`; offset bounds require one numeric key |
| `GROUPS BETWEEN ... AND ...` | Explicit peer-group-based frame | Not supported |
| `EXCLUDE CURRENT ROW` | Excludes current row from frame | Not supported |
| `EXCLUDE GROUP` | Excludes current row's peer group | Not supported |
| `EXCLUDE TIES` | Excludes peers of current row (keeps current) | Not supported |
| `NULLS FIRST` / `NULLS LAST` | Explicit NULL ordering | **Supported** |
| `FILTER (WHERE ...)` | Conditional window aggregation | **Supported** for aggregate window functions |
| `QUALIFY` | Filter on window function results | **Supported** (see section 11.13) |
| `PERCENT_RANK()` | `(RANK() - 1) / (partition row count - 1)`; returns `0` for a one-row partition | **Supported**; returns `double` |
| `CUME_DIST()` | Peer-group end ordinal divided by the partition row count | **Supported**; returns `double` |

#### 11.11.6 Practical Implications

Ranking and offset functions remain position-based according to their individual contracts. Ordered aggregate and value-access functions use the implicit peer-aware RANGE frame unless an explicit frame is supplied. Adding a tiebreaker ORDER BY key intentionally makes otherwise tied rows distinct peers.

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
| `RANGE BETWEEN` | Yes, with restrictions | Requires `ORDER BY` (MQ3052); PRECEDING/FOLLOWING offsets require exactly one numeric key (MQ3098) |
| `GROUPS BETWEEN` | No | Peer-group-based frame — not supported |

`ROWS BETWEEN` is fully supported. `RANGE BETWEEN` requires an `ORDER BY` clause; omitting it raises MQ3052. `CURRENT ROW` expands to the complete peer group using every ORDER BY key. Numeric PRECEDING/FOLLOWING offsets use a single numeric key; multiple or nonnumeric keys raise MQ3098 during binding. When the current bounded key is null, the frame resolves to that key's null peer group. `GROUPS BETWEEN` is not supported.

#### 11.12.4 Examples

**Sliding window sum (3-row window):**

```sql
select Name, Population,
       Sum(Population) over (order by Name rows between 1 preceding and 1 following)
from values {
    (Name: 'Ada', Population: 100d),
    (Name: 'Bob', Population: 200d),
    (Name: 'Cara', Population: 200d),
    (Name: 'Dan', Population: 400d)
} rows
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
from values {
    (Name: 'Ada', Population: 100d),
    (Name: 'Bob', Population: 200d),
    (Name: 'Cara', Population: 200d),
    (Name: 'Dan', Population: 400d)
} rows
```

**Full partition aggregate with explicit frame:**

```sql
select Name, Population,
       Sum(Population) over (rows between unbounded preceding and unbounded following) as Total
from values {
    (Name: 'Ada', Population: 100d),
    (Name: 'Bob', Population: 200d),
    (Name: 'Cara', Population: 200d),
    (Name: 'Dan', Population: 400d)
} rows
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
from values {
    (Name: 'Ada', City: 'WARSAW', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Population: 400d)
} rows
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
from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'BERLIN'),
    (Name: 'Cara', City: 'WARSAW'),
    (Name: 'Dan', City: 'MUNICH')
} rows
qualify rn <= 2
```

Returns at most 2 rows per `City`, ordered by `Name` within each city.

**Filter by ranking:**

```sql
select Name, Population, DenseRank() over (order by Population desc) as dr
from values {
    (Name: 'Ada', Population: 100d),
    (Name: 'Bob', Population: 200d),
    (Name: 'Cara', Population: 200d),
    (Name: 'Dan', Population: 400d)
} rows
qualify dr <= 3
```

Returns rows with the top 3 distinct population values.

**QUALIFY with WHERE:**

```sql
select Name, City, RowNumber() over (order by Name) as rn
from values {
    (Name: 'Ada', City: 'WARSAW', Population: 100d),
    (Name: 'Bob', City: 'BERLIN', Population: 200d),
    (Name: 'Cara', City: 'WARSAW', Population: 200d),
    (Name: 'Dan', City: 'MUNICH', Population: 400d)
} rows
where Population > 100
qualify rn <= 3
```

`WHERE` filters rows before window computation. `QUALIFY` filters after. In this example, only rows with `Population > 100` are numbered, and then only the first 3 are returned.

**QUALIFY with SKIP/TAKE:**

```sql
select Name, RowNumber() over (order by Name) as rn
from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows
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
    from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'BERLIN'),
    (Name: 'Cara', City: 'WARSAW'),
    (Name: 'Dan', City: 'MUNICH')
} rows
)
select Name, City, rn from ranked where rn <= 2

-- With QUALIFY (more concise)
select Name, City, RowNumber() over (partition by City order by Name) as rn
from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'BERLIN'),
    (Name: 'Cara', City: 'WARSAW'),
    (Name: 'Dan', City: 'MUNICH')
} rows
qualify rn <= 2
```

Both produce identical results. `QUALIFY` is more concise and expresses the filter directly in the window query.

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

All three operations use the same component-wise key equality. NULL key
components compare equal to NULL and unequal to non-NULL values. Text comparison
is ordinal and case-sensitive. Non-NULL values retain their logical type's
set-operation equality, including nominal enum identity; floating-point NaN does
not compare equal to itself. Duplicate keys in the right input do not multiply
membership matches.

Both sides of a set operation MUST return the same number of projected values. Corresponding projected values MUST have compatible types.

A bare `NULL` opposite a value type `T` makes the corresponding column `T?`
(`Nullable<T>`) in both inputs and the combined output. This rule applies to
`UNION`, `UNION ALL`, `EXCEPT`, and `INTERSECT`, including empty results, decoded
schema columns, computed expressions, CTEs, and chained operations. Reversing
the inputs does not change the inferred type. Pairing `T` with `T?` likewise
produces `T?`; it does not introduce additional numeric conversions.

Reference types retain their type when paired with `NULL`. Columns whose inputs
are all bare `NULL` retain the existing null-only (`DBNull`) metadata until a
concrete type supplies context. Enum identity must still match; nullable enum
carriers preserve that identity. Output column names come from the left input.
The typed-null workaround `CASE WHEN 1 = 0 THEN ToByte(0) ELSE NULL END` remains
valid and retains the `MQ5008_UnreachableCode` warning.

`UNION ALL` accepts omitted, empty, and explicit key syntax for consistency, but it preserves every row from both inputs and does not use the key list to remove duplicates.

### 12.2 UNION

Combines results from two queries, removing duplicates identified by the effective key. With omitted keys, every projected value participates:

```sql
select Name, City from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'BERLIN'),
    (Name: 'Cara', City: 'WARSAW'),
    (Name: 'Dan', City: 'MUNICH')
} rows
union
select Name, City from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'ROME'),
    (Name: 'Eve', City: 'HAMBURG'),
    (Name: 'Finn', City: 'ROME')
} rows
```

With an explicit subset key, only the named projected values participate:

```sql
select Name, City from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'BERLIN'),
    (Name: 'Cara', City: 'WARSAW'),
    (Name: 'Dan', City: 'MUNICH')
} rows
union (Name)
select Name, City from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'ROME'),
    (Name: 'Eve', City: 'HAMBURG'),
    (Name: 'Finn', City: 'ROME')
} rows
```

Representative selection considers the left input before the right input and
retains the first encountered complete row for each key. A matching left row
therefore wins over every right row, including when their non-key values differ.
If only the right input contains a key, its first encountered row wins. These
rules also apply to NULL keys and to intermediate results in a set-operation
chain.

An input with unspecified row order may encounter any of its equal-key rows
first; any such row is a permitted representative. This does not permit a right
representative when the left input contains that key. Representative selection
does not promise final output ordering; use ORDER BY on the combined result when
that ordering matters.

### 12.3 UNION ALL

Combines results preserving all rows including duplicates. The key list may be omitted:

```sql
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows
union all
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Eve'),
    (Name: 'Finn')
} rows
```

An explicit key list is accepted for syntactic consistency but does not change `UNION ALL` behavior:

```sql
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows
union all (Name)
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Eve'),
    (Name: 'Finn')
} rows
```

### 12.4 EXCEPT

Returns every row from the first query whose effective key does not appear in
the second query. Repeated left rows retain their multiplicity and complete
projected values; this operation does not deduplicate the left input:

```sql
select Name, City from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'BERLIN'),
    (Name: 'Cara', City: 'WARSAW'),
    (Name: 'Dan', City: 'MUNICH')
} rows
except
select Name, City from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'ROME'),
    (Name: 'Eve', City: 'HAMBURG'),
    (Name: 'Finn', City: 'ROME')
} rows
```

### 12.5 INTERSECT

Returns every row from the first query whose effective key appears in the second
query. Membership uses only the effective key. Matching left rows retain their
complete projected values and multiplicity, including repeated identical rows
and rows with different non-key values. Right duplicates do not multiply the
result:

```sql
select Name, City from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'BERLIN'),
    (Name: 'Cara', City: 'WARSAW'),
    (Name: 'Dan', City: 'MUNICH')
} rows
intersect
select Name, City from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'ROME'),
    (Name: 'Eve', City: 'HAMBURG'),
    (Name: 'Finn', City: 'ROME')
} rows
```

### 12.6 Chaining Set Operations

Three or more queries can be chained. Unparenthesized chains associate from
left to right, with no implicit higher precedence for INTERSECT. To choose another
grouping, use a CTE or derived-table wrapper. Trailing modifiers still apply once
to the entire result (§12.8):

```sql
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows
union all (Name)
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows
union all (Name)
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows
union all (Name)
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows
union all (Name)
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows
```

### 12.7 Different Source Columns

Source columns can differ if aliases unify them:

```sql
select Name from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'BERLIN'),
    (Name: 'Cara', City: 'WARSAW'),
    (Name: 'Dan', City: 'MUNICH')
} rows
union (Name)
select City as Name from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'ROME'),
    (Name: 'Eve', City: 'HAMBURG'),
    (Name: 'Finn', City: 'ROME')
} rows   -- City aliased as Name
```

### 12.8 SKIP/TAKE Scope

Trailing `SKIP` / `TAKE` apply once to the complete set result, after set
combination and after any trailing `ORDER BY`:

```sql
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows
union (Name)
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Eve'),
    (Name: 'Finn')
} rows order by Name skip 2 take 5
```

Use an explicit derived table or CTE when an operand-local slice is intended:

```sql
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows
union (Name)
select Name from (
    select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Eve'),
    (Name: 'Finn')
} rows order by Name skip 2 take 5
) sliced_branch
```

The parser never pushes trailing result modifiers into an operand.

### 12.9 Set Operations in CTEs

```sql
with p as (
    select 1 as Id, 'First' as Name from values {
    (Id: 1, Name: 'Ada'),
    (Id: 2, Name: 'Bob'),
    (Id: 3, Name: 'Cara'),
    (Id: 4, Name: 'Dan')
} rows
    union all (Name)
    select 2 as Id, 'Second' as Name from values {
    (Id: 1, Name: 'Ada'),
    (Id: 2, Name: 'Bob'),
    (Id: 3, Name: 'Cara'),
    (Id: 4, Name: 'Dan')
} rows
    union all (Name)
    select 3 as Id, 'Third' as Name from values {
    (Id: 1, Name: 'Ada'),
    (Id: 2, Name: 'Bob'),
    (Id: 3, Name: 'Cara'),
    (Id: 4, Name: 'Dan')
} rows
)
select Id, Name from p
```

### 12.10 ORDER BY with Set Operations

In standard SQL, an `ORDER BY` clause placed after the last query in a set operation applies to the **entire combined result**:

Syntax fragment (not a complete executable batch):

```sql
-- SQL comparison fragment; A is the relation defined for this illustration.
SELECT City FROM A WHERE Money > 200
UNION
SELECT City FROM A WHERE Money <= 200
ORDER BY City DESC
```

Musoq follows this convention. A trailing `ORDER BY` is parsed once after the
complete unparenthesized set tree and orders the combined result:

```sql
-- Musoq interpretation:
-- ORDER BY sorts the combined UNION result
select City from values {
    (City: 'WARSAW', Money: 100d),
    (City: 'BERLIN', Money: 200d),
    (City: 'WARSAW', Money: 300d),
    (City: 'MUNICH', Money: 400d)
} rows where Money > 200
union (City)
select City from values {
    (City: 'WARSAW', Money: 100d),
    (City: 'BERLIN', Money: 200d),
    (City: 'WARSAW', Money: 300d),
    (City: 'MUNICH', Money: 400d)
} rows where Money <= 200
order by City desc
```

Result ordering binds against the exported names of the first operand, which
define the combined output schema. Source aliases and aliases that exist only
in a later operand are not visible. `NULLS FIRST` / `NULLS LAST`, ascending and
descending keys, `SKIP`, and `TAKE` retain their ordinary result-level meaning.

An outer wrapper remains equivalent and can be useful when composing a set as a
larger query:

```sql
with combined as (
    select City from values {
    (City: 'WARSAW', Money: 100d),
    (City: 'BERLIN', Money: 200d),
    (City: 'WARSAW', Money: 300d),
    (City: 'MUNICH', Money: 400d)
} rows where Money > 200
    union (City)
    select City from values {
    (City: 'WARSAW', Money: 100d),
    (City: 'BERLIN', Money: 200d),
    (City: 'WARSAW', Money: 300d),
    (City: 'MUNICH', Money: 400d)
} rows where Money <= 200
)
select City from combined order by City desc
```

To preserve operand-local ordering or slicing, place that operand inside its own
derived table or CTE.

---

## 13. ORDER BY, SKIP, TAKE

### 13.1 ORDER BY

Sorts result rows. Default direction is ascending:

```sql
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows order by Name            -- ascending (default)
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows order by Name asc        -- explicit ascending
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows order by Name desc       -- descending
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows order by Name nulls last
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows order by Name desc nulls first
```

Multi-column ordering with mixed directions:

```sql
select City, Population from values {
    (City: 'WARSAW', Population: 100d),
    (City: 'BERLIN', Population: 200d),
    (City: 'WARSAW', Population: 200d),
    (City: 'MUNICH', Population: 400d)
} rows
order by Population desc, City asc
```

ORDER BY can use expressions:

```sql
select Name, Population from values {
    (Name: 'Ada', Population: 100d),
    (Name: 'Bob', Population: 200d),
    (Name: 'Cara', Population: 200d),
    (Name: 'Dan', Population: 400d)
} rows order by Population * -1
```

ORDER BY uses **ordinal (case-sensitive) comparison** for strings: uppercase letters sort before lowercase (`'A'` < `'a'`).

`NULLS FIRST` and `NULLS LAST` may be added after each order key. If omitted, ascending keys place `NULL` first and descending keys place `NULL` last.

> **Set operations:** Trailing `ORDER BY` applies to the complete combined
> result. See §12.10 for output-name binding and branch-local wrappers.

### 13.2 ORDER BY with SELECT Aliases

SELECT aliases defined with `AS` can be referenced directly in ORDER BY:

```sql
select City, Money as Amount from values {
    (City: 'WARSAW', Money: 100d),
    (City: 'BERLIN', Money: 200d),
    (City: 'WARSAW', Money: 300d),
    (City: 'MUNICH', Money: 400d)
} rows order by Amount desc
```

This also works with computed expressions:

```sql
select City, Money * 2 as DoubledMoney from values {
    (City: 'WARSAW', Money: 100d),
    (City: 'BERLIN', Money: 200d),
    (City: 'WARSAW', Money: 300d),
    (City: 'MUNICH', Money: 400d)
} rows order by DoubledMoney desc
```

And with aggregate functions after GROUP BY:

```sql
select City, Sum(Money) as TotalRevenue
from values {
    (City: 'WARSAW', Money: 100d),
    (City: 'BERLIN', Money: 200d),
    (City: 'WARSAW', Money: 300d),
    (City: 'MUNICH', Money: 400d)
} rows
group by City
order by TotalRevenue desc
```

This is the recommended form in multi-source grouped queries as well:

```sql
select
    e.Department as department,
    t.Sum(t.Amount) as total_amount
from values { (Id: 1, Department: 'Engineering'), (Id: 2, Department: 'Support') } e
inner join values { (EmployeeId: 1, Amount: 120d), (EmployeeId: 1, Amount: 80d), (EmployeeId: 2, Amount: 50d) } t
    on e.Id = t.EmployeeId
group by e.Department
order by total_amount desc
```

Alias lookup is **case-insensitive**:

```sql
select City as CITYNAME, Money as amount from values {
    (City: 'WARSAW', Money: 100d),
    (City: 'BERLIN', Money: 200d),
    (City: 'WARSAW', Money: 300d),
    (City: 'MUNICH', Money: 400d)
} rows order by Amount desc
-- "Amount" matches alias "amount"
```

> **Note:** Only explicit aliases (those declared with `AS`) can be referenced in ORDER BY. Auto-generated column names (where no `AS` is used) must be referenced by their expression directly.

When an explicit alias is used in `ORDER BY`, ordering applies to the aliased **SELECT expression result**, not to an unrelated source column with the same name.

### 13.3 SKIP

Skip the first N rows of the result:

```sql
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows skip 2
```

If SKIP exceeds the number of rows, zero rows are returned (no error).

A positive `SKIP` without `ORDER BY` produces `MQ5021_UnorderedSkip`. `SKIP 0`
does not count as slicing and does not produce the warning.

### 13.4 TAKE

Take the first N rows of the result:

```sql
select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows take 3
```

If TAKE exceeds the number of available rows, all available rows are returned (no error).

### 13.5 SKIP + TAKE (Pagination)

Combine for pagination:

```sql
select Name from values {
    (Name: 'Person01'),
    (Name: 'Person02'),
    (Name: 'Person03'),
    (Name: 'Person04'),
    (Name: 'Person05'),
    (Name: 'Person06'),
    (Name: 'Person07'),
    (Name: 'Person08'),
    (Name: 'Person09'),
    (Name: 'Person10'),
    (Name: 'Person11'),
    (Name: 'Person12'),
    (Name: 'Person13'),
    (Name: 'Person14'),
    (Name: 'Person15')
} people order by Name skip 10 take 5
-- Returns Person11 through Person15.
```

Both `SKIP` and `TAKE` accept an integer literal or a direct `$name` reference declared by `param(...)`, `params(...)`, or `let`:

```sql
params(offset: int = 0, limit: int = 100)
select r.Value
from values { (Value: 3), (Value: 1), (Value: 2) } r
order by r.Value
skip $offset take $limit
```

The referenced type must be `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, or `ulong`, optionally nullable. The count must contain a value between `0` and `2147483647`, inclusive. Null, negative, and oversized counts are errors. Other types, including strings, floating-point values, decimals, collections, and records, cannot be paging counts. Arithmetic, calls, row-dependent expressions, and member/index access are not accepted directly in these clauses. Existing `let` initializer and declaration-order rules still apply.

Literal and `let` counts are validated during binding (`MQ3118_InvalidPagingCount`). Runtime parameter counts are validated once per execution after parameter binding and before opening query sources (`MQ7019_InvalidRuntimePagingCount`); missing parameters and parameter-binding failures retain their existing diagnostics. Parameter defaults may be overridden, and changing a compiled query's parameters changes its next execution's slice without recompilation. Zero is valid: `SKIP 0` skips nothing and `TAKE 0` returns no rows.

Runtime counts preserve eligible bounded top-N/top-offset ordering. Combined bounds are calculated without integer overflow; a bound beyond the supported bounded-ordering range uses complete ordering before slicing. Runtime paging remains in the engine rather than being substituted into the compile-time datasource planning contract. An unordered runtime `SKIP` receives `MQ5021_UnorderedSkip` because its value may be positive; a known zero literal or `let` remains quiet.

### 13.6 Interaction with GROUP BY and HAVING

ORDER BY, SKIP, and TAKE are applied after GROUP BY and HAVING:

```sql
select City, Sum(Money) from values {
    (City: 'WARSAW', Money: 100d),
    (City: 'BERLIN', Money: 200d),
    (City: 'WARSAW', Money: 300d),
    (City: 'MUNICH', Money: 400d)
} rows
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
    select City, Country from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} rows
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
    select a.City from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} a
)
select City from p          -- valid

-- INVALID because a is local to the CTE body
select [a.City] from p
```

To expose a dotted name, declare that name explicitly:

```sql
with p as (
    select a.City as [a.City] from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} a
)
select [a.City] from p
```

Musoq MUST NOT automatically prefix duplicate implicit output names with the source alias. If a CTE projects same-named columns from multiple sources, query authors SHOULD provide explicit output aliases:

```sql
with p as (
    select
        a.Country as LeftCountry,
        b.Country as RightCountry
    from values {
    (Id: 1, Country: 'Poland'),
    (Id: 2, Country: 'Germany'),
    (Id: 3, Country: 'Poland'),
    (Id: 4, Country: 'Germany')
} a
    inner join values {
    (Id: 1, Country: 'Poland'),
    (Id: 2, Country: 'Germany'),
    (Id: 3, Country: 'Germany'),
    (Id: 4, Country: 'Italy')
} b on a.Id = b.Id
)
select LeftCountry, RightCountry from p
```

Source aliases are scoped to the query block where they are declared. Aliases declared inside a CTE body are not visible outside that body. A legal CTE name MUST NOT collide with internal intermediate producers, including names equal to concatenated source aliases or resembling generated names. Renaming aliases without changing their bindings MUST preserve query results.

Each set-operation operand has its own source-alias scope. Reusing an alias in
another operand or CTE definition does not change predicate placement in the
current query block. A deterministic predicate whose dependencies are available
on the left of APPLY may be evaluated before right-side setup. Right-dependent
and volatile predicates retain their existing dependency and stability rules.
This does not specify an unrestricted evaluation order for SQL expressions.

An authored CTE that is not transitively reachable from the outer query is
reported as `MQ5022_UnusedCte` during binding. References from live CTEs count
as reachability; a self-reference in an otherwise unused recursive definition
does not. Synthesized internal CTEs are not reported.

CTE definitions are independent named query expressions. A CTE definition MUST NOT reference aliases from a query block that later consumes the CTE. Subqueries nested inside a CTE body may correlate only to aliases visible in that CTE body's own query block:

```sql
with matched as (
    select a.City
    from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} a
    where exists (
        select b.City
        from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'HAMBURG', Country: 'Germany'),
    (City: 'ROME', Country: 'Italy')
} b
        where b.Country = a.Country
    )
)
select City from matched
```

### 14.4 Star Expansion from CTE

```sql
with p as (
    select City, Country from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} rows
)
select * from p    -- expands to City, Country
```

Qualified star follows the same output-name rules:

```sql
with p as (
    select a.* from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} a
)
select * from p    -- expands to the source column names, for example City, Country
```

### 14.5 CTE with Aggregation

```sql
with summary as (
    select Country, Sum(Population) from values {
    (Country: 'Poland', Population: 100d),
    (Country: 'Germany', Population: 200d),
    (Country: 'Poland', Population: 200d),
    (Country: 'Germany', Population: 400d)
} rows group by Country
)
select * from summary
```

Aggregation can also occur on a CTE reference:

```sql
with raw as (
    select Population, Country from values {
    (Country: 'Poland', Population: 100d),
    (Country: 'Germany', Population: 200d),
    (Country: 'Poland', Population: 200d),
    (Country: 'Germany', Population: 400d)
} rows
)
select Country, Sum(Population) from raw group by Country
```

### 14.6 Multiple CTEs

Define multiple CTEs separated by commas:

```sql
with
    cities as (select City, Country from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} rows),
    countries as (select distinct Country from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} rows)
select * from cities
```

### 14.7 CTE with Set Operations

```sql
with combined as (
    select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows
    union (Name)
    select Name from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Eve'),
    (Name: 'Finn')
} rows
)
select * from combined
```

### 14.8 CTE with JOIN

```sql
with p as (select City, Country from values {
    (City: 'WARSAW', Country: 'Poland', Population: 100d),
    (City: 'BERLIN', Country: 'Germany', Population: 200d),
    (City: 'WARSAW', Country: 'Poland', Population: 200d),
    (City: 'MUNICH', Country: 'Germany', Population: 400d)
} rows)
select p.City, b.Population
from p
inner join values {
    (City: 'WARSAW', Country: 'Poland', Population: 100d),
    (City: 'BERLIN', Country: 'Germany', Population: 200d),
    (City: 'HAMBURG', Country: 'Germany', Population: 200d),
    (City: 'ROME', Country: 'Italy', Population: 400d)
} b on p.City = b.City
```

If a CTE reference is given a table alias in `FROM`, that alias is the only qualifier visible for that CTE in the current query block:

```sql
with p as (select City, Country from values {
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'BERLIN', Country: 'Germany'),
    (City: 'WARSAW', Country: 'Poland'),
    (City: 'MUNICH', Country: 'Germany')
} rows)
select c.City from p c       -- valid

-- INVALID in the same query block
select p.City from p c
```

Unqualified identifiers in expression contexts resolve to columns before table or CTE names. This allows a CTE to expose a column with the same name as the CTE itself:

```sql
with cte as (
    select City as cte from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} rows
)
select cte from cte          -- reads the column named cte
```

### 14.9 Recursive CTEs and limitations

Musoq supports `WITH RECURSIVE` with one anchor, one top-level `UNION` or
`UNION ALL` boundary, and one recursive member. Recursive members reject
aggregation, grouping, windows, ordering, pagination, nested set operations,
multiple self-references, unsupported join families, pivot/unpivot, mutual
recursion, `SEARCH`, and `CYCLE` with dedicated diagnostics. These restrictions
do not apply to the query consuming the completed CTE.

Evaluate the anchor once to form the first frontier. Each iteration evaluates the
recursive member against only the preceding frontier, appends admitted rows, and
uses those newly admitted rows as the next frontier. An empty frontier terminates.
UNION ALL preserves multiplicity; UNION deduplicates across all admitted rows,
including the anchor. Explicit UNION keys use §12 key equality and preserve the
first admitted representative. Null key values compare equal for this purpose.
The recursive output must match the anchor's column count and logical types;
an explicit column list supplies unique names. Self-reference is visible only in
the recursive member. Exactly one self-reference is allowed, including through
supported derived forms. INNER JOIN and CROSS JOIN with nonrecursive sources and
supported APPLY forms may accompany that reference; outer, ASOF, semi and anti
recursive joins are rejected. A recursive CTE may use earlier completed CTEs;
mutual recursion and forward dependencies are rejected.

`CROSS APPLY Interpret<T>`, `InterpretAt<T>`, and `PartialInterpret<T>` may consume
byte-array columns from the current frontier, including computed byte-array
arguments. A chain of CROSS APPLY interpretation stages may consume fields decoded
by earlier stages. Internal projection transitions do not require intermediate
row objects for stable typed interpretation chains. Stable predicates depending
only on an available left input may run before interpretation; right-dependent
and volatile predicates retain their dependency and stability rules. The recursive
operator restrictions above still apply and report MQ3075 for unsupported
operators.

For partial interpretation, `BytesConsumed` is relative to the supplied byte array
or slice. Error metadata is retained for malformed chunks; applications requiring
complete chunks can filter on `ErrorField is null`.

Execution limits are host-selected safety limits, with default maximum 1,000
iterations, 10,000,000 admitted rows and 10,000,000 captured snapshot rows. Exceeding a limit reports a recursive-limit
diagnostic, never a silently truncated successful relation. Cancellation remains
cancellation. Without an outer ORDER BY, consumers must not assume result order.
- **Duplicate aliases**: Using the same alias for two tables within a CTE inner expression reports the duplicate-alias diagnostic.

```sql
-- ERROR: Duplicate alias 'a'
with p as (
    select 1 from values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'BERLIN'),
    (Name: 'Cara', City: 'WARSAW'),
    (Name: 'Dan', City: 'MUNICH')
} a inner join values {
    (Name: 'Ada', City: 'WARSAW'),
    (Name: 'Bob', City: 'BERLIN'),
    (Name: 'Cara', City: 'WARSAW'),
    (Name: 'Dan', City: 'MUNICH')
} a on 1 = 1
)
select * from p
```

---

## 15. TABLE and COUPLE Statements

This section summarizes the optional TABLE/COUPLE profile. It does not require
that profile for core-only conformance; its detailed rules are owned by the satellite.

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

TABLE always defines the coupled alias's visible columns and order. Source tables
explicitly declare the logical schema origin. The default `ProviderDefined`
requires exact declared/provider logical scalar types, including nullability:
`int` differs from `int?`, and `int` differs from `long`. Equivalent type spellings
resolve before comparison. `TableDefined` providers accept TABLE-established
types and retain automatic scalar nullable lifting. Array/dictionary row
representations do not imply either origin.

Binding preserves source storage indexes and actual read types independently
of declared output order. Missing, ambiguous, and incompatible declared columns
fail with MQ3001, MQ3002, and MQ3005 at their declarations before row sources open,
including unselected columns. Direct source calls and settings-only COUPLE retain
the original provider schema. See TABLE/COUPLE section 6.2 for the full contract.

The following nullable mappings describe `TableDefined` sources. For typed
sources, a scalar type is nullable only when its declaration includes `?`.

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

A TABLE column may also name a previously declared query-local enum
case-insensitively. Host-native enum type names, including nullable forms, MUST NOT
be named in TABLE declarations. Binding rejects them with
`MQ2012_InvalidSchemaDefinition` at the offending column, naming the table,
column, enum type, and suggested underlying primitive type. This applies even
when the column is not selected or the datasource supports logical scalar
reads. Declare the primitive type or a query-local ENUM instead.

Query-local TABLE enum fields are permitted only when the datasource advertises
logical scalar reads. Enum scalar types follow TABLE nullability rules while
retaining their logical enum identity. Provider-defined enum columns supplied directly
by a datasource remain supported.

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
couple schema.method with table TableName as AliasName;
couple schema.method with settings ProfileName as AliasName;
couple schema.method with table TableName and settings ProfileName as AliasName;
couple schema.method with settings ProfileName and table TableName as AliasName;
```

**Complete Example:**

```sql
table DummyTable {
    Name: string
};
couple schema.records with table DummyTable as SourceOfDummyRows;
select Name from SourceOfDummyRows();
```

With parameters:

```sql
table DummyTable { Name: string };
couple schema.records with table DummyTable as SourceOfDummyRows;
select Name from SourceOfDummyRows(true, 'filter');
```

The `settings` option selects a host-resolved source runtime settings profile. Settings-only couples infer table metadata from the underlying schema; table-and-settings couples use the declared table shape and selected profile. Explicit SQL-level profile selection is available only through `couple`.

**Settings example:**

```sql
couple schema.items with settings prod as ProdItems;
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
desc schema
```

Returns a single-column table named `Name`. Each row contains one available schema method (for example `empty`, `entities`).

### 16.2 Describe a Method (Overloads)

```sql
desc schema.records
```

Returns one row per available overload of the selected method.

The result shape is:

- `Name`
- `Param 0`, `Param 1`, ... as needed to fit the widest overload

Each parameter cell contains `ParameterName: logical-type` using the canonical names in §3.9. Overloads with fewer parameters leave the remaining parameter columns empty. Usable declared optional defaults are shown as `ParameterName: logical-type = literal` using canonical formatting; required and metadata-less parameters retain the required format.

### 16.3 Describe a Specific Constructor Result

Describe the columns produced by a concrete source call:

```sql
desc schema.records()
```

Returns a table with columns: `Name`, `Index`, `Type`.

Arguments may be provided when the schema method is overloaded and the engine needs to identify a specific signature:

```sql
desc schema.dynamicRows(0, 'test', 10.5d)
```

The argument values are matched against the selected source signature. The returned table describes the row shape produced by that callable.

The argument list accepts the same positional-prefix/named-suffix form as
`FROM`. `DESC FUNCTIONS` is an inventory operation and rejects named source
arguments; an inner datasource call in `DESC QUERY` follows normal binding.

### 16.4 Describe a Specific Column or Nested Property

Inspect the structure behind a complex column, private table, or nested property path:

```sql
desc schema.records() column Array             -- describe an array column
desc schema.records() column Self              -- describe a complex object column
desc schema.records() column Children          -- describe a complex type column
desc schema.records() column Self.Children     -- nested property path
desc schema.records() column Self.Other.Children  -- deep nested path
desc schema.records() column Self.Dictionary   -- collection path
```

Returns a table with columns: `Name`, `Index`, `Type`.

Rules:

- Root column names and nested property names are matched case-insensitively.
- Property paths may be arbitrarily deep: `A.B.C.D` is valid if each step resolves.
- The final target may be a complex object, an array, or another ordered collection.
- If the final target is an array or another ordered collection, the output describes the element type.
- If the final target is a complex object, the output describes that object's properties so you can continue exploratory navigation.
- If the final target is a primitive, `string`, or `object`, the statement fails.
- If any path segment does not exist, the statement fails.

For nested descriptions, the `Index` column refers to the original top-level column index from the described table.

### 16.5 Describe Schema Functions

```sql
desc functions schema
desc functions schema.records
desc functions schema.records()
desc functions schema.records('filter')    -- with arguments
```

Returns a table with columns: `Method`, `Description`, `Category`, `Source`.

This statement lists the query functions available for the schema context. A `.method` or `.method(...)` suffix is accepted by the parser, but it does not narrow the function list. These forms behave the same as `desc functions A`.

Only user-visible query functions are returned. Internal helpers and aggregation-set helpers are excluded.

### 16.6 Describe Source Runtime Settings

Inspect source runtime setting requirements and resolution status:

```sql
desc settings schema.records;
desc settings CoupledAlias;
```

For a coupled alias, the selected settings profile is the one declared by the `couple` statement:

```sql
couple schema.items with settings prod as ProdItems;
desc settings ProdItems;
```

Returns a table with columns: `Name`, `Required`, `Secret`, `Phases`, `Status`, `Description`.

`Status` is `Provided`, `Missing`, or `Default`. Setting values are never returned, even for non-secret settings. A coupled alias form uses the settings profile selected by the `couple ... with settings ...` statement.

### 16.7 Describe Query Output

Describe the projected output columns of a query without executing the query against source rows:

```sql
desc query (select Name as PersonName, Population + Money as Total from values { (Name: 'Ada', Population: 100d, Money: 50d) } people)
```

The inner query is parsed and bound normally so aliases, star expansion, CTEs, parameters, script variables, set operators, and validation rules match normal query compilation. Execution returns only metadata, not the query's data rows.

The result shape is:

- `Name`
- `Index`
- `Type`

One row is returned for each projected output column in final projection order.

### 16.8 Describe Source Arguments

Use `DESC ARGUMENTS` to inspect structural receiving contracts:

```sql
desc arguments schema.match
desc arguments schema.match('TODO', patterns: array { (Id: 'todo', Pattern: 'TODO') })
```

The parameterized form needs declarations, not host values:

```sql
param(patterns: (Id: string, Pattern: string)[])
desc arguments schema.match('TODO', patterns: $patterns)
```

A coupled source is described through its coupled alias:

```sql
table MatchRows { PatternId: string };
couple schema.match with table MatchRows as Matches;
desc arguments Matches
```

The result has exactly eleven columns: `Overload`, `Path`, `Kind`, `Type`, `Required`, `Nullable`, `HasDefault`, `Default`, `MaxDepth`, `MaxNodes`, and `MaxStringBytes`. Root collection rows carry the effective input limits. Element and field paths use `patterns[]`, `patterns[].Id`, and so on; `Required` is database null for element entries. Overload numbers are zero-based in deterministic canonical signature order. Concrete calls describe the selected receiving contract, while inventory `DESC FUNCTIONS` semantics are unchanged. Metadata callbacks may use safe metadata inputs, but descriptions never construct DTOs, open sources, enumerate CTEs, or require host values.

### 16.9 General DESC Rules

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
from values {
    (City: 'WARSAW', Country: 'POLAND', Population: 100d),
    (City: 'BERLIN', Country: 'GERMANY', Population: 200d),
    (City: 'WARSAW', Country: 'POLAND', Population: 200d),
    (City: 'MUNICH', Country: 'GERMANY', Population: 400d)
} rows select City, Country

-- With WHERE
from values {
    (City: 'WARSAW', Country: 'POLAND', Population: 100d),
    (City: 'BERLIN', Country: 'GERMANY', Population: 200d),
    (City: 'WARSAW', Country: 'POLAND', Population: 200d),
    (City: 'MUNICH', Country: 'GERMANY', Population: 400d)
} rows where Country = 'POLAND' select City, Country

-- With GROUP BY
from values {
    (City: 'WARSAW', Country: 'POLAND', Population: 100d),
    (City: 'BERLIN', Country: 'GERMANY', Population: 200d),
    (City: 'WARSAW', Country: 'POLAND', Population: 200d),
    (City: 'MUNICH', Country: 'GERMANY', Population: 400d)
} rows group by Country select Country, Sum(Population)

-- Full combination
from values {
    (City: 'WARSAW', Country: 'POLAND', Population: 100d),
    (City: 'BERLIN', Country: 'GERMANY', Population: 200d),
    (City: 'WARSAW', Country: 'POLAND', Population: 200d),
    (City: 'MUNICH', Country: 'GERMANY', Population: 400d)
} a
inner join values {
    (City: 'WARSAW', Country: 'POLAND', Population: 100d),
    (City: 'BERLIN', Country: 'GERMANY', Population: 200d),
    (City: 'HAMBURG', Country: 'Germany', Population: 200d),
    (City: 'ROME', Country: 'Italy', Population: 400d)
} b on a.City = b.City
where a.Country = 'POLAND'
group by a.Country
select a.Country, Sum(b.Population)
order by a.Country
skip 1
take 5

-- Inside a CTE
with cte as (
    from values {
    (City: 'WARSAW', Country: 'POLAND', Population: 100d),
    (City: 'BERLIN', Country: 'GERMANY', Population: 200d),
    (City: 'WARSAW', Country: 'POLAND', Population: 200d),
    (City: 'MUNICH', Country: 'GERMANY', Population: 400d)
} rows where Country = 'POLAND' select City, Country
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

Musoq provides optional libraries of functions covering string manipulation, math, date/time, type conversion, validation, JSON/XML processing, cryptography, compression, bitwise operations, networking utilities, and collections. Additionally, each data source may define its own functions.

Because the set of available functions depends on which data sources are in use, the way to discover installed extensions is the `DESC FUNCTIONS` statement (see [§16.5](#165-describe-schema-functions)):

```sql
-- List all functions available for a schema
desc functions schema

-- List all functions available in the context of a specific method
desc functions schema.records()
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

### 18.4 Enum Intrinsics

`EnumValue(value)`, `EnumName(value)`, `IsDefined(value)`,
`HasAnyFlags(value, members...)`, and `HasAllFlags(value, members...)` are
language intrinsics rather than datasource library methods. Member arguments
must be quoted literals and are resolved before execution. Flag helpers require
a flags enum. `EnumName` returns only an exact declared name and never builds a
comma-separated composite.

---

### 18.5 Required function surface

Core requires the scalar RowNumber operation (§5.6), enum intrinsics (§3.8),
Length(string or collection), Count, Sum, Avg, Min, Max, and the window operations
listed in §11. Names and context restrictions follow their owning sections.
Length counts UTF-16 units or collection elements, returns int?, and returns null
for null input. Count returns long, including zero for empty input. Numeric Sum,
Min and Max retain their input numeric kind with nullable result metadata. Avg
returns nullable decimal for integral inputs and preserves decimal, float or double
for other numeric inputs. For example, Avg over integral values [1, 2] returns
decimal 1.5. Numeric aggregates ignore null and return null for empty/all-null
input. Integral and decimal aggregate updates and partial-state merges use checked
arithmetic; runtime overflow fails the query with `MQ7018_NumericOverflow` in the
Runtime phase. These result and empty-input rules also apply to aggregate windows and
existing distinct variants. Floating-point results remain sensitive to input order.
String Min/Max use §10.2. DISTINCT and FILTER alter the contributing input before
the aggregate operates. Temporal extrema, AggregateValues, ToXxx, file readers,
formatting helpers and functions used only in external-provider examples are
library extensions with separately published signatures and behavior.

DESC FUNCTIONS inventories the receiving provider's exposed functions; it does
not redefine the minimum above. Additional functions may be supplied by a host or
provider and must not change the language operations or overload their meanings.

---

## 19. NULL Semantics

### 19.1 NULL Propagation

Most expressions involving `null` produce `null`:

```sql
select null + 1 from values { (Seed: 0) } seed        -- null
select null = null from values { (Seed: 0) } seed      -- null (not true)
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

Ordinary comparison operators applied to `NULL` do not test for nullness:
they produce `NULL` (and therefore do not select a row in a predicate).
Diagnostic-aware binding reports `MQ5017_NullComparison` for these comparisons
in `WHERE`, `HAVING`, `QUALIFY`, `JOIN ON`, and aggregate `FILTER` contexts.
Use `IS NULL`, `IS NOT NULL`, or a null-safe distinctness operator instead.

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
select Country, City, Count(1) from values { (Country: 'POLAND', City: 'WARSAW'), (Country: 'POLAND', City: null), (Country: 'GERMANY', City: 'BERLIN') } places group by Country, City
-- Groups: (POLAND, WARSAW), (POLAND, null), (GERMANY, BERLIN)
```

### 19.5 NULL from OUTER Joins

When `LEFT JOIN`, `RIGHT JOIN`, `FULL OUTER JOIN`, `OUTER APPLY`, or `ASOF LEFT JOIN` produces an unmatched side, columns from the missing side are `null`. Scalar expressions are evaluated over these null-extended columns using their normal semantics. A scalar function is not implicitly null-propagating: for example, `Coalesce(b.Name, 'fallback')` returns `'fallback'` when `b` is missing. Ordinary function arguments retain their established evaluation order and eager evaluation, including fallback arguments. This does not create an injected source carrier for a missing row.

Scalar types are automatically promoted to nullable:

```sql
-- If no match, b.Population becomes decimal? with value null
select a.City, b.Population
from values {
    (City: 'WARSAW', Population: 100d, Value: 1),
    (City: 'BERLIN', Population: 200d, Value: 2),
    (City: 'WARSAW', Population: 200d, Value: 3),
    (City: 'MUNICH', Population: 400d, Value: 4)
} a
left join values {
    (City: 'WARSAW', Population: 100d, Value: 1),
    (City: 'BERLIN', Population: 200d, Value: 2),
    (City: 'HAMBURG', Population: 200d, Value: 3),
    (City: 'ROME', Population: 400d, Value: 4)
} b on a.City = b.City
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
from values {
    (Name: 'Ada', Population: 100d),
    (Name: 'Bob', Population: 200d),
    (Name: 'Cara', Population: 200d),
    (Name: 'Dan', Population: 400d)
} a
asof left join values {
    (Name: 'Eve', Population: 100d),
    (Name: 'Finn', Population: 200d),
    (Name: 'Gia', Population: 200d),
    (Name: 'Hugo', Population: 400d)
} b on a.Population >= b.Population
-- If a.Population is NULL, b columns are NULL
```

### 19.7 NULL Fallback Operator

The `??` operator returns the first operand when that operand is not `null`; otherwise, it evaluates to the fallback operand:

```sql
select Name ?? 'Unknown' from values { (Name: null), (Name: 'Ada') } people
select NullableValue ?? 0 from values {
    (Name: 'Ada', NullableValue: null),
    (Name: 'Bob', NullableValue: 2),
    (Name: 'Cara', NullableValue: null),
    (Name: 'Dan', NullableValue: 4)
} rows
select null ?? Name from values {
    (Name: 'Ada', NullableValue: null),
    (Name: 'Bob', NullableValue: 2),
    (Name: 'Cara', NullableValue: null),
    (Name: 'Dan', NullableValue: 4)
} rows
```

Both operands MUST bind successfully, including an unreachable fallback. Thus
`1 ?? MissingMethod()` is a binding error, and incompatible operand types remain
invalid even when the left operand is statically nonnullable.

For numeric operands, the result uses the unique most-specific common type to
which both underlying types have a lossless conversion (§22.6). For example,
`int? ?? long` yields `long`, and `int ?? long` also yields `long`. The result is
nullable only when both alternatives can be null; `T? ?? T` therefore yields
`T`, while `T? ?? T?` yields `T?`. A literal NULL adopts the other operand's type.
Equal nonnumeric types retain their type; related reference types use the
compatible receiving type. Incompatible or ambiguous common types fail binding.

The left operand is evaluated once. The right operand is evaluated only when the
left value is null, after the same safe widening conversions used to determine
the result type. A statically nonnullable left operand may be converted to the
common type and retained after both operands have been validated. `??` is not a
null-navigation operator; `?.` is not part of the language.

### 19.8 NULL-Related Functions

Musoq provides functions for working with `NULL` values, such as coalescing, null-checking, and replacing nulls with defaults. To discover all available NULL-handling functions and their signatures, use `desc functions` (see [§16.5](#165-describe-schema-functions)).

### 19.9 NULL in Functions

Most built-in functions return `null` when any required parameter is `null`:

```sql
select Trim(null) from values { (Seed: 0) } seed        -- null
select ToUpper(null) from values { (Seed: 0) } seed      -- null
select Abs(null) from values { (Seed: 0) } seed          -- null
select Concat(Name, 'text') from values { (Name: null), (Name: 'Ada') } names -- NULL, 'Adatext'
```

### 19.10 NULL in Enum Intrinsics

For a null enum value, `EnumValue` and `EnumName` return `NULL`.
`IsDefined`, `HasAnyFlags`, and `HasAllFlags` return `false`. For a non-null
flags value and an empty or zero mask, `HasAllFlags` returns `true` and
`HasAnyFlags` returns `false`.

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

`Contains(value, pattern)` and `StartsWith(value, pattern)` use ordinal-ignore-case
comparison and return `NULL` when either string argument is `NULL`. Computed
strings have the same behavior as literal strings. Each argument is evaluated
once, from left to right, before the function handles NULLs; the pattern is still
evaluated when the receiver is NULL. These ordinary scalar calls remain within
the selected CASE branch and do not evaluate unselected branches.

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
select ToLower(Name), Count(Name) from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows group by ToLower(Name)
select distinct ToLower(Name) from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows
```

### 20.4 Unicode Support

Full Unicode support across all operations including LIKE, GROUP BY, ORDER BY, and all string functions. Tested with: Polish, Russian, French, Japanese (Hiragana/Katakana/Kanji), Chinese (Simplified/Traditional), Korean, Arabic, German, Thai, Hebrew, Hindi, Turkish, Greek, Ukrainian, Vietnamese, and emoji.

---

## 21. Array and Property Access

### 21.1 Array Indexing

Typed collection parameters and collection `let` values also support indexing (`$values[index]`). The result retains the declared element type, including nullable elements, and uses the same zero-based, modular negative-index and safe out-of-bounds rules below. Parameter indexing reads the collection captured for the current execution.

Array elements are accessed with bracket notation (0-based):

```sql
let numbers: int[] = array { 10, 20, 30 };
select $numbers[0] from values { (Seed: 0) } seed     -- first element
select $numbers[2] from values { (Seed: 0) } seed     -- third element
```

#### Negative Indexing

Negative indices count from the end using modular wrapping:

```sql
let numbers: int[] = array { 10, 20, 30 };
select $numbers[-1] from values { (Seed: 0) } seed    -- last element
select $numbers[-2] from values { (Seed: 0) } seed    -- second to last
```

#### Out-of-Bounds Access

Out-of-bounds access returns the element default: zero for numeric types, false
for bool, U+0000 for char, and null for nullable, string, dynamic, record and
collection elements. Empty collections return that default even for negative
indices; the modulo rule applies only when length is nonzero. Element temporal
defaults are tick zero and GUID defaults contain all zero bits.

Examples:

| Scenario | Result |
|----------|--------|
| `int_array[100]` (out of bounds) | `0` (default int) |
| `string_array[100]` | `null` |
| `Array[-100]` (excessive negative) | Wraps modularly: `effectiveIndex = ((index % length) + length) % length` |

### 21.2 String Character Access

Strings support bracket indexing to access individual characters:

```sql
select Name[0] from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows      -- first character
select Name[-1] from values {
    (Name: 'Ada'),
    (Name: 'Bob'),
    (Name: 'Cara'),
    (Name: 'Dan')
} rows     -- last character
```

Out-of-bounds on strings returns `'\0'` (null character). Null strings return `'\0'`.

### 21.3 Dictionary Key Access

Access dictionary values by key:

```sql
select Dict['key_name'] from schema.records()
```

Missing keys return `null` (no exception).

### 21.4 Property Navigation

Access nested object properties with dot notation:

```sql
select Self.Name from schema.records()              -- single level
select Self.Self.Name from schema.records()          -- two levels
select Self.Self.Array from schema.records()         -- deep property
select Self.Array[2] from schema.records()           -- property + index
select Inc(Self.Array[2]) from schema.records()      -- function on indexed property
```

Accessing a non-existing field of a known record type reports `MQ3028_UnknownProperty` during binding.

### 21.5 Method Calls on Entities

A source alias can select a registered bindable schema/library method using dot
notation. A method may receive the source entity through the registered
injection contract; this does not invoke a function stored on a column value:

```sql
select a.GetPopulation() from schema.records() a
select a.ToUpperInvariant(a.City) from schema.records() a
```

Calls on column values, such as `Self.ToString()`, are unsupported. If the prefix
resolves to a column rather than a visible source alias, binding rejects the
call with `MQ3117_UnsupportedColumnInstanceMethod`. Source aliases take
precedence over identically named columns. Ambiguous columns retain their
ambiguity diagnostic; unknown prefixes retain their unknown-alias diagnostic.
Property navigation such as `Self.Name` remains supported.

---

## 22. Automatic Type Coercion

### 22.1 String-to-Numeric Coercion

When a `string` column is compared to a numeric literal, the engine automatically attempts to parse the string as a number at runtime:

```sql
select Name from values { (Name: 'parcel', Size: '1500') } items where Size > 1000
select Name from values { (Name: 'parcel', Size: '1500') } items where Size = 1500
select Name from values { (Name: 'parcel', Size: '1500') } items where 1000 < Size
```

**Edge cases:**
- Non-numeric strings (e.g., `"abc"`) simply don't match — no exception thrown
- `null` strings don't match any numeric comparison
- Supports all comparison operators: `=`, `<>`, `>`, `<`, `>=`, `<=`
- Works with hex (`0xFF`), binary (`0b1010`), and long (`9223372036854775807l`) literals

### 22.2 String-to-DateTime Coercion

When a `DateTime`, `DateTimeOffset`, or `TimeSpan` column is compared to a string literal, automatic parsing occurs:

```sql
param(eventDate: datetime = '2023-03-15', eventOffset: datetimeoffset = '2023-03-15T12:00:00+00:00', duration: timespan = '03:00:00')
select Name from values { (Name: 'event', EventDate: $eventDate) } events where EventDate > '2023-01-01'
select Name from values { (Name: 'event', EventDate: $eventDate) } events where EventDate = '2023-03-15'
select Name from values { (Name: 'event', EventDate: $eventOffset) } events where EventDate = '2023-03-15T12:00:00+00:00'
select Name from values { (Name: 'event', Duration: $duration) } events where Duration >= '02:00:00'
```

Bidirectional comparisons work:

```sql
param(eventDate: datetime = '2023-03-15')
select Name from values { (Name: 'event', EventDate: $eventDate) } events where '2023-03-15' < EventDate
```

Works in CASE WHEN expressions:

```sql
param(eventDate: datetime = '2023-03-15')
select Name,
    case when EventDate > '2023-03-15' then 'Future'
         when EventDate = '2023-03-15' then 'Present'
         else 'Past'
    end as TimeCategory
from values { (Name: 'event', EventDate: $eventDate) } events
```

Nullable date/time types (`DateTime?`, `DateTimeOffset?`, `TimeSpan?`) behave identically.

**Edge cases:**
- Unparseable strings (e.g., `"not-a-date"`) simply don't match — no exception thrown
- `null` strings don't match any date/time comparison
- Portable conversion forms are defined in §3.9. Additional host parsing forms are extensions, not portable query syntax.
- Supports all comparison operators: `=`, `<>`, `>`, `<`, `>=`, `<=`

Ambiguous numeric date text such as `01/02/2026` can be interpreted in more
than one culture-specific order. When such a string is implicitly converted
to `DateTime` or `DateTimeOffset`, binding reports
`MQ5003_ImplicitTypeConversion`. ISO year-first text, an unambiguous
component greater than 12, month names, `TimeSpan`, and explicit conversion
formats remain quiet; prefer ISO text or `ToDateTimeWithFormat`.

### 22.3 Numeric Type Promotion

In arithmetic and bitwise operations involving different numeric types, values are promoted to the wider type:

| Operation | Result Type |
|-----------|-------------|
| `byte + int` | `int` |
| `int + long` | `long` |
| `int + decimal` | `decimal` |
| `sbyte AND byte` | `int` (`int?` when an operand is nullable) |
| `byte AND ulong` | `ulong` (`ulong?` when an operand is nullable) |

Nonconstant integral scalar arithmetic retains its declared width and wraps on
overflow. Decimal scalar arithmetic and integral/decimal aggregate accumulation
are checked; their runtime overflow reports `MQ7018_NumericOverflow`. Numeric
literal and constant-arithmetic overflow remain binding errors (§3.9.1).
Out-of-range strict numeric conversions during execution use the same runtime
overflow diagnostic.

Widen operands before arithmetic when a wider result is required. For example,
`Sum((x.Value::long) * 1000000)` performs long multiplication and accumulation;
`Sum((x.Value * 1000000)::long)` first performs int multiplication, which may
already have wrapped, and then accumulates those wrapped values as long. Casting
the final result does not change the arithmetic or accumulator width.

### 22.4 Object Column Coercion

When an `object`-typed column is compared to a numeric literal, runtime conversion is attempted. Same graceful failure as string coercion — no exception on failure.

### 22.5 Enum Non-Coercion

Enum operands never participate in generic string, object, or numeric
coercion. A quoted literal is accepted only as a compile-time member in a
context that already identifies one enum. Use `EnumValue` for explicit numeric
inspection and `EnumName` for explicit textual output. Dynamic objects are not
inspected row by row to infer enum identities.

---

### 22.6 Callable Conversions and Overload Preference

Implicit numeric conversions in callable arguments and `??` preserve every value
in the source logical domain. Identity conversions are always allowed. The
following table lists additional allowed targets; all other numeric conversions
require an explicit conversion:

| Source | Additional implicit targets |
|--------|-----------------------------|
| sbyte | short, int, long, float, double, decimal |
| byte | short, ushort, int, uint, long, ulong, float, double, decimal |
| short | int, long, float, double, decimal |
| ushort | int, uint, long, ulong, float, double, decimal |
| int | long, double, decimal |
| uint | long, ulong, double, decimal |
| long | decimal |
| ulong | decimal |
| float | double |
| double, decimal | none |
| char (UTF-16 code unit) | ushort, int, uint, long, ulong, float, double, decimal |

These rules are type-based, not dependent on the particular argument value.
For example, even the value `1` of type `long` does not implicitly convert to
`double`. Nullable lifting preserves the underlying conversion: `T` may convert
to `U?`, and `T?` to `U?`, when `T` converts to `U`. A nullable value cannot
implicitly convert to a nonnullable value parameter. NULL is applicable only to
nullable or reference parameters and does not invent a preferred unrelated type.
Arrays do not gain element-wise numeric conversions. Strings, Boolean values,
and nominal enums do not participate in this numeric widening table.

First apply callable visibility, ownership, argument mapping, arity, generic
constraints, and structural receiving-contract rules. Compare remaining scalar
candidates argument by argument: identity is preferred to nullable lifting,
which is preferred to numeric widening. Numeric widening is preferred to an
erased reference receiver. Among otherwise compatible targets, prefer a target
that converts implicitly to the other target when the reverse conversion does
not exist. For example, a short argument prefers `F(int)` over `F(long)`;
`F(double)` and `F(decimal)` remain incomparable for an int argument.

A candidate wins through conversions only when it is no worse for every supplied
argument and strictly better for at least one. Conflicting argument preferences
are ambiguous, not added into a total conversion cost. When conversions are
equivalent, prefer fixed arity over variadic, then nongeneric over generic, then
fewer omitted defaults. If no unique candidate remains, binding reports
`MQ3089_AmbiguousCallableOverload`. Named and positional datasource calls use the
same preference after mapping arguments to parameters; registration order never
breaks a tie. Structural receiving contracts retain their structural matching
rules; their scalar field conversions use the same lossless table.

Arithmetic and bitwise promotion (§3.9.1 and §22.3), explicit casts, and explicit
conversion functions retain their separately defined rules.

## 23. Diagnostic Contract and Catalog

Musoq exposes diagnostics as stable, code-first records. Exception class names and
engine implementation details are not part of the language contract. Hosts and
agents should classify a failure by its diagnostic code, phase, source domain,
arguments, related locations, and actions.

This section defines the language diagnostic contract and catalog. Implementations
MUST preserve the meanings of published codes; implementation registries are
projections of this contract rather than prerequisites for understanding it.

### 23.1 Diagnostic envelope contract

Every diagnostic-aware API preserves the following fields:

| Field | Contract |
|-------|----------|
| Code | Stable MQ number and enum name; one root-cause meaning only. |
| Severity | Error blocks the relevant phase; warning is advisory and does not change execution. |
| Phase | Parse, Bind, Schema, DataSource, CodeGeneration, Runtime, FeatureGate, or Internal. |
| Source domain | Query, GeneratedSource, Schema, DataSource, Runtime, or Internal. |
| Location | Zero-based absolute UTF-16 offset, one-based line and UTF-16 column, and end location when known. A zero-length span is a real insertion point; null means unknown. |
| Arguments | Stable string facts such as symbol, callableKind, actualTypes, expectedCounts, schema, source, alias, and lifecycle operation. Values containing secrets are excluded. |
| Related locations | Typed secondary locations for declarations, candidates, generated origins, and related source facts when exact mapping exists. |
| Actions | Structured fixes and optional text edits, with the existing textual suggestions retained for compatibility. |
| Details | Sanitized by default. Raw exception details are available only through an explicit verbose formatting operation. |
| Correlation | Optional correlation ID for internal compiler and execution failures. |

Query locations always refer to SQL source. Generated-artifact locations remain in the
generated-source domain and are never clamped to an invented SQL location. A
query related location is added only when an exact artifact-to-query map exists.

Diagnostic-aware host operations expose warnings as well as errors. Execution-only
operations may omit advisory warnings, but MUST preserve execution failures.

Warnings do not alter parsed values, execution behavior, query identity, datasource
arguments, runtime behavior, or query results. Errors stop the next dependent
phase, while independent sibling roots may continue so their diagnostics can be
reported in source order. Nodes that depend on an invalid root are suppressed
rather than reported as misleading secondary errors.

### 23.2 Phase and source-domain index

The JSON catalog contains one descriptor for every active public enum member.
The following index is a quick guide; the catalog supplies the exact message,
explanation, and correction for each entry.

| Phase | Active code families | Default source domain |
|-------|----------------------|------------------------|
| Parse | MQ1001–MQ1009, MQ2001–MQ2048 | Query |
| Bind | MQ3001, MQ3002, MQ3005, MQ3007, MQ3008, MQ3010–MQ3035, MQ3036–MQ3105; MQ5003, MQ5008, MQ5010–MQ5025 | Query |
| Schema | MQ4016 and schema-definition MQ4001–MQ4015 | Schema |
| DataSource | Source construction and provider diagnostics | DataSource or Schema |
| Runtime | MQ7003–MQ7012 | Runtime or DataSource |
| CodeGeneration | MQ8001 | GeneratedSource |
| Internal | MQ9001–MQ9002 | Internal |

A diagnostic instance may override the default source domain when the failure
comes from a different boundary. For example, a provider-reported contract
failure can retain a Query related location while its primary source domain is
DataSource.

### 23.3 Root-cause classification

Callable resolution is deterministic and always proceeds in this order:
owner/schema exists, callable name exists, arity matches, argument types match,
exactly one overload wins, and exactly one owner wins.

| Code | Root cause | Evidence and correction |
|------|------------|-------------------------|
| MQ3085_UnknownSource | The schema exists but the referenced source/table method does not. | Includes schema and source facts plus bounded available-source candidates. Check the source name. |
| MQ3086_UnknownCallable | No scalar, aggregate, row, or library callable has the requested name. | Includes callable kind and spelling candidates. Check the callable name. |
| MQ3087_InvalidCallableArity | The name exists but no overload accepts the supplied argument count. | Includes actual and expected counts plus up to five signatures. Add/remove arguments or use the shown overload. |
| MQ3088_NoMatchingCallableOverload | At least one overload has the right arity but none accepts the argument types. | Includes actual types and bounded candidate signatures. Convert the arguments or select a compatible overload. |
| MQ3089_AmbiguousCallableOverload | More than one overload remains equally valid. | Includes candidate signatures and related candidate locations when available. Cast arguments or qualify the owner. |

Owner ambiguity remains distinct from overload ambiguity. MQ3079, MQ3080,
MQ3081, and MQ3083 describe named datasource argument metadata and are not
callable-name or callable-arity fallbacks.

Symbol lookup uses the same one-role contract:

| Situation | Diagnostic |
|-----------|------------|
| Unknown schema | MQ3010_UnknownSchema |
| Unknown source/table/CTE name | MQ3023_TableNotDefined or MQ3085_UnknownSource, depending on the lookup owner |
| Unknown alias qualifier | MQ3015_UnknownAlias |
| Unknown column on a known source | MQ3001_UnknownColumn |
| Ambiguous column | MQ3002_AmbiguousColumn |
| Unknown property on a known object type | MQ3028_UnknownProperty |

Dependent column and property expressions are poisoned after these failures so
one misspelling does not create a cascade of unrelated binder errors. A single
close spelling candidate may be returned as a structured text edit.

### 23.4 Exact parser diagnostics

Parser errors are reported at the smallest useful query span. Insertion
diagnostics use a known zero-length span immediately after the relevant token.
The boundary token is not consumed while recovering.

| Code | Trigger | Correction |
|------|---------|------------|
| MQ1009_NumericLiteralOutOfRange | Decimal, hexadecimal, binary, or octal literal cannot be represented by the supported numeric type. | Use a value in range or an explicitly supported type. |
| MQ2036_MultipleExecutableStatements | More than one executable statement is supplied to an entry point that accepts one query. | Compile statements separately or use the script entry point. |
| MQ2037_EmptyPredicateListNotAllowed | CONTAINS or another predicate list is empty. | Supply at least one predicate value. |
| MQ2038_InvalidSliceCount | TAKE or SKIP has a negative or invalid count. | Use a non-negative integer. |
| MQ2039_TieBreakRequiresAsOfJoin | TIE BREAK BY appears outside ASOF JOIN. | Move it into an ASOF JOIN or remove it. |
| MQ2040_InvalidDiagnosticCommand | A diagnostic command or modifier is not valid in its position. | Use the documented diagnostic command form. |
| MQ2041_InvalidStarModifierOrder | Star modifiers are duplicated or appear in an unsupported order. | Use the documented EXCLUDE, REPLACE, LIKE, and RENAME order. |
| MQ2042_InvalidEnumDeclaration | A query-local enum declaration is structurally incomplete or uses duplicate type names. | Use the documented declaration, delimiters, and comma-separated members. |
| MQ2043_InvalidEnumBackingType | The declaration names a non-integral or unsupported backing type. | Use byte, sbyte, short, ushort, int, uint, long, or ulong. |
| MQ2044_MissingEnumMemberValue | A member has no explicit integral literal. | Add `= <integral literal>` after every member name. |
| MQ2045_DuplicateEnumMember | Member names repeat exactly or differ only by case. | Rename or remove the duplicate member. |
| MQ2046_EnumMemberValueOutOfRange | A value cannot be represented by the declared backing type. | Change the value or choose a wider backing type. |
| MQ2047_EmptyEnumDeclaration | The declaration contains no members. | Add at least one explicitly-valued member. |
| MQ2048_UnsupportedEnumSyntax | A borrowed host-language or database enum form is used. | Use Musoq's statement-level `enum Name : int { Member = 1 };` form. |
| MQ2035_MissingRequiredAlias | A multi-source JOIN/APPLY source has no stable addressable alias. | Add a source alias; use brackets for a reserved word. |
| MQ2022_InvalidAlias | An alias position contains an invalid token or malformed optional alias. | Use an identifier or bracketed identifier. |

These parser diagnostics remain distinct from bind-time MQ3022_MissingAlias,
which describes method ownership after syntax is valid.

### 23.5 Active advisory warnings

Advisories are default-on and never change behavior. MQ5014 is normally emitted
during parsing for rooted paths and may also be emitted during binding for a
relative path bound to a path-sensitive string destination. The remaining
advisories are Bind-phase diagnostics.

| Code | Trigger | Quiet cases | Correction |
|------|---------|-------------|------------|
| MQ5003_ImplicitTypeConversion | An ambiguous numeric date string is implicitly converted to DateTime or DateTimeOffset. | ISO/year-first text, month names, unambiguous components, TimeSpan, widening, and explicit format calls. | Use ISO text or an explicit format conversion. |
| MQ5008_UnreachableCode | CASE branch, CASE tail, or coalesce fallback is proven unreachable. | Nullable, dynamic, literal-NULL, and uncertain values. | Remove or correct the unreachable branch. |
| MQ5010_TautologicalCondition | A deterministic predicate is proven always true. | Floating-point, method-call, dynamic, and null-sensitive identities. | Remove the redundant predicate. |
| MQ5011_ContradictoryCondition | A deterministic predicate is proven impossible. | The same conservative proof limits as MQ5010. | Correct the conflicting condition. |
| MQ5013_SourceContractWarning | A datasource reports a non-fatal contract or modifier issue. | No provider warning. | Follow the provider correction. |
| MQ5014_SuspiciousOrdinaryStringEscape | A recognized ordinary escape changes rooted path text or relative path-like text bound to a path-sensitive string destination. | Raw strings, doubled backslashes, unknown escapes, standalone escapes, and bare relative projections. | Use r'...' or double each intended backslash. |
| MQ5015_SuspiciousRegexEscape | Ordinary source \b becomes backspace in RLIKE or a bound regex-pattern parameter. | Raw r'\b', ordinary '\\b', character-class [\b], and preserved \d, \s, \w. | Use a raw regex literal or double the backslash. |
| MQ5016_GlobWildcardInLike | A constant LIKE pattern looks like a glob using * or path-like ?. | SQL %/_ patterns, dynamic patterns, whitespace prose, and ordinary questions. | Use %/_ for LIKE or RLIKE for regex semantics. |
| MQ5017_NullComparison | =, <>, !=, <, <=, >, or >= compares with NULL in a predicate context. | IS NULL, IS NOT NULL, IS DISTINCT FROM, and projected/order expressions. | Use an explicit NULL or null-safe predicate. |
| MQ5018_AmbiguousOuterJoinNullCheck | An optional-side nullable column IS NULL cannot distinguish row absence from a present NULL value. | Non-nullable/unknown derived columns and explicit PRESENT/MISSING checks. | Test row presence or use a non-nullable presence column. |
| MQ5019_NullRejectingOuterJoinFilter | WHERE is provably false or UNKNOWN for NULL-extended optional rows. | ON predicates, inner joins, preserving OR branches, and unknown method semantics. | Move an intended match restriction into ON or state row presence. |
| MQ5021_UnorderedSkip | Positive SKIP has no ORDER BY in its query block. | SKIP 0, TAKE-only, and ordered slicing. | Add a deterministic ORDER BY or remove SKIP. |
| MQ5022_UnusedCte | A user CTE is not transitively reachable from the outer query. | Live dependencies, nested live scopes, and synthesized CTEs. | Remove it or reference it from live work. |
| MQ5023_UnusedScriptVariable | A let declaration is not transitively used by executable work. | Script parameters and declarations used only by live dependencies. | Remove it or use it. |
| MQ5024_NullSensitiveNotIn | NOT IN has a literal/proven-NULL member or nullable result. | IN, null-free collections, and proven null exclusion. | Filter NULLs or use a null-aware predicate. |
| MQ5025_ImpossibleImplicitConversion | An engine-selected implicit conversion of a constant is proven unable to succeed. | Dynamic values, explicit soft conversions, and uncertain plugin conversions. | Use a compatible literal or explicit conversion. |

Specific advisories take precedence over generic tautology, contradiction, and
unreachable diagnostics for the same source region. The legacy MQ5009
OrderByAliasBehavior identifier is retained in compatibility documentation only
and is not an active v18 enum member or emitted diagnostic. Tested alias
ordering and shadowing behavior remains unchanged.

`MQ5020_SetOperationOrderByScope` and `MQ5026_SetOperationSliceScope` are
retained as catalog compatibility identifiers for the completed set-modifier
migration. The current language version does not emit them.

### 23.6 Schema, datasource, runtime, and internal failures

| Code family | Root cause | Location/detail policy |
|-------------|------------|------------------------|
| MQ4001–MQ4016 | Schema definition or schema construction failure. | Schema source domain; include the schema declaration location when known. |
| MQ7003–MQ7009 | Script-parameter or recursive-CTE runtime contract failure. | Query or Runtime domain; include safe parameter/limit facts, never secret values. |
| MQ7010 | Datasource construction/open failure. | DataSource domain; include schema, source, alias, context ID, and operation open. |
| MQ7011 | Datasource row read or deferred enumeration failure. | DataSource domain; retain safe cause information without exposing source argument values. |
| MQ7012 | Datasource cleanup/disposal failure. | DataSource domain; cancellation remains cancellation, not a cleanup error. |
| MQ7017 | A finite regex match budget was exhausted. | Runtime domain; include safe operator, engine, and limit facts without exposing pattern or input values. |
| MQ8001 | Generated code did not compile. | GeneratedSource domain; retain target-native facts and add SQL relation only for exact mappings. |
| MQ8002 | A compiled artifact is incompatible with the current host contract. | GeneratedSource or Runtime domain; recompile rather than treating it as syntax. |
| MQ9001–MQ9002 | An engine invariant failed without a query- or source-owned root classification. | Internal domain, safe message, and correlation ID; raw details require explicit verbose output. |

Cancellation is propagated unchanged through datasource and runtime boundaries.
Unexpected implementation failures are never rewritten as unsupported SQL syntax merely
because they crossed a query boundary.

### 23.7 v17 to v18 migration

This historical identifier table is informative. Host API migration details are
outside the language contract. Active codes and their meanings above are normative.

Removed values are not reused. Hosts upgrading diagnostic consumers should use
the following replacements:

| v17 code or behavior | v18 replacement |
|----------------------|-----------------|
| MQ3003 unknown table | MQ3023_TableNotDefined or MQ3085_UnknownSource |
| MQ3004 unknown function | MQ3086_UnknownCallable |
| MQ3006 invalid argument count | MQ3087_InvalidCallableArity |
| MQ3009 null reference | The role-specific symbol, cast, member, datasource, or internal code |
| MQ3013 cannot resolve method | MQ3086–MQ3089 according to the callable decision tree |
| MQ3014 invalid property access | MQ3028_UnknownProperty or the precise index/member code |
| MQ3029 unresolvable method | MQ3086–MQ3089 |
| MQ3030 construction not supported | MQ4016_UnsupportedSchemaConstruction |
| MQ3031 set operator missing keys | The applicable set-column/count/type diagnostic |
| MQ3082 ambiguous source invocation | MQ3089 for overload ambiguity or MQ3035 for owner ambiguity |
| MQ5001, MQ5002, MQ5004–MQ5007, MQ5009, MQ5012 | No active style/optimization warning; MQ5009 remains a compatibility documentation identifier only |
| MQ6001–MQ6004 | The precise parse, bind, schema, or runtime diagnostic |
| MQ7001 datasource binding failed | MQ7010_DataSourceOpenFailed |
| MQ7002 datasource iterator error | MQ7011_DataSourceReadFailed |
| MQ9999 unknown | MQ9001_InternalCompilerError or MQ9002_InternalExecutionError |

Agents should not infer a root cause from exception text. Read the code,
phase, source domain, arguments, related locations, and suggested action.

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
root           ::= [parameter_block [';']] {declaration [';']} executable_statement [';']

declaration    ::= script_variable_decl | enum_declaration
                 | table_definition | couple_statement | profile_declaration

executable_statement ::= select_query | cte_expression | desc_statement
select_query   ::= set_operators
profile_declaration ::= declaration syntax of an enabled satellite profile

script_item    ::= script_variable_decl
                 | statement

parameter_block ::= parameter_keyword '(' [parameter_decl {',' parameter_decl}] ')'
parameter_keyword ::= PARAM | PARAMS

parameter_decl ::= identifier ':' parameter_type ['=' parameter_default]

parameter_type ::= (type_name | record_type) { '?' | '[]' }
record_type    ::= '(' parameter_decl {',' parameter_decl} [','] ')'

parameter_default ::= constant_expression | record_literal | array_literal

script_variable_decl ::= LET identifier [':' parameter_type] '=' constant_expression

constant_expression ::= record_literal
                      | array_literal
                      | literal
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
                 | enum_declaration
                 | table_definition
                 | couple_statement
                 | desc_statement

cte_expression ::= WITH [RECURSIVE] cte_def {',' cte_def} set_operators

cte_def        ::= identifier ['(' identifier {',' identifier} ')'] AS '(' set_operators ')'
```

### 24.3 Query Grammar

```ebnf
set_operators  ::= set_chain [ORDER BY order_list] [SKIP integer] [TAKE integer]
set_chain      ::= query {set_operator query}

set_operator   ::= UNION [set_operator_key_list]
                 | UNION ALL [set_operator_key_list]
                 | EXCEPT [set_operator_key_list]
                 | INTERSECT [set_operator_key_list]

set_operator_key_list ::= '(' key_list ')'

key_list       ::= [identifier {',' identifier}]

query          ::= regular_query | reordered_query | pivot_query | unpivot_query

regular_query  ::= SELECT [DISTINCT] select_list
                   FROM from_clause
                   [WHERE expression]
                   [group_by_clause]
                   [WINDOW window_def {',' window_def}]
                   [QUALIFY expression]


reordered_query ::= FROM from_clause
                    [WHERE expression]
                    [group_by_clause]
                    [WINDOW window_def {',' window_def}]
                    [QUALIFY expression]
                    SELECT [DISTINCT] select_list


pivot_query    ::= PIVOT from_clause
                   ON pivot_key_list IN '(' pivot_value {',' pivot_value} ')'
                   USING pivot_measure {',' pivot_measure}
                   [GROUP BY (expression_list | ALL)]


group_by_clause ::= GROUP BY (expression_list | ALL) [HAVING expression]

pivot_key_list ::= expression {',' expression}

pivot_value    ::= pivot_scalar_value [[AS] select_alias_name]
                 | '(' pivot_scalar_value {',' pivot_scalar_value} ')' [[AS] select_alias_name]

pivot_scalar_value ::= literal

pivot_measure  ::= identifier {'.' identifier} '(' [DISTINCT] [arg_list | '*'] ')' [[AS] select_alias_name]

unpivot_query  ::= UNPIVOT from_clause
                   ON identifier IN '(' unpivot_entry {',' unpivot_entry} ')'
                   USING identifier
                   [KEEP unpivot_keep {',' unpivot_keep}]


simple_reference ::= identifier {'.' identifier}

unpivot_entry  ::= simple_reference [[AS] select_alias_name]
                 | expression [AS] select_alias_name

unpivot_keep   ::= simple_reference [[AS] select_alias_name]
                 | expression [AS] select_alias_name
```

### 24.4 FROM Clause Grammar

```ebnf
from_clause    ::= single_source | multi_source

single_source  ::= named_source [alias]
                 | schema_source [alias]
                 | function_source [alias]
                 | row_method_source [alias]
                 | row_property_source [alias]
                 | derived_source
                 | values_source

-- A query block containing JOIN/APPLY uses this production. The first source
-- and every right operand must therefore have a stable addressable name.
multi_source   ::= stable_source {join_or_apply}

named_source   ::= cte_reference | in_memory_reference

explicit_source ::= schema_source
                  | function_source
                  | row_method_source
                  | row_property_source

stable_source  ::= named_source [alias]
                 | explicit_source alias
                 | derived_source
                 | values_source

cte_reference  ::= identifier
in_memory_reference ::= identifier

schema_source  ::= identifier '.' identifier '(' [source_arg_list] ')'

function_source ::= function_call
function_call ::= identifier {'.' identifier} '(' [arg_list] ')'
method_call ::= identifier '(' [arg_list] ')'
property_path ::= identifier {'.' identifier}

row_method_source ::= identifier '.' method_call

row_property_source ::= identifier '.' property_path

derived_source ::= '(' (set_operators | cte_expression) ')' alias

source_arg_list ::= source_arg {',' source_arg}

source_arg     ::= expression
                 | identifier ':' expression

-- after the first named source_arg, only named source_arg forms may follow

values_source  ::= VALUES '{' values_row {',' values_row} [','] '}' alias

values_row     ::= '(' values_field {',' values_field} [','] ')'

values_field   ::= identifier ':' values_expression

record_literal ::= '(' named_field {',' named_field} [','] ')'
named_field    ::= identifier ':' expression
array_literal  ::= ARRAY '{' [ expression {',' expression} [','] ] '}'

values_expression ::= literal
                    | script_reference
                    | values_expression arithmetic_op values_expression
                    | '(' values_expression ')'

arithmetic_op  ::= '+' | '-' | '*' | '/' | '%'

alias          ::= [AS] alias_name

alias_name     ::= identifier | bracketed_identifier

bracketed_identifier ::= '[' any_text ']'

join_or_apply  ::= join_clause | apply_clause

join_clause    ::= [INNER] JOIN stable_source ON expression
                 | LEFT [OUTER] JOIN stable_source ON expression
                 | RIGHT [OUTER] JOIN stable_source ON expression
                 | FULL [OUTER] JOIN stable_source ON expression
                 | SEMI JOIN stable_source ON expression
                 | ANTI JOIN stable_source ON expression
                 | CROSS JOIN stable_source
                 | ASOF JOIN stable_source ON asof_condition [tie_break_clause]
                 | ASOF LEFT [OUTER] JOIN stable_source ON asof_condition [tie_break_clause]

asof_condition ::= asof_expr { AND asof_expr }

asof_expr      ::= expression '=' expression
                 | expression inequality_op expression

inequality_op  ::= '>=' | '>' | '<=' | '<'

tie_break_clause ::= TIE BREAK BY ordered_field

apply_clause   ::= CROSS APPLY stable_source [WITH ORDINALITY]
                 | OUTER APPLY stable_source [WITH ORDINALITY]
```

### 24.5 SELECT List Grammar

```ebnf
select_list    ::= select_item {',' select_item}

select_item    ::= star_expr
                 | expression [[AS] select_alias_name]

star_expr      ::= ('*' | identifier '.' '*') [star_modifiers]

star_modifiers ::= [like_modifier] [exclude_modifier] [replace_modifier] [rename_modifier]

like_modifier  ::= LIKE string_literal
                 | NOT LIKE string_literal

exclude_modifier ::= EXCLUDE '(' identifier {',' identifier} ')'

replace_modifier ::= REPLACE '(' replace_item {',' replace_item} ')'

replace_item   ::= expression AS identifier

rename_modifier ::= RENAME '(' rename_item {',' rename_item} ')'

rename_item   ::= identifier {'.' identifier} AS identifier

select_alias_name ::= identifier
                    | string_literal
                    | bracketed_identifier
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
                 | coalesce_expr [NOT] LIKE coalesce_expr
                 | coalesce_expr [NOT] RLIKE coalesce_expr
                 | predicate_quantifier [NOT] LIKE coalesce_expr
                 | predicate_quantifier [NOT] RLIKE coalesce_expr
                 | coalesce_expr [NOT] BETWEEN coalesce_expr AND coalesce_expr
                 | coalesce_expr CONTAINS '(' expression_list ')'

subquery       ::= '(' set_operators ')'

quantified_subquery ::= (ANY | SOME | ALL) '(' set_operators ')'

predicate_quantifier ::= quantifier_name '(' nonempty_expression_list ')'

quantifier_name ::= 'any' | 'all'

nonempty_expression_list ::= expression {',' expression}

comp_op        ::= '=' | '<>' | '!=' | '>' | '>=' | '<' | '<='

coalesce_expr  ::= bitwise_expr ['??' coalesce_expr]

add_expr       ::= mul_expr {('+'|'-') mul_expr}
bitwise_expr   ::= shift_expr {('&'|'|'|'^') shift_expr}
shift_expr     ::= add_expr {('<<'|'>>') add_expr}
mul_expr       ::= unary_expr {('*'|'/'|'%') unary_expr}
unary_expr     ::= ['-'] cast_expr

cast_expr      ::= primary { '[' expression ']' | '.' identifier | '::' cast_type_name }

cast_type_name ::= identifier

primary        ::= record_literal
                 | array_literal
                 | literal
                 | script_reference
                 | identifier {'.' identifier} ['(' [call_arguments] ')'] [filter_clause] [window_over]
                 | identifier '[' expression ']'
                 | '(' expression ')'
                 | subquery
                 | case_expression

script_reference ::= '$' identifier

call_arguments ::= arg_list | aggregate_star_argument
aggregate_star_argument ::= '*'

filter_clause  ::= FILTER '(' WHERE expression ')'

case_expression ::= searched_case_expression
                  | simple_case_expression

searched_case_expression ::= CASE when_clause+ [ELSE expression] END

simple_case_expression ::= CASE expression simple_when_clause+ [ELSE expression] END

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

string_literal ::= ordinary_string_literal | raw_string_literal

ordinary_string_literal ::= "'" {char | escape_seq} "'"

raw_string_literal ::= ("r" | "R") "'" {raw_char | "''"} "'"

raw_char       ::= any source character except "'"

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
enum_declaration ::= [FLAGS] ENUM identifier ':' enum_backing_type
                     '{' enum_member_list '}'

enum_backing_type ::= BYTE | SBYTE | SHORT | USHORT
                    | INT | UINT | LONG | ULONG

enum_member_list ::= enum_member {',' enum_member} [',']

enum_member ::= identifier '=' integral_literal

table_definition ::= TABLE identifier '{' column_def_list '}'

column_def_list ::= column_def { ',' column_def } [',']

column_def     ::= identifier ':' type_name ['?'] {read_modifier}

read_modifier ::= 'encoding' string_literal | 'culture' string_literal
                | 'format' string_literal | 'trim' | 'source' identifier string_literal

type_name      ::= identifier
                  | qualified_type_name

qualified_type_name ::= identifier { '.' identifier }

couple_statement ::= COUPLE couple_schema_source WITH couple_option { AND couple_option } AS identifier

couple_schema_source ::= identifier '.' identifier

couple_option ::= TABLE identifier
                | SETTINGS identifier

desc_statement ::= DESC desc_target [column_clause]
                 | DESC ARGUMENTS desc_target
                 | DESC FUNCTIONS desc_function_target
                 | DESC SETTINGS desc_settings_target
                 | DESC QUERY '(' (set_operators | cte_expression) ')'

desc_target ::= identifier
              | identifier '.' identifier
               | identifier '.' identifier '(' [source_arg_list] ')'

desc_function_target ::= identifier
                       | identifier '.' identifier
                       | identifier '.' identifier '(' [arg_list] ')'

desc_settings_target ::= identifier
                       | identifier '.' identifier
                        | identifier '.' identifier '(' [source_arg_list] ')'

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

Window functions are recognized at the expression level — when a function call is followed by the `OVER` keyword, it is bound as a window expression.

**Supported function names** (case-insensitive, underscores ignored):

| Ranking | Offset | Value Access | Aggregate |
|---------|--------|--------------|-----------|
| `RowNumber()` / `ROW_NUMBER()` | `Lag(expr [, offset [, default]])` | `FirstValue(expr)` / `FIRST_VALUE(expr)` | `Sum(expr)` |
| `Rank()` / `RANK()` | `Lead(expr [, offset [, default]])` | `LastValue(expr)` / `LAST_VALUE(expr)` | `Count(expr)` |
| `DenseRank()` / `DENSE_RANK()` | | `NthValue(expr, n)` / `NTH_VALUE(expr, n)` | `Avg(expr)` |
| `PercentRank()` / `PERCENT_RANK()` | | | `Min(expr)` |
| `CumeDist()` / `CUME_DIST()` | | | `Max(expr)` |
| `Ntile(n)` / `NTILE(n)` | | | |

---

### 24.10 Shared grammar atoms

```ebnf
expression_list ::= expression {',' expression}
arg_list       ::= expression_list
order_list     ::= ordered_field {',' ordered_field}
integer        ::= ['-'] digit {digit}
integral_literal ::= ['-'] integer_literal [type_suffix]
                   | hex_literal | binary_literal | octal_literal
identifier     ::= identifier_start {identifier_continue}
identifier_start ::= Unicode letter | '_'
identifier_continue ::= Unicode letter | Unicode decimal digit | '_'
digit          ::= '0'..'9'
hex_digit      ::= digit | 'a'..'f' | 'A'..'F'
octal_digit    ::= '0'..'7'
```

Bracketed identifiers may replace unquoted identifiers in identifier positions;
their content extends to the closing bracket and may include spaces and reserved
words. Comments and whitespace are trivia between tokens unless a lexical rule
requires adjacency (for example raw literal prefixes). Integral enum literals
must satisfy the backing type's range and integral-suffix restriction (§3.8).
Indexing syntax is supported for declared columns, properties and script references;
brackets directly following a scalar literal are not supported.

An aggregate star argument is a complete argument list, not a scalar expression.
Binding accepts it for `Count` and registered argumentless aggregates; other
callables (including value-only aggregates such as `Sum`) reject it. A star
cannot be mixed with positional or named arguments. `DESC FUNCTIONS` remains
positional-only; concrete `DESC` and `DESC SETTINGS` use source argument rules.

---

## 25. Appendices

### Appendix A: Complete Keyword List

```
ALL, AND, ANY, AS, ASC, ASOF, ASOF JOIN, ASOF LEFT JOIN, ASOF LEFT OUTER JOIN,
BREAK, CASE, COLUMN, CONTAINS, COUPLE, CROSS APPLY, DESC, DESC QUERY, DISTINCT,
ELSE, END, ENUM, EXCEPT, EXISTS, FALSE, FILTER, FIRST, FLAGS, FROM, FUNCTIONS, GROUP BY, HAVING, IN,
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

### Appendix C: String Literal Reference

#### C.1 Ordinary Escape Sequences

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

Ordinary strings decode the supported sequences. Unknown escapes such as `\q`
remain literal. A fixed-length `\u` or `\x` sequence with too few hexadecimal
digits is malformed and produces `MQ1004` while preserving the source text.

#### C.2 Raw String Literals

| Syntax | Meaning |
|--------|---------|
| `r'...'` | Raw literal with a lowercase prefix |
| `R'...'` | Raw literal with an uppercase prefix |
| `''` inside a raw literal | One embedded single quote |

Raw literals preserve backslashes, whitespace, Unicode, separators, glob
characters, brackets, braces, punctuation, and other content exactly. The
prefix must be adjacent to the opening quote, and a backslash never escapes a
quote in a raw literal.

#### C.3 Suspicious Ordinary-String Path Escapes

`MQ5014_SuspiciousOrdinaryStringEscape` is advisory. It does not change the
ordinary literal's decoded value. It is reported for high-confidence rooted
paths during lexical parsing and for relative paths only after semantic
binding identifies a path-sensitive string parameter or variable. Because
user intent cannot always be inferred, an ordinary string in an unknown
context may remain warning-free.

Use either a raw literal or doubled backslashes when the path text should be
preserved:

```sql
select r'C:\new\test' as FullPath from values { (Seed: 0) } seed
select 'C:\\new\\test' as FullPath from values { (Seed: 0) } seed
```

Standalone intentional escapes and unknown preserved escapes do not produce
`MQ5014`. Malformed `\u` and `\x` sequences remain `MQ1004` errors.

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
| Inline row sources | `VALUES (...)` | `FROM values { (Field: literalOrStaticExpression) } alias`; scalar params/lets are allowed |
| Script parameters | Vendor-specific | Optional leading `param(name: type = default)` or `params(name: type = default)` block; `param(...)` is canonical; scalar refs use `$name`, collection params use `type[]` and `IN $name` |
| Script variables | Host-language or dialect-specific variables | `let name: type = constantExpression`; references use `$name` |
| Pagination | `OFFSET n LIMIT m` | `SKIP n TAKE m` |
| Not-equal | `<>` and `!=` | Both `<>` and `!=` are supported |
| CASE WHEN ELSE | ELSE optional | ELSE optional; implicit NULL |
| Simple CASE | `CASE expr WHEN value THEN ...` | Supported |
| Set operations | Omitted keys compare all projected values | Omitted keys and `()` compare all projected values; explicit key lists such as `UNION (key_columns)` compare a subset |
| Recursive CTEs | Supported | Supported with one anchor and one recursive member; see §14.9 for restrictions |
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

The `ELSE` clause is optional in searched and simple CASE expressions. Omitting
it is equivalent to `ELSE NULL`: an unmatched CASE returns NULL, and nullable
result typing is inferred in the same way as the explicit form. UNKNOWN WHEN
conditions do not match. Only the selected result expression is evaluated.
Every CASE still requires at least one WHEN branch, THEN, and END.

```sql
-- Explicit NULL fallback
select case when x > 0 then 'positive' else null end from values { (x: 1), (x: -1) } numbers
-- Equivalent implicit NULL fallback
select case when x > 0 then 'positive' end from values { (x: 1), (x: -1) } numbers
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
from values {
    (Population: 100d),
    (Population: 200d),
    (Population: 200d),
    (Population: 400d)
} rows
```

Simple CASE is also supported:

```sql
select
    case Population
        when 100 then 'small'
        when 1000 then 'large'
        else 'other'
    end
from values {
    (Population: 100d),
    (Population: 200d),
    (Population: 200d),
    (Population: 400d)
} rows
```

CASE expressions can be nested:

```sql
select
    case when x > 0 then
        case when x > 100 then 'very large' else 'moderate' end
    else 'non-positive'
    end
from values { (x: 101), (x: 1), (x: -1) } numbers
```

CASE can appear in SELECT, WHERE, GROUP BY, ORDER BY, and HAVING:

```sql
-- In GROUP BY
select case when Population >= 500 then 'big' else 'small' end, Count(1)
from values {
    (City: 'WARSAW', Population: 100d),
    (City: 'BERLIN', Population: 200d),
    (City: 'WARSAW', Population: 200d),
    (City: 'MUNICH', Population: 400d)
} rows
group by case when Population >= 500 then 'big' else 'small' end

-- In arithmetic
select 1 + (case when 2 > 1 then 1 else 0 end) - 1 from values { (Seed: 0) } seed

-- Short-circuit evaluation: only the matching branch is evaluated
select case when City <> 'X' then 'safe' else 'unreachable' end from values { (City: 'WARSAW') } places
```

Static `CASE` conditions and coalesce operands are inspected during binding.
`MQ5008_UnreachableCode` reports a false or null branch, a duplicate
deterministic condition/label, an unreachable tail after a proven-true branch,
or a fallback hidden by a statically non-null left operand. Nullable, dynamic,
and uncertain values remain quiet; branch evaluation and short-circuit
semantics are unchanged.

<a id="appendix-g-runtime-v2-cast-and-grouping-quick-reference"></a>

### Appendix G: Cast and Grouping Quick Reference

Bare double-colon numeric grouping shorthand is invalid. The `::` token is postfix cast syntax only:

```sql
select Population::Int32 from values {
    (Population: 100d),
    (Population: 200d),
    (Population: 200d),
    (Population: 400d)
} rows
select (Population + 1)::Decimal from values {
    (Population: 100d),
    (Population: 200d),
    (Population: 200d),
    (Population: 400d)
} rows
```

Use `GROUP BY` ordinals or aliases for projection-based grouping references:

```sql
select City, Count(*) from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} rows group by 1
select City as c, Count(*) from values {
    (City: 'WARSAW'),
    (City: 'BERLIN'),
    (City: 'WARSAW'),
    (City: 'MUNICH')
} rows group by c
```

Quick rules:
- `expr::TypeName` performs a strict cast to a supported Musoq type alias or primitive keyword; aliases map to canonical logical types.
- `expr::string` formats non-null values with canonical formatting (§3.9) and preserves null as null.
- Bare double-colon grouping references and bare cast targets are invalid syntax.
- Direct positive integer literals in `GROUP BY` are SELECT-list ordinals.
- `GROUP BY ALL` infers every non-aggregate, non-window SELECT expression after projection expansion.
- Non-aggregate SELECT aliases are visible in `WHERE` and `GROUP BY`; aggregate SELECT aliases are visible in `HAVING`.


### Appendix H: Generic Source Contracts Used in Examples

These are illustrative contracts, not installed providers or required package
names. A receiving host supplies them only when running examples about sources.
Ordinary expression and relational examples instead declare their input with
VALUES. Syntax fragments containing `...`, `<placeholder>`, or symbolic argument
names describe forms rather than complete executable batches. Consecutive queries
in a block are independent examples unless connected by a declaration or CTE.

| Source | Arguments | Returned shape and behavior |
|--------|-----------|-----------------------------|
| `schema.records` | `()`; overload `(filter: string)`; overload `(enabled: bool, filter: string)` | Typed rows: `Id: int`, `Name: string`, `City: string`, `Country: string`, `Population: decimal`, `Money: decimal`, `Value: int`, `RawData: string`, `Array: int[]`, `Dict: map<string,string>`, `Children: record[]` with element `Name: string`, and `Self: record`. `Self` exposes `Name: string`, `Array: int[]`, `Dictionary: map<string,string>`, `Children: record[]`, and `Other`/`Self` records with the same demonstrated paths. Array example input is `[10,20,30]`; dictionary input contains `key_name` and `key`. Primitive sample row is `(1,'Ada','WARSAW','Poland',100,50)`. |
| `schema.byCountry` | `()`; overload `(country: string)` | Same primitive columns as `schema.records`; returns only rows with matching `Country`, possibly none. Used to demonstrate lateral source arguments and null extension. |
| `schema.collections` | `()` | `Name: string`, `Column: record[]`; element shape `Value: int`; ordered elements `[1,2]`. Also supplies `Children: record[]` with `Name: string` where shown. |
| `schema.files` | `(path: string)` | `Name: string`; returns fixture names for the supplied key. Metadata is independent of that key; no filesystem access is implied. |
| `schema.match` | `(text: string, patterns: (Id: string, Pattern: string, Mode: string = 'literal')[])` | `PatternId: string`; one row per matching pattern, in input-pattern order. Literal mode tests substring membership; examples pass `TODO` and `FIXME`. |
| `schema.arguments` | `(first: int, second: decimal, enabled: bool, archived: bool, label: string)` | One `Value: string` column containing `label`; demonstrates argument types. |
| `schema.method` | `()`; overload `(required: string, limit: int = 10)` | `Value: string`; nonnegative limit bounds rows; no-argument overload also exposes escaped-name columns `case`, `order`, `Column With Spaces`. `arg1`/`arg2` in syntax fragments are placeholders, not undeclared script variables. |
| `schema.dynamicRows` | `(index: int, label: string, amount: decimal)` | TABLE-defined `Id: int?`, `Name: string`, `Amount: decimal?`; used for concrete metadata discovery. |
| `schema.items` | `()` | `Id: int`, `Name: string`; profile `prod` is passed unchanged to the host resolver. Required key `TOKEN`, ordinal comparison, secret, applicable to all source phases. No value is embedded in SQL. |
| `schema.otherRecords` | `()` | `Id: int`, `Value: int`; separately registered `AggregateMethodB()` for the ambiguity example. |

Additional member-source contracts are `schema.texts()` returning
`City: string`, `Text: string`, `Numbers: string`, and `Words: string` (sample
`Text = 'one two'`, `Numbers = '1,2'`, `Words = 'one two'`);
`schema.values()` returning `City: string`, `Values: int[]`, and
`ComplexType.PrimitiveValues: int[]` (sample arrays `[1,2]`); and
`schema.nestedValues()` returning `Values: record[]`, with two more record levels
exposing `Values`, ending in primitive integer elements. Collections are ordered.
The methods `Split(string,string) -> string[]`, `ToCharArray(string) -> char[]`,
`Skip(collection,int) -> collection` and `Take(collection,int) -> collection`
are registered for the corresponding source aliases in those callable examples.

Parent-level aggregate examples assume `SumIncome(decimal, int) -> decimal` and
`SumOutcome(decimal, int) -> decimal`, with the parent-depth argument following
§10.4. These are registered aggregate contracts.

The indexed-property example additionally publishes `Inc(int) -> int`, returning
its argument plus one. Source-function examples publish `GetPopulation() -> decimal` and
`ToUpperInvariant(string) -> string`. `SpecialParse(string) -> string` is registered
only for the left source; `Transform(int) -> int` has distinct registrations on the
two sources. The aggregate-owner example registers distinct argumentless
`AggregateMethodB` functions on both sources. The custom-window example assumes
`RunningProduct(decimal) -> decimal`, multiplying values in its effective frame.
These callables illustrate registration and binding; no particular plugin supplies
them as part of the core language contract.
