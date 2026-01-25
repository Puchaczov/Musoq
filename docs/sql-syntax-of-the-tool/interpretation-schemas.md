---
title: Interpretation Schemas
layout: home
parent: SQL Syntax of the Tool
nav_order: 10
---

# Interpretation Schemas

Interpretation schemas are a powerful feature that allows you to define how to parse binary data (bytes) or text data (strings) directly in SQL. This enables structured extraction of data from file formats, network protocols, log files, and other structured data sources.

## Overview

Interpretation schemas let you:
- Define **binary schemas** to parse byte sequences (files, packets, etc.)
- Define **text schemas** to parse character sequences (log files, CSV, etc.)
- Use the `Interpret()` function to apply schemas to data
- Combine with `CROSS APPLY` for powerful data transformations

## Binary Schema Syntax

Binary schemas define how to interpret byte sequences:

```sql
INTERPRET binary Header {
    Magic:   int le,
    Version: short le,
    Length:  int le,
    Flags:   byte
}
```

### Primitive Types

| Type | Description | Size |
|------|-------------|------|
| `byte` | Unsigned 8-bit | 1 byte |
| `sbyte` | Signed 8-bit | 1 byte |
| `short` | Signed 16-bit | 2 bytes |
| `ushort` | Unsigned 16-bit | 2 bytes |
| `int` | Signed 32-bit | 4 bytes |
| `uint` | Unsigned 32-bit | 4 bytes |
| `long` | Signed 64-bit | 8 bytes |
| `ulong` | Unsigned 64-bit | 8 bytes |
| `float` | 32-bit floating point | 4 bytes |
| `double` | 64-bit floating point | 8 bytes |

### Endianness

Multi-byte types require an endianness specifier:
- `le` - Little-endian (Intel, x86/x64)
- `be` - Big-endian (network byte order, MIPS, PowerPC)

```sql
INTERPRET binary NetworkPacket {
    SourcePort: ushort be,      -- Network byte order
    DestPort:   ushort be,
    Length:     int le          -- Local byte order
}
```

### Byte Arrays

Fixed-size byte arrays:

```sql
INTERPRET binary ImageHeader {
    Magic:    byte[4],          -- Fixed 4-byte signature
    Reserved: byte[16],         -- 16 reserved bytes
    Payload:  byte[256]         -- 256-byte payload
}
```

## Text Schema Syntax

Text schemas define how to parse character sequences:

```sql
INTERPRET text LogEntry {
    Timestamp: between '[' ']',
    _:         literal ' ',
    Level:     until ':',
    _:         literal ': ',
    Message:   rest
}
```

### Field Types

| Type | Description | Example |
|------|-------------|---------|
| `literal` | Match exact string | `literal ': '` |
| `until` | Capture until delimiter | `until ','` |
| `between` | Capture between delimiters | `between '[' ']'` |
| `chars[n]` | Capture exactly n characters | `chars[10]` |
| `token` | Whitespace-delimited token | `token` |
| `rest` | Remaining content | `rest` |
| `whitespace` | Skip whitespace | `whitespace` |
| `pattern` | Regex pattern match | `pattern '\d+'` |

### Modifiers

Text fields support optional modifiers:

```sql
INTERPRET text Record {
    Name:   chars[20] trim,     -- Trim whitespace
    Value:  until ',' lower,    -- Convert to lowercase
    Code:   chars[5] upper      -- Convert to uppercase
}
```

Available modifiers:
- `trim` - Trim both ends
- `ltrim` - Trim left
- `rtrim` - Trim right
- `lower` - Convert to lowercase
- `upper` - Convert to uppercase
- `nested` - Handle nested delimiters (for `between`)
- `optional` - Field is optional

## Using Interpretation Schemas

### The Interpret() Function

Apply a schema to data using `Interpret()`:

```sql
INTERPRET binary BmpHeader {
    Signature: byte[2],
    FileSize:  int le,
    Reserved:  int le,
    DataOffset: int le
}

SELECT 
    h.FileSize,
    h.DataOffset
FROM #os.file('image.bmp') f
CROSS APPLY Interpret(f.GetBytes(), BmpHeader) h
```

### The Parse() Function

For text schemas, use `Parse()`:

```sql
INTERPRET text CsvRow {
    Name:    until ',',
    _:       literal ',',
    Age:     until ',',
    _:       literal ',',
    Email:   rest
}

SELECT 
    r.Name,
    r.Age,
    r.Email
FROM #csv.lines('data.csv') l
CROSS APPLY Parse(l.Line, CsvRow) r
```

### InterpretAt() for Offset Access

Read at a specific offset:

```sql
SELECT 
    h.Magic
FROM #os.file('data.bin') f
CROSS APPLY InterpretAt(f.GetBytes(), 100, Header) h
```

## Schema Composition

Schemas can reference other schemas:

```sql
INTERPRET binary Point {
    X: int le,
    Y: int le
}

INTERPRET binary Rectangle {
    TopLeft:     Point,
    BottomRight: Point,
    Color:       int le
}
```

## Error Handling

Interpretation functions throw exceptions on parse errors:
- Use `TryInterpret()` or `TryParse()` for non-throwing variants (returns null on failure)
- Error codes follow the ISExxxx format (Interpretation Schema Error)

Common error codes:
- `ISE001` - Insufficient data
- `ISE002` - Validation failed
- `ISE003` - Pattern mismatch
- `ISE004` - Literal not found
- `ISE005` - Delimiter not found

## Practical Examples

### Reading a ZIP File Header

```sql
INTERPRET binary ZipLocalHeader {
    Signature:         int le,
    VersionNeeded:     short le,
    Flags:             short le,
    CompressionMethod: short le,
    LastModTime:       short le,
    LastModDate:       short le,
    Crc32:             int le,
    CompressedSize:    int le,
    UncompressedSize:  int le,
    FileNameLength:    short le,
    ExtraFieldLength:  short le
}

SELECT 
    z.CompressedSize,
    z.UncompressedSize,
    z.CompressionMethod
FROM #os.file('archive.zip') f
CROSS APPLY Interpret(f.GetBytes(), ZipLocalHeader) z
WHERE z.Signature = 0x04034B50  -- PK\x03\x04
```

### Parsing Apache Log Format

```sql
INTERPRET text ApacheLogEntry {
    IpAddress:  until ' ',
    _:          literal ' - - [',
    Timestamp:  until ']',
    _:          literal '] "',
    Method:     until ' ',
    _:          literal ' ',
    Path:       until ' ',
    _:          until '" ',
    StatusCode: until ' ',
    _:          literal ' ',
    Size:       rest
}

SELECT 
    a.IpAddress,
    a.Timestamp,
    a.Method,
    a.Path,
    a.StatusCode
FROM #text.lines('/var/log/apache/access.log') l
CROSS APPLY Parse(l.Line, ApacheLogEntry) a
WHERE a.StatusCode = '404'
```

## Discard Fields

Use `_` for fields you want to skip but not capture:

```sql
INTERPRET text KeyValue {
    Key:   until ':',
    _:     literal ': ',    -- Match but don't capture
    Value: rest
}
```

Multiple discard fields are allowed - they're matched in order but not included in the result.
