# Musoq.Parser

SQL lexing, parsing, and AST generation using a recursive descent parser. This is the first stage of the Musoq compilation pipeline — it transforms raw SQL text into a typed Abstract Syntax Tree.

## Internal Structure

```
Musoq.Parser/
├── Lexing/                         # Tokenizer components
│   ├── Lexer.cs                    # Main tokenizer — direct character scanning with regex fallbacks
│   ├── CharacterDispatcher.cs      # Routes characters to token handlers
│   ├── FastCharacterClassifier.cs  # Optimized character classification
│   ├── TokenFactory.cs             # Creates token instances from matched text
│   ├── KeywordLookup.cs            # Reserved keyword recognition
│   ├── ILexer.cs                   # Lexer interface
│   └── LexerException.cs           # Lexer error type
├── Tokens/                         # ~125 token types
│   ├── Token.cs                    # Base token class
│   ├── TokenType.cs                # Token type enum
│   ├── GenericFunctionToken.cs     # Type-parameterized: Func<Type>(...)
│   ├── FunctionToken.cs            # Standard function call
│   ├── WordToken.cs                # Identifiers
│   ├── StringLiteralToken.cs       # String literals
│   ├── IntegerToken.cs             # Integer literals
│   ├── HexIntegerToken.cs          # 0xFF literals
│   ├── BinaryIntegerToken.cs       # 0b101 literals
│   ├── OctalIntegerToken.cs        # 0o77 literals
│   └── ... (115+ more keyword/operator tokens)
├── Nodes/                          # ~180 AST node types (including From/ and InterpretationSchema/)
│   ├── Node.cs                     # Abstract base node
│   ├── RootNode.cs                 # AST root
│   ├── QueryNode.cs                # SELECT query
│   ├── SelectNode.cs               # SELECT clause
│   ├── WhereNode.cs                # WHERE clause
│   ├── GroupByNode.cs              # GROUP BY clause
│   ├── OrderByNode.cs              # ORDER BY clause
│   ├── AccessMethodNode.cs         # Method call (has TypeParameter for generics)
│   ├── AccessColumnNode.cs         # Column reference
│   ├── CteExpressionNode.cs        # Common Table Expression
│   ├── WindowFunctionNode.cs       # Window function
│   ├── CaseNode.cs                 # CASE WHEN expression
│   ├── From/                       # 21 FROM-clause node variants
│   │   ├── AliasedFromNode.cs      # Aliased source (has TypeParameter)
│   │   ├── JoinFromNode.cs         # JOIN
│   │   ├── ApplyFromNode.cs        # CROSS/OUTER APPLY
│   │   ├── SchemaFromNode.cs       # #schema.table() source
│   │   ├── SchemaMethodFromNode.cs # Schema method source
│   │   ├── InterpretFromNode.cs    # Interpretation function source
│   │   └── ... (13 more from-node variants)
│   └── InterpretationSchema/       # 30 binary/text schema definition nodes
│       ├── BinarySchemaNode.cs     # binary { ... } definition
│       ├── TextSchemaNode.cs       # text { ... } definition
│       ├── FieldDefinitionNode.cs  # Schema field
│       ├── PrimitiveTypeNode.cs    # Primitive type reference
│       ├── StringTypeNode.cs       # String type with encoding
│       └── ... (25 more schema definition nodes)
├── Helpers/                        # Parser utilities
├── Diagnostics/                    # Error reporting infrastructure
├── Recovery/                       # Error recovery strategies
├── Exceptions/                     # Parser-specific exceptions
├── Parser.cs                       # Main recursive descent parser
├── SchemaParser.cs                 # Interpretation schema parser (binary/text)
├── IExpressionVisitor.cs           # Visitor interface — ALL visitors implement this
├── IQueryPartAwareExpressionVisitor.cs  # Query-part-aware visitor extension
├── NoOpExpressionVisitor.cs        # Default no-op visitor implementation
├── ParseResult.cs                  # Parser output wrapper
├── ParseException.cs               # Parse error type
├── TextSpan.cs                     # Source location tracking
└── QueryPart.cs                    # Query part classification
```

## Key Classes

