# Musoq Interpretation Schemas: Language Extension Specification

**Version:** 2.0.0
**Status:** Specification
**Author:** Jakub Puchała

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Design Principles](#2-design-principles)
3. [Core Language Changes](#3-core-language-changes)
4. [Binary Schema Syntax](#4-binary-schema-syntax)
5. [Text Schema Syntax](#5-text-schema-syntax)
6. [Common Elements](#6-common-elements)
7. [Interpretation Functions](#7-interpretation-functions)
8. [Schema Composition](#8-schema-composition)
9. [Error Handling](#9-error-handling)
10. [Grammar Specification](#10-grammar-specification)
11. [Examples](#11-examples)
12. [Future Considerations](#12-future-considerations)
13. [Appendix A: Reserved Keywords](#appendix-a-reserved-keywords)
14. [Appendix B: Type Mapping](#appendix-b-type-mapping)
15. [Appendix C: Error Codes](#appendix-c-error-codes)
16. [Appendix D: CROSS APPLY / OUTER APPLY Behavior](#appendix-d-cross-apply--outer-apply-behavior)

---

## 1. Introduction

Core owns logical types, expression semantics and diagnostics. This profile adds
schema syntax and input-consumption rules. Executable examples use explicit typed byte-array declarations or literal text
rows with VALUES. They require no filesystem or installed datasource.
Declaration-only examples and callable signatures are syntax fragments; complete
query examples include the schemas and input they consume. The
[examples](#11-examples) illustrate the syntax and behavior defined by this profile.


### 1.1 Purpose

Interpretation Schemas extend Musoq's SQL dialect with declarative format definitions for parsing binary and textual data. Schemas serve triple duty as:

1. **Executable format rules** — Evaluated over binary or text input
2. **Format documentation** — Human-readable declarations of parsing behavior
3. **Queryable results** — Parsed schema fields are available through APPLY aliases

### 1.2 Motivation

Data investigation frequently requires correlating structured data across heterogeneous sources. While Musoq excels at querying files, git repositories, and APIs, many real-world data sources encode information in:

- Proprietary binary formats
- Legacy fixed-width text records
- Semi-structured logs with custom patterns
- Mixed binary/text protocols

Current approaches require writing throwaway scripts to parse data before analysis. Interpretation Schemas bring parsing into the query itself, enabling single-query workflows that parse AND analyze AND correlate.

### 1.3 Scope

This specification covers:

- Binary schema definitions for byte-level interpretation
- Text schema definitions for character-level interpretation
- Mixed schemas combining both paradigms
- Integration with existing Musoq syntax (CROSS APPLY, OUTER APPLY, JOINs, CTEs)
- Schema composition and reuse patterns
- Error handling semantics
- Introspection capabilities
- Required modifications to Musoq's CROSS/OUTER APPLY semantics

This specification is part of the Musoq specification family (see *Musoq Core SQL Language Specification*, §1.6). The notation conventions defined in the core specification (§1.5) apply to this document. In particular, the key words MUST, MUST NOT, SHOULD, SHOULD NOT, and MAY carry normative weight when capitalized.

An implementation that claims conformance to the Binary/Text Interpretation profile MUST implement all features defined in this specification. This profile is optional; see the core specification (§1.7) for the conformance model.

### 1.4 Terminology

| Term | Definition |
|------|------------|
| **Schema** | A named format definition describing how to interpret raw data |
| **Field** | A named element within a schema with a type and optional modifiers |
| **Primitive** | Built-in atomic type (`int`, `long`, `short`, etc.) |
| **Substrate** | The raw data being interpreted (byte array or character sequence) |
| **Interpretation** | The act of parsing a substrate according to a schema |
| **Cursor** | Current position within the substrate during parsing |

### 1.5 Design Conventions

| Aspect | Convention | Rationale |
|--------|------------|-----------|
| Types | Familiar names (`int`, `short`, `long`, `byte`) | Familiar naming, portable value domains |
| Operators | SQL operators (`<>`, `AND`, `OR`, `NOT`) | Consistency with Musoq SQL dialect |
| Keywords | Case-insensitive | SQL convention |
| Identifiers | Case-sensitive for field names | Matches core field-name rules |
| Schema references | Type name without parentheses | SQL convention for type references |

### 1.6 Relationship to Existing Musoq Constructs

This extension follows patterns established by Musoq's existing `table` definition syntax:

```sql
-- Existing Musoq table definition
table Invoice {
    ProductName: string,
    Price: decimal
};

-- New binary schema definition (analogous structure)
binary Header {
    Magic: int le,
    Version: short le
}
```

Both define named structures that can be referenced in queries. The key difference: `table` couples external schemas to unknown data sources, while `binary`/`text` schemas define how to interpret raw bytes or text.

---

## 2. Design Principles

### 2.1 Declarative Over Imperative

Schemas describe **what** data looks like, not **how** to parse it. The engine determines optimal parsing strategy. This aligns with SQL's declarative nature—you specify the desired result, not the execution plan.

```sql
-- Good: Declarative structure
binary Header {
    Magic:   int le,
    Version: short le,
    Length:  int le
}

-- Not supported: Imperative parsing
-- read 4 bytes into Magic
-- if Magic <> 0xDEADBEEF then fail
```

### 2.2 Schemas Are Documentation

Schema definitions should read as format specifications. Comments are first-class, preserved for introspection and documentation generation.

```sql
binary CustomerRecord {
    -- Magic number identifying record type
    -- Must be 0x43555354 ('CUST' in ASCII)
    Magic:        int le,

    -- Format version for backward compatibility
    -- Current version: 3
    Version:      short le,

    -- Customer identifier (unique, sequential)
    CustomerId:   long le
}
```

### 2.3 Composition Through References

Complex formats are built by composing simpler schemas. Nesting and arrays are natural. This mirrors SQL's use of named types and table references.

```sql
binary Point { X: float le, Y: float le }

binary Color { R: byte, G: byte, B: byte, A: byte }

binary Vertex {
    Position: Point,
    Color:    Color,
    Uv:       Point
}

binary Mesh {
    VertexCount: int le,
    Vertices:    Vertex[VertexCount]
}
```

### 2.4 Fail-Fast with Clear Diagnostics

Parsing failures should be immediate, specific, and actionable. The error should identify exactly where and why parsing failed.

<a id="25-zero-runtime-reflection"></a>

### 2.5 Execution Strategy

Execution strategy is implementation-defined. Implementations MAY compile or
interpret schemas and MUST preserve the values, cursor movement and diagnostics
defined here. No native type-inspection or code-generation mechanism is required.

### 2.6 SQL Spirit Alignment

The extension uses SQL-compatible constructs where possible:

| Concept | SQL Parallel | Schema Syntax |
|---------|--------------|---------------|
| Constraints | `CHECK` constraint | `check` clause |
| Conditional | `CASE WHEN` | `when` clause |
| Type reference | `CAST(x AS type)` | `Interpret<Type>(data)` |
| Null semantics | Core NULL rules | Conditional fields yield `null` |

---

## 3. Core Language Changes

### 3.1 Schema Definition Placement

Schemas MUST be defined before any query that references them. Schemas are scoped to the query batch in which they are defined.

```sql
-- Schema definitions come first
binary Header {
    Magic: int le,
    Version: short le
}

binary Record {
    Id: int le,
    Value: double le
}

-- Query follows schema definitions
let content: byte[] = array { 120ub, 86ub, 52ub, 18ub, 1ub, 0ub, 7ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 248ub, 63ub };
SELECT h.Magic, r.Id, r.Value
FROM values { (Seed: 0) } seed
CROSS APPLY Interpret<Header>($content) h
CROSS APPLY InterpretAt<Record>($content, 6) r
```

**Scope Rules:**
- Schemas are visible only within the query batch where defined
- Schema names MUST be unique within a batch
- Forward references between schemas MUST NOT occur (referenced schema MUST be defined first)
- Recursive schema definitions MUST NOT occur

### 3.2 CROSS APPLY / OUTER APPLY Relaxation

APPLY accepts ordered collections and single structured values:

| Right-hand value | CROSS APPLY | OUTER APPLY |
|------------------|-------------|-------------|
| Collection of N elements | N rows; zero if empty | N rows; one null-extended row if empty |
| Non-null structured value | One row exposing its fields | One row exposing its fields |
| Null | Zero rows | One null-extended row |

Interpret and Parse produce one structured value, not a collection. Nested array
fields expand through APPLY using core §9. These cardinalities do not require
wrapping a value in an actual host collection.

### 3.3 APPLY-Only Restriction

All interpretation functions — `Interpret<Schema>()`, `Parse<Schema>()`, `TryInterpret<Schema>()`, `TryParse<Schema>()`, `InterpretAt<Schema>()`, `PartialInterpret<Schema>()`, and `PartialParse<Schema>()` — MUST appear as the right-hand side of a `CROSS APPLY` or `OUTER APPLY` clause. They MUST NOT be used directly in `SELECT`, `WHERE`, `HAVING`, or other expression contexts.

**Valid usage:**

```sql
binary Header { Magic: int le, Version: short le };
let content: byte[] = array { 120ub, 86ub, 52ub, 18ub, 1ub, 0ub };
SELECT h.Magic, h.Version FROM values { (Seed: 0) } seed CROSS APPLY Interpret<Header>($content) h
```

```sql
text LogEntry { Timestamp: between '[' ']', _: literal ' ', Level: until ':', _: literal ' ', Message: rest };
SELECT log.Timestamp, log.Level FROM values { (Line: '[2024-01-02] INFO: ready'), (Line: 'malformed') } lines OUTER APPLY TryParse<LogEntry>(lines.Line) log
```

**Invalid usage (compile-time error MQ3033):**

```sql
text LogEntry { Timestamp: between '[' ']', _: literal ' ', Level: until ':', _: literal ' ', Message: rest };
binary Header { Magic: int le, Version: short le };
let content: byte[] = array { 120ub, 86ub, 52ub, 18ub, 1ub, 0ub };
-- ERROR MQ3033: Parse cannot appear in SELECT
SELECT Parse<LogEntry>(line.Line) FROM values { (Line: '[2024-01-02] INFO: ready') } line

-- ERROR MQ3033: TryInterpret cannot appear in WHERE
SELECT 1 FROM values { (Seed: 0) } seed WHERE TryInterpret<Header>($content) IS NOT NULL
```

**Rationale:**
Interpretation functions return structured objects whose fields MUST be bound to an alias through APPLY. Without an alias, there is no way to reference individual fields of the parsed result. The APPLY clause provides the necessary scoping and alias binding.

**Note:** Inline property access (e.g., `Interpret<Header>(data).Magic` in SELECT) is reserved for a future version. See §12.1.

---

## 4. Binary Schema Syntax

### 4.1 Schema Declaration

```
BinarySchema ::= 'binary' Identifier '{' BinaryFieldList '}'

BinaryFieldList ::= BinaryField (',' BinaryField)* ','?
```

**Field Separator Rules:**
- Fields are separated by commas
- Trailing comma after the last field is OPTIONAL
- Newlines are NOT significant (formatting only)

Example:

```sql
binary PacketHeader {
    Magic:      int le,
    Version:    short be,
    Flags:      byte,
    Length:     int le,
    Checksum:   int le
}
```

### 4.2 Primitive Types

#### 4.2.1 Integer Types

| Type | Endianness | Size | Logical type | Notes |
|------|------------|------|-----------|-------|
| `byte` | n/a | 1 | `byte` | Endianness not applicable |
| `sbyte` | n/a | 1 | `sbyte` | Endianness not applicable |
| `short le` | little-endian | 2 | `short` | Endianness REQUIRED |
| `short be` | big-endian | 2 | `short` | Endianness REQUIRED |
| `ushort le` | little-endian | 2 | `ushort` | Endianness REQUIRED |
| `ushort be` | big-endian | 2 | `ushort` | Endianness REQUIRED |
| `int le` | little-endian | 4 | `int` | Endianness REQUIRED |
| `int be` | big-endian | 4 | `int` | Endianness REQUIRED |
| `uint le` | little-endian | 4 | `uint` | Endianness REQUIRED |
| `uint be` | big-endian | 4 | `uint` | Endianness REQUIRED |
| `long le` | little-endian | 8 | `long` | Endianness REQUIRED |
| `long be` | big-endian | 8 | `long` | Endianness REQUIRED |
| `ulong le` | little-endian | 8 | `ulong` | Endianness REQUIRED |
| `ulong be` | big-endian | 8 | `ulong` | Endianness REQUIRED |

Signed binary integers decode using two's complement; floating fields decode
IEEE 754 binary32/binary64 bit patterns without numeric text conversion. Schema
field types have the corresponding logical core type; endianness affects byte
decoding, not logical identity.

**Endianness Rules:**
- Single-byte types (`byte`, `sbyte`): Endianness is not applicable (a single byte has no byte order)
- Multi-byte types: Endianness MUST be specified (`le` or `be`)
- Omitting endianness for multi-byte types is a parse error

#### 4.2.2 Floating-Point Types

| Type | Endianness | Size | Logical type |
|------|------------|------|-----------|
| `float le` | little-endian | 4 | `float` |
| `float be` | big-endian | 4 | `float` |
| `double le` | little-endian | 8 | `double` |
| `double be` | big-endian | 8 | `double` |

#### 4.2.3 Byte Arrays

```
ByteArrayType ::= 'byte' '[' SizeExpression ']'

SizeExpression ::= IntegerLiteral | FieldReference | Expression
```

**Size Expression Evaluation:**
- Evaluated at parse time using values of previously parsed fields
- MUST evaluate to a non-negative integer for meaningful data
- Negative values fail fast with diagnostic ISE0007

Examples:

```sql
binary Example {
    FixedData:    byte[16],              -- Fixed 16 bytes
    Length:       int le,
    DynamicData:  byte[Length],          -- Size from previous field
    Computed:     byte[Length * 2]       -- Computed size
}
```

#### 4.2.4 String Types

```
StringType ::= 'string' '[' SizeExpression ']' Encoding StringModifiers?

Encoding ::= 'utf8' | 'utf16le' | 'utf16be' | 'ascii' | 'latin1' | 'ebcdic'

StringModifiers ::= StringModifier+

StringModifier ::= 'trim' | 'rtrim' | 'ltrim' | 'nullterm'
```

**Encoding Semantics:**

| Encoding | Bytes per Char | Description |
|----------|----------------|-------------|
| `utf8` | Variable (1-4) | UTF-8, size is in BYTES not chars |
| `utf16le` | 2 per code unit | UTF-16 Little Endian |
| `utf16be` | 2 per code unit | UTF-16 Big Endian |
| `ascii` | 1 | 7-bit ASCII (values 0-127) |
| `latin1` | 1 | ISO-8859-1 |
| `ebcdic` | 1 | IBM EBCDIC (Code Page 037) |

**Modifier Application Order:**
1. Reserve and consume the entire declared byte range.
2. For `nullterm`, select the prefix before the first zero byte, or the first
   aligned zero code unit for UTF-16. Bytes after that terminator are ignored.
3. Strictly decode the selected bytes using the specified encoding.
4. Apply trim modifiers. Binary trim removes Unicode whitespace (core §3.9
   categories Zs/Zl/Zp plus U+0009–000D and U+0085), not U+0000.

Malformed UTF-8, incomplete sequences, invalid UTF-16 surrogate pairs, odd UTF-16
byte counts and non-ASCII bytes in ASCII fields report ISE0010. Decoding never
substitutes replacement characters. Latin1 maps every byte to the corresponding
U+0000–00FF value; EBCDIC uses IBM code page 037. A BOM is decoded as data unless
the schema explicitly discards it. Terminator padding is not decoded or validated.

| Modifier | Behavior |
|----------|----------|
| `trim` | Remove leading AND trailing whitespace |
| `rtrim` | Remove trailing whitespace only |
| `ltrim` | Remove leading whitespace only |
| `nullterm` | String ends at first null character; remaining bytes consumed but ignored |

Examples:

```sql
binary Record {
    Name:     string[32] utf8,              -- UTF-8, 32 bytes
    Code:     string[8] ascii trim,         -- ASCII, trimmed
    Legacy:   string[80] ebcdic,            -- EBCDIC mainframe text
    Label:    string[64] utf8 nullterm      -- Null-terminated within 64-byte buffer
}
```

### 4.3 Nested Schemas

Fields may reference other schema types by name. The referenced schema MUST be defined before the referencing schema.

```sql
binary Inner {
    X: int le,
    Y: int le
}

binary Outer {
    Header:  Inner,          -- Embedded schema (parsed inline)
    Count:   int le,
    Items:   Inner[Count]    -- Array of schemas
}
```

**Semantics:**
- Nested schemas are parsed inline at the current cursor position
- The cursor advances by the total size of the nested schema
- Nested schema fields are accessible via dot notation: `outer.Header.X`
- Member and index paths retain their bound source through APPLY, filters and
  CTE projections. A member with the same name as a source alias, including a
  different casing, remains a member segment rather than a source prefix.
- Indexing a collection through a record, such as `outer.Body.Items[1].Value`,
  consumes one collection dimension. Subsequent member binding uses the element
  record shape; indexing a primitive array yields its primitive element type.
  Each index is retained when several appear in one path, for example
  `outer.Items[1].Body.Values[0]`. Conditional records inside those indexed
  elements preserve the same NULL propagation as non-indexed records.
- A conditional leaf retains its nullable logical type through any number of
  record members. If an intermediate record is absent, projection of a member
  through that path yields `null`, rather than dereferencing the absent record.
  This also applies after selecting a switch branch, expanding a fixed or dynamic
  array in APPLY, and projecting a record or collection through CTEs. An absent
  conditional record inside an existing array element preserves that element's
  row and gives its primitive leaves nullable output types.
- Collection paths in APPLY have no fixed member-depth limit. A null path or an
  empty collection produces zero CROSS APPLY rows and one null-extended OUTER
  APPLY row, as defined in Appendix D.

### 4.4 Arrays

```
ArrayType ::= Type '[' SizeExpression ']'
```

Arrays may be of any type: primitives, strings, bytes, or schemas.

```sql
binary SubRecord { Id: byte };
binary Example {
    Count:      int le,
    Ids:        long le[Count],              -- Array of integers
    Names:      string[32] utf8[Count],      -- Array of fixed strings
    Records:    SubRecord[Count]             -- Array of nested schemas
}
```

**Array Semantics:**
- Elements are parsed sequentially at the current cursor position
- Each element advances the cursor by its size
- Empty arrays (Count = 0) are valid and produce an empty collection
- Arrays are ordered collections for CROSS APPLY iteration
- For `byte[width][count]`, the first size is each byte-array element's width and
  the second is the element count. Elements occupy consecutive ranges; zero count
  consumes no bytes. Projecting a nested primitive array preserves its element
  type rather than the containing record type.

### 4.5 Conditional Fields

Fields may be conditional based on previously parsed values:

```
ConditionalField ::= Identifier ':' Type 'when' Expression
```

**The `when` clause** uses SQL's WHEN keyword, consistent with CASE expressions.

Examples:

```sql
binary Message {
    MsgType:    byte,
    HasPayload: byte,

    -- Only present when HasPayload is non-zero
    PayloadLen: int le when HasPayload <> 0,
    Payload:    byte[PayloadLen] when HasPayload <> 0,

    -- Type-based conditions
    ErrorCode:  short le when MsgType = 0xFF
}
```

**Semantics:**
- Condition is evaluated using previously parsed field values
- Guards require a boolean expression and use core three-valued logic. Comparisons
  with absent fields yield UNKNOWN, including through computed-field dependencies.
- When condition is FALSE or UNKNOWN: field is NOT parsed, cursor does NOT advance,
  field value is `null`, and its size, position and validations are not evaluated
- When condition is TRUE: field is parsed normally
- Conditions may only reference fields declared BEFORE the conditional field
- Subsequent fields referencing a conditional field MUST handle potential `null`

**Presence and expressions:**
An absent field has a null value. Expressions referencing it follow core 2.0
three-valued Boolean, CASE, lossless numeric coalescing, and left-to-right lazy
evaluation rules. A reference to an absent field does not force its enclosing
expression to be null: CASE and `??` may provide a non-null result.

```sql
binary Example {
    HasData: byte,
    Length: int le when HasData <> 0,
    -- Absent when HasData is zero; Length is not evaluated as a size.
    Absent: byte[Length] when HasData <> 0,
    -- Present and empty when Length is null.
    Empty: byte[Length ?? 0]
}
```

Without a guard or explicit fallback, `byte[Length]` fails with ISE0007 when
Length is null. Static validation applies to all declarations and expression
branches, including branches whose runtime evaluation is skipped.

### 4.6 Computed Fields

Fields that derive values without consuming input:

```
ComputedField ::= Identifier ':' '='? Expression
```

Computed fields use the same `:` syntax as parsed fields. The parser distinguishes them by what follows the colon:

- **Type specification** (keyword, schema name, array) → Parsed field
- **Expression** (operators, function calls, parenthesized expressions) → Computed field

Examples:

```sql
binary Packet {
    RawFlags:   short le,

    -- Computed from RawFlags, no bytes consumed
    IsCompressed: (RawFlags & 0x01) <> 0,
    IsEncrypted:  (RawFlags & 0x02) <> 0,
    Priority:     (RawFlags >> 4) & 0x0F,

    Length:     int le,
    Data:       byte[Length]
}
```

**Disambiguation Rules:**

| After `:` | Interpretation | Example |
|-----------|----------------|---------|
| Primitive keyword + endianness | Parsed field | `int le`, `short be` |
| `byte[...]`, `string[...]`, `bits[...]` | Parsed field | `byte[16]`, `string[32] utf8` |
| Known schema name | Parsed field | `HeaderSchema` |
| `substream[...]` + `raw`/`as` | Parsed field | `substream[Length] as Body` |
| Expression with operators | Computed field | `(Flags & 1) <> 0` |
| Function call | Computed field | `Crc32(Data)` |
| Parenthesized expression | Computed field | `(Count)` |

Computed aliases of a parsed record retain its schema type. A CASE whose non-NULL
branches reference the same record type retains that type as well; a NULL branch
permits an absent record without erasing its members. Indexing a primitive array
in a schema expression retains its element type, including when the value feeds
a subsequent field size.
Member lookup follows the receiver's type. A scalar or array member such as
`Raw.Length` cannot resolve to a sibling schema field named `Length`.
Nullability inference obeys the same declaration-order and receiver rules as
value binding. Repeated primitive elements retain their primitive type for
arithmetic and bitwise expressions after indexing.
The same member-type rules apply through a parsed substream record. A possibly
absent computed string lifts member values such as Length to nullable types.
Substring accepts those values at runtime and reports ISE0014 for a NULL index
or length; a guarded CASE does not evaluate the unselected call.

**Semantics:**
- Computed fields do NOT advance the cursor
- Expression may reference any previously declared field (parsed or computed)
- Computed fields are evaluated in declaration order when reached, before following fields
- Type is inferred from the expression

### 4.7 Bit Fields

For sub-byte field access:

```
BitFieldType ::= 'bits' '[' BitCount ']'

BitCount ::= IntegerLiteral  -- 1 to 64
```

**Bit Field Semantics:**

Bit fields are read sequentially from the bit stream:

1. Bits are numbered 0-7 within each byte, where bit 0 is the LEAST significant bit
2. Bit fields are packed sequentially; NO automatic byte alignment between fields
3. Multi-bit fields that cross byte boundaries are supported
4. The first bit field in a schema starts at bit 0 of the current byte
5. Maximum bit field size is 64 bits. The first consumed bit becomes the least
   significant result bit. Result kinds are byte for 1–8 bits, ushort for 9–16,
   uint for 17–32 and ulong for 33–64.
6. Ordinary byte reads align to the next byte when preceded by a partial bit
   field. Explicit align advances to an N-bit boundary measured from the start
   of the current input slice and leaves an already aligned cursor unchanged.

```sql
binary TcpFlags {
    -- Packed into 2 bytes total (16 bits)
    Reserved:  bits[4],    -- bits 0-3 of byte 0
    DataOff:   bits[4],    -- bits 4-7 of byte 0
    Fin:       bits[1],    -- bit 0 of byte 1
    Syn:       bits[1],    -- bit 1 of byte 1
    Rst:       bits[1],    -- bit 2 of byte 1
    Psh:       bits[1],    -- bit 3 of byte 1
    Ack:       bits[1],    -- bit 4 of byte 1
    Urg:       bits[1],    -- bit 5 of byte 1
    Ece:       bits[1],    -- bit 6 of byte 1
    Cwr:       bits[1]     -- bit 7 of byte 1
}
```

**Alignment Directive:**

To force byte alignment after bit fields:

```sql
binary Example {
    Flags:     bits[3],     -- 3 bits
    _:         align[8],    -- Skip to next byte boundary (5 bits padding)
    NextByte:  byte         -- Starts at next byte
}
```

The `align[N]` directive advances to the next N-bit boundary. Common values:
- `align[8]` - byte alignment
- `align[16]` - 16-bit alignment
- `align[32]` - 32-bit alignment

**Compile-time validation:**

Field-size annotations are validated while parsing the schema. Violations raise
diagnostic `MQ4001` (invalid binary schema field) instead of failing at runtime:

- `bits[N]` requires `1 <= N <= 64`.
- `align[N]` requires `N >= 1`.
- Constant `byte[N]` and `Type[N]` array sizes must be non-negative.

### 4.8 Absolute Positioning

Fields may specify absolute offsets:

```
PositionedField ::= Identifier ':' Type 'at' Expression
```

Examples:

```sql
binary PeHeader {
    DosMagic:      string[2] ascii at 0,          -- Always at offset 0
    PeOffset:      int le at 0x3C,                -- PE header pointer at fixed offset
    PeSignature:   string[4] ascii at PeOffset,   -- Dynamic position from field
    Machine:       short le at PeOffset + 4       -- Computed position
}
```

**Semantics:**
- `at` moves the cursor to the ABSOLUTE position before reading
- Position is relative to the start of the current interpretation (byte 0)
- After reading, cursor is at (position + field size)
- Positions may go backward (re-read earlier data) or forward (skip data)
- Position expressions may reference previously parsed fields

### 4.9 Validation Constraints

Fields may include validation using the SQL `CHECK` keyword:

```
ValidatedField ::= Identifier ':' Type 'check' Expression
```

**The `check` clause** mirrors SQL's CHECK constraint syntax.

Examples:

```sql
binary ValidatedHeader {
    Magic:    uint le check Magic = 0xDEADBEEF,
    Version:  short le check Version >= 1 AND Version <= 5,
    Length:   int le check Length <= 1048576,
    Data:     byte[Length],
    Checksum: int le check Checksum = Crc32(Data)
}
```

**Semantics:**
- Field is parsed first, then validation is evaluated
- Failed validation raises a parse error (ISE0002) with field name and constraint
- Validation may reference the current field AND any previously declared field
- Validation expressions must evaluate to boolean
- A binary `check` succeeds only for TRUE; FALSE or UNKNOWN raises ISE0002.
  A skipped conditional field does not evaluate its check.

### 4.9.1 Field Value Validations (`const`, `magic`, `oneOf`)

Fields may declare value validations inline, immediately after the type/repeat
annotation and before any `at`, `when`, or `check` clause:

```
ValueValidatedField ::= Identifier ':' Type ValueValidation? Positioning? Guard? Check?
ValueValidation     ::= ('const' | 'magic') Value
                      | ('const' | 'magic') '[' ByteList ']'
                      | 'oneOf' '[' ValueList ']'
```

**`const` / `magic`** assert the field equals a single expected value. `magic` is
an alias of `const`; both have identical validation behavior and exist so signature/magic-number
fields can read naturally:

```sql
binary PngFile {
    Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
    Width:     int be,
    Height:    int be
}

binary Header {
    Version:  byte const 1,
    Reserved: byte const 0
}
```

**`oneOf`** asserts the field equals one of an explicit, non-empty set of values:

```sql
binary Chunk {
    Length:    int be,
    ChunkType: string[4] ascii oneOf ['IHDR', 'IDAT', 'IEND']
}
```

**Compatible types:**

| Validation | Allowed field types |
|------------|---------------------|
| `const` / `magic` | primitive numeric, `bits`, string, `byte[N]`, raw byte substreams |
| `oneOf` | primitive numeric, `bits`, string |

**Semantics:**
- Validation runs AFTER the field is read and string modifiers (`ascii`,
  trimming, etc.) are applied.
- When the field carries a `when` guard, validation is skipped if the guard is
  false (the field is not read, so nothing is validated).
- A failed validation raises the validation diagnostic ISE0002. With
  `TryInterpret<>` or `TryParse<>` this makes the whole interpretation yield
  `null`; with `Interpret<>` or `Parse<>` it throws and fails query execution.
- `const`/`magic` may coexist with a `check` clause on the same field.

**Compile-time validation:** Malformed value validations raise diagnostic
`MQ4006` (invalid field constraint), including: duplicate validation modifiers,
incompatible field types, an empty `oneOf` list, and byte-list literals outside
`0..255`.

### 4.10 Repetition Until Condition

For variable-length sequences without explicit count:

```
RepeatedField ::= Identifier ':' Type 'repeat' 'until' (Expression | 'eof')
```

Examples:

```sql
binary TlvRecord {
    Type:   byte,
    Length: short le,
    Value:  byte[Length]
}

binary TlvStream {
    -- Read TLV records until type is 0x00 (terminator)
    Records: TlvRecord repeat until Records[-1].Type = 0x00
}
```

**Semantics (`repeat until` Expression):**
- Parses elements repeatedly until condition becomes TRUE
- `Records[-1]` refers to the most recently parsed element
- A computed Boolean member of that record may be the stop condition directly,
  for example `Records[-1].Stop`. Numeric members remain invalid conditions.
- Condition is evaluated AFTER each element is parsed
- At least one element is always attempted (do-while)
- Maximum iteration count is a host-selected safety limit (default: 10,000); exceeding it reports ISE0009

Core callers select this limit with immutable `InterpretationExecutionLimits` and `CompilationOptions.WithInterpretationLimits(...)`:

```csharp
var options = new CompilationOptions()
    .WithInterpretationLimits(new InterpretationExecutionLimits(maxRepeatIterations: 20_000));
var query = InstanceCreator.CompileForExecution(sql, assemblyName, schemaProvider, loggerResolver,
    compilationOptions: options);
```

`MaxRepeatIterations` MUST be positive and defaults to 10,000. It applies independently to each binary or text repeat field, including nested interpreters and substreams. The selected value is compiled into the guard and participates in compilation-cache and stored-artifact identity; no per-element configuration lookup occurs. Stored artifacts require matching compilation options when loaded, including this limit. Fixed-count arrays and fixed-width fields are unaffected. Zero-progress protection, cancellation behavior, ISE0009/MQ7109 classification, and failure positions are unchanged. This Core option does not change CLI settings or recursive CTE limits.


**Repeat until end of input (`repeat until eof`):**

For sequences that fill the remaining input span:

```sql
binary TlvRecord {
    Type:   byte,
    Length: short le,
    Value:  byte[Length]
}

binary Inner {
    -- Read records until the current input span is exhausted
    Records: TlvRecord repeat until eof
}

binary Frame {
    Length:  byte,
    Payload: substream[Length] as Inner,
    Trailer: byte
}
```

**Semantics (`repeat until eof`):**
- Parses elements repeatedly until the current interpreter input boundary is reached
- Zero-or-more: if already at end of input, the result is an empty array (no element is attempted)
- `eof` is recognized only after `repeat until`; it is not a general keyword
- Each element must consume at least one bit of input; a zero-progress element raises a parse error (ISE0009)
- Composes with substreams: inside a `substream`, `eof` is bounded by the substream slice, so the repeat stops at the end of the bounded payload and parsing of the parent resumes at the field that follows the substream

A repeated substream applies its byte bound independently to every element.
Each successful element advances the containing cursor by its declared length,
including bytes skipped in lax mode. The repeat condition sees that element's
parsed value, and following fields resume after the final bounded slice.

### 4.11 Discard Fields

For bytes that must be consumed but not exposed:

```sql
binary Example {
    Magic:    int le,
    _:        byte[4],     -- Skip 4 bytes, unnamed
    _:        short le,    -- Skip 2 bytes
    Payload:  byte[16]
}
```

**Semantics:**
- Fields named `_` are parsed (cursor advances)
- Discarded fields are NOT accessible in query results
- Multiple `_` fields are permitted
- Discard fields may have any type, including conditional

### 4.12 Switch (Tagged Union) Payloads

A field whose parsed type depends on a previously parsed discriminator value:

```
SwitchField   ::= Identifier ':' 'switch' Selector '{' SwitchCaseList '}'

Selector      ::= FieldReference

SwitchCaseList ::= SwitchCase (',' SwitchCase)* ','?

SwitchCase    ::= CaseLabel '=>' Identifier ':' Type

CaseLabel     ::= ScalarLiteral | '_'
```

The selector is a reference to a previously declared field. Each case maps a
constant scalar value (or the default `_`) to a branch, where a branch has a
unique alias and a binary type annotation.

```sql
binary LoginPayload { UserId: int le };
binary DataPayload { Value: byte };
binary ErrorPayload { Code: byte };
binary Packet {
    Type:    byte,
    Length:  short le,
    Payload: switch Type {
        1 => Login: LoginPayload,
        2 => Data:  DataPayload,
        3 => Error: ErrorPayload,
        _ => Raw:   byte[Length]
    }
}
```

#### 4.12.1 Result Shape

A switch field produces a logical tagged-union record with:

- A `Case` member holding the selected branch alias as a string (e.g. `"Login"`,
  `"Raw"`).
- One member per branch alias. Exactly one branch member — the one matching the
  selector — is non-null; all other branch members are null.

```sql
binary LoginPayload { UserId: int le };
binary DataPayload { Value: byte };
binary ErrorPayload { Code: byte };
binary Packet {
    Type:    byte,
    Length:  short le,
    Payload: switch Type {
        1 => Login: LoginPayload,
        2 => Data:  DataPayload,
        3 => Error: ErrorPayload,
        _ => Raw:   byte[Length]
    }
};
let content: byte[] = array { 1ub, 4ub, 0ub, 7ub, 0ub, 0ub, 0ub };
SELECT
    p.Type,
    p.Payload.Case,            -- selected branch alias, e.g. 'Login'
    p.Payload.Login.UserId,    -- non-null only when Type = 1
    p.Payload.Raw              -- non-null only when no case matched
FROM values { (Seed: 0) } seed
CROSS APPLY Interpret<Packet>($content) p
```

#### 4.12.2 Cursor Semantics

- The selector is evaluated against already-parsed field values; it consumes no
  bytes.
- Only the selected branch is parsed. The cursor advances by exactly the size of
  the selected branch.
- Branch types follow the same cursor rules as the equivalent standalone field
  type (schema reference, `byte[...]`, primitive, etc.).

#### 4.12.3 Default and No-Match Behavior

- The default case `_` is OPTIONAL and, when present, MUST be the last case.
- When the selector matches no case and a default exists, the default branch is
  parsed.
- When the selector matches no case and NO default exists, parsing fails with a
  parse error identifying the field and unmatched selector value.

#### 4.12.4 Diagnostics

| Code | Condition |
|------|-----------|
| `MQ4011` | Switch selector references a field that is not declared before the switch field (forward reference or unknown field). |
| `MQ4012` | Two branches in the same switch declare the same alias. |
| `MQ4013` | A case label is not a constant scalar literal compatible with the selector type, or a branch type is not a supported binary type annotation. |

#### 4.12.5 Static restrictions and compositional types

- The selector MUST be a reference to a previously declared field; arbitrary
  expressions are not permitted.
- Case labels MUST be constant scalar literals (integers, etc.); ranges and
  expression cases are reserved (see §12).
- `_` is an OPTIONAL default; there is no fallthrough between cases.
- A switch field is a single value. Arrays of switch fields are not written
  directly; wrap the switch in a nested schema and make an array of that schema.

The same recursive binding and cursor rules apply in every permitted value
context. All branches are statically validated and bound; only the selected branch
reads input or evaluates runtime sizes. Binary switches retain `Case` and nullable
branch members; unselected branches are null. Branch-local repetition names shadow
enclosing field names while other enclosing references remain available.

| Binary value type | Ordinary field | Array element | Switch arm | Repeated element |
|---|---|---|---|---|
| Primitive, byte array, encoded string, bits | Yes | Yes | Yes | Yes |
| Named or inline binary schema | Yes | Yes | Yes | Yes |
| Fixed-count array | Yes | Yes | Yes | Yes |
| Repetition | Yes | Yes, group its type | Yes | Yes, group its type |
| Binary switch | Yes | No direct array; wrap in a schema | Yes | Yes |
| Raw or permitted structured substream | Yes | Yes | Yes | Yes |

A parenthesized repetition groups the element type before an array suffix, for
example `(byte repeat until Items[-1] = 0)[2]`. Each of the two elements is a
complete repetition with its own terminator. Fixed arrays and repetitions may
nest recursively. Bit elements and selected bit arms remain packed; following
byte reads use the ordinary alignment rules. EOF repetition measures forward
progress using both byte position and bit offset. Alignment directives are field
operations, not switch, array or repetition values.

### 4.13 Substream (Length-Bounded) Payloads

A field that parses a nested payload within an explicit byte length, so that a
nested schema cannot drift into following parent fields:

```
SubstreamField ::= Identifier ':' 'substream' '[' Expression ']' SubstreamBody

SubstreamBody  ::= 'raw'
                 | 'as' Type SubstreamMode?

SubstreamMode  ::= 'exact' | 'lax'
```

The size expression resolves to the substream length in bytes and may reference
previously parsed fields. The substream occupies exactly that many bytes; on
success the parent cursor advances to `substreamStart + length` regardless of
how many bytes the nested parser consumed.

```sql
binary PayloadBody { Value: byte };
binary Packet {
    Kind:     byte,
    Length:   uint le,
    Payload:  substream[Length] as PayloadBody,
    Checksum: uint le
}
```

**Forms:**

| Form | Result |
|------|--------|
| `substream[Length] raw` | `byte[]` of exactly `Length` bytes |
| `substream[Length] as Body` | nested `Body` value, exact mode (default) |
| `substream[Length] as Body exact` | nested `Body`; fails if nested parser consumes fewer than `Length` bytes |
| `substream[Length] as Body lax` | nested `Body`; unconsumed trailing bytes are skipped |
| `substream[Length] as { ... }` | inline-schema payload, exact/lax as above |

#### 4.13.1 Cursor Semantics

- The size expression is evaluated against already-parsed field values; the
  bracketed length itself consumes no extra bytes.
- The nested parser operates on a bounded slice of exactly `Length` bytes. An
  over-read past the slice fails naturally because the slice has no more bytes.
- On success, the parent cursor always advances to `substreamStart + Length`,
  even in `lax` mode where the nested parser stops early.

#### 4.13.2 Modes

- `as ...` defaults to `exact`.
- `exact` requires the nested terminal cursor to be exactly `(Length, 0)`: all
  bytes and all bits must be consumed. Otherwise it fails with ISE0002.
- `lax` permits the nested parser to consume fewer than `Length` bytes; the
  remaining bytes and bits inside the substream are discarded.

Nested binary errors report offsets in the supplied outer byte input; field and
schema paths identify the nested context. Absolute `at` positions use the supplied
input's coordinate origin. Ordinary nested schemas share that origin; a substream
supplies its own bounded slice, whose byte zero is local. InterpretAt changes the
initial cursor, not the input's coordinate origin. A substream prevents reads into the parent's following
fields. A short outer slice fails ISE0001; an under-consumed exact slice fails
ISE0002. Full-slice advancement occurs only after successful nested parsing.
On failure the parent retains the child terminal cursor rebased to the outer
input, including its bit offset; it does not jump to the end of the slice.

#### 4.13.3 Structured target restrictions

- `raw` returns bytes. Structured `as` targets are limited to a named binary schema
  (including a closed generic schema) or an inline binary schema. Scalars, arrays,
  text schemas, repetitions and switches are invalid structured targets.
- A switch payload may instead be contained in a permitted named or inline binary schema.
- The size expression MUST resolve to a non-negative length.

---

## 5. Text Schema Syntax

### 5.1 Schema Declaration

```
TextSchema ::= 'text' Identifier '{' TextFieldList '}'

TextFieldList ::= TextField (',' TextField)* ','?
```

Example:

```sql
text LogEntry {
    Timestamp: between '[' ']',
    _:         literal ' ',
    Level:     until ':',
    _:         literal ' ',
    Message:   rest
}
```

### 5.2 Pattern Matching

```
PatternField ::= 'pattern' RegexLiteral CaptureMode?

CaptureMode ::= 'capture' '(' Identifier (',' Identifier)* ')'
```

**Pattern Semantics:**
- Patterns are regular expressions. The supported subset includes: character classes (`\d`, `\w`, `\s`, `[...]`), quantifiers (`*`, `+`, `?`, `{n}`, `{n,m}`), alternation (`|`), grouping (`(...)`), named groups (`(?<name>...)`), anchors (`^`, `$`), and standard escape sequences. Lookahead, lookbehind, backreferences, and recursive patterns are not supported.
- Match MUST occur at the current position, with anchors evaluated against the remaining input slice
- Cursor advances past the entire match
- Match failure raises a parse error (ISE0003)

Examples:

```sql
text Example {
    -- Simple pattern
    Ip:       pattern '\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}',

    -- Pattern with named captures (creates a capture-only record)
    Coords:   pattern '(?<Lat>-?\d+\.\d+),(?<Lon>-?\d+\.\d+)'
              capture (Lat, Lon),

    -- Quantified patterns
    Digits:   pattern '\d+',
    Hex:      pattern '[0-9A-Fa-f]+'
}
```

Patterns are case-sensitive and culture-independent unless an inline case flag
is present. Character categories, escapes and matching units follow core §7.7.
Text patterns exclude lookaround, backreferences and recursive patterns, unlike
RLIKE. Alternation uses authored order; greedy/lazy quantifiers select the
longest/shortest viable match. Captures expose the last capture for a repeated
group, and unmatched optional groups expose an empty string. Pattern validation
failures in authored declarations are compilation errors (MQ4002), including
unused schemas and optional fields. Unsupported constructs and invalid capture
requests are rejected before any input is opened. Direct runtime pattern helpers
retain their typed ISE0015 failure for invalid patterns supplied through that API.

**Capture Semantics:**
- Without a `capture` clause, a pattern field returns the complete matched string. With `capture`, it returns a capture-only record: there is no additional full-match or synthetic `Value` member.
- The requested names determine the record's members and their declaration/serialization order, independently of regex group order. Names are case-sensitive. Duplicate requests and names not defined by the pattern are compilation errors (`MQ4002`), including in unused declarations, optional fields and unselected switch arms.
- Each member has logical type string. A nonparticipating group produces the empty string; a repeated group exposes its final capture. A failed optional pattern produces a null field, rather than a record of empty strings.
- String-processing modifiers apply separately to each captured string.
- Cursor advancement uses the complete match, including text outside retained groups. Keeping fewer captures or modifying captured strings does not change cursor advancement.
- Capture-only shapes and string member metadata are preserved inside repetitions and switch arms. A requested group literally named `Value` is an ordinary capture member; it is not a wrapper.

### 5.3 Literal Matching

```
LiteralField ::= 'literal' StringLiteral
```

Expects exact string at current position. Advances cursor past the literal.

```sql
text HttpRequest {
    Method:   until ' ',
    Path:     until ' ',
    Version:  until '\r\n',
}
```

**Semantics:**
- Match is case-sensitive
- Failure raises parse error (ISE0004)
- Escape sequences: `\r`, `\n`, `\t`, `\\`, `\'`

### 5.4 Delimiter-Based Capture

#### 5.4.1 Until Delimiter

```
UntilField ::= 'until' StringLiteral Modifiers?
```

Captures everything until delimiter (exclusive). Delimiter is consumed but not included.

```sql
text Example {
    Key:   until ':',
    _:     literal ' ',
    Value: until '\n'
}
```

**Semantics:**
- Scans forward for first occurrence of delimiter
- Captures all characters BEFORE the delimiter
- Cursor advances PAST the delimiter
- Delimiter not found raises parse error (ISE0005)

#### 5.4.2 Between Delimiters

```
BetweenField ::= 'between' StringLiteral StringLiteral Modifiers?
```

Captures content between opening and closing delimiters (exclusive).

```sql
text Example {
    Quoted:    between '"' '"',
    Bracketed: between '[' ']',
    Parens:    between '(' ')' nested
}
```

**The `nested` modifier** handles balanced delimiters:

```
Input:  "(a(b)c)"
Result: "a(b)c"   -- Inner content with nested parens preserved
```

Without `nested`, parsing stops at the first closing delimiter.

#### 5.4.3 Escaped Content

```
EscapedField ::= BetweenField 'escaped' StringLiteral?
```

Handles escape sequences within delimited content:

```sql
text Example {
    -- Default: backslash escapes (\" within quotes)
    String1: between '"' '"' escaped,

    -- Custom escape: doubling ('' within single quotes)
    String2: between '\'' '\'' escaped '\''
}
```

**Semantics:**
- Default escape character is backslash (`\`)
- An escaped closing delimiter is included in captured content without its escape character: input `<a\>b>` with `between '<' '>' escaped` returns `a>b`.
- The escape character itself can be escaped (`\\` → `\`). Decoding happens once; a decoded escape does not start another escape sequence.
- An escape before unrelated content is preserved. Only a complete closing delimiter is decoded, including multi-character delimiters.
- Doubled escape characters are recognized before escaped delimiters. This also supports doubling a quote when the custom escape character is the closing quote.
- Delimiters and cursor positions are determined from the original input; decoding precedes trim/case modifiers and does not change where subsequent fields or repeated entries begin.

### 5.5 Fixed-Width Fields

```
CharsField ::= 'chars' '[' IntegerLiteral ']' Modifiers?
```

Captures exactly N UTF-16 code units (core §3.9).

```sql
text CobolRecord {
    CustomerId:  chars[10],
    Name:        chars[30] trim,
    Balance:     chars[12] rtrim,
    Status:      chars[1]
}
```

**Semantics:**
- Reads exactly N UTF-16 code units, not bytes or grapheme clusters
- Insufficient characters raises parse error (ISE0001)
- Modifiers apply after capture

### 5.6 Token Capture

```
TokenField ::= 'token' Modifiers?
```

Captures whitespace-delimited token.

```sql
text SpaceSeparated {
    First:  token,
    _:      whitespace,
    Second: token,
    _:      whitespace,
    Third:  rest
}
```

**Semantics:**
- Captures consecutive non-whitespace characters
- Stops at first whitespace (not consumed)
- Empty token at end of input is valid (empty string)

### 5.7 Rest of Input

```
RestField ::= 'rest' Modifiers?
```

Captures all remaining content.

```sql
text KeyValue {
    Key:   until '=',
    Value: rest trim
}
```

**Semantics:**
- Captures from current position to end of input
- May be empty string if at end of input
- Typically the last field in a schema

### 5.8 Optional Fields

```
OptionalField ::= 'optional' TextField
```

Field may or may not be present. Results in `null` if not matched.

```sql
text LogLine {
    Timestamp: between '[' ']',
    _:         literal ' ',
    Level:     until ':',
    _:         literal ' ',
    Message:   until '\t',

    -- Message consumes its trailing tab; optional trace ID follows
    TraceId:   optional pattern '[a-f0-9]{32}'
}
```

**Semantics:**
- Attempts to match the inner field type
- On a recoverable interpretation failure: field is `null` and the cursor checkpoint is restored
- On success: field has value, cursor advances as normal
- Failed optional attempts leave no consumed input or incomplete payload

**Backtracking Rule:**
The cursor position is saved before attempting an optional field and restored
when a defined interpretation failure (ParseException) is recovered. Optional,
tolerant and partial interpretation recover only defined input interpretation
failures. Cancellation, provider failures and unrelated execution faults propagate.
Static validation applies to every declaration and branch, including unused and
optional declarations; recovery never suppresses an invalid schema.

Modifier validation applies recursively inside repetition and switch arms.
`nested` and `escaped` apply only to `between` and exclude each other.
`greedy` and `lazy` apply only to `until` and `pattern` and exclude each other.
`trim`, `ltrim`, `rtrim`, `lower` and `upper` process strings (including requested
captures); they do not apply to schema records, arrays or switch containers.
`optional` may apply to any text type.

### 5.9 Repetition

```
RepeatField ::= 'repeat' TextField UntilClause?

UntilClause ::= 'until' (StringLiteral | 'end')
```

Examples:

```sql
text HeaderLine {
    Name:  until ':',
    _:     literal ' ',
    Value: until '\r\n',
}

text HttpHeaders {
    -- Repeat until empty line
    Headers: repeat HeaderLine until '\r\n'
}
```

**Semantics:**
1. Before each element, check the active input boundary and then the authored
   delimiter, if any. EOF succeeds even when that delimiter was never encountered.
2. A matching delimiter terminates the repetition and is consumed exactly once.
   Delimiters are checked only between elements; delimiter text inside an element
   belongs to that element and does not interrupt it.
3. If another element is needed, check the configured repetition budget before
   parsing it. Reaching EOF or a delimiter at exactly the budget succeeds.
4. Parse one complete element. An interpretation failure aborts the repetition;
   it never returns a successful prefix. Optional or partial recovery outside the
   repetition follows the ordinary recovery and completion rules.
5. A successful element must advance the cursor. Otherwise fail immediately with
   ISE0009, including zero-width patterns and recovered empty optional elements.
6. Append the completed element and repeat.

The result is an array, including an empty array for immediate EOF or delimiter.
`until end` and an omitted `until` clause both stop at EOF. The existing configurable
limit defaults to 10,000 elements; nested interpreters receive the selected limit,
with an independent counter for each repetition. The selected limit is part of
compiled-artifact compatibility and is preserved when an artifact is reused.

### 5.10 Alternatives (Switch)

```
SwitchField ::= 'switch' '{' (SwitchCase+ DefaultCase? | DefaultCase) '}'

SwitchCase ::= 'pattern' RegexLiteral '=>' TextType ','?

DefaultCase ::= '_' '=>' TextType ','?
```

Choose parsing strategy based on lookahead:

```sql
text SectionHeader {
    _:    pattern '\s*',
    _:    literal '[',
    Name: until ']'
}

text KeyValue {
    _:     pattern '\s*',
    Key:   until '=',
    Value: rest trim
}

text Comment {
    _:    pattern '\s*[#;]',
    Text: rest
}

text Empty {}

text ConfigLine {
    Content: switch {
        pattern '\s*\[' => SectionHeader,
        pattern '\s*#'  => Comment,
        pattern '\s*;'  => Comment,
        pattern '\s*$'  => Empty,
        _               => KeyValue
    }
}
```

**Semantics:**
- Patterns are tested in order using lookahead (no cursor movement)
- First matching pattern determines which type to parse
- Default case (`_`) matches if no patterns match
- No default + no match raises parse error
- The matching pattern's text is NOT consumed; the selected type parses from current position
- A property present in every arm (including the default arm) has its common logical type and nested schema shape when those agree across all arms. Inherited fields participate after overrides are resolved.
- Each arm is a complete `TextType`, including captures, optional types, repetitions and nested switches. All arms are statically bound and validated before execution; only the selected arm reads input.
- The switch returns the selected value directly. A schema arm keeps paths such as `Content.Name`; scalar and array arms are projected as `Content`. There is no synthetic `.Value` member or merged branch record.
- A common whole-value type is inferred only when every arm agrees. Otherwise the value has dynamic (`object`) metadata. A property missing from an arm, or having different types or nested schema shapes across arms, likewise has dynamic metadata.
- A member declared by at least one statically known arm returns null when absent from the selected arm. A name absent from every known arm fails compilation with the existing unknown-member/source-contract diagnostic (`MQ3071`). Inherited members participate after overrides.
- An optional selected arm can return null without trying later alternatives. An optional switch recovers from a no-match or defined interpretation failure and restores its cursor checkpoint.
- A missing `TryParse` result projected through `OUTER APPLY` yields null property values.
- Serialization writes the selected scalar, array or record directly. Nested schema records contain authored payload members, including completion filtering for partial payloads; interpreter bookkeeping properties are omitted.

### 5.11 Whitespace Handling

```
WhitespaceField ::= 'whitespace' Quantifier?

Quantifier ::= '+' | '*' | '?'
```

| Syntax | Meaning | Failure Behavior |
|--------|---------|------------------|
| `whitespace` | One or more | Error if no whitespace |
| `whitespace+` | One or more | Error if no whitespace |
| `whitespace*` | Zero or more | Never fails |
| `whitespace?` | Zero or one | Never fails |

```sql
text Tokens {
    A: token,
    _: whitespace*,    -- Optional spacing
    B: token
}
```

**Whitespace Characters:** Space (0x20), Tab (0x09), CR (0x0D), LF (0x0A)

### 5.12 Text Field Modifiers

| Modifier | Applicable To | Behavior |
|----------|---------------|----------|
| `trim` | All capture types | Remove leading AND trailing whitespace |
| `rtrim` | All capture types | Remove trailing whitespace only |
| `ltrim` | All capture types | Remove leading whitespace only |
| `lower` | All capture types | Convert to lowercase |
| `upper` | All capture types | Convert to uppercase |
| `nested` | `between` only | Handle balanced/nested delimiters |
| `escaped` | `between` only | Handle escape sequences |
| `greedy` | `until`, `pattern` | Maximize match length (default for most) |
| `lazy` | `until`, `pattern` | Minimize match length |

**Modifier Application Order:**
1. Capture the raw text
2. Apply `nested`/`escaped` processing (if applicable)
3. Apply `greedy`/`lazy` (affects capture)
4. Apply trim (`ltrim`, `rtrim`, `trim`) using only space, tab, CR and LF.
5. Apply invariant simple case conversion (`lower`/`upper`), without normalization.

If both case modifiers are present, lower takes precedence. Pattern whitespace
classes retain their Unicode meaning and are not restricted to the four text
whitespace-directive units. Text trim preserves nonbreaking space and U+0000.

---

## 6. Common Elements

### 6.1 Identifiers

```
Identifier ::= [a-zA-Z_][a-zA-Z0-9_]*
```

- Case-sensitive for field names
- MUST NOT be a reserved keyword
- Convention: `PascalCase` for fields (matches Musoq conventions)

### 6.2 Comments

```sql
binary Example {
    -- Single line comment
    Field1: int le,

    /* Multi-line
       comment */
    Field2: short le,

    Field3: byte   -- Inline comment
}
```

**Comment Preservation:**
Comments are preserved in schema metadata and available via introspection for documentation generation.

### 6.3 Expressions

Schema expressions use core §24.6 precedence and expression operators, subject to
field scoping (§6.4). Expressions may appear in sizes, positions, guards, checks
and computed fields. A check can reference its own field after it is read; other
expressions may reference only earlier fields. A repeat condition may inspect
the just-read collection element. CASE may omit ELSE, which implies NULL as in core. A computed
field uses `Name: expression` or `Name: = expression`; `:=` is not its syntax.
Both computed-field forms accept core scalar literals, including ordinary and raw
strings and decimal, hexadecimal, binary and octal integer forms. A leading
literal must not be interpreted as a parsed-field type annotation.
Sizes and positions require finite integral values in the inclusive range 0 through
2,147,483,647. Null, fractional, negative, non-finite, or out-of-range values
fail with ISE0007 for sizes and ISE0008 for positions. An actual read or seek
must additionally respect the active input boundary. Dotted
references, arithmetic, parentheses and CASE are valid in both locations.
Null, fractional, non-finite, negative and overflowing values are invalid; null
does not imply zero. Invalid sizes report ISE0007, invalid positions ISE0008.
Evaluation occurs once when the field is reached, after its guard succeeds.
A FALSE or UNKNOWN guard skips parsing, size and position evaluation, and checks.

CASE selects only the first TRUE WHEN branch; FALSE and UNKNOWN select no branch.
Only the selected result expression is evaluated; omitted ELSE returns NULL. Simple
CASE uses ordinary core equality, including its null rules. Result branches use
core compatible-type rules. NOT, AND and OR preserve core three-valued logic.
Computed arithmetic uses the core numeric promotion rules: small integral
operands produce `int`, lifted to `int?` when an operand can be absent.
Integer literals in schema expressions preserve the explicit core type suffixes
(`b`, `ub`, `s`, `us`, `i`, `ui`, `l`, `ul`); an unsuffixed integer remains `int`.
Decimal literals have core type `decimal`. Mixed numeric arithmetic converts
both operands to the core common numeric type before applying the operator;
decimal mixed with `float` or `double` requires an explicit conversion. For
example, `F::Decimal + 0.5` produces `decimal?` when F is a parsed `float`, while
`F + 0.5` is a binding error. Strict postfix casts follow core §3.7, including
nullable scalar metadata and null propagation.
Numeric comparisons follow core comparison conversion rather than the arithmetic
compatibility table: a decimal compared with float or double converts both operands
to decimal without requiring an explicit cast. Comparisons preserve UNKNOWN for
absent operands. String ordering uses ordinal comparison, not host-language string
relational operators. String and array `Length` members have logical type `int`,
lifted to `int?` when their containing value can be absent. Intrinsic function
results retain the logical signatures in §6.5 for binding and later expressions.

#### SQL string concatenation

Where an embedded scalar expression is accepted, `||` concatenates string operands and propagates NULL. It shares additive precedence and left associativity with `+` and `-`. Operands execute once, left to right, including after NULL; numeric values require explicit conversion. A single `|` retains bitwise semantics. Query and schema expressions share the same string-only typing contract.

### 6.4 Field Reference Scoping

Within a schema, expressions may reference:
- Fields declared BEFORE the current field (by name)
- The current field (only in `check` clauses)
- Nested fields via dot notation: `Header.Version`
- Array elements via indexing: `Items[0]`, `Records[-1]` (last element)

**Forward references MUST NOT occur.** Following field expressions see the earlier parsed or computed values.

### 6.4.1 Explicit Enclosing Binary Record Context

In a nested binary schema, `parent.Field` refers to a completed field of the
immediately enclosing record instance. `parent.parent.Field` traverses two record
levels; further `parent` segments follow the same rule. This is record nesting,
not schema inheritance or a SQL alias. The extension applies to binary schemas.

```sql
binary Item { Value: byte };
binary Body { Items: Item[parent.Count] };
binary Record { Count: byte, Payload: Body };

let content: byte[] = array { 2ub, 10ub, 20ub };
SELECT i.Value
FROM values { (Seed: 0) } seed
CROSS APPLY Interpret<Record>($content) r
CROSS APPLY r.Payload.Items i
```

Here `$content` is the explicitly declared byte-array input; VALUES supplies
the invocation row.
The bytes `02 0A 14` yield the values 10 and 20.

- Only fields completed before entering the nested record are visible through
  its enclosing context. The record being constructed, later siblings, and
  forward references are unavailable.
- Parsed and computed enclosing values may be used in sizes, positions, guards,
  checks and computed expressions, subject to their normal type and null rules.
- A separately declared schema may express an enclosing-context requirement.
  Binding MUST validate that requirement at every embedding site. Missing fields,
  unavailable ancestors, forward references and incompatible types are schema
  diagnostics; they MUST NOT become internal compiler errors. Compatible numeric
  enclosing types may widen using core common-type rules.
- A parent-dependent schema cannot be interpreted directly without its required
  enclosing record. This is a binding error, reported as MQ2030.
- Array and repeat elements use the containing record as their parent. A
  substream wrapper does not add a record level; its embedded record does.
  Inheritance does not add a record level.
- Enclosing context is read-only and is not a parsed field, query column or
  serialized member. Each interpretation receives its own context values.
- `parent` is contextual, not a globally reserved identifier. An earlier local
  field named `parent` retains ordinary member access. Otherwise, a dotted
  expression rooted at `parent` denotes enclosing context. Bare identifiers in
  named schemas do not acquire implicit enclosing lookup. Existing unqualified
  enclosing references in anonymous schemas remain compatible; new declarations
  SHOULD use explicit `parent` paths.

A bare identifier matching a later local field is a forward reference. A bare
identifier that names no local field is an unknown field; MQ2030 SHOULD explain
the local scope and suggest explicit `parent.<field>` for a named schema when
the intended field might belong to an enclosing record. These diagnostics do
not enable implicit enclosing lookup or change the visibility of later fields.

For example, this named schema uses the enclosing length explicitly:

```sql
binary Inner { Payload: byte[parent.Len] };
binary Rec { Len: byte, Body: Inner };

let content: byte[] = array { 2ub, 10ub, 20ub };
SELECT r.Len, r.Body.Payload[0], r.Body.Payload[1]
FROM values { (Seed: 0) } seed
CROSS APPLY Interpret<Rec>($content) r
```

The bytes `02 0A 14` produce `(2, 10, 20)`. Writing `byte[Len]` inside
`Inner` is rejected because `Inner` declares no local `Len`. An earlier local
`Len` would take precedence for a bare reference; `parent.Len` continues to
refer to the enclosing record.

### 6.5 Built-in Functions for Schemas

Schema expressions require the following logical function signatures. These are **not** discoverable via `desc functions` because they are intrinsic to the schema definition language rather than provided by a schema plugin.

| Function | Signature | Description |
|----------|-----------|-------------|
| `Length` | `Length(byte[]) -> int` | Length of byte array |
| `Crc32` | `Crc32(byte[]) -> int` | CRC-32 checksum |
| `Crc16` | `Crc16(byte[]) -> short` | CRC-16 checksum |
| `Adler32` | `Adler32(byte[]) -> int` | Adler-32 checksum |
| `Md5` | `Md5(byte[]) -> byte[16]` | MD5 hash |
| `Sha256` | `Sha256(byte[]) -> byte[32]` | SHA-256 hash |
| `Substring` | `Substring(string, int, int) -> string` | String substring |
| `IndexOf` | `IndexOf(byte[], byte[]) -> int` | Find byte pattern (-1 if not found) |
| `ToHex` | `ToHex(byte[]) -> string` | Convert bytes to hex string |
| `FromHex` | `FromHex(string) -> byte[]` | Convert hex string to bytes |
| `ToText` | `ToText(byte[], string) -> string` | Decode bytes with encoding |

These functions are available within `check`, computed fields and `when`.
Length is the byte count. Crc32 uses reflected CRC-32/ISO-HDLC (polynomial
0xEDB88320, initial 0xFFFFFFFF, final XOR 0xFFFFFFFF), returned as signed int bits.
Crc16 uses reflected CRC-16/ARC (polynomial 0xA001, initial zero, no final XOR),
returned as signed short bits. Adler32 uses modulus 65521, initial A=1/B=0 and
returns (B << 16) | A as signed int bits. Md5 and Sha256 return their standard
digest bytes in digest order. ToHex emits uppercase two-digit hex per byte;
FromHex accepts pairs of ASCII hex digits ignoring letter case, rejects odd
length and nonhex content, and emits bytes in textual order. IndexOf returns
the first zero-based byte-pattern position or -1. Substring counts UTF-16 units
and requires nonnegative start/length within the input bounds. ToText uses the
strict decoding rules in §4.2.4 and the encoding names defined there. No native
method library or discovery API is required to implement these operations.
Malformed encoded input to `ToText` reports ISE0010. Invalid intrinsic arguments,
including malformed hex and invalid substring bounds, report ISE0014 during
expression evaluation; they must not become internal compiler failures.

---

## 7. Interpretation Functions

### 7.1 Method Signature Pattern

Interpretation functions are language operations parameterized by a visible schema
name. Interpret accepts a byte collection and a binary schema; Parse accepts a
string and a text schema. They produce the logical record shape defined by the
schema. The angle-bracket syntax is SQL syntax, not a host generic-function API.

### 7.2 SQL Syntax for Interpretation

In SQL queries, the schema type is referenced by name (without parentheses), consistent with SQL type references:

```sql
binary Header { Magic: int le, Version: short le };
-- Schema type referenced by name, no parentheses
let content: byte[] = array { 120ub, 86ub, 52ub, 18ub, 1ub, 0ub };
SELECT h.Magic, h.Version
FROM values { (Seed: 0) } seed
CROSS APPLY Interpret<Header>($content) h
```

**Binding and execution:**
1. Resolve `Header` to a visible binary schema.
2. Validate the input logical type during binding.
3. Evaluate the input and apply the schema rules during execution.

### 7.3 Binary Interpretation

```sql
Interpret<SchemaType>(data)
```

Parses byte array according to schema, returning structured result.

```sql
binary Header { Magic: int le, Version: short le };
let content: byte[] = array { 120ub, 86ub, 52ub, 18ub, 1ub, 0ub };
SELECT h.Magic, h.Version
FROM values { (Seed: 0) } seed
CROSS APPLY Interpret<Header>($content) h
```

#### 7.3.1 Decoded Values Through CTEs

When the host permits non-primitive intermediate projections, decoded records and
record arrays MUST retain their schema identity through CTE projections, explicit
column renaming and chained CTEs. Accessing a nested field after the CTE MUST use
the same logical type and nullability as accessing that field directly.
Compilation, repeated execution and stored-artifact loading MUST preserve this
metadata; execution MUST NOT infer a record's schema from its values.

For a test source supplying `Content = 01 02`, this query returns `X = 1, Y = 2`:

```sql
binary Inner { X: byte, Y: byte }
binary Outer { Inner: Inner }
let content: byte[] = array { 10ub, 20ub };
with c (Renamed) as (
    select s.Inner as Inner
    from values { (Seed: 0) } seed
    cross apply Interpret<Outer>($content) s
)
select c.Renamed.X as X, c.Renamed.Y as Y from c c
```

Here `$content` is an explicitly typed byte-array input; the VALUES source
provides the single row that invokes interpretation. Primitive-only projection validation remains applicable; schema
preservation does not disable that host policy. Projecting the scalar fields
inside the CTE remains valid under primitive-only validation.

### 7.4 Text Interpretation

```sql
Parse<SchemaType>(text)
```

Parses string according to text schema.

```sql
text LogEntry { Timestamp: between '[' ']', _: literal ' ', Level: until ':', _: literal ' ', Message: rest };
SELECT log.Timestamp, log.Level, log.Message
FROM values { (Line: '[2024-01-02] INFO: ready'), (Line: '[2024-01-03] WARN: retry') } lines
CROSS APPLY Parse<LogEntry>(lines.Line) log
```

### 7.5 Safe Interpretation

```sql
TryInterpret<SchemaType>(data) -> SchemaType?
TryParse<SchemaType>(text) -> SchemaType?
```

Returns `null` instead of throwing on parse failure. Like all interpretation functions, `TryInterpret` and `TryParse` MUST be used inside `CROSS APPLY` or `OUTER APPLY` (see §3.3).

```sql
binary Header { Magic: int le, Version: short le };
let content: byte[] = array { 120ub, 86ub, 52ub, 18ub, 1ub, 0ub };
let shortContent: byte[] = array { 1ub };
SELECT input.Name, CASE WHEN h.Magic IS NOT NULL THEN 'Valid' ELSE 'Invalid' END AS Status
FROM values { (Name: 'valid', IsValid: true), (Name: 'truncated', IsValid: false) } input
OUTER APPLY TryInterpret<Header>(case when input.IsValid then $content else $shortContent end) h
```

**Behavior with CROSS/OUTER APPLY:**
- `null` result with CROSS APPLY: no output row
- `null` result with OUTER APPLY: one row with NULL alias

### 7.6 Offset-Based Interpretation

```sql
InterpretAt<SchemaType>(data, offset)
```

Begin parsing at specified byte offset:

The query-level offset argument MUST have non-nullable `int` type or an integral
type that widens to it without loss: `byte`, `sbyte`, `short`, or `ushort`. Wider numeric
types require an explicit conversion; a nullable conversion requires an explicit
NULL default, for example `ToInt32(h.DataOffset) ?? 0`. An incompatible offset
type reports MQ3005 during binding. This differs from schema-field `at`
expressions, whose numeric values are checked against the schema cursor domain.

```sql
binary Header { Magic: int le, Version: short le, DataOffset: int le };
binary DataSection { RecordCount: int le };
let content: byte[] = array { 120ub, 86ub, 52ub, 18ub, 1ub, 0ub, 10ub, 0ub, 0ub, 0ub, 7ub, 0ub, 0ub, 0ub };
SELECT
    h.Magic,
    d.RecordCount
FROM values { (Seed: 0) } seed
CROSS APPLY Interpret<Header>($content) h
CROSS APPLY InterpretAt<DataSection>($content, h.DataOffset) d
```

### 7.7 Multiple Records

For files containing multiple sequential records of the same type:

**Option 1: Container schema with array**

```sql
binary Record { Id: int le, Value: double le };
binary RecordFile {
    Count:   int le,
    Records: Record[Count]
}

let content: byte[] = array { 2ub, 0ub, 0ub, 0ub, 7ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 248ub, 63ub, 8ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 4ub, 64ub };
SELECT r.Id, r.Value
FROM values { (Seed: 0) } seed
CROSS APPLY Interpret<RecordFile>($content) file
CROSS APPLY file.Records r
```

**Option 2: Explicit offset calculation**

```sql
binary Record { Id: int le, Value: double le };
let content: byte[] = array { 7ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 248ub, 63ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 8ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 4ub, 64ub };
WITH RecordOffsets AS (
    SELECT n.Value * 64 AS Offset FROM values { (Value: 0), (Value: 1) } n
)
SELECT r.Id, r.Value
FROM values { (Name: 'records') } input
CROSS JOIN RecordOffsets o
CROSS APPLY InterpretAt<Record>($content, o.Offset) r
WHERE o.Offset < Length($content)
```

---

### 7.8 Partial Interpretation (Debugging)

```
PartialInterpret<SchemaType>(data)
PartialParse<SchemaType>(text)
```

Returns a partial result for debugging malformed or incomplete data. Like all interpretation functions, `PartialInterpret` and `PartialParse` MUST be used inside `CROSS APPLY` or `OUTER APPLY` (see §3.3).

| Field | Type | Description |
|-------|------|-------------|
| `ParsedFields` | Typed schema payload | Declared members whose validity is identified by `CompletedFields` |
| `ErrorField` | `string?` | Name of field where parsing failed (`null` if successful) |
| `ErrorMessage` | `string?` | Error description (`null` if successful) |
| `BytesConsumed` | `int` | Terminal cursor position in bytes (binary) or UTF-16 code units (text) |
| `CompletedFields` | Read-only set of field names | Members that completed successfully, including resolved absent/optional members |
| `IsSuccess` | `bool` | Whether the complete schema succeeded |
| `BitOffset` | `int` | Bit position within the current binary byte (0–7); zero for text |

The original four SQL columns retain their positions; completion and bit metadata
are appended in the order shown. Existing partial-result API constructors remain
available and default `BitOffset` to zero; overloads carry the bit offset explicitly.

Cursor state separates the input origin, active boundary and terminal position.
Binary positions count bytes from the supplied input origin, with `BitOffset`
locating the next bit. Text positions count UTF-16 code units. Ordinary nested
schemas share the input origin and boundary; substreams introduce a bounded local
input and rebase failures to their parent. `BytesConsumed` is the terminal cursor,
not the sum of reads or the highest position visited: an `at` clause may move it
backward. Diagnostic failure location is separate from cursor state; for example,
invalid decoding may identify an offending byte while the cursor remains at the
start of the failed string read.

A field completes only after its read, decoding and checks succeed. A false or
unknown presence guard, or a recovered optional field, completes with null.
Failed nested fields and collections publish neither incomplete children nor
successful element prefixes. `ParsedFields` keeps its concrete declared type;
`CompletedFields` identifies which members are valid, and serialization omits
incomplete members. Optional recovery restores its cursor checkpoint. Ordinary
nested failures retain the child's terminal cursor even after backward positioning.
Repeated use of an interpreter starts a fresh cursor and completion capture.

---

## 8. Schema Composition

### 8.1 Schema References

Schemas may reference other schemas by name. Referenced schemas MUST be defined first.

```sql
binary Point { X: float le, Y: float le }

binary Triangle {
    A: Point,
    B: Point,
    C: Point
}
```

### 8.2 Inline Anonymous Schemas

For one-off nested structures:

```sql
binary Packet {
    Header: {
        Magic: int le,
        Version: short le
    },
    Body: byte[64]
}
```

**Semantics:**
- Anonymous schemas are defined inline
- Not referenceable by name
- Useful for simple nested structures

### 8.3 Schema Inheritance (Extension)

```
ExtendedSchema ::= 'binary' Identifier 'extends' Identifier '{' ... '}'
```

```sql
binary BaseMessage {
    MsgType: byte,
    Length:  short le
}

binary TextMessage extends BaseMessage {
    Content: string[Length] utf8
}

binary BinaryMessage extends BaseMessage {
    Data: byte[Length]
}
```

**Semantics:**
- Binary and text schemas may extend an earlier schema of the same kind.
- Child schema includes all parent fields first
- Child fields are appended after parent fields
- Parent fields are accessible by name in child
- Single inheritance only (no multiple extends)

### 8.4 Generic Schemas

Parameterized schemas for reusable patterns:

```sql
binary Header { Magic: int le, Version: short le };
binary Record { Id: int le, Value: double le };
binary LengthPrefixed<T> {
    Length: int le,
    Data:   T[Length]
}

binary Message {
    Header:   Header,
    Records:  LengthPrefixed<Record>
}
```

**Constraints:**
- Generic parameter `T` MUST be a schema type
- Primitive types cannot be generic parameters
- Generic schema arguments are resolved during binding

### 8.5 Binary-Text Composition

Text schemas may be embedded in binary schemas:

```sql
text KeyValue {
    Key:   until '=',
    Value: rest trim
}

binary ConfigBlock {
    Magic:      int le,
    ConfigLen:  short le,
    Config:     string[ConfigLen] utf8 as KeyValue
}
```

**The `as` clause** specifies that the string field should be further parsed using a text schema:
1. Read bytes as string with specified encoding
2. Parse string using the text schema
3. Result type is the text schema type

---

## 9. Error Handling

### 9.1 Parse Error Structure

Parse errors include:
- **Code**: Error code (ISExxxx)
- **Position**: Zero-based byte offset for binary input or UTF-16 code-unit offset for text input
- **Field**: Field being parsed when error occurred
- **Schema**: Schema name (and path for nested schemas)
- **Expected**: What the parser expected
- **Actual**: Safe classification of what was found; raw input requires an explicit diagnostic-detail operation
- **Message**: Human-readable description

Example:

```
ISE0002: Validation failed for field 'Magic' at offset 0
  Schema: PacketHeader
  Constraint: Magic = 0xDEADBEEF
  Actual value: 0x00000000
```

### 9.2 Error Categories

| Category | Codes | Description |
|----------|-------|-------------|
| Input errors | ISE0001 | Unexpected end of input |
| Validation errors | ISE0002 | CHECK constraint failed |
| Pattern errors | ISE0003-ISE0006, ISE0011-ISE0012 | Pattern/literal/delimiter/whitespace/alternative not found |
| Encoding errors | ISE0010 | Invalid character encoding |
| Expression errors | ISE0007-ISE0008, ISE0014 | Invalid size, position, or field reference expression |
| Schema errors | ISE0013 | Circular or undefined schema reference |
| General errors | ISE0009, ISE0015 | Repeat limit exceeded, repeat-until-eof element made no progress, or general parsing failure |

Static schema validity is checked before input interpretation, including unused
declarations and skipped branches. Static failures MUST NOT be reported as
recoverable input mismatches. Direct runtime helpers can still produce typed
ISE failures for invalid arguments supplied through those APIs.

| Condition | Engine code | Phase | Location |
|-----------|-------------|-------|----------|
| Invalid binary/text field, regex, capture, or modifier | `MQ4001` / `MQ4002` | Schema | Authored field/type/modifier when available |
| Undefined schema | `MQ4003` | Schema | Referencing field or type |
| Circular schema reference | `MQ4004` | Schema | Reference completing the cycle |
| Invalid field type/composition | `MQ4007`, `MQ4014`–`MQ4016` | Schema | Offending type/modifier |
| Duplicate schema field | `MQ4008` | Schema | Duplicate field |
| Invalid interpretation callable context | `MQ3033` | Bind | Call site |
| Input interpretation failure `ISE0001`–`ISE0015` | `MQ7101`–`MQ7115`, respectively | Runtime | Separate binary/text input location |

ISE codes describe interpretation input failures; the SQL diagnostic envelope
uses the corresponding MQ code. Query-source spans and input cursor positions
are different domains. Unknown query locations remain unknown in the public
envelope; they MUST NOT be fabricated as offset zero. A real zero-length
insertion point retains its position and zero length. Modifier and field spans
are preserved when supplied by the parser. Cancellation and unrelated provider
or execution faults keep their own classification and bypass tolerant recovery.

### 9.3 Error Behavior

These outcomes apply only to typed interpretation input failures. Compilation
failures, cancellation, provider failures, and unrelated execution exceptions
propagate through their own contracts.

| Function | On Error |
|----------|----------|
| `Interpret` | Fails execution with a diagnostic |
| `Parse` | Fails execution with a diagnostic |
| `TryInterpret` | Returns `null` |
| `TryParse` | Returns `null` |
| `InterpretAt` | Fails execution with a diagnostic |

### 9.4 Partial Results (Debugging)

For debugging malformed data:

```sql
binary Header { Magic: int le, Version: short le };
let content: byte[] = array { 120ub, 86ub, 52ub, 18ub, 1ub };
SELECT
    p.ParsedFields,
    p.ErrorField,
    p.ErrorMessage,
    p.BytesConsumed
FROM values { (Seed: 0) } seed
CROSS APPLY PartialInterpret<Header>($content) p
```

**`PartialInterpret` and `PartialParse` return:**
- `ParsedFields`: Typed payload; only members listed in `CompletedFields` are valid
- `ErrorField`: Name of field where parsing failed (null if successful)
- `ErrorMessage`: Error description (null if successful)
- `BytesConsumed`: Terminal byte or UTF-16 cursor position (not cumulative reads)
- `CompletedFields`: Names of valid typed payload members
- `IsSuccess`: Whether the whole schema completed
- `BitOffset`: Binary intra-byte cursor, zero for text

---

## 10. Grammar Specification

### 10.1 Complete Binary Schema Grammar

```ebnf
BinarySchema
    ::= 'binary' Identifier GenericParams? Inheritance? '{' BinaryFieldList '}'

GenericParams
    ::= '<' Identifier (',' Identifier)* '>'

Inheritance
    ::= 'extends' Identifier

BinaryFieldList
    ::= (BinaryField (',' BinaryField)* ','?)?

BinaryField
    ::= Comment* (NamedField | DiscardField | AlignDirective)

NamedField
    ::= Identifier ':' (BinaryType FieldModifiers? | '='? Expression)

DiscardField
    ::= '_' ':' BinaryType FieldModifiers?

AlignDirective
    ::= '_' ':' 'align' '[' INTEGER ']'

BinaryType
    ::= PrimitiveType
    |   ByteArrayType
    |   StringType
    |   BitFieldType
    |   SchemaReference
    |   ArrayType
    |   InlineSchema
    |   SwitchType
    |   GroupedRepeatType
    |   SubstreamType
    |   RepeatUntilType

SubstreamType
    ::= 'substream' '[' Expression ']' ('raw' | 'as' (SchemaReference | InlineSchema) ('exact' | 'lax')?)

RepeatUntilType
    ::= BinaryType 'repeat' 'until' (Expression | 'eof')

PrimitiveType
    ::= SingleByteType
    |   MultiByteType Endianness

SingleByteType
    ::= 'byte' | 'sbyte'

MultiByteType
    ::= 'short' | 'ushort' | 'int' | 'uint' | 'long' | 'ulong'
    |   'float' | 'double'

Endianness
    ::= 'le' | 'be'

ByteArrayType
    ::= 'byte' '[' SizeExpr ']'

StringType
    ::= 'string' '[' SizeExpr ']' Encoding StringModifiers? TextSchemaRef?

Encoding
    ::= 'utf8' | 'utf16le' | 'utf16be' | 'ascii' | 'latin1' | 'ebcdic'

StringModifiers
    ::= StringModifier+

StringModifier
    ::= 'trim' | 'rtrim' | 'ltrim' | 'nullterm'

TextSchemaRef
    ::= 'as' Identifier

BitFieldType
    ::= 'bits' '[' INTEGER ']'

SchemaReference
    ::= Identifier GenericArgs?

GenericArgs
    ::= '<' SchemaReference (',' SchemaReference)* '>'

GroupedRepeatType
    ::= '(' RepeatUntilType ')'

ArrayType
    ::= BinaryType '[' SizeExpr ']'

InlineSchema
    ::= '{' BinaryFieldList '}'

SwitchType
    ::= 'switch' FieldReference '{' SwitchCase (',' SwitchCase)* ','? '}'

SwitchCase
    ::= (ScalarLiteral | '_') '=>' Identifier ':' BinaryType

FieldModifiers
    ::= ValueValidation? PositionMod? ConditionMod? ValidationMod?

ValueValidation
    ::= ('const' | 'magic') (ScalarLiteral | '[' ByteList ']')
    |   'oneOf' '[' ScalarLiteral (',' ScalarLiteral)* ']'

ByteList
    ::= ScalarLiteral (',' ScalarLiteral)*

PositionMod
    ::= 'at' Expression

ConditionMod
    ::= 'when' Expression

ValidationMod
    ::= 'check' Expression

SizeExpr
    ::= Expression

EnclosingFieldReference
    ::= 'parent' '.' ('parent' '.')* Identifier ('.' Identifier)*

-- Schema Expression uses core precedence and includes searched/simple CASE,
-- unary NOT, dotted member access and enclosing references. Omitted CASE ELSE returns NULL.
```

### 10.2 Complete Text Schema Grammar

```ebnf
TextSchema
    ::= 'text' Identifier Inheritance? '{' TextFieldList '}'

TextFieldList
    ::= (TextField (',' TextField)* ','?)?

TextField
    ::= Comment* (NamedTextField | DiscardTextField)

NamedTextField
    ::= Identifier ':' TextType TextModifiers?

DiscardTextField
    ::= '_' ':' TextType TextModifiers?

TextType
    ::= PatternType
    |   LiteralType
    |   UntilType
    |   BetweenType
    |   CharsType
    |   TokenType
    |   RestType
    |   WhitespaceType
    |   OptionalType
    |   RepeatType
    |   SwitchType
    |   SchemaReference

PatternType
    ::= 'pattern' REGEX CaptureClause?

CaptureClause
    ::= 'capture' '(' Identifier (',' Identifier)* ')'

LiteralType
    ::= 'literal' STRING

UntilType
    ::= 'until' STRING

BetweenType
    ::= 'between' STRING STRING BetweenModifier*

BetweenModifier
    ::= 'nested' | 'escaped' STRING?

CharsType
    ::= 'chars' '[' INTEGER ']'

TokenType
    ::= 'token'

RestType
    ::= 'rest'

WhitespaceType
    ::= 'whitespace' Quantifier?

Quantifier
    ::= '+' | '*' | '?'

OptionalType
    ::= 'optional' TextType

RepeatType
    ::= 'repeat' TextType RepeatUntil?

RepeatUntil
    ::= 'until' (STRING | 'end')

SwitchType
    ::= 'switch' '{' (SwitchCase+ DefaultCase? | DefaultCase) '}'

SwitchCase
    ::= 'pattern' REGEX '=>' TextType ','?

DefaultCase
    ::= '_' '=>' TextType ','?

TextModifiers
    ::= TextModifier+

TextModifier
    ::= 'trim' | 'rtrim' | 'ltrim' | 'lower' | 'upper' | 'greedy' | 'lazy'
```

---

## 11. Examples

### 11.1 PNG File Header

```sql
binary PngSignature {
    Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47,
                                          0x0D, 0x0A, 0x1A, 0x0A]
}

binary PngChunk {
    Length:     int be,
    ChunkType:  string[4] ascii,
    Data:       byte[Length],
    Crc:        int be
}

binary IhdrData {
    Width:              int be,
    Height:             int be,
    BitDepth:           byte,
    ColorType:          byte,
    CompressionMethod:  byte,
    FilterMethod:       byte,
    InterlaceMethod:    byte
}

let content: byte[] = array { 137ub, 80ub, 78ub, 71ub, 13ub, 10ub, 26ub, 10ub, 0ub, 0ub, 0ub, 13ub, 73ub, 72ub, 68ub, 82ub, 0ub, 0ub, 0ub, 1ub, 0ub, 0ub, 0ub, 2ub, 8ub, 6ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub };
SELECT
    input.Name,
    ihdr.Width,
    ihdr.Height,
    ihdr.BitDepth,
    ihdr.ColorType
FROM values { (Name: 'image') } input
CROSS APPLY Interpret<PngSignature>($content) sig
CROSS APPLY InterpretAt<PngChunk>($content, 8) chunk
CROSS APPLY Interpret<IhdrData>(chunk.Data) ihdr
WHERE chunk.ChunkType = 'IHDR'
```

### 11.2 Apache Combined Log Format

```sql
text ApacheLog {
    RemoteHost:    until ' ',
    Identity:      until ' ',
    User:          until ' ',
    Timestamp:     between '[' ']',
    _:             literal ' "',
    Method:        until ' ',
    Path:          until ' ',
    Protocol:      until '"',
    _:             literal ' ',
    Status:        pattern '\d{3}',
    _:             literal ' ',
    Size:          pattern '\d+|-',
    _:             optional literal ' ',
    Referrer:      optional between '"' '"',
    _:             optional literal ' ',
    UserAgent:     optional between '"' '"'
}

SELECT
    log.Path,
    Count(*) AS ErrorCount,
    MaxDateTime(ToDateTimeWithFormat(log.Timestamp, 'dd/MMM/yyyy:HH:mm:ss zzz')) AS LastSeen
FROM values { (Line: '127.0.0.1 - - [02/Jan/2024:03:04:05 +0000] "GET /missing HTTP/1.1" 404 10 "-" "agent"') } input
CROSS APPLY Parse<ApacheLog>(input.Line) log
WHERE log.Status = '404'
GROUP BY log.Path
ORDER BY ErrorCount DESC
TAKE 20
```

### 11.3 Mixed Binary/Text Protocol

```sql
binary MessageFrame {
    Sync:        ushort le check Sync = 0xAA55,
    MsgType:     byte,
    PayloadLen:  short le,
    Payload:     byte[PayloadLen],
    Checksum:    short le
}

text CommandPayload {
    Command:   until ' ',
    _:         whitespace*,
    Args:      rest
}

binary TelemetryPayload {
    Timestamp:   long le,
    SensorId:    short le,
    Value:       double le,
    Flags:       byte
}

let content: byte[] = array { 85ub, 170ub, 1ub, 6ub, 0ub, 80ub, 73ub, 78ub, 71ub, 32ub, 49ub, 0ub, 0ub };
SELECT
    frame.MsgType,
    CASE frame.MsgType
        WHEN 0x01 THEN command.Command
        WHEN 0x02 THEN ToString(telemetry.SensorId)
        ELSE 'Unknown'
    END AS Identifier
FROM values { (Seed: 0) } seed
CROSS APPLY InterpretAt<MessageFrame>($content, 0) frame
OUTER APPLY TryParse<CommandPayload>(ToText(frame.Payload, 'utf8')) command
OUTER APPLY TryInterpret<TelemetryPayload>(frame.Payload) telemetry
WHERE frame.MsgType IN (0x01, 0x02)
```

### 11.4 COBOL Copybook Record

```sql
text CobolCustomerRecord {
    CustomerId:      chars[10],
    CustomerName:    chars[30] trim,
    AddressLine1:    chars[40] trim,
    AddressLine2:    chars[40] trim,
    City:            chars[20] trim,
    State:           chars[2],
    ZipCode:         chars[10] trim,
    Balance:         chars[12],
    StatusCode:      chars[1],
    LastUpdate:      chars[8]
}

SELECT
    r.CustomerId,
    r.CustomerName,
    Concat(r.AddressLine1, ', ', r.City, ', ', r.State, ' ', r.ZipCode) AS Address,
    ToDecimal(r.Balance) / 100.0 AS Balance,
    ToDateTimeWithFormat(r.LastUpdate, 'yyyyMMdd') AS LastUpdated
FROM values { (Line: '0000000001Ada                           Main Street                                                                     WARSAW              PL00-001    000000012345A20240102') } input
CROSS APPLY Parse<CobolCustomerRecord>(input.Line) r
WHERE r.StatusCode = 'A'
```

### 11.5 Invented Mixed Samples

This layout and its bytes are invented for this example. The result is revision
`3`, one sample, choice `7`, and text `toy`.

```sql
binary ToyParameters {
    Revision:       short le,
    Options:        byte,
    HasHint:        (Options & 0x20) <> 0,
    UsesAlternateValues: (Options & 0x04) <> 0,
    SampleCount:    int le,
    Hint:           long le when HasHint,
    SampleStart:    long le
}

binary ToyLabel {
    Name: string[5] ascii nullterm,
    Note: string[9] utf8 nullterm
}

binary ToySample {
    Choice:         byte,
    TextCount:      short le when Choice = 7,
    TextValue:      string[TextCount] utf8 when Choice = 7,
    ByteCount:      byte when Choice = 9,
    ByteValues:     byte[ByteCount] when Choice = 9,
    LabelCount:     byte when Choice = 11,
    Labels:         ToyLabel[LabelCount] when Choice = 11
}

let content: byte[] = array { 3ub, 0ub, 0ub, 1ub, 0ub, 0ub, 0ub, 15ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 0ub, 7ub, 3ub, 0ub, 116ub, 111ub, 121ub };

SELECT
    parameters.Revision,
    parameters.SampleCount,
    sample.Choice,
    CASE sample.Choice
        WHEN 7 THEN sample.TextValue
        WHEN 9 THEN ToHex(sample.ByteValues)
        WHEN 11 THEN 'Labels: ' + ToString(sample.LabelCount)
        ELSE 'Other sample'
    END AS Content
FROM values { (Seed: 0) } seed
CROSS APPLY Interpret<ToyParameters>($content) parameters
CROSS APPLY InterpretAt<ToySample>($content, ToInt32(parameters.SampleStart) ?? 0) sample
```

---

## 12. Future Considerations

### 12.1 Potential Extensions

- **Enum fields** — Direct enum field annotations are not part of the
  first-class enum v1 profile. A future extension must define the stored
  carrier, endianness, textual representation, unknown-value behavior, and
  decoding semantics explicitly; core TABLE enum support does
  not imply interpretation-schema enum support.
- **Inline property access on Interpret / Parse** — Allow `Interpret<Header>(data).Magic` syntax directly in SELECT, WHERE, and CASE expressions without requiring CROSS APPLY. This would let users access a single field from a parsed result inline. When accessing multiple fields, CROSS APPLY would remain preferred to avoid redundant parsing. Currently, all interpretation functions must appear inside CROSS APPLY or OUTER APPLY (see §3.3).
- **Binary switch variant extensions** (reserved; see §4.12) — range case labels (`1..3 => ...`), expression case labels, direct arrays of switch types, branch-level `check` constraints, and mixed text/binary switch branches.
- Compression/encryption integration
- Schema versioning annotations
- Schema imports from external files

### 12.2 Performance Considerations

- Zero-copy span parsing
- Memory-mapped file support
- Lazy field evaluation
- Schema compilation caching

### 12.3 Tooling Support

- Schema IntelliSense
- Hex view integration
- Schema diffing
- Test data generation

---

## Appendix A: Reserved Keywords

```
align, AND, as, ascii, at, be, between, binary, bits, byte,
capture, CASE, chars, check, double, ebcdic, ELSE, END,
escaped, extends, float, greedy, IN, int, latin1, lazy, le,
LIKE, literal, long, lower, ltrim, nested, NOT, NULL, nullterm,
optional, OR, pattern, repeat, rest, rtrim, sbyte, short,
string, substream, switch, text, THEN, token, trim, uint, ulong, until,
upper, ushort, utf16be, utf16le, utf8, WHEN, whitespace
```

---

## Appendix B: Type Mapping

| Schema Type | Logical type | Size (bytes) |
|-------------|-----------|--------------|
| `byte` | `byte` | 1 |
| `sbyte` | `sbyte` | 1 |
| `short le/be` | `short` | 2 |
| `ushort le/be` | `ushort` | 2 |
| `int le/be` | `int` | 4 |
| `uint le/be` | `uint` | 4 |
| `long le/be` | `long` | 8 |
| `ulong le/be` | `ulong` | 8 |
| `float le/be` | `float` | 4 |
| `double le/be` | `double` | 8 |
| `byte[n]` | `byte[]` | n |
| `string[n] enc` | `string` | n |
| `bits[n]` | `byte` (1-8), `ushort` (9-16), `uint` (17-32), `ulong` (33-64) | ⌈n/8⌉ |
| Schema reference | Logical record | Variable |
| Array `T[n]` | `T[]` | n × sizeof(T) |
| `substream[n] raw` | `byte[]` | n |
| `substream[n] as T` | T (nested value) | n |

---

## Appendix C: Error Codes

| Code | Category | Description |
|------|----------|-------------|
| ISE0001 | Input | Insufficient data to complete parsing |
| ISE0002 | Validation | Validation constraint failed |
| ISE0003 | Pattern | Pattern match failed at current position |
| ISE0004 | Literal | Expected literal not found at current position |
| ISE0005 | Delimiter | Delimiter not found in remaining input |
| ISE0006 | Delimiter | Expected opening delimiter not found |
| ISE0007 | Expression | Invalid size expression value (null, fractional, non-finite, negative or overflow) |
| ISE0008 | Position | Invalid position value (null, fractional, non-finite, negative or overflow) |
| ISE0009 | Control Flow | Maximum repeat iteration count exceeded or a `repeat until eof` element made no progress |
| ISE0010 | Encoding | String encoding error |
| ISE0011 | Whitespace | Expected whitespace not found |
| ISE0012 | Alternative | Switch/alternative exhausted without match |
| ISE0013 | Schema | Circular or undefined schema reference |
| ISE0014 | Expression | Field reference resolution error |
| ISE0015 | General | General parsing error |
| MQ3033 | Bind | Interpret/Parse function used outside CROSS APPLY or OUTER APPLY |

---

## Appendix D: CROSS APPLY / OUTER APPLY Behavior

### D.1 Behavior Summary

| Expression Type | CROSS APPLY | OUTER APPLY |
|-----------------|-------------|-------------|
| ordered collection of T (N items) | N rows | N rows (or 1 NULL if empty) |
| Single object `T` | 1 row | 1 row |
| `null` | 0 rows | 1 row (NULL) |

When an APPLY evaluates a non-null Interpret or Parse result, it exposes exactly one row containing the parsed fields. For OUTER APPLY, a `null` result still produces one output row with all aliased columns set to NULL.

---

*End of Specification*
