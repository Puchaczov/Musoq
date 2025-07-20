# Musoq Architecture Documentation

## Overview

Musoq is a SQL-like query engine designed to query diverse data sources without requiring a traditional database. It transforms SQL queries into executable code that can process files, APIs, databases, and other data sources through a flexible plugin architecture.

## High-Level Architecture

```
┌─────────────────┬─────────────────┬─────────────────┬─────────────────┐
│   SQL Query     │   Parse Tree    │  Execution Plan │     Results     │
│     Input       │   Generation    │   Generation    │    Execution    │
└─────────────────┴─────────────────┴─────────────────┴─────────────────┘
        │                   │                   │                   │
        ▼                   ▼                   ▼                   ▼
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Parser    │───▶│  Converter  │───▶│  Evaluator  │───▶│   Output    │
│   Module    │    │   Module    │    │   Module    │    │   Module    │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
                           │                   │
                           ▼                   ▼
                   ┌─────────────┐    ┌─────────────┐
                   │   Schema    │    │   Plugin    │
                   │   System    │    │   System    │
                   └─────────────┘    └─────────────┘
```

## Core Components

### 1. Parser Module (`Musoq.Parser`)

**Purpose**: Transforms SQL text into an Abstract Syntax Tree (AST)

**Key Components**:
- **Lexer**: Tokenizes SQL input into recognizable language elements
- **Parser**: Builds AST from token stream using recursive descent parsing
- **AST Nodes**: Represent different SQL constructs (SELECT, FROM, WHERE, etc.)

**Key Classes**:
- `Parser`: Main parsing logic with precedence handling
- `Lexer`: Token generation and lexical analysis
- `Node` hierarchy: Represents different SQL language constructs
- `Token` types: Define SQL language elements

**Responsibilities**:
- SQL syntax validation
- AST generation
- Error reporting for syntax issues
- Support for SQL extensions and custom syntax

### 2. Schema System (`Musoq.Schema`)

**Purpose**: Defines data source contracts and metadata management

**Key Components**:
- **ISchema Interface**: Contract for data source plugins
- **RowSource**: Abstract data provider interface
- **DataSources**: Base classes for implementing data providers
- **Runtime Context**: Execution environment and parameters

**Key Classes**:
- `ISchema`: Plugin interface for data sources
- `RowSource`: Base class for data iteration
- `IObjectResolver`: Dynamic property access interface
- `RuntimeContext`: Query execution context
- `SchemaMethodInfo`: Method metadata for data source operations

**Responsibilities**:
- Data source abstraction
- Type system management
- Method resolution for data source operations
- Metadata discovery and caching

### 3. Converter Module (`Musoq.Converter`)

**Purpose**: Transforms AST into executable C# code

**Key Components**:
- **Build Chain**: Multi-stage transformation pipeline
- **Visitors**: AST traversal and transformation logic
- **Code Generation**: C# code emission from AST

**Key Classes**:
- `BuildChain`: Chain of responsibility for transformations
- `ToCSharpRewriteTreeVisitor`: AST to C# code conversion
- `CreateTree`: Initial transformation setup
- `TurnQueryIntoRunnableCode`: Final code generation

**Transformation Pipeline**:
1. **CreateTree**: Initial AST processing and validation
2. **TranformTree**: Apply semantic transformations
3. **TurnQueryIntoRunnableCode**: Generate executable C# code

**Responsibilities**:
- AST semantic analysis
- Query optimization
- C# code generation
- Type inference and validation

### 4. Evaluator Module (`Musoq.Evaluator`)

**Purpose**: Compiles and executes generated code

**Key Components**:
- **Compilation**: Dynamic C# compilation
- **Runtime**: Query execution environment
- **Table Operations**: Result set management

**Key Classes**:
- `CompiledQuery`: Executable query wrapper
- `IRunnable`: Interface for executable queries
- `Table`: Result set representation
- `RuntimeLibraries`: Standard function library

**Responsibilities**:
- Dynamic compilation of generated C# code
- Query execution and result management
- Memory management during execution
- Performance monitoring and optimization

### 5. Plugin System (`Musoq.Plugins`)

**Purpose**: Extensible data source and function library

**Key Components**:
- **Standard Functions**: Built-in function library
- **Data Source Plugins**: External data source implementations
- **Function Extensions**: Custom function registration

**Standard Libraries**:
- String manipulation functions
- Mathematical operations
- Date/time operations
- Type conversion utilities
- Aggregation functions

## Query Processing Pipeline

### 1. Query Parsing
```
SQL Text → Lexer → Tokens → Parser → AST
```

### 2. Semantic Analysis
```
AST → Type Inference → Schema Resolution → Optimized AST
```

### 3. Code Generation
```
Optimized AST → C# Code Generation → Compilation → Executable
```

### 4. Execution
```
Executable → Data Source Access → Result Processing → Output
```

## Plugin Architecture

### Data Source Plugin Interface

