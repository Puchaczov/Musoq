# Musoq Interpretation Schemas: Language Extension Specification

**Version:** 1.0.0
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
12. [Future Considerations](#12-future-considerations)\n13. [Appendix A: Reserved Keywords](#appendix-a-reserved-keywords)\n14. [Appendix B: Type Mapping](#appendix-b-type-mapping)\n15. [Appendix C: Error Codes](#appendix-c-error-codes)\n16. [Appendix D: CROSS APPLY / OUTER APPLY Behavior](#appendix-d-cross-apply--outer-apply-behavior)

---

## 1. Introduction

### 1.1 Purpose

Interpretation Schemas extend Musoq's SQL dialect with declarative format definitions for parsing binary and textual data. Schemas serve triple duty as:

1. **Executable parsers** — Compiled to efficient executable parsing code at query compile time
2. **Format documentation** — Human-readable specifications that cannot drift from implementation
3. **Queryable metadata** — Schema definitions are themselves data sources

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
| Types | Familiar names (`int`, `short`, `long`, `byte`) | Familiar naming, native integration |
| Operators | SQL operators (`<>`, `AND`, `OR`, `NOT`) | Consistency with Musoq SQL dialect |
| Keywords | Case-insensitive | SQL convention |
| Identifiers | Case-sensitive for field names | Matches generated code conventions |
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

### 2.5 Zero Runtime Reflection

All schema definitions compile to statically-typed code. No runtime type inspection, no dynamic dispatch, no interpretation overhead.

### 2.6 SQL Spirit Alignment

The extension uses SQL-compatible constructs where possible:

| Concept | SQL Parallel | Schema Syntax |
|---------|--------------|---------------|
| Constraints | `CHECK` constraint | `check` clause |
| Conditional | `CASE WHEN` | `when` clause |
| Type reference | `CAST(x AS type)` | `Interpret<Type>(data)` |
| Null semantics | Three-valued logic | Conditional fields yield `null` |

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
SELECT h.Magic, r.Id, r.Value
FROM os.file('/data.bin') f
CROSS APPLY Interpret<Header>(f.GetBytes()) h
CROSS APPLY InterpretAt<Record>(f.GetBytes(), 6) r
```

**Scope Rules:**
- Schemas are visible only within the query batch where defined
- Schema names MUST be unique within a batch
- Forward references between schemas MUST NOT occur (referenced schema MUST be defined first)
- Recursive schema definitions MUST NOT occur

### 3.2 CROSS APPLY / OUTER APPLY Relaxation

**Current Behavior:**
CROSS APPLY and OUTER APPLY require the right-hand expression to yield an enumerable (table-valued). Each element produces one output row.

**Extended Behavior:**
Both CROSS APPLY and OUTER APPLY accept enumerable and scalar complex types:

| Right-Hand Type | CROSS APPLY Behavior | OUTER APPLY Behavior |
|-----------------|----------------------|----------------------|
| `IEnumerable<T>` | One row per element; no rows if empty | One row per element; one NULL row if empty |
| Complex object `T` | Single output row with object bound to alias | Single output row with object bound to alias |
| `null` | No output rows | One row with NULL alias |

**Rationale:**
`Interpret<Schema>()` returns a single structured object, not a collection. Relaxing CROSS/OUTER APPLY allows uniform syntax consistent with Musoq's existing patterns.

```sql
SELECT h.Magic, h.Version
FROM os.file('/data.bin') f
CROSS APPLY Interpret<Header>(f.GetBytes()) h
```

**Semantics:**
- File produces 1 row
- `Interpret` produces 1 Header object
- CROSS APPLY binds the object to alias `h`
- Result: 1 row with access to `h.Magic`, `h.Version`, etc.

**For arrays within schemas:**
Arrays remain enumerable and work with CROSS/OUTER APPLY as collections:

```sql
SELECT c.Magic, r.Id, r.Value
FROM os.file('/data.bin') f
CROSS APPLY Interpret<Container>(f.GetBytes()) c
CROSS APPLY c.Records r
```

**Implementation:**
Single complex objects are wrapped in a single-element sequence for uniform processing. A `null` result with CROSS APPLY produces no output rows; a `null` result with OUTER APPLY produces one row with NULL columns.

### 3.3 APPLY-Only Restriction

All interpretation functions — `Interpret<Schema>()`, `Parse<Schema>()`, `TryInterpret<Schema>()`, `TryParse<Schema>()`, `InterpretAt<Schema>()`, `PartialInterpret<Schema>()`, and `PartialParse<Schema>()` — MUST appear as the right-hand side of a `CROSS APPLY` or `OUTER APPLY` clause. They MUST NOT be used directly in `SELECT`, `WHERE`, `HAVING`, or other expression contexts.

**Valid usage:**

```sql
-- CROSS APPLY — parsing succeeds or the row is excluded
SELECT h.Magic, h.Version
FROM os.file('/data.bin') f
CROSS APPLY Interpret<Header>(f.GetBytes()) h

-- OUTER APPLY — row is preserved with NULLs when parsing fails
SELECT log.Timestamp, log.Level
FROM os.file('/app.log') f
CROSS APPLY Lines(f.GetContent()) line
OUTER APPLY TryParse<LogEntry>(line.Value) log
```

**Invalid usage (compile-time error MQ3033):**

```sql
-- ERROR: Parse cannot appear in SELECT
SELECT Parse<LogEntry>(line.Value)
FROM ...

-- ERROR: TryInterpret cannot appear in WHERE
SELECT 1
FROM os.files('./', '*.bin') f
WHERE TryInterpret<Header>(f.GetBytes()) IS NOT NULL
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

| Type | Endianness | Size | .NET Type | Notes |
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

**Endianness Rules:**
- Single-byte types (`byte`, `sbyte`): Endianness is not applicable (a single byte has no byte order)
- Multi-byte types: Endianness MUST be specified (`le` or `be`)
- Omitting endianness for multi-byte types is a parse error

#### 4.2.2 Floating-Point Types

| Type | Endianness | Size | .NET Type |
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
| `utf16le` | 2 | UTF-16 Little Endian |
| `utf16be` | 2 | UTF-16 Big Endian |
| `ascii` | 1 | 7-bit ASCII (values 0-127) |
| `latin1` | 1 | ISO-8859-1 |
| `ebcdic` | 1 | IBM EBCDIC (Code Page 037) |

**Modifier Application Order:**
1. Read raw bytes according to size
2. Decode using specified encoding
3. Apply `nullterm` (truncate at first null)
4. Apply trim modifiers (`ltrim`, `rtrim`, or `trim`)

| Modifier | Behavior |
|----------|----------|
| `trim` | Remove leading AND trailing whitespace/nulls |
| `rtrim` | Remove trailing whitespace/nulls only |
| `ltrim` | Remove leading whitespace/nulls only |
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

### 4.4 Arrays

```
ArrayType ::= Type '[' SizeExpression ']'
```

Arrays may be of any type: primitives, strings, bytes, or schemas.

```sql
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
- Arrays are exposed as `IEnumerable<T>` for CROSS APPLY iteration

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
- When condition is FALSE: field is NOT parsed, cursor does NOT advance, field value is `null`
- When condition is TRUE: field is parsed normally
- Conditions may only reference fields declared BEFORE the conditional field
- Subsequent fields referencing a conditional field MUST handle potential `null`

**Null Propagation:**
When a conditional field is `null`, any expression referencing it evaluates to `null`:

```sql
binary Example {
    HasData: byte,
    Length:  int le when HasData <> 0,
    -- If HasData = 0, then Length is null, and this array has size null (0 elements)
    Data:    byte[Length] when HasData <> 0
}
```

### 4.6 Computed Fields

Fields that derive values without consuming input:

```
ComputedField ::= Identifier ':' Expression
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

**Semantics:**
- Computed fields do NOT advance the cursor
- Expression may reference any previously declared field (parsed or computed)
- Computed fields are evaluated immediately after all parsing completes
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
5. Maximum bit field size is 64 bits

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
    Magic:    int le check Magic = 0xDEADBEEF,
    Version:  short le check Version >= 1 AND Version <= 5,
    Length:   int le check Length <= 1048576,
    Checksum: int le check Checksum = Crc32(Data),
    Data:     byte[Length]
}
```

**Semantics:**
- Field is parsed first, then validation is evaluated
- Failed validation raises a parse error (ISE0002) with field name and constraint
- Validation may reference the current field AND any previously declared field
- Validation expressions must evaluate to boolean

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
an alias of `const`; both compile identically and exist so signature/magic-number
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
- A failed validation raises `ParseException(ValidationFailed)`. With
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
binary TlvStream {
    -- Read TLV records until type is 0x00 (terminator)
    Records: TlvRecord repeat until Records[-1].Type = 0x00
}

binary TlvRecord {
    Type:   byte,
    Length: short le,
    Value:  byte[Length]
}
```

