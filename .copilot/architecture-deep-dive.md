# Core Architecture Deep Dive

## Overview

Musoq's architecture is designed around a pipeline pattern where each component has a specific responsibility in the query processing workflow. This document provides detailed insights into each component's internal architecture.

## Component Architecture

### 1. Parser Module Architecture

```
Lexer → Token Stream → Parser → AST → Validation
```

#### Key Classes and Responsibilities

**`Parser.cs`**:
- Main entry point for parsing operations
- Implements recursive descent parsing with operator precedence
- Manages token consumption and AST node creation
- Handles precedence dictionary for arithmetic operations

```csharp
private readonly Dictionary<TokenType, (short Precendence, Associativity Associativity)> _precedenceDictionary = new()
{
    {TokenType.Plus, (1, Associativity.Left)},
    {TokenType.Hyphen, (1, Associativity.Left)},
    {TokenType.Star, (2, Associativity.Left)},
    {TokenType.FSlash, (2, Associativity.Left)},
    {TokenType.Mod, (2, Associativity.Left)},
    {TokenType.Dot, (3, Associativity.Left)}
};
```

**`Lexing/Lexer.cs`**:
- Tokenizes input SQL text
- Recognizes keywords, operators, literals, and identifiers
- Handles string escaping and numeric literals
- Manages token position tracking for error reporting

**AST Node Hierarchy**:
```
Node (abstract base)
├── QueryNode (abstract)
│   ├── SelectNode
│   ├── WhereNode
│   ├── OrderByNode
│   └── GroupByNode
├── ExpressionNode (abstract)
│   ├── ArithmeticNode
│   ├── ColumnNode
│   ├── LiteralNode
│   └── MethodNode
└── FromNode (abstract)
    ├── SchemaFromNode
    ├── JoinFromNode
    └── CteFromNode
```

#### Parser Features

1. **SQL Syntax Support**:
   - Standard SELECT, FROM, WHERE, ORDER BY, GROUP BY
   - JOIN operations (INNER, LEFT, RIGHT, FULL OUTER)
   - Subqueries and CTEs (Common Table Expressions)
   - Set operations (UNION, EXCEPT, INTERSECT)
   - Window functions and aggregations

2. **Extended Syntax**:
   - Schema-prefixed data sources (`#schema.table()`)
   - CROSS APPLY and OUTER APPLY operations
   - Custom function calls
   - Dynamic parameter passing

3. **Error Handling**:
   - Syntax error reporting with position information
   - Graceful recovery from parse errors
   - Detailed error messages for debugging

### 2. Schema Module Architecture

```
ISchema Interface → SchemaBase → Concrete Schema Implementation
                                         ↓
RowSource → Data Iteration → Entity Objects
```

#### Core Abstractions

**`ISchema`**:
- Primary contract for data source plugins
- Defines methods for table/source resolution
- Handles method binding for data source operations

**`SchemaBase`**:
- Abstract base class for schema implementations
- Provides common functionality for source/table registration
- Manages constructor information and method aggregation
- Implements default behaviors for schema operations

```csharp
public abstract class SchemaBase : ISchema
{
    protected SchemaBase(string name, MethodsAggregator methodsAggregator)
    {
        Name = name;
        _aggregator = methodsAggregator;
        AddSource<SingleRowSource>("empty");
        AddTable<SingleRowSchemaTable>("empty");
    }
    
    public void AddSource<TType>(string name, params object[] args)
    {
        var sourceName = $"{name.ToLowerInvariant()}{SourcePart}";
        AddToConstructors<TType>(sourceName);
        AdditionalArguments.Add(sourceName, args);
    }
}
```

**`RowSource`**:
- Abstract base for data iteration
- Provides cancellation token support
- Manages data streaming and buffering
- Supports chunked data processing

#### Data Flow Architecture

1. **Schema Resolution**:
   - Query analyzer identifies schema references
   - Schema provider resolves schema by name
   - Schema creates appropriate table/source instances

2. **Data Source Initialization**:
   - Parameters passed to constructors
   - Connection establishment
   - Metadata collection and validation

3. **Data Iteration**:
   - RowSource implements IEnumerable<T>
   - Lazy evaluation for memory efficiency
   - Cancellation token monitoring
   - Error handling and resource cleanup

### 3. Converter Module Architecture

```
AST → BuildChain Pipeline → C# Code Generation → Compilation Ready Code
```

#### Build Chain Pattern

The converter uses a chain of responsibility pattern for AST transformations:

**`BuildChain.cs`**:
```csharp
public abstract class BuildChain(BuildChain successor)
{
    protected readonly BuildChain Successor = successor;
    public abstract void Build(BuildItems items);
}
```

**Pipeline Stages**:

1. **`CreateTree`**:
   - Initial AST processing
   - Symbol table creation
   - Type information gathering
   - Schema validation