```csharp
public interface ISchema
{
    string Name { get; }
    ISchemaTable GetTableByName(string name, RuntimeContext context, params object[] parameters);
    RowSource GetRowSource(string name, RuntimeContext context, params object[] parameters);
    // Method resolution for data source operations
    bool TryResolveMethod(string method, Type[] parameters, Type entityType, out MethodInfo methodInfo);
}
```

### Plugin Implementation Pattern

1. **Inherit from SchemaBase**: Provides common functionality
2. **Implement RowSource**: Define data iteration logic
3. **Define Schema Methods**: Expose data source-specific operations
4. **Register Plugin**: Make available to query engine

### Example Plugin Structure
```
MyDataSource/
├── MySchema.cs          # Main schema implementation
├── MyRowSource.cs       # Data source iterator
├── MyTable.cs           # Table metadata
└── MyEntityResolver.cs  # Property access resolver
```

## Data Flow Architecture

### Query Execution Flow

1. **Input Processing**
   - SQL query received
   - Initial validation and parsing

2. **AST Generation**
   - Token stream processed
   - Syntax tree constructed
   - Syntax validation

3. **Semantic Analysis**
   - Schema resolution
   - Type checking
   - Query optimization

4. **Code Generation**
   - AST transformation to C#
   - Runtime library integration
   - Compilation preparation

5. **Compilation**
   - Dynamic C# compilation
   - Assembly generation
   - Executable creation

6. **Execution**
   - Data source initialization
   - Query execution
   - Result aggregation

7. **Output**
   - Result formatting
   - Output delivery

### Data Source Integration

```
Query Engine ←→ Schema Interface ←→ Data Source Plugin ←→ External Data
     │                  │                    │                │
     │                  │                    │                │
  Manages           Abstracts           Implements        Provides
 Execution          Data Access         Data Logic        Raw Data
```

## Key Design Principles

### 1. **Extensibility First**
- Plugin-based architecture for data sources
- Visitor pattern for AST transformations
- Interface-driven design

### 2. **Performance Focus**
- Dynamic compilation for optimal execution
- Lazy evaluation where possible
- Memory-efficient data processing

### 3. **Type Safety**
- Strong typing throughout pipeline
- Compile-time error detection
- Runtime type validation

### 4. **SQL Compatibility**
- Standard SQL syntax support
- Extensions for non-relational data
- Familiar query patterns

## Integration Patterns

### 1. **Library Integration**
```csharp
// Direct API usage
var query = "SELECT * FROM #os.files('/path') WHERE Extension = '.txt'";
var compiledQuery = MusoqQueryCompiler.Compile(query);
var results = compiledQuery.Run();
```

### 2. **CLI Integration**
```bash
# Command-line usage
musoq "SELECT COUNT(*) FROM #git.commits('/repo')"
```

### 3. **Plugin Development**
```csharp
// Custom data source
public class MySchema : SchemaBase
{
    public override ISchemaTable GetTableByName(string name, RuntimeContext context, params object[] parameters)
    {
        return new MyTable(/* configuration */);
    }
}
```

## Performance Considerations

### 1. **Compilation Strategy**
- JIT compilation for dynamic queries
- Assembly caching for repeated queries
- Optimized IL generation

### 2. **Memory Management**
- Streaming data processing
- Minimal object allocation
- Efficient garbage collection

### 3. **Data Source Optimization**
- Predicate pushdown to data sources
- Parallel processing where applicable
- Connection pooling and reuse

## Error Handling Strategy

### 1. **Parse-Time Errors**
- Syntax validation
- Schema resolution failures
- Type checking errors

### 2. **Compile-Time Errors**
- Code generation failures
- Assembly compilation errors
- Dependency resolution issues

### 3. **Runtime Errors**
- Data source connection failures
- Type conversion errors
- Resource exhaustion handling

## Testing Architecture

### 1. **Unit Testing**
- Component isolation testing
- Mock data source implementations
- Parser and AST validation

### 2. **Integration Testing**
- End-to-end query processing
- Real data source testing
- Performance benchmarking

### 3. **Plugin Testing**
- Data source plugin validation
- Function library testing
- Compatibility verification

## Future Extensibility Points

### 1. **Query Optimization**
- Cost-based optimization
- Query plan caching
- Adaptive execution strategies

### 2. **Data Source Expansion**
- New plugin implementations
- Protocol-specific optimizations
- Cloud service integrations

### 3. **Language Extensions**
- Additional SQL features
- Domain-specific languages
- Query composition patterns

## Security Considerations

### 1. **Code Generation Security**
- Sanitized code generation
- Assembly isolation
- Resource access controls

### 2. **Data Source Security**
- Authentication integration
- Authorization checks
- Secure communication protocols

### 3. **Plugin Security**
- Plugin validation
- Sandboxed execution
- Permission management

---

This architecture documentation provides a comprehensive overview of Musoq's design and implementation. The modular architecture enables extensibility while maintaining performance and type safety throughout the query processing pipeline.