**Semantics (`repeat until` Expression):**
- Parses elements repeatedly until condition becomes TRUE
- `Records[-1]` refers to the most recently parsed element
- Condition is evaluated AFTER each element is parsed
- At least one element is always attempted (do-while)
- Maximum iteration count is implementation-defined (default: 10,000)

**Repeat until end of input (`repeat until eof`):**

For sequences that fill the remaining input span:

```sql
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

A switch field produces a tagged-union value with:

- A `Case` member holding the selected branch alias as a string (e.g. `"Login"`,
  `"Raw"`).
- One member per branch alias. Exactly one branch member — the one matching the
  selector — is non-null; all other branch members are null.

```sql
SELECT
    p.Type,
    p.Payload.Case,            -- selected branch alias, e.g. 'Login'
    p.Payload.Login.UserId,    -- non-null only when Type = 1
    p.Payload.Raw              -- non-null only when no case matched
FROM os.file('/packet.bin') f
CROSS APPLY Interpret<Packet>(f.GetBytes()) p
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

#### 4.12.5 v1 Limits

- The selector MUST be a reference to a previously declared field; arbitrary
  expressions are not permitted.
- Case labels MUST be constant scalar literals (integers, etc.); ranges and
  expression cases are reserved (see §12).
- `_` is an OPTIONAL default; there is no fallthrough between cases.
- A switch field is a single value. Arrays of switch fields are not written
  directly; wrap the switch in a nested schema and make an array of that schema.

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
- `exact` fails with a parse error if the nested parser consumes fewer than
  `Length` bytes, catching truncated or mis-sized payloads.