| Class | Purpose |
|-------|---------|
| `Parser.cs` | Recursive descent parser — converts token stream to AST. Entry point: `Parse()` method |
| `Lexer.cs` | Direct character-scanning tokenizer (17-42x faster than the previous regex lexer). Generic-function tokens are recognized via `TryScanGenericFunction` before a plain identifier/function is emitted |
| `IExpressionVisitor.cs` | Core visitor interface — every AST visitor in the entire Musoq system implements this |
| `SchemaParser.cs` | Parses `binary { ... }` and `text { ... }` interpretation schema definitions |
| `GenericFunctionToken` | Token for `FuncName<TypeParam>(...)` syntax. Extends `FunctionToken` with a `TypeParameter` property |
| `AccessMethodNode` | AST node for method calls. `TypeParameter` property carries the generic type from `GenericFunctionToken` |
| `AliasedFromNode` | AST node for aliased FROM sources. `TypeParameter` carries generic type for interpretation functions |

## Lexer Patterns

The Lexer scans tokens character-by-character via `CharacterDispatcher` and `FastCharacterClassifier`, falling back to compiled regexes (`TryMatchRegex`) only for a few multi-word keywords and bracketed/comment spans. Key behaviors:

- **Generic functions**: `TryScanGenericFunction` (in `Lexer.Identifiers.Access.cs`) scans `Interpret<Header>(...)` syntax directly and produces a `GenericFunctionToken` before the identifier would otherwise become a plain `FunctionToken`.
- **Standard functions**: a `WordToken` immediately followed by `(` becomes a `FunctionToken`.
- Numeric literals: hex (`0x`), binary (`0b`), octal (`0o`), decimal, integer patterns.
- Keywords: `KeywordLookup` maps strings to keyword token types; multi-word keywords (`NOT IN`, `UNION ALL`, `GROUP BY`, `ORDER BY`, etc.) use regex fallbacks dispatched on the first character.

## AST Node Hierarchy

All nodes extend `Node` (abstract base). Key specializations:

- **Expression nodes**: `AddNode`, `EqualityNode`, `AndNode`, `OrNode`, etc. — binary/unary operations
- **Literal nodes**: `IntegerNode`, `DecimalNode`, `StringNode`, `BooleanNode`, `NullNode`, etc.
- **Query structure nodes**: `QueryNode`, `SelectNode`, `WhereNode`, `GroupByNode`, `OrderByNode`, `HavingNode`
- **From nodes** (`Nodes/From/`): 19 variants modeling different source types (schema, join, apply, in-memory, interpret)
- **Interpretation schema nodes** (`Nodes/InterpretationSchema/`): `BinarySchemaNode`, `TextSchemaNode`, field definitions, type annotations
- **Set operation nodes**: `UnionNode`, `ExceptNode`, `IntersectNode`
- **Window nodes**: `WindowFunctionNode`, `WindowSpecificationNode`, `WindowFrameNode`

## Dependencies

```
Musoq.Parser (leaf project — no dependencies)
    ↑ depended on by: Musoq.Evaluator, Musoq.Converter
```

## Development Workflow

### Testing
```bash
# Run parser tests (1,403 tests, ~0.5 seconds)
dotnet test src/dotnet/Musoq.Parser.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"

# Rerun a specific failing parser test with useful failure detail
dotnet test src/dotnet/Musoq.Parser.Tests --configuration Release --no-build --filter "FullyQualifiedName~TestMethodName" --nologo --verbosity quiet --logger "console;verbosity=normal"
```

### Common Modifications

**Adding a new token type:**
1. Add token class in `Tokens/` extending `Token`
2. Add regex pattern or keyword mapping in `Lexer.cs`
3. Update `TokenFactory.cs` if needed
4. Update `CharacterDispatcher.cs` for new character routes

**Adding a new AST node type:**
1. Add node class in `Nodes/` (or appropriate subfolder) extending `Node`
2. Add `Accept(IExpressionVisitor)` method
3. Add corresponding `Visit` method to `IExpressionVisitor`
4. Update `NoOpExpressionVisitor` with default implementation
5. Update ALL visitor implementations across the entire codebase (especially in Musoq.Evaluator)

**Adding a new keyword:**
1. Add token type in `Tokens/`
2. Register in `KeywordLookup.cs`
3. Add parsing rule in `Parser.cs`

### Impact of Changes
Parser changes are high-impact — they affect every downstream module. After parser modifications:
- Run parser tests: `dotnet test src/dotnet/Musoq.Parser.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"`
- Run evaluator tests (integration): `dotnet test src/dotnet/Musoq.Evaluator.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"`
- If `IExpressionVisitor` was modified, every visitor in `Musoq.Evaluator/Visitors/` must be updated