2. **`TranformTree`**:
   - AST transformations and optimizations
   - Expression rewriting
   - Join optimization
   - Predicate pushdown

3. **`TurnQueryIntoRunnableCode`**:
   - C# code generation
   - Method binding resolution
   - Runtime library integration
   - Assembly preparation

#### Code Generation Strategy

1. **Template-Based Generation**:
   - Uses string templates for code structure
   - Dynamic method insertion
   - Type-safe code generation

2. **Runtime Integration**:
   - Links with Musoq.Plugins library
   - Provides access to standard functions
   - Manages schema provider integration

3. **Optimization Techniques**:
   - Expression tree optimization
   - Dead code elimination
   - Loop unrolling for simple operations

### 4. Evaluator Module Architecture

```
Generated Code → Dynamic Compilation → Assembly → IRunnable → Execution → Table Results
```

#### Compilation Process

**`CompiledQuery.cs`**:
- Wraps IRunnable instances
- Provides synchronous execution interface
- Manages cancellation tokens
- Handles execution context

**Dynamic Compilation Features**:
1. **In-Memory Compilation**:
   - Uses Roslyn compiler
   - No file I/O required
   - Fast compilation for simple queries

2. **Assembly Management**:
   - Temporary assembly creation
   - Memory-efficient disposal
   - Debugging support

3. **Execution Context**:
   - Thread-safe execution
   - Cancellation token propagation
   - Error handling and reporting

#### Result Management

**`Tables/Table.cs`**:
- Represents query results
- Supports indexing and enumeration
- Provides column metadata
- Memory-efficient storage

### 5. Plugin System Architecture

```
LibraryBase → Standard Functions → Aggregations → Custom Methods
                                              ↓
BindableMethodAttribute → Method Resolution → Runtime Binding
```

#### Function Registration

**`LibraryBase`**:
- Base class for function libraries
- Provides standard implementations
- Supports method overloading
- Handles type conversions

**Method Binding Process**:
1. **Attribute Discovery**:
   - Scans for `[BindableMethod]` attributes
   - Collects method metadata
   - Validates method signatures

2. **Type Resolution**:
   - Matches parameter types
   - Handles generic methods
   - Supports nullable types

3. **Runtime Invocation**:
   - Dynamic method calls
   - Parameter conversion
   - Exception handling

#### Standard Library Organization

**Function Categories**:
- **String Functions** (`LibraryBaseStrings.cs`)
- **Math Functions** (`LibraryBaseMath.cs`)
- **Date/Time Functions** (`LibraryBaseDate.cs`)
- **Conversion Functions** (`LibraryBaseConverting.cs`)
- **Aggregation Functions** (`LibraryBaseSum.cs`, `LibraryBaseCount.cs`, etc.)

## Integration Patterns

### Schema Provider Integration

```csharp
public class CustomSchemaProvider : ISchemaProvider
{
    private readonly Dictionary<string, ISchema> _schemas = new();
    
    public void RegisterSchema(string name, ISchema schema)
    {
        _schemas[name.ToLowerInvariant()] = schema;
    }
    
    public ISchema GetSchema(string name)
    {
        return _schemas.TryGetValue(name.ToLowerInvariant(), out var schema) ? schema : null;
    }
}
```

### Query Execution Pipeline

```csharp
// 1. Parse Query
var lexer = new Lexer(sqlText);
var parser = new Parser(lexer);
var rootNode = parser.ComposeAll();

// 2. Build Execution Plan
var buildItems = new BuildItems(rootNode, schemaProvider, loggerResolver);
var buildChain = new CreateTree(new TranformTree(new TurnQueryIntoRunnableCode(null)));
buildChain.Build(buildItems);

// 3. Compile and Execute
var compiledQuery = new CompiledQuery(buildItems.Runnable);
var results = compiledQuery.Run();
```

### Error Handling Strategy

1. **Parse-Time Errors**:
   - Syntax validation
   - Token recognition failures
   - Grammar rule violations

2. **Build-Time Errors**:
   - Schema resolution failures
   - Type checking errors
   - Method binding failures

3. **Runtime Errors**:
   - Data source connection issues
   - Type conversion failures
   - Resource exhaustion

## Performance Considerations

### Memory Management

1. **Lazy Evaluation**:
   - Deferred query execution
   - Streaming data processing
   - Minimal memory footprint

2. **Resource Cleanup**:
   - Automatic disposal of data sources
   - Cancellation token monitoring
   - Memory leak prevention

### Optimization Opportunities

1. **Query Planning**:
   - Predicate pushdown to data sources
   - Join order optimization
   - Index usage hints

2. **Code Generation**:
   - Expression tree optimization
   - Inlined method calls
   - Type-specific optimizations

3. **Parallel Execution**:
   - Multi-threaded data processing
   - Parallel aggregations
   - Async data source support

This architecture enables Musoq to be both flexible and performant, supporting diverse data sources while maintaining type safety and SQL compatibility.