- `lax` permits the nested parser to consume fewer than `Length` bytes; the
  remaining bytes inside the substream are discarded.

#### 4.13.3 v1 Limits

- Supported targets are `raw`, a schema-reference type, and an inline schema.
- Switch (tagged-union) substream targets are reserved and not yet supported.
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
    _:         literal ': ',
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
- Match MUST occur at current position (implicit `\G` anchor)
- Cursor advances past the entire match
- Match failure raises a parse error (ISE0003)

Examples:

```sql
text Example {
    -- Simple pattern
    Ip:       pattern '\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}',

    -- Pattern with named captures (creates multiple fields)
    Coords:   pattern '(?<Lat>-?\d+\.\d+),(?<Lon>-?\d+\.\d+)'
              capture (Lat, Lon),

    -- Quantified patterns
    Digits:   pattern '\d+',
    Hex:      pattern '[0-9A-Fa-f]+'
}
```

**Capture Semantics:**
- Named groups in the regex create separate accessible fields
- The `capture` clause lists which groups to expose
- The main field contains the full match; captured groups are sub-fields

### 5.3 Literal Matching

```
LiteralField ::= 'literal' StringLiteral
```

Expects exact string at current position. Advances cursor past the literal.

```sql
text HttpRequest {
    Method:   until ' ',
    _:        literal ' ',
    Path:     until ' ',
    _:        literal ' ',
    Version:  until '\r\n',
    _:        literal '\r\n'
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
    _:     literal ': ',
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
    String2: between '''' '''' escaped ''''''
}
```

