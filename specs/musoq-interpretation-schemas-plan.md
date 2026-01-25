# Musoq Interpretation Schemas: Implementation Plan

**Specification Version:** 0.3.0-draft  
**Plan Version:** 1.0.0  
**Created:** January 2, 2026  
**Status:** Planning Phase

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Architecture Overview](#2-architecture-overview)
3. [Implementation Stages](#3-implementation-stages)
4. [Session Breakdown](#4-session-breakdown)
5. [Testing Strategy](#5-testing-strategy)
6. [Risk Assessment](#6-risk-assessment)
7. [Success Criteria](#7-success-criteria)

---

## 1. Executive Summary

### 1.1 Goal

Implement the Interpretation Schemas extension to Musoq, enabling declarative parsing of binary and textual data within SQL queries. The implementation will be divided into incremental stages, each delivering testable, usable functionality.

### 1.2 Key Principles

- **Incremental Delivery**: Each session produces working, tested code
- **Early Usability**: Basic binary parsing available after Session 2-3
- **Test-Driven**: Comprehensive tests accompany every feature
- **Specification Compliance**: Always reference `musoq-interpretation-schemas-spec-v3.md`
- **Session Protocol**: Each session begins by reading the specification and creating TODO steps

### 1.3 Estimated Timeline

| Phase | Sessions | Deliverable |
|-------|----------|-------------|
| Foundation | 1-2 | Parser infrastructure, basic binary schema AST |
| Core Binary | 3-5 | Working binary interpretation with primitives |
| Extended Binary | 6-8 | Arrays, nested schemas, conditions |
| Text Schemas | 9-11 | Text parsing, patterns, delimiters |
| Advanced Features | 12-15 | Composition, inheritance, optimization |

---

## 2. Architecture Overview

### 2.1 Current Musoq Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         SQL Query                                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Musoq.Parser                                  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │   Lexer     │→│   Parser    │→│   AST (Syntax Nodes)    │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Musoq.Evaluator                                │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │  Visitors   │→│ Build Items │→│   Code Generation       │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Musoq.Converter                                │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │              Roslyn Compilation → Executable Assembly       ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Proposed Extension Points

```
┌─────────────────────────────────────────────────────────────────┐
│                    Musoq.Parser                                  │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  NEW: Schema Definition Nodes                                ││
│  │  - BinarySchemaNode                                          ││
│  │  - TextSchemaNode                                            ││
│  │  - BinaryFieldNode, TextFieldNode                            ││
│  │  - SchemaTypeNodes (primitives, arrays, etc.)               ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Musoq.Evaluator                                │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  NEW: Schema Processing                                      ││
│  │  - SchemaDefinitionVisitor                                   ││
│  │  - InterpreterClassGenerator                                 ││
│  │  - InterpretMethodBinder                                     ││
│  │  - CROSS/OUTER APPLY scalar extension                        ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Musoq.Schema (NEW)                             │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  Runtime Support                                             ││
│  │  - IBytesInterpreter<T>, ITextInterpreter<T>                ││
│  │  - BytesInterpreterBase<T>, TextInterpreterBase<T>          ││
│  │  - InterpretationMethods (Interpret, Parse, etc.)           ││
│  │  - ParseException hierarchy                                  ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

### 2.3 Files to Modify/Create

#### Musoq.Parser
| File | Action | Purpose |
|------|--------|---------|
| `Tokens/TokenType.cs` | Modify | Add enum values for new keywords |
| `Tokens/TokenRegexDefinition.cs` | Modify | Add regex patterns for new tokens |
| `Lexing/Lexer.cs` | Modify | Add to DefinitionSets.General, GetTokenCandidate |
| `Parser.cs` | Modify | Add ComposeBinarySchema(), ComposeTextSchema(), update ComposeStatement() |
| `Nodes/BinarySchemaNode.cs` | Create | Binary schema AST container |
| `Nodes/TextSchemaNode.cs` | Create | Text schema AST container |
| `Nodes/BinaryFieldNode.cs` | Create | Binary field definitions |
| `Nodes/TextFieldNode.cs` | Create | Text field definitions |
| `Nodes/BinaryTypeNodes.cs` | Create | Primitive, array, string type nodes |
| `Nodes/TextTypeNodes.cs` | Create | Pattern, literal, until type nodes |
| `IExpressionVisitor.cs` | Modify | Add Visit methods for schema nodes |
| `NoOpExpressionVisitor.cs` | Modify | Add no-op implementations |

#### Musoq.Schema (Runtime API - Public Contract)
| File | Action | Purpose |
|------|--------|---------|
| `Interpreters/IInterpreter.cs` | Create | Base interpreter interface |
| `Interpreters/IBytesInterpreter.cs` | Create | Binary parsing contract |
| `Interpreters/ITextInterpreter.cs` | Create | Text parsing contract |
| `Interpreters/BytesInterpreterBase.cs` | Create | Abstract base for binary |
| `Interpreters/TextInterpreterBase.cs` | Create | Abstract base for text |
| `Interpreters/ParseException.cs` | Create | Exception hierarchy (ISExxxx) |

#### Musoq.Evaluator
| File | Action | Purpose |
|------|--------|---------|
| `SchemaRegistry.cs` | Create | Track schemas within query batch |
| `Visitors/SchemaDefinitionVisitor.cs` | Create | Process schema AST, populate registry |
| `Visitors/InterpreterCodeGenerator.cs` | Create | Generate C# interpreter classes |
| `Visitors/BuildMetadataAndInferTypesVisitor.cs` | Modify | Type inference for scalar APPLY |
| `Visitors/ToCSharpRewriteTreeVisitor.cs` | Modify | Scalar wrapping code gen |
| `Visitors/Helpers/AccessMethodNodeProcessor.cs` | Modify | Schema type resolution |

#### Musoq.Converter
| File | Action | Purpose |
|------|--------|---------|
| `Build/TransformTree.cs` | Modify | Wire in schema processing |

#### Musoq.Plugins
| File | Action | Purpose |
|------|--------|---------|
| `Lib/LibraryBaseInterpretation.cs` | Create | Interpret, Parse, TryInterpret, InterpretAt |

### 2.4 Key Architecture Decisions

1. **Interpreter interfaces in `Musoq.Schema`**: These are public API consumed by generated code and external plugins. Placing them in the Schema project (not Evaluator) ensures proper layering.

2. **Schema definitions parsed BEFORE queries**: The parser's `ComposeStatement()` switch must handle `TokenType.Binary` and `TokenType.Text` cases to allow schemas to be defined before SELECT.

3. **Expression reuse**: Schema field size expressions (`byte[Length]`) and conditions (`when HasData <> 0`) reuse the existing expression parser infrastructure.

4. **CROSS APPLY scalar support**: Single objects are wrapped in `new[] { result }` during code generation to maintain uniform enumerable processing.

---

## 3. Implementation Stages

### Stage 1: Foundation (Sessions 1-2)

**Goal**: Establish parser infrastructure for schema definitions.

**Deliverables**:
- Lexer tokens for `binary`, `text`, primitive types, endianness
- Basic parser rules for schema definition structure
- AST node hierarchy for schemas
- Unit tests for lexer and parser

**Minimum Viable Feature**:
```sql
binary Header {
    Magic: int le,
    Version: short le
}
```

### Stage 2: Core Binary Interpretation (Sessions 3-5)

**Goal**: Working binary schema parsing and interpretation.

**Deliverables**:
- `Interpret()` function implementation
- Code generation for simple binary schemas
- Runtime interpreter base classes
- CROSS APPLY scalar support
- Integration tests with actual binary data

**Minimum Viable Feature**:
```sql
binary Header {
    Magic: int le,
    Version: short le
}

SELECT h.Magic, h.Version
FROM #os.file('/data.bin') f
CROSS APPLY Interpret(f.GetBytes(), Header) h
```

### Stage 3: Extended Binary Features (Sessions 6-8)

**Goal**: Complete binary schema feature set.

**Deliverables**:
- Byte arrays with fixed/dynamic sizes
- String types with encodings
- Nested schema references
- Arrays of schemas
- Conditional fields (`when`)
- Computed fields
- Validation (`check`)

**Target Features**:
```sql
binary Packet {
    Magic:      int le check Magic = 0xDEADBEEF,
    Length:     short le,
    HasPayload: byte,
    Payload:    byte[Length] when HasPayload <> 0,
    IsValid:    (Magic = 0xDEADBEEF) AND (Length > 0)
}
```

### Stage 4: Text Schema Foundation (Sessions 9-11)

**Goal**: Basic text parsing capabilities.

**Deliverables**:
- Text schema parser support
- Pattern matching with regex
- Literal matching
- Delimiter-based capture (until, between)
- Fixed-width fields
- `Parse()` function

**Target Features**:
```sql
text LogEntry {
    Timestamp: between '[' ']',
    _:         literal ' ',
    Level:     until ':',
    _:         literal ': ',
    Message:   rest
}

SELECT log.Level, log.Message
FROM #os.file('/app.log') f
CROSS APPLY Lines(f.GetContent()) line
CROSS APPLY Parse(line.Value, LogEntry) log
```

### Stage 5: Advanced Features (Sessions 12-15)

**Goal**: Complete specification implementation.

**Deliverables**:
- Bit fields
- Absolute positioning (`at`)
- Repetition (`repeat until`)
- Schema inheritance (`extends`)
- Generic schemas
- Binary-text composition
- `TryInterpret`, `TryParse`, `InterpretAt`
- `PartialInterpret` for debugging
- Error handling improvements

---

## 4. Session Breakdown

### Session 1: Lexer and Token Infrastructure

**Prerequisites**: Read `musoq-interpretation-schemas-spec-v3.md`

**TODO Steps**:
1. Define new token types in `TokenType.cs` enum for schema keywords
2. Add token regex definitions in `TokenRegexDefinition` class (patterns for `binary`, `text`, `le`, `be`, primitives)
3. Register new definitions in `DefinitionSets.General` array
4. Create token classes for new keywords (following existing pattern like `TableToken.cs`)
5. Implement lexer recognition for: `binary`, `text`, `le`, `be`, primitive types (`byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`)
6. Add string/encoding tokens: `utf8`, `utf16le`, `utf16be`, `ascii`, `latin1`, `ebcdic`
7. Add modifier tokens: `trim`, `rtrim`, `ltrim`, `nullterm`, `when`, `check`, `at`
8. Create comprehensive lexer tests
9. Document token specifications

**Files to Create/Modify**:
- `Musoq.Parser/Tokens/TokenType.cs` - Add enum values
- `Musoq.Parser/Tokens/TokenRegexDefinition.cs` - Add regex patterns
- `Musoq.Parser/Lexing/Lexer.cs` - Add to DefinitionSets.General, add GetTokenCandidate cases
- `Musoq.Parser/Tokens/BinaryToken.cs` - New token class
- `Musoq.Parser/Tokens/TextToken.cs` - New token class  
- `Musoq.Parser/Tokens/EndianToken.cs` - Token for le/be
- `Musoq.Parser.Tests/LexerSchemaTests.cs` - Token recognition tests

**Tests Required**:
- Token recognition for all new keywords
- Endianness suffix parsing
- Schema definition tokenization
- Error cases (invalid tokens)

**Exit Criteria**:
- All new tokens correctly recognized by lexer
- 100% test coverage for new token types
- No regression in existing lexer tests

---

### Session 2: Schema Definition AST Nodes

**Prerequisites**: Session 1 complete, read specification

**TODO Steps**:
1. Design AST node hierarchy for schemas
2. Implement `BinarySchemaNode` with field list
3. Implement `BinaryFieldNode` for named/discard fields
4. Implement primitive type nodes with endianness
5. **Add `TokenType.Binary` and `TokenType.Text` cases to `ComposeStatement()` switch** - schemas must be parseable before SELECT
6. Create `ComposeBinarySchema()` parser method (following `ComposeTable()` pattern)
7. Reuse existing expression parsing for size expressions (e.g., `byte[Length]`) - leverage `ComposeBaseTypes()` and arithmetic expression parsing
8. Add parser tests

**Files to Create/Modify**:
- `Musoq.Parser/Nodes/SchemaDefinitionNode.cs` - Base schema node
- `Musoq.Parser/Nodes/BinarySchemaNode.cs` - Binary schema container
- `Musoq.Parser/Nodes/BinaryFieldNode.cs` - Binary field definition
- `Musoq.Parser/Nodes/BinaryTypeNodes.cs` - Type hierarchy (PrimitiveTypeNode, ByteArrayTypeNode, etc.)
- `Musoq.Parser/Parser.cs` - Add `ComposeBinarySchema()`, update `ComposeStatement()` switch
- `Musoq.Parser/IExpressionVisitor.cs` - Add Visit methods for new nodes
- `Musoq.Parser/NoOpExpressionVisitor.cs` - Add no-op implementations
- `Musoq.Parser.Tests/ParserSchemaTests.cs` - Parser tests

**AST Node Hierarchy**:
```
SchemaDefinitionNode (abstract)
├── BinarySchemaNode
│   └── BinaryFieldNode[]
│       ├── FieldName (string)
│       ├── FieldType (BinaryTypeNode)
│       └── Modifiers (optional)
└── TextSchemaNode (Session 9+)

BinaryTypeNode (abstract)
├── PrimitiveTypeNode
│   ├── TypeName (byte, short, int, long, etc.)
│   └── Endianness (le, be, null for byte)
├── ByteArrayTypeNode
│   ├── SizeExpression
│   └── (inherits from BinaryTypeNode)
├── StringTypeNode
│   ├── SizeExpression
│   ├── Encoding
│   └── Modifiers
└── SchemaReferenceNode
    └── ReferencedSchemaName
```

**Tests Required**:
- Parse simple binary schema with primitives
- Parse schema with multiple fields
- Parse discard fields (`_`)
- Verify endianness required for multi-byte types
- Error case: missing endianness
- Error case: endianness on byte type

**Exit Criteria**:
- Can parse: `binary Header { Magic: int le, Version: short le }`
- AST correctly represents schema structure
- Parser tests pass
- No regression in existing parser tests

---

### Session 3: Evaluator Schema Processing Foundation

**Prerequisites**: Sessions 1-2 complete, read specification

**TODO Steps**:
1. Create `SchemaRegistry` to track defined schemas within query batch scope
2. Implement `SchemaDefinitionVisitor` to process schema AST and populate registry
3. **Create runtime interfaces in `Musoq.Schema` project** (not Evaluator) - these are public API:
   - `IInterpreter<TOut>` - base interface
   - `IBytesInterpreter<TOut>` - binary parsing contract
   - `ITextInterpreter<TOut>` - text parsing contract (placeholder for Session 9)
4. Create interpreter base classes in `Musoq.Schema`:
   - `BytesInterpreterBase<TOut>` - abstract base with Interpret method
   - `TextInterpreterBase<TOut>` - abstract base (placeholder for Session 9)
5. Add expression evaluation support for schema field size/condition expressions
6. Generate simple interpreter class stubs (skeleton with properties, no parsing logic yet)
7. Wire schema processing into `TransformTree` build chain
8. Add evaluator tests for schema registration

**Files to Create/Modify**:
- `Musoq.Schema/Interpreters/IInterpreter.cs` - Base interface
- `Musoq.Schema/Interpreters/IBytesInterpreter.cs` - Binary interpreter interface
- `Musoq.Schema/Interpreters/ITextInterpreter.cs` - Text interpreter interface (placeholder)
- `Musoq.Schema/Interpreters/BytesInterpreterBase.cs` - Abstract base class
- `Musoq.Schema/Interpreters/TextInterpreterBase.cs` - Abstract base class (placeholder)
- `Musoq.Schema/Interpreters/ParseException.cs` - Exception hierarchy
- `Musoq.Evaluator/Visitors/SchemaDefinitionVisitor.cs` - Process schema AST
- `Musoq.Evaluator/SchemaRegistry.cs` - Track schemas in batch
- `Musoq.Converter/Build/TransformTree.cs` - Wire in schema processing
- `Musoq.Evaluator.Tests/SchemaProcessingTests.cs` - Tests

**Tests Required**:
- Schema definition registered correctly
- Multiple schemas in same batch
- Schema name uniqueness enforcement
- Forward reference detection and error
- Schema accessible by name after definition

**Exit Criteria**:
- Evaluator recognizes schema definitions
- Schemas registered and retrievable
- Generated classes compile (even if minimal)
- Evaluator tests pass

---

### Session 4: Binary Interpreter Code Generation

**Prerequisites**: Session 3 complete, read specification

**TODO Steps**:
1. Implement C# code generation for primitive types
2. Generate `Interpret` method body with `BinaryPrimitives`
3. Handle endianness in code generation
4. Compile generated interpreter using Roslyn
5. Add code generation tests
6. Integration test: parse actual bytes

**Files to Create/Modify**:
- `Musoq.Evaluator/Visitors/InterpreterCodeGenerator.cs`
- `Musoq.Evaluator/Build/InterpreterCompilationUnit.cs`
- `Musoq.Evaluator.Tests/InterpreterCodeGenTests.cs`
- `Musoq.Evaluator.Tests/BinaryInterpretationTests.cs`

**Generated Code Example**:
```csharp
public sealed class Header : BytesInterpreterBase<Header>
{
    public int Magic { get; init; }
    public short Version { get; init; }
    
    public override Header Interpret(ReadOnlySpan<byte> data, ref int offset)
    {
        var magic = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset));
        offset += 4;
        var version = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(offset));
        offset += 2;
        return new Header { Magic = magic, Version = version };
    }
}
```

**Tests Required**:
- Code generation for each primitive type
- Little-endian vs big-endian reading
- Generated code compiles
- Generated code parses test data correctly
- Byte-level accuracy verification

**Exit Criteria**:
- Can generate and compile interpreter for primitives
- Interpreter correctly reads binary data
- All primitive types supported

---

### Session 5: Interpret Function and CROSS APPLY Integration

**Prerequisites**: Session 4 complete, read specification

**TODO Steps**:
1. Implement `Interpret<TInterpreter, TOut>(byte[], TInterpreter)` library method in `Musoq.Plugins`
2. **Modify CROSS APPLY to handle scalar complex types** - wrap single objects in `new[] { result }`:
   - Update `BuildMetadataAndInferTypesVisitor.Visit(ApplyFromNode)` for type inference
   - Update code generation in `ToCSharpRewriteTreeVisitor` or relevant emitter
3. Wire up schema type resolution in method binding:
   - `AccessMethodNodeProcessor` needs to recognize schema type references
   - Schema registry lookup during method resolution
4. Handle schema type as second argument (not a runtime value, but a type reference)
5. Create end-to-end integration tests with in-memory byte arrays
6. Test with real file data using `#os.file()` schema

**Files to Create/Modify**:
- `Musoq.Plugins/Lib/LibraryBaseInterpretation.cs` - Interpret, InterpretAt methods
- `Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.cs` - Type inference for scalar APPLY
- `Musoq.Evaluator/Visitors/ToCSharpRewriteTreeVisitor.cs` - Scalar wrapping code gen
- `Musoq.Evaluator/Visitors/Helpers/AccessMethodNodeProcessor.cs` - Schema type resolution
- `Musoq.Evaluator.Tests/InterpretFunctionTests.cs` - Unit tests
- `Musoq.Evaluator.Tests/CrossApplyScalarTests.cs` - Integration tests
- `Musoq.Evaluator.Tests/BinaryInterpretation/BasicBinaryParsingTests.cs` - E2E tests

**Tests Required**:
- `Interpret()` returns parsed object
- CROSS APPLY with scalar Interpret result
- Field access via dot notation (`h.Magic`)
- Multiple schemas in same query
- Error handling for insufficient data

**Exit Criteria**:
- Complete working query:
  ```sql
  binary Header { Magic: int le, Version: short le }
  SELECT h.Magic, h.Version
  FROM #os.file('/data.bin') f
  CROSS APPLY Interpret(f.GetBytes(), Header) h
  ```
- Tests pass with real binary data
- Error messages are clear and actionable

---

### Session 6: Byte Arrays and String Types

**Prerequisites**: Session 5 complete, read specification

**TODO Steps**:
1. Implement `byte[N]` fixed-size array parsing
2. Implement `byte[FieldRef]` dynamic-size array parsing
3. Implement string types with encodings (utf8, ascii, etc.)
4. Implement string modifiers (trim, nullterm)
5. Add size expression evaluation
6. Comprehensive tests for all variants

**Files to Create/Modify**:
- `Musoq.Parser/AST/ByteArrayTypeNode.cs`
- `Musoq.Parser/AST/StringTypeNode.cs`
- `Musoq.Evaluator/Visitors/InterpreterCodeGenerator.cs` (extend)
- `Musoq.Evaluator.Tests/ByteArrayTests.cs`
- `Musoq.Evaluator.Tests/StringTypeTests.cs`

**Tests Required**:
- Fixed-size byte arrays
- Dynamic-size byte arrays (size from field)
- All encoding types
- String modifiers (trim, rtrim, ltrim, nullterm)
- Size expression with arithmetic
- Error: negative size

**Exit Criteria**:
```sql
binary Record {
    NameLen: short le,
    Name: string[NameLen] utf8 trim,
    Data: byte[16]
}
```
- All string encodings work correctly
- Modifiers apply in correct order

---

### Session 7: Nested Schemas and Arrays

**Prerequisites**: Session 6 complete, read specification

**TODO Steps**:
1. Implement schema reference in fields
2. Implement nested schema parsing (inline)
3. Implement arrays of schemas (`Schema[Count]`)
4. Handle CROSS APPLY with array fields
5. Test complex nested structures

**Files to Create/Modify**:
- `Musoq.Parser/AST/SchemaReferenceNode.cs`
- `Musoq.Parser/AST/ArrayTypeNode.cs`
- `Musoq.Evaluator/Visitors/InterpreterCodeGenerator.cs` (extend)
- `Musoq.Evaluator.Tests/NestedSchemaTests.cs`
- `Musoq.Evaluator.Tests/SchemaArrayTests.cs`

**Tests Required**:
- Nested schema field access (`outer.inner.field`)
- Array of primitives
- Array of schemas
- CROSS APPLY over schema arrays
- Nested arrays
- Reference ordering (must define before use)

**Exit Criteria**:
```sql
binary Point { X: float le, Y: float le }
binary Mesh {
    VertexCount: int le,
    Vertices: Point[VertexCount]
}

SELECT v.X, v.Y
FROM #os.file('/mesh.bin') f
CROSS APPLY Interpret(f.GetBytes(), Mesh) m
CROSS APPLY m.Vertices v
```

---

### Session 8: Conditional Fields, Computed Fields, and Validation

**Prerequisites**: Session 7 complete, read specification

**TODO Steps**:
1. Implement `when` conditional parsing
2. Implement computed fields (no bytes consumed)
3. Implement `check` validation constraints
4. Handle null propagation for conditionals
5. Expression evaluation in schema context

**Files to Create/Modify**:
- `Musoq.Parser/AST/ConditionalFieldModifier.cs`
- `Musoq.Parser/AST/ComputedFieldNode.cs`
- `Musoq.Parser/AST/ValidationModifier.cs`
- `Musoq.Evaluator/Visitors/InterpreterCodeGenerator.cs` (extend)
- `Musoq.Evaluator/Runtime/ParseException.cs`
- `Musoq.Evaluator.Tests/ConditionalFieldTests.cs`
- `Musoq.Evaluator.Tests/ComputedFieldTests.cs`
- `Musoq.Evaluator.Tests/ValidationTests.cs`

**Tests Required**:
- Conditional field parsed when true
- Conditional field null when false
- Cursor doesn't advance for false condition
- Computed fields evaluate correctly
- Check constraint passes
- Check constraint fails with clear error
- Bitwise operations in expressions
- Null propagation through dependent fields

**Exit Criteria**:
```sql
binary Message {
    Type: byte,
    Flags: short le,
    HasPayload: byte,
    Length: int le when HasPayload <> 0,
    Payload: byte[Length] when HasPayload <> 0,
    IsCompressed: (Flags & 0x01) <> 0,
    Magic: int le check Magic = 0xDEADBEEF
}
```

---

### Session 9: Text Schema Foundation

**Prerequisites**: Session 8 complete, read specification

**TODO Steps**:
1. Extend lexer with text schema tokens
2. Implement `TextSchemaNode` AST
3. Implement `TextFieldNode` hierarchy
4. Implement `literal`, `until`, `between` types
5. Implement `rest` type
6. Create `ITextInterpreter<T>` infrastructure

**Files to Create/Modify**:
- `Musoq.Parser/AST/TextSchemaNode.cs`
- `Musoq.Parser/AST/TextFieldNodes.cs`
- `Musoq.Parser/AST/TextTypeNodes.cs`
- `Musoq.Evaluator/Runtime/Interpreters/ITextInterpreter.cs`
- `Musoq.Evaluator/Runtime/Interpreters/TextInterpreterBase.cs`
- `Musoq.Evaluator/Visitors/TextInterpreterCodeGenerator.cs`
- `Musoq.Evaluator.Tests/TextSchemaParserTests.cs`
- `Musoq.Evaluator.Tests/TextInterpretationTests.cs`

**Tests Required**:
- Parse text schema definition
- `literal` exact match
- `until` delimiter capture
- `between` delimiter capture
- `rest` captures remainder
- Discard fields in text schemas

**Exit Criteria**:
```sql
text KeyValue {
    Key: until '=',
    _: literal '=',
    Value: rest trim
}

SELECT kv.Key, kv.Value
FROM #os.file('/config.txt') f
CROSS APPLY Lines(f.GetContent()) line
CROSS APPLY Parse(line.Value, KeyValue) kv
```

---

### Session 10: Text Schema Pattern Matching and Modifiers

**Prerequisites**: Session 9 complete, read specification

**TODO Steps**:
1. Implement `pattern` type with regex
2. Implement capture groups
3. Implement `chars[N]` fixed-width
4. Implement `token` and `whitespace`
5. Implement text modifiers (trim, lower, upper)
6. Implement `optional` fields

**Files to Create/Modify**:
- `Musoq.Parser/AST/PatternTypeNode.cs`
- `Musoq.Parser/AST/CharsTypeNode.cs`
- `Musoq.Parser/AST/TokenTypeNode.cs`
- `Musoq.Evaluator/Visitors/TextInterpreterCodeGenerator.cs` (extend)
- `Musoq.Evaluator.Tests/PatternMatchingTests.cs`
- `Musoq.Evaluator.Tests/TextModifierTests.cs`

**Tests Required**:
- Regex pattern matching
- Named capture groups
- Fixed-width field parsing
- Token capture
- Whitespace handling
- All modifiers
- Optional field present
- Optional field absent (null)

**Exit Criteria**:
```sql
text ApacheLog {
    RemoteHost: until ' ',
    _: literal ' ',
    Identity: until ' ',
    _: literal ' ',
    User: until ' ',
    _: literal ' ',
    Timestamp: between '[' ']',
    _: literal ' "',
    Method: until ' ',
    _: literal ' ',
    Path: until ' ',
    _: literal ' ',
    Protocol: until '"',
    _: literal '" ',
    Status: pattern '\d{3}',
    _: literal ' ',
    Size: pattern '\d+|-'
}
```

---

### Session 11: Text Schema Advanced Features

**Prerequisites**: Session 10 complete, read specification

**TODO Steps**:
1. Implement `repeat` type with until clause
2. Implement `switch` for alternatives
3. Implement `escaped` and `nested` modifiers
4. Implement `greedy`/`lazy` matching
5. Complete text schema feature set

**Files to Create/Modify**:
- `Musoq.Parser/AST/RepeatTypeNode.cs`
- `Musoq.Parser/AST/SwitchTypeNode.cs`
- `Musoq.Evaluator/Visitors/TextInterpreterCodeGenerator.cs` (extend)
- `Musoq.Evaluator.Tests/RepeatTests.cs`
- `Musoq.Evaluator.Tests/SwitchTests.cs`

**Tests Required**:
- Repeat until delimiter
- Repeat until end
- Switch with pattern matching
- Switch default case
- Escaped content in between
- Nested delimiters

**Exit Criteria**:
```sql
text ConfigLine {
    Content: switch {
        pattern '\s*\[' => SectionHeader,
        pattern '\s*#' => Comment,
        _ => KeyValue
    }
}
```

---

### Session 12: Bit Fields and Alignment

**Prerequisites**: Session 8 complete (binary), read specification

**TODO Steps**:
1. Implement `bits[N]` type
2. Implement bit-level cursor tracking
3. Implement `align[N]` directive
4. Handle cross-byte bit fields
5. Comprehensive bit manipulation tests

**Files to Create/Modify**:
- `Musoq.Parser/AST/BitFieldTypeNode.cs`
- `Musoq.Parser/AST/AlignDirectiveNode.cs`
- `Musoq.Evaluator/Visitors/InterpreterCodeGenerator.cs` (extend)
- `Musoq.Evaluator.Tests/BitFieldTests.cs`

**Tests Required**:
- Single bit extraction
- Multi-bit field (1-64 bits)
- Cross-byte boundary fields
- Alignment after bits
- LSB ordering verification

**Exit Criteria**:
```sql
binary TcpFlags {
    Reserved: bits[4],
    DataOff: bits[4],
    Fin: bits[1],
    Syn: bits[1],
    Rst: bits[1],
    Psh: bits[1],
    Ack: bits[1],
    Urg: bits[1],
    Ece: bits[1],
    Cwr: bits[1]
}
```

---

### Session 13: Absolute Positioning and InterpretAt

**Prerequisites**: Session 8 complete, read specification

**TODO Steps**:
1. Implement `at` position modifier
2. Implement `InterpretAt(data, offset, Schema)`
3. Handle forward/backward positioning
4. Position expression evaluation
5. Tests for complex offset scenarios

**Files to Create/Modify**:
- `Musoq.Parser/AST/PositionModifier.cs`
- `Musoq.Plugins/Lib/LibraryBaseInterpretation.cs` (extend)
- `Musoq.Evaluator/Visitors/InterpreterCodeGenerator.cs` (extend)
- `Musoq.Evaluator.Tests/PositioningTests.cs`

**Tests Required**:
- Absolute offset positioning
- Dynamic offset from field
- `InterpretAt` function
- Re-reading earlier data
- Skipping forward

**Exit Criteria**:
```sql
binary PeHeader {
    DosMagic: string[2] ascii at 0,
    PeOffset: int le at 0x3C,
    PeSignature: string[4] ascii at PeOffset
}
```

---

### Session 14: Schema Inheritance and Generics

**Prerequisites**: Sessions 1-13 complete, read specification

**TODO Steps**:
1. Implement `extends` keyword
2. Implement parent field inheritance
3. Implement generic schema parameters
4. Implement generic instantiation
5. Tests for inheritance hierarchies

**Files to Create/Modify**:
- `Musoq.Parser/AST/SchemaInheritance.cs`
- `Musoq.Parser/AST/GenericSchemaNode.cs`
- `Musoq.Evaluator/Visitors/SchemaDefinitionVisitor.cs` (extend)
- `Musoq.Evaluator.Tests/SchemaInheritanceTests.cs`
- `Musoq.Evaluator.Tests/GenericSchemaTests.cs`

**Tests Required**:
- Single inheritance
- Child field access
- Parent field access
- Generic type instantiation
- Nested generics

**Exit Criteria**:
```sql
binary BaseMessage { MsgType: byte, Length: short le }
binary TextMessage extends BaseMessage { Content: string[Length] utf8 }

binary LengthPrefixed<T> {
    Length: int le,
    Data: T[Length]
}
```

---

### Session 15: Error Handling, Safe Interpretation, and Polish

**Prerequisites**: Sessions 1-14 complete, read specification

**TODO Steps**:
1. Implement comprehensive `ParseException` hierarchy
2. Implement `TryInterpret`, `TryParse` functions
3. Implement `PartialInterpret` for debugging
4. OUTER APPLY null handling
5. Error code standardization (ISExxxx)
6. Performance optimization pass
7. Documentation and examples

**Files to Create/Modify**:
- `Musoq.Evaluator/Runtime/ParseException.cs`
- `Musoq.Plugins/Lib/LibraryBaseInterpretation.cs` (extend)
- `Musoq.Evaluator.Tests/SafeInterpretationTests.cs`
- `Musoq.Evaluator.Tests/ErrorHandlingTests.cs`
- `docs/interpretation-schemas.md`

**Tests Required**:
- All error codes produced correctly
- `TryInterpret` returns null on failure
- `TryParse` returns null on failure
- OUTER APPLY with null result
- `PartialInterpret` partial results
- Error messages include position and context

**Exit Criteria**:
- All specification features implemented
- Comprehensive error handling
- Performance acceptable for production use
- Documentation complete

---

## 5. Testing Strategy

### 5.1 Test Categories

| Category | Purpose | Location |
|----------|---------|----------|
| Lexer Tests | Token recognition | `Musoq.Parser.Tests/LexerSchemaTests.cs` |
| Parser Tests | AST generation | `Musoq.Parser.Tests/ParserSchemaTests.cs` |
| Code Gen Tests | C# generation | `Musoq.Evaluator.Tests/InterpreterCodeGenTests.cs` |
| Integration Tests | End-to-end queries | `Musoq.Evaluator.Tests/InterpretationIntegrationTests.cs` |
| Binary Tests | Binary parsing | `Musoq.Evaluator.Tests/BinaryInterpretation/*.cs` |
| Text Tests | Text parsing | `Musoq.Evaluator.Tests/TextInterpretation/*.cs` |
| Error Tests | Error handling | `Musoq.Evaluator.Tests/InterpretationErrorTests.cs` |

### 5.2 Test Data

Create test data files:
- `Musoq.Evaluator.Tests/TestData/Binary/` - Binary test files
- `Musoq.Evaluator.Tests/TestData/Text/` - Text test files

Inline test data using byte arrays for precision:
```csharp
var testData = new byte[] { 0xEF, 0xBE, 0xAD, 0xDE, 0x01, 0x00 };
```

### 5.3 Test Naming Convention

```
[Feature]_[Scenario]_[ExpectedResult]

Examples:
BinarySchema_PrimitiveInt32LittleEndian_ParsesCorrectly
TextSchema_UntilDelimiter_CapturesContent
ConditionalField_WhenFalse_ReturnsNull
CheckConstraint_ValidationFails_ThrowsISE002
```

### 5.4 Regression Testing

After each session:
1. Run full test suite: `dotnet test --configuration Release`
2. Verify no existing tests broken
3. Validate all new tests pass
4. Check for performance regression with benchmarks

---

## 6. Risk Assessment

### 6.1 Technical Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Parser complexity | High | Incremental implementation, thorough testing |
| Code generation bugs | High | Generated code review, extensive tests |
| CROSS APPLY changes | Medium | Careful modification, regression tests |
| Performance | Medium | Span-based parsing, benchmark monitoring |
| Roslyn integration | Medium | Reuse existing compilation patterns |

### 6.2 Schedule Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Underestimated complexity | High | Conservative estimates, buffer time |
| Specification ambiguity | Medium | Document decisions, update spec |
| Integration issues | Medium | Continuous integration testing |

---

## 7. Success Criteria

### 7.1 Per-Session Criteria

- [ ] All planned features implemented
- [ ] All tests pass
- [ ] No regression in existing tests
- [ ] Code reviewed and documented
- [ ] Session summary written to `.copilot_session_summary.md`

### 7.2 Milestone Criteria

**After Session 5 (Core Binary)**:
- [ ] Can parse simple binary files
- [ ] Query works end-to-end
- [ ] Basic production use possible

**After Session 11 (Core Text)**:
- [ ] Can parse common text formats
- [ ] Apache log example works
- [ ] Key-value config parsing works

**After Session 15 (Complete)**:
- [ ] Full specification implemented
- [ ] All examples from spec work
- [ ] Documentation complete
- [ ] Performance acceptable

### 7.3 Quality Gates

Each session must pass:
1. `dotnet build --configuration Release` succeeds
2. `dotnet test --configuration Release` all tests pass
3. New feature has >90% test coverage
4. No compiler warnings in new code
5. Code follows existing Musoq patterns

---

## Appendix A: Session Template

Each session should follow this template:

```markdown
# Session N: [Title]

## Prerequisites
- Read `musoq-interpretation-schemas-spec-v3.md`
- Previous session complete

## TODO Steps
1. [ ] Step 1
2. [ ] Step 2
...

## Files to Create/Modify
- `path/to/file.cs` - Purpose

## Tests Required
- Test case 1
- Test case 2

## Exit Criteria
- Working feature description
- Example query

## Notes
- Decisions made
- Issues encountered
- Carry-forward items
```

---

## Appendix B: Reference Implementation Patterns

### Binary Primitive Reading
```csharp
// Little-endian int
var value = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset));
offset += 4;

// Big-endian short
var value = BinaryPrimitives.ReadInt16BigEndian(data.Slice(offset));
offset += 2;
```

### String Reading
```csharp
// UTF-8 string
var bytes = data.Slice(offset, length);
var value = Encoding.UTF8.GetString(bytes);
offset += length;

// With null-termination
var nullIndex = bytes.IndexOf((byte)0);
if (nullIndex >= 0)
    value = Encoding.UTF8.GetString(bytes.Slice(0, nullIndex));
```

### Conditional Field
```csharp
int? length = null;
byte[]? payload = null;
if (hasPayload != 0)
{
    length = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset));
    offset += 4;
    payload = data.Slice(offset, length.Value).ToArray();
    offset += length.Value;
}
```

---

*End of Implementation Plan*