**Semantics:**
- Default escape character is backslash (`\`)
- Escaped delimiters are included in the captured content
- The escape character itself can be escaped (`\\` → `\`)

### 5.5 Fixed-Width Fields

```
CharsField ::= 'chars' '[' IntegerLiteral ']' Modifiers?
```

Captures exactly N characters.

```sql
text CobolRecord {
    CustomerId:  chars[10],
    Name:        chars[30] trim,
    Balance:     chars[12] rtrim,
    Status:      chars[1]
}
```

**Semantics:**
- Reads exactly N characters (not bytes)
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
    _:     literal '=',
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
    _:         literal ': ',
    Message:   until '\t',

    -- Optional trace ID at end
    _:         optional literal '\t',
    TraceId:   optional pattern '[a-f0-9]{32}'
}
```

**Semantics:**
- Attempts to match the inner field type
- On failure: field is `null`, cursor is UNCHANGED (no backtracking needed)
- On success: field has value, cursor advances as normal
- `optional` only looks ahead; it does not consume input on failure

**Backtracking Rule:**
Optional fields use lookahead, not backtracking. The cursor position is saved before attempting the match and restored if the match fails.

### 5.9 Repetition

```
RepeatField ::= 'repeat' TextField UntilClause?

UntilClause ::= 'until' (StringLiteral | 'end')
```

Examples:

```sql
text HttpHeaders {
    -- Repeat until empty line
    Headers: repeat HeaderLine until '\r\n'
}

text HeaderLine {
    Name:  until ':',
    _:     literal ': ',
    Value: until '\r\n',
    _:     literal '\r\n'
}
```

**Semantics:**
- Parses the inner type repeatedly
- `until` delimiter: stops when delimiter is found (delimiter consumed)
- `until end`: stops at end of input
- No `until` clause: stops at end of input (same as `until end`)
- Results in array of parsed elements

### 5.10 Alternatives (Switch)

```
SwitchField ::= 'switch' '{' SwitchCase+ DefaultCase? '}'

SwitchCase ::= 'pattern' RegexLiteral '=>' TextType ','?

DefaultCase ::= '_' '=>' TextType ','?
```

Choose parsing strategy based on lookahead:

```sql
text ConfigLine {
    Content: switch {
        pattern '\s*\[' => SectionHeader,
        pattern '\s*#'  => Comment,
        pattern '\s*;'  => Comment,
        pattern '\s*$'  => Empty,
        _               => KeyValue
    }
}

text SectionHeader {
    _:    pattern '\s*',
    _:    literal '[',
    Name: until ']',
    _:    literal ']'
}

text KeyValue {
    _:     pattern '\s*',
    Key:   until '=',
    _:     literal '=',
    Value: rest trim
}

text Comment {
    _:    pattern '\s*[#;]',
    Text: rest
}

text Empty {}
```

**Semantics:**
- Patterns are tested in order using lookahead (no cursor movement)
- First matching pattern determines which type to parse
- Default case (`_`) matches if no patterns match
- No default + no match raises parse error
- The matching pattern's text is NOT consumed; the selected type parses from current position

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
4. Apply case conversion (`lower`/`upper`)
5. Apply trim (`ltrim`, `rtrim`, `trim`)

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

Expressions may appear in:
- Size specifications: `byte[Length]`
- Conditions: `when HasData <> 0`
- Validations: `check Checksum = Crc32(Data)`
- Computed fields: `Total := Count * Size`
- Positions: `at HeaderOffset + 16`

**Expression Grammar (SQL-style operators):**

```
Expression ::= OrExpression

OrExpression ::= AndExpression ('OR' AndExpression)*

AndExpression ::= NotExpression ('AND' NotExpression)*

NotExpression ::= 'NOT' NotExpression | CompareExpression

CompareExpression ::= BitwiseExpression (CompareOp BitwiseExpression)?

CompareOp ::= '=' | '<>' | '<' | '>' | '<=' | '>=' | 'LIKE' | 'IN'

BitwiseExpression ::= AddExpression (BitwiseOp AddExpression)*

BitwiseOp ::= '&' | '|' | '^' | '<<' | '>>'

AddExpression ::= MulExpression (AddOp MulExpression)*

AddOp ::= '+' | '-'

MulExpression ::= UnaryExpression (MulOp UnaryExpression)*

MulOp ::= '*' | '/' | '%'

UnaryExpression ::= '-' UnaryExpression
                  | '~' UnaryExpression
                  | 'NOT' UnaryExpression
                  | PrimaryExpression

PrimaryExpression ::= Literal
                    | FieldReference
                    | FunctionCall
                    | '(' Expression ')'
                    | ArrayAccess
                    | CaseExpression
                    | NullLiteral

FieldReference ::= Identifier ('.' Identifier)*

ArrayAccess ::= FieldReference '[' Expression ']'

FunctionCall ::= Identifier '(' ArgumentList? ')'

CaseExpression ::= 'CASE' WhenClause+ ElseClause? 'END'

WhenClause ::= 'WHEN' Expression 'THEN' Expression

ElseClause ::= 'ELSE' Expression

NullLiteral ::= 'NULL'
```

**Operator Summary:**

| Category | Operators | Precedence (high to low) |
|----------|-----------|--------------------------|
| Unary | `-`, `~`, `NOT` | 1 (highest) |
| Multiplicative | `*`, `/`, `%` | 2 |
| Additive | `+`, `-` | 3 |
| Bitwise | `&`, `\|`, `^`, `<<`, `>>` | 4 |
| Comparison | `=`, `<>`, `<`, `>`, `<=`, `>=`, `LIKE`, `IN` | 5 |
| Logical AND | `AND` | 6 |
| Logical OR | `OR` | 7 (lowest) |

### 6.4 Field Reference Scoping

Within a schema, expressions may reference:
- Fields declared BEFORE the current field (by name)
- The current field (only in `check` clauses)
- Nested fields via dot notation: `Header.Version`
- Array elements via indexing: `Items[0]`, `Records[-1]` (last element)

**Forward references MUST NOT occur.** This ensures single-pass parsing.

### 6.5 Built-in Functions for Schemas

Schema expressions have access to the following built-in functions. These are **not** discoverable via `desc functions` because they are intrinsic to the schema definition language rather than provided by a schema plugin.

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

These functions are available within `check` clauses, computed field expressions, and `when` conditions.

---

## 7. Interpretation Functions

### 7.1 Method Signature Pattern

Interpretation functions are generic methods in the Musoq method library. Each schema definition generates a typed interpreter, and the engine pairs it with the appropriate interpretation function based on schema kind (binary or text).

- **Binary interpretation**: `Interpret<SchemaType>(data)` — the engine passes the byte array and the generated binary interpreter to the underlying generic method.
- **Text interpretation**: `Parse<SchemaType>(text)` — the engine passes the string and the generated text interpreter to the underlying generic method.

The generated interpreter encapsulates all parsing logic derived from the schema definition. Query authors interact only with the SQL-level syntax shown in §7.2–§7.7; the generic dispatch is an engine implementation detail.

### 7.2 SQL Syntax for Interpretation

In SQL queries, the schema type is referenced by name (without parentheses), consistent with SQL type references:

```sql
-- Schema type referenced by name, no parentheses
SELECT h.Magic, h.Version
FROM os.file('/data.bin') f
CROSS APPLY Interpret<Header>(f.GetBytes()) h
```

**The engine:**
1. Recognizes `Header` as a schema type defined in the query batch
2. Instantiates the generated interpreter class
3. Calls the generic `Interpret` method with the interpreter instance

### 7.3 Binary Interpretation

```sql
Interpret<SchemaType>(data)
```

Parses byte array according to schema, returning structured result.

```sql
SELECT h.Magic, h.Version
FROM os.file('/data.bin') f
CROSS APPLY Interpret<Header>(f.GetBytes()) h
```

### 7.4 Text Interpretation

```sql
Parse<SchemaType>(text)
```

Parses string according to text schema.

```sql
SELECT log.Timestamp, log.Level, log.Message
FROM os.file('/app.log') f
CROSS APPLY Lines(f.GetContent()) line
CROSS APPLY Parse<LogEntry>(line.Value) log
```

### 7.5 Safe Interpretation

```sql
TryInterpret<SchemaType>(data) -> SchemaType?
TryParse<SchemaType>(text) -> SchemaType?
```

Returns `null` instead of throwing on parse failure. Like all interpretation functions, `TryInterpret` and `TryParse` MUST be used inside `CROSS APPLY` or `OUTER APPLY` (see §3.3).

```sql
-- Use OUTER APPLY so that rows are preserved when parsing fails
SELECT
    f.Name,
    CASE WHEN h.Magic IS NOT NULL
         THEN 'Valid'
         ELSE 'Invalid'
    END AS Status
FROM os.files('./', '*.bin') f
OUTER APPLY TryInterpret<Header>(f.GetBytes()) h
```

**Behavior with CROSS/OUTER APPLY:**
- `null` result with CROSS APPLY: no output row
- `null` result with OUTER APPLY: one row with NULL alias

### 7.6 Offset-Based Interpretation

```sql
InterpretAt<SchemaType>(data, offset)
```

Begin parsing at specified byte offset:

```sql
SELECT
    h.Magic,
    d.RecordCount
FROM os.file('/complex.bin') f
CROSS APPLY Interpret<Header>(f.GetBytes()) h
CROSS APPLY InterpretAt<DataSection>(f.GetBytes(), h.DataOffset) d
```

### 7.7 Multiple Records

For files containing multiple sequential records of the same type:

**Option 1: Container schema with array**

```sql
binary RecordFile {
    Count:   int le,
    Records: Record[Count]
}

SELECT r.Id, r.Value
FROM os.file('/records.bin') f
CROSS APPLY Interpret<RecordFile>(f.GetBytes()) file
CROSS APPLY file.Records r
```

**Option 2: Explicit offset calculation**

```sql
WITH RecordOffsets AS (
    SELECT n.Value * 64 AS Offset
    FROM range.numbers(0, 100) n
)
SELECT r.Id, r.Value
FROM os.file('/records.bin') f
CROSS JOIN RecordOffsets o
CROSS APPLY InterpretAt<Record>(f.GetBytes(), o.Offset) r
WHERE o.Offset < Length(f.GetBytes())
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
| `ParsedFields` | Dictionary | Successfully parsed field names and values |
| `ErrorField` | `string?` | Name of field where parsing failed (`null` if successful) |
| `ErrorMessage` | `string?` | Error description (`null` if successful) |
| `BytesConsumed` | `int` | Number of bytes/characters successfully processed |

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
- Child schema includes all parent fields first
- Child fields are appended after parent fields
- Parent fields are accessible by name in child
- Single inheritance only (no multiple extends)

### 8.4 Generic Schemas

Parameterized schemas for reusable patterns:

```sql
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
- Generic schemas are instantiated at compile time

### 8.5 Binary-Text Composition

Text schemas may be embedded in binary schemas:

```sql
text KeyValue {
    Key:   until '=',
    _:     literal '=',
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
- **Position**: Byte/character offset where error occurred
- **Field**: Field being parsed when error occurred
- **Schema**: Schema name (and path for nested schemas)
- **Expected**: What the parser expected
- **Actual**: What was found
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
| General errors | ISE0009, ISE0015 | Repeat limit exceeded or general parsing failure |

### 9.3 Error Behavior

| Function | On Error |
|----------|----------|
| `Interpret` | Throws exception |
| `Parse` | Throws exception |
| `TryInterpret` | Returns `null` |
| `TryParse` | Returns `null` |
| `InterpretAt` | Throws exception |

### 9.4 Partial Results (Debugging)

For debugging malformed data:

```sql
SELECT
    p.ParsedFields,
    p.ErrorField,
    p.ErrorMessage,
    p.BytesConsumed
FROM os.file('/corrupted.bin') f
CROSS APPLY PartialInterpret<Header>(f.GetBytes()) p
```

**`PartialInterpret` and `PartialParse` return:**
- `ParsedFields`: Dictionary of successfully parsed field names and values
- `ErrorField`: Name of field where parsing failed (null if successful)
- `ErrorMessage`: Error description (null if successful)
- `BytesConsumed`: Number of bytes or characters successfully processed

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
    ::= Identifier ':' (BinaryType FieldModifiers? | Expression)

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
    |   SubstreamType
    |   RepeatUntilType

SubstreamType
    ::= 'substream' '[' Expression ']' ('raw' | 'as' BinaryType ('exact' | 'lax')?)

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
    ::= '<' BinaryType (',' BinaryType)* '>'

ArrayType
    ::= BinaryType '[' SizeExpr ']'

InlineSchema
    ::= '{' BinaryFieldList '}'

SwitchType
    ::= 'switch' FieldReference '{' SwitchCase (',' SwitchCase)* ','? '}'

SwitchCase
    ::= (ScalarLiteral | '_') '=>' Identifier ':' BinaryType

FieldModifiers
    ::= PositionMod? ConditionMod? ValidationMod?

PositionMod
    ::= 'at' Expression

ConditionMod
    ::= 'when' Expression

ValidationMod
    ::= 'check' Expression

SizeExpr
    ::= Expression
```

### 10.2 Complete Text Schema Grammar

```ebnf
TextSchema
    ::= 'text' Identifier '{' TextFieldList '}'

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
    ::= 'switch' '{' SwitchCase+ DefaultCase? '}'

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
    Signature: byte[8] check Signature = [0x89, 0x50, 0x4E, 0x47,
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

SELECT
    f.Name,
    ihdr.Width,
    ihdr.Height,
    ihdr.BitDepth,
    ihdr.ColorType
FROM os.files('./', '*.png') f
CROSS APPLY Interpret<PngSignature>(f.GetBytes()) sig
CROSS APPLY InterpretAt<PngChunk>(f.GetBytes(), 8) chunk
CROSS APPLY Interpret<IhdrData>(chunk.Data) ihdr
WHERE chunk.ChunkType = 'IHDR'
```

### 11.2 Apache Combined Log Format

```sql
text ApacheLog {
    RemoteHost:    until ' ',
    _:             literal ' ',
    Identity:      until ' ',
    _:             literal ' ',
    User:          until ' ',
    _:             literal ' ',
    Timestamp:     between '[' ']',
    _:             literal ' "',
    Method:        until ' ',
    _:             literal ' ',
    Path:          until ' ',
    _:             literal ' ',
    Protocol:      until '"',
    _:             literal '" ',
    Status:        pattern '\d{3}',
    _:             literal ' ',
    Size:          pattern '\d+|-',
    _:             optional literal ' "',
    Referrer:      optional between '"' '"',
    _:             optional literal ' "',
    UserAgent:     optional between '"' '"'
}

SELECT
    log.Path,
    Count(*) AS ErrorCount,
    Max(log.Timestamp) AS LastSeen
FROM os.file('/var/log/apache2/access.log') f
CROSS APPLY Lines(f.GetContent()) line
CROSS APPLY Parse<ApacheLog>(line.Value) log
WHERE log.Status = '404'
GROUP BY log.Path
ORDER BY ErrorCount DESC
TAKE 20
```

### 11.3 Mixed Binary/Text Protocol

```sql
binary MessageFrame {
    Sync:        short le check Sync = 0xAA55,
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

SELECT
    frame.MsgType,
    CASE frame.MsgType
        WHEN 0x01 THEN command.Command
        WHEN 0x02 THEN ToString(telemetry.SensorId)
        ELSE 'Unknown'
    END AS Identifier
FROM os.file('/capture.bin') f
CROSS APPLY InterpretAt<MessageFrame>(f.GetBytes(), 0) frame
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
    ParseDate(r.LastUpdate, 'yyyyMMdd') AS LastUpdated
FROM os.file('/mainframe/CUSTMAST.DAT') f
CROSS APPLY Lines(f.GetContent()) line
CROSS APPLY Parse<CobolCustomerRecord>(line.Value) r
WHERE r.StatusCode = 'A'
```

### 11.5 Structured Storage with Mixed Data

```sql
binary StorageHeader {
    Magic:          int le check Magic = 0x53544F52,
    Version:        short le,
    Flags:          short le,

    IsCompressed: (Flags & 0x01) <> 0,
    HasIndex:     (Flags & 0x02) <> 0,

    RecordCount:    int le,
    IndexOffset:    long le when HasIndex,
    DataOffset:     long le
}

binary StorageRecord {
    RecordType:     byte,
    RecordLength:   int le,

    StringLen:      short le when RecordType = 1,
    StringData:     string[StringLen] utf8 when RecordType = 1,

    BlobData:       byte[RecordLength - 1] when RecordType = 2,

    NestedCount:    int le when RecordType = 3,
    Nested:         SubRecord[NestedCount] when RecordType = 3
}

binary SubRecord {
    Key:    string[32] utf8 nullterm,
    Value:  string[64] utf8 nullterm
}

SELECT
    h.Version,
    h.RecordCount,
    r.RecordType,
    CASE r.RecordType
        WHEN 1 THEN r.StringData
        WHEN 2 THEN ToHex(r.BlobData)
        WHEN 3 THEN 'Nested: ' + ToString(r.NestedCount) + ' items'
        ELSE 'Unknown record type'
    END AS Content
FROM os.file('/data/storage.dat') f
CROSS APPLY Interpret<StorageHeader>(f.GetBytes()) h
CROSS APPLY InterpretAt<StorageRecord>(f.GetBytes(), h.DataOffset) r
```

---

## 12. Future Considerations

### 12.1 Potential Extensions

- **Enum fields** — Direct enum field annotations are not part of the
  first-class enum v1 profile. A future extension must define the stored
  carrier, endianness, textual representation, unknown-value behavior, and
  generated zero-reflection decoder explicitly; core TABLE enum support does
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

| Schema Type | .NET Type | Size (bytes) |
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
| Schema reference | Generated class | Variable |
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
| ISE0007 | Expression | Invalid size expression value (negative or overflow) |
| ISE0008 | Position | Invalid position value (negative) |
| ISE0009 | Control Flow | Maximum iteration count exceeded in repeat |
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
| `IEnumerable<T>` (N items) | N rows | N rows (or 1 NULL if empty) |
| Single object `T` | 1 row | 1 row |
| `null` | 0 rows | 1 row (NULL) |

When the right-hand side of an APPLY is an `Interpret` or `Parse` call, the engine wraps the single result into a one-element sequence for CROSS APPLY processing. For OUTER APPLY, a `null` result still produces one output row with all aliased columns set to NULL.

---

*End of Specification*
